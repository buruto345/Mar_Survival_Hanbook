# Discover Mars API artifacts

The ASP.NET controllers/DTOs generate `specs/discover-mars.openapi.json`. Run `scripts/export-discovery-openapi.ps1` with the Development backend running to refresh the spec and generated `collections/Discover Mars API` directory. That directory is replaced on regeneration; keep maintained assertions in `Discover Mars Smoke`.

- **Discover Mars API**: all 85 operations; replace generated example bodies, IDs and ETags before sending a selected request. It is an API catalog, not a sequential approval workflow.
- **Discover Mars Smoke**: eight read-only requests, twenty assertions, captured local response examples. Run with `postman collection run "postman/collections/Discover Mars Smoke" --no-report-events`.
- **Local.environment.yaml**: localhost URL and an empty secret token placeholder. Set credentials locally; do not commit token values.
- **nasa.sim.yaml**: routes synthetic upstream responses through `http://127.0.0.1:4010/nasa`. Start with `postman simulation run postman/simulations/nasa.sim.yaml --port 4010 --no-history`.

The actual .NET worker, HTTP transport and SQL Server persistence run in the simulation tests. Scenarios exercise normal/paginated ingestion, unchanged/PDF changes/A→B→A, partial retry, timeout, 429 with Retry-After, 500, malformed schema and resume after a committed item. Additional fixtures cover image credit/original bytes, WMTS metadata and MEDA samples with missing UTC/quality. Unknown mock routes return 501.

The simulation uses synthetic documents and is not evidence of scientific verification, production availability or support for every PDS dataset. Runtime local URL allowances are enabled only in Development tests; production retains the NASA/approved-host and public-IP checks.

No cloud workspace was modified or collection published. Postman CLI 1.75.0 printed a signed-out notice but executed the local smoke collection successfully: 8 requests and 20 assertions passed. See the [Vietnamese runbook](../Docs/Discover_Mars_Implementation_Status.md) for migration, account provisioning, import examples and remaining editorial acceptance.
