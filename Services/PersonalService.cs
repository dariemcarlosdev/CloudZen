using CloudZen.Models;

namespace CloudZen.Services;

/// <summary>
/// Service for managing and retrieving personal service offerings.
/// This service centralizes service data management and can be extended to load from external sources (API, database, JSON files, etc.).
/// </summary>
public class PersonalService
{
    /// <summary>
    /// Retrieves all professional services offered.
    /// </summary>
    /// <returns>A list of ServiceInfo objects representing the service portfolio.</returns>
    public List<ServiceInfo> GetAllServices()
    {
        return GetServicesData();
    }

    /// <summary>
    /// Central method containing all service data.
    /// TODO: In future, this can be replaced with loading from:
    /// - JSON file (wwwroot/data/services.json)
    /// - Database (via Entity Framework)
    /// - External API
    /// </summary>
    /// <returns>Complete list of services.</returns>
    private List<ServiceInfo> GetServicesData()
    {
        return
        [
            new ServiceInfo("💻", "Custom Solutions Built for Your Business",
                "Get a system that actually fits how you work — designed around your goals, your processes, and your customers. No generic templates, just solutions that grow with you."),

            new ServiceInfo("☁️", "Smarter Hosting & Infrastructure",
                "Move your business online with confidence. Cut costs, gain flexibility, and support your growth — without the tech overwhelm or hidden surprises."),

            new ServiceInfo("🚀", "Modernize Without the Disruption",
                "Tired of outdated systems holding you back? I'll help you transition smoothly to modern solutions that keep your business running while setting you up for the future."),

            new ServiceInfo("🚚", "Faster Delivery, Happier Customers",
                "Speed up how you deliver updates and new services. Reduce delays, streamline your operations, and give your customers better experiences—every time."),

            new ServiceInfo("📊", "Clear Insights from Your Data",
                "Stop guessing. Get visual dashboards that turn your raw data into decisions you can act on - see what matters, when it matters."),

            new ServiceInfo("🤖", "Let AI Handle the Repetitive Stuff",
                "Free up your time to focus on what matters. I help you automate tasks, catch issues early, and deliver smarter experiences — all powered by practical AI."),

            new ServiceInfo("🤝", "Extended Expertise When You Need It",
                "Need expertise beyond one person? I bring in trusted specialists to fill gaps and deliver complete solutions—so you get results without the hiring headache."),

            new ServiceInfo("🧪", "Launch with Confidence",
                "No crossed fingers at launch. I test everything thoroughly so your solution works reliably from day one — giving you and your customers peace of mind."),

            new ServiceInfo("🔁", "We Build Together, Step by Step",
                "Your input shapes every stage. I work in focused cycles so you see progress quickly, give feedback, and get exactly what your business needs—no surprises.")
        ];
    }
}
