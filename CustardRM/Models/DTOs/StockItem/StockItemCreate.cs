using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs.StockItem;

public class StockItemCreate
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } = 0;
    public decimal CostPrice { get; set; } = 0;
    public int StockLevel { get; set; } = 0;
    public string MetaData { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
