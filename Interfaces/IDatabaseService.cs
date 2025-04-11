using CustardRM.Models.DTOs;
using CustardRM.Models.Requests;
using CustardRM.Services;

namespace CustardRM.Interfaces
{
    public interface IDatabaseService
    {
        DatabaseService.DatabaseResult CreateCategory(CategoryDTO category);
        DatabaseService.DatabaseResult CreateStockItem(StockItemDTO stockItemDTO);
        DatabaseService.DatabaseResult CreateStockItemCategoryLink(int stockItemID, int categoryID);
        DatabaseService.DatabaseResult CreateStockItemSupplierLink(SupplierLinkDTO sLinkDTO);
        DatabaseService.DatabaseResult CreateUser(CreateUserRequest req, string PasswordHash, string PasswordSalt);
        DatabaseService.DatabaseResult DeleteStockItemCategoryLink(int stockItemID, int categoryID);
        DatabaseService.DatabaseResult DeleteStockItemSupplierLink(int stockItemID, int supplierID);
        DatabaseService.DatabaseResult DoesEmailExist(string email);
        DatabaseService.DatabaseResult EditCategory(CategoryDTO category);
        DatabaseService.DatabaseResult EditStockItem(StockItemViewDTO stockItemDTO);
        DatabaseService.DatabaseResult GetAllStockItems();
        DatabaseService.DatabaseResult GetCategories();
        DatabaseService.DatabaseResult GetCategoryByID(int id);
        DatabaseService.DatabaseResult GetCategoryByName(string name);
        DatabaseService.DatabaseResult GetLinkedCategoriesByStockItemID(int id);
        DatabaseService.DatabaseResult GetLinkedSuppliersByStockItemID(int id);
        DatabaseService.DatabaseResult GetStockItemByCode(string code, List<int>? excludedIDs = null);
        DatabaseService.DatabaseResult GetStockItemByID(int id);
        DatabaseService.DatabaseResult GetStockItemByName(string name, List<int>? excludedIDs = null);
        DatabaseService.DatabaseResult GetStockItemDataByID(int stockItemID);
        DatabaseService.DatabaseResult GetSubcategoriesByCategoryID(int CategoryID);
        DatabaseService.DatabaseResult GetSupplierByID(int id);
        DatabaseService.DatabaseResult StoreToken(int userID, string token, DateTime expiresAt);
        DatabaseService.DatabaseResult ValidateToken(string token, int refreshMinutes);
        DatabaseService.DatabaseResult VerifyLoginDetails(LoginRequest req);
    }
}