using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
	public DbSet<Wishlist> Wishlists { get; init; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		var wishlist = modelBuilder.Entity<Wishlist>();
		wishlist.HasKey(x => x.Id);
		wishlist.Property(x => x.Name).IsRequired();
		wishlist.HasOne(x => x.Owner);
	}

	public static void Configure(IServiceProvider services, DbContextOptionsBuilder options) {
		var configuration = services.GetRequiredService<IConfiguration>();
		options.UseNpgsql(
			configuration.GetConnectionString("Wishlists"),
			x => x.SetPostgresVersion(18, 0));
	}
}
