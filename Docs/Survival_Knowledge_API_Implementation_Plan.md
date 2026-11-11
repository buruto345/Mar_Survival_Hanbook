# Survival Knowledge API implementation plan

Planned on November 7, 2026. The release language is English. Scope includes the backend, reviewed content, integration tests and the frontend contract.

## 1. Deliverables

Readers open Survival Knowledge, choose a topic, read a published article and follow each scientific claim to a specific passage, table or figure in a NASA source. Editors can save drafts, preview versions, submit reviews, request changes, publish an approved version and withdraw a publication. Public reads use content stored by the project and continue to work during NASA outages.

| Order | Category | Slug |
|---|---|---|
| 1 | Oxygen and low pressure | oxygen-and-low-pressure |
| 2 | Extreme temperatures | extreme-temperatures |
| 3 | Space radiation | space-radiation |
| 4 | Dust storms | dust-storms |
| 5 | Partial gravity and the human body | partial-gravity |

Ship one module and at least one published English article per category. Article details include scientific values, citations and media where available.

## 2. Baseline and reusable models

The original LocalDB inspection found Discover Mars, 25 scientific sources, accepted revisions for Mars Facts and the Mars Fact Sheet, and four relevant facts awaiting review. The English atmosphere article was a draft. The sol 15 MEDA product awaited review and there were no measurements. Recheck current records before importing. [Import report](Discover_Mars_English_Data_Import.md) · [Source review record](NASA_Source_Review_2026-11-07.md)

| Need | Model and implementation |
|---|---|
| Organization | Seed Module and Category by slug. Keep category order, module-scoped category keys and globally unique article slugs. |
| English content | Set Article.LanguageCode to en. Supply an English title, summary, section anchors, headings and plain-text bodies. |
| Scientific values | Reuse ScientificFact, Unit and ArticleFact. Keep original values and units, converted values, ranges, quantities and context. |
| Observations | Use Measurement and ArticleMeasurement only after verifying instrument, time, location and quality. Overview articles may precede MEDA ingestion. |
| Evidence | Share ScientificSource and SourceRevision across modules. Pin each claim to a revision and supply a section, table, page or figure locator. |
| Publication | Reuse ArticleVersion, ReviewTask, PublicationResource and AuditLog. Public reads resolve the published version. |
| Images | Use MediaAsset and ArticleMedia. Preserve credits, usage terms, alternative text and version-specific placement. |

## 3. HTTP contract

Public routes under `/api/survival-knowledge` include the module root, category list and detail, article list and detail, exact source revisions and version-scoped media files. The category list includes empty topics in display order. Article lists support category, q, language, page, pageSize and sort. Default to English, page 1 and 20 items, with a limit of 100. Return items, page, pageSize and total. Use stable ordering for publication ties. Empty searches return 200. Drafts, language mismatches and out-of-scope IDs return 404.

Admin routes under `/api/admin/survival-knowledge` cover article list, create, get and draft update, version list, detail and preview, submission, review list, detail, claim and decision, publication, withdrawal and restoration. Use the existing Admin policy and shared Discover source, fact, media and import endpoints. Document DTOs, success bodies, Location, ETag and error responses. Use article ETags for article mutations and task ETags for review mutations. [Endpoint and workflow reference](Survival_Knowledge_API.md)

## 4. Module isolation

Resolve the module from the matched controller. Scope category lookup, article reads and mutations, version ownership, review queries and related articles to that module. Store the module slug in the snapshot and generate the matching media and review URLs. Preserve existing Discover routes and old snapshots.

Use the same published-version query for lists, counts, details, citations and files. Shared resources must be reachable from a currently published version in the requested module. Keep transactions, row versions, locks and audit events. Revoking a shared revision affects all dependent publications across modules.

Test direct ID substitution in both directions for articles, previews, reviews and media. Test a revision that two modules legitimately share.

## 5. Seed and pilot

Seed the module and five categories idempotently on fresh databases and databases that already contain Discover. Verify category counts before publication. Use the oxygen and pressure article to exercise draft, preview, submit, claim, approval, publication and anonymous retrieval. Check source revision, pressure, oxygen abundance, original units and reference conditions.

