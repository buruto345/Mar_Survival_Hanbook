const http = require('http');
// Synthetic records follow the documented NTRS wire shape; they are not scientific content.
let mode = 'normal', revision = 'A', requests = [];
const server = http.createServer(async (req, res) => {
  const u = new URL(req.url, 'http://localhost');
  const json = (code, value, headers = {}) => {
    res.writeHead(code, { 'Content-Type': 'application/json', ...headers });
    res.end(JSON.stringify(value));
  };
  if (u.pathname === '/control') {
    mode = u.searchParams.get('mode') || 'normal';
    revision = u.searchParams.get('revision') || 'A';
    requests = []; return json(200, { mode, revision });
  }
  if (u.pathname === '/requests') return json(200, requests);
  requests.push({ method: req.method, path: u.pathname, query: Object.fromEntries(u.searchParams) });
  if (mode === 'timeout') { setTimeout(() => json(504, { error: 'delayed' }), 2500); return; }
  if (mode === '429') return json(429, { error: 'rate limited' }, { 'Retry-After': '1' });
  if (mode === '500') return json(500, { error: 'upstream error' });
  if (mode === 'invalid') return json(200, { unexpected: true });
  if (u.pathname === '/api/citations/search') {
    const offset = Number(u.searchParams.get('page.from') || 0);
    return json(200, { stats: { total: 2 }, results: [{ id: offset === 0 ? '123' : '124', title: 'Mars synthetic fixture' }] });
  }
  const match = u.pathname.match(/^\/api\/citations\/(123|124)$/);
  if (match) {
    if (mode === 'partial' && match[1] === '124') return json(404, { error: 'missing fixture' });
    return json(200, {
      id: match[1], title: 'Mars synthetic fixture ' + revision, distribution: 'PUBLIC',
      disseminated: 'DOCUMENT_AND_METADATA', abstract: 'Synthetic Mars model for testing only.',
      stiType: 'TECHNICAL_MEMORANDUM', center: { name: 'NASA fixture center' },
      authorAffiliations: [{ meta: { author: { name: 'Fixture author' }, organization: { name: 'NASA fixture organization' } } }],
      downloads: [{ mimetype: 'application/pdf', links: { pdf: 'pdf/' + match[1] } }]
    });
  }
  if (u.pathname.startsWith('/pdf/')) {
    res.writeHead(200, { 'Content-Type': 'application/pdf' });
    return res.end('%PDF-1.4\nSynthetic attachment ' + (mode === 'pdf-change' ? 'B' : 'A') + '\n%%EOF');
  }
  const base = 'http://127.0.0.1:4010/nasa';
  if (u.pathname === '/search') return json(200, { collection: { items: [{
    data: [{ nasa_id: 'PIA_TEST', title: 'Mars synthetic image', description: 'Mars fixture only', center: 'JPL',
      secondary_creator: 'NASA/JPL/USGS', media_type: 'image' }],
    links: [{ href: base + '/image~orig.png', rel: 'preview', render: 'image' }]
  }], metadata: { total_hits: 1 } } });
  if (u.pathname === '/asset/PIA_TEST') return json(200, { collection: { items: [{ href: base + '/image~orig.png' }] } });
  if (u.pathname === '/metadata/PIA_TEST') return json(200, { location: base + '/image-metadata' });
  if (u.pathname === '/image-metadata') return json(200, { 'XMP:Credit': 'NASA/JPL/USGS', 'XMP:UsageTerms': 'Synthetic test fixture only' });
  if (u.pathname === '/image~orig.png') {
    res.writeHead(200, { 'Content-Type': 'image/png' });
    return res.end(Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a/2kAAAAASUVORK5CYII=', 'base64'));
  }
  const xml = value => { res.writeHead(200, { 'Content-Type': 'application/xml' }); res.end(value); };
  if (u.pathname.startsWith('/tiles/')) return xml('<Capabilities xmlns="http://www.opengis.net/wmts/1.0" xmlns:ows="http://www.opengis.net/ows/1.1"><Contents><Layer><ows:Title>Mars test map</ows:Title><ows:Identifier>test-mola</ows:Identifier><Style><ows:Identifier>default</ows:Identifier></Style><Format>image/jpeg</Format><TileMatrixSetLink><TileMatrixSet>default028mm</TileMatrixSet></TileMatrixSetLink><ResourceURL template="https://trek.nasa.gov/test/{TileMatrix}/{TileRow}/{TileCol}.jpg"/><ows:WGS84BoundingBox><ows:LowerCorner>-180 -90</ows:LowerCorner><ows:UpperCorner>180 90</ows:UpperCorner></ows:WGS84BoundingBox></Layer><TileMatrixSet><ows:SupportedCRS>Mars EQ</ows:SupportedCRS></TileMatrixSet></Contents></Capabilities>');
  const product = 'urn:nasa:pds:mars2020_meda:data_calibrated_env:test_pressure::1.0';
  if (decodeURIComponent(u.pathname) === '/products/' + product) return json(200, {
    id: product, type: 'Product_Observational', title: 'Mars MEDA synthetic pressure product',
    targets: [{ id: 'urn:nasa:pds:context:target:planet.mars' }],
    metadata: { label_url: base + '/meda.xml', version: '1.0' }
  });
  if (u.pathname === '/meda.xml') return xml('<Product_Observational><Identification_Area><logical_identifier>urn:nasa:pds:mars2020_meda:data_calibrated_env:test_pressure</logical_identifier><version_id>1.0</version_id></Identification_Area><Observation_Area><processing_level>Calibrated</processing_level><lid_reference>urn:nasa:pds:context:instrument:mars2020.meda</lid_reference><lid_reference>urn:nasa:pds:context:target:planet.mars</lid_reference></Observation_Area><File_Area_Observational><File><file_name>meda.csv</file_name></File><Table_Delimited><offset>0</offset><field_delimiter>Comma</field_delimiter><Record_Delimited><fields>3</fields><groups>0</groups><Field_Delimited><name>SCLK</name><field_number>1</field_number></Field_Delimited><Field_Delimited><name>LMST</name><field_number>2</field_number></Field_Delimited><Field_Delimited><name>BAROCAP1_PRESSURE</name><field_number>3</field_number><unit>pascal</unit><description>Synthetic pressure fixture</description></Field_Delimited></Record_Delimited></Table_Delimited></File_Area_Observational></Product_Observational>');
  if (u.pathname === '/meda.csv') { res.writeHead(200, { 'Content-Type': 'text/csv' }); return res.end('123,00015M12:00:00.000,600\r\n'); }
  return json(501, { error: 'Endpoint not modeled in this simulation' });
});
server.listen(process.env.PORT || 4510);
