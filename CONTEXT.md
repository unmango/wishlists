# Wishlists

A private wishlists app for one household. Each member keeps a list, and other members secretly mark items bought.

## Language

**Household**:
The group of people who use the app together. Never described by size, names or relationships.
_Avoid_: family, the family

**Member**:
A person allowed to sign in.
_Avoid_: user, family member, any real name

**Owner**:
The member whose wishlist it is. An owner never sees what has been bought from their own list.
_Avoid_: recipient

**Gift-giver**:
A member looking at someone else's wishlist, who can mark items bought.
_Avoid_: buyer, shopper

**Wishlist**:
The one list of items a member keeps.
_Avoid_: list (when ambiguous), registry

**Item**:
One thing on a wishlist: a name, and optionally a link, price, notes and a "really want" flag.
_Avoid_: gift, wish

**Mark bought**:
What a gift-giver does to an item so other gift-givers know it's taken. Visible to every member except the owner.
_Avoid_: claim, reserve

**Maintainer**:
The person who runs the app and the repo, named on GitHub only by their handle.

## Example dialogue

> **Dev:** "A member reported the sheet won't close on Android."
> **Maintainer:** "Was it the owner editing their own wishlist, or a gift-giver?"
> **Dev:** "A gift-giver, right after they marked an item bought."
