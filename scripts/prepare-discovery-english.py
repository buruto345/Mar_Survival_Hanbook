"""Download official English NASA pages and prepare a repeatable local data pack.

Requires beautifulsoup4. Raw evidence and generated pack stay under ignored App_Data.
Only whitespace/HTML structure is normalized; selected NASA paragraphs are not translated.
"""
import argparse
import concurrent.futures
import datetime as dt
import json
from pathlib import Path
import re
import urllib.request
from bs4 import BeautifulSoup

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Backend/Mars_Survival_Hanbook/App_Data/imports/nasa-english'
URLS = {
    'mars-facts': 'https://science.nasa.gov/mars/facts/',
    'mars-factsheet': 'https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html',
    'mariner-4': 'https://science.nasa.gov/mission/mariner-4/',
    'viking-1': 'https://science.nasa.gov/mission/viking-1/',
    'curiosity': 'https://science.nasa.gov/mission/msl-curiosity/',
    'perseverance': 'https://science.nasa.gov/mission/mars-2020-perseverance/',
    'mro': 'https://science.nasa.gov/mission/mars-reconnaissance-orbiter/',
    'gale': 'https://science.nasa.gov/resource/oblique-view-of-gale-crater-from-the-northwest/',
    'olympus': 'https://science.nasa.gov/photojournal/pj-olympus-mons-11/',
    'valles': 'https://marsoweb.nas.nasa.gov/landingsites/previous/sites/Valles_Marineris/imageBig.html',
}


def extract(path):
    soup = BeautifulSoup(path.read_bytes(), 'html.parser', from_encoding='utf-8')
    entry = soup.select_one('.entry-content') or soup.body
    start = entry.find('h1')
    active = start is None
    heading = 'Overview'
    sections = {}
    for element in entry.find_all(['h1', 'h2', 'h3', 'p', 'li']):
        if element == start:
            active = True
            continue
        if not active or element.find_parent(['nav', 'header', 'footer', 'aside']):
            continue
        text = ' '.join(element.get_text(' ', strip=True).split())
        if element.name in ('h2', 'h3'):
            heading = text
        elif len(text) > 70 and not element.find(['p', 'li']):
            sections.setdefault(heading, []).append(text)
    title = soup.find('h1') or soup.title
    title = ' '.join(title.get_text(' ', strip=True).split())
    return soup, title, sections


