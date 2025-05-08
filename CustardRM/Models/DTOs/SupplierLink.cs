using CustardRM.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs;

public class SupplierLink
{
    public Entities.Supplier Supplier { get; set; }
    public int StockItemID { get; set; }
    public int SupplierID { get; set; }
    public int LeadTimeDays { get; set; }
    public int AverageLeadTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
