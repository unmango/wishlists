using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