def prepare(refresh=False):
    OUT.mkdir(parents=True, exist_ok=True)

    def fetch(item):
        key, url = item
        path = OUT / (key + '.html')
        if refresh or not path.exists():
            with urllib.request.urlopen(url, timeout=45) as response:
                payload = response.read(5_000_001)
            if len(payload) > 5_000_000:
                raise ValueError('Page exceeds 5 MB limit: ' + key)
            path.write_bytes(payload)
        return key, extract(path)

    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        pages = dict(pool.map(fetch, URLS.items()))
    blocks = {key: value[2] for key, value in pages.items()}
    facts = blocks['mars-facts']
    sheet = pages['mars-factsheet'][0]
    # Fail if the upstream values used for typed resources no longer match.
    gravity_row = next(row for row in sheet.select('tr') if 'Surface gravity (mean)' in row.get_text())
    assert gravity_row.select('td')[0].get_text(strip=True) == '3.73'
    assert '6.36 mb at mean radius' in sheet.get_text()
    assert '3,390 kilometers' in facts['Size and Distance'][0]
    for key in ['Structure', 'Atmosphere', 'Water on Mars', 'A Very Large Canyon']:
        assert key in facts

    pack = {'key': 'nasa-english-starter-v1', 'language': 'en', 'sources': [], 'imports': [], 'resources': [], 'articles': []}
    for key, (soup, title, sections) in pages.items():
        excerpt = '\n\n'.join(h + '\n' + '\n\n'.join(paragraphs) for h, paragraphs in sections.items())
        if key in ('mars-factsheet', 'valles'):
            excerpt = soup.get_text('\n', strip=True)
        assert excerpt and '\ufffd' not in excerpt, 'Missing or corrupt text: ' + key
        author = 'Dr. David R. Williams' if key == 'mars-factsheet' else 'NASA (corporate publisher; individual byline requires verification)'
        organization = 'NASA Goddard Space Flight Center' if key == 'mars-factsheet' else 'NASA'
        pack['sources'].append({
            'key': key, 'retrievedAt': dt.datetime.fromtimestamp((OUT / (key + '.html')).stat().st_mtime, dt.timezone.utc).isoformat(),
            'metadata': {'title': title, 'url': URLS[key], 'authors': [{'name': author, 'organization': organization}],
                         'organization': organization, 'sourceType': 'WebPage', 'excerpt': excerpt,
                         'usageNote': 'Original English NASA text archived for editorial review. Verify authorship and any third-party rights before publication.',
                         'providerMetadata': {'language': 'en', 'extraction': 'HTML to plain text; whitespace normalized; no translation',
                                              'individualBylineVerified': key == 'mars-factsheet'}},
            'files': [{'path': key + '.html', 'name': 'original-page', 'url': URLS[key], 'mimeType': 'text/html'}]})

    for key, connector, ids in [
        ('ntrs', 'ntrs', ['20240012934', '20205008958', '20230016438']),
        ('images', 'images', ['PIA00407', 'PIA02980', 'PIA01685', 'PIA15443', 'PIA02005', 'PIA01167', 'PIA23239', 'PIA24096', 'PIA19808', 'PIA24487']),
        ('meda-pressure', 'pds', ['urn:nasa:pds:mars2020_meda:data_calibrated_env:we__0015___________cal_ps__________________p::2.0'])
    ]:
        pack['imports'].append({'key': key, 'request': {'connectorKey': connector, 'recordIds': ids, 'pageSize': len(ids), 'maxItems': len(ids)}})
    pack['imports'].append({'key': 'mola', 'request': {'connectorKey': 'mars-trek', 'layerKey': 'mola-color-shade', 'pageSize': 1, 'maxItems': 1}})

    def resource(key, kind, source, data, **links):
        pack['resources'].append({'key': key, 'kind': kind, 'sourceKey': source, 'data': data, **links})

    for key, mission_type, status, launch, section in [
        ('mariner-4', 'Flyby', 'Completed', '1964-11-28T14:22:01Z', 'What was Mariner 4?'),
        ('viking-1', 'Other', 'Completed', '1975-08-20T21:22:00Z', 'What was Viking 1?'),
        ('curiosity', 'Rover', 'Unknown', None, 'Overview'),
        ('perseverance', 'Rover', 'Unknown', None, 'Objective'),
        ('mro', 'Orbiter', 'Unknown', None, 'Overview')
    ]:
        description = '\n\n'.join(blocks[key][section])
        resource(key, 'missions', key, {'nasaIdentifier': 'nasa-science:' + key, 'name': pages[key][1], 'agency': 'NASA',
            'missionType': mission_type, 'status': status, 'launchDate': launch, 'description': description,
            'statusVerifiedAt': None})

    for key, mission, when, title, quote in [
        ('mariner-launch', 'mariner-4', '1964-11-28T14:22:01Z', 'Mariner 4 launch', 'Nov 28, 1964 / 14:22:01 UT'),
        ('mariner-flyby', 'mariner-4', '1965-07-15T01:00:57Z', 'Mariner 4 closest approach to Mars', '01:00:57 UT July 15'),
        ('viking-launch', 'viking-1', '1975-08-20T21:22:00Z', 'Viking 1 launch', 'Aug. 20, 1975 / 21:22:00 UT'),
        ('viking-landing', 'viking-1', '1976-07-20T11:53:06Z', 'Viking 1 landing on Mars', '11:53:06 UT July 20, 1976')
    ]:
        text = pages[mission][0].get_text(' ', strip=True)
        assert quote in text, 'Event locator changed: ' + key
        resource(key, 'timeline-events', mission, {'title': title, 'description': quote,
            'eventDate': when, 'isPlanned': False, 'agency': 'NASA'}, missionKey=mission)

    for key, source, name, lat, lon, context in [
        ('gale-site', 'gale', 'Gale Crater intended MSL landing site (2011)', -4.5, 137.4,
         'The intended landing site is at 4.5 degrees south latitude, 137.4 degrees east longitude.'),
        ('viking-site', 'viking-1', 'Viking 1 landing site, Chryse Planitia', 22.483, -47.94,
         '22.483 degrees north latitude and 47.94 degrees west longitude'),
        ('olympus-site', 'olympus', 'Olympus Mons southeast flank, THEMIS orbit 39041', 15.5617, -129.798,
         'Orbit Number: 39041 Latitude: 15.5617 Longitude: 230.202'),
        ('valles-site', 'valles', 'Valles Marineris proposed landing site (Clifford and George)', -4.1, -35.2,
         'Valles Marineris S. Clifford and J. George Latitude Longitude 4.1S 35.2W')
    ]:
        text = ' '.join(pages[source][0].get_text(' ', strip=True).split())
        assert context in text, 'Location evidence changed: ' + key
        resource(key, 'locations', source, {'name': name, 'slug': key + '-en', 'latitude': lat, 'longitude': lon,
            'coordinateSystem': 'Mars latitude; east-positive longitude normalized to -180..180; source datum requires review',
            'description': context + ('; stored longitude = 230.202 - 360 degrees.' if source == 'olympus' else '')})

    def fact(key, source, name, original, original_unit, value, unit, context, upper=None):
        resource(key, 'facts', source, {'name': name, 'originalValue': original, 'originalUnit': original_unit,
            'value': value, 'upperValue': upper, 'measurementKind': 'Range' if upper is not None else 'Estimate',
            'context': context, 'evidenceType': 'Unspecified', 'testEnvironment': 'Unspecified'}, unitSymbol=unit)

    fact('radius', 'mars-facts', 'Mars radius (rounded NASA overview value)', 3390, 'km', 3390000, 'm', facts['Size and Distance'][0])
    fact('core-radius', 'mars-facts', 'Mars core radius range', 1500, 'km', 1500000, 'm', facts['Structure'][0], 2100000)
    fact('mantle', 'mars-facts', 'Mars rocky mantle thickness range', 1240, 'km', 1240000, 'm', facts['Structure'][0], 1880000)
    fact('crust', 'mars-facts', 'Mars crust thickness range', 10, 'km', 10000, 'm', facts['Structure'][0], 50000)
    fact('gravity', 'mars-factsheet', 'Surface gravity (mean)', 3.73, 'm/s²', 3.73, 'm/s²', 'Mars Fact Sheet: Physical parameters; Surface gravity (mean). Preserve this definition rather than substituting an equatorial value.')
    fact('pressure', 'mars-factsheet', 'Surface pressure at mean radius', 6.36, 'mb', 636, 'Pa', 'Surface pressure: 6.36 mb at mean radius (variable from 4.0 to 8.7 mb depending on season). 1 mb = 100 Pa.')
    fact('average-temperature', 'mars-factsheet', 'Average atmospheric temperature', 214, 'K', 214, 'K', 'Mars atmosphere: Average temperature: ~214 K (-59 C). An overview estimate, not a timestamped measurement.')
    fact('diurnal-temperature', 'mars-factsheet', 'Diurnal temperature range at Viking 1 Lander site', 184, 'K', 184, 'K', 'Diurnal temperature range: 184 K to 242 K (-89 to -31 C) (Viking 1 Lander site).', 242)

    def article(slug, category, title, selections, resources):
        sections, claims, source_keys = [], [], []
        for index, (source, heading) in enumerate(selections):
            paragraphs = blocks[source][heading]
            sections.append({'anchor': 'section-' + str(index + 1), 'heading': heading, 'body': '\n\n'.join(paragraphs)})
            claims.append({'key': 'claim-' + str(index + 1), 'text': paragraphs[0], 'evidenceType': 'Unspecified',
                'testEnvironment': 'Unspecified', 'sourceKeys': [source], 'locator': {'heading': heading, 'url': URLS[source]}})
            source_keys.append(source)
        summary = blocks[selections[0][0]][selections[0][1]][0]
        assert len(summary) <= 2000
        pack['articles'].append({'draft': {'slug': slug, 'categorySlug': category, 'languageCode': 'en', 'title': title,
            'summary': summary, 'sections': sections, 'changeSummary': 'Initial English NASA source excerpts; original wording preserved; scientific review pending.'},
            'sourceKeys': list(dict.fromkeys(source_keys)), 'claims': claims, 'resourceKeys': resources})

    article('mariner-4-and-viking-1-en', 'exploration-history', 'Mariner 4 and Viking 1',
        [('mariner-4', 'What was Mariner 4?'), ('mariner-4', 'Results'), ('viking-1', 'What was Viking 1?'), ('viking-1', 'Results')],
        ['mariner-4', 'viking-1', 'mariner-launch', 'mariner-flyby', 'viking-launch', 'viking-landing', 'viking-site', 'image:PIA02980', 'image:PIA01685'])
    article('mars-structure-and-surface-en', 'structure-and-terrain', 'Mars Facts: Structure and Surface',
        [('mars-facts', h) for h in ['Formation', 'Structure', 'Surface', 'A Very Large Canyon', 'The Largest Volcano']],
        ['radius', 'core-radius', 'mantle', 'crust', 'olympus-site', 'valles-site', 'image:PIA00407', 'image:PIA15443', 'image:PIA02005', 'image:PIA01167', 'layer:Mars_MGS_MOLA_ClrShade_merge_global_463m'])
    article('mars-atmosphere-en', 'atmosphere-temperature-gravity', 'Mars Facts: Atmosphere',
        [('mars-facts', 'Atmosphere'), ('mars-facts', 'Magnetosphere')],
        ['gravity', 'pressure', 'average-temperature', 'diurnal-temperature', 'viking-site', 'image:PIA00407'])
    article('mars-water-and-jezero-en', 'water-and-life', 'Water on Mars and Jezero Crater',
        [('mars-facts', 'Water on Mars'), ('mars-facts', 'Potential for Life'), ('perseverance', 'Landing Site: Jezero Crater'), ('perseverance', 'Objective')],
        ['perseverance', 'mro', 'image:PIA23239', 'image:PIA24096'])
    article('mars-rovers-and-orbiter-en', 'missions-and-rovers', 'Mars Rovers and Mars Reconnaissance Orbiter',
        [('curiosity', 'Overview'), ('curiosity', 'Landing Site: Gale Crater'), ('perseverance', 'Objective'), ('perseverance', 'Science'), ('mro', 'Overview'), ('mro', 'Science')],
        ['curiosity', 'perseverance', 'mro', 'gale-site', 'image:PIA19808', 'image:PIA24487'])
    for item in pack['articles']:
        slug = item['draft']['slug']
        if slug == 'mars-atmosphere-en':
            item['sourceKeys'].append('ntrs:20240012934')
        if slug == 'mars-water-and-jezero-en':
            item['sourceKeys'].extend(['ntrs:20205008958', 'ntrs:20230016438'])
    target = OUT / 'pack.json'
    target.write_text(json.dumps(pack, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Prepared {len(pack["sources"])} archived pages, {len(pack["resources"])} typed resources, {len(pack["articles"])} English drafts and {len(pack["imports"])} NASA jobs.')
    print(target)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--refresh', action='store_true', help='Download pages again; otherwise reuse the saved capture.')
    prepare(parser.parse_args().refresh)
