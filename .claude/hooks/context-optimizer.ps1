# SessionStart hook: Provide project context to Claude
$context = "NexTruzt.io EscrowApp: .NET 10 Blazor Server fintech escrow. Clean Architecture + CQRS/MediatR. Layers: Components/ (UI) -> Features/ (handlers) -> Models/Events (domain) <- Data/ (EF Core/PostgreSQL). Payment strategies: IFundHoldable/IFundReleasable/IFundCancellable. Always: code-behind, scoped CSS, docs sync, OWASP security-first, idempotency keys."

@{ additionalContext = $context } | ConvertTo-Json -Compress | Write-Output

exit 0
