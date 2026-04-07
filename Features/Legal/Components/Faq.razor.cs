using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Legal.Components;

public sealed partial class Faq : ComponentBase
{
    private int? _openIndex;

    private void Toggle(int index) =>
        _openIndex = _openIndex == index ? null : index;

    private sealed record FaqItem(string Question, RenderFragment Answer);

    private readonly List<FaqItem> _faqItems =
    [
        new("What does CloudZen do?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>CloudZen helps small and medium-sized businesses modernize their technology. " +
                "We build custom systems, migrate to the cloud, automate workflows, and create data dashboards — " +
                "so you can focus on growing your business instead of wrestling with outdated tools.</p>");
        }),

        new("How does the Build & Grow model work?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>Our process has three simple stages:</p>" +
                "<ol class=\"faq-ordered-list\">" +
                "<li><strong>Discover</strong> — We learn about your business, goals, and pain points in a free consultation.</li>" +
                "<li><strong>Build</strong> — We design and develop your solution in short stages, with weekly check-ins so you see progress every step of the way.</li>" +
                "<li><strong>Launch &amp; Grow</strong> — We deploy, train your team, and provide ongoing support as your business evolves.</li>" +
                "</ol>");
        }),

        new("How much do your services cost?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>We use a <strong>flat-fee project model</strong> — no hourly surprises. After your free consultation, " +
                "we provide a clear proposal with a fixed price based on your project scope. " +
                "The initial consultation is always free with no obligation.</p>");
        }),

        new("How do I get started?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>Easy — <a href=\"/contact\" class=\"faq-answer-link\">book a free 30-minute consultation</a>. " +
                "We'll discuss your needs, answer your questions, and outline how we can help. " +
                "No commitment required. You can also email us at " +
                "<a href=\"mailto:info@cloud-zen.net\" class=\"faq-answer-link\">info@cloud-zen.net</a> " +
                "and we'll respond within 24 hours.</p>");
        }),

        new("What technologies do you use?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>We work with modern, proven technologies including .NET, Blazor, Azure cloud services, " +
                "PostgreSQL, and AI-powered tools. But we're technology-agnostic — we choose the best tools " +
                "for <em>your</em> specific needs, not the other way around.</p>");
        }),

        new("How long do projects typically take?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>Timelines depend on scope, but most projects follow our staged approach:</p>" +
                "<ul class=\"faq-unordered-list\">" +
                "<li><strong>Small automation or dashboard</strong> — 2 to 4 weeks</li>" +
                "<li><strong>System modernization</strong> — 1 to 3 months</li>" +
                "<li><strong>Full custom platform</strong> — 3 to 6 months</li>" +
                "</ul>" +
                "<p>We break every project into short stages so you see progress weekly — no disappearing for months.</p>");
        }),

        new("Do you offer ongoing support after launch?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>Yes. We don't build and vanish. After launch, we offer ongoing support and maintenance to keep " +
                "your system running smoothly. We can also iterate on your solution as your business grows and needs evolve.</p>");
        }),

        new("What industries do you work with?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>We work across industries — our focus is on <strong>small and medium businesses</strong> that need " +
                "better technology without the overhead of a full IT department. We've helped companies in professional services, " +
                "logistics, healthcare administration, retail operations, and more.</p>");
        }),

        new("Is my data secure?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>We take security seriously. Our website uses HTTPS encryption, strict security headers, and " +
                "all secrets are managed through Azure Key Vault. We follow industry best practices for input validation, " +
                "rate limiting, and data protection. For full details, see our " +
                "<a href=\"/privacy\" class=\"faq-answer-link\">Privacy Policy</a>.</p>");
        }),

        new("What if I'm not sure what I need?", builder =>
        {
            builder.AddMarkupContent(0,
                "<p>That's exactly what the free consultation is for. Many of our clients start with a vague feeling that " +
                "\"things could be better\" — and we help them identify specific improvements that will have the biggest " +
                "impact. No jargon, no pressure. Just a conversation about your goals.</p>");
        }),
    ];
}
