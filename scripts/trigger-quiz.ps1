param(
    [long[]]$ChannelIds
)

$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$url = "http://localhost:5000/api/v1/mezon/quiz/trigger"
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   [LINGUAL] PHAT DONG QUIZ TRIGGER LIEN CLAN (CROSS-CLAN)  " -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan

$body = @{}
if ($ChannelIds -and $ChannelIds.Length -gt 0) {
    $body["channelIds"] = $ChannelIds
}

$jsonBody = $body | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $jsonBody -TimeoutSec 15
    Write-Host "SlotKey: $($response.slotKey) (Time: $($response.timeSlot))" -ForegroundColor Green
    Write-Host "Total Clans: $($response.totalClans)" -ForegroundColor Green
    Write-Host "------------------------------------------------------------"
    foreach ($res in $response.results) {
        if ($res.success) {
            Write-Host " [SUCCESS] Channel $($res.channelId) -> $($res.message) (SessionId: $($res.sessionId))" -ForegroundColor Cyan
        } else {
            Write-Host " [FAILED] Channel $($res.channelId) -> $($res.message) $($res.error)" -ForegroundColor Yellow
        }
    }
    Write-Host "------------------------------------------------------------"
    Write-Host "[OK] Phat dong thanh cong tren tat ca cac Clan!" -ForegroundColor Green
    Write-Host "Mo Mezon kiem tra dong thoi o Clan TESTBOTLINGLING va DUT CAN TRI!" -ForegroundColor White
} catch {
    Write-Host "[ERROR] Loi khi goi API: $_" -ForegroundColor Red
    Write-Host "Dam bao Backend Lingual.Api dang chay tai http://localhost:5000" -ForegroundColor Yellow
}
