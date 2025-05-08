using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Azure.Core;
using System.Security.Cryptography;
using System.Text.Json;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using CustardRM.Interfaces;
using CustardRM.Models.DTOs;
using CustardRM.Models.Entities;
using CustardRM.Models.Requests;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Services.AI;
using CustardRM.Models.Entities.Sales;
using CustardRM.Models.Entities.Purchases;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.ML.Data;
using CustardRM.Models.DTOs.StockItem;
using CustardRM.Models.DTOs.Customer;
using CustardRM.Models.DTOs.AI;
using CustardRM.Models.DTOs.Supplier;
using Microsoft.EntityFrameworkCore.Storage;
using CustardRM.Models.Entities.AI;
using Microsoft.ML.Tokenizers;

namespace CustardRM.Services;

public class DatabaseService : IDatabaseService
{
	private readonly IConfiguration _config;
	private readonly string _connString;
	private readonly PasswordHasher _passwordHasher;

	public DatabaseService(IConfiguration config)
	{
		_config = config;
		_connString = _config.GetConnectionString("DefaultConnection") ??
			throw new Exception("Cannot find DefaultConnection in appsettings.json");
		_passwordHasher = new PasswordHasher();
	}

	public class DatabaseResult
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public object? Result { get; set; }
	}

	protected virtual IDbConnection CreateConnection() => new SqlConnection(_connString);

	public DatabaseResult StoreToken(int userID, string token, DateTime expiresAt)
	{
		DeleteTokensForUser(userID);

		using var connection = CreateConnection();

		DatabaseResult dbResult = new();

		var sql = @"INSERT INTO [UserToken] (UserID, Value, ExpiresAt)
					VALUES (@UserID, @Value, @ExpiresAt)";

		var param = new
		{
			UserID = userID,
			Value = token,
			ExpiresAt = expiresAt
		};

		var result = connection.Execute(sql, param);

		if (result == 1)
		{
			dbResult.Success = true;
			dbResult.Message = @$"[DatabaseResult] Token stored successfully.";
		}
		else
		{
			dbResult.Success = false;
			dbResult.Message = @$"[DatabaseResult] Unexpected number of rows created.
								  [DatabaseResult] Expected: 1 Actual: {result}";
		}

		return dbResult;
	}

	private void DeleteTokensForUser(int userID)
	{
		using var connection = CreateConnection();

		var sql = @"delete from UserToken
					where UserID = @UserID";

		var param = new
		{
			UserID = userID
		};

		connection.Execute(sql, param);
	}

	public string ExtractTokenFromAuthHeaderString(string authHeader)
	{
		if (!authHeader.StartsWith("Bearer {\"token\":\""))
		{
			throw new Exception($"AuthHeader not in expected format 'Bearer ...' actual: {authHeader}");
		}

		// Cut out token value from auth header string e.g. Bearer "token":"VALUE" -> VALUE
		return authHeader.Substring(17, 64);
	}

	public DatabaseResult ValidateToken(string token, int refreshMinutes)
	{
		if (string.IsNullOrEmpty(token))
		{
			throw new Exception("Token is required");
		}

		DatabaseResult dbResult = new();

		string tokenSubstring = ExtractTokenFromAuthHeaderString(token);

		using var connection = CreateConnection();

		var sql = @"select ID, ExpiresAt from UserToken
					where Value = @Token";

		var param = new
		{
			Token = tokenSubstring
		};

		var results = connection.Query<(int, DateTime)>(sql, param).ToList();

		if (results.Count > 1)
		{
			throw new Exception("Found more results than expected when validating token");
		}

		if (results.Count == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Token is invalid - DOES NOT EXIST";
			return dbResult;
		}

		if (results.First().Item2 > DateTime.UtcNow)
		{
			dbResult.Success = true;
			dbResult.Message = $"[DatabaseResult] Token is valid";

			// refresh token before returning
			var resultOfTokenRefresh = RefreshToken(results.First().Item1, refreshMinutes);

			if (resultOfTokenRefresh.Success == true)
			{
				Console.WriteLine(resultOfTokenRefresh.Message);
			}
		}
		else
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Token is invalid - TOKEN HAS EXPIRED";
		}

		return dbResult;
	}

	public DatabaseResult GetSupplierView(int supplierID)
	{
		using var connection = CreateConnection();

		DatabaseResult dbResult = new();

		Supplier supplier = (Supplier)GetSupplierByID(supplierID).Result;

		if (supplier == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseService] Could not find supplier with id: {supplierID}";
			return dbResult;
		}

		List<PurchaseOrder> purchaseOrders = GetPurchaseOrdersForSupplier(supplierID);
		float avgLeadTime = GetAverageLeadTimeForSupplier(supplierID, purchaseOrders);
		float leadTimeVariance = GetLeadTimeVarianceForSupplier(supplierID, purchaseOrders);

		SupplierView supplierView = new SupplierView
		{
			Supplier = supplier,
			AvgLeadTime = avgLeadTime,
			LeadTimeVariance = leadTimeVariance,
			PurchaseOrders = purchaseOrders
		};

		dbResult.Success = true;
		dbResult.Result = supplierView;

		return dbResult;
	}

	public float GetAverageLeadTimeForSupplier(int supplierID, List<PurchaseOrder> orders)
	{
		var leadTimes = orders
			.Where(o => o.OrderDate != default && o.ActualDeliveryDate != default)
			.Select(o => (o.ActualDeliveryDate - o.OrderDate).TotalDays)
			.ToList();

		if (!leadTimes.Any())
			return 0;

		return Convert.ToSingle(leadTimes.Average());
	}

	public float GetLeadTimeVarianceForSupplier(int supplierID, List<PurchaseOrder> orders)
	{
		var leadTimes = orders
			.Where(o => o.OrderDate != default && o.ActualDeliveryDate != default)
			.Select(o => (o.ActualDeliveryDate - o.OrderDate).TotalDays)
			.ToList();

		if (!leadTimes.Any())
			return 0;

		double avg = leadTimes.Average();
		double variance = leadTimes.Average(l => Math.Pow(l - avg, 2));

		return Convert.ToSingle(Math.Sqrt(variance));
	}


	private List<PurchaseOrder> GetPurchaseOrdersForSupplier(int supplierID)
	{
		using var connection = CreateConnection();

		var sql = @"select * from PurchaseOrder where SupplierID = @SupplierID";

		var param = new
		{
			SupplierID = supplierID
		};

		return connection.Query<PurchaseOrder>(sql, param).ToList();
	}

	protected virtual DatabaseResult RefreshToken(int tokenID, int refreshMinutes)
	{
		DatabaseResult dbResult = new();

		DateTime newTime = DateTime.UtcNow.AddMinutes(refreshMinutes);

		using var connection = CreateConnection();

		var sql = @"update UserToken
					set ExpiresAt = @ExpiresAt
					where ID = @ID";

		var param = new
		{
			ID = tokenID,
			ExpiresAt = newTime
		};

		int result = connection.Execute(sql, param);

		if (result == 1)
		{
			dbResult.Success = true;
			dbResult.Message = $@"[DatabaseResult] token refreshed successfully. New Value: {newTime}";
		}
		else
		{
			dbResult.Success = false;
			dbResult.Message = $@"[DatabaseResult] Unexpected number of rows affected
								  [DatabaseResult] Expected: 1 Actual: {result}";
		}

		return dbResult;
	}

	public DatabaseResult VerifyLoginDetails(LoginRequest req)
	{
		DatabaseResult dbResult = new();

		if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] Email or password is missing from login request";
			return dbResult;
		}

		using var connection = CreateConnection();
		var sql = @"SELECT ID, PasswordHash, PasswordSalt FROM [SystemUser] WHERE Email = @Email";
		var result = connection.QueryFirstOrDefault(sql, new { req.Email });

		if (result == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] Email provided does not exist";
			return dbResult;
		}

		int ID = result.ID;
		string passwordHash = result.PasswordHash;
		string salt = result.PasswordSalt;

		var passwordResult = _passwordHasher.VerifyPassword(req.Password, passwordHash, salt);
		dbResult.Success = passwordResult;

		if (passwordResult)
		{
			dbResult.Message = "[DatabaseResult] Password validation succeeded";
			dbResult.Result = ID;
		}
		else
		{
			dbResult.Message = "[DatabaseResult] Password validation failed";
			dbResult.Result = false;
		}

		return dbResult;
	}

	public DatabaseResult DoesEmailExist(string email)
	{
		DatabaseResult dbResult = new();

		if (string.IsNullOrEmpty(email))
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] Email is missing";
			return dbResult;
		}

		using var connection = CreateConnection();
		var sql = @"SELECT COUNT(*) FROM [SystemUser] WHERE Email = @Email";

		var result = connection.Query<int>(sql, new
		{
			Email = email,
		}).FirstOrDefault();


		if (result > 0)
		{
			dbResult.Success = true;
			dbResult.Message = $"[DatabaseResult] Email '{email}' already exists";
			dbResult.Result = false; // we use dbResult.Result to tell the caller whether the email exists or not
		}
		else
		{
			dbResult.Success = true;
			dbResult.Message = $"[DatabaseResult] Email '{email}' is not in use";
			dbResult.Result = true;
		}
		return dbResult;
	}

	public DatabaseResult CreateUser(CreateUserRequest req, string PasswordHash, string PasswordSalt)
	{
		using var connection = CreateConnection();

		var sql = @"INSERT INTO [SystemUser] (FirstName, LastName, Email, PasswordHash, PasswordSalt, PhoneNumber, CreatedAt, UpdatedAt)
					VALUES (@FirstName, @LastName, @Email, @PasswordHash, @PasswordSalt, @PhoneNumber, @CreatedAt, null)";

		var param = new
		{
			req.FirstName,
			req.LastName,
			req.Email,
			PasswordHash,
			PasswordSalt,
			PhoneNumber = req.PhoneNumber ?? null,
			CreatedAt = DateTime.UtcNow
		};

		int result = connection.Execute(sql, param);

		DatabaseResult dbResult = new();

		if (result > 0)
		{
			dbResult.Success = true;
			dbResult.Message = $"[DatabaseResult] User created successfully";
		}
		else
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Failed to create user";
		}
		return dbResult;
	}

	public DatabaseResult CreateReview(StockItemReviewCreateEdit review)
	{
		using var connection = CreateConnection();

		var sql = @"INSERT INTO StockItemReview (UserID, StockItemID, ReviewHelpfulness, ReviewText, ReviewRating, CreatedAt, PredictedAISentiment)
                    SELECT
                        @UserID,
                        @StockItemID,
                        @ReviewHelpfulness,
                        @ReviewText,
                        @ReviewRating,
                        @CreatedAt,
                        @PredictedAISentiment
                    WHERE EXISTS (SELECT 1 FROM Customer WHERE UserID = @UserID)
                      AND EXISTS (SELECT 1 FROM StockItem WHERE ID = @StockItemID)";

		var param = new
		{
			UserID = review.CustomerID,
			StockItemID = review.StockItemID,
			ReviewHelpfulness = "0/0",
			ReviewText = review.ReviewText,
			ReviewRating = review.Rating,
			CreatedAt = DateTime.UtcNow
		};

		int result = connection.ExecuteScalar<int>(sql, param);

		DatabaseResult dbResult = new();

		dbResult.Result = result;

		if (result == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Failed to create review";
			return dbResult;
		}
		else
		{
			dbResult.Success = true;
			dbResult.Message = "[DatabaseResult] Review created successfully";

			return dbResult;
		}
	}

	public void ExecuteSentimentAnalysisOnAllReviews()
	{
		using var connection = CreateConnection();

		// Get list of review IDs which dont have a sentiment
		var sql = @"select ID, StockItemID, ReviewText from StockItemReview
                    where PredictedAISentiment is null";

		var reviewsToAnalyse = connection.Query<(int ID, int StockItemID, string ReviewText)>(sql).ToList();

		Console.WriteLine($"[DatabaseService] {reviewsToAnalyse.Count} reviews to perform sentiment analysis");

		int count = 0;

		foreach (var review in reviewsToAnalyse)
		{
			var result = SentimentAnalysisService.AnalyseReviewText(review.ReviewText);

			StoreReviewAnalysisResult(review.ID, review.StockItemID, result);

			count++;

			Console.WriteLine($"[DatabaseService] {count} of {reviewsToAnalyse.Count} completed");
		}

		Console.WriteLine($"[DatabaseService] All {reviewsToAnalyse.Count} reviews have been analysed");
	}

	// This runs after we respond with status code ok, so write the output to console and dont return anything
	public void StoreReviewAnalysisResult(int reviewID, int stockItemID, SentimentAnalysisService.ReviewAnalysisResult analysisResult)
	{
		using var connection = CreateConnection();

		var sql = @"update StockItemReview
                    set PredictedAISentiment = @PredictedAISentiment
                    where ID = @ID";

		var param = new
		{
			PredictedAISentiment = analysisResult.Sentiment,
			ID = reviewID
		};

		int result = connection.Execute(sql, param);

		DatabaseResult dbResult = new();

		if (result == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"No rows updated, potential issue with review ID {reviewID}";
		}

		foreach (var bigramFrequency in analysisResult.BigramFrequencies)
		{
			var sqlBigram = @"MERGE StockItemReviewBigram AS target
                          USING (SELECT @StockItemID AS StockItemID, @Bigram AS Bigram, @Frequency AS Frequency) AS source
                              ON target.StockItemID = source.StockItemID AND target.Bigram = source.Bigram
                          WHEN MATCHED THEN
                              UPDATE SET Frequency = target.Frequency + source.Frequency
                          WHEN NOT MATCHED THEN
                              INSERT (StockItemID, Bigram, Frequency)
                              VALUES (source.StockItemID, source.Bigram, source.Frequency);";

			var paramBigram = new
			{
				StockItemID = stockItemID,
				Bigram = bigramFrequency.Key,
				Frequency = bigramFrequency.Value
			};

			connection.Execute(sqlBigram, paramBigram);
		}
	}

	public DatabaseResult GetAllStockItems()
	{
		using var connection = CreateConnection();

		var sql = @"select * from StockItem";

		var stockItems = connection.Query<StockItem>(sql).ToList();

		DatabaseResult dbResult = new();

		if (stockItems == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] stockItems is null";
		}

		List<StockItemTableRow> rows = new();

		foreach (var item in stockItems)
		{
			var sqlScore = @"select top 1 Score from ProfitabilityScorePrediction
							 where StockItemID = @StockItemID
							 order by GeneratedAt desc";

			var paramScore = new
			{
				StockItemID = item.ID
			};

			var result = connection.ExecuteScalar<float>(sqlScore, paramScore);

			StockItemTableRow row = new()
			{
				StockItem = item,
				ProfitabilityScore = result
			};

			rows.Add(row);
		}

		dbResult.Success = true;
		dbResult.Result = rows;

		return dbResult;
	}

	public DatabaseResult GetAllLinkedStockItemsAndSuppliers()
	{
		using var connection = CreateConnection();

		var sql = @"select pol.StockItemID, po.SupplierID from PurchaseOrderLine pol
					join PurchaseOrder po on pol.PurchaseOrderID = po.ID
					group by po.SupplierID, pol.StockItemID";

		var links = connection.Query<(int, int)>(sql).ToList();

		DatabaseResult dbResult = new();

		if (links == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] stockItems is null";
		}
		else if (links.Count == 0)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] stockItems is empty";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = links;
		}

		return dbResult;
	}

	public DatabaseResult GetMostRecentPurchaseOrderForStockItemID(int id)
	{
		using var connection = CreateConnection();

		var sql = @"select top 1 po.ActualDeliveryDate from PurchaseOrder po
					join PurchaseOrderLine pol on po.ID = pol.PurchaseOrderID
					where pol.StockItemID = @StockItemID
					order by po.ActualDeliveryDate desc";

		var param = new
		{
			StockItemID = id
		};

		var latest = connection.Query<DateTime>(sql, param).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (latest == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] Could not find an order for that ID";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = latest;
		}

		return dbResult;
	}

	public DatabaseResult GetAllSalesOrders()
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM SalesOrder";

		var salesOrders = connection.Query<SalesOrder>(sql).ToList();

		DatabaseResult dbResult = new();

		if (salesOrders == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] salesOrders is null";
		}

		foreach (SalesOrder so in salesOrders)
		{
			var sqlLines = @"SELECT * FROM SalesOrderLine where SalesOrderID = @SalesOrderID";

			var param = new
			{
				SalesOrderID = so.ID
			};

			so.SalesOrderLines.AddRange(connection.Query<SalesOrderLine>(sql).ToList());
		}

		dbResult.Success = true;
		dbResult.Result = salesOrders;

		return dbResult;
	}

	public DatabaseResult GetAllSalesOrderLinesOfStockItem(StockItem item)
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM SalesOrderLine where StockItemID = @StockItemID";

		var param = new
		{
			StockItemID = item.ID
		};

		var salesOrderLines = connection.Query<SalesOrderLine>(sql, param).ToList();

		DatabaseResult dbResult = new();

		if (salesOrderLines == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] salesOrderLines is null";
		}

		dbResult.Success = true;
		dbResult.Result = salesOrderLines;

		return dbResult;
	}

	public DatabaseResult CreatePurchaseOrder(PurchaseOrder po)
	{
		using var connection = CreateConnection();

		var sql = @"INSERT INTO PurchaseOrder (SupplierID, OrderDate, ExpectedDeliveryDate, ActualDeliveryDate, Status)
					VALUES(@SupplierID, @OrderDate, @ExpectedDeliveryDate, @ActualDeliveryDate, @Status)";

		var param = new
		{
			SupplierID = po.SupplierID,
			OrderDate = po.OrderDate,
			ExpectedDeliveryDate = po.ExpectedDeliveryDate,
			ActualDeliveryDate = po.ActualDeliveryDate,
			Status = po.Status
		};

		var poID = connection.Execute(sql, param);

		foreach (var pol in po.PurchaseOrderLines)
		{
			var sqlLine = @"INSERT INTO PurchaseOrderLine (PurchaseOrderID, StockItemID, QuantityOrdered, QuantityReceived, UnitCost)
							VALUES(@PurchaseOrderID, @StockItemID, @QuantityOrdered, @QuantityReceived, @UnitCost)";

			var paramLine = new
			{
				PurchaseOrderID = poID,
				StockItemID = pol.StockItemID,
				QuantityOrdered = pol.QuantityOrdered,
				QuantityReceived = pol.QuantityReceived,
				UnitCost = pol.UnitCost
			};

			connection.Execute(sqlLine, paramLine);
		}

		DatabaseResult dbResult = new();

		dbResult.Success = true;
		dbResult.Message = $"[DatabaseService] PO: {poID} created with {po.PurchaseOrderLines.Count} lines";

		return dbResult;
	}

	public DatabaseResult GetAllPurchaseOrderLinesOfStockItem(StockItem item)
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM PurchaseOrderLine where StockItemID = @StockItemID";

		var param = new
		{
			StockItemID = item.ID
		};

		var purchaseOrderLines = connection.Query<PurchaseOrderLine>(sql, param).ToList();

		DatabaseResult dbResult = new();

		if (purchaseOrderLines == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] purchaseOrderLines is null";
		}

		dbResult.Success = true;
		dbResult.Result = purchaseOrderLines;

		return dbResult;
	}

	public DatabaseResult GetAllSuppliers()
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM Supplier";

		var suppliers = connection.Query<Supplier>(sql).ToList();

		DatabaseResult dbResult = new();

		if (suppliers == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] suppliers is null";
		}

		dbResult.Success = true;
		dbResult.Result = suppliers;

		return dbResult;
	}

	public Supplier? GetRandomActiveSupplier()
	{
		var result = GetAllSuppliers();

		if (result == null || result.Success == false)
		{
			Console.WriteLine($"No suppliers found");

			return null;
		}

		List<Supplier> activeSuppliers = new();

		foreach (var item in (List<Supplier>)result.Result)
		{
			if (item.IsActive)
			{
				activeSuppliers.Add(item);
			}
		}

		var _rand = new Random();

		int chosenSupplierNum = _rand.Next(0, activeSuppliers.Count);
		Console.WriteLine($"Chosen number is {chosenSupplierNum}");
		var chosenSupplier = activeSuppliers[chosenSupplierNum];


		return chosenSupplier;
	}

	public DatabaseResult GetProfitabilityScoreMetricsByStockItemID(int id)
	{
		using var connection = CreateConnection();

		var sql = @"WITH Metrics AS (
					SELECT 
					SI.ID,
					(SI.UnitPrice - SI.CostPrice) / NULLIF(SI.UnitPrice, 0) AS ProfitMargin,
					CAST(SUM(SOL.Quantity) AS FLOAT) / NULLIF(DATEDIFF(DAY, SI.CreatedAt, GETDATE()), 0) AS SalesVelocity,
					ISNULL(AVG(R.ReviewRating), 3.0) AS AvgReviewRating,
					ISNULL(AVG(SOL.DiscountRate), 0.0) AS DiscountRate,
					LEAST(
						100,
						(SUM(SOL.Quantity) * SI.CostPrice) / 
						CASE 
							WHEN SI.StockLevel * SI.CostPrice = 0 THEN 1
							ELSE SI.StockLevel * SI.CostPrice 
						END
					) AS StockTurnoverRate,
					SI.StockLevel * SI.CostPrice * 0.01 AS HoldingCostEstimate,
					NULL AS ProfitabilityScore

					FROM StockItem SI
					JOIN SalesOrderLine SOL ON SOL.StockItemID = SI.ID
					JOIN SalesReturn SR ON SR.SalesOrderLineID = SOL.ID
					JOIN StockItemReview R ON R.StockItemID = SI.ID

					GROUP BY SI.ID, SI.UnitPrice, SI.CostPrice, SI.StockLevel, SI.CreatedAt
				),
				MinMax AS (
					SELECT
						MIN(ProfitMargin) AS MinProfitMargin, MAX(ProfitMargin) AS MaxProfitMargin,
						MIN(SalesVelocity) AS MinSalesVelocity, MAX(SalesVelocity) AS MaxSalesVelocity,
						MIN(AvgReviewRating) AS MinAvgReviewRating, MAX(AvgReviewRating) AS MaxAvgReviewRating,
						MIN(DiscountRate) AS MinDiscountRate, MAX(DiscountRate) AS MaxDiscountRate,
						MIN(StockTurnoverRate) AS MinTurnoverRate, MAX(StockTurnoverRate) AS MaxTurnoverRate,
						MIN(HoldingCostEstimate) AS MinHoldingCost, MAX(HoldingCostEstimate) AS MaxHoldingCost
					FROM Metrics
				)

				SELECT 
					M.ID,
					M.ProfitMargin,
					M.SalesVelocity,
					M.AvgReviewRating,
					M.DiscountRate,
					M.StockTurnoverRate,
					M.HoldingCostEstimate
				FROM Metrics M
				CROSS JOIN MinMax MM
				WHERE M.ID = @ID
				ORDER BY ProfitabilityScore DESC;";

		var param = new
		{
			ID = id
		};

		var metrics = connection.Query<ProfitabilityScoreMetrics>(sql, param).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (metrics == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Cannot find metrics for stock item ID: {id}";

			return dbResult;
		}

		dbResult.Success = true;
		dbResult.Result = metrics;

		return dbResult;
	}

	public void StoreProfitabilityScore(int stockItemID, ProfitabilityScoreResult result)
	{
		using var connection = CreateConnection();

		var sql = @"INSERT INTO ProfitabilityScorePrediction (StockItemID, Score, Justification, GeneratedAt)
					VALUES (@StockItemID, @Score, @Justification, @GeneratedAt)";

		var param = new
		{
			StockItemID = stockItemID,
			Score = result.Score,
			Justification = string.Join("\n", result.Justification),
			GeneratedAt = DateTime.UtcNow
		};

		connection.Execute(sql, param);
	}

	public DatabaseResult StoreReorderSuggestion(ReorderSuggestion suggestion)
	{
		using var connection = CreateConnection();

		var sql = @"INSERT INTO SuggestedReorder (StockItemID,SupplierID,SuggestedQuantity,SuggestedTiming,GeneratedAt)
					VALUES(@StockItemID,@SupplierID,@SuggestedQuantity,@SuggestedTiming,@GeneratedAt)";

		var param = new
		{
			StockItemID = suggestion.StockItemID,
			SupplierID = suggestion.SupplierID,
			SuggestedQuantity = suggestion.SuggestedQuantity,
			SuggestedTiming = suggestion.SuggestedTiming,
			GeneratedAt = suggestion.GeneratedAt
		};

		connection.Execute(sql, param);

		DatabaseResult dbResult = new();
		dbResult.Success = true;
		dbResult.Message = "[DatabaseService] Reorder suggestion successfully stored";

		return dbResult;
	}

	public DatabaseResult UpdateStockLevelOfStockItem(int itemID, int quantity)
	{
		if (quantity < 0)
		{
			throw new Exception($"[DatabaseService] Stock item quantity cannot go below 0 - quantity: {quantity}");
		}

		using var connection = CreateConnection();

		var sql = @"update StockItem set StockLevel = @StockLevel where ID = @ID";

		var param = new
		{
			StockLevel = quantity,
			ID = itemID
		};

		int result = connection.Execute(sql, param);

		DatabaseResult dbResult = new();

		if (result == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"Cannot find stock item ID: {itemID}";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Message = $"Successfully updated stock level of stock item ID: {itemID} to {quantity}";
		}

		return dbResult;
	}

	public DatabaseResult GetAllPurchaseOrders()
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM PurchaseOrder";

		var purchaseOrders = connection.Query<PurchaseOrder>(sql).ToList();

		DatabaseResult dbResult = new();

		if (purchaseOrders == null)
		{
			dbResult.Success = false;
			dbResult.Message = "[DatabaseResult] purchaseOrders is null";
		}

		foreach (PurchaseOrder po in purchaseOrders)
		{
			var sqlLines = @"SELECT * FROM PurchaseOrderLine where PurchaseOrderID = @PurchaseOrderID";

			var param = new
			{
				PurchaseOrderID = po.ID
			};

			po.PurchaseOrderLines.AddRange(connection.Query<PurchaseOrderLine>(sqlLines, param).ToList());
		}


		dbResult.Success = true;
		dbResult.Result = purchaseOrders;

		return dbResult;
	}

	public DatabaseResult UpdateStockItem(StockItem stockItem)
	{
		List<string> errors = new List<string>();

		if (stockItem.UnitPrice < 0)
		{
			throw new Exception($"[DatabaseService] Unit price cannot go below 0 - unit price: {stockItem.UnitPrice}");
		}

		if (stockItem.CostPrice < 0)
		{
			throw new Exception($"[DatabaseService] Cost price cannot go below 0 - cost price: {stockItem.CostPrice}");
		}

		if (stockItem.StockLevel < 0)
		{
			throw new Exception($"[DatabaseService] Stock Level cannot go below 0 - stock level: {stockItem.StockLevel}");
		}

		using var connection = CreateConnection();

		const string sql = @"
		UPDATE StockItem
		SET
			ItemCode = @ItemCode,
			ItemName = @ItemName,
			Description = @Description,
			UnitPrice = @UnitPrice,
			CostPrice = @CostPrice,
			StockLevel = @StockLevel,
			IsActive = @IsActive,
			MetaData = @MetaData,
			UpdatedAt = @UpdatedAt
		WHERE ID = @ID";

		var param = new
		{
			ItemCode = stockItem.ItemCode,
			ItemName = stockItem.ItemName,
			Description = stockItem.Description,
			UnitPrice = stockItem.UnitPrice,
			CostPrice = stockItem.CostPrice,
			StockLevel = stockItem.StockLevel,
			IsActive = stockItem.IsActive,
			MetaData = stockItem.MetaData,
			UpdatedAt = DateTime.UtcNow,
			ID = stockItem.ID
		};

		var result = connection.Execute(sql, param);

		DatabaseResult dbResult = new();

		if (result == 1)
		{
			dbResult.Success = true;
			dbResult.Message = $"Stock item {stockItem.ID} has been updated";
		}
		else if (result == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"Stock item {stockItem.ID} does not exist";
		}
		else
		{
			throw new Exception($"({result}) rows have been edited when trying to update {stockItem.ID}");
		}

		return dbResult;
	}

	public DatabaseResult GetStockItemViewByID(int id)
	{
		using var connection = CreateConnection();

		var stockItemResult = GetStockItemByID(id);

		if (!stockItemResult.Success)
		{
			return stockItemResult;
		}

		StockItem stockItem = (StockItem)stockItemResult.Result;

		var sqlScore = @"select top 1 * from ProfitabilityScorePrediction
							 where StockItemID = @StockItemID
							 order by GeneratedAt desc";

		var paramScore = new
		{
			StockItemID = stockItem.ID
		};

		var profitabilityScorePrediction = connection.Query<ProfitabilityScorePrediction>(sqlScore, paramScore).FirstOrDefault();

		var sqlReviews = "select * from StockItemReview where StockItemID = @StockItemID";

		var paramReviews = new
		{
			StockItemID = id
		};

		var reviews = connection.Query<Review>(sqlReviews, paramReviews).ToList();

		var sqlBigrams = @"select top 10 Bigram, Frequency from StockItemReviewBigram
						   where StockItemID = @StockItemID
						   order by Frequency desc";

		var paramBigrams = new
		{
			StockItemID = id
		};

		var result = connection.Query<(string Bigram, int Frequency)>(sqlBigrams, paramBigrams);
		Dictionary<string, int> bigrams = result.ToDictionary(x => x.Bigram, x => x.Frequency);

		DatabaseResult databaseResult = new();

		databaseResult.Message = $"[DatabaseService] Stock Item view retrieved for id: {id} reviews: {reviews.Count} bigrams: {bigrams}";
		databaseResult.Success = true;

		StockItemView stockItemView = new StockItemView(stockItem, profitabilityScorePrediction, reviews, bigrams);

		databaseResult.Result = stockItemView;

		return databaseResult;
	}

	public DatabaseResult GetStockItemByID(int id)
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM StockItem WHERE ID = @ID";

		var stockItem = connection.Query<StockItem>(sql, new { ID = id }).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (stockItem == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock item with an id of {id} could not be found";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = stockItem;
		}

		return dbResult;
	}

	public DatabaseResult GetAverageLeadTimeforSupplierForStockItem(int stockItemID, int supplierID)
	{
		using var connection = CreateConnection();

		var sql = @"SELECT 
						AVG(DATEDIFF(DAY, po.OrderDate, po.ActualDeliveryDate)) AS AvgLeadTime
					FROM PurchaseOrderLine pol
					JOIN PurchaseOrder po ON pol.PurchaseOrderID = po.ID
					WHERE po.SupplierID = @SupplierID
					  AND pol.StockItemID = @StockItemID
					  AND po.ActualDeliveryDate IS NOT NULL
					GROUP BY 
						pol.StockItemID,
						po.SupplierID";

		var param = new
		{
			SupplierID = supplierID,
			StockItemID = stockItemID
		};

		var averageLeadTime = connection.ExecuteScalar<int>(sql, param);

		DatabaseResult dbResult = new();

		dbResult.Success = true;
		dbResult.Result = averageLeadTime;

		return dbResult;
	}

	public DatabaseResult GetAllPurchaseOrdersForStockItemAndSupplier(int stockItemID, int supplierID)
	{
		using var connection = CreateConnection();

		var sql = @"select po.OrderDate, po.ExpectedDeliveryDate, po.ActualDeliveryDate from PurchaseOrder po
					join PurchaseOrderLine pol on po.id = pol.PurchaseOrderID
					where SupplierID = @SupplierID
					and pol.StockItemID = @StockItemID";

		var param = new
		{
			SupplierID = supplierID,
			StockItemID = stockItemID
		};

		List<(DateTime, DateTime, DateTime)> dates = connection.Query<(DateTime, DateTime, DateTime)>(sql, param).ToList();

		DatabaseResult dbResult = new();

		dbResult.Success = true;
		dbResult.Result = dates;

		return dbResult;
	}

	public DatabaseResult GetStockItemOrderDatesAndQuantities(int id)
	{
		using var connection = CreateConnection();

		var sql = @"select so.OrderDate, sol.Quantity from SalesOrderLine sol
					join SalesOrder so on sol.SalesOrderID = so.ID
					where sol.StockItemID = @StockItemID";

		var parameters = new
		{
			StockItemID = id
		};

		var dates = connection.Query<(DateTime, int)>(sql, parameters).ToList();

		DatabaseResult dbResult = new();

		dbResult.Success = true;
		dbResult.Result = dates;

		return dbResult;
	}

	public DatabaseResult GetStockItemByCode(string code, List<int>? excludedIDs = null)
	{
		using var connection = CreateConnection();

		code = code.ToLower();

		var sql = @"select * from StockItem where LOWER(ItemCode) = @ItemCode";

		if (excludedIDs != null && excludedIDs.Any())
		{
			sql += " AND ID NOT IN @ExcludedIDs";
		}

		var parameters = new
		{
			ItemCode = code,
			ExcludedIDs = excludedIDs
		};

		var stockItem = connection.Query<StockItem>(sql, parameters).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (stockItem == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock item with a code of {code} could not be found";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = stockItem;
		}

		return dbResult;
	}

	public DatabaseResult GetStockItemByName(string name, List<int>? excludedIDs = null)
	{
		using var connection = CreateConnection();

		name = name.ToLower();

		var sql = @"select * from StockItem where LOWER(ItemName) = @ItemName";

		if (excludedIDs != null && excludedIDs.Any())
		{
			sql += " AND ID NOT IN @ExcludedIDs";
		}

		var parameters = new
		{
			ItemName = name,
			ExcludedIDs = excludedIDs
		};

		var stockItem = connection.Query<StockItem>(sql, parameters).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (stockItem == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock item with a name of {name} could not be found";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = stockItem;
		}

		return dbResult;
	}

	public DatabaseResult GetSupplierByID(int id)
	{
		using var connection = CreateConnection();

		var sql = @"SELECT * FROM Supplier where ID = @ID";

		var param = new
		{
			ID = id
		};

		var supplier = connection.Query<Supplier>(sql, param).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (supplier == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Category with an id of {id} could not be found";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = supplier;
		}

		return dbResult;
	}

	public DatabaseResult GetLinkedSuppliersByStockItemID(int id)
	{
		DatabaseResult dbResult = new();

		var result = GetStockItemByID(id);

		if (result.Success == false)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock Item with ID {id} could not be found";
			return dbResult;
		}

		using var connection = CreateConnection();

		var sql = @"select c.* from StockItem si
					join StockItemSupplierLink cl on si.ID = sl.StockItemID
					join Supplier s on sl.SupplierID = s.SupplierID
					where si.ID = @ID";

		var suppliers = connection.Query<Supplier>(sql, new { ID = id }).ToList();

		if (suppliers == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Suppliers is null";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = suppliers;
		}

		return dbResult;
	}

	public DatabaseResult CreateStockItemSupplierLink(SupplierLink sLinkDTO)
	{
		DatabaseResult dbResult = new();

		using var connection = CreateConnection();

		var sql = @"insert into StockItemSupplierLink (StockItemID, SupplierID, LeadTimeDays, AverageLeadTime, IsActive, CreatedAt)
					VALUES (@StockItemID, @SupplierID, @LeadTimeDays, @AverageLeadTimeDays, @IsActive, @CreatedAt)";

		var parameters = new
		{
			sLinkDTO.StockItemID,
			sLinkDTO.SupplierID,
			sLinkDTO.LeadTimeDays,
			sLinkDTO.AverageLeadTime,
			CreatedAt = DateTime.UtcNow
		};

		connection.Execute(sql, parameters);

		dbResult.Success = true;
		dbResult.Message = $"[DatabaseResult] Link between stock item ID: {sLinkDTO.StockItemID} and supplier ID: {sLinkDTO.SupplierID} has been created";

		return dbResult;
	}

	public DatabaseResult GetAllCustomers()
	{
		DatabaseResult dbResult = new();

		using var connection = CreateConnection();

		var sqlCustomer = @"select * from Customer";

		List<Customer> customers = connection.Query<Customer>(sqlCustomer).ToList();

		if (customers == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseService] Could not retreieve customer list";

			return dbResult;
		}

		dbResult.Success = true;
		dbResult.Result = customers;

		return dbResult;
	}

	public DatabaseResult GetCustomerViewByID(int id)
	{
		DatabaseResult dbResult = new();

		using var connection = CreateConnection();

		var sqlCustomer = @"select * from Customer where ID = @ID";

		var paramCustomer = new
		{
			ID = id
		};

		Customer customer = connection.Query<Customer>(sqlCustomer, paramCustomer).FirstOrDefault();

		if (customer == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseService] Customer with id: {id} cannot be found";

			return dbResult;
		}

		var sqlReviews = @"select sir.* from StockItemReview sir
						   join Customer c on sir.UserID = c.UserID
						   where c.ID = @ID";

		var paramReviews = new
		{
			ID = id
		};

		List<Review> reviews = connection.Query<Review>(sqlReviews, paramReviews).ToList();

		var sqlSalesOrder = @"select * from SalesOrder
							   where CustomerRef = @CustomerRef";

		var paramSalesOrder = new
		{
			CustomerRef = $"WEB-{id}"
		};

		List<SalesOrder> salesOrders = connection.Query<SalesOrder>(sqlSalesOrder, paramSalesOrder).ToList();

		var sqlSalesReturn = @"select sr.* from salesorder so
							   join SalesOrderLine sol on so.ID = sol.SalesOrderID
							   join SalesReturn sr on sol.ID = sr.SalesOrderLineID
							   where so.CustomerRef = @CustomerRef";

		var paramSalesReturn = new
		{
			CustomerRef = $"WEB-{id}"
		};

		List<SalesReturn> salesReturns = connection.Query<SalesReturn>(sqlSalesReturn, paramSalesReturn).ToList();

		CustomerView customerView = new CustomerView()
		{
			Customer = customer,
			Reviews = reviews,
			Orders = salesOrders,
			Returns = salesReturns
		};

		dbResult.Success = true;
		dbResult.Result = customerView;

		return dbResult;
	}

	public DatabaseResult DeleteStockItemSupplierLink(int stockItemID, int supplierID)
	{
		DatabaseResult dbResult = new();

		using var connection = CreateConnection();

		var deleteSql = "DELETE FROM StockItemSupplierLink WHERE StockItemID = @StockItemID and SupplierID = @SupplierID";
		int rows = connection.Execute(deleteSql, new { StockItemID = stockItemID, SupplierID = supplierID });

		if (rows == 0)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Link between Stock Item ID: {stockItemID} and Supplier ID: {supplierID} does not exist";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Message = $"[DatabaseResult] Link between stock item ID: {stockItemID} and Supplier ID: {supplierID} has been deleted";
		}

		return dbResult;
	}

	public DatabaseResult CreateStockItem(StockItemCreate stockItemDTO)
	{
		List<string> errors = new List<string>();

		if (stockItemDTO.UnitPrice < 0)
		{
			throw new Exception($"[DatabaseService] Unit price cannot go below 0 - unit price: {stockItemDTO.UnitPrice}");
		}

		if (stockItemDTO.CostPrice < 0)
		{
			throw new Exception($"[DatabaseService] Cost price cannot go below 0 - cost price: {stockItemDTO.CostPrice}");
		}

		if (stockItemDTO.StockLevel < 0)
		{
			throw new Exception($"[DatabaseService] Stock Level cannot go below 0 - stock level: {stockItemDTO.StockLevel}");
		}

		DatabaseResult dbResult = new();

		var resultByCode = GetStockItemByCode(stockItemDTO.ItemCode.ToLower());

		if (resultByCode.Success == true)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock Item with code {stockItemDTO.ItemCode} already exists";
			return dbResult;
		}

		var resultByName = GetStockItemByName(stockItemDTO.ItemName.ToLower());

		if (resultByCode.Success == true)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Stock Item with name {stockItemDTO.ItemName} already exists";
			return dbResult;
		}

		using var connection = CreateConnection();

		var sql = @"INSERT INTO [StockItem] (ItemCode, ItemName, Description, UnitPrice, CostPrice, StockLevel, IsActive, MetaData, CreatedAt, UpdatedAt)
					VALUES (@ItemCode, @ItemName, @Description, @UnitPrice, @CostPrice, @StockLevel, @IsActive, @MetaData, @CreatedAt, null)";

		var param = new
		{
			stockItemDTO.ItemCode,
			stockItemDTO.ItemName,
			stockItemDTO.Description,
			stockItemDTO.UnitPrice,
			stockItemDTO.CostPrice,
			stockItemDTO.StockLevel,
			stockItemDTO.MetaData,
			stockItemDTO.IsActive,
			CreatedAt = DateTime.UtcNow
		};

		connection.Execute(sql, param);

		dbResult.Success = true;
		dbResult.Message = $"[DatabaseResult] Stock item {stockItemDTO.ItemCode} successfully created";

		return dbResult;
	}

	public DatabaseResult GetCategoryByName(string name)
	{
		using var connection = CreateConnection();

		name = name.ToLower();

		var sql = @"select * from Category where LOWER(CategoryName) = @CategoryName";

		var category = connection.Query<StockItem>(sql, new { CategoryName = name }).FirstOrDefault();

		DatabaseResult dbResult = new();

		if (category == null)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Category with a code of {name} could not be found";
		}
		else
		{
			dbResult.Success = true;
			dbResult.Result = category;
		}

		return dbResult;
	}

	public DatabaseResult CreateCategory(CategoryCreateEdit category)
	{
		DatabaseResult dbResult = new();

		var result = GetCategoryByName(category.CategoryName.ToLower());

		if (result.Success == true)
		{
			dbResult.Success = false;
			dbResult.Message = $"[DatabaseResult] Category with name {category.CategoryName} already exists";
			return dbResult;
		}

		using var connection = CreateConnection();

		var sql = @"INSERT INTO [Category] (CategoryName, CategoryDescription, CreatedAt, UpdatedAt)
					VALUES (@ItemCode, @ItemName, @CreatedAt, null);";

		var param = new
		{
			category.CategoryName,
			category.CategoryDescription,
			CreatedAt = DateTime.UtcNow
		};

		connection.Execute(sql, param);

		dbResult.Success = true;
		dbResult.Message = $"[DatabaseResult] Category {category.CategoryName} successfully created";

		return dbResult;
	}
}
