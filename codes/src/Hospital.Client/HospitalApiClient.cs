using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hospital.Application.Common.Models;

namespace Hospital.Client;

public sealed class HospitalApiException(HttpStatusCode status, string message, string? traceId = null,
    IReadOnlyList<string>? errors = null) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
    public string? TraceId { get; } = traceId;
    public IReadOnlyList<string> Errors { get; } = errors ?? [];
}

public sealed class HospitalApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null,
        string? token = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body, options: Json);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json, cancellationToken);
            if (!response.IsSuccessStatusCode || payload?.Success != true)
                throw new HospitalApiException(response.StatusCode, payload?.Message ?? "Chưa thể xử lý yêu cầu. Vui lòng thử lại.",
                    payload?.TraceId, payload?.Errors);
            return payload.Data!;
        }
        catch (HttpRequestException)
        { throw new HospitalApiException(HttpStatusCode.ServiceUnavailable, "Chưa thể kết nối hệ thống. Vui lòng thử lại sau."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new HospitalApiException(HttpStatusCode.GatewayTimeout, "Kết nối mất nhiều thời gian. Vui lòng thử lại."); }
        catch (JsonException)
        { throw new HospitalApiException(HttpStatusCode.BadGateway, "Chưa thể đọc phản hồi từ hệ thống. Vui lòng thử lại."); }
    }
}
