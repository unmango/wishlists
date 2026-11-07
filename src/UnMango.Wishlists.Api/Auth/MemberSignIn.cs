using System.Security.Claims;
using Marten;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Auth;

internal sealed record GoogleProfile(
	string Subject,
	string Email,
	string DisplayName,
	string? GivenName,
	string? AvatarUrl);

internal sealed class MemberSignIn(IDocumentSession session, ILogger<MemberSignIn> logger)
{
	public const string MemberIdClaim = "member_id";

	/// <summary>
	/// Finds the member for a Google account, creating them and their wishlist on first sign-in.
	/// </summary>
	public async Task<MemberView> FindOrJoinAsync(GoogleProfile profile, CancellationToken cancellationToken) {
		if (await FindAsync(profile.Subject, cancellationToken) is { } existing) {
			return existing;
		}

		var memberId = Guid.CreateVersion7();
		var wishlistId = Guid.CreateVersion7();
		var purchasesId = Guid.CreateVersion7();
		var joined = new MemberJoined(memberId, profile.Subject, profile.Email, profile.DisplayName, profile.AvatarUrl);
		var name = $"{profile.GivenName ?? profile.DisplayName}'s wishlist";

		// One wishlist per person for now. When that changes, this is the only place that assumes it.
		session.Events.StartStream<MemberView>(memberId, joined);
		session.Events.StartStream<WishlistView>(wishlistId, new WishlistCreated(wishlistId, memberId, name, purchasesId));
		session.Events.StartStream<PurchasesView>(purchasesId, new PurchasesOpened(purchasesId, wishlistId));

		try {
			await session.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Member {MemberId} joined", memberId);
			return MemberView.Create(joined);
		}
		catch (Exception ex) when (IsUniqueViolation(ex)) {
			// Two sign-ins raced and the unique index on GoogleSubject rejected the second.
			session.EjectAllPendingChanges();
			return await FindAsync(profile.Subject, cancellationToken)
			       ?? throw new InvalidOperationException("Member disappeared after a sign-in race");
		}
	}

	public static ClaimsPrincipal Principal(MemberView member) => new(new ClaimsIdentity(
		[
			new Claim(MemberIdClaim, member.Id.ToString()),
			new Claim(ClaimTypes.Name, member.DisplayName),
			new Claim(ClaimTypes.Email, member.Email),
		],
		CookieAuthenticationDefaults.AuthenticationScheme));

	private static bool IsUniqueViolation(Exception? ex) {
		for (; ex is not null; ex = ex.InnerException) {
			if (ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) return true;
		}

		return false;
	}

	private Task<MemberView?> FindAsync(string subject, CancellationToken cancellationToken) =>
		session.Query<MemberView>().FirstOrDefaultAsync(x => x.GoogleSubject == subject, cancellationToken);
}

internal static class ClaimsPrincipalExtensions
{
	extension(ClaimsPrincipal principal)
	{
		public Guid MemberId => Guid.Parse(principal.FindFirstValue(MemberSignIn.MemberIdClaim)
		                                   ?? throw new InvalidOperationException("Signed-in user has no member id"));
	}
}
