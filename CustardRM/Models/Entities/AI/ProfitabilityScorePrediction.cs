namespace CustardRM.Models.Entities.AI;

public class ProfitabilityScorePrediction
{
	public int ID { get; set; }
	public int StockItemID { get; set; }
	public float Score { get; set; }
	public string Justification { get; set; }
	public DateTime GeneratedAt { get; set; }
}
