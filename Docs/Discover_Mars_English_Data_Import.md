# Discover Mars: English NASA data import

Imported and verified on **2026-11-03** into SQL Server `(localdb)\MSSQLLocalDB`, database **MarsSurvivalHandbook**.

This is a curated starter dataset covering all five Discover Mars categories. The database contains real NASA content, source revisions and imported resources. Original English paragraphs are retained; HTML structure and whitespace are normalized. Titles, resource labels and explanatory context added by the application are also in English. Article `LanguageCode` is `en`.

## Imported inventory

| Content | Count | Storage / status |
| --- | ---: | --- |
| English articles | 5 | `Articles`, all `Draft` |
| Immutable article versions | 10 | Original drafts retained; current version 2 includes images and supporting resources |
| NASA sources and source revisions | 25 / 25 | All revisions `Pending`; 10 web pages, 10 Images records, 3 NTRS records, 1 PDS product, 1 Mars Trek layer |
| Scientific facts | 8 | `ScientificFacts`, normalized units with original values and context |
| Missions | 5 | Mariner 4, Viking 1, Curiosity, Perseverance, Mars Reconnaissance Orbiter |
| Timeline events | 4 | Source-supported timestamps for Mariner 4 and Viking 1 |
| Locations | 4 | Landing/proposed landing sites and an image-center location, with coordinate context |
| Images | 10 | `MediaAssets`; original files archived with SHA-256, original NASA credit and English alternative text |
| Research papers | 3 | `ResearchPapers`; NTRS metadata and supplied PDFs archived |
| Map layers | 1 | Mars Trek MOLA color-shaded relief WMTS metadata and capabilities |
| MEDA pressure CSV | 26,818 rows | Complete original CSV archived; 100 parsed candidate values retained in PDS revision metadata |
| Published articles / approved measurements | 0 / 0 | Import does not approve or publish content |

All four native ingestion jobs (`ntrs`, `images`, `meda-pressure`, `mola`) finished with `Succeeded` and zero failed items.

## Articles

| Category | English title | Slug |
| --- | --- | --- |
| Exploration history | Mariner 4 and Viking 1 | `mariner-4-and-viking-1-en` |
| Structure and terrain | Mars Facts: Structure and Surface | `mars-structure-and-surface-en` |
| Atmosphere, temperature and gravity | Mars Facts: Atmosphere | `mars-atmosphere-en` |
| Water and life | Water on Mars and Jezero Crater | `mars-water-and-jezero-en` |
| Missions and rovers | Mars Rovers and Mars Reconnaissance Orbiter | `mars-rovers-and-orbiter-en` |

Each current draft includes selected original English sections, claim locators, pinned source revision IDs and the relevant images/resources. The terrain article also references the MOLA map layer. The PDS product is stored as a separate source for subsequent scientific review.

## Source selection and formatting

