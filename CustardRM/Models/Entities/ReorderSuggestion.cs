namespace CustardRM.Models.Entities;

public class ReorderSuggestion
{
	public int ID { get; set; }
	public int StockItemID { get; set; }
	public int SupplierID { get; set; }
	public float SuggestedQuantity { get; set; }
	public DateTime SuggestedTiming { get; set; }
	public DateTime GeneratedAt { get; set; }
}
