using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Domain.Tasks;

namespace PaxScan.Infrastructure.Web;

/// <summary>
/// Typed client for <c>/api/v1/hub/*</c> (see contracts/openapi.yaml). Hand-written because the
/// surface is two endpoints and the contract is OpenAPI 3.1, which NSwag does not fully support.
/// </summary>
internal sealed class WebApiClient(HttpClient http) : IWebApiClient
{
    public async Task<WebTaskSnapshot> GetTaskSnapshotAsync(WebConnection connection, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, connection, "api/v1/hub/tasks");
        var data = await SendAsync<SnapshotDto>(request, cancellationToken);
        return new WebTaskSnapshot(
            data.DatasetVersion,
            data.Checksum,
            data.Tasks.Select(t => new WebTaskFields(t.Id, t.Name, t.Type, t.Description, t.Status, t.Version, t.CreatedAt, t.UpdatedAt)).ToList());
    }

    public async Task<IReadOnlyList<WebItemResult>> PushTaskUpdatesAsync(
        WebConnection connection,
        Guid hubId,
        IReadOnlyList<WebStatusUpdate> items,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, connection, "api/v1/hub/task-updates");
        request.Content = JsonContent.Create(new PushDto(hubId, items), options: WireJson.Options);
        var data = await SendAsync<PushResultDto>(request, cancellationToken);
        return data.Results;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, WebConnection connection, string path)
    {
        var baseUrl = connection.BaseUrl.AbsoluteUri.EndsWith('/') ? connection.BaseUrl : new Uri(connection.BaseUrl.AbsoluteUri + "/");
        var request = new HttpRequestMessage(method, new Uri(baseUrl, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new WebApiException($"Cannot reach Online: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WebApiException("Online did not answer in time.", ex);
        }

        using (response)
        {
            Envelope<T>? envelope;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(WireJson.Options, cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new WebApiException($"Online returned an unreadable response (HTTP {(int)response.StatusCode}).", ex) { StatusCode = (int)response.StatusCode };
            }

            if (!response.IsSuccessStatusCode || envelope is not { Success: true, Data: not null })
            {
                throw new WebApiException(envelope?.Message ?? $"Online returned HTTP {(int)response.StatusCode}.") { StatusCode = (int)response.StatusCode };
            }

            return envelope.Data;
        }
    }

    private sealed record Envelope<T>(bool Success, string? Message, T? Data);

    private sealed record SnapshotDto(string? DatasetVersion, string Checksum, IReadOnlyList<TaskDto> Tasks);

    private sealed record TaskDto(
        Guid Id,
        string Name,
        TaskItemType Type,
        string? Description,
        TaskItemStatus Status,
        int Version,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record PushDto(Guid HubId, IReadOnlyList<WebStatusUpdate> Items);

    private sealed record PushResultDto(IReadOnlyList<WebItemResult> Results);
}
