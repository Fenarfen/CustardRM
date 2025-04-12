using CustardRM.Attributes;
using CustardRM.Interfaces;
using CustardRM.Models.Entities;
using CustardRM.Models.DTOs;
using CustardRM.Models.Entities.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpGet("get-stock-item/{stockItemID}")]
    public IActionResult GetStockItemData(int stockItemID)
    {
        try
        {
            var result = _databaseService.GetStockItemDataByID(stockItemID);

            Console.WriteLine(result.Message);

            if (result.Success)
            {
                return Ok(result.Result);
            }
            else
            {
                return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("create-stock-item")]
    public IActionResult CreateStockItem([FromBody] StockItemDTO stockItem)
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
                return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "[InventoryController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("create-category")]
    public IActionResult CreateCategory([FromBody] CategoryDTO category)
    {
        try
        {
            var result = _databaseService.CreateCategory(category);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("edit-category")]
    public IActionResult EditCategory([FromBody] CategoryDTO category)
    {
        try
        {
            var result = _databaseService.EditCategory(category);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("link-stock-item-to-category/{stockItemID}-{categoryID}")]
    public IActionResult LinkStockItemToCategory(int stockItemID, int categoryID)
    {
        try
        {
            var result = _databaseService.CreateStockItemCategoryLink(stockItemID, categoryID);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpGet("get-category/{id}")]
    public IActionResult GetCategoryByID(int id)
    {
        try
        {
            var result = _databaseService.GetCategoryByID(id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("link-stock-item-to-supplier/{stockItemID}-{supplierID}")]
    public IActionResult LinkStockItemToSupplier([FromBody] SupplierLinkDTO sLinkDTO)
    {
        try
        {
            var result = _databaseService.CreateStockItemSupplierLink(sLinkDTO);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }
}
