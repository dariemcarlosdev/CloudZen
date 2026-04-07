# PostToolUse hook: Check .NET/Blazor coding conventions
$jsonInput = [Console]::In.ReadToEnd()
if (-not $jsonInput) { exit 0 }

$data = $jsonInput | ConvertFrom-Json

$filePath = $data.tool_input.file_path
if (-not $filePath) { exit 0 }

$issues = @()

if ($filePath -match '\.cs$') {
    $content = $data.tool_input.content
    if (-not $content) { $content = $data.tool_input.new_str }
    if (-not $content) { $content = $data.tool_input.file_text }
    if (-not $content) { exit 0 }

    # Check for block-scoped namespaces (should use file-scoped)
    if ($content -match 'namespace\s+\S+\s*\{') {
        $issues += "Use file-scoped namespace (no braces) instead of block-scoped namespace"
    }

    # Check code-behind files missing partial keyword
    if ($filePath -match '\.razor\.cs$' -and $content -match 'class\s+' -and $content -notmatch 'partial\s+class') {
        $issues += "Code-behind class must be declared as 'partial'"
    }

    # Check for missing nullable enable
    if ($content -match 'namespace\s+' -and $content -notmatch '#nullable\s+enable' -and $content -notmatch '<Nullable>enable</Nullable>') {
        $issues += "Consider adding '#nullable enable' or verify it is set in .csproj"
    }
}
elseif ($filePath -match '\.razor$') {
    $content = $data.tool_input.content
    if (-not $content) { $content = $data.tool_input.new_str }
    if (-not $content) { $content = $data.tool_input.file_text }
    if (-not $content) { exit 0 }

    # Check for inline @code blocks (should use code-behind)
    if ($content -match '@code\s*\{') {
        $issues += "Use code-behind (.razor.cs) instead of inline @code blocks"
    }

    # Check for inline style attributes
    if ($content -match 'style\s*=\s*"') {
        $issues += "Use scoped CSS (.razor.css) instead of inline style attributes"
    }
}
else {
    exit 0
}

if ($issues.Count -gt 0) {
    $message = "Convention issues: " + ($issues -join "; ") + ". Fix before continuing."
    @{ additionalContext = $message } | ConvertTo-Json -Compress | Write-Output
}

exit 0
