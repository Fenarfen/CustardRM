using CustardRM.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace CustardRM.Attributes
{
    public class TokenAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                context.Result = new BadRequestObjectResult("Auth header is missing");
                return;
            }

            var tokenService = context.HttpContext.RequestServices.GetService(typeof(ITokenService)) as ITokenService;
            if (tokenService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var isTokenValid = tokenService.ValidateToken(authHeader);
            if (!isTokenValid)
            {

                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}
