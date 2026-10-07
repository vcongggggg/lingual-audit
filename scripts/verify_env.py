#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script kiểm tra môi trường phát triển cho dự án Mezon Campus Studio 2026.
Tác giả: Ngô Văn Công
"""

import sys
import shutil
import subprocess
from pathlib import Path

# Fix console encoding on Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass


def check_tool(name: str, cmd: list) -> tuple[bool, str]:
    try:
        res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=False)
        output = (res.stdout or res.stderr).strip().splitlines()
        first_line = output[0] if output else "OK"
        return res.returncode == 0, first_line
    except Exception as e:
        return False, str(e)


def main():
    print("\n" + "=" * 60)
    print("🚀 MEZON CAMPUS STUDIO 2026 — KIỂM TRA MÔI TRƯỜNG PHÁT TRIỂN")
    print("=" * 60)

    tools = [
        ("Git CLI", ["git", "--version"]),
        ("Python 3", [sys.executable, "--version"]),
        ("Node.js", ["node", "-v"]),
        ("NPM", ["npm.cmd" if sys.platform == "win32" else "npm", "-v"]),
        ("Docker (Tùy chọn)", ["docker", "--version"]),
    ]

    all_core_ok = True
    for name, cmd in tools:
        ok, detail = check_tool(name, cmd)
        status_icon = "✅" if ok else ("⚠️" if "Docker" in name else "❌")
        print(f" {status_icon} {name:<20}: {detail}")
        if not ok and "Docker" not in name:
            all_core_ok = False

    # Kiểm tra file cấu hình
    print("-" * 60)
    base_dir = Path(__file__).resolve().parent.parent
    env_file = base_dir / ".env"
    env_example = base_dir / ".env.example"

    if env_file.exists():
        print(" ✅ File .env             : Đã khởi tạo")
    else:
        print(" ℹ️  File .env             : Chưa tạo (Hãy copy từ .env.example)")

    if env_example.exists():
        print(" ✅ File .env.example     : Sẵn sàng")

    print("=" * 60)
    if all_core_ok:
        print("🎉 Môi trường cơ sở đã hoàn toàn sẵn sàng cho Mezon Campus Studio!")
    else:
        print("⚠️ Có một số công cụ cốt lõi chưa đạt, vui lòng kiểm tra lại.")
    print("=" * 60 + "\n")


if __name__ == "__main__":
    main()
