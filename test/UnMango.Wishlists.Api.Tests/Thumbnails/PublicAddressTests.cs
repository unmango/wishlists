using System.Net;
using UnMango.Wishlists.Api.Thumbnails;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

public sealed class PublicAddressTests
{
	[Theory]
	[InlineData("127.0.0.1")] // loopback
	[InlineData("10.1.2.3")] // private (also the cluster's pod network)
	[InlineData("10.43.0.1")] // cluster services
	[InlineData("172.16.0.1")]
	[InlineData("192.168.1.10")]
	[InlineData("169.254.169.254")] // link-local, cloud metadata
	[InlineData("100.64.0.1")] // CGNAT
	[InlineData("0.0.0.0")]
	[InlineData("224.0.0.1")] // multicast
	[InlineData("255.255.255.255")]
	[InlineData("::1")]
	[InlineData("::")]
	[InlineData("fe80::1")] // link-local
	[InlineData("fc00::1")] // unique local
	[InlineData("fd12:3456::1")]
	[InlineData("ff02::1")] // multicast
	[InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
	[InlineData("::ffff:10.0.0.1")]
	[InlineData("64:ff9b::a00:1")] // NAT64 of 10.0.0.1
	public void Refuses_non_public_addresses(string address) {
		Assert.False(PublicAddress.IsPublic(IPAddress.Parse(address)));
	}

	[Theory]
	[InlineData("93.184.215.14")]
	[InlineData("1.1.1.1")]
	[InlineData("2606:4700:4700::1111")]
	[InlineData("::ffff:1.1.1.1")]
	public void Allows_public_addresses(string address) {
		Assert.True(PublicAddress.IsPublic(IPAddress.Parse(address)));
	}
}
