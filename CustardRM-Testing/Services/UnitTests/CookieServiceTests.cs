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
