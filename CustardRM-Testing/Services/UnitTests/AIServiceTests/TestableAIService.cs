using CustardRM.Interfaces;
using CustardRM.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM_Testing.Services.UnitTests.AIServiceTests;

public class TestableAIService
{
    public readonly Mock<IDatabaseService> _db = new(MockBehavior.Strict);
    public readonly Mock<IReorderPredictionService> _re = new(MockBehavior.Strict);
    public readonly Mock<IProfitabilityScoreService> _prof = new(MockBehavior.Strict);

    public AIService Build() => new AIService(_db.Object, _re.Object, _prof.Object);
}
