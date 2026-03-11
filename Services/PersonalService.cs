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
            new ServiceInfo("💻", "Systems That Work the Way You Do",
                "Stop trying to fit your business into a box. <span class='font-semibold text-indigo-700'>CloudZen</span> builds <span class='font-semibold text-teal-cyan-aqua-300'>custom tools</span> for you designed around your specific daily routine and goals, so your technology finally supports you instead of getting in your way."),

            new ServiceInfo("☁️", "Your Business, Everywhere You Need It",
                "Move your operations online securely so you can access your work from <span class='font-semibold text-teal-cyan-aqua-300'>anywhere</span>. It’s all about giving you more <span class='font-semibold text-indigo-700'>flexibility</span> and <span class='font-semibold text-indigo-700'>lower costs</span> without the &quot;tech headache&quot; or surprise bills."),

            new ServiceInfo("🚀", "Out With the Old, In With the New",
                "Tired of outdated systems holding you back? I'll help you <span class='font-semibold text-teal-cyan-aqua-300'>transition smoothly</span> to <span class='font-semibold text-indigo-700'>modern solutions</span> that keep your business running while setting you up for the future."),

            new ServiceInfo("🚚", "Faster Results mean Happier Customers",
                "<span class='font-semibold text-indigo-700'>CloudZen</span> helps you clear the <span class='font-semibold text-teal-cyan-aqua-300'>bottlenecks</span> so you can respond to clients and deliver your services quicker. When you move <span class='font-semibold text-indigo-700'>faster</span>, your customers stay <span class='font-semibold text-indigo-700'>happier</span> and keep coming back."),

            new ServiceInfo("📊", "Clear, Simple Insights From Your Data",
                "Stop digging through messy spreadsheets. I create simple, clear <span class='font-semibold text-teal-cyan-aqua-300'>dashboards</span> that show you exactly how your business is performing at a glance, so you can make decisions with <span class='font-semibold text-indigo-700'>total confidence</span>."),

            new ServiceInfo("🤖", "Put the \"Busy Work\" on Autopilot",
                "Reclaim your calendar. <span class='font-semibold text-indigo-700'>CloudZen</span> set up <span class='font-semibold text-teal-cyan-aqua-300'>smart systems</span> to handle those boring, repetitive tasks that eat up your day, leaving you free to focus on the work that actually <span class='font-semibold text-indigo-700'>makes you money</span>."),

            new ServiceInfo("🤝", "A Big Team’s Brains, a Solo Partner’s Care",
                "You get the <span class='font-semibold text-indigo-700'>best of both worlds</span>. <span class='font-semibold text-indigo-700'>CloudZen</span> leads your project personally, and when we need a specific niche expert, I bring in a <span class='font-semibold text-teal-cyan-aqua-300'>trusted specialist</span> so you get top-tier results without the corporate runaround."),

            new ServiceInfo("🧪", "Total Peace of Mind on Day One",
                "Your solution undergoes <span class='font-semibold text-teal-cyan-aqua-300'>rigorous testing</span> behind the scenes before your customers ever see it. You can launch your new tools with <span class='font-semibold text-indigo-700'>zero stress</span>, knowing everything will work perfectly the moment you hit &apos;go&apos;."),

            new ServiceInfo("🔁", "You’re in the Loop Every Step of the Way",
                "No &quot;big reveals&quot; or expensive surprises at the end. We work together in <span class='font-semibold text-teal-cyan-aqua-300'>short stages</span> so you can see the <span class='font-semibold text-indigo-700'>progress every week</span> and make sure the final result is exactly what your business needs.")
        ];
    }
}
