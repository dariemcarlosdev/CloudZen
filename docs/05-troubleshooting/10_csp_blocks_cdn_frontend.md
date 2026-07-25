# Issue #1 — CSP Blocks CDN Resources on First Load (Service Worker)

**Date:** June 2025
**Status:** Resolved
**Affected files:** `wwwroot/staticwebapp.config.json`, `wwwroot/service-worker.published.js`

## Description

On first load in production, the Azure Static Web App fails to render correctly — the browser console floods with hundreds of `Content Security Policy` violations blocking requests to CDN origins (`cdn.jsdelivr.net`, `cdn.tailwindcss.com`, `fonts.googleapis.com`). A hard refresh (`Ctrl+Shift+R`) resolves the issue temporarily, but every new visitor or cleared cache reproduces it.

## Why It Happens

In a Blazor WebAssembly PWA, the published service worker (`service-worker.published.js`) intercepts **all** fetch requests — including those for external CDN resources like Tailwind CSS, Bootstrap Icons, and Google Fonts. When the service worker calls `fetch(event.request)` for these cross-origin URLs, the browser enforces the `connect-src` Content Security Policy directive. The original `connect-src` only allowed `'self'` and the Azure Function/Blob Storage origins, so every CDN fetch was blocked.

A hard refresh bypasses the service worker entirely, which is why the page loaded correctly after refresh — resources were fetched directly by the browser using `default-src` (which did include the CDN origins).

Additionally, the CSP lacked explicit `style-src` and `script-src` directives. The browser fell back to `default-src` for stylesheet and script evaluation, which in some cases was truncated or misapplied by the Azure Static Web Apps platform, causing Google Fonts stylesheets to be blocked.

## How It Was Solved

**1. Updated CSP in `staticwebapp.config.json`:**

- Added CDN origins to `connect-src` so the service worker's fetch calls are allowed:
  ```
  connect-src 'self' <api-url> <blob-url> https://cdn.jsdelivr.net https://cdn.tailwindcss.com https://fonts.googleapis.com https://fonts.gstatic.com;
  ```
- Added explicit `style-src` and `script-src` directives instead of relying on `default-src` fallback:
  ```
  style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com;
  script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' https://cdn.tailwindcss.com;
  ```

**2. Updated `service-worker.published.js`:**

- **Critical fix:** Moved cross-origin filtering into the `fetch` event listener itself, **before** `event.respondWith()` is called. When `respondWith()` is not called, the browser handles the fetch natively — completely outside the service worker's CSP scope. The previous approach of doing `return fetch(event.request)` inside `onFetch()` was insufficient because that `fetch()` still executed within the service worker context, where `connect-src` was enforced.
  ```javascript
  self.addEventListener('fetch', event => {
      // Only intercept same-origin requests
      if (event.request.url.startsWith(self.origin)) {
          event.respondWith(onFetch(event));
      }
      // Cross-origin requests fall through to the browser's default fetch
  });
  ```

## How to Test

**For new visitors / clean verification:**

1. Deploy the changes to production.
2. Open the site in a **completely fresh incognito/private browser window** (ensures no cached service worker).
3. Open DevTools (`F12`) → **Console** tab.
4. Verify there are **no** CSP violation errors.
5. Verify the page renders correctly with all styles (Tailwind, Bootstrap Icons, Google Fonts).
6. New visitors will never hit this issue.

**For existing regular browser sessions (one-time fix):**

The old service worker cached the old CSP headers with the HTML responses. You must clear it once:

1. Open your site in a normal browser window.
2. Press `F12` → **Application** tab → **Service Workers** (left sidebar).
3. Check **"Update on reload"**.
4. Click **Unregister** on the listed service worker.
5. Go to **Application** → **Storage** → click **Clear site data**.
6. Close the tab and reopen your site.
7. Subsequent visits will use the new service worker without issues.
