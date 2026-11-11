# Survival Knowledge API audit

**November 8, 2026 · Windows, .NET 8, Kestrel, SQL Server LocalDB**

All 22 method/path combinations in the running OpenAPI document returned a successful response on valid input. The audit reproduced one CMS preview limitation and three differences between OpenAPI and runtime behavior. No request in the audit returned a 5xx response.

## Test scope

- Read-only requests against the project's MarsSurvivalHandbook database found five English articles across five topics. All eleven claims resolved to five pinned NASA source revisions. These articles have no attached media.
- All writes used a newly migrated, disposable LocalDB database with the English edition loaded, a separate raw-file directory and the ingestion worker disabled. The test provisioned an Admin and registered a Viewer. A synthetic PNG exercised file delivery, version scoping and withdrawal.
- The HTTP audit issued 105 requests, including 99 to Survival Knowledge and six to authentication, shared media or Discover isolation routes. It exercised every feature route successfully and tested all fifteen CMS operations with both a guest and a Viewer. Status codes were checked against the expected outcome. JSON responses were checked against the running OpenAPI schemas, including nullable fields. Binary image bytes were compared directly.
- A separate run of the maintained Postman collection completed all 16 requests and all 22 assertions. The backend build completed with zero errors or warnings.

## Reproduced findings

1. **A reviewer cannot load a stored draft image from its preview URL.** Attach a stored PNG to a draft, then request its version preview as Admin. The preview returns 200 and a media URL under the public prefix. Following that URL returns 404 for both Admin and guest. After publishing the exact version, the same URL returns 200 and the original PNG bytes. The public file endpoint correctly requires a published version. The CMS needs an authenticated version-scoped file URL to display the frozen bytes during review. See [URL construction](../Backend/Mars_Survival_Hanbook/Services/Editorial/ArticleService.cs) and [public file lookup](../Backend/Mars_Survival_Hanbook/Controllers/DiscoverMarsController.cs).
2. **Swagger advertises an article filter the list ignores.** Request the article list with a random, nonexistent missionId. It returns 200 and total 5, the same five published articles. The operation exposes the shared DiscoveryQuery model, but ArticlesAsync applies language, category, text search, sorting and pagination. The frontend guide lists the supported filters accurately. A dedicated article-list query schema would prevent clients from relying on missionId. See [query model](../Backend/Mars_Survival_Hanbook/Contracts/DiscoveryContracts.cs) and [article query](../Backend/Mars_Survival_Hanbook/Services/Discovery/DiscoveryService.cs).
3. **CMS authentication failures do not provide the documented JSON error.** Across all fifteen operations, guest requests returned 401 and Viewer requests returned 403 without the advertised application/problem+json representation. Clients should branch on the status before parsing JSON. Authentication and authorization enforcement worked in every case.
4. **The media response type differs from OpenAPI.** The spec declares application/octet-stream. The stored PNG was served as image/png, with the expected bytes and Cache-Control: no-store. Browser display works; a client enforcing the documented response media type could reject it. [Response contract generation](../Backend/Mars_Survival_Hanbook/Services/DiscoveryOpenApiFilter.cs)

## Observed HTTP results

Public paths are relative to `/api/survival-knowledge`. Admin paths are relative to `/api/admin/survival-knowledge`. Parentheses show the number of requests with that status. A 2xx entry confirms a successful call for the operation. The negative cases are described below.

| Scope | Method | Path | Observed HTTP statuses |
|---|---|---|---|
| Public | GET | `/` | 200 (2) |
| Public | GET | `/articles` | 200 (9), 400 (1) |
| Public | GET | `/articles/{slug}` | 200 (7), 404 (2) |
| Public | GET | `/categories` | 200 (1) |
| Public | GET | `/categories/{slug}` | 200 (6), 404 (1) |
| Public | GET | `/media/{id}/file` | 200 (1), 404 (4) |
| Public | GET | `/sources/{id}/revisions/{revisionId}` | 200 (11) |
| Admin | GET | `/articles` | 200 (1), 401 (1), 403 (1) |
| Admin | POST | `/articles` | 201 (1), 400 (1), 401 (1), 403 (1), 409 (1) |
| Admin | GET | `/articles/{id}` | 200 (2), 401 (1), 403 (1) |
| Admin | PUT | `/articles/{id}/draft` | 201 (1), 401 (1), 403 (1), 412 (1), 428 (1) |
| Admin | POST | `/articles/{id}/publish` | 200 (1), 401 (1), 403 (1), 422 (1) |
| Admin | POST | `/articles/{id}/restore` | 201 (1), 401 (1), 403 (1) |
| Admin | POST | `/articles/{id}/unpublish` | 200 (1), 401 (1), 403 (1) |
| Admin | GET | `/articles/{id}/versions` | 200 (1), 401 (1), 403 (1) |
| Admin | GET | `/articles/{id}/versions/{versionId}` | 200 (1), 401 (1), 403 (1) |
| Admin | GET | `/articles/{id}/versions/{versionId}/preview` | 200 (2), 401 (1), 403 (1) |
| Admin | POST | `/articles/{id}/versions/{versionId}/submit` | 201 (1), 401 (1), 403 (1), 409 (1) |
| Admin | GET | `/reviews` | 200 (1), 401 (1), 403 (1) |
| Admin | GET | `/reviews/{id}` | 200 (1), 401 (1), 403 (1) |
| Admin | POST | `/reviews/{id}/claim` | 200 (1), 401 (1), 403 (1) |
| Admin | POST | `/reviews/{id}/decision` | 200 (1), 401 (1), 403 (1), 422 (1) |

## Workflow and error interpretation

The test created a draft, edited it to produce version 2, checked the version history and rendered its preview. It followed the review Location, claimed the task, approved it and published the exact version. Anonymous reads returned the revised title and PNG. Restoring version 1 created a third draft while version 2 remained public. Withdrawing the article removed public access to both the article and its image.

| Status | Reproduction | Interpretation |
|---|---|---|
| 400 | pageSize=101 or create a draft with languageCode=vi | Input validation rejected the request. |
| 401 / 403 | Call each CMS endpoint without a token or with a Viewer token | Access was denied as expected. The error body contract differs from Swagger. |
| 404 | Unknown category, draft article, wrong media version, cross-module article/file, or withdrawn article/file | Scope and publication checks worked. Draft media in an authenticated preview is the usability issue described above. |
| 409 | Create an existing slug or submit the same version twice with a fresh ETag | Uniqueness and workflow checks worked. |
| 412 / 428 | Edit a draft with an older article ETag or omit If-Match | Concurrency protection worked. Reload the article and use its quoted ETag. |
| 422 | Publish before approval or approve with an incomplete checklist | The publication gate prevented both actions. |

This audit covers local functional behavior and the observed HTTP contracts. Production configuration, load and latency under concurrency were outside its scope.
