# UserPromptSubmit hook: Encourage research-first approach before implementation
$jsonInput = [Console]::In.ReadToEnd()
if (-not $jsonInput) { exit 0 }

$data = $jsonInput | ConvertFrom-Json

$prompt = $data.user_prompt
if (-not $prompt) { exit 0 }

$implKeywords = @('create', 'implement', 'build', 'add', 'write', 'refactor', 'fix', 'update', 'modify', 'change', 'delete', 'remove', 'replace', 'migrate')
$researchKeywords = @('explain', 'analyze', 'review', 'understand', 'explore', 'investigate', 'describe', 'show', 'list', 'what is', 'how does', 'why')

$promptLower = $prompt.ToLower()

$hasImpl = $false
foreach ($kw in $implKeywords) {
    if ($promptLower -match "\b$kw\b") {
        $hasImpl = $true
        break
    }
}

$hasResearch = $false
foreach ($kw in $researchKeywords) {
    if ($promptLower -match "\b$kw\b") {
        $hasResearch = $true
        break
    }
}

if ($hasImpl -and -not $hasResearch) {
    @{ additionalContext = [char]0x1F4DA + " Research-First: Before implementing, check docs/ for existing documentation and understand the affected architecture layer." } | ConvertTo-Json -Compress | Write-Output
}

exit 0
