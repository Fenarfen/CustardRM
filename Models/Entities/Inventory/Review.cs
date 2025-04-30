using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.Entities.Inventory;

public class Review
{
    public int ID { get; set; }
    public string UserID { get; set; }
    public int StockItemID { get; set; }
    public string ReviewHelpfulness { get; set; }
    public string ReviewText { get; set; }
    public float ReviewRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PredictedAISentiment { get; set; }
}
