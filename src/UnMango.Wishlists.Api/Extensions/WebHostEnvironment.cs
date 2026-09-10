using System.Reflection;

namespace UnMango.Wishlists.Api.Extensions;

internal static class WebHostEnvironment
{
	extension(IWebHostEnvironment environment)
	{
		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cvisual-studio-code#customizing-run-time-behavior-during-build-time-document-generation
		public bool IsOpenApiCodegen() => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
	}
}
