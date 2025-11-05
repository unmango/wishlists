using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Extensions;

internal static class Host {
	extension(IHost host) {
		public void Migrate<T>() where T : DbContext {
			using var scope = host.Services.CreateScope();
			Migrate(
				scope.GetRequiredService<ILogger<T>>(),
				scope.ServiceProvider.GetRequiredService<T>());
		}
	}

	private static void Migrate<T>(ILogger logger, T context) where T : DbContext {
		logger.LogInformation("Migrating database");
		context.Database.Migrate();
	}
}
