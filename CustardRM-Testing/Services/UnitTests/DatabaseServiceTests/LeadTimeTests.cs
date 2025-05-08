using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CustardRM.Models.Entities.Purchases;

namespace CustardRM_Testing.Services.UnitTests.DatabaseServiceTests;

class LeadTimeTests
{
    private static PurchaseOrder PO(DateTime order, DateTime delivery) =>
       new() { OrderDate = order, ActualDeliveryDate = delivery };

    private static readonly DateTime Today = DateTime.Today;

    [Test]
    public void Average_returns_zero_when_no_orders()
    {
        var (sut, _) = TestableDbService.BuildSut();
        var avg = sut.GetAverageLeadTimeForSupplier(1, new List<PurchaseOrder>());
        Assert.That(avg, Is.EqualTo(0f));
    }

    [Test]
    public void Average_ignores_orders_with_missing_dates()
    {
        var (sut, _) = TestableDbService.BuildSut();
        var orders = new List<PurchaseOrder>
        {
            PO(Today.AddDays(-8), Today),   // valid: 8 days
            PO(default, default)            // invalid row
        };

        var avg = sut.GetAverageLeadTimeForSupplier(2, orders);
        Assert.That(avg, Is.EqualTo(8f));
    }

    [Test]
    public void Average_multiple_orders_returns_correct_value()
    {
        var (sut, _) = TestableDbService.BuildSut();
        var orders = new List<PurchaseOrder>
        {
            PO(Today.AddDays(-10), Today),
            PO(Today.AddDays(-5),  Today),
            PO(Today.AddDays(-15), Today)
        };

        Assert.That(
            sut.GetAverageLeadTimeForSupplier(3, orders),
            Is.EqualTo(10f).Within(1e-3));
    }

    [Test]
    public void Variance_returns_zero_when_no_valid_dates()
    {
        var (sut, _) = TestableDbService.BuildSut();
        var orders = new List<PurchaseOrder> { PO(default, default) };

        Assert.That(sut.GetLeadTimeVarianceForSupplier(1, orders), Is.EqualTo(0f));
    }

    [Test]
    public void Variance_single_order_is_zero()
    {
        var (sut, _) = TestableDbService.BuildSut();
        var orders = new List<PurchaseOrder>
        {
            PO(Today.AddDays(-7), Today)
        };

        Assert.That(sut.GetLeadTimeVarianceForSupplier(1, orders), Is.EqualTo(0f));
    }

    [Test]
    public void Variance_multiple_orders_returns_correct_std_dev()
    {
        var (sut, _) = TestableDbService.BuildSut();
        // lead times 10 and 4  ->  average = 7, variance = 9, st-dev = 3
        var orders = new List<PurchaseOrder>
        {
            PO(Today.AddDays(-10), Today),  // 10
            PO(Today.AddDays(-4),  Today)   // 4
        };

        Assert.That(
            sut.GetLeadTimeVarianceForSupplier(1, orders),
            Is.EqualTo(3f).Within(1e-3));
    }
}
