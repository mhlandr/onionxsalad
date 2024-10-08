using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using onion.Areas.Identity.Data;

namespace onion.Areas.Identity.Data;

public class AuthSystemDbContex : IdentityDbContext<AppUser>
{
    public AuthSystemDbContex(DbContextOptions<AuthSystemDbContex> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
