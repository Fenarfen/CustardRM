using CustardRM.Models.DTOs.AI;
using CustardRM.Models.Entities;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Services.AI;

namespace CustardRM.Interfaces
{
    public interface IAIService
    {
        ReorderSuggestion PredictReorderQuantityAndTiming(int stockItemID, int supplierID);
		SentimentAnalysisService.ReviewAnalysisResult PredictSentiment(Review review);
		ProfitabilityScoreResult PredictProfitabilityScore(int stockItemID, bool store = false);
	}
}