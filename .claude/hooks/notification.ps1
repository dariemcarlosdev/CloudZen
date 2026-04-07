# -------------------------------------------------
# Notification Hook – Multi-Channel Support
# -------------------------------------------------
# Features:
# - Console & file logging always on
# - Slack, Teams webhooks
# - Multiple fallback email accounts with credential prompts
# - HTML email templates (configurable)
# - Rate limiting to avoid spam
# - All channels fail independently
#
# Usage:
#   .\notification.ps1                  # Normal run (prompts for email credentials if needed)
#   .\notification.ps1 -SkipEmail       # Skip email notifications entirely
#   .\notification.ps1 -NoPrompt        # Don't prompt for credentials (skip email if not stored)

[CmdletBinding()]
param(
    [switch]$SkipEmail,
    [switch]$NoPrompt
)

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$message = "Notification hook triggered at $timestamp"

# Load configuration
$configPath = ".claude/hooks/notification-config.json"
$config = $null

if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
    } catch {
        Write-Warning "Failed to parse config file. Using defaults."
    }
} else {
    Write-Warning "Config file not found. Creating default."
    $defaultConfig = @{
        slack = @{ webhookUrl = ""; channel = "#claude-hooks"; enabled = $false }
        email = @{
            accounts = @(
                @{
                    smtpServer = "smtp.gmail.com"
                    smtpPort = 587
                    from = ""
                    to = ""
                    subjectPrefix = "[Claude Hook]"
                    useHtml = $true
                    enabled = $false
                }
            )
        }
        teams = @{ webhookUrl = ""; enabled = $false }
        console = @{ enabled = $true }
        fileLog = @{ enabled = $true; path = ".claude/hooks/notifications.log" }
        rateLimit = @{ enabled = $true; intervalSeconds = 30; lastNotificationFile = ".claude/hooks/.rate-limit-timestamp" }
    }
    $defaultConfig | ConvertTo-Json -Depth 3 | Set-Content $configPath
    $config = $defaultConfig
}

# -------------------------------------------------
# Rate Limiting Check
# -------------------------------------------------
$rateLimited = $false
if ($config.rateLimit.enabled) {
    $lastTimePath = $config.rateLimit.lastNotificationFile
    if (-not $lastTimePath) { $lastTimePath = ".claude/hooks/.rate-limit-timestamp" }

    if (Test-Path $lastTimePath) {
        try {
            $lastSent = Get-Content $lastTimePath -Raw | Get-Date
            $interval = $config.rateLimit.intervalSeconds
            if ($interval -lt 1) { $interval = 30 }

            if (((Get-Date) - $lastSent).TotalSeconds -lt $interval) {
                $remaining = [math]::Ceiling($interval - ((Get-Date) - $lastSent).TotalSeconds)
                Write-Host "Rate limit active. Wait $remaining seconds." -ForegroundColor Yellow
                $rateLimited = $true
            }
        } catch {
            # If timestamp exists but invalid, ignore and proceed
        }
    }
}

# If rate-limited, skip all external notifications but still log
if ($rateLimited) {
    if ($config.console.enabled) {
        Write-Host "$message (rate limited)" -ForegroundColor Yellow
    }
    if ($config.fileLog.enabled) {
        $logPath = $config.fileLog.path
        if (-not $logPath) { $logPath = ".claude/hooks/notifications.log" }
        "$timestamp`t$message (rate-limited)" | Add-Content -Path $logPath -Encoding UTF8
    }
    return
}

# Record successful notification time
if ($config.rateLimit.enabled) {
    $lastTimePath = $config.rateLimit.lastNotificationFile
    if (-not $lastTimePath) { $lastTimePath = ".claude/hooks/.rate-limit-timestamp" }
    $timestamp | Set-Content -Path $lastTimePath -Encoding UTF8
}

# -------------------------------------------------
# 1. Console notification
# -------------------------------------------------
if ($config.console.enabled) {
    Write-Host "[BELL] $message" -ForegroundColor Cyan
}

# -------------------------------------------------
# 2. File logging
# -------------------------------------------------
if ($config.fileLog.enabled) {
    $logPath = $config.fileLog.path
    if (-not $logPath) { $logPath = ".claude/hooks/notifications.log" }
    "$timestamp`t$message" | Add-Content -Path $logPath -Encoding UTF8
}

# -------------------------------------------------
# 3. Slack notification
# -------------------------------------------------
if ($config.slack.enabled -and $config.slack.webhookUrl) {
    try {
        $payload = @{
            text = "$message"
            channel = $config.slack.channel
            username = "Claude Hooks"
            icon_emoji = ":robot_face:"
        } | ConvertTo-Json -Depth 10

        Invoke-RestMethod -Uri $config.slack.webhookUrl -Method Post -Body $payload -ContentType "application/json" -ErrorAction Stop | Out-Null
        Write-Host "[OK] Slack notification sent" -ForegroundColor Green
    } catch {
        Write-Warning "Slack notification failed: $($_.Exception.Message)"
    }
}

