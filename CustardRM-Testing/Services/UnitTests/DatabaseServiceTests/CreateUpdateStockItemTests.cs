using CustardRM.Models.DTOs.StockItem;
using CustardRM.Models.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM_Testing.Services.UnitTests.DatabaseServiceTests;

public class CreateUpdateStockItemTests
{
    private static StockItemCreate StockItemCreate(Action<StockItemCreate>? mutator = null)
    {
        var s = new StockItemCreate
        {
            ItemCode = "ABC",
            ItemName = "Widget",
            Description = "Test item",
            UnitPrice = 10m,
            CostPrice = 6m,
            StockLevel = 20,
            IsActive = true,
            MetaData = "{}"
        };
        mutator?.Invoke(s);
        return s;
    }

    private static StockItem StockItem(Action<StockItem>? mutator = null)
    {
        var s = new StockItem
        {
            ID = 1,
            ItemCode = "ABC",
            ItemName = "Widget",
            Description = "Test item",
            UnitPrice = 10m,
            CostPrice = 6m,
            StockLevel = 20,
            IsActive = true,
            MetaData = "{}"
        };
        mutator?.Invoke(s);
        return s;
    }

    [TestCase(-10, 6, 5, "Unit price cannot go below 0")]
    [TestCase(10, -6, 5, "Cost price cannot go below 0")]
    [TestCase(10, 6, -5, "Stock Level cannot go below 0")]
    public void Negative_values_on_create_throw(decimal unit, decimal cost, int stock, string msgPart)
    {
        var (sut, _) = TestableDbService.BuildSut();

        var item = StockItemCreate(i =>
        {
            i.UnitPrice = unit;
            i.CostPrice = cost;
            i.StockLevel = stock;
        });

        var ex = Assert.Throws<Exception>(() =>
            sut.CreateStockItem(item));

        StringAssert.Contains(msgPart, ex?.Message);
    }

    [TestCase(-10, 6, 5, "Unit price cannot go below 0")]
    [TestCase(10, -6, 5, "Cost price cannot go below 0")]
    [TestCase(10, 6, -5, "Stock Level cannot go below 0")]
    public void Negative_values_on_update_throw(decimal unit, decimal cost, int stock, string msgPart)
    {
        var (sut, _) = TestableDbService.BuildSut();

        var item = StockItem(i =>
        {
            i.UnitPrice = unit;
            i.CostPrice = cost;
            i.StockLevel = stock;
        });

        var ex = Assert.Throws<Exception>(() =>
            sut.UpdateStockItem(item));

        StringAssert.Contains(msgPart, ex?.Message);
    }
}
