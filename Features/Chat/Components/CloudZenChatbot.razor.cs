using System.Text.RegularExpressions;

namespace CloudZen.Features.Chat.Components;

/// <summary>
/// Code-behind partial class for the <see cref="CloudZenChatbot"/> Blazor component.
/// Contains helper logic for detecting and highlighting contact information
/// (emails, phone numbers, and URLs) within chatbot message content.
/// </summary>
public partial class CloudZenChatbot
{
    /// <summary>
    /// Compiled regular expression that matches common contact-information patterns
    /// within message text, including URLs, email addresses, and phone numbers.
    /// </summary>
    /// <remarks>
    /// Named capture groups:
    /// <list type="bullet">
    ///   <item><description><c>url</c> - matches <c>http://</c> and <c>https://</c> URLs.</description></item>
    ///   <item><description><c>email</c> - matches standard email address formats.</description></item>
    ///   <item><description><c>phone</c> - matches North-American-style phone numbers with optional country code.</description></item>
    /// </list>
    /// </remarks>
    private static readonly Regex ContactPattern = new(
        @"(?<url>https?://[^\s<>""']+)|(?<email>[\w.+-]+@[\w-]+\.[\w.-]+)|(?<phone>(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Scans the supplied message <paramref name="content"/> for contact information and
    /// returns an HTML string in which every detected email, phone number, or URL is
    /// wrapped in a styled, clickable <c>&lt;a&gt;</c> tag.
    /// </summary>
    /// <param name="content">The raw text content of a chatbot message.</param>
    /// <returns>
    /// An HTML-encoded string with contact-information segments replaced by anchor
    /// elements that use the <c>contact-highlight</c> CSS class.
    /// </returns>
    /// <remarks>
    /// The input is first HTML-encoded via <see cref="System.Net.WebUtility.HtmlEncode"/>
    /// to prevent XSS, then regex replacements insert safe anchor markup.
    /// </remarks>
    private static string HighlightContactInfo(string content)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(content);
        return ContactPattern.Replace(encoded, match =>
        {
            if (match.Groups["email"].Success)
            {
                var email = match.Value;
                return $"""<a href="mailto:{email}" class="contact-highlight">📧 {email}</a>""";
            }
            if (match.Groups["phone"].Success)
            {
                var phone = match.Value;
                return $"""<a href="tel:{phone}" class="contact-highlight">📞 {phone}</a>""";
            }
            if (match.Groups["url"].Success)
            {
                var url = match.Value;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    return match.Value;
                }
                var safeHref = new Uri(url).AbsoluteUri;
                return $"""<a href="{safeHref}" target="_blank" rel="noopener noreferrer" class="contact-highlight">🔗 {url}</a>""";
            }
            return match.Value;
        });
    }
}
