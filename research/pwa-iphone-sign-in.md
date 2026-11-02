# Google sign-in from installed home-screen web apps (iOS, Android)

Research for issue #487. Date of research: 2026-11-02.

The question: our sign-in is a full-page redirect (`/auth/login` → 302 to `accounts.google.com` → `/signin-google` → `returnUrl`), set up in `src/UnMango.Wishlists.Api/Auth/AuthSetup.cs`. Does it finish inside an installed home-screen web app, and how long does the session last there?

Evidence labels used below:

- **[primary]**: vendor documentation, a vendor engineer's statement, a spec, or source code.
- **[anecdotal]**: third-party bug reports, forum threads or PR descriptions that have not been confirmed by a vendor.

## TL;DR

1. **iOS: sign-in is expected to finish inside the app, but this relies on a heuristic.** Apple says links outside a web app's scope open in an in-app Safari View Controller sheet. It also says OAuth on a third-party domain "will still open in your web app" because of heuristics, and that `window.open` always keeps a page inside the web app [primary, WWDC23]. Safari 17.2 fixed "some login pages unexpectedly open in Safari" [primary]. Several third-party reports from 2025–2026 say the heuristic can still miss, which leaves Google in a sheet whose cookie jar is not the app's [anecdotal]. In our flow a miss would make the correlation cookie disappear. The callback would then fail into `/?signin=failed`, or the user would end up signed in only inside the sheet.
2. **Our real persistence bug is on our side and affects every platform.** The `wishlists` cookie is a *browser-session cookie*, so it is sent with no `Expires`. ASP.NET Core only writes `Expires` when `AuthenticationProperties.IsPersistent` is true, and we never set it [primary, aspnetcore source]. `ExpireTimeSpan = 30 days` only limits how long the ticket inside the cookie is valid. A session cookie is dropped when the client "shuts down" [primary, MDN]. For an iOS home-screen app, that is when the app process is killed or the phone restarts. Users report exactly this: a forced re-login after the app is closed [anecdotal].
3. **ITP's 7-day cap does not apply to our cookie.** The cap covers *script-writable* storage, including cookies created with `document.cookie`. Our cookie is set by the server with `HttpOnly`. A first-party, uncloaked `Set-Cookie` is listed as "No cap". The domain of a home-screen web app is also exempt from ITP's website-data removal [primary, WebKit].
4. **Android Chrome (WebAPK) works.** A WebAPK shares Chrome's profile, including cookies [primary, web.dev]. Out-of-scope pages open in a Custom Tab inside the app [primary, W3C/Chromium discussion]. Google explicitly allows Custom Tabs [primary, Google]. Firefox for Android behaves the same way where it can install the site, but that part is less well documented.
5. **Google blocks `disallowed_useragent` only in embedded WebViews.** That means Android `WebView`, iOS `WKWebView`/`UIWebView`, and in-app browsers such as Instagram or Facebook. Safari View Controller, Custom Tabs and the system browsers are allowed [primary, Google]. I found no primary source saying Google blocks an iOS home-screen web app. Third-party reports of the error on iOS come from in-app browsers and native WKWebView wrappers [anecdotal].

## Our current flow, as the code has it

- `AddCookie`: name `wishlists`, `HttpOnly`, `Secure` (outside Development), `SameSite=Lax`, `ExpireTimeSpan = 30 days`, `SlidingExpiration = true`.
- Nothing sets `IsPersistent`. Neither `GoogleChallengeProperties` in `/auth/login` nor `OnTicketReceived` does, and `/auth/dev-login` calls `SignInAsync` with no properties.
- In ASP.NET Core's `CookieAuthenticationHandler`, `Expires` is only set when `IsPersistent` is true, both at sign-in and when a sliding renewal happens: `if (signInContext.Properties.IsPersistent) { ... CookieOptions.Expires = expiresUtc }` and `if (properties.IsPersistent && _refreshExpiresUtc.HasValue)` ([source](https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Cookies/src/CookieAuthenticationHandler.cs)) [primary].
- The Google handler first sets a `.AspNetCore.Correlation.*` cookie on the 302 to Google. That cookie is `SameSite=None`, `Secure` and `HttpOnly` ([RemoteAuthenticationOptions](https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/Core/src/RemoteAuthenticationOptions.cs)) [primary]. The handler then requires the same cookie on `/signin-google`. When it is missing, the request is a remote failure, and our `OnRemoteFailure` turns that into `/?signin=failed`. So **the 302 to Google and the callback must happen in the same cookie jar.**
- The repo has no web app manifest (`vite.config.ts`'s `build.manifest: true` is Vite's asset manifest, not a web app manifest). iOS 26 no longer requires one (see below), and its default scope is the host. Android Chrome and Firefox need one before they install the site as an app.

## iOS / iPadOS

### Separate cookie jar

