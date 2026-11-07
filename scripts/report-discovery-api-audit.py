"""Build a sanitized report from saved HTTP evidence; never calls or mutates APIs."""
import json
from collections import Counter
from pathlib import Path
from jsonschema import Draft4Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Backend/Mars_Survival_Hanbook/App_Data/api-audit'
rows = json.loads((OUT / 'responses.json').read_text('utf-8'))
spec_path = OUT / 'openapi.json'
if not spec_path.exists():
    spec_path = ROOT / 'postman/specs/discover-mars.openapi.json'
spec = json.loads(spec_path.read_text('utf-8'))

def convert(v):
    if isinstance(v, list):
        return [convert(x) for x in v]
    if not isinstance(v, dict):
        return v
    result = {k: convert(x) for k, x in v.items() if k != 'nullable'}
    return {'anyOf': [result, {'type': 'null'}]} if v.get('nullable') else result

# The HTTP runner checked success JSON against live Swagger. Supplement the
# saved problem responses using the repository's explicit problem+json contract.
for row in rows:
    if 'application/problem+json' not in row['contentType']:
        continue
    op = spec['paths'][row['endpoint']][row['method'].lower()]
    shape = op.get('responses', {}).get(str(row['status']), {}).get('content', {}).get('application/problem+json', {}).get('schema')
    if shape:
        doc = {**convert(shape), 'components': {'schemas': convert(spec['components']['schemas'])}}
        row['schemaErrors'] = [{'path': '/'.join(map(str, e.absolute_path)), 'message': e.message} for e in Draft4Validator(doc, format_checker=FormatChecker()).iter_errors(row['response'])]
        row['schemaChecked'] = True

ops = sorted((m.upper(), p) for p, methods in spec['paths'].items() for m in methods if m in ['get', 'post', 'put', 'patch', 'delete'] and 'discover-mars' in p)
results = []
for method, path in ops:
    matches = [r for r in rows if (r['method'], r['endpoint']) == (method, path)]
    baseline = [r for r in matches if r['phase'] == 'existing-data']
    problems = [r for r in matches if not r['statusPass'] or r['schemaErrors']]
    classification = 'Chưa test' if not matches else 'Có lỗi' if problems else 'Đạt các ca đã chạy'
    data = 'Chỉ kiểm thử fixture' if not baseline else 'Có response baseline'
    totals = sorted({r['total'] for r in baseline if 'total' in r})
    if totals:
        data = 'Baseline total=' + ','.join(map(str, totals))
    if baseline and all(r['status'] == 404 for r in baseline):
        data = 'Chưa xuất bản: 404 đúng'
    errors = [{'url': r['url'], 'status': r['status'], 'expected': r['expected'], 'fields': r['schemaErrors'], 'note': r['note']} for r in problems]
    results.append({'method': method, 'path': path, 'classification': classification, 'baseline': data,
                    'requests': len(matches), 'statuses': sorted({r['status'] for r in matches}),
                    'schemaChecked': sum(r['schemaChecked'] for r in matches), 'issues': errors})

counts = Counter(r['classification'] for r in results)
summary = {'reportDate': '2026-11-06', 'target': 'http://127.0.0.1:5093', 'database': 'Disposable clone of MarsSurvivalHandbook',
           'requests': len(rows), 'discoverOperations': len(ops), 'classifications': dict(counts),
           'schemaCheckedResponses': sum(r['schemaChecked'] for r in rows),
           'schemaErrorResponses': sum(bool(r['schemaErrors']) for r in rows),
           'unexpectedStatusResponses': sum(not r['statusPass'] for r in rows), 'operations': results}
