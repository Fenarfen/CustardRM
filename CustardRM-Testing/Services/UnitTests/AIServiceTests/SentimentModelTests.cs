using CustardRM.Models.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM_Testing.Services.UnitTests.AIServiceTests;

public class SentimentModelTests : TestableAIService
{
    [Test]
    public void PredictSentiment_returns_analyser_result()
    {
        // no mocks needed – just a deterministic string
        var review = new Review { ReviewText = "I love it!" };
        var result = Build().PredictSentiment(review);

        List<string> acceptableResults = new() { "positive", "negative" };
        Assert.That(result.Sentiment, Is.AnyOf("positive", "negative"));
    }
}
