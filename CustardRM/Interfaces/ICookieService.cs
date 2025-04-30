using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Interfaces;

public interface ICookieService
{
    void SetCookie(string key, string value);
    string? GetCookie(string key);
    void DeleteCookie(string key);
}
