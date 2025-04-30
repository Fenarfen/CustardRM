namespace CustardRM.Models.DTOs.StockItem;
using CustardRM.Models.Entities.Inventory;

public class StockItemTableRow
{
	public StockItem StockItem { get; set; }
	public float ProfitabilityScore { get; set; }
}
