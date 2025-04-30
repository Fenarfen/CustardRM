namespace CustardRM.Models.Entities.Sales;

public class SalesOrderLine
{
	public int ID { get; set; }
	public int SalesOrderID { get; set; }
	public int StockItemID { get; set; }
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal Discount { get; set; }
	public decimal DiscountRate { get; set; }
}