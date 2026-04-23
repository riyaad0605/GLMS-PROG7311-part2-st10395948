using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GLMS.Web.Models;

namespace GLMS.Web.Data
{
    public class GlmsDbContext : IdentityDbContext<IdentityUser>
    {
        public GlmsDbContext(DbContextOptions<GlmsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(c => c.ClientId);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.ContactDetails).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Region).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(c => c.ContractId);
                entity.Property(c => c.ServiceLevel).IsRequired().HasMaxLength(50);
                entity.HasOne(c => c.Client)
                      .WithMany(cl => cl.Contracts)
                      .HasForeignKey(c => c.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(sr => sr.ServiceRequestId);
                entity.Property(sr => sr.Description).IsRequired().HasMaxLength(500);
                entity.Property(sr => sr.Status).IsRequired().HasMaxLength(50);
                entity.HasOne(sr => sr.Contract)
                      .WithMany(c => c.ServiceRequests)
                      .HasForeignKey(sr => sr.ContractId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed roles
            var adminRoleId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
            var managerRoleId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = adminRoleId },
                new IdentityRole { Id = managerRoleId, Name = "Manager", NormalizedName = "MANAGER", ConcurrencyStamp = managerRoleId }
            );

            // Seed default Admin user  (password: Admin@123!)
            var adminUserId = "c3d4e5f6-a7b8-9012-cdef-123456789012";
            var hasher = new PasswordHasher<IdentityUser>();
            var adminUser = new IdentityUser
            {
                Id = adminUserId,
                UserName = "admin@glms.com",
                NormalizedUserName = "ADMIN@GLMS.COM",
                Email = "admin@glms.com",
                NormalizedEmail = "ADMIN@GLMS.COM",
                EmailConfirmed = true,
                SecurityStamp = adminUserId,
                ConcurrencyStamp = adminUserId
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123!");
            modelBuilder.Entity<IdentityUser>().HasData(adminUser);

            // Seed default Manager user  (password: Manager@123!)
            var managerId = "d4e5f6a7-b8c9-0123-defa-234567890123";
            var managerUser = new IdentityUser
            {
                Id = managerId,
                UserName = "manager@glms.com",
                NormalizedUserName = "MANAGER@GLMS.COM",
                Email = "manager@glms.com",
                NormalizedEmail = "MANAGER@GLMS.COM",
                EmailConfirmed = true,
                SecurityStamp = managerId,
                ConcurrencyStamp = managerId
            };
            managerUser.PasswordHash = hasher.HashPassword(managerUser, "Manager@123!");
            modelBuilder.Entity<IdentityUser>().HasData(managerUser);

            // Assign roles to seed users
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = adminUserId, RoleId = adminRoleId },
                new IdentityUserRole<string> { UserId = managerId, RoleId = managerRoleId }
            );

            // Seed business data
            modelBuilder.Entity<Client>().HasData(
                new Client { ClientId = 1, Name = "Oceanic Freight Ltd", ContactDetails = "info@oceanicfreight.com | +27 11 234 5678", Region = "Southern Africa" },
                new Client { ClientId = 2, Name = "Nordic Cargo Solutions", ContactDetails = "ops@nordiccargo.eu | +46 8 123 4567", Region = "Northern Europe" },
                new Client { ClientId = 3, Name = "Pacific Trade Group", ContactDetails = "hello@pacifictrade.com.au | +61 2 9876 5432", Region = "Asia-Pacific" }
            );

            modelBuilder.Entity<Contract>().HasData(
                new Contract { ContractId = 1, ClientId = 1, StartDate = new DateTime(2024, 1, 15), EndDate = new DateTime(2025, 1, 14), Status = ContractStatus.Active, ServiceLevel = "Premium" },
                new Contract { ContractId = 2, ClientId = 2, StartDate = new DateTime(2023, 6, 1), EndDate = new DateTime(2024, 5, 31), Status = ContractStatus.Expired, ServiceLevel = "Standard" },
                new Contract { ContractId = 3, ClientId = 3, StartDate = new DateTime(2024, 3, 1), EndDate = new DateTime(2025, 2, 28), Status = ContractStatus.Draft, ServiceLevel = "Basic" }
            );
        }
    }
}
