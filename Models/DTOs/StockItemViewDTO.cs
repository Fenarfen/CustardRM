using CustardRM.Models.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs;

public class StockItemViewDTO
{
    public StockItem StockItem { get; set; } = new();
    public List<Category> LinkedCategories { get; set; } = new();
    public List<SupplierLinkDTO> LinkedSuppliers { get; set; } = new();
}
