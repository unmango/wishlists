using System.Net;
using System.Net.Sockets;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>
/// The HTTP handler for every request the server makes to an item's link. It checks the resolved
/// address when it connects, so redirects and DNS tricks can't reach the cluster or the host.
/// </summary>
public static class GuardedHandler
{
	public static SocketsHttpHandler Create() => new() {
		// ThumbnailClient follows redirects itself so it can check and count each one.
		AllowAutoRedirect = false,
		AutomaticDecompression = DecompressionMethods.All,
		ConnectTimeout = TimeSpan.FromSeconds(5),
		PooledConnectionLifetime = TimeSpan.FromMinutes(2),
		// A proxy would make the connection for us, out of reach of the address check.
		UseProxy = false,
		ConnectCallback = ConnectAsync,
	};

	private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {
		var host = context.DnsEndPoint.Host.Trim('[', ']');
		var addresses = IPAddress.TryParse(host, out var literal)
			? [literal]
			: await Dns.GetHostAddressesAsync(host, cancellationToken);

		// Refuse the host outright if any address it resolves to is off limits.
		if (addresses.Length == 0 || !addresses.All(PublicAddress.IsPublic)) {
			throw new HttpRequestException($"Refusing to connect to {host}: it isn't a public address.");
		}

		var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		try {
			await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
			return new NetworkStream(socket, ownsSocket: true);
		} catch {
			socket.Dispose();
			throw;
		}
	}
}
