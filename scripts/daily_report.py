#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script tự động sinh báo cáo Daily Standup và hỗ trợ gửi thông báo cho dự án Mezon Campus Studio 2026.
Tác giả: Ngô Văn Công
"""

import sys
import subprocess
from datetime import datetime
from pathlib import Path

# Fix console encoding on Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass


def get_today_git_commits(repo_dir: Path) -> list[str]:
    try:
        # Lấy commit từ 00:00 hôm nay
        since_time = datetime.now().strftime("%Y-%m-%d 00:00:00")
        cmd = ["git", "log", f"--since={since_time}", "--pretty=format:- %s (%h)"]
        res = subprocess.run(cmd, cwd=str(repo_dir), stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=False)
        lines = [line.strip() for line in res.stdout.splitlines() if line.strip()]
        return lines
    except Exception:
        return []


def generate_daily_standup_report(project_name: str = "Mezon Campus Studio 2026") -> str:
    today_str = datetime.now().strftime("%d/%m/%Y")
    repo_dir = Path(__file__).resolve().parent.parent
    commits = get_today_git_commits(repo_dir)

    yesterday_done = "\n".join(commits) if commits else "- Hoàn thành thiết lập cấu trúc nền tảng và tài liệu kỹ thuật."

    report = f"""**[DAILY STANDUP] - Ngày: {today_str}**
- **Họ và tên:** Ngô Văn Công
- **Dự án:** {project_name}
- **Vai trò:** Fullstack Developer / Lead

1. **Hôm qua / Hôm nay đã làm được gì (Accomplished):**
{yesterday_done}

2. **Kế hoạch tiếp theo (Next Steps):**
   - [ ] Hoàn thiện Product Requirements Document (PRD) chi tiết.
   - [ ] Họp thống nhất ý tưởng sản phẩm cùng Mentor NCC+ / Mezon.
   - [ ] Khởi tạo module mã nguồn chính thức.

3. **Khó khăn / Vướng mắc (Blockers):**
   - None (Mọi việc đang tiến triển đúng lộ trình).

4. **Thời gian đã cống hiến:** 2 giờ.
"""
    return report


def main():
    report = generate_daily_standup_report()
    print("\n" + "=" * 60)
    print("📋 MẪU BÁO CÁO DAILY STANDUP HÔM NAY:")
    print("=" * 60)
    print(report)
    print("=" * 60)

    # Tùy chọn gửi qua Telegram nếu có script notify_telegram
    if "--telegram" in sys.argv:
        try:
            telegram_script = Path(__file__).resolve().parent.parent.parent / "notify_telegram.py"
            if telegram_script.exists():
                import importlib.util
                spec = importlib.util.spec_from_file_location("notify_telegram", str(telegram_script))
                mod = importlib.util.module_from_spec(spec)
                spec.loader.exec_module(mod)
                if hasattr(mod, "send_telegram_notification"):
                    sent = mod.send_telegram_notification(f"🚀 <b>[MCS] BÁO CÁO TIẾN ĐỘ</b>\n\n<pre>{report}</pre>")
                    if sent:
                        print("✅ Đã gửi báo cáo Daily thành công về Telegram của anh Văn Công!")
                    else:
                        print("⚠️ Gửi qua Telegram không thành công.")
        except Exception as e:
            print(f"⚠️ Lỗi gửi Telegram: {e}")


if __name__ == "__main__":
    main()
