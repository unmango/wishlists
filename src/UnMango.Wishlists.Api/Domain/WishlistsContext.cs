using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class WishlistsContext(DbContextOptions<WishlistsContext> options)
	: DbContext(options)
{
	public required DbSet<User> Users { get; init; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<User>().HasKey(x => x.Id);
	}
}
