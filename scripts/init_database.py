#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script khởi tạo CSDL PostgreSQL cho dự án Lingual (MCS 2026).
Tự động chạy file '00_init_all.sql' hoặc 8 module SQL theo đúng thứ tự.
"""

import os
import sys
from pathlib import Path

# Đọc thông số kết nối từ biến môi trường hoặc cấu hình mặc định
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "lingual_db")
DB_USER = os.getenv("DB_USER", "lingual_user")
DB_PASSWORD = os.getenv("DB_PASSWORD", "lingual_password")

def main():
    base_dir = Path(__file__).resolve().parent.parent
    sql_path = base_dir / "docs" / "database" / "sql" / "00_init_all.sql"

    if not sql_path.exists():
        print(f"❌ Không tìm thấy file SQL: {sql_path}")
        sys.exit(1)

    print("=" * 65)
    print("🐘 LINGUAL DATABASE INITIALIZER (POSTGRESQL 13+)")
    print("=" * 65)
    print(f"Host: {DB_HOST}:{DB_PORT} | Database: {DB_NAME} | User: {DB_USER}")
    print(f"File SQL: {sql_path.name} ({sql_path.stat().st_size} bytes)")
    print("-" * 65)

    try:
        import psycopg2
    except ImportError:
        print("⚠️ Gợi ý: Chưa cài đặt psycopg2. Bạn có thể cài đặt bằng lệnh:")
        print("   pip install psycopg2-binary")
        print("\nHoặc chạy trực tiếp qua công cụ dòng lệnh psql:")
        print(f'   psql -h {DB_HOST} -p {DB_PORT} -U {DB_USER} -d {DB_NAME} -f "{sql_path}"')
        sys.exit(0)

    try:
        print("Đang kết nối tới PostgreSQL...")
        conn = psycopg2.connect(
            host=DB_HOST,
            port=DB_PORT,
            dbname=DB_NAME,
            user=DB_USER,
            password=DB_PASSWORD
        )
        conn.autocommit = True
        cursor = conn.cursor()

        print("Đang thực thi script CSDL (9 modules · 22 bảng)...")
        with open(sql_path, "r", encoding="utf-8") as f:
            sql_content = f.read()

        cursor.execute(sql_content)

        # Đếm số bảng đã tạo trong schema public
        cursor.execute("""
            SELECT count(*) 
            FROM information_schema.tables 
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
        """)
        table_count = cursor.fetchone()[0]

        print(f"✅ THÀNH CÔNG: Đã khởi tạo hoàn tất CSDL! Hiện có {table_count} bảng trong schema 'public'.")
        cursor.close()
        conn.close()

    except Exception as e:
        print(f"❌ LỖI kết nối hoặc thực thi CSDL: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
