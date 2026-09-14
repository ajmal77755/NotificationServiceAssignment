using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.Persistence
{
    public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
    {
        public DbSet<Domain.Notification> Notifications => Set<Domain.Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        }
    }
}
