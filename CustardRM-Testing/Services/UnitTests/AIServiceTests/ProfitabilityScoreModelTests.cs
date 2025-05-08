using CustardRM.Models.DTOs.AI;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CustardRM.Services.DatabaseService;

namespace CustardRM_Testing.Services.UnitTests.AIServiceTests;

public class ProfitabilityScoreModelTests : TestableAIService
{
    [Test]
    public void Profitability_returns_null_when_model_returns_null()
    {
        _prof.Setup(p => p.PredictProfitabilityScore(99)).Returns((ProfitabilityScoreResult)null);
        var svc = Build();

        Assert.IsNull(svc.PredictProfitabilityScore(99));
        _db.VerifyNoOtherCalls();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Profitability_returns_and_optionally_stores(bool store)
    {
        var score = new ProfitabilityScoreResult { Score = 0.8f };
        _prof.Setup(p => p.PredictProfitabilityScore(1)).Returns(score);

        if (store)
        {
            _db.Setup(d => d.StoreProfitabilityScore(1, score)).Verifiable();
        }

        var svc = Build();
        var value = svc.PredictProfitabilityScore(1, store);

        Assert.That(value, Is.EqualTo(score));

        if (store)
            _db.Verify(d => d.StoreProfitabilityScore(1, score), Times.Once);
        else
            _db.Verify(d => d.StoreProfitabilityScore(It.IsAny<int>(), It.IsAny<ProfitabilityScoreResult>()),
                       Times.Never);
    }
}
