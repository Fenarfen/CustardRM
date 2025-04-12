using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;
using CustardRM.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.RateLimiting;
using CustardRM.Interfaces;
using CustardRM.Models.Entities;
using Azure.Core;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Net.WebSockets;

namespace CustardRM.Controllers;

[ApiController]
[Route("api/user")]
public class SystemUserController : Controller
{
    private readonly IDatabaseService _databaseService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _TokenService;

    public SystemUserController(IDatabaseService databaseService, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _databaseService = databaseService;
        _passwordHasher = passwordHasher;
        _TokenService = tokenService;
    }

    [HttpPost("attempt-sign-in")]
    [EnableRateLimiting("fixed")]
    public IActionResult SignIn([FromBody] Models.Requests.LoginRequest request)
    {
        try
        {
            Console.WriteLine("Login request received at " + DateTime.UtcNow);

            if (request == null ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Missing required fields");
            }

            var result = _databaseService.VerifyLoginDetails(request);
            Console.WriteLine(result.Message);

            if (result.Success)
            {
                var token = _TokenService.CreateTokenOnLogin((int)result.Result);
                return Ok(new { token });
            }
            else
            {
                return Unauthorized(new { message = "Incorrect email or password, please try again." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpPost("create-account")]
    public IActionResult CreateAccount([FromBody] Models.Requests.CreateUserRequest request)
    {
        try
        {
            if (request == null ||
                string.IsNullOrEmpty(request.FirstName) ||
                string.IsNullOrEmpty(request.LastName) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Required data is missing" });
            }

            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            var result = _databaseService.CreateUser(request, hash, salt);

            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }

    [HttpGet("does-email-exist/{email}")]
    public IActionResult DoesEmailExistEndpoint(string email)
    {
        try
        {
            var result = _databaseService.DoesEmailExist(email);

            Console.WriteLine(result.Message);

            if (result.Success == true)
            {
                return Ok(new { message = (bool)result.Result });
            }
            else
            {
                return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request. Please try again later.\n" + ex.ToString() });
        }
    }
}
