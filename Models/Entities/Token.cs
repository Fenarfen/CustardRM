using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.Entities;

public class Token
{
    public int Id { get; set; }
    public int UserID { get; set; }
    public required string Value { get; set; }
    public DateTime ExpiresAt { get; set; }
}
