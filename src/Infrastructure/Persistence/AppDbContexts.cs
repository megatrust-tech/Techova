using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using taskedin_be.src.Modules.Users.Entities;
using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {

        // Tables
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Global soft delete query filter for all BaseEntity types
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, "DeletedAt");
                    var nullConstant = Expression.Constant(null, typeof(DateTime?));
                    var equality = Expression.Equal(property, nullConstant);
                    var lambda = Expression.Lambda(equality, parameter);
                    
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(lambda);
                }
            }
        }
    }
}
