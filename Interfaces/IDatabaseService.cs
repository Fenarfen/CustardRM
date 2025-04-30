using CustardRM.Models.DTOs;
using CustardRM.Models.DTOs.AI;
using CustardRM.Models.DTOs.StockItem;
using CustardRM.Models.Entities;
using CustardRM.Models.Entities.Inventory;
using CustardRM.Models.Entities.Purchases;
using CustardRM.Models.Requests;
using CustardRM.Services.AI;

namespace CustardRM.Services
{
	public interface IDatabaseService
	{
		DatabaseService.DatabaseResult CreateCategory(CategoryCreateEdit category);
		DatabaseService.DatabaseResult CreatePurchaseOrder(PurchaseOrder po);
		DatabaseService.DatabaseResult CreateReview(StockItemReviewCreateEdit review);
		DatabaseService.DatabaseResult CreateStockItem(StockItemCreate stockItemDTO);
		DatabaseService.DatabaseResult CreateStockItemSupplierLink(SupplierLink sLinkDTO);
		DatabaseService.DatabaseResult CreateUser(CreateUserRequest req, string PasswordHash, string PasswordSalt);
		DatabaseService.DatabaseResult DeleteStockItemSupplierLink(int stockItemID, int supplierID);
		DatabaseService.DatabaseResult DoesEmailExist(string email);
		void ExecuteSentimentAnalysisOnAllReviews();
		string ExtractTokenFromAuthHeaderString(string authHeader);
		DatabaseService.DatabaseResult GetAllCustomers();
		DatabaseService.DatabaseResult GetAllLinkedStockItemsAndSuppliers();
		DatabaseService.DatabaseResult GetAllPurchaseOrderLinesOfStockItem(StockItem item);
		DatabaseService.DatabaseResult GetAllPurchaseOrders();
		DatabaseService.DatabaseResult GetAllPurchaseOrdersForStockItemAndSupplier(int stockItemID, int supplierID);
		DatabaseService.DatabaseResult GetAllSalesOrderLinesOfStockItem(StockItem item);
		DatabaseService.DatabaseResult GetAllSalesOrders();
		DatabaseService.DatabaseResult GetAllStockItems();
		DatabaseService.DatabaseResult GetAllSuppliers();
		float GetAverageLeadTimeForSupplier(int supplierID, List<PurchaseOrder> orders);
		DatabaseService.DatabaseResult GetAverageLeadTimeforSupplierForStockItem(int stockItemID, int supplierID);
		DatabaseService.DatabaseResult GetCategoryByName(string name);
		DatabaseService.DatabaseResult GetCustomerViewByID(int id);
		float GetLeadTimeVarianceForSupplier(int supplierID, List<PurchaseOrder> orders);
		DatabaseService.DatabaseResult GetLinkedSuppliersByStockItemID(int id);
		DatabaseService.DatabaseResult GetMostRecentPurchaseOrderForStockItemID(int id);
		DatabaseService.DatabaseResult GetProfitabilityScoreMetricsByStockItemID(int id);
		Supplier? GetRandomActiveSupplier();
		DatabaseService.DatabaseResult GetStockItemByCode(string code, List<int>? excludedIDs = null);
		DatabaseService.DatabaseResult GetStockItemByID(int id);
		DatabaseService.DatabaseResult GetStockItemByName(string name, List<int>? excludedIDs = null);
		DatabaseService.DatabaseResult GetStockItemOrderDatesAndQuantities(int id);
		DatabaseService.DatabaseResult GetStockItemViewByID(int id);
		DatabaseService.DatabaseResult GetSupplierByID(int id);
		DatabaseService.DatabaseResult GetSupplierView(int supplierID);
		void StoreProfitabilityScore(int stockItemID, ProfitabilityScoreResult result);
		DatabaseService.DatabaseResult StoreReorderSuggestion(ReorderSuggestion suggestion);
		void StoreReviewAnalysisResult(int reviewID, int stockItemID, SentimentAnalysisService.ReviewAnalysisResult analysisResult);
		DatabaseService.DatabaseResult StoreToken(int userID, string token, DateTime expiresAt);
		DatabaseService.DatabaseResult UpdateStockItem(StockItem stockItem);
		DatabaseService.DatabaseResult UpdateStockLevelOfStockItem(int itemID, int quantity);
		DatabaseService.DatabaseResult ValidateToken(string token, int refreshMinutes);
		DatabaseService.DatabaseResult VerifyLoginDetails(LoginRequest req);
	}
}