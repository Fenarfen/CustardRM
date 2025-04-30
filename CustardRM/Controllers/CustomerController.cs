using CustardRM.Attributes;
using CustardRM.Interfaces;
using CustardRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustardRM.Controllers;

[ApiController]
[Route("api/customer")]
[TokenAuthorize]
public class CustomerController : Controller
{
	private readonly IDatabaseService _databaseService;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ITokenService _tokenService;

	public CustomerController(IDatabaseService databaseService, IPasswordHasher passwordHasher, ITokenService tokenService)
	{
		_databaseService = databaseService;
		_passwordHasher = passwordHasher;
		_tokenService = tokenService;
	}

	[HttpGet("get-customers")]
	public IActionResult GetAllCustomers()
	{
		try
		{
			var result = _databaseService.GetAllCustomers();

			Console.WriteLine(result.Message);

			if (result.Success)
			{
				return Ok(result.Result);
			}
			else
			{
				return StatusCode(500, new { message = "[CustomerController] An error occurred while processing your request. Please try again later.\n" });
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[CustomerController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}

	[HttpGet("get-customer-view/id={id}")]
	public IActionResult GetCustomerViewByID(int id)
	{
		try
		{
			var result = _databaseService.GetCustomerViewByID(id);

			Console.WriteLine(result.Message);

			if (result.Success)
			{
				return Ok(result.Result);
			}
			else
			{
				return StatusCode(500, new { message = "[CustomerController] An error occurred while processing your request. Please try again later.\n" });
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "[CustomerController] An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
		}
	}
}
