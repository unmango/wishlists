# PWA install requirements (issue #486)

Question: what must the Wishlists app serve before Chrome on Android, Firefox on Android,
Samsung Internet and Safari on iPhone offer "Install" / "Add to Home Screen" and open the
app standalone (full screen, without browser UI)?

Researched 2026-11-02 against primary sources. Each claim links to its source. Notes
marked **(unverified)** could not be confirmed in a primary source during this pass.

## TL;DR

- **One manifest covers all four browsers.** Serve `/manifest.webmanifest` with `name`,
  `short_name`, `start_url: "/"`, `scope: "/"`, `id: "/"`, `display: "standalone"`,
  `theme_color`, `background_color`, and PNG icons at 192x192 and 512x512 (`purpose: "any"`),
  plus a separate 512x512 `purpose: "maskable"` icon. Link it from `index.html`. Add a
  180x180 `apple-touch-icon` PNG.
- **No service worker is needed for install** on Chrome for Android (since Chrome 108) or
  Safari on iOS. Samsung Internet's own (older) docs still list a registered service worker
  for its address-bar install badge. Firefox for Android installs a shortcut whatever you
  serve.
- **HTTPS is required** everywhere. `localhost` is exempt for development.
- **Light vs dark:** the manifest has one `theme_color` and one `background_color`. Per-scheme
  colors only come from `<meta name="theme-color" media="(prefers-color-scheme: ...)">`, which
  `index.html` already has. Safari on iOS ignores `background_color`.
- **Host:** fix the Dockerfile `COPY ... public/ ./` (it flattens `public/`, see section 7). ASP.NET Core already maps `.webmanifest` to `application/manifest+json`. There is
  no SPA fallback, so a missing file returns 404 rather than `index.html`. Keep the manifest
  and icons public, and give them short or revalidating cache headers.
- **Recommendation:** hand-write the manifest and icons in `public/`. vite-plugin-pwa is built
  around a Workbox service worker that this issue does not need.

## 1. Per-browser requirements

### Chrome on Android

Chrome's documented install criteria ([web.dev: What does it take to be installable?](https://web.dev/articles/install-criteria)):

- The app is not already installed.
- Engagement: the user has tapped the page at least once and spent at least 30 seconds on it.
  This only gates the automatic prompt and `beforeinstallprompt`. The menu item is always
  there.
- Served over HTTPS.
- The manifest has `short_name` or `name`, `icons` with a 192px and a 512px icon, `start_url`,
  and `display` set to `fullscreen`, `standalone`, `minimal-ui` or `window-controls-overlay`.
- `prefer_related_applications` is absent or `false`.

