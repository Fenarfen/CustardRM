using CustardRM.Interfaces;
using CustardRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CustardRM.Components;
using CustardRM.DataServices;
using static CustardRM.DataServices.SimulationService;
using CustardRM.Services.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<HttpClientService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUriStr = config.GetValue<string>("BaseURI")
        ?? throw new Exception("Cannot find BaseURI in appsettings.json");
    if (!Uri.TryCreate(baseUriStr, UriKind.Absolute, out var baseUri))
    {
        throw new Exception("The provided BaseURI must be an absolute URI.");
    }

    client.BaseAddress = baseUri;
});

builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IReorderPredictionService, ReorderPredictionService>();
builder.Services.AddScoped<IProfitabilityScoreService, ProfitabilityScoreService>();

builder.Services.AddRateLimiter(rateLimitOptions =>
{
    rateLimitOptions.AddFixedWindowLimiter("fixed", options =>
     {
         options.PermitLimit = 2;
         options.Window = TimeSpan.FromSeconds(1);
         options.QueueLimit = 0;
     });
    rateLimitOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
    

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAntiforgery();

app.Run();