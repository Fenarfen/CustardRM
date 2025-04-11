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

    public IDbConnection CreateConnection() => new SqlConnection(_connString);

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

    public DatabaseResult ValidateToken(string token, int refreshMinutes)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Token is required");
        }

        if (!token.StartsWith("Bearer {\"token\":\""))
        {
            throw new Exception($"Token not in expected format 'Bearer ...' actual: {token}");
        }

        DatabaseResult dbResult = new();

        // Cut out token value from auth header string e.g. Bearer "token":"VALUE" -> VALUE
        string tokenSubstring = token.Substring(17, 64);

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

    private DatabaseResult RefreshToken(int tokenID, int refreshMinutes)
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
        var sql = @"SELECT ID, PasswordHash, PasswordSalt FROM [User] WHERE Email = @Email";
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
        var sql = @"SELECT COUNT(*) FROM [User] WHERE Email = @Email";

        var result = connection.Query<int>(sql, new
        {
            Email = email,
        }).FirstOrDefault();


        if (result > 0)
        {
            dbResult.Success = true;
            dbResult.Message = $"[DatabaseResult] Email '{email}' already exists";
            dbResult.Result = false; // we use dbResult.Result to tell the caller whether the email is available or not
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

        var sql = @"INSERT INTO [User] (FirstName, LastName, Email, PasswordHash, PasswordSalt, PhoneNumber, CreatedAt, UpdatedAt)
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

    public DatabaseResult GetAllStockItems()
    {
        using var connection = CreateConnection();

        var sql = @"SELECT * FROM StockItem";

        var stockItems = connection.Query<StockItem>(sql).ToList();

        DatabaseResult dbResult = new();

        if (stockItems == null)
        {
            dbResult.Success = false;
            dbResult.Message = "[DatabaseResult] stockItems is null";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Result = stockItems;
        }

        return dbResult;
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

    public DatabaseResult GetCategories()
    {
        using var connection = CreateConnection();

        var sql = @"SELECT * FROM Category";

        var categories = connection.Query<Category>(sql).ToList();

        DatabaseResult dbResult = new();

        if (categories == null)
        {
            dbResult.Success = false;
            dbResult.Message = "[DatabaseResult] categories is null";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Result = categories;
        }

        return dbResult;
    }

    public DatabaseResult GetCategoryByID(int id)
    {
        using var connection = CreateConnection();

        var sql = @"SELECT * FROM Category where CategoryID = @CategoryID";

        var category = connection.Query<Category>(sql, new { CategoryID = id }).FirstOrDefault();

        DatabaseResult dbResult = new();

        if (category == null)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Category with an id of {id} could not be found";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Result = category;
        }

        return dbResult;
    }

    public DatabaseResult GetSupplierByID(int id)
    {
        using var connection = CreateConnection();

        var sql = @"SELECT * FROM Supplier where SupplierID = @SupplierID";

        var supplier = connection.Query<Supplier>(sql, new { SupplierID = id }).FirstOrDefault();

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

    public DatabaseResult GetLinkedCategoriesByStockItemID(int id)
    {
        DatabaseResult dbResult = new();

        var result = GetCategoryByID(id);

        if (result.Success == false)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Category with ID {id} could not be found";
            return dbResult;
        }

        using var connection = CreateConnection();

        var sql = @"select c.* from StockItem si
					join StockItemCategoryLink cl on si.ID = cl.StockItemID
					join Category c on cl.CategoryID = c.CategoryID
					where si.ID = @ID";

        var categories = connection.Query<Category>(sql, new { ID = id }).ToList();

        if (categories == null)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] categories is null";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Result = categories;
        }

        return dbResult;
    }

    public DatabaseResult CreateStockItemCategoryLink(int stockItemID, int categoryID)
    {
        DatabaseResult dbResult = new();

        using var connection = CreateConnection();

        var sql = "insert into StockItemCategoryLink (StockItemID, CategoryID, CreatedAt) VALUES (@StockItemID, @CategoryID, @CreatedAt)";

        var parameters = new
        {
            StockItemID = stockItemID,
            CategoryID = categoryID,
            CreatedAt = DateTime.UtcNow
        };

        connection.Execute(sql, parameters);

        dbResult.Success = true;
        dbResult.Message = $"[DatabaseResult] Link between stock item ID: {stockItemID} and category ID: {categoryID} has been created";

        return dbResult;
    }

    public DatabaseResult DeleteStockItemCategoryLink(int stockItemID, int categoryID)
    {
        DatabaseResult dbResult = new();

        using var connection = CreateConnection();

        var deleteSql = "DELETE FROM StockItemCategoryLink WHERE StockItemID = @StockItemID and CategoryID = @CategoryID";
        int rows = connection.Execute(deleteSql, new { StockItemID = stockItemID, CategoryID = categoryID });

        if (rows == 0)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Link between Stock Item ID: {stockItemID} and Category ID: {categoryID} does not exist";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Message = $"[DatabaseResult] Link between stock item ID: {stockItemID} and category ID: {categoryID} has been deleted";
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

    public DatabaseResult CreateStockItemSupplierLink(SupplierLinkDTO sLinkDTO)
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

    public DatabaseResult GetSubcategoriesByCategoryID(int CategoryID)
    {
        DatabaseResult dbResult = new();

        var result = GetCategoryByID(CategoryID);

        if (result.Success == false)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Category with ID {CategoryID} could not be found";
            return dbResult;
        }

        using var connection = CreateConnection();

        var sql = @"select * from Subcategory where CategoryID = @CategoryID";

        var subcategories = connection.Query<Subcategory>(sql, new { CategoryID }).ToList();

        if (subcategories == null)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Subcategories is null";
        }
        else
        {
            dbResult.Success = true;
            dbResult.Result = subcategories;
        }

        return dbResult;
    }

    public DatabaseResult CreateStockItem(StockItemDTO stockItemDTO)
    {
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

        var sql = @"INSERT INTO [StockItem] (ItemCode, ItemName, Description, UnitPrice, CostPrice, StockLevel, IsActive, CreatedAt, UpdatedAt)
					VALUES (@ItemCode, @ItemName, @Description, @UnitPrice, @CostPrice, @StockLevel, @IsActive, @CreatedAt, null)
					SELECT CAST(SCOPE_IDENTITY() AS int);";

        var param = new
        {
            stockItemDTO.ItemCode,
            stockItemDTO.ItemName,
            stockItemDTO.Description,
            stockItemDTO.UnitPrice,
            stockItemDTO.CostPrice,
            stockItemDTO.StockLevel,
            stockItemDTO.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        int newStockItemId = connection.ExecuteScalar<int>(sql, param);

        // Category links
        foreach (var categoryID in stockItemDTO.LinkedCategories)
        {
            var result = GetCategoryByID(categoryID);

            // Should be checked earlier as well, but here we ensure that the category exists before creating the link
            if (result.Success == false)
            {
                continue;
            }

            var sqlCategoryLinks = @"insert into StockItemCategoryLink (StockItemID, CategoryID, CreatedAt)
						values (@StockItemID, @CategoryID, GETUTCDATE())";

            var paramCategoryLinks = new
            {
                StockItemID = newStockItemId,
                CategoryID = categoryID
            };

            connection.Execute(sqlCategoryLinks, paramCategoryLinks);
        }

        // Supplier Links
        foreach (var supplierID in stockItemDTO.LinkedSuppliers)
        {
            var result = GetSupplierByID(supplierID);

            // Should be checked earlier as well, but here we ensure that the supplier exists before creating the link
            if (result.Success == false)
            {
                continue;
            }

            var sqlCategoryLinks = @"insert into StockItemSupplierLink (StockItemID, SupplierID, LeadTimeDays, AverageLeadTime, IsActive, CreatedAt)
									 values (@StockItemID, @SupplierID, 0, 0, 1, GETUTCDATE())";

            var paramCategoryLinks = new
            {
                StockItemID = newStockItemId,
                SupplierID = supplierID
            };

            connection.Execute(sqlCategoryLinks, paramCategoryLinks);
        }

        dbResult.Success = true;
        dbResult.Message = $"[DatabaseResult] Stock item {stockItemDTO.ItemCode} successfully created";

        return dbResult;
    }

    public DatabaseResult EditStockItem(StockItemViewDTO stockItemDTO)
    {
        DatabaseResult dbResult = new();

        // Check if Stock Item exists
        var resultByID = GetStockItemByID(stockItemDTO.StockItem.ID);

        if (resultByID.Success == false)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Stock Item with ID {stockItemDTO.StockItem.ID} does not exist";
            return dbResult;
        }

        List<int> excludedIDs = new();

        excludedIDs.Add(stockItemDTO.StockItem.ID);

        // Check that the item code isn't already in use, must exclude stock item we're editing by ID in case the code hasn't changed
        var resultByCode = GetStockItemByCode(stockItemDTO.StockItem.ItemCode, excludedIDs);

        if (resultByCode.Success == true)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Stock Item with code {stockItemDTO.StockItem.ItemCode} already exists";
            return dbResult;
        }

        // Check that the item name isn't already in use, must exclude stock item we're editing by ID in case the code hasn't changed
        var resultByName = GetStockItemByName(stockItemDTO.StockItem.ItemName, excludedIDs);

        if (resultByName.Success == true)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Stock Item with name {stockItemDTO.StockItem.ItemName} already exists";
            return dbResult;
        }

        using var connection = CreateConnection();

        var sql = @"update StockItem
					set ItemCode = @ItemCode,
					ItemName = @ItemName,
					Description = @Description,
					UnitPrice = @UnitPrice,
					CostPrice = @CostPrice,
					StockLevel = @StockLevel,
					IsActive = @IsActive,
					UpdatedAt = @UpdatedAt
					where ID = @StockItemID";

        var param = new
        {
            stockItemDTO.StockItem.ItemCode,
            stockItemDTO.StockItem.ItemName,
            stockItemDTO.StockItem.Description,
            stockItemDTO.StockItem.UnitPrice,
            stockItemDTO.StockItem.CostPrice,
            stockItemDTO.StockItem.StockLevel,
            stockItemDTO.StockItem.IsActive,
            UpdatedAt = DateTime.UtcNow,
            StockItemID = stockItemDTO.StockItem.ID
        };

        var rows = connection.Execute(sql, param);

        if (rows == 0)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Could not update stock item ID: {stockItemDTO.StockItem.ID}";
            return dbResult;
        }

        List<int> existingLinkedCategoryIDs = new List<int>();
        List<Category> categoryLinksToCreate = new List<Category>();
        List<int> categoryLinksToRemove = new List<int>();

        var result = GetLinkedCategoriesByStockItemID(stockItemDTO.StockItem.ID);

        if (result.Success == true)
        {
            foreach (var cat in stockItemDTO.LinkedCategories)
            {
                if (existingLinkedCategoryIDs.Contains(cat.CategoryID))
                {
                    categoryLinksToCreate.Add(cat);
                }
            }

            foreach (var cat in existingLinkedCategoryIDs)
            {
                if (!stockItemDTO.LinkedCategories.Any(c => c.CategoryID == cat))
                {
                    categoryLinksToRemove.Add(cat);
                }
            }
        }

        foreach (var item in categoryLinksToCreate)
        {
            var resultOfCreateCategoryLink = CreateStockItemCategoryLink(stockItemDTO.StockItem.ID, item.CategoryID);

            Console.WriteLine(resultOfCreateCategoryLink.Message);
        }

        foreach (var item in categoryLinksToRemove)
        {
            var resultOfRemoveCategoryLink = DeleteStockItemCategoryLink(stockItemDTO.StockItem.ID, item);

            Console.WriteLine(resultOfRemoveCategoryLink.Message);
        }

        List<int> existingLinkedSupplierIDs = new List<int>();
        List<SupplierLinkDTO> supplierLinksToCreate = new List<SupplierLinkDTO>();
        List<int> supplierLinksToRemove = new List<int>();

        var resultSupplier = GetLinkedSuppliersByStockItemID(stockItemDTO.StockItem.ID);

        if (resultSupplier.Success == true)
        {
            foreach (var sup in stockItemDTO.LinkedSuppliers)
            {
                if (existingLinkedSupplierIDs.Contains(sup.SupplierID))
                {
                    supplierLinksToCreate.Add(sup);
                }
            }

            foreach (var sup in existingLinkedSupplierIDs)
            {
                if (!stockItemDTO.LinkedSuppliers.Any(s => s.SupplierID == sup))
                {
                    supplierLinksToRemove.Add(sup);
                }
            }
        }

        foreach (var item in supplierLinksToCreate)
        {
            var resultOfCreateSupplierLink = CreateStockItemSupplierLink(item);

            Console.WriteLine(resultOfCreateSupplierLink.Message);
        }

        foreach (var item in supplierLinksToRemove)
        {
            var resultOfRemoveSupplierLink = DeleteStockItemSupplierLink(stockItemDTO.StockItem.ID, item);

            Console.WriteLine(resultOfRemoveSupplierLink.Message);
        }

        dbResult.Success = true;
        dbResult.Message = $"[DatabaseResult] Stock Item ID: {stockItemDTO.StockItem.ID} successfully updated";

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

    public DatabaseResult CreateCategory(CategoryDTO category)
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

    public DatabaseResult EditCategory(CategoryDTO category)
    {
        DatabaseResult dbResult = new();

        var result = GetCategoryByID(category.CategoryID);

        if (result.Success == false)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Cannot find category with ID {category.CategoryID}";
            return dbResult;
        }

        using var connection = CreateConnection();

        var sql = @"update Category
					set CategoryName = @CategoryName,
					CategoryDescription = @CategoryDescription,
					UpdatedAt = @UpdatedAt
					where CategoryID = @CategoryID";

        var param = new
        {
            category.CategoryName,
            category.CategoryDescription,
            UpdatedAt = DateTime.UtcNow,
            category.CategoryID
        };

        connection.Execute(sql, param);

        dbResult.Success = true;
        dbResult.Message = $"[DatabaseResult] Category {category.CategoryName} successfully updated";

        return dbResult;
    }

    public DatabaseResult GetStockItemDataByID(int stockItemID)
    {
        DatabaseResult dbResult = new();

        var resultStockItem = GetStockItemByID(stockItemID);

        if (resultStockItem.Success == false)
        {
            dbResult.Success = false;
            dbResult.Message = $"[DatabaseResult] Stock item with an ID {stockItemID} could not be found";
            return dbResult;
        }

        var resultLinkedCategories = GetLinkedCategoriesByStockItemID(stockItemID);
        var resultLinkedSuppliers = GetLinkedSuppliersByStockItemID(stockItemID);

        StockItemViewDTO stockItemViewDTO = new()
        {
            StockItem = (StockItem)resultStockItem.Result,
            LinkedCategories = (List<Category>)resultLinkedCategories.Result,
            LinkedSuppliers = (List<SupplierLinkDTO>)resultLinkedSuppliers.Result
        };

        dbResult.Success = true;
        dbResult.Message = "[DatabaseResult] Stock Item Data retreieved successfully";
        dbResult.Result = stockItemViewDTO;

        return dbResult;
    }
}
