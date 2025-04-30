using System.Data;
using System.Collections.Generic;
using Moq;
using Moq.Dapper;
using Moq.Language.Flow;
using Dapper;

namespace CustardRM_Testing.Services.UnitTests.DatabaseServiceTests;

public static class MoqDapperExtensions
{
    public static ISetup<IDbConnection, IEnumerable<TResult>> SetupAnyQuery<TResult>(
        this Mock<IDbConnection> mock)
    {
        return mock.SetupDapper(c => c.Query<TResult>(
            It.IsAny<string>(),              // sql
            It.IsAny<object>(),              // param
            It.IsAny<IDbTransaction>(),      // transaction (null is allowed at run-time)
            It.IsAny<bool>(),                // buffered
            It.IsAny<int?>(),                // commandTimeout
            It.IsAny<CommandType?>()));      // commandType
    }
}
