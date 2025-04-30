using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CustardRM.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace CustardRM.Services;

public class TokenService : ITokenService
{
    private readonly IDatabaseService _databaseService;
    private readonly int _expiresMinutes;
    private readonly int _refreshMinutes;
    private readonly int _tokenLength;

    public TokenService(IConfiguration config, IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _expiresMinutes = int.Parse(config["Token:ExpiresMinutes"] ?? "120");
        _refreshMinutes = int.Parse(config["Token:ResfreshMinutes"] ?? "30");
        _tokenLength = int.Parse(config["Token:TokenLength"] ?? "64");
    }

    private string GenerateToken()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return RandomNumberGenerator.GetString(chars, _tokenLength);
    }

    public bool ValidateToken(string token)
    {
        var result = _databaseService.ValidateToken(token, _refreshMinutes);

        Console.WriteLine(result.Message);

        return result.Success;
    }

    public string CreateTokenOnLogin(int userID)
    {
        string token = GenerateToken();
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_expiresMinutes);

        _databaseService.StoreToken(userID, token, expiresAt);

        return token;
    }
}
