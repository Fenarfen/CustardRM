using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Interfaces;

public interface IPasswordHasher
{
    public (string hash, string salt) HashPassword(string password);
    public bool VerifyPassword(string password, string storedHash, string storedSalt);
}
