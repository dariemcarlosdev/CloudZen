using Microsoft.AspNetCore.Components;

namespace CloudZen.Common.Components;

/// <summary>
/// Displays an animated floating card with progress bar, stat counters, and terminal log.
/// Animations loop continuously, restarting after completion.
/// </summary>
public partial class AutomationProgressCard : ComponentBase, IDisposable
{
    #region Parameters

    /// <summary>
    /// Window title displayed in the chrome bar.
    /// </summary>
    [Parameter] public string Title { get; set; } = "cloudzen-workflow.ai";

    /// <summary>
    /// Label for the progress bar.
    /// </summary>
    [Parameter] public string ProgressLabel { get; set; } = "Automation Progress";

    /// <summary>
    /// Target progress percentage (0-100). Animation will cycle from 0 to 100.
    /// </summary>
    [Parameter] public int TargetProgress { get; set; } = 100;

    /// <summary>
    /// Number of tasks to display in stats.
    /// </summary>
    [Parameter] public int TasksCount { get; set; } = 847;

    /// <summary>
    /// Number of hours to display in stats.
    /// </summary>
    [Parameter] public int HoursCount { get; set; } = 124;

    /// <summary>
    /// Number of workflows to display in stats.
    /// </summary>
    [Parameter] public int WorkflowsCount { get; set; } = 18;

    /// <summary>
    /// Custom terminal log messages. Uses default messages if not provided.
    /// </summary>
    [Parameter] public List<TerminalMessage>? TerminalMessages { get; set; }