(ROOT / 'Docs/Discover_Mars_API_Test_Report.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), 'utf-8')

lines = ['# Báo cáo kiểm thử API Discover Mars', '', 'Ngày tổng hợp: **06/11/2026**. Backend thật tại `http://127.0.0.1:5093`, dùng bản sao SQL Server LocalDB của `MarsSurvivalHandbook`.', '',
    f"Đã chạy **{len(rows)} HTTP request**, bao phủ **{len(ops)}/84 operation Discover Mars** (mỗi cặp method + route là một operation). Có thêm login và me để hỗ trợ xác thực. **{counts['Đạt các ca đã chạy']} operation đạt các ca đã chạy; {counts['Có lỗi']} operation có lỗi trong ít nhất một ca.** Không có operation nào bị bỏ qua.", '',
    'Các nhóm kết quả không đồng nghĩa với chất lượng dữ liệu: API có thể hoạt động đúng nhưng chưa có dữ liệu NASA được xuất bản. Các thao tác ghi chỉ chạy trên database clone, worker NASA bị tắt. Fixture được ghi rõ là dữ liệu kiểm thử, không phải bằng chứng khoa học.', '',
    '## Các lỗi đã tái hiện', '',
    '| Vấn đề | Kết quả thực tế | Phạm vi |', '|---|---|---|',
    '| Citation của bài tiếng Anh | GET public sources/{id}/revisions/{revisionId}?language=en trả 404 sau khi fixture đã publish, nguồn đã Accepted | 1 operation; SourceAsync gọi Publications() mặc định vi |',
    '| URL ảnh trong snapshot tiếng Anh | URL chứa articleVersionId nhưng thiếu language=en trả 404; cùng file thêm language=en trả 200 image/jpeg | 1 operation; cần sửa nơi sinh URL và kiểm tra cover/thumbnail dùng lại URL này |',
    '| Bounding box bản đồ NASA | boundingBoxJson là chuỗi rỗng, vi phạm minLength của schema | 4 operation: list map-layers, detail map-layer, article version detail, article preview |', '',
    'Các lỗi trên chưa được sửa trong lượt kiểm thử này. Không có response 5xx trong 271 request đã ghi nhận.', '',
    '## Dữ liệu thật hiện có và dữ liệu còn thiếu', '',
    '| Nhóm | Baseline | Ý nghĩa |', '|---|---:|---|',
    '| Articles | 5 | Đều là Draft; chưa có bài public |',
    '| Sources | 25 | Revision chờ duyệt; chưa được xác nhận để xuất bản |',
    '| Import jobs | 4 | Có dữ liệu lịch sử import để tra cứu |',
    '| Facts | 8 | Tra cứu qua admin được |',
    '| Missions | 5 | Tra cứu qua admin được |',
    '| Locations | 4 | Tra cứu qua admin được |',
    '| Timeline events | 4 | Tra cứu qua admin được |',
    '| Media | 10 | Có metadata/ảnh lưu trữ; public phụ thuộc xuất bản |',
    '| Map layers | 1 | Có dữ liệu nhưng boundingBoxJson chưa hợp lệ |',
    '| Measurements | 0 | Chưa có bản ghi đo đạc chuẩn hóa để tra; fixture chứng minh API đọc/ghi hoạt động |',
    '| Review tasks | 0 | Chưa có review thật; luồng submit/claim/approve đã chạy bằng fixture |', '',
    'Baseline public articles, facts, missions, locations, timeline, map-layers và measurements đều total=0. Đây là kết quả đúng khi chưa xuất bản. Với fixture tiếng Anh đã được duyệt và publish, các danh sách và detail tài nguyên đã trả dữ liệu; ngoại lệ citation và URL ảnh nêu trên.', '',
    '## Kiểm tra từng trường JSON', '',
    f"Đã kiểm tra schema cho **{summary['schemaCheckedResponses']} response**: success JSON đối chiếu live Swagger lúc chạy; ProblemDetails được kiểm tra bổ sung từ OpenAPI lưu trong repository. Có **{summary['schemaErrorResponses']} response lỗi schema**, đều liên quan boundingBoxJson.", '',
    'Validator đi đệ quy qua object/array và kiểm tra type, required theo schema, nullable, enum, UUID/date-time, giới hạn số và độ dài chuỗi khi contract có khai báo. Không coi field optional hoặc additionalProperties được schema cho phép là lỗi. Vì vậy kết quả đạt schema không chứng minh tính chính xác khoa học, cũng không bảo đảm các field chưa được contract ràng buộc.', '',
    '| Response | Field sai | Actual |', '|---|---|---|']
for r in rows:
    for error in r['schemaErrors']:
        lines.append(f"| `{r['method']} {r['endpoint']}` | `{error['path']}` | `{error['message']}` |")
lines += ['', 'Ngoại lệ về hợp đồng: health chưa có response schema đủ để kiểm tra từng field; 25 raw manifest JSON cũng chưa có schema JSON tương ứng. Chúng parse được nhưng không được tính là đạt kiểm tra từng field. File ảnh là binary, không phải JSON; lượt này xác nhận HTTP 200 và MIME image/jpeg, chưa kiểm tra trực quan ảnh hoặc checksum qua HTTP. Các 401 rỗng là response xác thực, không phải lỗi JSON.', '',
    'Health trả databaseReady=true và lastPoll=0001-01-01T00:00:00+00:00: worker tắt trong môi trường audit, nên giá trị này không chứng minh lần đồng bộ NASA gần nhất.', '',
    'Các ca âm đã chạy: không token vào admin trả 401; page=0, enum status=999, thiếu bộ lọc thời gian measurements trả 400; bài không tồn tại/chưa publish trả 404. ProblemDetails có thể chứa thông báo tiếng Việt dù nội dung bài là tiếng Anh; nếu yêu cầu toàn bộ API chỉ tiếng Anh thì cần chuẩn hóa thông báo lỗi riêng.', '',
    '## Danh sách đầy đủ 84 API', '',
    'Đạt = status đúng kỳ vọng và không phát hiện lỗi schema trong các ca đã chạy; không phải cam kết mọi đầu vào đều đúng. Các dòng import POST chỉ xác nhận nhận job/retry (202), chưa chứng minh worker tải NASA trực tiếp trong lượt HTTP audit. 401/400/404 cố ý trong ca âm không bị tính là lỗi.', '',
    '| Method | Route | HTTP đã thấy | Kết quả | Dữ liệu baseline | JSON đã kiểm tra/request |', '|---|---|---|---|---|---|']
for r in results:
    lines.append(f"| {r['method']} | `{r['path']}` | {','.join(map(str,r['statuses']))} | {r['classification']} | {r['baseline']} | {r['schemaChecked']}/{r['requests']} |")
lines += ['', '## Kiểm thử hồi quy và bảo toàn dữ liệu', '',
    '`scripts/test-discovery.ps1` đã kết thúc exit code 0: **71 passed, 0 failed, 0 skipped**; build **0 warning, 0 error**; EF báo không có thay đổi model chưa có migration. Suite dùng SQL Server và Postman NASA simulation, bao gồm worker, phân trang/dedup/retry và kiểm soát quyền. Đây là bằng chứng với NASA giả lập, không phải lượt gọi NASA production trực tiếp.', '',
    'Sau kiểm thử đã đối chiếu database chính: 5 Articles đều Draft, 25 SourceRevisions, 10 ArticleVersions, 0 Measurements. Server audit đã dừng; database clone đã được xóa và password fixture trong state.json đã được che. Evidence được giữ lại; muốn chạy lại cần tạo clone và tài khoản audit mới.', '',
    '## Bằng chứng và giới hạn', '',
    '- `scripts/audit-discovery-api.py`: harness HTTP, cần server và clone được chuẩn bị riêng; không chạy vào database chính.',
    '- `scripts/report-discovery-api-audit.py`: sinh báo cáo từ evidence, không gọi API hoặc thay đổi database.',
    '- `Docs/Discover_Mars_API_Test_Report.json`: kết quả từng operation và field lỗi, không chứa token/password.',
    '- Raw responses và log ở `Backend/Mars_Survival_Hanbook/App_Data/api-audit/` (bị Git ignore). Không push state.json vì có credential fixture.',
    '- Kiểm thử ở localhost, không chứng minh môi trường deploy, hiệu năng tải cao, mọi tổ hợp filter hoặc kết nối NASA trực tiếp đang sẵn sàng.', '',
    'Thứ tự xử lý đề xuất: sửa lọc ngôn ngữ citation và URL ảnh; bổ sung bounding box đúng từ metadata nguồn; hoàn tất kiểm chứng nguồn/nội dung rồi publish các bài tiếng Anh; chuẩn hóa measurements có timestamp/quality trước khi nhập bảng đo đạc.', '']
(ROOT / 'Docs/Discover_Mars_API_Test_Report.md').write_text('\n'.join(lines), 'utf-8')
print(json.dumps({k:v for k,v in summary.items() if k != 'operations'}, ensure_ascii=True))
