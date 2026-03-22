using CloudZen.Models;

namespace CloudZen.Services;

/// <summary>
/// Provides the list of tool/feature items displayed in the Tools Overview section.
/// </summary>
public class ToolService
{
    public List<ToolInfo> GetAllTools() => new()
    {
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-person-check\"></i>",
            Title: "One Point of Contact",
            Description: "Work directly with the person building your solution — no phone trees, no runaround, just clear answers when you need them."
        ),
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-people\"></i>",
            Title: "The Right Help, When You Need It",
            Description: "When your project calls for specialized skills, we bring in trusted experts, so every detail is covered and nothing slips through the cracks."
        ),
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-graph-up-arrow\"></i>",
            Title: "Real-World Results",
            Description: "Proven results replacing outdated systems, saving teams hours every week, and helping businesses win more customers through streamlined operations."
        ),
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-bullseye\"></i>",
            Title: "Solutions That Fit Your Business",
            Description: "Every business is different. We tailor solutions to what actually moves the needle for you. Less hassle, more impact."
        ),
        //new ToolInfo(
        //    SvgMarkup: "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 64 64\" stroke=\"currentColor\" stroke-width=\"1.5\" class=\"w-full h-full\">"
        //        + "<rect x=\"8\" y=\"20\" width=\"48\" height=\"30\" rx=\"4\" />"
        //        + "<line x1=\"8\" y1=\"30\" x2=\"56\" y2=\"30\" />"
        //        + "<circle cx=\"20\" cy=\"14\" r=\"6\" />"
        //        + "<circle cx=\"32\" cy=\"14\" r=\"6\" />"
        //        + "<circle cx=\"44\" cy=\"14\" r=\"6\" />"
        //        + "<path d=\"M16 38h8M16 42h6\" />"
        //        + "<path d=\"M22 25l2 2 4-4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />"
        //        + "<path d=\"M32 25l2 2 4-4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />"
        //        + "<path d=\"M42 25l2 2 4-4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />"
        //        + "</svg>",
        //    Title: "Online Reviews",
        //    Description: "Automate your online reviews with a few simple clicks & respond to reviews in one place."
        //),
        //new ToolInfo(
        //    SvgMarkup: "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 64 64\" stroke=\"currentColor\" stroke-width=\"1.5\" class=\"w-full h-full\">"
        //        + "<rect x=\"12\" y=\"12\" width=\"40\" height=\"32\" rx=\"4\" />"
        //        + "<line x1=\"20\" y1=\"22\" x2=\"44\" y2=\"22\" />"
        //        + "<line x1=\"20\" y1=\"28\" x2=\"44\" y2=\"28\" />"
        //        + "<line x1=\"20\" y1=\"34\" x2=\"36\" y2=\"34\" />"
        //        + "<path d=\"M24 44l-6 8v-8z\" />"
        //        + "</svg>",
        //    Title: "Messaging",
        //    Description: "Manage your messages with a single inbox for text, Facebook messages, Google messages, and more."
        //),
        //new ToolInfo(
        //    SvgMarkup: "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 64 64\" stroke=\"currentColor\" stroke-width=\"1.5\" class=\"w-full h-full\">"
        //        + "<circle cx=\"22\" cy=\"28\" r=\"14\" />"
        //        + "<circle cx=\"42\" cy=\"28\" r=\"14\" />"
        //        + "<circle cx=\"26\" cy=\"26\" r=\"1.5\" fill=\"currentColor\" />"
        //        + "<circle cx=\"32\" cy=\"26\" r=\"1.5\" fill=\"currentColor\" />"
        //        + "<circle cx=\"38\" cy=\"26\" r=\"1.5\" fill=\"currentColor\" />"
        //        + "</svg>",
        //    Title: "Webchat",
        //    Description: "Convert more website visitors into leads & sales conversations with Webchat."
        //),
        //new ToolInfo(
        //    SvgMarkup: "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 64 64\" stroke=\"currentColor\" stroke-width=\"1.5\" class=\"w-full h-full\">"
        //        + "<circle cx=\"20\" cy=\"20\" r=\"6\" />"
        //        + "<circle cx=\"44\" cy=\"20\" r=\"6\" />"
        //        + "<circle cx=\"20\" cy=\"44\" r=\"6\" />"
        //        + "<circle cx=\"44\" cy=\"44\" r=\"6\" />"
        //        + "<path d=\"M26 20h12M20 26v12M44 26v12M26 44h12\" stroke-linecap=\"round\" />"
        //        + "<circle cx=\"32\" cy=\"32\" r=\"6\" />"
        //        + "<circle cx=\"31\" cy=\"31\" r=\"1\" fill=\"currentColor\" />"
        //        + "<circle cx=\"34\" cy=\"31\" r=\"1\" fill=\"currentColor\" />"
        //        + "<path d=\"M30 34c0 1.5 1.5 2.5 3 2.5s3-1 3-2.5\" stroke-linecap=\"round\" />"
        //        + "</svg>",
        //    Title: "Missed Call Text Back",
        //    Description: "Never lose a lead again — automatically text back customers when you miss their call."
        //),
        //new ToolInfo(
        //    SvgMarkup: "<svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 64 64\" stroke=\"currentColor\" stroke-width=\"1.5\" class=\"w-full h-full\">"
        //        + "<circle cx=\"32\" cy=\"32\" r=\"20\" />"
        //        + "<line x1=\"32\" y1=\"12\" x2=\"32\" y2=\"52\" />"
        //        + "<line x1=\"12\" y1=\"32\" x2=\"52\" y2=\"32\" />"
        //        + "<polyline points=\"20,40 28,28 36,34 44,22\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />"
        //        + "<text x=\"24\" y=\"50\" font-size=\"8\" fill=\"currentColor\" stroke=\"none\" font-weight=\"bold\">CRM</text>"
        //        + "</svg>",
        //    Title: "CRM",
        //    Description: "Track every lead, manage your pipeline, and close more deals with a powerful built-in CRM."
        //),
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-lightning-charge\"></i>",
            Title: "Speed & Reliability",
            Description: "Fast, scalable systems that grow with you - so you can reach customers and get to market quickly and confidently."
        ),
        new ToolInfo(
            IconMarkup: "<i class=\"bi bi-robot\"></i>",
            Title: "Smart Automation",
            Description: "Automate repetitive tasks and free up your team to focus on what actually matters - growing your business."
        )
    };
}
