namespace CloudZen.Models.Options;

/// <summary>
/// Configuration options for Azure Blob Storage access.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the ASP.NET Core Options pattern to configure blob storage access
/// for the resume download feature. Settings are configured in <c>appsettings.json</c>
/// under the <c>BlobStorage</c> section.
/// </para>
/// <para>
/// <b>Security Note:</b> In Blazor WebAssembly, only SAS token URLs should be stored here.
/// Never store connection strings or account keys in client-side configuration.
/// </para>
/// <para>
/// Example configuration in <c>wwwroot/appsettings.json</c>:
/// <code>
/// {
///   "BlobStorage": {
///     "ResumeUrl": "https://storage.blob.core.windows.net/container/resume.pdf?sv=...",
///     "ContainerName": "documents"
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class BlobStorageOptions
{
    /// <summary>
    /// The configuration section name for blob storage options.
    /// </summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the full URL (with SAS token) for the resume PDF.
    /// </summary>
    /// <value>The resume blob URL including SAS token for read access.</value>
    public string ResumeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob container name.
    /// </summary>
    /// <value>The container name. Defaults to "documents".</value>
    public string ContainerName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets the storage account name (for display/logging purposes only).
    /// </summary>
    /// <value>The storage account name.</value>
    public string? StorageAccountName { get; set; }
}