    /// <summary>
    /// Whether to loop the animation continuously. Defaults to true.
    /// </summary>
    [Parameter] public bool Loop { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds before restarting the animation loop. Defaults to 2000ms.
    /// </summary>
    [Parameter] public int LoopDelayMs { get; set; } = 2000;

    /// <summary>
    /// Delay in milliseconds between each terminal message appearing. Defaults to 800ms.
    /// </summary>
    [Parameter] public int TerminalMessageDelayMs { get; set; } = 800;

    #endregion

    #region State

    private bool _isVisible;
    private bool _statsVisible;
    private bool _showCursor = true;
    private int _currentProgress;
    private int _currentTasks;
    private int _currentHours;
    private int _currentWorkflows;
    private List<TerminalMessage> _visibleMessages = [];
    private CancellationTokenSource? _cts;

    // Animation timing constants
    private const int ProgressSteps = 50;
    private const int ProgressStepDelayMs = 120; // Slower animation (was 60ms)
    private static int ProgressDurationMs => ProgressSteps * ProgressStepDelayMs; // 6000ms total

    #endregion

    #region Computed Properties

    private string CardCssClass => _isVisible ? "automation-card float-animation" : "automation-card";

    private string GetStatCardCssClass(int delayIndex) =>
        _statsVisible ? $"stat-card stat-enter stat-delay-{delayIndex}" : "stat-card stat-hidden";

    private static List<TerminalMessage> DefaultMessages =>
    [
        new("Scanning legacy systems", false),
        new("12 automation opportunities found", true),
        new("Building custom dashboard", false),
        new("Connecting with autopilot-framework",true),
        new("Deployment complete", true)
    ];

    #endregion

    #region Lifecycle

    /// <summary>
    /// Blazor lifecycle hook invoked after the component has rendered.
    /// On the very first render, initializes the cancellation token and kicks off
    /// the animation loop so the card appears with its entrance + progress sequence.
    /// Subsequent renders are ignored to prevent duplicate animation loops.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _cts = new CancellationTokenSource();
            await RunAnimationLoopAsync();
        }
    }

    /// <summary>
    /// Disposes the component by cancelling any in-flight animation tasks and releasing
    /// the <see cref="CancellationTokenSource"/>. Calling <see cref="GC.SuppressFinalize"/>
    /// satisfies the dispose pattern since no finalizer is needed.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Main animation orchestrator that drives the full card lifecycle.
    /// Each iteration resets state, runs the entrance + progress + counter + terminal
    /// sequence, then optionally pauses for <see cref="LoopDelayMs"/> before restarting.
    /// The loop exits gracefully when <see cref="_cts"/> is cancelled (component disposal)
    /// or when <see cref="Loop"/> is <c>false</c> (single-play mode).
    /// </summary>
    private async Task RunAnimationLoopAsync()
    {
        try
        {
            do
            {
                // Reset state for new animation cycle
                ResetAnimationState();
                await InvokeAsync(StateHasChanged);

                // Run the animation sequence
                await StartAnimationSequenceAsync();

                if (Loop && !_cts!.Token.IsCancellationRequested)
                {
                    // Hold at 100% briefly before restarting
                    await Task.Delay(LoopDelayMs, _cts.Token);
                }

            } while (Loop && !_cts!.Token.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            // Component disposed during animation - expected behavior
        }
    }

    /// <summary>
    /// Resets all mutable animation state (progress percentage, stat counters,
    /// visible terminal messages, and stats visibility) back to zero/empty so
    /// the next animation cycle starts from a clean slate. The card's own
    /// visibility (<see cref="_isVisible"/>) is intentionally preserved after
    /// the first run to avoid a jarring disappear-reappear flash between loops.
    /// </summary>
    private void ResetAnimationState()
    {
        _currentProgress = 0;
        _currentTasks = 0;
        _currentHours = 0;
        _currentWorkflows = 0;
        _visibleMessages = [];
        _statsVisible = false;
        // Keep card visible after first run for smooth transitions
    }

    /// <summary>
    /// Executes a single animation cycle from start to finish. On the very first
    /// cycle the card fades in; on subsequent cycles it skips that step. After a
    /// short stagger delay the stat cards appear, then the progress bar, counters,
    /// and terminal messages all animate concurrently via <see cref="Task.WhenAll"/>,
    /// ensuring they finish at roughly the same time regardless of message count.
    /// </summary>
    private async Task StartAnimationSequenceAsync()
    {
        // Initial delay before card appears (only on first run)
        if (!_isVisible)
        {
            await Task.Delay(200, _cts!.Token);
            _isVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        // Small delay before starting new cycle
        await Task.Delay(300, _cts!.Token);

        // Show stats cards with stagger animation
        _statsVisible = true;
        await InvokeAsync(StateHasChanged);

        // Run all animations concurrently - they will complete at approximately the same time
        var progressTask = AnimateProgressAsync();
        var countersTask = AnimateCountersAsync();
        var terminalTask = ShowTerminalMessagesAsync();

        // Wait for all animations to complete
        await Task.WhenAll(progressTask, countersTask, terminalTask);
    }

    /// <summary>
    /// Smoothly animates the progress bar from 0 to <see cref="TargetProgress"/>
    /// over <see cref="ProgressSteps"/> increments. Each step adds a fixed fraction
    /// of the target, capped with <see cref="Math.Min"/> to prevent overshooting,
    /// and triggers a re-render so the CSS width binding updates in real time.
    /// Total duration equals <see cref="ProgressSteps"/> × <see cref="ProgressStepDelayMs"/>.
    /// </summary>
    private async Task AnimateProgressAsync()
    {
        var increment = (double)TargetProgress / ProgressSteps;

        for (var i = 1; i <= ProgressSteps && !_cts!.Token.IsCancellationRequested; i++)
        {
            _currentProgress = (int)Math.Min(increment * i, TargetProgress);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(ProgressStepDelayMs, _cts.Token);
        }

        _currentProgress = TargetProgress;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Animates the three stat counters (Tasks, Hours, Workflows) from 0 to their
    /// target values using a cubic ease-out curve <c>(1 − (1−t)³)</c> so the numbers
    /// accelerate quickly then settle smoothly. The step delay is derived from
    /// <see cref="ProgressDurationMs"/> to keep counters synchronized with the
    /// progress bar. Final values are snapped to exact targets after the loop to
    /// avoid rounding drift.
    /// </summary>
    private async Task AnimateCountersAsync()
    {
        // Match counter animation to progress duration
        const int steps = 30;
        var delayMs = ProgressDurationMs / steps; // Sync with progress bar

        for (var i = 1; i <= steps && !_cts!.Token.IsCancellationRequested; i++)
        {
            var progress = (double)i / steps;
            // Cubic ease-out for smoother animation
            var eased = 1 - Math.Pow(1 - progress, 3);

            _currentTasks = (int)(TasksCount * eased);
            _currentHours = (int)(HoursCount * eased);
            _currentWorkflows = (int)(WorkflowsCount * eased);

            await InvokeAsync(StateHasChanged);
            await Task.Delay(delayMs, _cts!.Token);
        }

        // Ensure final values are exact
        _currentTasks = TasksCount;
        _currentHours = HoursCount;
        _currentWorkflows = WorkflowsCount;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Reveals terminal log messages one at a time with a staggered delay, simulating
    /// a real CLI output stream. The delay between messages is calculated so all
    /// messages appear within the progress bar's total duration (minus a 200 ms buffer),
    /// but is capped at <see cref="TerminalMessageDelayMs"/> so messages never feel
    /// sluggish. Uses <see cref="TerminalMessages"/> when provided, otherwise falls
    /// back to <see cref="DefaultMessages"/>.
    /// </summary>
    private async Task ShowTerminalMessagesAsync()
    {
        var messages = TerminalMessages ?? DefaultMessages;
        var messageCount = messages.Count;

        if (messageCount == 0) return;

        // Calculate delay so all messages appear within the progress bar duration
        // Subtract a small buffer to ensure last message appears before progress completes
        var totalTimeForMessages = ProgressDurationMs - 200; // 200ms buffer
        var calculatedDelay = totalTimeForMessages / messageCount;

        // Use the calculated delay or the parameter, whichever fits the timeframe
        var delayMs = Math.Min(TerminalMessageDelayMs, calculatedDelay);

        foreach (var message in messages)
        {
            if (_cts!.Token.IsCancellationRequested) break;

            _visibleMessages.Add(message);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(delayMs, _cts.Token);
        }
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Represents a terminal log message with optional highlight styling.
    /// </summary>
    /// <param name="Text">The message text to display.</param>
    /// <param name="IsHighlight">Whether to highlight this message (shown in orange with + prefix).</param>
    public record TerminalMessage(string Text, bool IsHighlight);

    #endregion
}
