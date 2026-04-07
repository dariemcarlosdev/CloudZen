# PostToolUse hook: Remind to update documentation when key source files change
$jsonInput = [Console]::In.ReadToEnd()
if (-not $jsonInput) { exit 0 }

$data = $jsonInput | ConvertFrom-Json

$filePath = $data.tool_input.file_path
if (-not $filePath) { exit 0 }

$docTriggerPaths = @(
    'Components/',  'Components\\'
    'Features/',    'Features\\'
    'Services/',    'Services\\'
    'Models/',      'Models\\'
    'Events/',      'Events\\'
    'Infrastructure/', 'Infrastructure\\'
)

$needsDocSync = $false
foreach ($trigger in $docTriggerPaths) {
    if ($filePath -like "*$trigger*") {
        $needsDocSync = $true
        break
    }
}

if ($needsDocSync) {
    @{ additionalContext = [char]0x1F4DD + " Remember: update corresponding docs/ README.md to reflect these changes." } | ConvertTo-Json -Compress | Write-Output
}

exit 0
