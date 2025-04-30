namespace CustardRM.Models.Entities;

public class Customer
{
	public int ID { get; set; }
	public string UserID { get; set; }
	public string ProfileName { get; set; }
	public string Email { get; set; }
	public DateTime CreatedAt { get; set; }
}