Chromium's own installability checker
([installable_evaluator.cc](https://github.com/chromium/chromium/blob/main/components/webapps/browser/installable/installable_evaluator.cc),
[installable_logging.cc](https://github.com/chromium/chromium/blob/main/components/webapps/browser/installable/installable_logging.cc))
is slightly looser than the docs:

- The minimum primary icon size is **144px** (`kMinimumPrimaryIconSizeInPx = 144`). The icon
  must be PNG, SVG or WebP and must declare `sizes`. If `purpose` is set, it must include
  `"any"` or `"maskable"`.
- A `display_override` whose first supported mode is `browser` fails the check.
- Manifest URLs may not contain a username, a password or a port.
- Incognito windows cannot install.
- The current error list has **no service-worker error**.

Ship 192 and 512 anyway: the web.dev docs and the Lighthouse audit
([installable-manifest](https://developer.chrome.com/docs/lighthouse/pwa/installable-manifest))
still name those two sizes.

**Service worker:** Chrome dropped the service-worker and fetch-handler requirement for
install in Chrome 108 on mobile
([Chrome blog: Revisiting installability criteria](https://developer.chrome.com/blog/update-install-criteria);
[blink-dev FYI](https://groups.google.com/a/chromium.org/g/blink-dev/c/0uhGufIFLeo)). That
2023 post still said the automatic install prompt needed a `fetch` handler. The current
criteria page and the Chromium checker above no longer list one. The Android prompt is now
also driven by an ML model that uses signals such as visits in the last 14 days
([Chrome blog, 2024](https://developer.chrome.com/blog/how_chrome_helps_users_install_the_apps_they_value)).
**(unverified)** whether any 2026 prompt surface still checks for a fetch handler. The menu
install path does not.

**What the user gets:** on devices with Google Mobile Services, Chrome mints a **WebAPK**: a
real launcher entry, an entry in Android settings, and intent filters for every URL in
`scope`. If minting fails or the criteria are not met, it falls back to a plain shortcut
([web.dev: WebAPKs](https://web.dev/articles/webapks);
[web.dev learn: Installation](https://web.dev/learn/pwa/installation)). A site that misses the
criteria can still be added through "Add to home screen", but it gets the manual dialog, not
the app install ([Chrome blog](https://developer.chrome.com/blog/how_chrome_helps_users_install_the_apps_they_value)).

**Updates:** Chrome re-checks the manifest at launch if its copy is more than 24 hours old.
These fields trigger a rebuilt WebAPK: `name`, `short_name`, `icons`, `background_color`,
`display`, `orientation`, `scope`, `shortcuts`, `start_url`, `theme_color` and
`web_share_target`. The rebuild waits until the app is closed and the device is charging and
on Wi-Fi. "Don't change the name or location of your web app manifest file"
([web.dev: manifest updates](https://web.dev/articles/manifest-updates)).

### Samsung Internet

- MDN groups Samsung Internet with the Chromium browsers that need the same manifest fields as
  Chrome ([MDN: Making PWAs installable](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable)).
- It installs a **WebAPK only on Samsung-made devices**. On other phones it creates a shortcut
  ([web.dev learn: Installation](https://web.dev/learn/pwa/installation);
  [MDN](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable)).
- Samsung's ambient-badge doc lists these conditions for the "+" install icon in the address bar
  ([Samsung Internet docs: ambient badging](https://samsunginternet.github.io/docs/ambient-badging)):
  HTTPS, a manifest with `name`/`short_name`, `start_url`, an icon of at least 144x144, and
  `display` set to `standalone` or `fullscreen`. It also requires **a registered service
  worker**. That page is old. **(unverified)** whether current Samsung Internet, which is on a
  recent Chromium base, still requires the service worker. If the address-bar badge matters on
  Samsung, a no-op service worker is the only extra thing to add (see section 4).

### Firefox on Android

- In the menu, tap **Install**, then "Add to Home Screen" (drag the icon or "Add automatically")
  ([Mozilla Support: Use Web Apps with Firefox for Android](https://support.mozilla.org/en-US/kb/use-web-apps-firefox-android)).
- Firefox has no trusted WebAPK minting server, so it creates a **shortcut**, not a WebAPK.
  Shortcuts get no launcher or settings entry and cannot update their icon or metadata
  ([web.dev learn: Installation](https://web.dev/learn/pwa/installation)). MDN describes it as
  a "browser-badged home-screen shortcut that opens the site in the browser"
  ([MDN](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable)).
- Firefox reads the manifest's name and icons. **(unverified)** whether the shortcut opens
  standalone when `display: "standalone"` is set. Mozilla's support page does not say. Plan for
  Firefox to give "home-screen icon" quality, not a full app.
- Requirements beyond the manifest above: none documented. The manifest is used for the name
  and icon ([Bugzilla 1234558](https://bugzilla.mozilla.org/show_bug.cgi?id=1234558)).

### Safari on iPhone (iOS 26)

- In iOS/iPadOS 26, **every site added to the Home Screen opens as a web app by default**. The
  user can switch off "Open as Web App". "There are now zero requirements for 'installability'
  in Safari." A manifest is still honored: "If you define your icons in the manifest, they're
  used" ([WebKit: Safari 26.0 features](https://webkit.org/blog/17333/webkit-features-in-safari-26-0/);
  [WebKit: WWDC25 / Safari 26 beta](https://webkit.org/blog/16993/news-from-wwdc25-web-technology-coming-this-fall-in-safari-26-beta/)).
- Before iOS 26, standalone required the `apple-mobile-web-app-capable` meta tag or a manifest
  `display` value. Keep `display: "standalone"` for older iOS.
- There is no install prompt and no `beforeinstallprompt` on iOS. The user uses Share, then
  "Add to Home Screen". Since iOS 16.4 this also works from Chrome, Edge, Firefox and Orion on
  iOS ([MDN](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable);
  [WebKit: Safari 16.4](https://webkit.org/blog/13966/webkit-features-in-safari-16-4/)).
- iOS supports only `standalone`. `minimal-ui` falls back to a browser shortcut and
  `fullscreen` falls back to `standalone`
  ([web.dev learn: Installation](https://web.dev/learn/pwa/installation)).
- Safari honors the manifest `id` member, so separate installs of the same app can coexist
  ([WebKit: Safari 16.4](https://webkit.org/blog/13966/webkit-features-in-safari-16-4/)).
- **No service worker is needed.** One is only needed for offline use
  ([WebKit WWDC25 post](https://webkit.org/blog/16993/news-from-wwdc25-web-technology-coming-this-fall-in-safari-26-beta/)).

## 2. Icons

| File | Size / format | Purpose | Why |
|---|---|---|---|
| `icons/icon-192.png` | 192x192 PNG | `any` | Chrome/Samsung criteria ([web.dev](https://web.dev/articles/install-criteria)) |
| `icons/icon-512.png` | 512x512 PNG | `any` | Chrome criteria; splash screen; Safari manifest icon |
| `icons/maskable-512.png` | 512x512 PNG, opaque, art inside the central 80% (40% radius circle) | `maskable` | Android adaptive icon shapes. Without one, Android may shrink the icon onto a white background ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)) |
| `apple-touch-icon.png` | 180x180 PNG, opaque | link `rel="apple-touch-icon"` | iPhone retina home-screen size ([Apple: Configuring Web Applications](https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html)) |
| `favicon.svg` | SVG | `<link rel="icon">` | Replaces `vite.svg`. Safari 26 accepts SVG icons everywhere ([WebKit 26.0](https://webkit.org/blog/17333/webkit-features-in-safari-26-0/)) |

Notes:

- Keep the `maskable` icon separate from the `any` icon. `"any maskable"` is allowed, but a
  single image then has to serve both full-bleed and padded art
  ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)).
- Make icons square and opaque. Some operating systems fill transparent areas with a color you
  don't control ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)).
- `apple-touch-icon`:
  - A `<link rel="apple-touch-icon">` overrides the `/apple-touch-icon.png` root-file fallback.
  - If no size matches, iOS uses the smallest icon larger than the device size.
  - iOS 7+ adds no gloss effects ([Apple](https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html)).
  - With no icon at all, iOS uses a screenshot of the page
    ([web.dev learn: Installation](https://web.dev/learn/pwa/installation)).
  - Safari 26 says it uses manifest icons. Keeping the 180px link is still the safe choice for
    older iOS and costs one file.
- Don't add `apple-mobile-web-app-capable`. web.dev says the legacy meta tags are no longer
  recommended and can harm installation if the manifest fails to load
  ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)).

## 3. start_url, scope, id, HTTPS

Spec rules ([W3C Web App Manifest](https://w3c.github.io/manifest/)):

- **start_url** is resolved against the manifest URL. It is ignored if it is not the same origin
  as the document, and then defaults to the document URL.
- **scope** defaults to the start_url's directory. A scope that does not contain start_url is
  ignored.
- **id** is resolved against the start_url's origin. It is ignored if it is not the same origin,
  and then defaults to start_url. The fragment is stripped. It does not have to be in scope.

Browser behavior:

- Chrome 96+ derives `id` from `start_url` when it is missing. Changing `start_url` later without
  a fixed `id` can make browsers treat the app as a different app
  ([Chrome: manifest id](https://developer.chrome.com/docs/capabilities/pwa-manifest-id);
  [web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)). **Set `"id": "/"`
  now**, before anyone installs the app.
- Android registers intent filters for every URL within origin + `scope`, so in-scope links open
  the installed app ([web.dev: WebAPKs](https://web.dev/articles/webapks)).
- **HTTPS:** required. `localhost` and `127.0.0.1` are allowed for development
  ([MDN](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable);
  Chromium `IsContentSecure` in [installable_evaluator.cc](https://github.com/chromium/chromium/blob/main/components/webapps/browser/installable/installable_evaluator.cc)).
  Prod terminates TLS at the ingress and `UseForwardedHeaders` restores the scheme, so the browser
  sees HTTPS. Use `vite --host` plus a LAN IP only for testing over a tunnel or with a trusted
  cert.

What this means for Wishlists:

- Screens are hash routes (see `Program.cs` comment). `start_url: "/"` and `scope: "/"` cover the
  whole app, and hash changes never leave scope.
- Avoid query strings on `start_url`, such as UTM tags. They change the derived `id` and make
  WebAPK updates noisy.
- Google sign-in navigates to `accounts.google.com`, which is out of scope. Installed apps show
  out-of-scope pages in an in-app browser and then return
  ([web.dev learn: manifest, scope](https://web.dev/learn/pwa/web-app-manifest)).
  `/auth/login` returns to `/`, which is in scope. **(unverified)** test the full OAuth
  round-trip in standalone mode on iOS, where cookie and storage isolation for Home Screen apps
  has historically differed from Safari.

## 4. Service worker: needed anywhere?

| Browser | Needed to install/standalone? |
|---|---|
| Chrome Android | No (removed in Chrome 108; not in the current Chromium checker) |
| Samsung Internet | Listed in Samsung's ambient-badge doc for the address-bar icon (old page; unverified today). The menu "Add page to > Home screen" works regardless |
| Firefox Android | No (shortcut only) |
| Safari iOS 26 | No |

If the Samsung badge turns out to matter, the smallest compliant addition is a hand-written
`public/sw.js` with a pass-through `fetch` listener, registered from `main.tsx`. Don't add
Workbox precaching just for install. It brings cache invalidation work with authenticated API
responses.

## 5. theme_color and background_color in light vs dark mode

- The manifest takes **one** `theme_color` and **one** `background_color`, and neither accepts a
  media query
  ([MDN theme_color](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Manifest/Reference/theme_color);
  [MDN background_color](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Manifest/Reference/background_color)).
- MDN says browsers "may override" these values to follow the app's CSS `prefers-color-scheme`.
  This is not guaranteed behavior.
- **`<meta name="theme-color" media="(prefers-color-scheme: dark)">` overrides the manifest's
  `theme_color` per scheme** ([MDN theme_color](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Manifest/Reference/theme_color);
  [web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)). `index.html` already
  ships `#2b2d6e` (light) and `#12131f` (dark).
- In the manifest, use the light value (`#2b2d6e`) for `theme_color`, so it matches the meta
  tag. vite-plugin-pwa's docs also say these should match
  ([vite-pwa minimal requirements](https://vite-pwa-org.netlify.app/guide/pwa-minimal-requirements)).
  Pick a `background_color` equal to the app's CSS body background.
- Use opaque hex, rgb() or hsl() colors. No transparency, CSS variables or gradients
  ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)).
- **Android splash screen:** Android builds it from `background_color`, `theme_color` and the
  icon ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest)). It is
  single-color, so a dark-mode user sees the light `background_color` briefly at launch. The
  only mitigation is a neutral color that works in both schemes.
- **iOS** ignores `background_color` and builds no splash screen from the manifest. It uses a
  screenshot, or `apple-touch-startup-image`
  ([web.dev learn: manifest](https://web.dev/learn/pwa/web-app-manifest);
  [Apple](https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html)).
- A dark-mode manifest extension (`user_preferences` / `color_scheme`) exists only as a WICG
  incubation explainer
  ([manifest-incubations: user preferences](https://github.com/WICG/manifest-incubations/blob/b0562dee3d61782df4a0a191b9f192be0ab9cefb/user-preferences-explainer.md)).
  Don't rely on it.

## 6. vite-plugin-pwa vs hand-written files

| | Hand-written (`public/`) | vite-plugin-pwa 2.0.0 |
|---|---|---|
| What you get | Exactly the files you write. Vite copies `public/` into `dist/`, then into `wwwroot/` | Generated `manifest.webmanifest`, injected link, **Workbox service worker** and registration script ([vite-pwa guide](https://vite-pwa-org.netlify.app/guide/)) |
| Vite compatibility | n/a | peer `vite ^3 … ^8` (npm, 2026-10). Untested here with the `rolldown-vite@7.3.1` override |
| Service worker | None (not needed, section 4) | Generated by default. Supports `manifest: false` and other modes, but the plugin's point is the SW |
| Icons | Made once (by hand or with `@vite-pwa/assets-generator` CLI, run ad hoc) | Can wire in `@vite-pwa/assets-generator` |
| Risk | Low. Static JSON plus PNGs | A caching SW in front of a cookie-auth API needs an update strategy (`autoUpdate`/prompt) and testing |

**Recommendation:** hand-write `public/manifest.webmanifest` and the icons, and add the link tags
to `index.html`. Revisit vite-plugin-pwa only if offline support or a precaching SW becomes a
goal.

Naming caution: `vite.config.ts` sets `build.manifest: true`. That writes
`dist/.vite/manifest.json`, a **build** manifest used by `Vite.AspNetCore`. It is unrelated to
the web app manifest. Keep the web manifest named `manifest.webmanifest` so the two are never
confused.

## 7. ASP.NET static host (`Program.cs`, Dockerfile)

**Dockerfile issue (fix this first):** `COPY vite.config.ts tsconfig*.json index.html public/ ./`
copies the *contents* of `public/` into `/work`, not into `/work/public`. When a `COPY` source
is a directory, Docker copies the directory's contents, not the directory itself
([Dockerfile reference: COPY](https://docs.docker.com/reference/dockerfile/#copy)). Vite's
`publicDir` defaults to `<root>/public`, so in the image build it finds nothing to copy into
`dist/`. Today `/vite.svg` is probably missing from `wwwroot`. A manifest and icons in
`public/` would be missing too, and the browser would get 404s. Fix it with a separate line,
`COPY public/ ./public/`, and confirm by listing `wwwroot` in the built image.

- **MIME type:** the default `FileExtensionContentTypeProvider` already maps `.webmanifest` to
  `application/manifest+json`, which is the type the spec registers. `.png` and `.svg` are mapped
  too ([dotnet/aspnetcore source](https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/StaticFiles/src/FileExtensionContentTypeProvider.cs);
  [MDN manifest](https://developer.mozilla.org/en-US/docs/Web/Manifest)). No code is needed.
- **SPA fallback:** the app uses `UseDefaultFiles()` and `UseStaticFiles()` with no `MapFallback`,
  so a wrong manifest path returns a real 404 instead of `index.html` served as the manifest.
  Keep it that way. If a fallback is ever added, exclude `/manifest.webmanifest`, `/icons/*` and
  `/apple-touch-icon.png`.
- **Auth:** there is no `FallbackPolicy`; `AddAuthorization()` has defaults, and only `/api`
  `RequireAuthorization()`. Static files therefore stay public
  ([Microsoft Learn: static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0)).
  The manifest and icons must stay public: browsers fetch the manifest without credentials
  unless `crossorigin="use-credentials"` is set ([MDN](https://developer.mozilla.org/en-US/docs/Web/Manifest)).
- **Cache headers:** `UseStaticFiles` sends `ETag` and `Last-Modified` but no `Cache-Control` by
  default ([Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0)).
  That is acceptable: revalidation works.
  - If headers are added via `OnPrepareResponse`, give `/manifest.webmanifest` and the
    non-hashed icons `no-cache` (or a short max-age), and Vite's hashed `/assets/*` a long
    `immutable`.
  - Chrome re-fetches the manifest about every 24 hours. A long-cached manifest delays icon or
    name updates.
  - Keep the file names and paths stable over time ([web.dev manifest updates](https://web.dev/articles/manifest-updates)).
- **Development:** in development the Vite dev server serves `public/` through
  `UseViteDevelopmentServer`. Installability needs HTTPS or `localhost`.

## 8. Minimal file set

`public/manifest.webmanifest`:

```json
{
  "id": "/",
  "name": "Wishlists",
  "short_name": "Wishlists",
  "start_url": "/",
  "scope": "/",
  "display": "standalone",
  "theme_color": "#2b2d6e",
  "background_color": "#2b2d6e",
  "icons": [
    { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png", "purpose": "any" },
    { "src": "/icons/maskable-512.png", "sizes": "512x512", "type": "image/png", "purpose": "maskable" }
  ]
}
```

(`background_color` above is a placeholder. Set it to the real CSS body background.)

`index.html` `<head>` additions:

```html
<link rel="manifest" href="/manifest.webmanifest" />
<link rel="icon" type="image/svg+xml" href="/favicon.svg" />
<link rel="apple-touch-icon" href="/apple-touch-icon.png" />
<!-- keep the existing light/dark theme-color meta pair -->
```

Then check:

- Chrome DevTools > Application > Manifest shows no installability errors and the computed `id`.
- `about://webapks` on Android.
- On a real iPhone, Share > Add to Home Screen opens without Safari UI.
