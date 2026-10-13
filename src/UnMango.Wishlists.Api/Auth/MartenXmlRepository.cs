using System.Xml.Linq;
using Marten;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace UnMango.Wishlists.Api.Auth;

public sealed record DataProtectionKey(string Id, string Xml);

/// <summary>
/// Keeps ASP.NET data-protection keys in Postgres so restarting the app doesn't sign everyone out.
/// </summary>
internal sealed class MartenXmlRepository(IDocumentStore store) : IXmlRepository
{
	public IReadOnlyCollection<XElement> GetAllElements() {
		using var session = store.QuerySession();
		return session.Query<DataProtectionKey>()
			.ToList()
			.Select(x => XElement.Parse(x.Xml))
			.ToList();
	}

	public void StoreElement(XElement element, string friendlyName) {
		using var session = store.LightweightSession();
		session.Store(new DataProtectionKey(friendlyName, element.ToString(SaveOptions.DisableFormatting)));
		// IXmlRepository is synchronous and Marten only saves asynchronously. Keys are written rarely.
		session.SaveChangesAsync().GetAwaiter().GetResult();
	}
}
