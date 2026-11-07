using System.Net;
using System.Net.Sockets;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>Decides whether an outbound connection may go to an address: only the public internet is allowed.</summary>
public static class PublicAddress
{
	private static readonly IPNetwork[] BlockedV4 = [
		IPNetwork.Parse("0.0.0.0/8"), // "this" network
		IPNetwork.Parse("10.0.0.0/8"), // private, and the cluster's pod and service networks
		IPNetwork.Parse("100.64.0.0/10"), // CGNAT
		IPNetwork.Parse("127.0.0.0/8"), // loopback
		IPNetwork.Parse("169.254.0.0/16"), // link-local, including cloud metadata
		IPNetwork.Parse("172.16.0.0/12"), // private
		IPNetwork.Parse("192.0.0.0/24"), // IETF protocol assignments
		IPNetwork.Parse("192.0.2.0/24"), // documentation
		IPNetwork.Parse("192.88.99.0/24"), // 6to4 relay
		IPNetwork.Parse("192.168.0.0/16"), // private
		IPNetwork.Parse("198.18.0.0/15"), // benchmarking
		IPNetwork.Parse("198.51.100.0/24"), // documentation
		IPNetwork.Parse("203.0.113.0/24"), // documentation
		IPNetwork.Parse("224.0.0.0/4"), // multicast
		IPNetwork.Parse("240.0.0.0/4"), // reserved and broadcast
	];

	private static readonly IPNetwork[] BlockedV6 = [
		IPNetwork.Parse("::/96"), // unspecified, loopback and IPv4-compatible
		IPNetwork.Parse("100::/64"), // discard
		IPNetwork.Parse("2001::/32"), // Teredo
		IPNetwork.Parse("2001:db8::/32"), // documentation
		IPNetwork.Parse("2002::/16"), // 6to4
		IPNetwork.Parse("fc00::/7"), // unique local
		IPNetwork.Parse("fe80::/10"), // link-local
		IPNetwork.Parse("fec0::/10"), // site-local
		IPNetwork.Parse("ff00::/8"), // multicast
	];

	private static readonly IPNetwork Nat64 = IPNetwork.Parse("64:ff9b::/96");

	public static bool IsPublic(IPAddress address) {
		if (address.IsIPv4MappedToIPv6) return IsPublic(address.MapToIPv4());

		if (address.AddressFamily == AddressFamily.InterNetwork) {
			return !BlockedV4.Any(x => x.Contains(address));
		}

		if (address.AddressFamily != AddressFamily.InterNetworkV6) return false;

		// NAT64 addresses embed an IPv4 address in their last four bytes.
		if (Nat64.Contains(address)) return IsPublic(new IPAddress(address.GetAddressBytes()[12..]));

		return !BlockedV6.Any(x => x.Contains(address));
	}
}
