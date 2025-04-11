using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs;

public class StockItemDTO
{
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int StockLevel { get; set; }
    public bool IsActive { get; set; }
    public List<int> LinkedCategories { get; set; } = new();
    public List<int> LinkedSuppliers { get; set; } = new();
}
