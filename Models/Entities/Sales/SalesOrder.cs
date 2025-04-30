using CustardRM.Models.Entities.Purchases;

namespace CustardRM.Models.Entities.Sales;

public class SalesOrder
{
	public int ID { get; set; }
	public DateTime OrderDate { get; set; }
	public string CustomerRef { get; set; }
	public decimal TotalAmount { get; set; }

	public List<SalesOrderLine> SalesOrderLines { get; set; } = new();
}