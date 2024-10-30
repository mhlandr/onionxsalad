using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using onion.Areas.Identity.Data;
using onion.Models;

namespace onion.Areas.Identity.Data
{
    public class AuthSystemDbContext : IdentityDbContext<AppUser>
    {
        public AuthSystemDbContext(DbContextOptions<AuthSystemDbContext> options)
            : base(options)
        {
        }

        public DbSet<SearchRecord> SearchRecords { get; set; }
        public DbSet<ScreenshotRequestLog> ScreenshotRequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Additional configurations if needed
        }
    }
}