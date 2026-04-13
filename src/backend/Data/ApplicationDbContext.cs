using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Models;

namespace MGSPlus.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<News> News => Set<News>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Patient");
        });

        // UserProfile — one-to-one
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasOne(p => p.User)
             .WithOne(u => u.Profile)
             .HasForeignKey<UserProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Doctor
        modelBuilder.Entity<Doctor>(e =>
        {
            e.HasIndex(d => d.LicenseNumber).IsUnique();
            e.HasOne(d => d.User)
             .WithMany()
             .HasForeignKey(d => d.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(d => d.ConsultationFee).HasColumnType("decimal(18,2)");
        });

        // Appointment
        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.User)
             .WithMany(u => u.Appointments)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Doctor)
             .WithMany(d => d.Appointments)
             .HasForeignKey(a => a.DoctorId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // BlogPost
        modelBuilder.Entity<BlogPost>(e =>
        {
            e.HasIndex(b => b.Slug).IsUnique();
            e.HasOne(b => b.Author)
             .WithMany(u => u.BlogPosts)
             .HasForeignKey(b => b.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Category)
             .WithMany(c => c.Posts)
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // BlogCategory slug unique
        modelBuilder.Entity<BlogCategory>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        // NewsCategory slug unique
        modelBuilder.Entity<NewsCategory>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        // News
        modelBuilder.Entity<News>(e =>
        {
            e.HasOne(n => n.Category)
             .WithMany(c => c.NewsItems)
             .HasForeignKey(n => n.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ChatSession
        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasOne(s => s.User)
             .WithMany(u => u.ChatSessions)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasOne(m => m.Session)
             .WithMany(s => s.Messages)
             .HasForeignKey(m => m.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MedicalRecord
        modelBuilder.Entity<MedicalRecord>(e =>
        {
            e.HasOne(r => r.User)
             .WithMany(u => u.MedicalRecords)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Doctor)
             .WithMany(d => d.MedicalRecords)
             .HasForeignKey(r => r.DoctorId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogCategory>().HasData(
            new BlogCategory { Id = 1, Name = "Sức khỏe tổng quát", Slug = "suc-khoe-tong-quat", CreatedAt = new DateTime(2024, 1, 1) },
            new BlogCategory { Id = 2, Name = "Dinh dưỡng", Slug = "dinh-duong", CreatedAt = new DateTime(2024, 1, 1) },
            new BlogCategory { Id = 3, Name = "Bảo hiểm y tế", Slug = "bao-hiem-y-te", CreatedAt = new DateTime(2024, 1, 1) },
            new BlogCategory { Id = 4, Name = "Pháp luật y tế", Slug = "phap-luat-y-te", CreatedAt = new DateTime(2024, 1, 1) }
        );

        modelBuilder.Entity<NewsCategory>().HasData(
            new NewsCategory { Id = 1, Name = "Tin tức y tế", Slug = "tin-tuc-y-te", CreatedAt = new DateTime(2024, 1, 1) },
            new NewsCategory { Id = 2, Name = "Cảnh báo dịch bệnh", Slug = "canh-bao-dich-benh", CreatedAt = new DateTime(2024, 1, 1) },
            new NewsCategory { Id = 3, Name = "Chính sách y tế", Slug = "chinh-sach-y-te", CreatedAt = new DateTime(2024, 1, 1) }
        );
    }
}
