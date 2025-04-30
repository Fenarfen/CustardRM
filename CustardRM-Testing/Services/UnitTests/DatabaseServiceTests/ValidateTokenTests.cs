using CustardRM.Services;
using Dapper;
using Moq;
using Moq.Dapper;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM_Testing.Services.UnitTests.DatabaseServiceTests;

[TestFixture]
public class ValidateTokenTests
{
    [Test]
    public void Valid_extracted_token_is_64_characters()
    {
        var (sut, _) = TestableDbService.BuildSut();

        string value = new string('A', 64);
        string header = $@"Bearer {{""token"":""{value}""}}";

        var result = sut.ExtractTokenFromAuthHeaderString(header);

        Assert.That(result, Is.EqualTo(value));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Null_or_empty_header_throws(string? header)
    {
        var (sut, _) = TestableDbService.BuildSut();

        var ex = Assert.Throws<Exception>(() => sut.ValidateToken(header!, 5));
        Assert.That(ex?.Message, Is.EqualTo("Token is required"));
    }

    [TestCase("Bearer 123")]                    // missing
    [TestCase("bearer {\"token\":\"xyz\"}")]    // wrong case
    [TestCase("FooBar")]                        // no prefix
    public void Header_without_expected_prefix_throws(string header)
    {
        var (sut, _) = TestableDbService.BuildSut();

        var ex = Assert.Throws<Exception>(() => sut.ExtractTokenFromAuthHeaderString(header));
        StringAssert.StartsWith("AuthHeader not in expected format", ex?.Message);
    }

    [Test]
    public void Header_shorter_than_17_plus_64_chars_throws()
    {
        var (sut, _) = TestableDbService.BuildSut();
        string shortHeader = "Bearer {\"token\":\"SHORT\"}";

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.ValidateToken(shortHeader, 5));
    }
}
