using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustardRM.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CustardRM.Services;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void SetCookie(string key, string value)
    {
        var options = new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(1)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    public string? GetCookie(string key)
    {
        string? value = null;
        _httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(key, out value);
        return value;
    }

    public void DeleteCookie(string key)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key);
    }
}
