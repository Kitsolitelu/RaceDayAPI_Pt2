using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<RouteInfo> RouteInfo { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue("Participant");
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.EventDate).HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            
                entity.HasOne(e => e.Organiser)
                      .WithMany(u => u.Events)
                      .HasForeignKey(e => e.OrganiserId)
                      .OnDelete(DeleteBehavior.Restrict);

               
                entity.HasOne(e => e.RouteInfo)
                      .WithOne(r => r.Event)
                      .HasForeignKey<RouteInfo>(r => r.EventId);
            });

           
            modelBuilder.Entity<RouteInfo>(entity =>
            {
                entity.Property(r => r.Distance).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Price).HasColumnType("decimal(8,2)").HasDefaultValue(0m);

                entity.HasOne(c => c.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(c => c.EventId);
            });

         
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("Confirmed");
                entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");

              
                entity.HasIndex(e => new { e.CategoryId, e.ParticipantId }).IsUnique();

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(e => e.CategoryId);

                entity.HasOne(e => e.Participant)
                      .WithMany(u => u.Enrolments)
                      .HasForeignKey(e => e.ParticipantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            
            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasOne(r => r.Enrolment)
                      .WithOne(e => e.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentId);
            });
        }
    }
}
