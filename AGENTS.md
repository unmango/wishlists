## Keep the household private

This repo is public. Nothing pushed to GitHub may identify the household or what they want. That covers issues, PRs, commits, code comments, docs, test data, screenshots and release notes, here and in UnstoppableMango/the-cluster.

- **Never write** members' names, emails, relationships or ages, how many members there are, anything on a real wishlist (items, gift ideas, who bought what), or when the household will use something.
- **Use role words** from `CONTEXT.md` instead: member, owner, gift-giver, the household. "A member reported...", not a name.
- **Name the maintainer** only by GitHub handle (`UnstoppableMango`) or as "the maintainer", never by first name next to household details.
- **Test data and screenshots** use made-up people on `example.com` (Ada, Bob, ...), never real data from the live site.
- **Keep private details** in project memory or chat, never in the repo. If a task needs one, paraphrase it with role words before you push it to GitHub.

## Write with transitive verbs

Give every verb an object, in UI copy, error and log messages, code comments and docs. Name who acts on what.

- "Couldn’t sign you out.", not "Couldn’t sign out."
- "Tap “I’ll get this” to add it here.", not "...and it shows up here."
- "A background job looks for the thumbnail.", not "The thumbnail is looked for in the background."

Leave fixed wording alone: "Try again" in error messages, Google's sign-in button text ("Continue with Google") and the standard "Sign out" label, event type names (renaming one orphans stored events), API routes and contract names, and Helm chart boilerplate.

## Stored events are contracts

Never rename, retype or remove a stored event type or field. Add new ones instead, and add an example to `test/UnMango.Wishlists.Api.Tests/StoredEvents/stored-events.json` for any new event or field. See `docs/adr/0001-stored-events-are-contracts.md`.

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues on unmango/wishlists (via the `gh` CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
