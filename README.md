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
is missing, storage endpoints return HTTP 500 with
`The API is missing its 'AzureStorageConnection' application setting.`
The usage endpoint instead returns the generic HTTP 503 described below.

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

## Link usage

Successful short-link resolutions emit a `ShortLinkUsed` custom event to the
redirect function's **existing Application Insights resource**. Missing links do
not emit this event. The event contains only the stored `shortKey` and `domain`;
it does not add visitor identifiers, IP addresses, referrers, or destination URLs.
Existing Application Insights request telemetry is unchanged.

No new services, dependencies, or Table Storage writes are required. The existing
SDK buffers and sends events in the background: the redirect does not wait for
telemetry delivery or flush the buffer. Recording adds only local telemetry
processing; telemetry failures do not prevent the redirect. The redirect function
must have its existing `APPLICATIONINSIGHTS_CONNECTION_STRING` setting configured
(the deployment template already supplies it).

Open that Application Insights resource's **Logs** view and run this query for
per-link usage over the last 30 days:

```kusto
customEvents
| where timestamp >= ago(30d)
| where name == "ShortLinkUsed"
| extend domain = tostring(customDimensions.domain),
         shortKey = tostring(customDimensions.shortKey)
| summarize Uses = sum(itemCount), LastUsed = max(timestamp) by domain, shortKey
| order by Uses desc
```

For daily usage of one link, replace the example domain and key below:

```kusto
customEvents
| where timestamp >= ago(30d)
| where name == "ShortLinkUsed"
| where tostring(customDimensions.domain) == "example.com"
    and tostring(customDimensions.shortKey) == "abc123"
| summarize Uses = sum(itemCount) by bin(timestamp, 1d)
| order by timestamp asc
```

Counts represent resolutions that reach the function, **not unique visitors or
guaranteed click totals**. Existing permanent redirects remain unchanged, so
browser/CDN-cached redirects are not counted; bots and repeated requests are.
Telemetry is best-effort and eventually visible: sampling (accounted for with
`sum(itemCount)`), ingestion limits, retention, or process termination can make
counts approximate. Tracking starts with this deployment; historical usage cannot
be recovered. Events use the existing telemetry ingestion allowance and may
increase Application Insights costs. Usage is never exposed through the public
redirect endpoint.

### Frontend usage API configuration

The signed-in frontend can call `GET /api/Links/Usage`. The API uses the same
Static Web Apps identity as `GET /api/Links`, loads only that user's owned links
from storage, and queries only those `(shortKey, domain)` pairs in one workspace
query. It returns the fixed last **30 days** as:

```json
[
  {"partitionKey":"abc123","rowKey":"example.com","uses":12,"lastUsed":"2026-10-08T12:00:00.0000000+00:00"},
  {"partitionKey":"unused","rowKey":"example.com","uses":0,"lastUsed":null}
]
```

`partitionKey` is the short key; `rowKey` is the domain. Counts use
`sum(ItemCount)` from `AppEvents` (`TimeGenerated`, `Name`, `Properties`,
`ItemCount`), not the Application Insights `customEvents` aliases used above.
Zero counts are returned only after a successful query; accounts with no owned
links return `[]` without analytics configuration, authentication, or querying.
Missing configuration,
workspace access failures, query errors (including partial results), and timeouts
return an actionable, generic HTTP **503**, never fabricated zeros. Unsigned users
receive **401**. All usage responses have `Cache-Control: no-store`; requests are
bounded to 30 seconds. These telemetry-based frontend counts remain approximate,
eventually consistent, and exclude browser/CDN-cached redirects as described above.

**My short links** displays the 30-day resolution count and last-used date with
independent loading and retry; link management remains unaffected.

Use the **existing Log Analytics workspace linked to the redirect function's
Application Insights**; do not create another workspace or telemetry service.
Find its **Workspace ID** (not its Azure resource ID). Create or use a Microsoft
Entra app registration and its service principal in the workspace's tenant, and
create a client secret. Assign that service principal the **Log Analytics Reader**
role scoped to this existing workspace. The managed API obtains an Entra
`client_credentials` token for `https://api.loganalytics.io/.default` and queries
`https://api.loganalytics.azure.com/v1/workspaces/{workspaceId}/query`.
Static Web Apps Free managed APIs cannot rely on managed identity for this.

Set these four application settings on the **Static Web App managed API**, not
on the redirect app and never as browser/Vite environment variables:

```bash
az staticwebapp appsettings set \
  --name <static-web-app-name> \
  --setting-names \
    "AnalyticsWorkspaceId=<workspace-guid>" \
    "AnalyticsTenantId=<entra-directory-tenant-guid>" \
    "AnalyticsClientId=<app-registration-client-guid>" \
    "AnalyticsClientSecret=<client-secret-value>"
```

Use the secret **value**, not its ID; rotate it before expiry. Tenant and client
IDs must be GUIDs. Configure preview environments separately using
`--environment-name <environment-name>`; they do not inherit production settings.
For local Functions development, the same names can be placed in the `Values`
section of your uncommitted `backend/local.settings.json`. Keep secrets out of
source control, frontend bundles, logs, and screenshots. No new runtime
dependencies, Azure services, or redirect-path changes are required.
