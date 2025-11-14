using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;

namespace StreamKey.Core.Services;

public class CamoufoxService(HttpClient client, ILogger<CamoufoxService> logger) : ICamoufoxService
{
    public async Task<string?> GetPageHtml(CamoufoxRequest request)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/fetch-html");
            requestMessage.Content = JsonContent.Create(request);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        
            var httpResponse = await client.SendAsync(requestMessage);
            httpResponse.EnsureSuccessStatusCode();
        
            var response = await httpResponse.Content.ReadAsStringAsync()
                           ?? throw new InvalidOperationException("Пустой ответ Camoufox");

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка получения HTML для URL: {Url}", request.Url);
            return null;
        }
    }

    public async Task<byte[]?> GetPageScreenshot(CamoufoxRequest request)
    {
        try
        {
            var httpResponse = await client.PostAsJsonAsync("/fetch-screenshot", request);
            
            httpResponse.EnsureSuccessStatusCode();
            
            var bytes = await httpResponse.Content.ReadAsByteArrayAsync();
            
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения скриншота для URL: {Url}", request.Url);
            return null;
        }
    }
}