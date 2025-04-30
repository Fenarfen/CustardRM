namespace CustardRM.Interfaces
{
    public interface IReorderPredictionService
    {
        float PredictReorderQuantityForStockItemAndSupplier(int stockitemID, int supplierID);
        DateTime PredictReorderTimingForStockItemAndSupplier(int stockitemID, int supplierID);
    }
}