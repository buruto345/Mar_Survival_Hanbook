# Survival Knowledge API

## Run locally

From the repository root, configure the backend's `MarsDatabase` connection string and run:

```powershell
dotnet ef database update --project Backend/Mars_Survival_Hanbook
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- publish-survival-edition
dotnet run --project Backend/Mars_Survival_Hanbook --launch-profile https
```

The `seed` command initializes the module, categories, units and connectors. The release command imports [the reviewed English edition](../Backend/Mars_Survival_Hanbook/Data/Survival/release.en.json), verifies the facts and takes each article through the CMS review and publication workflow. The audit identity is `survival-release`, a local system account with interactive login disabled. The first NTRS import requires network access. Published pages, pinned citations and stored files are served locally.

On databases containing the previous release, the command publishes the five English articles and then withdraws the release-owned Vietnamese edition. The old versions and audit records remain available to administrators. Repeated runs preserve English article and version IDs. Subsequent editorial changes belong in the CMS.

## Image storage

Imported images live in `ImageMarHanbook` under the backend content root. Recognized formats use a filename such as `<sha256>.jpg` or `<sha256>.png`. The database references each image by checksum and records its MIME type and NASA source URL. Manifests and other evidence remain in `App_Data/raw`. Back up both directories with the database. The public media URL enforces publication scope and serves the stored bytes.

Set `Storage__ImageRoot` to use a separate persistent image directory. When only `Storage__RawRoot` is overridden, images go in its `ImageMarHanbook` subdirectory. Older raw stores can still serve checksum references during migration.

## Read an article

1. Fetch `GET /api/survival-knowledge/categories`. Render the categories in display order, including categories with a published article count of zero.
2. Fetch `GET /api/survival-knowledge/articles?category=dust-storms&q=dust&page=1&pageSize=20`. The result contains `items`, `page`, `pageSize` and `total`. English is the default language. Valid sort values are `publishedAtDesc` and `relevance`. The maximum page size is 100.
3. Open `GET /api/survival-knowledge/articles/dust-storms-and-solar-power`. Render the plain-text sections in array order. In this edition, each claim key identifies the corresponding section anchor.
4. Match each ID in `claim.sourceRevisionIds` to `sources[].sourceRevisionId`. Fetch `GET /api/survival-knowledge/sources/{sourceId}/revisions/{sourceRevisionId}`. Present the citation URL alongside `claim.locator.section` so the reader can find the evidence.
5. Use `media[].url` for images. Stored file URLs identify the published version and language. Unpublishing an article or revoking its evidence can make its dependent URLs return 404.

| Category | Published article slug |
|---|---|
| Oxygen and low pressure | oxygen-and-surface-pressure |
| Extreme temperatures | temperatures-at-viking-1 |
| Space radiation | radiation-at-curiosity |
| Dust storms | dust-storms-and-solar-power |
| Partial gravity and the human body | partial-gravity-and-human-health |

## Admin workflow

Log in with `POST /api/auth/login`. Send `Authorization: Bearer <accessToken>` to `/api/admin/survival-knowledge`. When creating or editing a draft, omit languageCode or send en. Other codes return 400. Historical versions in other languages remain available for audit and cannot be restored or published. Article and review lists default to English. Use an explicit language filter to inspect historical records. Reviewers check that the submitted prose is English before approval. The table uses A for the article ETag and R for the review task ETag. Preserve the quotation marks around the header value.

| Method and relative path | Body or If-Match | Success response |
|---|---|---|
| GET /articles | Filters and pagination | 200, article page |
| POST /articles | ArticleDraftRequest | 201, article, currentDraftVersionId, A and article Location |
| GET /articles/{id} | — | 200, article and A |
| PUT /articles/{id}/draft | ArticleDraftRequest, A | 201, article, new A and version Location |
| GET /articles/{id}/versions | Pagination | 200, version history page |
| GET /articles/{id}/versions/{v} | — | 200, snapshot and version ETag |
| GET /articles/{id}/versions/{v}/preview | — | 200, rendered preview |
| POST /articles/{id}/versions/{v}/submit | A | 201, review task, R and review Location |
| GET /reviews | articleId, state, reviewerId or dueBefore | 200, review page |
| GET /reviews/{r} | — | 200, task and R |
| POST /reviews/{r}/claim | R | 200, InProgress task and new R |
| POST /reviews/{r}/decision | ReviewDecision, R | 200, task decision and new R |
| POST /articles/{id}/publish | {"versionId":"…"}, A | 200, article and new A |
| POST /articles/{id}/unpublish | {"reason":"…"}, A | 200, article and new A |
| POST /articles/{id}/restore | {"sourceVersionId":"…","changeSummary":"…"}, A | 201, article, new A and draft version Location |

Fetch the article again before publishing. Approval requires all five scientific checklist entries. After `ChangesRequested`, save a new draft and submit it for review. Restoring a historical version also creates a new draft.

Shared editorial resources use `/api/admin/discover-mars`. Use `/sources/manual`, `/sources/{id}/revisions`, `/source-revisions/{id}/decision`, `/source-revisions/{id}/revoke`, `/content/facts`, `/content/facts/{id}/decision`, `/content/media` and `/imports`. For a fact decision, send the fact ETag and a body containing `decision` and `notes`. Save a fresh article draft after fact approval so the snapshot records Accepted status.

| HTTP status | Client action |
|---|---|
| 400 | Check the slug, query parameters, page size and enum values. |
| 401 / 403 | Authenticate or check the current user's Admin role. |
| 404 | Check the module, language, article, version and publication status. |
| 409 | Reload the workflow state before retrying. |
| 412 | Fetch the article or review, reconcile changes and use the fresh ETag. |
| 422 | Inspect Problem Details and correct the cited evidence, units or checklist. |
| 428 | Supply If-Match from the GET response for the resource being changed. |

[OpenAPI specification](../postman/specs/discover-mars.openapi.json) · [Frontend catalog](Api.xlsx) · [Postman collection](../postman/collections/Survival%20Knowledge.postman_collection.json)

## Verification

Run `./scripts/test-discovery.ps1` on Windows with SQL Server LocalDB and the repository's NASA simulation. Run the Postman Public folder against the released edition. For the complete CMS smoke test, run Public followed by CMS on a disposable database and supply baseUrl, username and password through a local environment. The CMS test creates and withdraws an article.

The release contains five articles, eleven claims, seven linked facts and five NASA citations. The sol 15 MEDA measurement product remains outside this release pending scientific review.

On November 8, 2026, anonymous HTTP reads on LocalDB returned one English article in each of the five categories and zero articles for language=vi. All eleven claims resolved to their pinned NASA citations. The backend suite passed 76 of 76 tests, and the Postman Public folder passed all ten assertions. A repeat release preserved the published versions.
