namespace UnMango.Wishlists.Api.Domain;

public sealed record MemberJoined(
	Guid MemberId,
	string GoogleSubject,
	string Email,
	string DisplayName,
	string? AvatarUrl);

public sealed record MemberView(
	Guid Id,
	string GoogleSubject,
	string Email,
	string DisplayName,
	string? AvatarUrl)
{
	public static MemberView Create(MemberJoined e) =>
		new(e.MemberId, e.GoogleSubject, e.Email, e.DisplayName, e.AvatarUrl);
}
