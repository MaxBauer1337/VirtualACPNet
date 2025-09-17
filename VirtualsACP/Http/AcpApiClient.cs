using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VirtualsAcp.Exceptions;
using VirtualsAcp.Models;

namespace VirtualsAcp.Http;

public class AcpApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly string _baseUrl;

    public AcpApiClient(string baseUrl, ILogger? logger = null)
    {
        _baseUrl = baseUrl;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<List<IACPAgent>> BrowseAgentsAsync(
        string keyword,
        string? cluster = null,
        List<AcpAgentSort>? sortBy = null,
        int? topK = null,
        AcpGraduationStatus? graduationStatus = null,
        AcpOnlineStatus? onlineStatus = null,
        string? walletAddressToExclude = null)
    {
        try
        {
            var url = $"{_baseUrl}/agents/v2/search?search={Uri.EscapeDataString(keyword)}";
            
            if (sortBy != null && sortBy.Any())
            {
                var sortValues = string.Join(",", sortBy.Select(s => s.ToString()));
                url += $"&sortBy={Uri.EscapeDataString(sortValues)}";
            }

            if (topK.HasValue)
            {
                url += $"&top_k={topK.Value}";
            }

            if (!string.IsNullOrEmpty(walletAddressToExclude))
            {
                url += $"&walletAddressesToExclude={Uri.EscapeDataString(walletAddressToExclude)}";
            }

            if (!string.IsNullOrEmpty(cluster))
            {
                url += $"&cluster={Uri.EscapeDataString(cluster)}";
            }

            if (graduationStatus.HasValue)
            {
                url += $"&graduationStatus={graduationStatus.Value.ToString().ToLowerInvariant()}";
            }

            if (onlineStatus.HasValue)
            {
                url += $"&onlineStatus={onlineStatus.Value.ToString().ToLowerInvariant()}";
            }

            _logger?.LogDebug("Making request to: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<IACPAgent>>>(content);

            return result?.Data ?? new List<IACPAgent>();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while browsing agents");
            throw new AcpApiError("Failed to browse agents", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while browsing agents");
            throw new AcpError("An unexpected error occurred while browsing agents", ex);
        }
    }

    public async Task<List<ACPJob>> GetActiveJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("active", walletAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCompletedJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("completed", walletAddress, page, pageSize);
    }

    public async Task<List<ACPJob>> GetCancelledJobsAsync(string walletAddress, int page = 1, int pageSize = 10)
    {
        return await GetJobsAsync("cancelled", walletAddress, page, pageSize);
    }

    private async Task<List<ACPJob>> GetJobsAsync(string jobType, string walletAddress, int page, int pageSize)
    {
        try
        {
            var url = $"{_baseUrl}/jobs/{jobType}?pagination[page]={page}&pagination[pageSize]={pageSize}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("wallet-address", walletAddress);

            _logger?.LogDebug("Making request to: {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<ACPJob>>>(content);

            return result?.Data ?? new List<ACPJob>();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while getting {JobType} jobs", jobType);
            throw new AcpApiError($"Failed to get {jobType} jobs", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while getting {JobType} jobs", jobType);
            throw new AcpError($"An unexpected error occurred while getting {jobType} jobs", ex);
        }
    }

    public async Task<ACPJob?> GetJobByIdAsync(int jobId, string walletAddress)
    {
        try
        {
            var url = $"{_baseUrl}/jobs/{jobId}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("wallet-address", walletAddress);

            _logger?.LogDebug("Making request to: {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ACPJob>>(content);

            if (result?.Error != null)
            {
                throw new AcpApiError(result.Error.Message);
            }

            return result?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while getting job {JobId}", jobId);
            throw new AcpApiError($"Failed to get job by ID: {jobId}", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while getting job {JobId}", jobId);
            throw new AcpError($"An unexpected error occurred while getting job: {jobId}", ex);
        }
    }

    public async Task<ACPMemo?> GetMemoByIdAsync(int jobId, int memoId, string walletAddress)
    {
        try
        {
            var url = $"{_baseUrl}/jobs/{jobId}/memos/{memoId}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("wallet-address", walletAddress);

            _logger?.LogDebug("Making request to: {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ACPMemo>>(content);

            if (result?.Error != null)
            {
                throw new AcpApiError(result.Error.Message);
            }

            return result?.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while getting memo {MemoId} for job {JobId}", memoId, jobId);
            throw new AcpApiError($"Failed to get memo by ID: {memoId}", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while getting memo {MemoId} for job {JobId}", memoId, jobId);
            throw new AcpError($"An unexpected error occurred while getting memo: {memoId}", ex);
        }
    }

    public async Task<IACPAgent?> GetAgentAsync(string walletAddress)
    {
        try
        {
            var url = $"{_baseUrl}/agents?filters[walletAddress]={Uri.EscapeDataString(walletAddress)}";

            _logger?.LogDebug("Making request to: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<IACPAgent>>>(content);

            return result?.Data?.FirstOrDefault();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while getting agent {WalletAddress}", walletAddress);
            throw new AcpApiError("Failed to get agent", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while getting agent {WalletAddress}", walletAddress);
            throw new AcpError("An unexpected error occurred while getting agent", ex);
        }
    }   

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private class ApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("error")]
        public ApiError? Error { get; set; }
    }

    private class ApiError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
