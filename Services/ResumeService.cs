using RestSharp.Portable;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloudZen.Services
{
    public class ResumeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ResumeBlobUrl;
        private readonly ILogger<ResumeService> _logger;
        // This implement one of programming best practices: Encapsulation as exposing the field via a public property with only a getter.
        public string ResumeBlobUrl => _ResumeBlobUrl; // Public property to access the URL if needed
        public ResumeService(HttpClient httpClient, string resumeBlobUrl, ILogger<ResumeService> logger)
        {
            _httpClient = httpClient;
            _ResumeBlobUrl = resumeBlobUrl;
            _logger = logger;
        }

        public async Task<byte[]> DownloadResumeAsync()
        {
            _logger.LogInformation("Downloading resume from URL: {ResumeBlobUrl}", _ResumeBlobUrl);
            return await _httpClient.GetByteArrayAsync(_ResumeBlobUrl);
        }
    }
}
