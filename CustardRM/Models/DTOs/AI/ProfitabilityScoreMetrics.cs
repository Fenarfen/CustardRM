using System.Linq;

namespace CustardRM.Models.DTOs.AI;

public class ProfitabilityScoreMetrics
{
    public int ID { get; set; }
    public float ProfitMargin { get; set; }
    public float SalesVelocity { get; set; }
    public float AvgReviewRating { get; set; }
    public float DiscountRate { get; set; }
    public float StockTurnoverRate { get; set; }
    public float HoldingCostEstimate { get; set; }
    public List<string> Justification
    {
        get
        {
            var justifications = new List<string>();

            if (ProfitMargin > 0.4f)
                justifications.Add("High profit margin indicates good pricing strategy.");
            else if (ProfitMargin < 0.1f)
                justifications.Add("Low profit margin may be reducing profitability.");

            if (SalesVelocity > 0.5f)
                justifications.Add("Fast sales velocity shows strong demand.");
            else if (SalesVelocity < 0.1f)
                justifications.Add("Slow sales velocity indicates poor product turnover.");

            if (AvgReviewRating >= 4.0f)
                justifications.Add("Positive customer reviews suggest high satisfaction.");
            else if (AvgReviewRating <= 2.5f)
                justifications.Add("Low customer satisfaction may be impacting sales.");

            if (DiscountRate > 0.2f)
                justifications.Add("Frequent discounting may reduce profitability.");
            else if (DiscountRate < 0.05f)
                justifications.Add("Low discounting maintains margin strength.");

            if (StockTurnoverRate > 5f)
                justifications.Add("Efficient stock turnover reduces holding costs.");
            else if (StockTurnoverRate < 1f)
                justifications.Add("Slow stock turnover leads to high inventory costs.");

            if (HoldingCostEstimate > 1000f)
                justifications.Add("High holding costs may hurt profitability.");

            return justifications;
        }
    }

    public ProfitabilityScoreModel.ModelInput ConvertToModelInput()
    {
        return new ProfitabilityScoreModel.ModelInput()
        {
            ProfitMargin = ProfitMargin,
            SalesVelocity = SalesVelocity,
            AvgReviewRating = AvgReviewRating,
            DiscountRate = DiscountRate,
            StockTurnoverRate = StockTurnoverRate,
            HoldingCostEstimate = HoldingCostEstimate,
        };
    }

    public List<string> GetJustification()
    {
        var justifications = new List<string>();

        if (ProfitMargin > 0.4f)
            justifications.Add("High profit margin indicates good pricing strategy.");
        else if (ProfitMargin < 0.1f)
            justifications.Add("Low profit margin may be reducing profitability.");

        if (SalesVelocity > 0.5f)
            justifications.Add("Fast sales velocity shows strong demand.");
        else if (SalesVelocity < 0.1f)
            justifications.Add("Slow sales velocity indicates poor product turnover.");

        if (AvgReviewRating >= 4.0f)
            justifications.Add("Positive customer reviews suggest high satisfaction.");
        else if (AvgReviewRating <= 2.5f)
            justifications.Add("Low customer satisfaction may be impacting sales.");

        if (DiscountRate > 0.2f)
            justifications.Add("Frequent discounting may reduce profitability.");
        else if (DiscountRate < 0.05f)
            justifications.Add("Low discounting maintains margin strength.");

        if (StockTurnoverRate > 5f)
            justifications.Add("Efficient stock turnover reduces holding costs.");
        else if (StockTurnoverRate < 1f)
            justifications.Add("Slow stock turnover leads to high inventory costs.");

        if (HoldingCostEstimate > 1000f)
            justifications.Add("High holding costs may hurt profitability.");

        return justifications;
    }
}
