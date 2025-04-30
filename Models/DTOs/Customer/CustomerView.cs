using CustardRM.Models.Entities.Inventory;
using CustardRM.Models.Entities.Sales;

namespace CustardRM.Models.DTOs.Customer;

public class CustomerView
{
	public Entities.Customer Customer { get; set; }
	public List<Review> Reviews { get; set; }
	public List<SalesOrder> Orders { get; set; }
	public List<SalesReturn> Returns { get; set; }
}
