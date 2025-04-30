using CustardRM.Attributes;
using CustardRM.Interfaces;
using CustardRM.Models.Entities;
using CustardRM.Models.DTOs;
using CustardRM.Models.Entities.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CustardRM.Services.AI;
using static CustardRM.Services.AI.SentimentAnalysisService;
using CustardRM.DataServices;
using System.Runtime.CompilerServices;
using CustardRM.Models.Entities.Purchases;
using CustardRM.Models.Entities.Sales;
using System.Linq;
using System.Collections.Generic;
using CustardRM.Models.DTOs.StockItem;
using CustardRM.Services;

namespace CustardRM.Controllers;

[ApiController]
[Route("api/ai")]
[TokenAuthorize]
public class AIController : Controller
{
	private readonly IDatabaseService _databaseService;
	private readonly ITokenService _tokenService;
	private readonly IAIService _aiService;

	public AIController(IDatabaseService databaseService, ITokenService tokenService, IAIService aIService)
	{
		_databaseService = databaseService;
		_tokenService = tokenService;
		_aiService = aIService;
	}

	[HttpGet("create-timing-training-data")]
	public IActionResult CreateTimingTrainingData()
	{
		try
		{
			Task.Run(() => 
			{
				List<(int,int)> links = (List<(int, int)>)_databaseService.GetAllLinkedStockItemsAndSuppliers().Result;
				List<ReorderSuggestion> suggestions = new List<ReorderSuggestion>();

				Console.WriteLine($"[AIController] Processing {links.Count} links");
				int count = 0;

				foreach (var link in links)
				{
					var suggestion = _aiService.PredictReorderQuantityAndTiming(link.Item1, link.Item2);
					Console.WriteLine($"[AIController] SuggestedQuantity: {suggestion.SuggestedQuantity} SuggestedTiming: {suggestion.SuggestedTiming}");
					_databaseService.StoreReorderSuggestion(suggestion);
					count++;
					Console.WriteLine($"[AIController] {count}/{links.Count} processed");
				}

				Console.WriteLine("[AIController] Finished creating and storing Timing training data");
			});

			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpPost("profitability-score/predict/{stockItemID}/{shouldStore}")]
	public IActionResult PredictSingleProfitabilityScore(int stockItemID, bool shouldStore)
	{
		try
		{
			var result = _aiService.PredictProfitabilityScore(stockItemID, shouldStore);

			Console.WriteLine($"[AIController] Profitability score prediction for id: {stockItemID} score: {result}");

			return Ok(result);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[AIController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpGet("profitability-score/predict-all")]
	public IActionResult PredictAllProfitabilityScores()
	{
		try
		{
			Task.Run(() =>
			{
				List<StockItemTableRow> rows = (List<StockItemTableRow>)_databaseService.GetAllStockItems().Result;

				foreach (var row in rows)
				{
					var item = row.StockItem;

                    Console.WriteLine($"[AIController] Predicting profitability score for id: {item.ID}");

                    var result = _aiService.PredictProfitabilityScore(item.ID, true);

					if(result != null)
					{
						Console.WriteLine($"[AIController] Profitability score prediction for id: {item.ID} score: {result}");
					}
					else
					{
						Console.WriteLine($"[AIController] Could not predict profitability score for {item.ID}");
					}
				}
			});

			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[AIController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}
}
