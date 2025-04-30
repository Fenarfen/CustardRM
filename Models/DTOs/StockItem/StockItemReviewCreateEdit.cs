using CustardRM.Services.AI;
using System.ComponentModel.DataAnnotations;
using static CustardRM.Services.AI.SentimentAnalysisService;

namespace CustardRM.Models.DTOs.StockItem;

public class StockItemReviewCreateEdit
{
    public int CustomerID { get; set; }
    public int StockItemID { get; set; }
    public int Rating { get; set; }
    public string ReviewText { get; set; }
}
