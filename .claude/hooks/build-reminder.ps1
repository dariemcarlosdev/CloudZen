# PostToolUse hook: Remind to verify build after source file changes
$jsonInput = [Console]::In.ReadToEnd()
if (-not $jsonInput) { exit 0 }

$data = $jsonInput | ConvertFrom-Json

$filePath = $data.tool_input.file_path
if (-not $filePath) { exit 0 }

if ($filePath -match '\.(cs|csproj|razor)$') {
    @{ additionalContext = [char]0x1F3D7 + [char]0xFE0F + " Source file modified. Remember to verify the build compiles (dotnet build)." } | ConvertTo-Json -Compress | Write-Output
}

exit 0
