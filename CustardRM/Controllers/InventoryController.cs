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
using CustardRM.Services;
using CustardRM.Models.DTOs.StockItem;

namespace CustardRM.Controllers;

[ApiController]
[Route("api/inventory")]
[TokenAuthorize]
public class InventoryController : Controller
{
    private readonly IDatabaseService _databaseService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public InventoryController(IDatabaseService databaseService, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _databaseService = databaseService;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpGet("get-stock-items")]
    public IActionResult GetAllStockItems()
    {
        try
        {
            var result = _databaseService.GetAllStockItems();

            Console.WriteLine(result.Message);

            if (result.Success)
            {
                return Ok(result.Result);
            }
            else
            {
                return StatusCode(500, new { message = $"[InventoryController] An error occurred while processing your request. Please try again later.\n{result.Message}" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpGet("get-stock-item/id={stockItemID}")]
    public IActionResult GetStockItemData(int stockItemID)
    {
        try
        {
            var result = _databaseService.GetStockItemViewByID(stockItemID);

            Console.WriteLine(result.Message);

            if (result.Success)
            {
                return Ok(result.Result);
            }
            else
            {
                return StatusCode(500, new { message = $"[InventoryController] An error occurred while processing your request. Please try again later.\n{result.Message}" });
            }
        }
        catch (Exception ex)
        {
			return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

    [HttpPost("create-stock-item")]
    public IActionResult CreateStockItem([FromBody] StockItemCreate stockItem)
    {
        try
        {
            var result = _databaseService.CreateStockItem(stockItem);

            Console.WriteLine(result.Message);

            if (result.Success)
            {
                return Ok();
            }
            else
            {
                return StatusCode(500, new { message = $"[InventoryController] An error occurred while processing your request. Please try again later.\n{result.Message}" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

	[HttpPost("update-stock-item")]
	public IActionResult UpdateStockItem([FromBody] StockItem stockItem)
	{
		try
		{
			var result = _databaseService.UpdateStockItem(stockItem);

			Console.WriteLine(result.Message);

			if (result.Success)
			{
				return Ok();
			}
			else
			{
                return StatusCode(500, new { message = $"[InventoryController] An error occurred while processing your request. Please try again later.\n{result.Message}" });
            }
        }
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpGet("analyse-all-reviews")]
	public IActionResult AnalyseAllReviews()
    {
		try
		{
			Task.Run(() =>
			{
				try
				{
                    _databaseService.ExecuteSentimentAnalysisOnAllReviews();
				}
				catch (Exception e)
				{
					Console.WriteLine($"[AnalyseAllReviews Task error] {e}");
				}
			});

			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpPost("create-stock-item-review")]
	public IActionResult CreateStockItemReview([FromBody] StockItemReviewCreateEdit stockItemReview)
    {
        try
        {
            if(stockItemReview == null)
            {
                return BadRequest("Expected StockItemReviewCreateEdit in body of request");
            }

            var result = _databaseService.CreateReview(stockItemReview);

			Task.Run(() =>
			{
				try
				{
					ReviewAnalysisResult reviewAnalysisResult = AnalyseReviewText(stockItemReview.ReviewText);
					_databaseService.StoreReviewAnalysisResult((int)result.Result, stockItemReview.StockItemID, reviewAnalysisResult);
				}
				catch (Exception e)
				{
					// Optional: log the error, avoid crashing background thread silently
					Console.WriteLine($"[Background Task Error] {e}");
				}
			});

			return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }
}
