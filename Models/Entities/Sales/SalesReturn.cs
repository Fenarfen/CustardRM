namespace CustardRM.Models.Entities.Sales;

public class SalesReturn
{
	public int ID { get; set; }
	public int SalesOrderLineID { get; set; }

	public int QuantityReturned { get; set; }
	public DateTime ReturnDate { get; set; }
	public string Reason { get; set; }
}