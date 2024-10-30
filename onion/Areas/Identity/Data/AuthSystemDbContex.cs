using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using onion.Areas.Identity.Data;
using onion.Models;

namespace onion.Areas.Identity.Data
{
    public class AuthSystemDbContex : IdentityDbContext<AppUser>
    {
        public AuthSystemDbContex(DbContextOptions<AuthSystemDbContex> options)
            : base(options)
        {
        }

        public DbSet<SearchRecord> SearchRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // You can customize the ASP.NET Identity model here if needed.
        }
    }
}
