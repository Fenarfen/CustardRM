using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM_Testing.Services.UnitTests.AIServiceTests;

public class ReorderModelsTests : TestableAIService
{
    [Test]
    public void PredictReorder_combines_quantity_and_timing_into_DTO()
    {
        _re.Setup(r => r.PredictReorderQuantityForStockItemAndSupplier(10, 5))
            .Returns(42f);
        var tomorrow = DateTime.UtcNow.AddDays(1);
        _re.Setup(r => r.PredictReorderTimingForStockItemAndSupplier(10, 5))
            .Returns(tomorrow);

        var svc = Build();
        var dto = svc.PredictReorderQuantityAndTiming(10, 5);

        Assert.That(dto.StockItemID, Is.EqualTo(10));
        Assert.That(dto.SupplierID, Is.EqualTo(5));
        Assert.That(dto.SuggestedQuantity, Is.EqualTo(42f));
        Assert.That(dto.SuggestedTiming, Is.EqualTo(tomorrow));
        Assert.That(dto.GeneratedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(10)));
    }
}
