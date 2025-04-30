using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.Entities.Inventory;

public class Subcategory
{
    public int ID { get; set; }
    public int CategoryID { get; set; }
    public string SubcategoryName { get; set; }
    public string SubcategoryDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
