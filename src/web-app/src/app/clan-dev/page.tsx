"use client";

import Link from "next/link";
import { useAuth } from "@/components/auth/useAuth";
import { ArrowLeft, Clock3, LockKeyhole, Trophy } from "lucide-react";

const activity = [
  { name: "Minh Anh", xp: 420, activity: "Hoàn thành 8 bài học" },
  { name: "Quang Huy", xp: 385, activity: "Ôn tập 24 từ vựng" },
  { name: "Lan Phương", xp: 340, activity: "Hoàn thành 6 bài học" },
  { name: "Tuấn Minh", xp: 295, activity: "Luyện 2 bài Quiz" },
  { name: "Khánh Linh", xp: 260, activity: "Ôn tập 18 từ vựng" },
];

export default function ClanDevPage() {
  const { user, loading } = useAuth();
  const isModerator = user?.role === "clan_moderator";
  return <main className="clan-dev-page"><header className="clan-dev-heading"><div><p className="clan-dev-eyebrow">CLAN DEV / BẢN THỬ NGHIỆM</p><h1>Hoạt động Clan</h1><p>Không gian xem trước dành cho Clan Moderator.</p></div><Link href="/" className="clan-dev-back"><ArrowLeft size={16}/> Trở về</Link></header>
    {loading ? <section className="clan-dev-state" role="status">Đang kiểm tra phiên đăng nhập…</section> : !isModerator ? <section className="clan-dev-state"><LockKeyhole size={24}/><h2>Chưa xác minh quyền Clan Moderator</h2><p>Phiên đăng nhập hiện tại chưa có quyền Clan Moderator được máy chủ xác nhận. Bản thử nghiệm không tự cấp quyền.</p></section> : <>
      <div className="clan-dev-demo"><strong>Chế độ xem demo</strong><span>API chưa cung cấp dữ liệu hoạt động, grant theo Clan hoặc thao tác phiên cộng đồng.</span></div>
      <section className="clan-dev-summary"><div><span>Clan hiện tại</span><strong>Clan đã cấu hình</strong><small>Không hỗ trợ đổi Clan trong bản thử nghiệm</small></div><div><span>Kênh</span><strong>Chưa có dữ liệu</strong><small>Tên kênh cần API tích hợp xác nhận</small></div><div><span>Phiên cộng đồng</span><strong><Clock3 size={16}/> Chưa có phiên thật</strong><small>Không thể mở hoặc đóng phiên từ giao diện này</small></div></section>
      <section className="clan-dev-panel"><div className="clan-dev-panel-title"><div><p className="clan-dev-eyebrow">TỔNG HỢP MINH HỌA</p><h2>Top học viên tuần này</h2></div><span><Trophy size={15}/> Top 10 trong Clan</span></div><p className="clan-dev-demo-note">Dữ liệu mẫu cục bộ, không phản ánh thành viên hoặc thứ hạng thật.</p><ol className="clan-dev-list">{activity.map((entry, index) => <li key={entry.name}><strong>{index + 1}</strong><div><b>{entry.name}</b><small>{entry.activity}</small></div><span>{entry.xp} XP</span></li>)}</ol></section>
      <section className="clan-dev-panel clan-dev-quiz"><div><p className="clan-dev-eyebrow">QUIZ CỘNG ĐỒNG</p><h2>Phiên và câu hỏi đã phát hành</h2><p>Danh sách quiz phát hành và điều khiển mở, kết thúc hoặc hủy phiên chưa có API hỗ trợ. Đáp án và giải thích chỉ được công bố sau khi phiên kết thúc.</p></div><button disabled title="Chưa có API hỗ trợ">Chưa khả dụng</button></section>
      <p className="clan-dev-footnote">Bản thử nghiệm chỉ minh họa bố cục. Không có thao tác nào được ghi lên máy chủ.</p>
    </>}
  </main>;
}
