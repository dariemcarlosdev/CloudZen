using Microsoft.AspNetCore.Components;

namespace CloudZen.Common.Components;

/// <summary>
/// Reusable pagination component that displays page navigation controls.
/// Accepts total item count, page size, and current page; emits page-change events to the parent.
/// </summary>
/// <remarks>
/// SOLID alignment:
/// - S: Single purpose — pagination navigation only.
/// - O: Configurable via parameters (page size, visible page count) without code changes.
/// - I: Focused parameter surface — only what's needed.
/// - D: No service dependencies; pure presentation component driven by parent state.
/// </remarks>
public partial class Pagination
{
    // ── Parameters ────────────────────────────────────────────────────────

    /// <summary>Total number of items across all pages.</summary>
    [Parameter, EditorRequired]
    public int TotalItems { get; set; }

    /// <summary>Number of items displayed per page.</summary>
    [Parameter, EditorRequired]
    public int PageSize { get; set; } = 5;

    /// <summary>The current active page (1-based).</summary>
    [Parameter, EditorRequired]
    public int CurrentPage { get; set; } = 1;

    /// <summary>Maximum number of page buttons visible in the navigation bar.</summary>
    [Parameter]
    public int MaxVisiblePages { get; set; } = 5;

    /// <summary>Fires when the user selects a different page.</summary>
    [Parameter, EditorRequired]
    public EventCallback<int> OnPageChanged { get; set; }

    // ── Computed ──────────────────────────────────────────────────────────

    private int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    private bool HasPrevious => CurrentPage > 1;
    private bool HasNext => CurrentPage < TotalPages;

    // ── Handlers ─────────────────────────────────────────────────────────

    private async Task GoToPage(int page)
    {
        if (page < 1 || page > TotalPages || page == CurrentPage) return;
        await OnPageChanged.InvokeAsync(page);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the visible page numbers with ellipsis gaps when the total
    /// page count exceeds <see cref="MaxVisiblePages"/>.
    /// Returns null entries to represent ellipsis ("...") placeholders.
    /// </summary>
    internal IEnumerable<int?> GetVisiblePageNumbers()
    {
        if (TotalPages <= MaxVisiblePages)
        {
            for (int i = 1; i <= TotalPages; i++)
                yield return i;
            yield break;
        }

        int half = MaxVisiblePages / 2;
        int start = Math.Max(2, CurrentPage - half);
        int end = Math.Min(TotalPages - 1, CurrentPage + half);

        // Adjust window when near edges
        if (start <= 2) end = Math.Min(TotalPages - 1, MaxVisiblePages - 1);
        if (end >= TotalPages - 1) start = Math.Max(2, TotalPages - MaxVisiblePages + 2);

        yield return 1;

        if (start > 2) yield return null; // left ellipsis

        for (int i = start; i <= end; i++)
            yield return i;

        if (end < TotalPages - 1) yield return null; // right ellipsis

        yield return TotalPages;
    }
}
