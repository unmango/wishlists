## Keep the household private

This repo is public. Nothing pushed to GitHub may identify the household or what they want. That covers issues, PRs, commits, code comments, docs, test data, screenshots and release notes, here and in UnstoppableMango/the-cluster.

- **Never write** members' names, emails, relationships or ages, how many members there are, anything on a real wishlist (items, gift ideas, who bought what), or when the household will use something.
- **Use role words** from `CONTEXT.md` instead: member, owner, gift-giver, the household. "A member reported...", not a name.
- **Name the maintainer** only by GitHub handle (`UnstoppableMango`) or as "the maintainer", never by first name next to household details.
- **Test data and screenshots** use made-up people on `example.com` (Ada, Bob, ...), never real data from the live site.
- **Private details** belong in project memory or chat, never in the repo. If a task needs one, paraphrase it with role words before it reaches GitHub.

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues on unmango/wishlists (via the `gh` CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
