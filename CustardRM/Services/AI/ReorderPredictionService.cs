using CustardRM.Components;
using CustardRM.Interfaces;
using CustardRM.Models.Entities;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Models.Entities.Purchases;
using CustardRM.Models.Entities.Sales;
using Microsoft.EntityFrameworkCore.Storage;

namespace CustardRM.Services.AI;

public class ReorderPredictionService : IReorderPredictionService
{
	private readonly IDatabaseService _databaseService;
	public ReorderPredictionService(IDatabaseService databaseService)
	{
		_databaseService = databaseService;
	}

	public float PredictReorderQuantityForStockItemAndSupplier(int stockitemID, int supplierID)
	{
		var resultStockItem = _databaseService.GetStockItemByID(stockitemID);

		if (!resultStockItem.Success)
		{
			throw new Exception(resultStockItem.Message);
		}

		StockItem stockItem = (StockItem)resultStockItem.Result;

		var resultSupplier = _databaseService.GetSupplierByID(supplierID);

		if (!resultSupplier.Success)
		{
			throw new Exception(resultSupplier.Message);
		}

		Supplier supplier = (Supplier)resultSupplier.Result;

		float avgDailyConsumption = CalculateAverageDailyConsumption(stockItem.ID);
		float stockLevel = Convert.ToSingle(stockItem.StockLevel);
		float AvgLeadTime = GetAverageLeadTimeforSupplier(stockItem.ID, supplier.ID);
		float leadTimeVariance = GetLeadTimeVariance(stockItem.ID, supplier.ID);
		float daysSinceLastOrder = Convert.ToSingle(GetDaysSinceLastOrder(stockItem.ID));

		var quantityData = new SuggestedReorderQuantityModel.ModelInput()
		{
			AvgDailyConsumption = avgDailyConsumption,
			StockLevel = stockLevel,
			AvgLeadTime = AvgLeadTime,
			LeadTimeVariance = leadTimeVariance,
			DaysSinceLastOrder = daysSinceLastOrder,
		};

		var quantityResult = SuggestedReorderQuantityModel.Predict(quantityData);
		Console.WriteLine($"[ReorderPredictionService] Quantity prediction result: {quantityResult.Score}");

		var suggestedReorderQuantity = quantityResult.Score;

		return suggestedReorderQuantity;
	}

	public DateTime PredictReorderTimingForStockItemAndSupplier(int stockitemID, int supplierID)
	{
		var resultStockItem = _databaseService.GetStockItemByID(stockitemID);

		if (!resultStockItem.Success)
		{
			throw new Exception(resultStockItem.Message);
		}

		StockItem stockItem = (StockItem)resultStockItem.Result;

		var resultSupplier = _databaseService.GetSupplierByID(supplierID);

		if (!resultSupplier.Success)
		{
			throw new Exception(resultSupplier.Message);
		}

		Supplier supplier = (Supplier)resultSupplier.Result;

		float avgDailyConsumption = CalculateAverageDailyConsumption(stockItem.ID);
		float stockLevel = Convert.ToSingle(stockItem.StockLevel);
		float AvgLeadTime = GetAverageLeadTimeforSupplier(stockItem.ID, supplier.ID);
		float leadTimeVariance = GetLeadTimeVariance(stockItem.ID, supplier.ID);
		float daysSinceLastOrder = Convert.ToSingle(GetDaysSinceLastOrder(stockItem.ID));

		var timingData = new SuggestedReorderTimingModel.ModelInput()
		{
			DaysSinceLastOrder = daysSinceLastOrder,
			AvgDailyConsumption = avgDailyConsumption,
			StockLevel = stockLevel,
			AvgLeadTime = AvgLeadTime,
			LeadTimeVariance = leadTimeVariance,
		};

		var result = SuggestedReorderTimingModel.Predict(timingData);
		var suggestedTimingDays = result.Score;

		DateTime suggestedDateTime = DateTime.Now.AddDays(suggestedTimingDays);

		Console.WriteLine($"[ReorderPredictionService] Timing prediction result: {suggestedTimingDays} {suggestedDateTime}");

		return suggestedDateTime;
	}

	private int GetDaysSinceLastOrder(int id)
	{
		var lastOrderDate = _databaseService.GetMostRecentPurchaseOrderForStockItemID(id);

		if (!lastOrderDate.Success)
		{
			throw new Exception(lastOrderDate.Message);
		}

		return (DateTime.UtcNow - (DateTime)lastOrderDate.Result).Days;
	}

	private float GetAverageLeadTimeforSupplier(int stockItemID, int supplierID)
	{
		var averageLeadTime = _databaseService.GetAverageLeadTimeforSupplierForStockItem(stockItemID, supplierID);

		if (!averageLeadTime.Success)
		{
			throw new Exception(averageLeadTime.Message);
		}

		return Convert.ToSingle(averageLeadTime.Result);
	}

	private float GetLeadTimeVariance(int stockItemID, int supplierID)
	{
		var orders = _databaseService.GetAllPurchaseOrdersForStockItemAndSupplier(stockItemID, supplierID);

		List<(DateTime, DateTime, DateTime)> values = (List<(DateTime, DateTime, DateTime)>)orders.Result;

		var leadTimes = values
			.Where(v => v.Item1 != null && v.Item2 != null && v.Item3 != null)  // Ensure that order, expected, and actual date values exist
			.Select(v => (v.Item3 - v.Item1).TotalDays)                         // Get the difference between actual delivery date and promised delivery date
			.ToList();

		if (leadTimes.Count() == 0)
			return 0;

		if (leadTimes.Sum() == 0) // Avoid divide by 0 error
		{
			return 0;
		}

		double avg = leadTimes.Average(); // Get the average of all the leadTimes
		double variance = leadTimes.Average(l => Math.Pow(l - avg, 2));

		return Convert.ToSingle(Math.Sqrt(variance)); // return standard deviation
	}

	private float CalculateAverageDailyConsumption(int id)
	{
		var orders = _databaseService.GetStockItemOrderDatesAndQuantities(id);

		if (!orders.Success)
		{
			return 0;
		}

		List<(DateTime, int)> value = (List<(DateTime, int)>)orders.Result;

		if (value.Count() == 0) return 0;

		DateTime minDate = value.Min(v => v.Item1);
		int countOrdered = value.Sum(v => v.Item2);

		int totalDays = 0;

		totalDays = (int)(DateTime.UtcNow - minDate).TotalDays;

		if (totalDays == 0)
		{
			return 0;
		}

		return countOrdered / totalDays;
	}
}
