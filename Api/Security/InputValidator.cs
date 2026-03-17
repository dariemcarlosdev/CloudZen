using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CloudZen.Api.Security;

/// <summary>
/// Provides input validation and sanitization utilities to protect against common security attack vectors
/// including XSS (Cross-Site Scripting), SQL injection, and other malicious input patterns.
/// </summary>
/// <remarks>
/// This static class should be used to validate and sanitize all user inputs before processing.
/// It implements defense-in-depth strategies for input handling in the CloudZen API.
/// </remarks>
public static partial class InputValidator
{
    /// <summary>
    /// Collection of dangerous patterns commonly used in XSS and injection attacks.
    /// Covers OWASP A03:2021 – Injection threat vectors including script injection,
    /// JavaScript protocol handlers, HTML event handlers, and dangerous HTML elements.
    /// </summary>
    private static readonly HashSet<string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Script injection
        "<script",
        "</script",
        // JavaScript and VBScript protocol handlers
        "javascript:",
        "vbscript:",
        // Data URI injection
        "data:text/html",
        "data:application/javascript",
        // Dangerous HTML elements that can execute scripts or load remote content
        "<iframe",
        "<frame",
        "<object",
        "<embed",
        // Mouse/pointer event handlers
        "onerror=",
        "onclick=",
        "onload=",
        "onmouseover=",
        "onmouseout=",
        "onmousedown=",
        "onmouseup=",
        "onmousemove=",
        "ondblclick=",
        "oncontextmenu=",
        // Focus/keyboard event handlers
        "onfocus=",
        "onblur=",
        "onkeydown=",
        "onkeypress=",
        "onkeyup=",
        // Form event handlers
        "onchange=",
        "onsubmit=",
        "onreset=",
        "onselect=",
        "oninput=",
        "oninvalid=",
        // Document/window event handlers
        "onabort=",
        "onunload=",
        "onbeforeunload=",
        // Code execution
        "eval(",
        "expression(",
        // Server-Side Template Injection (SSTI) patterns
        "${",
        "#{",
        "<%=",
    };

    /// <summary>
    /// Validates an email address format and checks for malicious content.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether the email is valid.
    /// Returns <see cref="ValidationResult.Valid"/> if the email passes all checks,
    /// or <see cref="ValidationResult.Invalid"/> with an appropriate error message otherwise.
    /// </returns>
    /// <remarks>
    /// Validation includes:
    /// <list type="bullet">
    ///   <item><description>Null or empty check</description></item>
    ///   <item><description>Maximum length validation (254 characters per RFC 5321)</description></item>
    ///   <item><description>Dangerous content pattern detection</description></item>
    ///   <item><description>Format validation using <see cref="System.Net.Mail.MailAddress"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = InputValidator.ValidateEmail("user@example.com");
    /// if (!result.IsValid)
    /// {
    ///     Console.WriteLine(result.ErrorMessage);
    /// }
    /// </code>
    /// </example>
    public static ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Invalid("Email is required.");

        if (email.Length > 254)
            return ValidationResult.Invalid("Email address is too long.");

        if (ContainsDangerousContent(email))
            return ValidationResult.Invalid("Invalid email format.");

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                return ValidationResult.Invalid("Invalid email format.");

            return ValidationResult.Valid();
        }
        catch
        {
            return ValidationResult.Invalid("Invalid email format.");
        }
    }

    /// <summary>
    /// Validates and sanitizes a text input field against security threats and length constraints.
    /// </summary>
    /// <param name="input">The text input to validate.</param>
    /// <param name="fieldName">The display name of the field (used in error messages).</param>
    /// <param name="maxLength">The maximum allowed length for the input. Defaults to 1000 characters.</param>
    /// <param name="required">Indicates whether the field is required. Defaults to <c>true</c>.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether the input is valid.
    /// Returns <see cref="ValidationResult.Valid"/> if validation passes,
    /// or <see cref="ValidationResult.Invalid"/> with a descriptive error message otherwise.
    /// </returns>
    /// <remarks>
    /// Validation includes:
    /// <list type="bullet">
    ///   <item><description>Null or whitespace check (if required)</description></item>
    ///   <item><description>Maximum length enforcement</description></item>
    ///   <item><description>XSS pattern detection via <see cref="ContainsDangerousContent"/></description></item>
    ///   <item><description>SQL injection pattern detection via <see cref="ContainsSqlInjectionPatterns"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = InputValidator.ValidateTextInput(userMessage, "Message", maxLength: 5000);
    /// if (!result.IsValid)
    /// {
    ///     return BadRequest(result.ErrorMessage);
    /// }
    /// </code>
    /// </example>
    public static ValidationResult ValidateTextInput(string? input, string fieldName, int maxLength = 1000, bool required = true)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return required
                ? ValidationResult.Invalid($"{fieldName} is required.")
                : ValidationResult.Valid();
        }

        if (input.Length > maxLength)
            return ValidationResult.Invalid($"{fieldName} exceeds maximum length of {maxLength} characters.");

        if (ContainsDangerousContent(input))
            return ValidationResult.Invalid($"{fieldName} contains invalid content.");

        // Check for SQL injection patterns
        if (ContainsSqlInjectionPatterns(input))
            return ValidationResult.Invalid($"{fieldName} contains invalid content.");

        // Check for path traversal patterns (A01: Broken Access Control)
        if (ContainsPathTraversal(input))
            return ValidationResult.Invalid($"{fieldName} contains invalid content.");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Checks if the input contains potentially dangerous content patterns commonly used in XSS attacks.
    /// </summary>
    /// <param name="input">The string input to check for dangerous patterns.</param>
    /// <returns>
    /// <c>true</c> if the input contains any dangerous patterns defined in <see cref="DangerousPatterns"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method performs case-insensitive matching against known XSS attack vectors including
    /// script tags, JavaScript protocol handlers, and event handler attributes.
    /// </remarks>
    public static bool ContainsDangerousContent(string input)
    {
        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks for common SQL injection patterns in the input string.
    /// </summary>
    /// <param name="input">The string input to check for SQL injection patterns.</param>
    /// <returns>
    /// <c>true</c> if the input contains any SQL injection patterns; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Detects common SQL injection techniques including:
    /// <list type="bullet">
    ///   <item><description>Comment-based injection (e.g., '; --)</description></item>
    ///   <item><description>Tautology attacks (e.g., 1=1, ' OR ''=')</description></item>
    ///   <item><description>Union-based injection (e.g., UNION SELECT)</description></item>
    ///   <item><description>Destructive statements (e.g., DROP, DELETE, INSERT)</description></item>
    /// </list>
    /// Note: This is a supplementary defense layer. Always use parameterized queries as the primary protection.
    /// </remarks>
    private static bool ContainsSqlInjectionPatterns(string input)
    {
        // Common SQL injection patterns
        string[] sqlPatterns =
        {
            "'; --",
            "'; DROP",
            "1=1",
            "1' OR '1'='1",
            "' OR ''='",
            "UNION SELECT",
            "UNION ALL SELECT",
            "INSERT INTO",
            "DELETE FROM",
            "UPDATE SET"
        };

        foreach (var pattern in sqlPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for path traversal patterns in the input string.
    /// Prevents OWASP A01:2021 – Broken Access Control via directory traversal.
    /// </summary>
    /// <param name="input">The string input to check for path traversal patterns.</param>
    /// <returns>
    /// <c>true</c> if the input contains path traversal patterns; otherwise, <c>false</c>.
    /// </returns>
    public static bool ContainsPathTraversal(string input)
    {
        string[] traversalPatterns =
        [
            // Standard path traversal
            "../", "..\\", "/..", "\\..",
            // URL-encoded forward slash variants
            "%2e%2e%2f", "%2e%2e/", "..%2f",
            // URL-encoded backslash variants
            "%2e%2e%5c", "%2e%2e\\", "..%5c",
            // Double URL-encoded variants (bypass double-decode scenarios)
            "%252e%252e%252f", "%252e%252e%255c",
        ];
        foreach (var pattern in traversalPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Validates a URL against an allowlist of permitted hosts to prevent
    /// Server-Side Request Forgery (SSRF) attacks (OWASP A10:2021).
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="allowedHosts">
    /// A non-empty array of permitted hostnames (e.g., "api.example.com").
    /// Passing an empty array is a configuration error and will cause all URLs to be rejected.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether the URL is safe to use.
    /// </returns>
    /// <remarks>
    /// Only HTTPS scheme URLs to known allowed hosts are permitted.
    /// Private/loopback addresses and non-HTTPS schemes are rejected.
    /// An empty <paramref name="allowedHosts"/> array is treated as deny-all for security.
    /// </remarks>
    public static ValidationResult ValidateUrl(string? url, string[] allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Invalid("URL is required.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ValidationResult.Invalid("Invalid URL format.");

        // Only allow HTTPS scheme (A02: Cryptographic Failures)
        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Invalid("Only HTTPS URLs are allowed.");

        // Block private/loopback IP ranges to prevent SSRF to internal services (A10: SSRF)
        var host = uri.Host;
        if (IsPrivateOrLoopbackHost(host))
            return ValidationResult.Invalid("URL points to a private or internal address.");

        // Validate against the allowlist — an empty allowlist means no host is permitted (deny-all)
        if (allowedHosts.Length == 0 || !allowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            return ValidationResult.Invalid("URL host is not permitted.");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Determines whether a hostname or IP address refers to a private, loopback,
    /// or link-local address that should be blocked to prevent SSRF attacks.
    /// </summary>
    /// <param name="host">The hostname or IP address string to check.</param>
    /// <returns><c>true</c> if the host is private or loopback; otherwise, <c>false</c>.</returns>
    private static bool IsPrivateOrLoopbackHost(string host)
    {
        // Block localhost and loopback hostnames
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase))
            return true;

        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            return System.Net.IPAddress.IsLoopback(ip) || IsPrivateIpAddress(ip);
        }

        return false;
    }

    /// <summary>
    /// Checks whether an IP address falls within a private or link-local range.
    /// </summary>
    private static bool IsPrivateIpAddress(System.Net.IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        // IPv4 private ranges
        if (bytes.Length == 4)
        {
            return bytes[0] == 10 ||                                        // 10.0.0.0/8
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
                   (bytes[0] == 192 && bytes[1] == 168) ||                  // 192.168.0.0/16
                   (bytes[0] == 169 && bytes[1] == 254) ||                  // 169.254.0.0/16 (link-local)
                   bytes[0] == 127;                                          // 127.0.0.0/8 (loopback)
        }

        // IPv6 private/link-local/loopback ranges
        if (bytes.Length == 16)
        {
            // ::1 loopback (handled by IsLoopback but checked here for completeness)
            if (System.Net.IPAddress.IsLoopback(ip)) return true;

            // fc00::/7 — Unique Local Addresses (ULA)
            if ((bytes[0] & 0xFE) == 0xFC) return true;

            // fe80::/10 — link-local
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) return true;

            // fec0::/10 — deprecated site-local (still used in some environments)
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0xC0) return true;

            // ::ffff:0:0/96 — IPv4-mapped IPv6 addresses (e.g., ::ffff:10.0.0.1)
            // Bytes 0-9 are 0, bytes 10-11 are 0xFF
            bool isIpv4Mapped = bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0 &&
                                 bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0 &&
                                 bytes[8] == 0 && bytes[9] == 0 && bytes[10] == 0xFF && bytes[11] == 0xFF;
            if (isIpv4Mapped)
            {
                // Check the embedded IPv4 address (bytes 12-15)
                return bytes[12] == 10 ||
                       (bytes[12] == 172 && bytes[13] >= 16 && bytes[13] <= 31) ||
                       (bytes[12] == 192 && bytes[13] == 168) ||
                       (bytes[12] == 169 && bytes[13] == 254) ||
                       bytes[12] == 127;
            }
        }

        return false;
    }

    /// <summary>
    /// Sanitizes HTML content by encoding special characters to prevent XSS attacks.
    /// </summary>
    /// <param name="input">The raw string input that may contain HTML characters.</param>
    /// <returns>
    /// An HTML-encoded string where special characters are converted to their HTML entity equivalents
    /// (e.g., &lt; becomes &amp;lt;). Returns <see cref="string.Empty"/> if the input is null or whitespace.
    /// </returns>
    /// <remarks>
    /// Uses <see cref="System.Net.WebUtility.HtmlEncode"/> for encoding.
    /// This method should be called before embedding any user-provided content in HTML output.
    /// </remarks>
    /// <example>
    /// <code>
    /// var safeContent = InputValidator.SanitizeHtml("&lt;script&gt;alert('xss')&lt;/script&gt;");
    /// // Result: "&amp;lt;script&amp;gt;alert('xss')&amp;lt;/script&amp;gt;"
    /// </code>
    /// </example>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return System.Net.WebUtility.HtmlEncode(input);
    }

    /// <summary>
    /// Sanitizes input for safe logging by masking sensitive data patterns.
    /// </summary>
    /// <param name="input">The string input to sanitize for logging purposes.</param>
    /// <returns>
    /// A sanitized string safe for logging, with sensitive data masked and length truncated if necessary.
    /// Returns "[empty]" if the input is null or whitespace.
    /// </returns>
    /// <remarks>
    /// Sanitization includes:
    /// <list type="bullet">
    ///   <item><description>Email addresses are replaced with "[email]"</description></item>
    ///   <item><description>Potential API keys/tokens (32+ alphanumeric characters) are replaced with "[token]"</description></item>
    ///   <item><description>Content exceeding 200 characters is truncated with "...[truncated]" suffix</description></item>
    /// </list>
    /// This helps prevent accidental exposure of PII or secrets in log files.
    /// </remarks>
    public static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[empty]";

        // Mask email addresses
        var result = EmailMaskRegex().Replace(input, "[email]");

        // Mask potential API keys or tokens (long alphanumeric strings)
        result = TokenMaskRegex().Replace(result, "[token]");

        // Truncate if too long
        if (result.Length > 200)
            result = result[..200] + "...[truncated]";

        return result;
    }

    /// <summary>
    /// Generated regex pattern for matching email addresses.
    /// </summary>
    /// <returns>A compiled <see cref="Regex"/> for email address detection.</returns>
    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailMaskRegex();

    /// <summary>
    /// Generated regex pattern for matching potential API keys or tokens.
    /// </summary>
    /// <returns>A compiled <see cref="Regex"/> for token detection (32+ consecutive alphanumeric characters).</returns>
    [GeneratedRegex(@"\b[a-zA-Z0-9]{32,}\b")]
    private static partial Regex TokenMaskRegex();
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// Use <see cref="Valid"/> to create a successful result or <see cref="Invalid"/> to create a failed result with an error message.
/// </remarks>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    /// <value><c>true</c> if validation was successful; otherwise, <c>false</c>.</value>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the error message when validation fails.
    /// </summary>
    /// <value>A descriptive error message if <see cref="IsValid"/> is <c>false</c>; otherwise, <c>null</c>.</value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> with <see cref="IsValid"/> set to <c>true</c>.</returns>
    public static ValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// </summary>
    /// <param name="message">The error message describing why validation failed.</param>
    /// <returns>A <see cref="ValidationResult"/> with <see cref="IsValid"/> set to <c>false</c> and the error message populated.</returns>
    public static ValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Extension methods to add security headers to HTTP responses and extract request information.
/// </summary>
/// <remarks>
/// These extensions implement security best practices for HTTP response headers
/// as recommended by OWASP and security frameworks.
/// </remarks>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds standard security headers to the HTTP response to protect against common web vulnerabilities.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/> to add security headers to.</param>
    /// <remarks>
    /// The following security headers are added:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Header</term>
    ///     <description>Purpose</description>
    ///   </listheader>
    ///   <item>
    ///     <term>X-Frame-Options: DENY</term>
    ///     <description>Prevents clickjacking attacks by disabling iframe embedding</description>
    ///   </item>
    ///   <item>
    ///     <term>X-Content-Type-Options: nosniff</term>
    ///     <description>Prevents MIME type sniffing attacks</description>
    ///   </item>
    ///   <item>
    ///     <term>X-XSS-Protection: 1; mode=block</term>
    ///     <description>Enables XSS filter in legacy browsers</description>
    ///   </item>
    ///   <item>
    ///     <term>Referrer-Policy</term>
    ///     <description>Controls referrer information leakage</description>
    ///   </item>
    ///   <item>
    ///     <term>Content-Security-Policy</term>
    ///     <description>Restricts resource loading to prevent XSS</description>
    ///   </item>
    ///   <item>
    ///     <term>Permissions-Policy</term>
    ///     <description>Disables sensitive browser features</description>
    ///   </item>
    ///   <item>
    ///     <term>Cache-Control/Pragma</term>
    ///     <description>Prevents caching of sensitive responses</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public static void AddSecurityHeaders(this HttpResponse response)
    {
        var headers = response.Headers;

        // Prevent clickjacking attacks
        headers.TryAdd("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        headers.TryAdd("X-Content-Type-Options", "nosniff");

        // Enable XSS filter in older browsers
        headers.TryAdd("X-XSS-Protection", "1; mode=block");

        // Control referrer information
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content Security Policy
        headers.TryAdd("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

        // Permissions Policy (previously Feature-Policy)
        headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        // HTTP Strict Transport Security — force HTTPS for 1 year (A02: Cryptographic Failures)
        headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

        // Cache control for sensitive data
        headers.TryAdd("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
        headers.TryAdd("Pragma", "no-cache");
    }

    /// <summary>
    /// Adds CORS headers to the HTTP response for cross-origin requests.
    /// Required for Azure Functions isolated worker model where host.json CORS doesn't work.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/> to add CORS headers to.</param>
    /// <param name="request">The <see cref="HttpRequest"/> to check the origin from.</param>
    /// <param name="corsSettings">The CORS settings containing allowed origins.</param>
    public static void AddCorsHeaders(this HttpResponse response, HttpRequest request, CorsSettings corsSettings)
    {
        var origin = request.Headers["Origin"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(origin) && corsSettings.IsOriginAllowed(origin))
        {
            response.Headers.TryAdd("Access-Control-Allow-Origin", origin);
            response.Headers.TryAdd("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.TryAdd("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-Correlation-Id");
            response.Headers.TryAdd("Access-Control-Max-Age", "600"); // 10 minutes
        }
    }

    /// <summary>
    /// Checks if the request is a CORS preflight (OPTIONS) request.
    /// </summary>
    /// <param name="request">The HTTP request to check.</param>
    /// <returns>True if this is a preflight request, false otherwise.</returns>
    public static bool IsCorsPreflightRequest(this HttpRequest request)
    {
        return request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) &&
               request.Headers.ContainsKey("Origin") &&
               request.Headers.ContainsKey("Access-Control-Request-Method");
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP request, considering proxy and load balancer headers.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to extract the client IP from.</param>
    /// <returns>
    /// The client's IP address as a string. Returns "unknown" if the IP address cannot be determined.
    /// </returns>
    /// <remarks>
    /// The method checks headers in the following priority order:
    /// <list type="number">
    ///   <item><description>X-Forwarded-For header (standard proxy header, uses first IP in chain)</description></item>
    ///   <item><description>X-Azure-ClientIP header (Azure-specific header)</description></item>
    ///   <item><description>Connection.RemoteIpAddress (direct connection IP)</description></item>
    /// </list>
    /// When behind reverse proxies or load balancers, the X-Forwarded-For header typically contains
    /// the original client IP as the first entry in a comma-separated list.
    /// </remarks>
    public static string GetClientIpAddress(this HttpRequest request)
    {
        // Check for forwarded headers (when behind a proxy/load balancer)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs; take the first one
            var ip = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Check Azure-specific header
        var clientIp = request.Headers["X-Azure-ClientIP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
            return clientIp;

        // Fall back to connection remote IP
        return request.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// CORS settings for isolated worker model functions.
/// </summary>
public record CorsSettings(string[] AllowedOrigins)
{
    /// <summary>
    /// Checks if the specified origin is allowed by CORS policy.
    /// Supports wildcard "*" to allow any origin (intended for staging environments only).
    /// Staging fuctions App need to allow all origins to work with the Blazor WASM app running on localhost, but production should specify allowed origins explicitly.
    /// </summary>
    /// <param name="origin">The origin to check.</param>
    /// <returns>True if the origin is allowed, false otherwise.</returns>
    public bool IsOriginAllowed(string? origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;
        if (AllowedOrigins.Contains("*")) return true; // only for staging/testing, not recommended for production
        return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
