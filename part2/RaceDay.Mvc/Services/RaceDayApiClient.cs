using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient(IHttpClientFactory factory, IHttpContextAccessor context)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient("RaceDayApi");

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var cookie = context.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = Request(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UploadAsync(string path, IFormFile file)
    {
        using var request = Request(HttpMethod.Post, path);
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "file", file.FileName);
        request.Content = form;
        return await Client.SendAsync(request);
    }

    public async Task<T?> ReadAsync<T>(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path);
        return await ReadAsync<T>(response);
    }

    public static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public static string? SessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;
        return values.FirstOrDefault(value =>
            value.StartsWith("RaceDay.Session=", StringComparison.OrdinalIgnoreCase))?.Split(';')[0];
    }

    public static async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized) return "Please sign in to continue.";
        if (response.StatusCode == HttpStatusCode.Forbidden) return "You do not have permission for this action.";
        if (string.IsNullOrWhiteSpace(body)) return $"The API returned {(int)response.StatusCode}.";
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("detail", out var detail)) return detail.GetString() ?? body;
            if (json.RootElement.TryGetProperty("title", out var title)) return title.GetString() ?? body;
        }
        catch (JsonException) { }
        return body;
    }
}
