using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CloudZen.Models.Options;

namespace CloudZen.Services
{
    public class ResumeService
    {
        private readonly HttpClient _httpClient;
        private readonly BlobStorageOptions _options;
        private readonly ILogger<ResumeService> _logger;

        public string ResumeBlobUrl => _options.ResumeUrl;

        public ResumeService(HttpClient httpClient, IOptions<BlobStorageOptions> options, ILogger<ResumeService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<byte[]> DownloadResumeAsync()
        {
            _logger.LogInformation("Downloading resume from URL: {ResumeBlobUrl}", _options.ResumeUrl);
            return await _httpClient.GetByteArrayAsync(_options.ResumeUrl);
        }
    }
}
