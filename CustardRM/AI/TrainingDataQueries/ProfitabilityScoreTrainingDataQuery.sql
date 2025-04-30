-- Step 1: Gather metrics
WITH Metrics AS (
    SELECT 
    -- Profit Margin
    (SI.UnitPrice - SI.CostPrice) / NULLIF(SI.UnitPrice, 0) AS ProfitMargin,
    -- Sales Velocity
    CAST(SUM(SOL.Quantity) AS FLOAT) / NULLIF(DATEDIFF(DAY, SI.CreatedAt, GETDATE()), 0) AS SalesVelocity,
    -- Avg Review Rating
	-- NOTE: Fallback value of 3 should result in the absence of real data minimally effecting the result
    ISNULL(AVG(R.ReviewRating), 3.0) AS AvgReviewRating,
    -- Avg Discount Rate
	ISNULL(AVG(SOL.DiscountRate), 0.0) AS DiscountRate,
	-- Stock Turnover Rate
	-- NOTE: Value is capped at 100 to avoid extreme variance as low StockLevel values can overinflate the result
	LEAST(
		100,
		(SUM(SOL.Quantity) * SI.CostPrice) / 
		CASE 
			WHEN SI.StockLevel * SI.CostPrice = 0 THEN 1
			ELSE SI.StockLevel * SI.CostPrice 
		END
	) AS StockTurnoverRate,
    -- Holding Cost Estimate
	-- NOTE: Holding cost data has not been simulated, as such, we make an assumption that the holding cost is proportinal to the cost price of the item.
    SI.StockLevel * SI.CostPrice * 0.01 AS HoldingCostEstimate,
    -- Target (Profitability Score): NULL for now, this will be filled during training labeling
    NULL AS ProfitabilityScore

	FROM StockItem SI
	JOIN SalesOrderLine SOL ON SOL.StockItemID = SI.ID
	JOIN SalesReturn SR ON SR.SalesOrderLineID = SOL.ID
	JOIN StockItemReview R ON R.StockItemID = SI.ID

	GROUP BY SI.ID, SI.UnitPrice, SI.CostPrice, SI.StockLevel, SI.CreatedAt
),
MinMax AS (
    SELECT
        MIN(ProfitMargin) AS MinProfitMargin, MAX(ProfitMargin) AS MaxProfitMargin,
        MIN(SalesVelocity) AS MinSalesVelocity, MAX(SalesVelocity) AS MaxSalesVelocity,
        MIN(AvgReviewRating) AS MinAvgReviewRating, MAX(AvgReviewRating) AS MaxAvgReviewRating,
        MIN(DiscountRate) AS MinDiscountRate, MAX(DiscountRate) AS MaxDiscountRate,
        MIN(StockTurnoverRate) AS MinTurnoverRate, MAX(StockTurnoverRate) AS MaxTurnoverRate,
        MIN(HoldingCostEstimate) AS MinHoldingCost, MAX(HoldingCostEstimate) AS MaxHoldingCost
    FROM Metrics
)

-- Step 2: Normalize, Weight, and Compute Final Score
-- NOTE: Any change to the weighting of each metric will result in the model needing to be retrained or the score of predicted items will be misaligned
-- this could be implemented into a full scale system where changes to the weights triggers retraining of the model, but in this proof of concept we will hard code them
SELECT 
	M.ProfitMargin,
	M.SalesVelocity,
	M.AvgReviewRating,
	M.DiscountRate,
	M.StockTurnoverRate,
	M.HoldingCostEstimate,
    -- Normalized & weighted values
    CAST(ROUND((
        -- Profit Margin (positive)
        ((M.ProfitMargin - MM.MinProfitMargin) / NULLIF(MM.MaxProfitMargin - MM.MinProfitMargin, 0)) * 0.25 +

        -- Sales Velocity (positive)
        ((M.SalesVelocity - MM.MinSalesVelocity) / NULLIF(MM.MaxSalesVelocity - MM.MinSalesVelocity, 0)) * 0.20 +

        -- Avg Review Rating (positive, normalized 1–5)
        ((M.AvgReviewRating - 1) / 4.0) * 0.10 +

        -- Discount Rate (negative)
        (1 - M.DiscountRate) * 0.10 +

        -- Stock Turnover Rate (positive)
        ((M.StockTurnoverRate - MM.MinTurnoverRate) / NULLIF(MM.MaxTurnoverRate - MM.MinTurnoverRate, 0)) * 0.15 +

        -- Holding Cost Estimate (negative)
        (1 - ((M.HoldingCostEstimate - MM.MinHoldingCost) / NULLIF(MM.MaxHoldingCost - MM.MinHoldingCost, 0))) * 0.05
    ) * 100, 2) AS FLOAT) AS ProfitabilityScore

FROM Metrics M
CROSS JOIN MinMax MM
ORDER BY ProfitabilityScore DESC;
