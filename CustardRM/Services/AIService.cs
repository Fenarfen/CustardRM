using CustardRM.Interfaces;
using CustardRM.Models.DTOs.AI;
using CustardRM.Models.Entities;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Models.Entities.Purchases;
using CustardRM.Models.Entities.Sales;
using CustardRM.Services.AI;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.ML;

namespace CustardRM.Services;

public class AIService : IAIService
{
	private readonly IDatabaseService _databaseService;
	private readonly IReorderPredictionService _reorder;
	private readonly IProfitabilityScoreService _profit;

	public AIService(IDatabaseService databaseService,
					 IReorderPredictionService reorder,
					 IProfitabilityScoreService profit)
	{
		_databaseService = databaseService;
		_reorder = reorder;
		_profit = profit;
	}

	public ReorderSuggestion PredictReorderQuantityAndTiming(int stockItemID, int supplierID)
	{
		float reorderQuantity = _reorder.PredictReorderQuantityForStockItemAndSupplier(stockItemID, supplierID);
		DateTime reorderTiming = _reorder.PredictReorderTimingForStockItemAndSupplier(stockItemID, supplierID);
		
		ReorderSuggestion reorderSuggestion = new()
		{
			StockItemID = stockItemID,
			SupplierID = supplierID,
			SuggestedQuantity = reorderQuantity,
			SuggestedTiming = reorderTiming,
			GeneratedAt = DateTime.UtcNow
		};

		return reorderSuggestion;
	}

	public SentimentAnalysisService.ReviewAnalysisResult PredictSentiment(Review review)
	{
		return SentimentAnalysisService.AnalyseReviewText(review.ReviewText);
	}

	public ProfitabilityScoreResult PredictProfitabilityScore(int stockItemID, bool store = false)
	{
		var result = _profit.PredictProfitabilityScore(stockItemID);

		if(result == null)
		{
			return null;
		}

		if (store)
		{
			_databaseService.StoreProfitabilityScore(stockItemID, result);
		}

		return _profit.PredictProfitabilityScore(stockItemID);
	}
}