# -------------------------------------------------
# 4. Email notification - Multiple accounts with fallback
# -------------------------------------------------
if ($SkipEmail) {
    Write-Host "[INFO] Email notification skipped (-SkipEmail)" -ForegroundColor DarkGray
} elseif ($config.email -and $config.email.accounts) {
    $accounts = @($config.email.accounts | Where-Object { $_.enabled -and $_.smtpServer -and $_.from -and $_.to })
    if ($accounts.Count -eq 0) {
        Write-Host "[INFO] Email notification: no enabled accounts with valid config" -ForegroundColor DarkGray
    } else {
        foreach ($account in $accounts) {
            try {
                $subject = "$($account.subjectPrefix) $message"

                # Build HTML or plain text body
                if ($account.useHtml) {
                    $fromAddr = $account.from
                    $toAddr = $account.to
                    $envName = "Development"
                    $body = @"
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4; padding: 20px; }
.container { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); max-width: 600px; margin: 0 auto; }
.header { font-size: 20px; font-weight: 600; color: #333; margin-bottom: 10px; }
.detail { color: #666; line-height: 1.6; }
.detail p { margin: 4px 0; }
.footer { margin-top: 20px; font-size: 12px; color: #999; border-top: 1px solid #eee; padding-top: 10px; }
</style>
</head>
<body>
<div class="container">
<div class="header">Claude Code Hook Triggered</div>
<div class="detail">
<p><strong>Timestamp:</strong> $timestamp</p>
<p><strong>Source:</strong> Claude Code Hook System</p>
<p><strong>From:</strong> $fromAddr</p>
<p><strong>To:</strong> $toAddr</p>
<p><strong>Environment:</strong> $envName</p>
</div>
<div class="footer">
This is an automated notification from Claude Code. Do not reply.
</div>
</div>
</body>
</html>
"@
                    $bodyAsHtml = $true
                } else {
                    $body = "Notification hook triggered at $timestamp. Event: Hook Triggered. Source: Claude Code."
                    $bodyAsHtml = $false
                }

                $safeFrom = $account.from.Replace("@", "_")
                $credPath = ".claude/hooks/smtp-cred-$safeFrom.xml"

                # Credential handling
                $credential = $null
                if (Test-Path $credPath) {
                    try {
                        $credential = Import-CliXml -Path $credPath
                        Write-Host "[INFO] Using stored credentials for $($account.from)" -ForegroundColor DarkGray
                    } catch {
                        Write-Warning "Failed to load stored credentials for $($account.from): $($_.Exception.Message)"
                        $credential = $null
                    }
                }

                if ((-not (Test-Path $credPath)) -or $null -eq $credential) {
                    if ($NoPrompt) {
                        Write-Host "[INFO] No stored credentials for $($account.from) and -NoPrompt specified. Skipping email." -ForegroundColor DarkGray
                        continue
                    }

                    Write-Host "[INFO] Enter SMTP credentials for $($account.from) on $($account.smtpServer)" -ForegroundColor Yellow
                    try {
                        $credential = Get-Credential -UserName $account.from -Message "Enter password for $($account.from)"
                        if ($credential) {
                            $credential | Export-CliXml -Path $credPath
                            Write-Host "[OK] Credentials saved (encrypted)" -ForegroundColor Green
                        } else {
                            Write-Warning "No credentials provided for $($account.from). Skipping..."
                            continue
                        }
                    } catch {
                        if ($_.Exception.Message -like "*Get-Credential*") {
                            Write-Warning "Get-Credential failed (non-interactive environment?). Skipping email."
                        } else {
                            Write-Warning "Credential prompt failed: $($_.Exception.Message)"
                        }
                        continue
                    }
                }

                $smtpParams = @{
                    SmtpServer = $account.smtpServer
                    Port = $account.smtpPort
                    From = $account.from
                    To = $account.to
                    Subject = $subject
                    Body = $body
                    UseSsl = $true
                    Credential = $credential
                }

                if ($bodyAsHtml) {
                    $smtpParams.BodyAsHtml = $true
                }

                Send-MailMessage @smtpParams -ErrorAction Stop
                Write-Host "[OK] Email sent via $($account.from) to $($account.to)" -ForegroundColor Green
                break
            } catch {
                Write-Warning "Email via $($account.from) failed: $($_.Exception.Message)"
                continue
            }
        }
    }
}

# -------------------------------------------------
# 5. Teams notification
# -------------------------------------------------
if ($config.teams.enabled -and $config.teams.webhookUrl) {
    try {
        $card = @{
            title = "Claude Hook Notification"
            text = $message
            themeColor = "0076D7"
            sections = @(
                @{
                    activityTitle = "Hook Triggered"
                    activitySubtitle = $timestamp
                    facts = @(
                        @{ name = "Source"; value = "Claude Code" },
                        @{ name = "Environment"; value = "Development" }
                    )
                }
            )
        } | ConvertTo-Json -Depth 10

        Invoke-RestMethod -Uri $config.teams.webhookUrl -Method Post -Body $card -ContentType "application/json" -ErrorAction Stop | Out-Null
        Write-Host "[OK] Teams notification sent" -ForegroundColor Green
    } catch {
        Write-Warning "Teams notification failed: $($_.Exception.Message)"
    }
}
