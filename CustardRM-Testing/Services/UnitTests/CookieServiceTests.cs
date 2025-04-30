using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using CustardRM.Services;
using CustardRM.Interfaces;

namespace CustardRM_Testing.Services.UnitTests;

[TestFixture]
public class CookieServiceTests
{
    private DefaultHttpContext _context = null!;
    private ICookieService _service = null!;

    [SetUp]
    public void Setup()
    {
        _context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = _context };
        _service = new CookieService(accessor);
    }

    [Test]
    public void SetCookie_Appends_Strict_SameSite_And_OneDayExpiry()
    {
        _service.SetCookie("foo", "bar");

        Assert.IsTrue(_context.Response.Headers.ContainsKey("Set-Cookie"), "No Set-Cookie header");
        var header = _context.Response.Headers["Set-Cookie"].ToString();

        StringAssert.StartsWith("foo=bar;", header);
        StringAssert.Contains("SameSite=Strict", header);

        // Cookies expire in one day's time
        var expiresPart = header
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(expiresPart, "Expires= missing");
        var dateStr = expiresPart!["Expires=".Length..];
        DateTime dt = DateTime.Parse(dateStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var diff = dt - DateTime.UtcNow;
        Assert.That(diff.TotalHours, Is.InRange(23.5, 24.5),
            "Expires should be ~24h from now");
    }

    [Test]
    public void GetCookie_Returns_Value_When_Present()
    {
        _context.Request.Headers["Cookie"] = "foo=bar; baz=qux";

        var value = _service.GetCookie("foo");

        Assert.AreEqual("bar", value);
    }

    [Test]
    public void GetCookie_Returns_Null_When_Missing()
    {
        var value = _service.GetCookie("does_not_exist");
        Assert.IsNull(value);
    }
}
