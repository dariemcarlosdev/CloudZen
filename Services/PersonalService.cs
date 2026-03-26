using CloudZen.Models;
using CloudZen.Services.Abstractions;

namespace CloudZen.Services;

/// <summary>
/// Service for managing and retrieving personal service offerings.
/// This service centralizes service data management and can be extended to load from external sources (API, database, JSON files, etc.).
/// </summary>
public class PersonalService : IPersonalService
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
    /// Icon values are Bootstrap Icon class names (without the "bi-" prefix is added in the component).
    /// </summary>
    /// <returns>Complete list of services.</returns>
    private List<ServiceInfo> GetServicesData()
    {
        return
        [
            new ServiceInfo("bi-laptop", "Systems That Work the Way You Do",
                "Stop trying to fit your business into a box. <span class='font-semibold text-teal-cyan-aqua-600'>CloudZen</span> builds <span class='font-semibold text-gray-800'>custom tools</span> designed around your specific daily routine and goals, so your technology finally supports you instead of getting in your way."),

            new ServiceInfo("bi-cloud-arrow-up", "Your Business, Everywhere You Need It",
                "Move your operations online securely so you can access your work from <span class='font-semibold text-gray-800'>anywhere</span>. It's all about giving you more <span class='font-semibold text-teal-cyan-aqua-600'>flexibility</span> and <span class='font-semibold text-teal-cyan-aqua-600'>lower costs</span> without the tech headache or surprise bills."),

            new ServiceInfo("bi-rocket-takeoff", "Out With the Old, In With the New",
                "Tired of outdated systems holding you back? We help you <span class='font-semibold text-gray-800'>transition smoothly</span> to <span class='font-semibold text-teal-cyan-aqua-600'>modern solutions</span> that keep your business running while setting you up for the future."),

            new ServiceInfo("bi-speedometer2", "Faster Results Mean Happier Customers",
                "<span class='font-semibold text-teal-cyan-aqua-600'>CloudZen</span> helps you clear the <span class='font-semibold text-gray-800'>bottlenecks</span> so you can respond to clients and deliver your services quicker. When you move faster, your customers stay happier and keep coming back."),

            new ServiceInfo("bi-bar-chart-line", "Clear, Simple Insights From Your Data",
                "Stop digging through messy spreadsheets. We create simple, clear <span class='font-semibold text-gray-800'>dashboards</span> that show you exactly how your business is performing at a glance, so you can make decisions with <span class='font-semibold text-teal-cyan-aqua-600'>total confidence</span>."),

            new ServiceInfo("bi-robot", "Put the Busy Work on Autopilot",
                "Reclaim your calendar. <span class='font-semibold text-teal-cyan-aqua-600'>CloudZen</span> sets up <span class='font-semibold text-gray-800'>smart systems</span> to handle those boring, repetitive tasks that eat up your day, leaving you free to focus on the work that actually makes you money."),

            new ServiceInfo("bi-people", "A Big Team's Brains, a Solo Partner's Care",
                "You get the <span class='font-semibold text-teal-cyan-aqua-600'>best of both worlds</span>. CloudZen leads your project personally, and when we need a specific niche expert, we bring in a <span class='font-semibold text-gray-800'>trusted specialist</span> so you get top-tier results without the corporate runaround."),

            new ServiceInfo("bi-shield-check", "Total Peace of Mind on Day One",
                "Your solution undergoes <span class='font-semibold text-gray-800'>rigorous testing</span> behind the scenes before your customers ever see it. You can launch your new tools with <span class='font-semibold text-teal-cyan-aqua-600'>zero stress</span>, knowing everything will work perfectly the moment you hit go."),

            new ServiceInfo("bi-arrow-repeat", "You're in the Loop Every Step of the Way",
                "No big reveals or expensive surprises at the end. We work together in <span class='font-semibold text-gray-800'>short stages</span> so you can see the <span class='font-semibold text-teal-cyan-aqua-600'>progress every week</span> and make sure the final result is exactly what your business needs.")
        ];
    }
}
