"""Exercise the local audit clone and record response contracts for every operation.

Run only against the disposable database prepared in App_Data/api-audit/state.json.
"""
import copy
import datetime
import json
import re
import urllib.request
import urllib.error
import uuid
from pathlib import Path
from jsonschema import Draft4Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Backend/Mars_Survival_Hanbook/App_Data/api-audit'
STATE = json.loads((OUT / 'state.json').read_text('utf-8-sig'))
BASE = 'http://127.0.0.1:5093'
A = '/api/admin/discover-mars'
P = '/api/discover-mars'
spec = json.load(urllib.request.urlopen(BASE + '/swagger/v1/swagger.json'))
(OUT / 'openapi.json').write_text(json.dumps(spec, ensure_ascii=False, indent=2), encoding='utf-8')

def convert(value):
    if isinstance(value, list):
        return [convert(x) for x in value]
    if not isinstance(value, dict):
        return value
    result = {k: convert(v) for k, v in value.items() if k != 'nullable'}
    if value.get('nullable'):
        return {'anyOf': [result, {'type': 'null'}]}
    return result

schemas = convert(spec['components']['schemas'])
results = []
token = None
phase = 'existing-data'

def operation(method, path):
    path = path.split('?')[0]
    for template, ops in spec['paths'].items():
        if method.lower() in ops and re.fullmatch(re.sub(r'\{[^}]+\}', '[^/]+', template), path):
            return template, ops[method.lower()]
    raise ValueError(path)

def call(method, path, body=None, expected=200, etag=None, public=False, note=''):
    template, op = operation(method, path)
    headers = {'Content-Type': 'application/json'}
    if token and not public:
        headers['Authorization'] = 'Bearer ' + token
    if etag:
        headers['If-Match'] = etag
    if method == 'POST' and path == A + '/imports':
        headers['Idempotency-Key'] = 'audit-' + uuid.uuid4().hex
    request = urllib.request.Request(BASE + path, data=None if body is None else json.dumps(body).encode(), headers=headers, method=method)
    try:
        response = urllib.request.urlopen(request, timeout=60)
    except urllib.error.HTTPError as error:
        response = error
    raw = response.read()
    mime = response.headers.get('Content-Type', '')
    data = json.loads(raw) if 'json' in mime and raw else None
    errors = []
    shape = op.get('responses', {}).get(str(response.status), {}).get('content', {}).get(mime.split(';')[0].strip(), {}).get('schema')
    if shape and data is not None:
        document = {**convert(shape), 'components': {'schemas': schemas}}
        errors = [{'path': '/'.join(map(str, e.absolute_path)), 'message': e.message[:500]} for e in Draft4Validator(document, format_checker=FormatChecker()).iter_errors(data)]
    record = {'method': method, 'endpoint': template, 'url': path, 'phase': phase, 'status': response.status,
              'expected': expected, 'statusPass': response.status == expected, 'schemaErrors': errors,
              'schemaChecked': shape is not None and data is not None, 'contentType': mime, 'note': note,
              'response': data if '/auth/' not in path else {'redacted': True}, 'bytes': len(raw)}
    if isinstance(data, dict) and 'items' in data:
        record['total'] = data.get('total')
        record['empty'] = not data['items']
    results.append(record)
    (OUT / 'responses.json').write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding='utf-8')
    print(method, path, response.status, 'schema errors', len(errors), flush=True)
    return data, response.headers.get('ETag')

def get(path, **kwargs):
    return call('GET', path, **kwargs)

def edit(method, path, body=None, expected=200, resource=None):
    _, tag = get(resource or path)
    return call(method, path, body, expected, tag)

login, _ = call('POST', '/api/auth/login', {'email': STATE['email'], 'password': STATE['password']})
token = login['accessToken']
get('/api/auth/me')
get(A + '/health')
articles, _ = get(A + '/articles?pageSize=100')
sources, _ = get(A + '/sources?pageSize=100')
jobs, _ = get(A + '/imports?pageSize=100')
get(A + '/reviews')
connectors, _ = get(A + '/connectors')
for connector in connectors:
    get(A + '/connectors/' + connector['key'])
for job in jobs['items']:
    get(A + '/imports/' + job['id'])
    get(A + '/imports/' + job['id'] + '/items?pageSize=100')
for source in sources['items']:
    get(A + '/sources/' + source['id'])
    revisions, _ = get(A + '/sources/' + source['id'] + '/revisions')
    for revision in revisions['items']:
        get(A + '/source-revisions/' + revision['id'])
        get(A + '/source-revisions/' + revision['id'] + '/raw')
