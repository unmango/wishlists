using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace UnMango.Wishlists.Api.Tests;

public sealed class DataProtectionTests(AppFixture app)
{
	[Fact]
	public void Keys_round_trip_through_marten() {
		// Signing a cookie writes a key to Marten and reads the key ring back.
		var protector = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");

		Assert.Equal("hello", protector.Unprotect(protector.Protect("hello")));
	}
}
