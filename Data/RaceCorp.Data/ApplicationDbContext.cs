﻿namespace RaceCorp.Data
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using RaceCorp.Data.Common.Models;
    using RaceCorp.Data.Models;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        private static readonly MethodInfo SetIsDeletedQueryFilterMethod =
            typeof(ApplicationDbContext).GetMethod(
                nameof(SetIsDeletedQueryFilter),
                BindingFlags.NonPublic | BindingFlags.Static);

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Image> Images { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<Gpx> Gpxs { get; set; }

        public DbSet<Difficulty> Difficulties { get; set; }

        public DbSet<Ride> Rides { get; set; }

        public DbSet<Race> Races { get; set; }

        public DbSet<Trace> Traces { get; set; }

        public DbSet<Format> Formats { get; set; }

        public DbSet<Town> Towns { get; set; }

        public DbSet<Mountain> Mountains { get; set; }

        public DbSet<Logo> Logos { get; set; }

        public DbSet<Setting> Settings { get; set; }

        public override int SaveChanges() => this.SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.ApplyAuditInfoRules();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            this.SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            this.ApplyAuditInfoRules();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Needed for Identity models configuration
            base.OnModelCreating(builder);

            this.ConfigureUserIdentityRelations(builder);

            EntityIndexesConfiguration.Configure(builder);

            var entityTypes = builder.Model.GetEntityTypes().ToList();

            // Set global query filter for not deleted entities only
            var deletableEntityTypes = entityTypes
                .Where(et => et.ClrType != null && typeof(IDeletableEntity).IsAssignableFrom(et.ClrType));
            foreach (var deletableEntityType in deletableEntityTypes)
            {
                var method = SetIsDeletedQueryFilterMethod.MakeGenericMethod(deletableEntityType.ClrType);
                method.Invoke(null, new object[] { builder });
            }

            // Disable cascade delete
            var foreignKeys = entityTypes
                .SelectMany(e => e.GetForeignKeys().Where(f => f.DeleteBehavior == DeleteBehavior.Cascade));
            foreach (var foreignKey in foreignKeys)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            builder.Entity<Team>()
                .HasMany(t => t.TeamMembers)
                .WithOne(u => u.MemberInTeam)
                .HasForeignKey(u => u.MemberInTeamId);

            builder.Entity<Team>()
                .HasOne(t => t.Creator)
                .WithOne(u => u.Team)
                .HasForeignKey<Team>(t => t.CreatorId);

            builder.Entity<Race>()
                .HasOne(r => r.Logo)
                .WithOne(l => l.Race)
                .HasForeignKey<Logo>(l => l.RaceId);

            builder.Entity<Logo>()
                .HasOne(l => l.Race)
                .WithOne(r => r.Logo)
                .HasForeignKey<Race>(r => r.LogoId);

            builder.Entity<Ride>()
                            .HasOne(l => l.Trace)
                            .WithOne(r => r.Ride)
                            .HasForeignKey<Trace>(r => r.RideId);

            builder.Entity<Trace>()
                            .HasOne(l => l.Ride)
                            .WithOne(r => r.Trace)
                            .HasForeignKey<Ride>(r => r.TraceId);

            builder.Entity<Trace>()
                .HasOne(t => t.Gpx)
                .WithOne(g => g.Trace)
                .HasForeignKey<Gpx>(g => g.TraceId);

            builder.Entity<Gpx>()
                .HasOne(g => g.Trace)
                .WithOne(t => t.Gpx)
                .HasForeignKey<Trace>(t => t.GpxId);

            builder.Entity<Town>()
                .HasMany(t => t.Users)
                .WithOne(u => u.Town)
                .HasForeignKey(u => u.TownId);
        }

        private static void SetIsDeletedQueryFilter<T>(ModelBuilder builder)
            where T : class, IDeletableEntity
        {
            builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }

        // Applies configurations
        private void ConfigureUserIdentityRelations(ModelBuilder builder)
             => builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        private void ApplyAuditInfoRules()
        {
            var changedEntries = this.ChangeTracker
                .Entries()
                .Where(e =>
                    e.Entity is IAuditInfo &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in changedEntries)
            {
                var entity = (IAuditInfo)entry.Entity;
                if (entry.State == EntityState.Added && entity.CreatedOn == default)
                {
                    entity.CreatedOn = DateTime.UtcNow;
                }
                else
                {
                    entity.ModifiedOn = DateTime.UtcNow;
                }
            }
        }
    }
}
