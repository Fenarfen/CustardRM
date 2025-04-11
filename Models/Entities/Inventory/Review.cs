using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.Entities.Inventory;

public class Review
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public int StockItemID { get; set; }
    public string Body { get; set; }
    public float Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
