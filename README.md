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
