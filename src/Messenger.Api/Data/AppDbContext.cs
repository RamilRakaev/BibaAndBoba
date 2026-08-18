using Messenger.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Role).IsRequired().HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<ChatMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.JoinedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(x => x.ChatId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.ChatId, x.UserId }).IsUnique();

            entity.HasOne(x => x.Chat)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ChatMembers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired().HasMaxLength(4000);
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.EditedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(x => x.ChatId);
            entity.HasIndex(x => x.SenderUserId);
            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.Chat)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Sender)
                .WithMany(x => x.SentMessages)
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