- "Home Screen web apps have a standalone, app-like experience on iOS, with separate cookies and storage from the browser." ([WWDC23 "What's new in web apps"](https://developer.apple.com/videos/play/wwdc2023/10120/)) [primary]
- Since Safari 17.2, cookies (and only cookies) are copied from Safari once, when the user taps Add to Home Screen. "This will only work if the authentication state is stored within cookies." ([WebKit Features in Safari 17.2](https://webkit.org/blog/14787/webkit-features-in-safari-17-2/)) [primary]. After that the two jars are independent, so signing in later in Safari does not sign in the app (WWDC23 says this for macOS Dock apps, and the iOS jar is separate in the same way) [primary]. I found no documentation on whether a *session* cookie, one with no `Expires`, is included in the copy.

### Where `accounts.google.com` opens

- Apple says: "In Home Screen web apps on iOS, links outside the scope will open in Safari View Controller." and: "Authentication through OAuth on a third-party domain will still open in your web app. This is done through heuristics. If you test and find that your OAuth flow opens the authentication experience in the user's default browser, please send us feedback … If you want to ensure your OAuth flows do not open in the user's default browser, you can use window.open. Links loaded through window.open will always open in the web app regardless of scope." ([WWDC23](https://developer.apple.com/videos/play/wwdc2023/10120/)) [primary]
- Safari 17.2 also "Fixed an issue where some login pages unexpectedly open in Safari" and "Fixed a bug where the `scope` member in the web app manifest is not respected." ([Safari 17.2](https://webkit.org/blog/14787/webkit-features-in-safari-17-2/)) [primary]
- History: iOS 11.3 sent OAuth out to Safari. One poster said iOS 12.2 beta fixed it, and later posts report it again on 13.5.1 and in 2022–2023 ([Apple Developer Forums 100407](https://developer.apple.com/forums/thread/100407)) [anecdotal, no Apple answer].
- Recent third-party reports (2025–2026) say a redirect to Google from an installed iPhone app opened in a Safari View Controller sheet with its own cookies. In those reports the first callback failed on a missing state/PKCE cookie, and a retry signed in only the sheet. The fixes they describe switch to a `window.open` flow or a server-side handoff code. Examples: [FlashCardApp PR #19](https://github.com/Naijei1/FlashCardApp/pull/19) and [wynteam PR #663](https://github.com/warren-wyn-dev/wynteam/pull/663). Neither cites an Apple source, and neither gives an iOS version or a confirmed real-device result [anecdotal]. Whether the Safari View Controller sheet shares the web app's cookies is **not documented by Apple**.
- **What this means for us:** if the heuristic keeps the redirect in the app, everything happens in one jar and sign-in works. If it misses, the correlation cookie stays in the app's jar, the callback in the sheet fails and lands on `/?signin=failed`, and the app stays signed out. This has to be checked on a real device for each iOS version we care about.

### How long the session cookie survives

| Event | Persistent first-party `HttpOnly` cookie (`Expires` set) | Our current session cookie (no `Expires`) |
|---|---|---|
| 7 days without opening the app | Kept. The ITP 7-day cap covers only script-writable storage, a home-screen web app counts its own days of use, and its domain is exempt from ITP data removal [primary] | Kept while the process stays alive; usually killed sooner |
| App swiped away / process killed by iOS | Kept until `Expires` | Lost: a session cookie ends when the client shuts down ([MDN Set-Cookie](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Set-Cookie)) [primary]; home-screen reports match ([ProcessWire thread](https://processwire.com/talk/topic/6541-permanent-login-for-an-ios-web-app)) [anecdotal] |
| Phone restart | Kept until `Expires` | Lost |
| Storage pressure / quota eviction | Cookies are "not subject to" WebKit's quota and eviction policy ([Updates to Storage Policy, Safari 17](https://webkit.org/blog/14403/updates-to-storage-policy/)) [primary] | Same |

ITP details, all from WebKit [primary]:

- The 7-day cap applies to "all script-writeable storage": cookies created with `document.cookie`, LocalStorage, IndexedDB, service worker registrations and so on. "Web applications added to the home screen are not part of Safari and thus have their own counter of days of use." ([Full Third-Party Cookie Blocking and More](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/))
- "Home Screen Web Application Domain Exempt From ITP": "ITP always skips that domain in its website data removal algorithm." ([Tracking Prevention in WebKit](https://webkit.org/tracking-prevention/))
- `Set-Cookie` responses are capped to 7 days only for CNAME or third-party-IP cloaking of a *third-party* tracker. A first-party host that is not cloaked is "No cap" ([same page, CNAME and Third-Party IP Address Cloaking Defense](https://webkit.org/tracking-prevention/#cname-and-third-party-ip-address-cloaking-defense)). We set the cookie on top-level same-origin responses, so this does not apply.

A known bug to watch: [WebKit 272325](https://bugs.webkit.org/show_bug.cgi?id=272325), "REGRESSION (iOS 17.x): Session cookies being reset randomly in a Home Screen web app". It is open and has an Apple Radar. An Apple engineer reproduced it on 17.5 and 17.5.1 but not on the iOS 18 beta. Third parties still reported stale cookies on iOS 18.1 in Nov 2024 [primary thread; root cause unknown].

### iOS version notes (17 → 26)

- **17.0**: new storage policy. Home-screen apps get the same quota, and cookies are outside the quota ([WebKit](https://webkit.org/blog/14403/updates-to-storage-policy/)).
- **17.2**: Safari cookies are copied once on Add to Home Screen, the login-pages-opening-in-Safari bug is fixed, and manifest `scope` is respected ([WebKit](https://webkit.org/blog/14787/webkit-features-in-safari-17-2/)).
- **17.4 (EU)**: the betas dropped standalone home-screen apps in the EU. Apple reversed this before release: "we will continue to offer the existing Home Screen web apps capability in the EU" ([Apple DMA page](https://developer.apple.com/support/dma-and-apps-in-the-eu/#dev-qa), as quoted by [The Register](https://www.theregister.com/2024/03/02/apple_reverses_pwa_decision/)).
- **17.x–18.1**: WebKit 272325, random cookie resets (above).
- **26**: "By default, every website added to the Home Screen opens as a web app", and no manifest is required ([WebKit WWDC25](https://webkit.org/blog/16993/news-from-wwdc25-web-technology-coming-this-fall-in-safari-26-beta/); [heise](https://heise.de/-10749652)). More users will therefore be in standalone mode without choosing it. With no manifest, the scope defaults to the host (WWDC23), so `/signin-google` is in scope. Apple documents no change to cookies or out-of-scope navigation in 26.
- I found no primary source for any change to the OAuth heuristic itself between 17.2 and 26.

## Android

### Chrome (WebAPK)

- A WebAPK uses Chrome's current profile, so "cookies are shared", and clearing Chrome's site data also clears the app ([WebAPKs on Android](https://web.dev/articles/webapks)) [primary]. **There is no separate cookie jar.** A redirect to Google and back uses the same cookies whichever surface it runs in.
- Intent filters cover every URL inside `scope`, and an in-scope link opens the app ([web.dev](https://web.dev/articles/webapks)) [primary].
- An out-of-scope navigation, such as one to `accounts.google.com`, stays in the app's window as a Custom Tab with a URL bar. The manifest spec says the browser "SHOULD show a prominent UI element" with the origin ([w3c/manifest discussion, Chromium engineer, 2024](https://lists.w3.org/Archives/Public/public-webapps-github/2024Sep/0127.html); [MDN scope](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Manifest/Reference/scope)) [primary]. A Chromium engineer has noted that Android captures *every* in-scope navigation into the PWA, auxiliary windows included. That breaks some popup-based logins (PayPal is the example given), but our full-page redirect is not affected ([w3c/manifest #989 comment, 2026](https://lists.w3.org/Archives/Public/public-webapps-github/2026Mar/0404.html)) [primary].
- Session-cookie lifetime in a WebAPK is not documented. Use a persistent cookie anyway (see below).

### Firefox for Android

- Firefox installs an app only when the site has a valid web app manifest. Otherwise "Add to Home screen" creates a plain shortcut that opens a normal Firefox tab ([Mozilla support 1567788, community moderator, 2026](https://support.mozilla.org/zu/questions/1567788)) [anecdotal / moderator]. Mozilla's [bug 2020304](https://bugzilla.mozilla.org/show_bug.cgi?id=2020304) proposes a default app experience for every site, with no timeline.
- Out-of-scope links from a standalone app open in a Custom Tab, and the default scope is the origin ([bug 1364471](https://bugzilla.mozilla.org/show_bug.cgi?id=1364471), [bug 1389236](https://bugzilla.mozilla.org/show_bug.cgi?id=1389236)) [primary, but from the old Fennec era; current Fenix behavior is not documented]. That bug also named auth redirects to other origins as something "needing care".
- I found no primary source on whether Fenix's installed apps share Firefox's cookie jar. I assume they do, since they run in the same GeckoView profile [unverified].

## Does Google block any of these? (`disallowed_useragent`)

- Policy: "A developer must not direct a Google OAuth 2.0 authorization request to an embedded user-agent under the developer's control", meaning libraries that can "insert arbitrary scripts, alter the default routing … or access session cookies" ([OAuth 2.0 Policies, "Use secure browsers"](https://developers.google.com/identity/protocols/oauth2/policies)) [primary].
- Enforcement has applied since 2021-09-30 to Android `WebView` and iOS/macOS `WKWebView`/`UIWebView`. Google recommends the system browser, **Android Custom Tabs**, or **SFSafariViewController** ([Google Developers Blog, 2021-06-29](https://developers.googleblog.com/upcoming-security-changes-to-googles-oauth-20-authorization-endpoint-in-embedded-webviews/)) [primary].
- On this basis, a Chrome WebAPK (Chrome plus a Custom Tab), Firefox (GeckoView, a full browser) and an iOS home-screen app (Apple's own WebKit web-app runtime, plus Safari View Controller for out-of-scope pages) are not embedded user-agents under a developer's control. I found no primary source saying Google blocks them.
- Third-party reports of `disallowed_useragent` on iOS come from in-app browsers such as Instagram and Facebook ([next-auth #7044](https://github.com/nextauthjs/next-auth/issues/7044)) and from native WKWebView wrappers ([Apple forums 808734](https://developer.apple.com/forums/thread/808734)) [anecdotal]. **This is the more likely real-world failure:** someone taps a shared wishlist link inside Instagram, Facebook or Gmail's in-app browser and tries to sign in there.

## Changes that keep sign-in working

In rough order of value:

1. **Make the auth cookie persistent (server, small change).** In `OnTicketReceived`, set `ctx.Properties!.IsPersistent = true`. You can also set `ctx.Properties.ExpiresUtc`, or leave it so `ExpireTimeSpan` applies. Do the same in `/auth/dev-login` with `new AuthenticationProperties { IsPersistent = true }`. The cookie then carries `Expires` (≤30 days, and sliding renewals keep it, per the handler source above). It survives app kills and restarts on iOS, and ITP does not shorten it (first-party, server-set, `HttpOnly`). This is the fix for "signed out after closing the app". Keep auth state in cookies only, not localStorage, because only cookies are copied on Add to Home Screen (Safari 17.2).
2. **Add a web app manifest (`public/manifest.webmanifest` plus `<link rel="manifest">`).** Use `"id": "/"`, `"start_url": "/"`, `"scope": "/"` and `"display": "standalone"`, with icons. A root scope makes sure `/auth/login`, `/signin-google` and every `returnUrl` count as in scope on iOS, Android Chrome and Firefox. A narrower scope would push the callback out of the app. Firefox for Android needs a manifest before it will install the site as an app at all. On iOS 26 a site with no manifest already defaults to host scope, but an explicit one avoids depending on that.
3. **In standalone iOS only, start sign-in with `window.open` instead of a same-window link.** Apple: "Links loaded through window.open will always open in the web app regardless of scope" (WWDC23). Detect standalone with `matchMedia('(display-mode: standalone)')` or `navigator.standalone`. The button calls `window.open('/auth/login?returnUrl=/auth/done…')` from the tap handler. The opened window runs the whole redirect inside the web app's jar, so the correlation cookie and the `wishlists` cookie stay together. Make `/auth/done` a tiny page that notifies the main window and closes itself. Use `BroadcastChannel`, or have the main window re-query `/api/me` on `visibilitychange`/`focus`, because Google's COOP may null out `window.opener` after the cross-origin hop. This approach comes from Apple's statement, but the full round trip is **untested by us**; check it on a device.
4. **Detect a lost-jar callback and show a useful message.** If `/signin-google` fails with a missing correlation cookie, the result is `/?signin=failed`. When that page loads outside standalone mode, as happens in a Safari View Controller sheet or a Safari tab, say "return to the app and tap Sign in again", or offer step 3. Do not build a cross-jar handoff, such as a one-time code to type or confirm, unless step 3 fails in testing. A handoff adds attack surface (session transfer between jars) and has to be designed with care.
5. **Leave `SameSite=Lax` on `wishlists` and `SameSite=None; Secure` on the correlation cookie as they are.** The callback is a top-level GET navigation, which Lax allows, and the correlation cookie must be sent on the cross-site return from Google. [WebKit 219650](https://bugs.webkit.org/show_bug.cgi?id=219650) once reported Safari not sending `SameSite=Lax` cookies after a popup OAuth redirect when a service worker was installed. If we ever add a service worker, test sign-in again.
6. **Handle in-app browsers (Instagram, Facebook, Gmail).** Google returns `disallowed_useragent` there whatever we do. Detecting them by user agent is fragile, so at least catch the error landing and tell the user to "Open in Safari/Chrome".

## Open questions (need a real device)

- On iOS 17.x, 18.x and 26.x, does a same-window 302 from `/auth/login` to `accounts.google.com` stay in the web app, or open a Safari View Controller sheet? If it opens a sheet, does that sheet share the app's cookies?
- Does `window.open` from a standalone iOS app keep the whole Google round trip in the app's jar? Can the opened window close itself afterwards?
- Is a session cookie (no `Expires`) included in iOS 17.2+'s cookie copy on Add to Home Screen?
- Does Firefox for Android (Fenix) share Firefox's cookie jar with its installed apps?
