using CustardRM.Models.Entities;
namespace CustardRM.Models.DTOs.Supplier;

public class SupplierView
{
    public Entities.Supplier Supplier { get; set; }
    public float AvgLeadTime { get; set; }
    public float LeadTimeVariance { get; set; }
    public List<Entities.Purchases.PurchaseOrder> PurchaseOrders { get; set; }

}
