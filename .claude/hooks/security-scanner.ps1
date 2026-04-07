# PreToolUse hook: Scan for hardcoded secrets in file content
$jsonInput = [Console]::In.ReadToEnd()
if (-not $jsonInput) { exit 0 }

$data = $jsonInput | ConvertFrom-Json

$toolName = $data.tool_name
if ($toolName -notmatch 'Edit|Write|MultiEdit|Create') { exit 0 }

$content = $null
if ($data.tool_input.content) { $content = $data.tool_input.content }
elseif ($data.tool_input.new_str) { $content = $data.tool_input.new_str }
elseif ($data.tool_input.file_text) { $content = $data.tool_input.file_text }

if (-not $content) { exit 0 }

$secretPatterns = @(
    @{ Name = "Connection string with password"; Pattern = '(?i)(connection\s*string|Server=|Data Source=).*(?:Password|Pwd)\s*=' }
    @{ Name = "AWS access key";                  Pattern = 'AKIA[0-9A-Z]{16}' }
    @{ Name = "API key (sk- prefix)";            Pattern = 'sk-[a-zA-Z0-9]{20,}' }
    @{ Name = "API key (pk_ prefix)";            Pattern = 'pk_[a-zA-Z0-9]{20,}' }
    @{ Name = "Bearer token";                    Pattern = '(?i)bearer\s+[a-zA-Z0-9\-._~+/]+=*' }
    @{ Name = "Private key block";               Pattern = '-----BEGIN\s+(RSA\s+)?PRIVATE KEY-----' }
    @{ Name = "Hardcoded password literal";       Pattern = '(?i)(password|passwd|pwd)\s*=\s*"[^"]{4,}"' }
    @{ Name = "Generic secret assignment";        Pattern = '(?i)(secret|api_key|apikey)\s*=\s*"[^"]{8,}"' }
)

foreach ($sp in $secretPatterns) {
    if ($content -match $sp.Pattern) {
        $result = @{
            hookSpecificOutput = @{
                hookEventName          = "PreToolUse"
                permissionDecision     = "deny"
                permissionDecisionReason = "Security: hardcoded secret detected ($($sp.Name)). Use user-secrets, environment variables, or Azure Key Vault instead."
            }
        }
        $result | ConvertTo-Json -Depth 3 -Compress | Write-Output
        exit 0
    }
}

exit 0
