using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Interfaces
{
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> SendGet(string url, string? token);
        Task<HttpResponseMessage> SendPost(string url, object data, string? token);
    }
}
