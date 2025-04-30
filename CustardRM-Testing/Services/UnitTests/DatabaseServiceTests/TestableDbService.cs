using CustardRM.Services;
using Dapper;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Dapper;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CustardRM.Services.DatabaseService;

namespace CustardRM_Testing.Services.UnitTests.DatabaseServiceTests;

public class TestableDbService : DatabaseService
{
    private readonly IDbConnection _conn;

    public TestableDbService(IDbConnection conn, IConfiguration config) : base(config)
    {
        _conn = conn;
    }

    public static (TestableDbService sut, Mock<IDbConnection> dbMock) BuildSut()
    {
        var dbMock = new Mock<IDbConnection>();

        var cfgValues = new Dictionary<string, string?>
    {
        { "ConnectionStrings:DefaultConnection",
          "Server=(local);Database=UnitTest;Trusted_Connection=True;" }
    };

        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(cfgValues)
            .Build();

        var sut = new TestableDbService(dbMock.Object, cfg);
        return (sut, dbMock);
    }


    protected override IDbConnection CreateConnection() => _conn;

    protected override DatabaseResult RefreshToken(int id, int minutes) =>
        new DatabaseResult { Success = true, Message = "[stub] refreshed" };
}