Save a second draft while version one is public. Verify the original remains visible. Request changes, restore a previous version as a new draft and unpublish. Test concurrent edits with the same ETag and repeated submissions of a single version.

## 6. Editorial sources

| Article | Source and review requirements |
|---|---|
| Oxygen and low pressure | Distinguish the volume fraction of oxygen from total atmospheric pressure. Document original pressure units, conversion and reference level. [Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) |
| Extreme temperatures | Identify the Viking 1 landing site for the daily range. Distinguish that range from the global extremes and average atmospheric temperature. [Mars Facts](https://science.nasa.gov/mars/facts/) · [Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) |
| Space radiation | Identify RAD, the observation period and the absorbed dose rate unit. Verify graph axes and interpretation. [Radiation Measurements on Mars](https://science.nasa.gov/resource/radiation-measurements-on-mars/) |
| Dust storms | Locate the passage on sunlight, batteries and Opportunity. Explain optical depth and identify the 2018 event. [NASA JPL account](https://www.jpl.nasa.gov/edu/resources/teachable-moment/mars-rover-mission-comes-to-an-end-but-science-legacy-live-on/) |
| Partial gravity | Verify the definition of mean surface gravity. Import the Ames report through NTRS and distinguish indirect evidence from direct long-term observations in humans. [Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) · [NASA Ames report](https://ntrs.nasa.gov/citations/20210019591) |

Resolve sources by identity or canonical URL before importing. Review author, affiliation, revision and locator. Verify facts independently of source acceptance. Preserve source titles and credits. Check stored image bytes, MIME type, credit and alternative text when publishing media.

## 7. Integration and regression tests

Use SQL Server for writes, concurrency and revocation. Use NASA fixtures or the existing simulation for retries and upstream failures. Check live NASA sources separately during editorial review.

| Scenario | Expected behavior |
|---|---|
| Navigation and search | Five ordered categories, correct English search, category and language filtering, stable pagination and correct totals. Invalid page or page size returns 400. |
| Authorization | Guests read published content. CMS returns 401 without authentication and 403 for Viewer. Private assets and revisions stay inaccessible. |
| Versioning | Missing If-Match returns 428 and stale ETag returns 412. Duplicate submission conflicts. Editing cancels a superseded review. |
| Publication gate | Missing claim locators, unaccepted evidence, incomplete checklist or unapproved versions fail the corresponding gate with 422. |
| Frozen publications | A later draft cannot change the public title, category, content or images. Search, counts and citations switch together on publication. |
| Withdrawal | Unpublishing removes public access. Revocation hides all dependent publications. An independently reachable resource remains available. |
| Discover regression | Verify existing HTTP and Postman tests, old media URLs, shared sources, CMS routes and pagination. |
| Retry and rollback | Repeated seeding preserves IDs. Import retries do not duplicate records. Failed draft saves leave no partial version, links or review. |

## 8. Release and handoff

Publish five English articles through the editorial workflow. Verify each category, title and citation through the anonymous API. Export the backend OpenAPI document and check old and new routes. Provide a runnable Postman collection that captures IDs and ETags from responses. Update the frontend catalog with article, category and citation examples and recovery steps for 404, 412, 422 and 428. Record the environment, test results, published versions and pending evidence. [Frontend catalog](Api.xlsx) · [Release reference](Survival_Knowledge_API.md)

## 9. Future extensions

**Hazard lookup.** Reuse Hazard and ArticleHazard. Add a nullable Hazard.SourceRevisionId and verify that it belongs to the selected SourceId. Add hazard IDs to resource references and hazards to snapshots, defaulting to empty arrays for old versions. Update working links, projections and dependencies before exposing a filter. Test edits and revocations against published snapshots.

**Pressure time series.** Reuse the MEDA pressure connector. Verify revision authorship, product review, instrument channel, units, quality flags and time conventions. Import samples using stable identifiers, attach reviewed samples to an article, then expose a paginated measurements endpoint with location, quantity and time filters. Label Jezero and the observation sol in the chart. [MEDA parser](../Backend/Mars_Survival_Hanbook/Integrations/Nasa/PdsMedaParser.cs) · [NASA PDS](https://pds.nasa.gov/api/search/1/products/urn:nasa:pds:mars2020_meda)
