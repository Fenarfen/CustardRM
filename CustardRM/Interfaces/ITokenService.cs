using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Interfaces;

public interface ITokenService
{
    public string CreateTokenOnLogin(int userID);
    public bool ValidateToken(string token);
}
