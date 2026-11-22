# Stored events are contracts; views are disposable

Wishlists are event-sourced with Marten, and the household's data has to stay readable by every later release. So a stored event is a contract for good: a release may add new event types, or add fields that are nullable or have a default, but it never renames, retypes, repurposes or deletes a stored event type or field. A change that really needs a new name or shape uses one of [Marten's versioning tools](https://martendb.io/events/versioning.html): `MapEventType` to keep the old stored name, an upcaster (`options.Events.Upcast<Old, New>(...)`), or a new versioned event type (`ItemAddedV2`) that the views handle alongside the old one. The old class stays in the code for as long as stored events use it.

Views (`MemberView`, `WishlistView`, `PurchasesView`) are disposable. The app rebuilds all of them from events on every startup, before the async daemon or the server starts (`RebuildViewsOnStartup`). The `Thumbnails` subscription keeps its own position, so nothing is fetched again.

`StoredEventsTests` enforces this. `stored-events.json` holds events exactly as Marten stored them (type name plus data, made-up people on `example.com`). The test appends them raw to a fresh schema, rebuilds the views and checks the result, and it fails if the current code would store any event under a different type name or with different fields. The file only ever gains entries: when a field is added, the new shape goes in as a new example after the old one, so both keep being replayed.

## Considered options

Marten's usual advice is to rebuild a projection only when it changes, or to use `ProjectionVersion` with async side-by-side builds for zero-downtime deploys. We rebuild every time so nobody has to remember to. The data is tiny and the chart runs one replica. Revisit this if there's ever more than one replica, since pods would rebuild over each other, or if startup gets slow.