Web evidence comes from [NASA Mars Facts](https://science.nasa.gov/mars/facts/), the [NASA Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html), official NASA pages for the five missions, and the three location reference pages listed in `scripts/prepare-discovery-english.py`.

NTRS documents:

- [20240012934 — Mars-GRAM 2024 User Guide](https://ntrs.nasa.gov/citations/20240012934).
- [20205008958 — Availability of Subsurface Water-Ice Resources in the Northern Mid-Latitudes of Mars](https://ntrs.nasa.gov/citations/20205008958).
- [20230016438 — In Situ Geologic Context Mapping Transect on the Floor of Jezero Crater From Mars 2020 Perseverance Rover Observations](https://ntrs.nasa.gov/citations/20230016438).

Images use NASA IDs `PIA00407`, `PIA02980`, `PIA01685`, `PIA15443`, `PIA02005`, `PIA01167`, `PIA23239`, `PIA24096`, `PIA19808`, and `PIA24487`. Individual source metadata retains the original credits, including partner organizations.

The PDS product is pinned to LIDVID `urn:nasa:pds:mars2020_meda:data_calibrated_env:we__0015___________cal_ps__________________p::2.0`. The archived registry response, XML label and CSV preserve provenance. `SourceRevisions.MetadataSnapshot.providerMetadata.samples` contains the parsed candidates. Each candidate retains its instrument channel, SCLK, LMST, original value/unit, normalized value/unit and validation errors.

The map layer is `Mars_MGS_MOLA_ClrShade_merge_global_463m`, from the [official Mars Trek WMTS capabilities](https://trek.nasa.gov/tiles/Mars/EQ/Mars_MGS_MOLA_ClrShade_merge_global_463m/1.0.0/WMTSCapabilities.xml).

Formatting decisions:

- Lengths are stored in metres, pressure in pascals, temperature in kelvin, and gravity in m/s². For example, `6.36 mb` becomes `636 Pa` while the original value, unit and seasonal context remain available.
- Mean surface gravity remains **3.73 m/s²**, as labeled by the imported NASA fact sheet. It is not replaced with a value using a different definition.
- Approximate and range values retain their context and `Estimate` / `Range` classification. These overview facts are not timestamped sensor measurements.
- Longitudes use east-positive `-180..180`. The Olympus image longitude `230.202° E` becomes `-129.798°`. Labels distinguish landing sites, proposed sites and image centers from the centers of geographic features.
- Exact source-supported UTC times are used for the four historical events. Day-only launch dates for other missions remain in source text; an exact time is not invented.

## Local files and repeatable import

Generated data and raw evidence are local and ignored by Git:

```text
Backend/Mars_Survival_Hanbook/App_Data/
  raw/                                      # Content-addressed evidence and manifests
  imports/nasa-english/
    *.html                                  # Original captured web pages
    pack.json                               # Formatted English input pack
    import-report.json                      # Job, revision, resource and article IDs
    verification-report.json                # Checksum and text verification results
  backups/
    MarsSurvivalHandbook_before_english_import_20261103_210438.bak
```

Keep the `raw` directory with the database when moving to another environment: database source revisions and media refer to these files. The SQL backup above was taken **before** this import.

Run from the repository root in PowerShell. Preparation requires Python 3 and `beautifulsoup4`; the .NET command uses the application's configured database connection. Apply existing migrations first when using a new database. This change requires no new migration.

```powershell
python scripts/prepare-discovery-english.py
$packPath = (Resolve-Path 'Backend/Mars_Survival_Hanbook/App_Data/imports/nasa-english/pack.json').Path
dotnet run --project Backend/Mars_Survival_Hanbook --no-launch-profile -- import-discovery-pack --file $packPath
```

The absolute pack path is intentional: `dotnet run` can set the working directory to the application project directory.

The normal rerun reuses existing source revisions, native jobs, marked resources and article slugs. It does not replace an existing article draft. Successful native jobs are reused, so this is a reproducible import command rather than a periodic upstream resynchronization job.

Optional recovery switches:

- `--retry-failed`: retry failed items in an existing failed/partially successful native job through the normal ingestion service.
- `--refresh-drafts`: append new immutable versions for import-owned drafts. Previous versions remain available. Refresh is rejected after human version edits, review submission or publication. Use deliberately; each explicit refresh creates a new version.
- `python scripts/prepare-discovery-english.py --refresh`: fetch web pages again. Existing typed resources are reused by stable pack keys; inspect changed upstream values and prepare a new reviewed pack/version when updating scientific content.

The importer uses a deactivated technical Admin identity, `nasa-import@local.invalid`, for audit attribution (converted from the former Editor role by the 2026-11-04 account migration). It has no usable interactive password and performs no scientific approval. An Admin can inspect and edit the imported content through the existing admin APIs. See [registration and authorization](User_Registration_And_Authorization.md).

## Review state and remaining scientific work

The public endpoints serve published snapshots, so these drafts are not visible publicly yet. After the normal review and publication workflow, request the English version with `language=en`.

Before publication, complete the existing source and resource review gates: authorship and rights, evidence classifications, mission-status verification, coordinate datum/context, and article review. These fields were left pending where the downloaded evidence did not establish them.

The MEDA pilot has no verified per-sample UTC clock solution or quality flag. All 100 candidates retain `utc_clock_solution_required` and `quality_review_required`; `Measurements` therefore remains empty. The original CSV's 26,818 rows are archived, but no candidate is presented as an approved observation or current Mars weather.

## Verification performed

- Reimported the same pack successfully: 25 source revisions, 5 articles, 10 versions and 21 marked typed resources remained unchanged.
- Compared every current article's sections and summary against the prepared original English excerpts.
- Verified SHA-256 for all 25 revision manifests and all 71 referenced raw evidence files, plus the five current article snapshots.
- Confirmed the MEDA row count, 100 pending candidates and missing per-sample UTC; confirmed zero published articles.
- Ran `scripts/test-discovery.ps1`: **65 tests passed, 0 failed, 0 skipped**, including SQL Server integration tests and the Postman NASA simulation. Build completed with zero warnings/errors; EF reported no pending model changes.

The live import also exposed and fixed two upstream compatibility issues: NASA Images emits legacy HTTP asset URLs, now upgraded only for the exact official asset host; the PDS registry redirects to `pds.mcp.nasa.gov`, now included in the exact HTTPS allowlist. Requests identify the application with its own User-Agent. Existing private-address, port and redirect checks remain in place.

Quick read-only SQL checks:

```sql
USE MarsSurvivalHandbook;

SELECT Slug, LanguageCode, Status, CurrentDraftVersionId, PublishedVersionId
FROM Articles
WHERE LanguageCode = 'en';

SELECT Provider, COUNT(*) AS SourceCount
FROM ScientificSources
GROUP BY Provider;

SELECT c.[Key], j.Status, j.RecordsFailed
FROM IngestionJobs AS j
JOIN NasaConnectors AS c ON c.Id = j.ConnectorId;

SELECT r.Id, JSON_VALUE(r.MetadataSnapshot, '$.providerMetadata.samples.totalRows') AS ArchivedRows
FROM SourceRevisions AS r
JOIN ScientificSources AS s ON s.Id = r.SourceId
WHERE s.Provider = 'Pds';
```
