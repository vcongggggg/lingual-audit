param(
    [long[]]$ChannelIds
)

$url = "http://localhost:5000/api/v1/mezon/quiz/trigger"
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   🚀 LINGUAL - PHÁT ĐỘNG QUIZ TRIGGER LIÊN CLAN (CROSS-CLAN)" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan

$body = @{}
if ($ChannelIds -and $ChannelIds.Length -gt 0) {
    $body["channelIds"] = $ChannelIds
}

$jsonBody = $body | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json" -Body $jsonBody -TimeoutSec 15
    Write-Host "Khung giờ phát động (SlotKey): $($response.slotKey) (Giờ: $($response.timeSlot))" -ForegroundColor Green
    Write-Host "Tổng số Clan mục tiêu: $($response.totalClans)" -ForegroundColor Green
    Write-Host "------------------------------------------------------------"
    foreach ($res in $response.results) {
        if ($res.success) {
            Write-Host " [OK] Kênh $($res.channelId) -> $($res.message) (SessionId: $($res.sessionId))" -ForegroundColor Cyan
        } else {
            Write-Host " [WARN/ERR] Kênh $($res.channelId) -> $($res.message) $($res.error)" -ForegroundColor Yellow
        }
    }
    Write-Host "------------------------------------------------------------"
    Write-Host "✅ Đã phát động thành công lên các Clan!" -ForegroundColor Green
    Write-Host "👉 Hãy mở Mezon kiểm tra đồng thời ở Clan TESTBOTLINGLING & ĐỤT CẬN TRĨ!" -ForegroundColor White
} catch {
    Write-Host "❌ Lỗi khi gọi API phát động: $_" -ForegroundColor Red
    Write-Host "Vui lòng đảm bảo Backend Lingual.Api đang chạy tại http://localhost:5000" -ForegroundColor Yellow
}
