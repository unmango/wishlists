using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
	: IdentityDbContext<User, IdentityRole<Guid>, Guid>(options);
