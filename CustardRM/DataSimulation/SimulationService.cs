using CustardRM.Interfaces;
using CustardRM.Models.Entities;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Models.Entities.Purchases;
using CustardRM.Models.Entities.Sales;
using CustardRM.Services;

namespace CustardRM.DataServices;

public class SimulationService
{
	private readonly IDatabaseService _databaseService;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ITokenService _tokenService;

	public SimulationService(IDatabaseService databaseService)
	{
		_databaseService = databaseService;
	}

	public int GetCountOfSoldItemOfStockItem(StockItem item)
	{
		int count = 0;

		var result = _databaseService.GetAllSalesOrderLinesOfStockItem(item);

		if (!result.Success)
		{
			Console.WriteLine($"[DataSimulation] result of GetAllSalesOrderLinesOfStockItem was false for {item.ID}");
			Console.WriteLine($"[DataSimulation] {result.Message}");
		}

		foreach (var line in (List<SalesOrderLine>)result.Result)
		{
			count += line.Quantity;
		}

		return count;
	}

	public int GetCountOfPurchasedItemsOfStockItem(StockItem item)
	{
		int count = 0;

		var result = _databaseService.GetAllPurchaseOrderLinesOfStockItem(item);

		if (!result.Success)
		{
			Console.WriteLine($"[DataSimulation] result of GetAllPurchaseOrderLinesOfStockItem was false for {item.ID}");
			Console.WriteLine($"[DataSimulation] {result.Message}");
		}

		foreach (var line in (List<PurchaseOrderLine>)result.Result)
		{
			count += line.QuantityOrdered;
		}

		return count;
	}
}
