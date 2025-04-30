using CustardRM.Models.DTOs.AI;

namespace CustardRM.Interfaces
{
    public interface IProfitabilityScoreService
    {
        ProfitabilityScoreResult PredictProfitabilityScore(int stockItemID);
    }
}