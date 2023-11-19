using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Queries;

public partial class Query
{
	public IQueryable<Wishlist> ListWishlists()
	{
		return new List<Wishlist> { new Wishlist(new List<string>() { "Thing 1", "Thing 2" }) }.AsQueryable();
	}
}
