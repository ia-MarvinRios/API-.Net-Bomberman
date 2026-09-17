using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace API.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // -- Sets --
        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<PlayerSettings> PlayerSettings { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PlayerStats> PlayerStats { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchPlayer> MatchPlayers { get; set; }
        public DbSet<Skin> Skins { get; set; }
        public DbSet<GameConfig> GameConfig { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // -- User --
            modelBuilder.Entity<User>(entity => 
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.Property(u => u.Username).HasMaxLength(16);
                entity.Property(u => u.PasswordHash).HasMaxLength(255);
            });

            // -- Profile --
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(p => p.UserId);
                entity.HasOne(p => p.User)
                      .WithOne(u => u.Profile)
                      .HasForeignKey<Profile>(p => p.UserId);

                entity.Property(p => p.UnlockedSkinIds)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                          v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                      )
                      .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                          (a, b) => a!.SequenceEqual(b!),
                          v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                          v => v.ToList()       
                      ));
            });

            // -- PlayerSettings --
            modelBuilder.Entity<PlayerSettings>(entity =>
            {
                entity.HasKey(s => s.UserId);
                entity.HasOne(s => s.User)
                      .WithOne(u => u.PlayerSettings)
                      .HasForeignKey<PlayerSettings>(s => s.UserId);
            });

            // -- PlayerStats --
            modelBuilder.Entity<PlayerStats>(entity =>
            {
                entity.HasKey(s => s.UserId);
                entity.HasOne(s => s.User)
                      .WithOne(u => u.PlayerStats)
                      .HasForeignKey<PlayerStats>(s => s.UserId);
            });

            // -- RefreshToken --
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId);
            });

            // -- Match --
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(m => m.MatchId); // Client generates this, no AI (Auto Incremental)
            });

            // -- MatchPlayer --
            modelBuilder.Entity<MatchPlayer>(entity =>
            {
                entity.HasOne(mp => mp.Match)
                      .WithMany(m => m.Players)
                      .HasForeignKey(mp => mp.MatchId);
            });

            // -- GameConfig --
            modelBuilder.Entity<GameConfig>(entity =>
            {
                entity.Property(g => g.EnabledMapIds)
                      .HasConversion(
                          v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                          v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
                      )
                      .Metadata.SetValueComparer(new ValueComparer<List<string>?>(
                          (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                          v => v == null ? 0 : v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                          v => v == null ? null : v.ToList()
                      ));
            });

            // --- Seed: game init config (singleton row) ---
            modelBuilder.Entity<GameConfig>().HasData(new GameConfig
            {
                Id = 1,
                ConfigVersion = 1,
                EnabledMapIds = null, // null = all maps unlocked
                MinClientVersion = "0.1.0",
                BalanceFuseSeconds = 2.5f,
                BalanceBaseSpeed = 4f,
                BalanceBaseRange = 1,
                BalanceBaseMaxBombs = 1,
                BalanceMatchDurationSeconds = 180,
                BalanceWallCooldownSeconds = 8f,
                BalanceWallDurationSeconds = 4f,
                BalanceBombRequestTimeoutSeconds = 2f,
                BalanceTimedFuseSeconds = 5f,
                BalanceRemoteMaxSeconds = 15f,
                BalanceThrowDistanceCells = 3,
                BalanceThrowDurationSeconds = 0.5f,
                BalanceMaxRange = 8,
                BalanceMaxBombs = 6,
                BalanceMaxSpeedLevel = 4
            });

            // --- Seed: available skins ---
            modelBuilder.Entity<Skin>().HasData(
                new Skin { Id = "default", DisplayName = "Default", EnabledInSelector = true, SortOrder = 0 },
                new Skin { Id = "skin_01", DisplayName = "Skin 01", EnabledInSelector = true, SortOrder = 1 },
                new Skin { Id = "skin_02", DisplayName = "Skin 02", EnabledInSelector = true, SortOrder = 2 },
                new Skin { Id = "skin_03", DisplayName = "Skin 03", EnabledInSelector = true, SortOrder = 3 }
            );

        }

    }
}
