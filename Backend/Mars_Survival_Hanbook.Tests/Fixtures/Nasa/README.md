# NASA contract fixtures

Retrieved from official public endpoints on 2026-11-02 for contract/parser inspection:

- `ntrs_record.json`: https://ntrs.nasa.gov/api/citations/20240012934
- `images_record.json`: https://images-api.nasa.gov/search?nasa_id=PIA00407
- `meda_bundle.json`: https://pds.nasa.gov/api/search/1/products/urn:nasa:pds:mars2020_meda
- `meda_pressure.json`: PDS registry product `urn:nasa:pds:mars2020_meda:data_calibrated_env:we__0015___________cal_ps__________________p::2.0`.
- `meda_pressure.xml`: XML label located through that registry product's `ops:Label_File_Info` metadata.
- `meda_product.xml`: an additional official MEDA engineering label inspected during schema discovery.

The pressure CSV rows in parser tests and all responses in `postman/mocks/nasa/default.js` are **synthetic test inputs**, not scientific measurements to publish. The pressure fixture has SCLK/LMST; missing verified UTC/quality remains pending. These fixtures do not establish live endpoint uptime or scientific approval.
