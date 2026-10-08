# Url Shorter — frontend

React 19 + TypeScript single page app, bundled with [Vite](https://vite.dev) and
routed with [React Router](https://reactrouter.com). It is deployed as the static
content of the Azure Static Web App and talks to the managed Functions API under
`/api`.

## Getting started

```bash
npm install
npm run dev      # Vite dev server on http://localhost:3000
npm run lint     # oxlint
npm run build    # type-check + production build into dist/
npm run preview  # serve the production build
```

### Running with authentication and the API

`/.auth/*` and `/api/*` are provided by Azure Static Web Apps, so use the
[Static Web Apps CLI](https://azure.github.io/static-web-apps-cli/) to get the
full experience locally:

```bash
# in backend/
func start

# in frontend/
swa start http://localhost:3000 --run "npm run dev" --api-devserver-url http://localhost:7071
```

The app then runs on <http://localhost:4280> with the emulated login at
`/.auth/login/aad`. When the auth endpoint is unavailable (plain `npm run dev`)
the app treats the visitor as anonymous and shows the sign-in page.

## Configuration

| Variable            | Default            | Description                                                   |
| ------------------- | ------------------ | ------------------------------------------------------------- |
| `VITE_API_BASE_URL` | *(current origin)* | Absolute base URL of the API, for pointing at a remote backend |

Set it in a `.env.local` file or as a build-time environment variable.

## Routes

| Route    | Description                                            |
| -------- | ------------------------------------------------------ |
| `/`      | Create a short link (requires sign-in)                 |
| `/links` | Manage existing short links (requires sign-in)         |
| `/login` | Sign-in prompt                                         |
| `/List`  | Redirects to `/links` (the previous Blazor route)      |
| `*`      | Not found                                              |

`/?url=https://example.com` pre-fills the destination field, which makes the app
usable as a bookmarklet target.

## Project structure

```
src/
  api/        fetch wrapper and typed API calls
  auth/       Static Web Apps client principal context
  components/ layout, routing guard and shared UI
  hooks/      theme hook
  lib/        URL helpers and validation
  pages/      route components
  styles/     design tokens and global styles
  toast/      notification provider
public/
  staticwebapp.config.json  Static Web Apps routing and auth rules
```