resources = {}
for kind in ['facts', 'missions', 'locations', 'timeline-events', 'media', 'map-layers', 'measurements']:
    listing, _ = get(A + '/content/' + kind + '?pageSize=100')
    resources[kind] = listing['items']
    for item in listing['items']:
        get(A + '/content/' + kind + '/' + item['id'])
for article in articles['items']:
    path = A + '/articles/' + article['id']
    get(path)
    get(path + '/versions')
    get(path + '/versions/' + article['currentDraftVersionId'])
    get(path + '/versions/' + article['currentDraftVersionId'] + '/preview')
    get(P + '/articles/' + article['slug'] + '?language=en', expected=404, public=True)
for path in ['', '/categories', '/categories/atmosphere-temperature-gravity', '/articles', '/facts', '/missions', '/locations', '/timeline', '/map-layers']:
    get(P + path + '?language=en', public=True)
get(P + '/measurements?language=en&factId=' + resources['facts'][0]['id'] + '&from=2021-03-05T00:00:00Z&to=2021-03-07T00:00:00Z', public=True)

phase = 'synthetic-workflow'
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
suffix = uuid.uuid4().hex[:10]
source_body = {'url': 'https://science.nasa.gov/mars/facts/?audit=' + suffix, 'title': 'Synthetic API audit evidence',
               'authors': [{'name': 'Software fixture', 'organization': 'Test fixture'}], 'organization': 'Test fixture',
               'sourceType': 'WebPage', 'excerpt': 'Synthetic software validation data.', 'usageNote': 'Test fixture only.', 'retrievedAt': now}
source, tag = call('POST', A + '/sources/manual', source_body, 201)
revision = source['id']
source_id = source['sourceId']
call('POST', A + '/source-revisions/' + revision + '/decision', {'decision': 'Accepted', 'notes': 'Synthetic test decision only.'}, etag=tag)
unit = resources['facts'][0]['unit']['id']
# Choose the pressure unit from an existing imported fact.
unit = next(x['unit']['id'] for x in resources['facts'] if x['unit']['symbol'] == 'Pa')
bodies = {
 'missions': {'nasaIdentifier': 'audit-' + suffix, 'name': 'Synthetic audit rover', 'agency': 'Software test', 'missionType': 'Rover', 'status': 'Active', 'statusVerifiedAt': now},
 'locations': {'name': 'Synthetic audit location', 'slug': 'audit-' + suffix, 'latitude': 1, 'longitude': 2, 'coordinateSystem': 'Mars planetocentric east -180..180'},
 'facts': {'name': 'Synthetic audit pressure', 'value': 600, 'originalValue': 6, 'originalUnit': 'hPa', 'unitId': unit, 'measurementKind': 'Single', 'context': 'Synthetic test only', 'evidenceType': 'Modeled', 'testEnvironment': 'Simulation'},
 'timeline-events': {'title': 'Synthetic audit event', 'description': 'Software test only', 'eventDate': now, 'isPlanned': False, 'agency': 'Software test'},
 'media': {'title': 'Synthetic media wrapper for archived image', 'mediaType': 'Image', 'url': resources['media'][0]['originalUrl'], 'originalUrl': resources['media'][0]['originalUrl'], 'credit': resources['media'][0]['credit'], 'usageNote': 'Software test only', 'altText': 'Audit image', 'mimeType': resources['media'][0]['mimeType'], 'fileChecksum': resources['media'][0]['fileChecksum']},
 'map-layers': {'name': 'Synthetic audit map', 'serviceUrl': 'https://trek.nasa.gov/tiles/', 'layerIdentifier': 'audit-' + suffix, 'tileMatrixSet': 'default', 'format': 'image/jpeg', 'attribution': 'Software test', 'style': 'default', 'resourceUrlTemplate': 'https://trek.nasa.gov/tiles/{TileMatrix}/{TileRow}/{TileCol}.jpg', 'boundingBoxJson': '[-180,-90,180,90]', 'coordinateSystem': 'Mars EQ'}
}
ids = {}
for kind, body in bodies.items():
    body['sourceRevisionId'] = revision
    item, tag = call('POST', A + '/content/' + kind, body, 201)
    ids[kind] = item['id']
    call('PUT', A + '/content/' + kind + '/' + item['id'], body, etag=tag)
