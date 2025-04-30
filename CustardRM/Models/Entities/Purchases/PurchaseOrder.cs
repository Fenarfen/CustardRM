namespace CustardRM.Models.Entities.Purchases;

public class PurchaseOrder
{
	public int ID { get; set; }
	public int SupplierID { get; set; }
	public DateTime OrderDate { get; set; }
	public DateTime ExpectedDeliveryDate { get; set; }
	public DateTime ActualDeliveryDate { get; set; }
	public string Status { get; set; }

	public List<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new();
}
