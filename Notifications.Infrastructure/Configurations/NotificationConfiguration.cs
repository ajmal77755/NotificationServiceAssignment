using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Domain.Notification> builder)
        {
            builder.ToTable("Notifications", "NS"); //NS schema notification sender
            
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Payload).IsRequired();

            builder.Property(n => n.Level).HasConversion<string>().IsRequired();
            builder.Property(n => n.Status).HasConversion<string>().IsRequired();
        
            builder.HasIndex(n=> new { n.Status, n.ReceivedAt });
            builder.HasIndex(n=> n.SentAt);
        }
  
    }
}
