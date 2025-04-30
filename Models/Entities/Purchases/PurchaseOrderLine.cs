namespace CustardRM.Models.Entities.Purchases;

public class PurchaseOrderLine
{
	public int ID { get; set; }
	public int PurchaseOrderID { get; set; }
	public int StockItemID { get; set; }
	public int QuantityOrdered { get; set; }
	public int QuantityReceived { get; set; }
	public decimal UnitCost { get; set; }
}
