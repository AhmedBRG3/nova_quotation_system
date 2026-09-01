# NOVA — Website Under Process

Animated holding page carrying the INOVA wordmark, shown while the real site is built.

## Run it locally

```bash
cd nova-web
npm run dev      # http://localhost:5173
```

## Build for GoDaddy

```bash
npm run build    # writes ./build
npm run preview  # http://localhost:5174 — checks the built folder
```

Then upload the **contents** of `build/` (not the folder itself) into `public_html`
via GoDaddy's File Manager or FTP.

## Files

- `index.html` — markup
- `css/style.css` — animation + layout
- `js/main.js` — headline split, progress read-out
- `assets/inova-logo.jpg` — brand wordmark
- `.htaccess` — compression, caching, no caching of the holding page itself
