using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using taskedin_be.src.Modules.Users.Entities;

namespace taskedin_be.src.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            // Configure Id as auto-increment primary key
            builder.HasKey(r => r.Id);
        
            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();
        }
    }

}
