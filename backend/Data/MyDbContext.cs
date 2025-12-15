using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<UserData> user_data { get; set; }
    public DbSet<GameData> game_data { get; set; }
    public DbSet<PendingInteraction> pending_interactions { get; set; }
    public DbSet<GameSession> game_session { get; set; }
    public DbSet<BotStrategy> bot_strat { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Optional: keep table names exactly like your SQL Server
        modelBuilder.Entity<UserData>().ToTable("user_data");
        modelBuilder.Entity<GameData>().ToTable("game_data");
        modelBuilder.Entity<PendingInteraction>().ToTable("pending_interactions");
        modelBuilder.Entity<GameSession>().ToTable("game_session");
        modelBuilder.Entity<BotStrategy>().ToTable("bot_strat");

        modelBuilder.Entity<UserData>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.UserId).HasColumnName("user_id");
            entity.Property(u => u.UserName).HasColumnName("user_name");
            entity.Property(u => u.Password).HasColumnName("password");
            entity.Property(u => u.MaxTurns)
                  .HasColumnName("nr_turns");
            entity.Property(u => u.GameNr).HasColumnName("game_nr");
            entity.Property(u => u.Role).HasColumnName("role");

            entity.HasOne(u => u.GameData)
                .WithOne(gm => gm.UserData)
                .HasForeignKey<GameData>(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.BotStrategy)
                .WithOne(b => b.UserData)
                .HasForeignKey<BotStrategy>(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameData>(entity =>
        {
            entity.HasKey(gm => gm.GDataId);

            entity.Property(gm => gm.GDataId)
                  .HasColumnName("gm_id")
                  .ValueGeneratedOnAdd();

            entity.Property(gm => gm.UserId).HasColumnName("user_id");
            entity.Property(gm => gm.MoneyPoints).HasColumnName("m_points");
            entity.Property(gm => gm.CoopCoop).HasColumnName("coopcoop");
            entity.Property(gm => gm.CoopDeflect).HasColumnName("coopdeflect");
            entity.Property(gm => gm.DeflectCoop).HasColumnName("deflectcoop");
            entity.Property(gm => gm.DeflectDeflect).HasColumnName("deflectdeflect");

            // 1-1: unique user_id
            entity.HasIndex(gm => gm.UserId).IsUnique();
        });

        modelBuilder.Entity<BotStrategy>(entity =>
        {
            entity.HasKey(b => b.BotId);

            entity.Property(b => b.BotId)
                  .HasColumnName("bot_id")
                  .ValueGeneratedOnAdd();

            entity.Property(b => b.UserId).HasColumnName("user_id");
            entity.Property(b => b.Start).HasColumnName("start");
            entity.Property(b => b.Strategy).HasColumnName("strategy");
            entity.Property(b => b.MoneyLimit).HasColumnName("money");

            // 1-1: unique user_id
            entity.HasIndex(b => b.UserId).IsUnique();
        });

        modelBuilder.Entity<PendingInteraction>(entity =>
        {
            entity.HasKey(p => p.PendingId);

            entity.Property(p => p.PendingId)
                  .HasColumnName("pending_id")
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.UserId).HasColumnName("user_id");
            entity.Property(p => p.TargetId).HasColumnName("target_id");

            entity.Property(p => p.UserChoice)
                  .HasColumnName("user_choice")
                  .HasConversion<string>();

            entity.Property(p => p.TargetChoice)
                  .HasColumnName("target_choice")
                  .HasConversion<string>();

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Target)
                .WithMany()
                .HasForeignKey(p => p.TargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(gs => gs.ID);

            entity.Property(gs => gs.ID)
                  .HasColumnName("ID")
                  .ValueGeneratedOnAdd();

            entity.Property(gs => gs.User1).HasColumnName("user1_id");
            entity.Property(gs => gs.Choice1).HasColumnName("choice_type");
            entity.Property(gs => gs.GameNr1).HasColumnName("game_nr");
            entity.Property(gs => gs.MoneyPoints1).HasColumnName("m_points");
            entity.Property(gs => gs.CoopCoop1).HasColumnName("coopcoop");
            entity.Property(gs => gs.CoopDeflect1).HasColumnName("coopdeflect");
            entity.Property(gs => gs.DeflectCoop1).HasColumnName("deflectcoop");
            entity.Property(gs => gs.DeflectDeflect1).HasColumnName("deflectdeflect");

            entity.Property(gs => gs.User2).HasColumnName("user2_id");
            entity.Property(gs => gs.Choice2).HasColumnName("choice_type2");
            entity.Property(gs => gs.GameNr2).HasColumnName("game2_nr");
            entity.Property(gs => gs.MoneyPoints2).HasColumnName("m_points2");
            entity.Property(gs => gs.CoopCoop2).HasColumnName("coopcoop2");
            entity.Property(gs => gs.CoopDeflect2).HasColumnName("coopdeflect2");
            entity.Property(gs => gs.DeflectCoop2).HasColumnName("deflectcoop2");
            entity.Property(gs => gs.DeflectDeflect2).HasColumnName("deflectdeflect2");

            entity.HasOne(gs => gs.User1Nav)
                .WithMany()
                .HasForeignKey(gs => gs.User1)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(gs => gs.User2Nav)
                .WithMany()
                .HasForeignKey(gs => gs.User2)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
