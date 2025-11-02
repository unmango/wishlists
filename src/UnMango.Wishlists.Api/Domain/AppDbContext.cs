using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
	}

	public static void Configure(IServiceProvider services, DbContextOptionsBuilder options) {
		var configuration = services.GetRequiredService<IConfiguration>();
		options.UseNpgsql(
			configuration.GetConnectionString("Wishlists"),
			x => x.SetPostgresVersion(18, 0));
	}
}
