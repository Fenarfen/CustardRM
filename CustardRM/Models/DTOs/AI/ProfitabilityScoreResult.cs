namespace CustardRM.Models.DTOs.AI;

public class ProfitabilityScoreResult
{
    public int StockItemID { get; set; }
    public float Score { get; set; }
    public List<string> Justification { get; set; }
}