body = {**bodies['facts'], 'factId': ids['facts'], 'observedAt': now, 'instrument': 'Software test', 'qualityFlag': 'valid', 'missionId': ids['missions'], 'locationId': ids['locations']}
body.pop('name')
bodies['measurements'] = body
item, tag = call('POST', A + '/content/measurements', body, 201)
ids['measurements'] = item['id']
get(A + '/content/measurements/' + item['id'])
call('PUT', A + '/content/measurements/' + item['id'], body, etag=tag)
refs = dict(zip(['factIds','missionIds','locationIds','timelineEventIds','measurementIds','mediaIds','mapLayerIds'], [[ids[k]] for k in ['facts','missions','locations','timeline-events','measurements','media','map-layers']]))
draft = {'slug': 'audit-' + suffix, 'categorySlug': 'atmosphere-temperature-gravity', 'languageCode': 'en', 'title': 'Synthetic audit article', 'summary': 'Software validation only',
         'sections': [{'anchor': 'overview', 'heading': 'Overview', 'body': 'Synthetic English content for testing.'}],
         'claims': [{'key': 'claim', 'text': 'Synthetic claim', 'evidenceType': 'Modeled', 'testEnvironment': 'Simulation', 'sourceRevisionIds': [revision], 'locator': {'section': 'Test fixture'}}], 'sourceRevisionIds': [revision], 'resourceRefs': refs}
article, tag = call('POST', A + '/articles', draft, 201)
article_path = A + '/articles/' + article['id']
article, tag = call('PUT', article_path + '/draft', draft, 201, tag)
version = article['currentDraftVersionId']
task, task_tag = call('POST', article_path + '/versions/' + version + '/submit', expected=201, etag=tag)
task_path = A + '/reviews/' + task['id']
get(task_path)
task, task_tag = call('POST', task_path + '/claim', etag=task_tag)
call('POST', task_path + '/decision', {'decision': 'Approved', 'notes': 'Synthetic software test only', 'checklist': {k: True for k in ['sourcesChecked','claimsChecked','unitsChecked','evidenceChecked','mediaRightsChecked']}}, etag=task_tag)
edit('POST', article_path + '/publish', {'versionId': version}, resource=article_path)
for path in ['', '/categories', '/categories/atmosphere-temperature-gravity', '/articles', '/articles/' + draft['slug'], '/facts', '/missions', '/locations', '/timeline', '/map-layers']:
    get(P + path + '?language=en', public=True)
for kind in ['missions', 'locations', 'map-layers']:
    get(P + '/' + kind + '/' + ids[kind] + '?language=en&articleVersionId=' + version, public=True)
get(P + '/sources/' + source_id + '/revisions/' + revision + '?language=en', public=True, note='English-only publication must expose its citation.')
get(P + '/media/' + ids['media'] + '/file?articleVersionId=' + version + '&language=en', public=True)
get(P + '/media/' + ids['media'] + '/file?articleVersionId=' + version, public=True, note='Exact image URL emitted by the English article snapshot.')
start = (datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=1)).strftime('%Y-%m-%dT00:00:00Z')
end = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=1)).strftime('%Y-%m-%dT00:00:00Z')
get(P + '/measurements?language=en&factId=' + ids['facts'] + '&from=' + start + '&to=' + end, public=True)
edit('POST', article_path + '/restore', {'sourceVersionId': version, 'changeSummary': 'Synthetic restore'}, 201, article_path)
edit('POST', article_path + '/unpublish', {'reason': 'Synthetic audit cleanup'}, resource=article_path)
for kind in bodies:
    path = A + '/content/' + kind + '/' + ids[kind]
    edit('POST', path + '/archive', resource=path)
amendment = {k: source_body[k] for k in ['title','authors','organization','excerpt','usageNote']}
amendment['title'] += ' amendment'
edit('POST', A + '/source-revisions/' + revision + '/amend', amendment, 201, A + '/source-revisions/' + revision)
edit('POST', A + '/source-revisions/' + revision + '/revoke', {'reason': 'Synthetic audit revocation'}, resource=A + '/source-revisions/' + revision)
connector = next(x for x in connectors if x['key'] == 'ntrs')
edit('PUT', A + '/connectors/ntrs', {k: connector[k] for k in ['isEnabled','baseUrl','accessPolicy','rateLimitPerMinute']})
# The audit disables the worker. Queue acceptance and retry state transitions are measured here.
call('POST', A + '/imports', {'connectorKey': 'ntrs', 'recordIds': ['20240012934'], 'maxItems': 1, 'pageSize': 1}, 202)
edit('POST', A + '/imports/' + jobs['items'][0]['id'] + '/retry', expected=202, resource=A + '/imports/' + jobs['items'][0]['id'])
phase = 'negative-cases'
for path in [A + '/articles', A + '/sources', A + '/content/facts']:
    get(path, expected=401, public=True)
get(P + '/articles?page=0', expected=400, public=True)
get(P + '/missions?status=999', expected=400, public=True)
get(P + '/measurements', expected=400, public=True)
get(P + '/articles/does-not-exist?language=en', expected=404, public=True)
print('Audit completed with', len(results), 'requests', flush=True)
