# AzUrlShorter

Url shorter based on Azure Static Web Apps and Azure Table Storage

## Repository layout

| Path               | Description                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| `frontend/`        | React + TypeScript single page app (Vite) deployed as the static content |
| `backend/`         | Azure Functions managed API used by the frontend (`/api/*`)              |
| `redirectFunction/`| Azure Function that resolves a short key and performs the redirect       |
| `template/`        | ARM template used to provision the Azure resources                       |

## Frontend

The frontend is a React 19 SPA built with Vite and routed with React Router.
See [`frontend/README.md`](frontend/README.md) for local development and build
instructions.

## Managed API runtime

`backend/` is deployed as the Static Web Apps *managed* functions API. Static Web
Apps only runs the runtime declared by `platform.apiRuntime` in
[`frontend/public/staticwebapp.config.json`](frontend/public/staticwebapp.config.json),
currently `dotnet-isolated:9.0`. The project's `TargetFramework` and the
`api_version` input of the deployment workflow must stay in sync with it —
publishing a newer target framework deploys successfully but every `/api/*`
request then fails with HTTP 500, because the managed host cannot load the
assemblies.

`redirectFunction/` is deployed to a standalone Azure Function App and is not
bound by that restriction.

## Managed API storage configuration

The managed API reads and writes the `shorturls` and `configuration` tables using
the `AzureStorageConnection` application setting. This setting has to be present on
the **static site itself** — the connection string configured on the redirect
function app is a separate resource and is not visible to the managed API. When it
is missing, every `/api/*` call returns HTTP 500 with
`The API is missing its 'AzureStorageConnection' application setting.`

[`template/resources.json`](template/resources.json) now provisions it, so a
deployment of the ARM template configures the production environment. Preview
environments created for pull requests do **not** inherit production application
settings and have to be configured separately:

```bash
az staticwebapp appsettings set \
  --name <static-web-app-name> \
  --environment-name <environment-name> \
  --setting-names "AzureStorageConnection=<connection-string>"
```

Use `az staticwebapp environment list --name <static-web-app-name>` to find the
name of a preview environment.
