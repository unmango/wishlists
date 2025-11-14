using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
	public const string ConnectionStringName = "App";

	public DbSet<Wishlist> Wishlists { get; init; }

	public DbSet<Item> Items { get; init; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Wishlist>(wishlist => {
			wishlist.HasKey(x => x.Id);
			wishlist.Property(x => x.Name).IsRequired();
			wishlist.HasOne(x => x.Owner);
			wishlist.HasMany(x => x.Items).WithOne(x => x.Wishlist);
		});

		modelBuilder.Entity<Item>(item => {
			item.HasKey(x => x.Id);
			item.Property(x => x.Name).IsRequired();
			item.Property(x => x.Note);
			item.Property(x => x.Purchased).IsRequired().HasDefaultValue(false);
		});
	}

	public static void Configure(IServiceProvider services, DbContextOptionsBuilder options) {
		var configuration = services.GetRequiredService<IConfiguration>();
		options.UseNpgsql(
			configuration.GetConnectionString(ConnectionStringName),
			x => x.SetPostgresVersion(18, 0));
	}
}
