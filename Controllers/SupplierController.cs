using CustardRM.Attributes;
using CustardRM.Interfaces;
using CustardRM.Services;
using CustardRM.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CustardRM.Controllers;

[ApiController]
[Route("api/supplier")]
[TokenAuthorize]
public class SupplierController : Controller
{
	private readonly IDatabaseService _databaseService;
	private readonly ITokenService _tokenService;

	public SupplierController(IDatabaseService databaseService, IPasswordHasher passwordHasher, ITokenService tokenService)
	{
		_databaseService = databaseService;
		_tokenService = tokenService;
	}

	[HttpGet("get-suppliers")]
	public IActionResult GetAllSuppliers()
	{
		try
		{
			var result = _databaseService.GetAllSuppliers();

			Console.WriteLine(result.Message);

			if (result.Success)
			{
				return Ok(result.Result);
			}
			else
			{
				return StatusCode(500, new { message = "[SupplierController] An error occurred while processing your request. Please try again later.\n" });
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[SupplierController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpGet("get-supplier-view/id={id}")]
	public IActionResult GetSupplierView(int id)
	{
		try
		{
			var result = _databaseService.GetSupplierView(id);

			Console.WriteLine(result.Message);

			if (result.Success)
			{
				return Ok(result.Result);
			}
			else
			{
				return StatusCode(500, new { message = "[SupplierController] An error occurred while processing your request. Please try again later.\n" });
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[SupplierController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}
}
