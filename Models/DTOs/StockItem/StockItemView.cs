using CustardRM.Models.Entities.AI;
using CustardRM.Models.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs.StockItem;

public class StockItemView
{
    public Entities.Inventory.StockItem StockItem { get; set; }
    public ProfitabilityScorePrediction ProfitabilityScorePrediction { get; set; }
    public List<Review> Reviews { get; set; }
    public Dictionary<string, int> TopBigrams { get; set; }
    public string OverallAISentiment { get; set; }

    public StockItemView(Entities.Inventory.StockItem stockItem, ProfitabilityScorePrediction profitabilityScorePrediction, List<Review> reviews, Dictionary<string, int> topBigrams)
    {
        StockItem = stockItem;
        ProfitabilityScorePrediction = profitabilityScorePrediction;
        Reviews = reviews;
        TopBigrams = topBigrams;

        int countPositiveReviewAISentiment = 0;
        int countNegativeReviewAISentiment = 0;

        foreach (var item in reviews)
        {
            if (item.PredictedAISentiment == "positive")
            {
                countPositiveReviewAISentiment++;
            }
            else if (item.PredictedAISentiment == "negative")
            {
                countNegativeReviewAISentiment++;
            }
        }

        if (countPositiveReviewAISentiment + countNegativeReviewAISentiment == 0)
        {
            OverallAISentiment = "No reviews - AI sentiment cannot be calculated";
        }
        else if (countPositiveReviewAISentiment == countNegativeReviewAISentiment)
        {
            OverallAISentiment = "Equal sentiment";
        }
        else if (countPositiveReviewAISentiment > countNegativeReviewAISentiment)
        {
            OverallAISentiment = "Positive";
        }
        else
        {
            OverallAISentiment = "Negative";
        }
    }
}
