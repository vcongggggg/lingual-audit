using Lingual.Modules.Gamification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingual.Modules.Gamification.Infrastructure.Data.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(b => b.Category)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(b => b.Rarity)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(b => b.Icon)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.MaxProgress)
            .IsRequired();

        builder.Property(b => b.XpReward)
            .IsRequired();

        builder.Property(b => b.Lore)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Seed 6 danh hiệu huy hiệu chuẩn của hệ thống
        builder.HasData(
            new Badge
            {
                Id = "b-1",
                Name = "Kỷ Lục Gia Streak",
                Category = "streak",
                Rarity = "legendary",
                Icon = "🔥",
                Description = "Duy trì chuỗi học liên tục trong 14 ngày không đứt quãng.",
                MaxProgress = 14,
                XpReward = 500,
                Lore = "Ngọn lửa kiên trì rực cháy thắp sáng con đường làm chủ ngoại ngữ.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Badge
            {
                Id = "b-2",
                Name = "Kho Báu Từ Vựng",
                Category = "vocabulary",
                Rarity = "epic",
                Icon = "📚",
                Description = "Thành thạo vượt mốc 200 từ vựng A1-A2 theo thuật toán SRS SM-2.",
                MaxProgress = 200,
                XpReward = 350,
                Lore = "Một trí tuệ sắc bén lưu giữ hàng trăm từ ngữ sống động.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Badge
            {
                Id = "b-3",
                Name = "Bất Khả Chiến Bại",
                Category = "speed",
                Rarity = "rare",
                Icon = "⚡",
                Description = "Đạt tỷ lệ ghi nhớ chính xác 100% trong 5 bài kiểm tra liên tiếp.",
                MaxProgress = 5,
                XpReward = 250,
                Lore = "Tốc độ và sự chuẩn xác kết hợp tạo nên phản xạ phi thường.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Badge
            {
                Id = "b-4",
                Name = "Chiến Hữu Mezon",
                Category = "social",
                Rarity = "common",
                Icon = "🛡️",
                Description = "Gia nhập Clan và cùng đồng đội đạt 5,000 XP tuần.",
                MaxProgress = 5000,
                XpReward = 150,
                Lore = "Sức mạnh tập thể nâng bước từng cá nhân vươn lên đỉnh vinh quang.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Badge
            {
                Id = "b-5",
                Name = "Đại Sứ Học Thuật",
                Category = "vocabulary",
                Rarity = "epic",
                Icon = "🎯",
                Description = "Hoàn thành toàn bộ gói từ vựng 500 từ cốt lõi chuẩn Bách Khoa.",
                MaxProgress = 500,
                XpReward = 750,
                Lore = "Mục tiêu tối thượng của bất kỳ học giả ngôn ngữ nào trên Mezon.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Badge
            {
                Id = "b-6",
                Name = "Đấu Vương Thời Gian Thực",
                Category = "speed",
                Rarity = "legendary",
                Icon = "👑",
                Description = "Chiến thắng 20 trận đối kháng 1vs1 không thua.",
                MaxProgress = 20,
                XpReward = 1000,
                Lore = "Huyền thoại đấu trường trí tuệ được tôn kính trong toàn server Mezon.",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
