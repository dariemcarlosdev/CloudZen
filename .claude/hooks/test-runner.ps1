# -------------------------------------------------
# Test Runner for Notification Hook (Multi-Channel)
# -------------------------------------------------
# Tests the notification hook and verifies config exists.
# Usage: powershell -File .claude/hooks/test-runner.ps1

Write-Host "=== Claude Hook Notification Test ===" -ForegroundColor Cyan

# 1. Verify config file exists
$configPath = ".claude/hooks/notification-config.json"
if (Test-Path $configPath) {
    Write-Host "[✓] Config file found" -ForegroundColor Green
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-Host "    Enabled channels:" -NoNewline
    $enabled = @()
    if ($config.console.enabled) { $enabled += "Console" }
    if ($config.fileLog.enabled) { $enabled += "File" }
    if ($config.slack.enabled -and $config.slack.webhookUrl) { $enabled += "Slack" }
    if ($config.email.enabled -and $config.email.from -and $config.email.to) { $enabled += "Email" }
    if ($config.teams.enabled -and $config.teams.webhookUrl) { $enabled += "Teams" }
    Write-Host ($enabled -join ", ")
} else {
    Write-Host "[✗] Config file missing. Creating default..." -ForegroundColor Red
    Write-Host "    Run the notification script to auto-create." -ForegroundColor Yellow
}

# 2. Run the notification script directly
Write-Host "`n[→] Triggering notification hook directly..." -ForegroundColor Cyan
powershell -ExecutionPolicy Bypass -File ".claude/hooks/notification.ps1"

# 3. Verify log file created
$logPath = ".claude/hooks/notifications.log"
if (Test-Path $logPath) {
    $lastEntry = Get-Content $logPath -Tail 1
    Write-Host "`n[✓] Log entry created:" -ForegroundColor Green
    Write-Host "    $lastEntry" -ForegroundColor Gray
} else {
    Write-Host "`n[✗] Log file not found" -ForegroundColor Red
}

# 4. Instructions for enabling external channels
Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "To enable Slack, Teams, or Email:" -ForegroundColor White
Write-Host "1. Edit: .claude/hooks/notification-config.json" -ForegroundColor Yellow
Write-Host "2. Set 'enabled' to true and fill in credentials" -ForegroundColor Yellow
Write-Host "3. Re-run this test" -ForegroundColor Yellow

Write-Host "`nExample Slack config:" -ForegroundColor Gray
Write-Host '{ "slack": { "webhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL", "enabled": true } }' -ForegroundColor Gray

Write-Host "`nTest complete! 🎉" -ForegroundColor Green
