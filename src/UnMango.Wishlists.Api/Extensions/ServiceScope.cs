namespace UnMango.Wishlists.Api.Extensions;

internal static class ServiceScope
{
	extension(IServiceScope scope)
	{
		public T GetRequiredService<T>() where T : class => scope.ServiceProvider.GetRequiredService<T>();
	}
}
