namespace CustardRM.Models.DTOs.AI;

public class SuggestedReorderModel
{
    public float AvgDailyConsumption { get; set; }
    public float StockLevel { get; set; }
    public float AvgLeadTime { get; set; }
    public float LeadTimeVariance { get; set; }
    public float DaysSinceLastOrder { get; set; }

    public static implicit operator SuggestedReorderTimingModel.ModelInput(SuggestedReorderModel model)
    {
        return new SuggestedReorderTimingModel.ModelInput
        {
            AvgDailyConsumption = model.AvgDailyConsumption,
            StockLevel = model.StockLevel,
            AvgLeadTime = model.AvgLeadTime,
            LeadTimeVariance = model.LeadTimeVariance,
            DaysSinceLastOrder = model.DaysSinceLastOrder
        };
    }

    public static implicit operator SuggestedReorderQuantityModel.ModelInput(SuggestedReorderModel model)
    {
        return new SuggestedReorderQuantityModel.ModelInput
        {
            AvgDailyConsumption = model.AvgDailyConsumption,
            StockLevel = model.StockLevel,
            AvgLeadTime = model.AvgLeadTime,
            LeadTimeVariance = model.LeadTimeVariance,
            DaysSinceLastOrder = model.DaysSinceLastOrder
        };
    }
}
