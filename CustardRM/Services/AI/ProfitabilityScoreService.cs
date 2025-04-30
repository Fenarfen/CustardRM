using CustardRM.Interfaces;
using CustardRM.Models.DTOs.AI;

namespace CustardRM.Services.AI;

public class ProfitabilityScoreService : IProfitabilityScoreService
{
	private readonly IDatabaseService _databaseService;
	public ProfitabilityScoreService(IDatabaseService databaseService)
	{
		_databaseService = databaseService;
	}

	public ProfitabilityScoreResult PredictProfitabilityScore(int stockItemID)
	{
		var result = _databaseService.GetProfitabilityScoreMetricsByStockItemID(stockItemID);

		if (!result.Success)
		{
			Console.WriteLine(result.Message);
			return null;
		}

		var metrics = (ProfitabilityScoreMetrics)result.Result;

		ProfitabilityScoreModel.ModelInput sampleData = metrics.ConvertToModelInput();

		return new ProfitabilityScoreResult()
		{
			StockItemID = stockItemID,
			Score = ProfitabilityScoreModel.Predict(sampleData).Score,
			Justification = metrics.Justification
		};
	}
}
