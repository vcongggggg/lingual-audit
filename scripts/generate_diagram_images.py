#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Tự động biên dịch các sơ đồ Mermaid sang ảnh PNG độ nét cao (High-DPI) để nhúng vào file DOCX.
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


def generate_diagrams():
    assets_dir = Path(__file__).resolve().parent.parent / "docs" / "assets"
    assets_dir.mkdir(parents=True, exist_ok=True)

    # Sơ đồ 1: 3 Trụ cột giá trị cốt lõi
    d1 = """graph TD
    A["🌟 LINGUAL MEZON"] --> B["1. KHOA HỌC TRÍ NHỚ (Spaced Repetition)"]
    A --> C["2. GẮN KẾT CỘNG ĐỒNG (Clan Social & Gamification)"]
    A --> D["3. ĐỒNG HÀNH THÔNG MINH (AI Companion LingLing)"]
    B --> B1["Thuật toán nhắc ôn đúng điểm rơi trí nhớ (10p/ngày)"]
    C --> C1["Đố vui Bot trong kênh, Đấu trường Clan War, BXH đồng đội"]
    D --> D1["Sửa ngữ pháp tức thì, Chat đóng vai tình huống 24/7"]"""

    # Sơ đồ 2: Kịch bản tương tác người dùng trên Mezon
    d2 = """sequenceDiagram
    autonumber
    actor User as Nguoi hoc (Minh)
    participant Channel as Kenh Chat Clan Mezon
    participant Bot as Lingual Bot
    participant MiniApp as Channel Mini-App (Nhung)
    participant AI as Tro Ly AI LingLing

    Note over User,Bot: Kich ban 1: Tuong tac qua Kenh Chat
    User->>Channel: Go lenh /learn
    Bot-->>Channel: Tra ve Flashcard 3 tu moi hom nay
    User->>Bot: Bam "Toi da thuoc"
    Bot-->>Channel: Cap nhat +15 XP va Thong bao Chuoi 3 ngay!

    Note over User,MiniApp: Kich ban 2: Hoc chuyen sau tren Channel App
    User->>Channel: Bam Tab "Lingual Study Cockpit"
    MiniApp->>User: Mo giao dien bai hoc: Lo trinh Unit, Lat the 3D
    User->>MiniApp: Hoan thanh bai trac nghiem nhanh
    MiniApp-->>User: Ghi nhan tien do SRS va dong bo diem ve Clan

    Note over User,AI: Kich ban 3: Luyen phan xa voi AI LingLing
    User->>AI: Nhan tin: "Hi LingLing, let's practice ordering coffee!"
    AI-->>User: Dong vai Barista: "Welcome to Mezon Cafe!"
    User->>AI: "I want drink a hot chocolate please."
    AI-->>User: "Sure! 💡 Ban nen noi: 'I would like a hot chocolate' nhe!" """

    # Sơ đồ 3: Lộ trình 10 tuần & 4 Sprint của dự án LINGUAL
    d3 = """graph TD
    A["🌟 LỘ TRÌNH 10 TUẦN DỰ ÁN LINGUAL (MCS 2026)"] --> B["GIAI ĐOẠN 1: KICKOFF & CHUẨN BỊ (Tuần 1 - 2)"]
    A --> C["GIAI ĐOẠN 2: PHÁT TRIỂN MVP THỰC CHIẾN (Tuần 3 - 8)"]
    A --> D["GIAI ĐOẠN 3: ĐÓNG BĂNG & BẢO MẬT (Tuần 9)"]
    A --> E["GIAI ĐOẠN 4: NGHIỆM THU & BẢO VỆ (Tuần 10)"]

    B --> B1["🚀 Sprint 0: Milestone 1 (Hạn 12/10/2026)<br/>- CSDL ERD: Minh<br/>- PRD & Plan: Công<br/>- Scaffold .NET & Next.js<br/>- Docker Compose DB & Redis"]

    C --> C1["📦 Sprint 1 (Tuần 3 - 4): CSDL Cốt lõi & Học từ vựng<br/>- EF Core Migrations & Mezon SSO Auth<br/>- Thuật toán Spaced Repetition SM-2<br/>- Giao diện Học Flashcard 3D"]
    C --> C2["🎮 Sprint 2 (Tuần 5 - 6): Gamification & Mezon Bot<br/>- Tính điểm XP, Streak, BXH Clan trên Redis<br/>- Mezon Bot nhận lệnh /learn, /quiz, /streak<br/>- Mini-game Đố vui Từ vựng trong Kênh Chat"]
    C --> C3["⚔️ Sprint 3 (Tuần 7 - 8): Word Duel 1vs1 & AI Tutor<br/>- SignalR GameHub Đấu từ vựng thời gian thực<br/>- Trợ lý AI Mascot LingLing (Google Gemini API)<br/>- Màn hình Đấu trường Word Duel Arena"]

    D --> D1["🛡️ Sprint 4: FEATURE FREEZE (Toàn bộ code xong!)<br/>- Security Audit theo tiêu chuẩn NCC+<br/>- Stress Testing tải cao & Tối ưu CSDL<br/>- Sửa lỗi & Hoàn thiện UI/UX"]

    E --> E1["🏆 Final: DEMO DAY & BẢO VỆ HỘI ĐỒNG<br/>- Triển khai Production lên VPS/Cloud<br/>- Video Demo 3 phút & Slide Thuyết trình<br/>- Bảo vệ đề tài trước Hội đồng NCC+ & Mezon"]"""

    diagrams = [
        ("diagram_pillars.png", d1),
        ("diagram_sequence.png", d2),
        ("diagram_sprint_timeline.png", d3)
    ]

    for filename, code in diagrams:
        print(f"Đang sinh ảnh cho {filename}...")
        payload = {
            "code": code,
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
            target = assets_dir / filename
            target.write_bytes(data)
            print(f"✅ Đã lưu {target} ({len(data)} bytes)")


if __name__ == "__main__":
    generate_diagrams()
