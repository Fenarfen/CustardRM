using Azure;
using CustardRM.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CustardRM.Services;

public class HttpClientService : IHttpClientService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    public HttpClient Http => _httpClient;

    public HttpClientService(IConfiguration config, HttpClient client)
    {
        _httpClient = client;
        _config = config;
    }

    public async Task<HttpResponseMessage> SendGet(string url, string? token = null)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        Console.WriteLine($"Sending GET request to {_httpClient.BaseAddress}{url}");
        var result = await _httpClient.GetAsync(url);
        Console.WriteLine($"Response status code: {result.StatusCode.ToString()}");
        Console.WriteLine($"Response message: {await result.Content.ReadAsStringAsync()}");
        return result;
    }

    public async Task<HttpResponseMessage> SendPost(string url, object data, string? token = null)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        Console.WriteLine($"Sending POST request to {_httpClient.BaseAddress}{url}");
        string json = JsonSerializer.Serialize(data);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await _httpClient.PostAsync(url, content);
        Console.WriteLine($"Response status code: {result.StatusCode.ToString()}");
        Console.WriteLine($"Response message: {await result.Content.ReadAsStringAsync()}");
        return result;
    }

    public class RequestAuthorisationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
