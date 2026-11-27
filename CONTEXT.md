# Wishlists

A private wishlists app for one household. Each member keeps a list, and other members secretly mark items bought.

## Language

**Household**:
The group of people who share the app. Never described by size, names or relationships.
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
One thing on a wishlist: a name, and optionally a link, price, notes and a "really want" flag. An item with a link may also show a thumbnail.
_Avoid_: gift, wish

**Order**:
The owner's arrangement of the items on their wishlist. Every member sees it by default. Only the owner changes it, and a new item starts at the top.
_Avoid_: sort order, ranking, priority

**Sort**:
How one member chooses to show a wishlist while looking at it, such as "really want" first or by price. A sort never changes the order, and items it treats as equal keep the order.
_Avoid_: order (for a viewer's choice), filter

**Thumbnail**:
A small picture on an item, found automatically from the item's link. Never chosen or uploaded by the owner. Changing or clearing the link removes the thumbnail.
_Avoid_: picture, photo, preview image, og image

**Import**:
Adding many items to your own wishlist at once from a file. An import only adds items: it never edits or removes ones already there, and skips items already on the wishlist.
_Avoid_: upload, sync, bulk add

**Sale date**:
A day the owner expects an item's price might change, such as a holiday sale. It's a hint, not a promise. An item has at most one, only the owner sets or clears it, and every member sees it. A past sale date stays until the owner changes or clears it.
_Avoid_: deal, price drop, sale (for the date itself)

**Mark bought**:
What a gift-giver does to an item so other gift-givers know it's taken. Visible to every member except the owner.
_Avoid_: claim, reserve

**Installed app**:
Wishlists added to a phone's home screen, with its own icon and name. It opens without the browser bar.
_Avoid_: PWA, web app, home screen shortcut

**Maintainer**:
The person who runs the app and the repo, named on GitHub only by their handle.

## Example dialogue

> **Dev:** "A member reported they can't close the sheet on Android."
> **Maintainer:** "Was it the owner editing their own wishlist, or a gift-giver?"
> **Dev:** "A gift-giver, right after they marked an item bought."
