#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Tự động biên dịch Sơ đồ Lộ trình Sprint sang ảnh PNG độ nét cao để nhúng vào file DOCX.
"""

import urllib.request
import base64
import json
import sys
from pathlib import Path

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass


def generate_sprint_diagram():
    assets_dir = Path(__file__).resolve().parent.parent / "docs" / "assets"
    assets_dir.mkdir(parents=True, exist_ok=True)

    mermaid_code = """gantt
    title LỘ TRÌNH 10 TUẦN PHÁT TRIỂN DỰ ÁN LINGUAL (MCS 2026)
    dateFormat  YYYY-MM-DD
    axisFormat  %d/%m

    section GIAI ĐOẠN 1: KICKOFF & CHUẨN BỊ
    Sprint 0: Kickoff, CSDL ERD, PRD & Scaffold (Milestone 1) :done, s0, 2026-10-01, 2026-10-12

    section GIAI ĐOẠN 2: PHÁT TRIỂN MVP THỰC CHIẾN
    Sprint 1: Core DB EF Core, Auth SSO & SRS SM-2 Engine :active, s1, 2026-10-13, 2026-10-26
    Sprint 2: Gamification (XP, Streak, BXH) & Mezon Bot :s2, 2026-10-27, 2026-11-09
    Sprint 3: Realtime 1vs1 Word Duel (SignalR) & AI Tutor :s3, 2026-11-10, 2026-11-23

    section GIAI ĐOẠN 3: ĐÓNG BĂNG & BẢO MẬT (TUẦN 9)
    Sprint 4: Feature Freeze, Security Audit NCC+ & QA :crit, s4, 2026-11-24, 2026-11-30

    section GIAI ĐOẠN 4: NGHIỆM THU & BẢO VỆ (TUẦN 10)
    Final: Production Deploy, Video Demo & Demo Day :milestone, f1, 2026-12-01, 2026-12-07
"""

    print("Đang sinh ảnh cho diagram_sprint_timeline.png...")
    payload = {
        "code": mermaid_code,
        "mermaid": {
            "theme": "default",
            "themeVariables": {
                "fontFamily": "Segoe UI, Arial, sans-serif",
                "fontSize": "16px",
                "primaryColor": "#EBF8FF",
                "primaryBorderColor": "#3182CE",
                "lineColor": "#2B6CB0"
            }
        }
    }
    b64 = base64.urlsafe_b64encode(json.dumps(payload).encode("utf-8")).decode("ascii").rstrip("=")
    url = f"https://mermaid.ink/img/{b64}"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=25) as resp:
        data = resp.read()
        target = assets_dir / "diagram_sprint_timeline.png"
        target.write_bytes(data)
        print(f"✅ Đã lưu {target} ({len(data)} bytes)")


if __name__ == "__main__":
    generate_sprint_diagram()
