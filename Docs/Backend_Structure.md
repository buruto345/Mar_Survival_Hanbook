# Sổ tay tích hợp backend Mars Survival

Đối chiếu ngày **14/11/2026**, theo source `7322c0b` và các lần gọi trực tiếp trên Render. Base URL staging là `https://mar-survival-hanbook.onrender.com`. Mọi đường dẫn bắt đầu bằng `/api/` trong sổ tay đều tính từ địa chỉ này.

| Bạn đang làm màn hình nào? | Đi tới |
|---|---|
| Kết nối API, xử lý trạng thái tải | [Bắt đầu](#start) |
| Trang chủ, danh mục, bài có ảnh, bản đồ | [Discover Mars](#discover) |
| Form tính tài nguyên theo sol | [Living on Mars](#living) |
| Đăng ký, đăng nhập, hồ sơ | [Tài khoản](#auth) |
| Bài kiến thức, bài tập, hồ sơ công nghệ | [Survival Knowledge](#knowledge) · [Human Survival](#human) · [Science and Technology](#science) |
| Soạn, duyệt, xuất bản và rút bài | [CMS](#cms) |
| Biên tập dữ liệu, quản lý nguồn và nhập NASA | [Quản trị](#admin) |
| Kiểu dữ liệu và query của API frontend | [Tra response public](#public-responses) |
| Chẩn đoán hoặc tra cứu một request | [Lỗi](#errors) · [Bằng chứng và nghiệm thu](#verify) · [Method + path](#routes) |

<a id="start"></a>
## Bắt đầu từ staging

Chạy frontend ở `http://localhost:5173`. Trong Console của trang frontend, chạy helper dưới đây một lần để dùng các ví dụ tiếp theo. Trong ứng dụng, đặt `base` trong cấu hình môi trường của frontend. Các API công khai chỉ cần URL HTTPS; backend trên Render quản lý kết nối Azure SQL.

```js
const base = globalThis.MARS_API_ORIGIN ?? "https://mar-survival-hanbook.onrender.com";
async function api(path, {method = "GET", body, token, etag, headers = {}} = {}) {
  const r = await fetch(new URL(path, base), {
    method,
    headers: {
      ...(body === undefined ? {} : {"Content-Type": "application/json"}),
      ...(token ? {Authorization: "Bearer " + token} : {}),
      ...(etag ? {"If-Match": etag.replace(/^W\//, "")} : {}), ...headers
    },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  const json = (r.headers.get("Content-Type") ?? "").includes("json");
  const data = json ? await r.json() : null;
  if (!r.ok) throw Object.assign(new Error(data?.title ?? r.statusText),
    {status: r.status, problem: data});
  return {data, etag: r.headers.get("ETag"), location: r.headers.get("Location")};
}
console.table((await api("/api/discover-mars")).data.categories);
```

Mỗi lần đổi bộ lọc, đặt trạng thái đang tải, đưa trang về 1 và bỏ kết quả của request cũ nếu nó về sau. Chỉ hiển thị “Chưa có nội dung” khi GET thành công và danh sách rỗng. Nếu fetch ném TypeError, kiểm tra Network và Console trước khi báo lỗi kết nối. Các trang danh sách bài dùng JSON dạng `{"items":[],"page":1,"pageSize":20,"total":0}`. Trang bắt đầu ở 1 (tối đa 100000), kích thước mặc định 20 (1–100), `q` tối đa 200 ký tự. Tổng số trang là `Math.ceil(total/pageSize)`. Danh mục và danh sách scenario trả mảng trực tiếp. ID đối tượng là UUID, số version của scenario là số nguyên, thời điểm có offset ISO 8601. Gửi enum dạng chuỗi theo schema, JSON camelCase, chỉ gửi trường được DTO chấp nhận.

**CORS.** Origin phát triển đã kiểm tra bằng Chrome là `http://localhost:5173`. Host `127.0.0.1`, HTTPS hoặc một cổng khác tạo ra origin khác. Trước khi phát hành frontend lên domain riêng, thêm origin chính xác vào `Cors:AllowedOrigins` của backend (ví dụ biến môi trường Render `Cors__AllowedOrigins__1` cho origin kế tiếp), deploy và thử lại từ domain đó. Backend cho phép header Authorization, Content-Type, If-Match và expose ETag, Location. Dùng Bearer token cho API yêu cầu đăng nhập.

### Kiểm tra kết nối bằng dữ liệu staging

Các link sau gọi được từ trình duyệt mà không cần tài khoản. Ở lần bàn giao, mỗi danh sách bên dưới có một bản ghi. Khi dựng UI, lấy slug từ `items[].slug` của phản hồi hiện tại để mở chi tiết. Cả ba cặp danh sách và chi tiết đã trả HTTP 200.

| Màn hình | Request danh sách | Ví dụ chi tiết đã xuất bản |
|---|---|---|
| Human Survival | [GET /api/human-survival/articles?language=en](https://mar-survival-hanbook.onrender.com/api/human-survival/articles?language=en) | [staging-crew-water](https://mar-survival-hanbook.onrender.com/api/human-survival/articles/staging-crew-water?language=en) |
| Living on Mars | [GET /api/living-on-mars/articles?language=en](https://mar-survival-hanbook.onrender.com/api/living-on-mars/articles?language=en) | [staging-water-loop](https://mar-survival-hanbook.onrender.com/api/living-on-mars/articles/staging-water-loop?language=en) |
| Science & Technology | [GET /api/science-technology/technologies?language=en](https://mar-survival-hanbook.onrender.com/api/science-technology/technologies?language=en) | [iss-water-recovery-system](https://mar-survival-hanbook.onrender.com/api/science-technology/technologies/iss-water-recovery-system?language=en) |

Nếu trang chi tiết có `media: []` hoặc thẻ có `thumbnailUrl: null`, vẫn hiển thị nội dung chữ. Thư viện ảnh có sẵn cho màn hình Discover được mô tả ngay bên dưới.

<a id="discover"></a>
## Discover Mars — từ danh mục đến bài có ảnh

| Thao tác | Request và đầu vào | Kết quả | Hiển thị hoặc bước kế |
|---|---|---|---|
| Vào trang | GET `/api/discover-mars?language=en` | Module gồm `categories, publishedArticleCount, coverUrl` | Render danh mục theo thứ tự trả về, dùng ảnh dự phòng khi URL null |
| Chọn danh mục | GET `categories/{slug}` và `articles?category={slug}&language=en` dưới cùng prefix, có thể gọi song song | Chi tiết danh mục và trang thẻ | Lấy slug từ `categories[].slug`; giữ language trên mọi lần chuyển trang |
| Lọc bài | GET `articles` với category, q, page, pageSize, sort | `items[].title, summary, thumbnailUrl, slug, articleVersionId` | Sort nhận publishedAtDesc (mặc định) hoặc relevance khi có q |
| Chọn thẻ | GET `articles/{slug}?language=en` | Snapshot công khai gồm sections, claims, media, sources | Render section theo thứ tự, dùng anchor làm mục lục và textContent cho body plainText |
| Xem ảnh hoặc bằng chứng | GET URL trong `media[].url`; GET `sources/{sourceId}/revisions/{sourceRevisionId}` từ cùng citation | Bytes theo MIME của ảnh; JSON citation | Giữ query của URL, altText, caption, credit; ghép claim với source theo revision ID và hiển thị locator |

Discover mặc định `vi`. Để chạy ví dụ trên staging đang có bài, dùng prefix Survival Knowledge dưới đây. Khi DB test có bài Discover tiếng Anh, chỉ đổi `modulePath` và chọn thẻ có thumbnail. Nếu bài vừa bị rút, GET có thể trả 404; quay lại danh sách và tải mới.

```js
async function openArticle(modulePath, language = "en") {
  const page = (await api(modulePath + "/articles?language=" + language)).data;
  const card = page.items.find(x => x.thumbnailUrl) ?? page.items[0];
  if (!card) return {empty: true};
  const {data: article} = await api(modulePath + "/articles/" +
    encodeURIComponent(card.slug) + "?language=" + language);
  const image = article.media.find(x => x.mediaType === "Image" && x.url);
  if (image) {
    const img = document.createElement("img");
    img.alt = image.altText ?? "";
    img.src = new URL(image.url, base).href;
    await img.decode();
    document.body.append(img);
  }
  const s = article.sources[0];
  const citation = s && (await api(modulePath + "/sources/" +
    s.sourceId + "/revisions/" + s.sourceRevisionId)).data;
  return {article, citation};
}
console.log(await openArticle("/api/survival-knowledge"));
```

**Ảnh bìa và thư viện Discover.** Gọi `GET /api/discover-mars/categories`. Mỗi danh mục có `imageUrl` (bìa, có thể null), `imageCredit` và `images[]` với `nasaId, title, credit, url`. Bìa là ảnh đầu tiên của danh mục. Chrome đã giải mã 11 URL trong các mảng images của 5 danh mục, tương ứng 10 ảnh riêng biệt vì hai nhóm dùng chung PIA00407. Các URL minh họa này truy cập được ngay cả khi danh sách bài xuất bản rỗng. Ảnh đính kèm của từng phiên bản bài nằm riêng trong `article.media[]`.

```js
const groups = (await api("/api/discover-mars/categories")).data;
for (const group of groups) {
  for (const photo of group.images) {
    const figure = document.createElement("figure");
    const img = document.createElement("img");
    img.src = new URL(photo.url, base).href;
    img.alt = photo.title;
    const caption = document.createElement("figcaption");
    caption.textContent = photo.title + " · " + photo.credit;
    figure.append(img, caption);
    document.body.append(figure);
  }
}
```

Khi màn hình đã có slug bài trong bảng dưới, `GET /api/discover-mars/articles/{articleSlug}/images` trả mảng cùng cấu trúc. Slug ngoài bộ minh họa trả 404. Dùng URL do API trả về để mở JPEG. Luôn ghép đường dẫn tương đối với **origin Render** để trình duyệt tải đúng máy chủ.

| Slug danh mục | Slug bài dùng cho route images | Số ảnh |
|---|---|---:|
| exploration-history | mariner-4-and-viking-1-en | 2 |
| structure-and-terrain | mars-structure-and-surface-en | 4 |
| atmosphere-temperature-gravity | mars-atmosphere-en | 1 |
| water-and-life | mars-water-and-jezero-en | 2 |
| missions-and-rovers | mars-rovers-and-orbiter-en | 2 |

**Màn hình dữ liệu Discover.** Các danh sách sau dùng prefix `/api/discover-mars`, có phân trang. Khi cần chi tiết trong ngữ cảnh bài, truyền cả `articleVersionId` từ item và ngôn ngữ hiện tại.

| Màn hình | Đầu vào | Dữ liệu cho UI |
|---|---|---|
| Timeline | GET timeline, from/to, includePlanned (mặc định false) | Sắp sự kiện theo eventDate; đánh dấu isPlanned |
| Nhiệm vụ | GET missions, status/type; GET missions/{id} từ item.id | Name, agency, trạng thái, ngày phóng, relatedArticles |
| Địa điểm | GET locations, bbox dạng west,south,east,north; GET locations/{id} | Latitude, longitude và coordinateSystem cho điểm trên bản đồ |
| Số liệu | GET facts với quantityKind hoặc evidenceType | Hiển thị value với unit, giữ originalValue/originalUnit, context và uncertainty |
| Chuỗi đo | GET measurements với factId từ fact hoặc missionId từ mission, bắt buộc cả from/to | Khoảng tối đa mặc định 31 ngày. Dùng observedAt, value, unit, qualityFlag; thiếu khoảng trả 400 |
| Lớp bản đồ | GET map-layers, sau đó GET map-layers/{id} | Dùng resourceUrlTemplate, tileMatrixSet, format, attribution và hệ tọa độ từ bản ghi |

<a id="living"></a>
## Living on Mars — dựng form và chạy preview

| Người dùng | Request | Kết quả | UI xử lý |
|---|---|---|---|
| Mở mô phỏng | GET `/api/living-on-mars/scenarios` | Mảng các bản truy cập được, có scenarioKey, version, title | Rỗng thì tắt nút chạy. Cho chọn một item |
| Chọn scenario | GET `scenarios/{scenarioKey}/versions/{version}` | ScenarioPack với stocks, facilities, parameters, solRules và giới hạn | Gắn form với đúng cặp key/version; bỏ kết quả cũ khi đổi scenario |
| Chạy | POST `simulations/preview`, JSON tối đa 32768 byte | 200 với engineVersion, scenarioVersion, firstShortage và sols | Vẽ balances từng sol, needs và limitingFactors. firstShortage có thể null |
| Đổi đầu vào | Gửi lại cùng cặp key/version | Tính lại, không lưu kết quả | Sai đơn vị trả 400; giữ giá trị nhập và hiện lỗi. Bản bị rút trả 404, tải lại danh sách rồi yêu cầu chọn lại |

Dựng ô crewSize và durationSols từ mặc định và max trong pack. Stock dùng `{quantity,unit}` theo stocks[key].unit và capacity. Số thiết bị theo facilities[key].defaultCount/maxCount. powerPriority phải đủ các khóa một lần; crewAllocations tính người-giờ của cả đoàn mỗi sol. housingToBuild là số habitat xây mới, cropArea tính m². Các override có thể bỏ qua. Hiển thị nguồn, basis, assumptionNote bên cạnh parameter. Ví dụ chạy dưới đây dùng 1 sol, các thông số còn lại lấy mặc định.

```js
async function simulate(overrides = {}) {
  const root = "/api/living-on-mars";
  const [choice] = (await api(root + "/scenarios")).data;
  if (!choice) return {empty: true};
  const pack = (await api(root + "/scenarios/" + encodeURIComponent(choice.scenarioKey)
    + "/versions/" + choice.version)).data;
  return (await api(root + "/simulations/preview", {method: "POST", body: {
    ...overrides, scenarioKey: pack.scenarioKey, scenarioVersion: pack.version
  }})).data;
}
console.log(await simulate({durationSols: 1}));
```

Trên DB có pack outpost, thử `simulate({initialStocks:{water:{quantity:1,unit:"invalid"}}})` để hiển thị lỗi. Trích response local thành công cho outpost 2 có `{"scenarioVersion":2}` và một phần tử trong `sols`. Ví dụ chi tiết outpost 1 và lệnh tạo dữ liệu có trong [runbook Living](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Living_On_Mars_Backend.md). Trang bài viết của Living dùng cùng chuỗi category → articles → article → citation như [Discover](#discover), với language=en.

<a id="auth"></a>
## Tài khoản và quyền

| Màn hình / thao tác | Khách | Viewer | Admin |
|---|---|---|---|
| Nội dung công khai và simulation preview | Có | Có | Có |
| Đăng ký và đăng nhập | Có | Có | Có |
| GET /api/auth/me | 401 | Hồ sơ của mình | Hồ sơ của mình |
| Tất cả /api/admin, gồm đọc, sửa, duyệt, publish, health | 401 | 403 | Có, theo trạng thái nghiệp vụ |

| Thao tác | Request | Thành công | Nếu thất bại |
|---|---|---|---|
| Đăng ký tài khoản Viewer | POST /api/auth/register với username (3–50, chữ/số ASCII đầu, sau đó thêm . _ -), fullName (1–150), email (≤254), password (12–128) | 201 + Location và hồ sơ role Viewer | 400 khi gửi thêm role; 409 khi username/email trùng không phân biệt hoa thường |
| Đăng nhập | POST /api/auth/login với password và đúng một trong email hoặc username | 200, accessToken, tokenType Bearer, expiresIn tính giây, profile | 400 khi gửi cả hai định danh, 401 invalid_credentials khi sai |
| Mở lại tài khoản | GET /api/auth/me với Authorization Bearer | 200 gồm id, username, fullName, email, role; tài khoản được provision có thể thiếu fullName/email | 401 khi token hết hạn, tài khoản bị vô hiệu hoặc đổi role; xoá phiên và yêu cầu đăng nhập lại |
| Vào CMS | Gọi GET danh sách với cùng token | Admin thấy trang quản trị | 403 với Viewer; đưa người dùng về màn hình có quyền |

Trong handler của form, dùng `await api("/api/auth/register",{method:"POST",body:{username,fullName,email,password}})`, rồi `const {data:session}=await api("/api/auth/login",{method:"POST",body:{username,password}})`. Khôi phục hồ sơ bằng `await api("/api/auth/me",{token:session.accessToken})`. Chặn double-click khi đang gửi. Giới hạn mặc định mỗi IP mỗi phút là 5 lần đăng ký và 10 lần đăng nhập. Bộ fixture tự tạo tài khoản Viewer/Admin trong DB test. [Kiểm thử đăng nhập](../Backend/Mars_Survival_Hanbook.Tests/AuthenticationHttpTests.cs)

<a id="knowledge"></a>
## Survival Knowledge — đọc bài theo chủ đề

| Thao tác | Request dưới /api/survival-knowledge | Response và bước sau |
|---|---|---|
| Chọn chủ đề | GET categories | Hiển thị cả nhóm có publishedArticleCount bằng 0. Dùng slug của nhóm được chọn |
| Duyệt bài | GET articles?category=dust-storms&q=dust&page=1&pageSize=20 | items[].slug và title để mở thẻ. Ví dụ slug trên staging là dust-storms-and-solar-power |
| Đọc và tra nguồn | GET articles/{slug}, sau đó GET sources/{sourceId}/revisions/{sourceRevisionId} | Đối chiếu claims[].sourceRevisionIds với sources[].sourceRevisionId; đặt locator.section bên cạnh URL trích dẫn |
| Quay lại sau khi gỡ bài | GET chi tiết có thể trả 404 | Bỏ thẻ cũ và refetch danh mục/danh sách; trang nguồn cũng có thể mất quyền truy cập |

Mặc định en. GET với language=vi có thể trả trang rỗng. Khi tạo draft chỉ hỗ trợ tiếng Anh. Ví dụ chạy được là `openArticle("/api/survival-knowledge")` ở trên. [Bộ bài và luồng CMS](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Survival_Knowledge_API.md)

<a id="human"></a>
## Human Survival — từ thẻ đến bài thực hành

| Thao tác | Request dưới /api/human-survival | Response và bước sau |
|---|---|---|
| Mở overview | GET overview | categories[] có slug, publishedArticleCount, articleSlug; khi duy nhất một bài có thể mở thẳng, khi null dựa vào count |
| Chọn nhóm | GET articles?category=teamwork&page=1&pageSize=20 | Dựng thẻ từ title, summary, thumbnailUrl (có thể null); lấy slug |
| Làm bài | GET articles/{slug} | Dùng sections theo thứ tự, hiện question trước explanation; nối claims với sources và locator. Chỉ hiện survivalKnowledgeUrl nếu có |
| Xem bằng chứng | GET sources/{sourceId}/revisions/{sourceRevisionId} | Hiển thị tác giả, URL, ghi chú; 404 thì cập nhật bài |

Chỉ hỗ trợ en, `?language=vi` trả 400. Bài staging đã xuất bản có slug `staging-crew-water`. Dùng [response overview](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Human_Survival_Examples/overview.json), [trang thẻ](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Human_Survival_Examples/teamwork-cards.json) và [chi tiết bài tập](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Human_Survival_Examples/teamwork-article.json) từ LocalDB để dựng giao diện. [Runbook Human](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Human_Survival_Runbook.md)

<a id="science"></a>
## Science and Technology — lọc thẻ và xem hồ sơ

| Thao tác | Request dưới /api/science-technology | Response và bước sau |
|---|---|---|
| Chọn nhóm | GET module ở prefix gốc | categories[].slug để lọc, publishedArticleCount để hiển thị số lượng |
| Lọc | GET technologies?category=water-recycling-systems&page=1&pageSize=20 | Trang items gồm id, slug, title, summary, categorySlug, technologyStatus, testEnvironment |
| Mở hồ sơ | GET technologies/{slug} từ item | Render sections problem/principle; technologyProfile chứa capabilities, limitations, resources, hazards, evidenceType và testEnvironment |
| Tra số liệu và nguồn | Trong detail, duyệt facts và sources, rồi GET sources/{sourceId}/revisions/{sourceRevisionId} | Đọc originalValue/originalUnit và uncertainty trên fact. Tra vị trí bằng technologyProfile.factLocators[fact.id]. facts có thể rỗng |

Danh mục Science ngoài taxonomy trả 400. Endpoint technologies chỉ lọc category và phân trang; q không lọc nội dung thẻ. Chỉ hỗ trợ en; `?language=vi` trả 400. Staging có thẻ `iss-water-recovery-system`. Xem [JSON thẻ](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Science_Examples/water-cards.json), [JSON hồ sơ ISS](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Science_Examples/water-profile.json) và [runbook tạo bộ dữ liệu](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Science_And_Technology_Runbook.md).

<a id="cms"></a>
## CMS — lưu, duyệt và phát hành

Mọi thao tác trong bảng yêu cầu Bearer Admin. Prefix là `/api/admin/{module}`, với module lấy từ URL màn hình. Từ màn hình danh sách, GET articles (lọc category, publicationStatus, reviewState, requiresReview) hoặc GET reviews (state, reviewerId, articleId, dueBefore). Lấy ETag **nguyên cả dấu nháy** từ header của GET chi tiết. Danh sách không cấp ETag riêng cho từng hàng.

| Hành động | Request, dữ liệu đầu vào | Trạng thái, response và ETag | Ảnh hưởng public |
|---|---|---|---|
| Tạo | POST articles với ArticleDraftRequest | 201, id, currentDraftVersionId, ETag bài, Location bài | Chưa hiện |
| Lưu | PUT articles/{id}/draft, toàn bộ form, If-Match bài | 201, version mới, ETag bài, Location version. Task đang mở của version trước bị hủy | Bản đã publish tiếp tục phục vụ |
| Soát | GET articles/{id}/versions, chọn items[].id; GET versions/{versionId} và versions/{versionId}/preview dưới URL bài | Lịch sử phân trang, version có snapshotHash và ETag version, preview là chi tiết để đọc | Chỉ Admin thấy |
| Xác nhận tiếng Anh cho Living | POST versions/{versionId}/english-review dưới URL bài, If-Match **version**, body snapshotHash và allTextReviewed=true | 204 rỗng. Trước xác nhận, GET version/preview có thể chỉ trả fieldsToReview; duyệt bản export theo hash | Chưa phát hành |
| Gửi duyệt | POST articles/{id}/versions/{versionId}/submit, If-Match **bài**, không body | 201, version InReview, task Pending, response.id và ETag **task**, Location review | Bài mới vẫn ẩn |
| Nhận duyệt | POST reviews/{taskId}/claim, If-Match task | 200, Pending → InProgress, ETag task mới | Như trước |
| Quyết định | POST reviews/{taskId}/decision với ETag sau claim, decision, notes và checklist | 200, Approved / ChangesRequested / Rejected trên task và version. Nếu trả bài, tác giả lưu draft mới và gửi lại | Approved vẫn chưa hiện công khai |
| Phát hành | GET lại articles/{id}; POST articles/{id}/publish với ETag bài và versionId vừa Approved | 200, status Published, publishedVersionId trỏ đúng version | Đọc lại detail theo slug để thấy snapshot mới |
| Gỡ | POST articles/{id}/unpublish với ETag bài và reason | 200, status Draft; publishedVersionId có thể còn giá trị lịch sử | Bài rời danh sách, detail trả 404 |
| Khôi phục | POST articles/{id}/restore với ETag bài, sourceVersionId từ lịch sử, changeSummary | 201, currentDraftVersionId mới và ETag bài. Duyệt lại, kể cả xác nhận tiếng Anh ở Living | Chỉ đổi bản công khai sau publish |

Ví dụ từ màn hình duyệt, sau khi đã lưu form, lấy `articleId` và `token` từ phiên hiện tại. Chạy từng bước sau khi người dùng chọn thao tác trên DB test.

```js
async function submitDraft(root, articleId, token) {
  const path = root + "/articles/" + articleId;
  const {data, etag} = await api(path, {token});
  const versionPath = path + "/versions/" + data.currentDraftVersionId;
  return api(versionPath + "/submit", {method: "POST", token, etag});
}
async function claimTask(root, taskId, token) {
  const path = root + "/reviews/" + taskId;
  const {etag} = await api(path, {token});
  return api(path + "/claim", {method: "POST", token, etag});
}
```

Sau khi đã kiểm tra đủ năm mục, gửi body quyết định như sau.

```json
{"decision":"Approved","notes":"Reviewed the cited revision and units.","checklist":{"sourcesChecked":true,"claimsChecked":true,"unitsChecked":true,"evidenceChecked":true,"mediaRightsChecked":true}}
```

Trên Render, ETag có thể mang tiền tố `W/`. Backend hiện so sánh row version trong dấu ngoặc kép. Helper ở đầu tài liệu bỏ riêng tiền tố này khi tạo If-Match và giữ nguyên dấu ngoặc kép. Khi gặp 412, lấy lại ETag trước khi quyết định gửi lại.

Form soạn bài lấy categorySlug từ taxonomy cùng module. Slug dùng chữ thường ASCII, số và dấu gạch ngang, tối đa 200, title ≤300, summary ≤2000, sections có 1–100 phần tử (anchor, heading, body). Gắn claim với sourceRevisionIds và locator; các mảng resourceRefs chứa ID của **working resource**, mediaPlacements thêm displayOrder/caption. PUT là lưu toàn bộ draft. Khóa nút khi đang gửi; nếu mất response, GET bài, version và task trước khi gửi lại. Lưu hai lần có thể tạo hai version. Hai người cùng lưu bằng ETag cũ sẽ gặp 412. Xem [body draft đầy đủ](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Human_Survival_Examples/teamwork-draft.json); thay ID nguồn/tài nguyên bằng ID trên DB đang dùng. [DTO biên tập](../Backend/Mars_Survival_Hanbook/Contracts/DiscoveryContracts.cs)

- **Discover** cho phép nội dung theo ngôn ngữ của draft. **Survival Knowledge** và **Human Survival** tạo draft tiếng Anh; duyệt nội dung, số liệu và nguồn trước khi submit. Sau khi duyệt fact, lưu draft mới để chụp trạng thái Accepted.
- **Living** cần bản tiếng Anh khớp snapshotHash. Sau restore, thực hiện lại bước english-review. Một số GET quản trị lược trường văn bản cho tới khi duyệt.
- **Science** lấy hoặc tạo technologyId qua `/api/admin/science-technology/catalog`. GET catalog/{id} lấy gợi ý, GET catalog/resources và catalog/hazards lấy ID cho form. technologyProfile dùng danh mục hợp lệ, các section problem/principle, claim có citation và factLocators cho từng fact. Một định danh công nghệ có một hồ sơ chính thức. [Quy trình Science](https://github.com/buruto345/Mar_Survival_Hanbook/blob/7322c0be43e68681c7ba3d8f8a4fa755b126d435/Docs/Science_And_Technology_Runbook.md)

<a id="admin"></a>
## Quản trị nguồn, dữ liệu và import

Dùng prefix `/api/admin/discover-mars` và quyền Admin cho các màn hình dưới đây, kể cả nguồn được dùng trong module khác.

| Thao tác ở màn hình nguồn | Request và đầu vào | Kết quả, hành động tiếp |
|---|---|---|
| Tìm hoặc nhập nguồn | GET sources (provider, verificationStatus, q) hoặc POST sources/manual với url, title, authors[{name,organization}], organization, sourceType, excerpt, usageNote, retrievedAt và credit nếu có | 201 khi tạo revision, 200 khi tái sử dụng. Lấy sourceId và id của revision từ response |
| So sánh revision | GET sources/{sourceId}/revisions → GET source-revisions/{revisionId}; GET …/raw | Header ETag của revision; raw trả file manifest JSON. Muốn tải attachment, gửi ?blob= bằng giá trị files[].blob trong manifest |
| Duyệt | POST source-revisions/{id}/decision, If-Match revision, ví dụ {"decision":"Accepted","notes":"Verified locator and credit."} | Pending → Accepted hoặc Rejected. Kiểm lại currentAcceptedRevisionId trên source |
| Sửa metadata | POST …/{id}/amend với ETag revision, title, authors, organization, excerpt, usageNote, credit | 201 nếu revision mới, 200 khi trùng. Bản sửa chờ duyệt; bài đã phát hành vẫn ghim revision trước |
| Thu hồi | POST …/{id}/revoke với ETag revision đã Accepted và {"reason":"…"} | revokedAt có giá trị. Lượt GET kế tiếp ẩn bài, file và scenario phụ thuộc khi bằng chứng bắt buộc mất hiệu lực |

**Màn hình tài nguyên.** GET `content/{loại}` (phân trang, tùy chọn sourceId), chọn items[].id rồi GET chi tiết lấy ETag. POST vào danh sách để tạo (201); PUT vào chi tiết với If-Match của tài nguyên để thay dữ liệu (200); POST `{id}/archive` cùng ETag để lưu trữ. Với facts, POST `{id}/decision` cùng ETag fact và body decision/notes. Form chung phải giữ sourceRevisionId. Đối chiếu field bắt buộc, nullable, độ dài và enum theo [DTO tài nguyên](../Backend/Mars_Survival_Hanbook/Contracts/ContentContracts.cs).

| Loại | Trường để dựng và kiểm tra form |
|---|---|
| facts | name, value, unitId, originalValue/originalUnit, measurementKind, context, evidenceType, testEnvironment; upperValue, uncertainty khi có |
| measurements | factId, observedAt có múi giờ, instrument, qualityFlag và cặp giá trị/đơn vị. Giữ số gốc để giải thích phép quy đổi |
| missions | nasaIdentifier, name, agency, missionType, status; launchDate/endDate có thể trống |
| locations | name, slug, latitude, longitude, coordinateSystem |
| timeline-events | title, description, eventDate, isPlanned, agency và missionId nếu có |
| media | title, mediaType, url, originalUrl, altText, credit, usageNote; fileChecksum và mimeType cho file đã lưu |
| map-layers | name, serviceUrl, layerIdentifier, tileMatrixSet, format, attribution, style, resourceUrlTemplate, boundingBoxJson, coordinateSystem |

Lấy ID đơn vị từ bộ dữ liệu đã seed hoặc bản ghi có cùng đơn vị; API hiện chưa có route liệt kê toàn bộ đơn vị. Có thể sửa working resource trong khi public vẫn đọc dữ liệu đã chụp. Sau thay đổi, lưu, duyệt và publish version mới để cập nhật phần hiển thị trong bài.

| Thao tác nhập | Request và đầu vào | Kết quả / điều kiện retry |
|---|---|---|
| Chọn connector | GET connectors; GET connectors/{key} | Chọn key có isEnabled. PUT cùng URL với ETag connector và ConnectorRequest nếu chỉnh cấu hình |
| Xếp hàng | POST imports, header Idempotency-Key không rỗng ≤200 ký tự, body gồm connectorKey, query hoặc recordIds theo loại | 202, Location, ETag job và jobId. Lưu key cho cùng lần bấm. Cùng actor + connector + key + nội dung chuẩn hóa nhận lại job cũ; đổi body cùng key trả 409 |
| Theo dõi | GET imports/{jobId}, GET imports/{jobId}/items?page=1&pageSize=20 | Đọc status, recordsAccepted, recordsFailed, nextAttemptAt; từng item có status, errorCode, sourceId/sourceRevisionId. Poll khi Queued/Running |
| Thử lại | GET job lấy ETag mới, POST imports/{jobId}/retry với If-Match job | 202, job về Queued. Running, Queued, Cancelled trả 409. Chỉ item Failed có attemptCount <10 được đưa về Queued |
| Sử dụng dữ liệu | Mở revision từ item rồi duyệt và gắn vào draft | Succeeded hoặc PartiallySucceeded là kết quả xử lý job. Việc hiển thị public còn cần nguồn hợp lệ và bài đã publish |

Ví dụ build body với `connectorKey` từ hàng Ntrs đang bật: `{connectorKey,recordIds:["20210024855"],maxItems:1,pageSize:1}`. Tạo key một lần bằng `crypto.randomUUID()` rồi dùng `api(root+"/imports",{method:"POST",token,headers:{"Idempotency-Key":key},body})`. NTRS nhận ID chữ số; Images/Ntrs cần query hoặc recordIds; PDS cần LID/LIDVID, khi includeMembers cần from/to tối đa 31 ngày; MarsTrek cần layerKey đã cấu hình. maxItems 1–500, pageSize 1–100 và ≤maxItems, mặc định cả hai là 20. Health trả databaseReady, lastPoll của worker. [Controller import](../Backend/Mars_Survival_Hanbook/Controllers/Admin/DiscoveryImportsController.cs) · [Quy tắc nhận và retry](../Backend/Mars_Survival_Hanbook/Services/Ingestion/ImportService.cs)

<a id="errors"></a>
## Phục hồi theo thao tác

| Tín hiệu | Khi nào gặp | UI cần làm |
|---|---|---|
| 400 invalid_request | Sai kiểu, enum số, field lạ, pageSize=101, sai đơn vị hoặc giới hạn | Gắn errors vào field nếu có; giữ form để sửa |
| 401 / 403 | Sai mật khẩu, phiên mất hiệu lực / Viewer vào CMS | Đăng nhập lại khi 401, ẩn hành động cần Admin khi 403. Body có thể rỗng |
| 404 not_found | Slug sai, sai module, đã rút, version hoặc nguồn mất quyền truy cập | Tải lại danh sách, bỏ kết quả cũ, hiện nút quay lại |
| 409 conflict | Trùng tài khoản, task đã claim, trạng thái không cho retry, idempotency key khác body | Đọc lại thực thể; sửa định danh hoặc đổi thao tác theo trạng thái |
| 412 stale_etag / 428 if_match_required | Phiên bản bị người khác thay đổi / thiếu header | Giữ bản sửa, GET đúng đối tượng và ETag, cho người dùng so sánh rồi mới gửi |
| 422 publication_gate | Thiếu bằng chứng, checklist hoặc hash tiếng Anh không khớp | Hiện title, quay về revision/preview để sửa và duyệt lại |
| 429 / 5xx | Vượt giới hạn hoặc lỗi xử lý | Dừng gửi liên tục, đợi trước khi thử lại; lưu traceId nếu có. Với thao tác ghi mất response, kiểm tra trạng thái trước |

Ví dụ phần ổn định của lỗi phân trang là `{"status":400,"title":"Invalid request data.","code":"invalid_request"}`. Living có Problem Details cho lỗi quyền; ở module khác chỉ đọc JSON nếu Content-Type phù hợp. [Middleware và ETag](../Backend/Mars_Survival_Hanbook/Services/ApiSupport.cs)

<a id="verify"></a>
## Nghiệm thu và tái hiện

| Môi trường ngày 14/11/2026 | Cách kiểm | Kết quả |
|---|---|---|
| Development | Đối chiếu Swagger runtime với phụ lục method + path | 189 thao tác trên 167 đường dẫn, gồm 53 thao tác cho phép khách |
| Windows, SQL Server LocalDB riêng cho test | Từ gốc repo chạy `./scripts/test-discovery.ps1` | 87 pass, 0 fail, 0 skip; build 0 warning, 0 error; EF khớp migration. NASA dùng mô phỏng của repo trên cổng 4010 (log có 70 request) |
| DB test, luồng ghi | AuthenticationHttpTests; SurvivalDraftChangesRequireFreshArticleAndTaskEtags; FullWorkflowFreezesWorkingDataAndRevocationHidesEveryPublicRepresentation; IdempotencyIsScopedAndChangedBodiesConflict | Đã chạy tạo Viewer, kiểm tra quyền, submit/claim/quyết định, ghi trùng và sửa đồng thời. Draft 404 → publish 200, sửa working copy vẫn giữ snapshot, revoke → 404. ScienceHttpTests chạy cả restore; LivingHttpTests kiểm preview và thu hồi |
| Ảnh fixture SQL | PublishedMediaServesFrozenBytesAndRevocationImmediatelyBlocksTheFile trong DiscoveryHttpTests | Fixture tạo ảnh PNG 1×1, duyệt, publish bài tiếng Anh. GET URL ở media[0] trả 200 và đúng từng byte; revoke nguồn rồi GET cùng URL trả 404 |
| Render, Discover | `postman collection run "postman/collections/Discover Mars Smoke" --env-var baseUrl=https://mar-survival-hanbook.onrender.com --no-report-events` | 8 request, 20/20 assertion, exit 0; module/categories 200, tìm rỗng 200, pageSize/enum/range sai 400, CMS khách 401, slug thiếu 404 |
| Render, nội dung | GET danh sách → dùng slug thật mở chi tiết | Human, Living và Science mỗi module có 1 bản ghi mẫu; cả ba luồng trả 200. Survival Knowledge có 5 bài trong lượt kiểm tra trước. |
| Chrome | Từ http://localhost:5173 gọi Render | Đọc 5 danh mục Discover và giải mã 11 URL ảnh. JavaScript đọc được 401 với token sai. Preflight PUT cho phép Authorization, Content-Type và If-Match. |

[Fixture HTTP và ảnh](../Backend/Mars_Survival_Hanbook.Tests/DiscoveryHttpTests.cs) · [Kiểm thử Science](../Backend/Mars_Survival_Hanbook.Tests/ScienceHttpTests.cs) · [Fixture Living](../Backend/Mars_Survival_Hanbook.Tests/LivingHttpTests.cs) · [Script tái hiện toàn bộ](../scripts/test-discovery.ps1)

Trên DB cục bộ dùng riêng, Chrome đã chạy lại các khối JavaScript trong sổ tay. Tài khoản Admin đăng nhập bằng username, submit và claim với ETag, duyệt và publish bài có ảnh. Mở bài qua danh sách Discover tiếng Anh, trình duyệt giải mã ảnh PNG rộng 1 pixel, GET file trả 200 image/png và đọc được citation. Ví dụ simulate chạy với outpost 2 trả một sol. Đây là lần kiểm tra kỹ thuật; bước đọc độc lập của lập trình viên frontend vẫn còn mở.

**Sai khác giữa các nguồn.** Spec Discover chứa 113 operation, gồm cả Auth và Survival. Spec Human có 23, Living 26, Science 27. Collection sinh tự động Discover Mars API chỉ có 87 request (thiếu quyết định fact và các module bổ sung so với Swagger đầy đủ). Authentication Smoke, Discover Smoke, Survival, Human, Living và Science lần lượt có 6, 8, 16, 16, 5 và 17 request; các collection kịch bản có thể gọi lại cùng operation. [Spec lưu trong repo](../postman/specs) · [Collections](../postman/collections)

- Các spec cũ khác Swagger ở schema dùng chung. Trong Discover, kiểm lại technologyProfile, englishCitations, survivalKnowledgeUrl và relatedArticles[].url/languageCode; trong Living có khác biệt tương ứng ở ArticleDetailResponse, ArticleDraftRequest và RelatedArticle; Science khác survivalKnowledgeUrl của ArticleDetailResponse. Human khớp. OperationId của health thay đổi theo tên hàm do compiler sinh. Dùng method + path cho client.
- Filter Swagger mô tả 200 và ETag bài cho english-review; controller thực trả 204 và kiểm ETag version. Login hỗ trợ cả username dù description chỉ nhắc email. Swagger liệt kê lỗi chung cho nhiều route; bảng lỗi phía trên chỉ ra điều kiện áp dụng. Preview/version/review của Living có thể lược trường theo trạng thái. [OpenAPI filter](../Backend/Mars_Survival_Hanbook/Services/DiscoveryOpenApiFilter.cs) · [Controller bài và English review](../Backend/Mars_Survival_Hanbook/Controllers/Admin/DiscoveryArticlesController.cs)
- Muốn chốt nghiệm thu trình duyệt thật, ghi origin frontend và SHA từ Render Deploys, chạy GET công khai, GET me, GET đối tượng đọc ETag, PUT cùng If-Match trên DB test và mở ảnh từ URL trong bài. Hiện chưa có xác nhận từ một lập trình viên frontend độc lập. Người nghiệm thu dùng riêng hai ví dụ openArticle và simulate, ghi lại bước phải hỏi, URL, status và sửa mục tương ứng.
- Khi PR thay đổi route, DTO, policy, trạng thái, header hoặc lỗi, cập nhật cả chương nghiệp vụ và dòng trong phụ lục; chạy lại test liên quan. Sau deploy cập nhật SHA, ngày và các kết quả staging.


<a id="routes"></a>
## Phụ lục — tra theo method + path

Các dòng “Swagger Dev” xác nhận operation được đăng ký trên host Development ngày 14/11/2026; cột cuối ghi nhận ca kiểm thử cụ thể. Cả 53 thao tác cho phép khách đã được gọi trên Render ít nhất một lần; một số route chi tiết mới có bằng chứng cho trường hợp 404. Cột body dẫn tới tên schema trong components.schemas của spec cùng dòng. Các quy tắc trạng thái và header nằm ở chương liên kết.


<details>
<summary>Tra kiểu, giới hạn và enum của các body JSON</summary>

Các bảng dưới lấy từ Swagger Development. Dấu gạch ngang nghĩa là schema không khai báo thêm ràng buộc cho trường. Khi gửi request, áp dụng cả điều kiện nghiệp vụ ở chương tương ứng, chẳng hạn chọn đúng một định danh đăng nhập hoặc lấy unit từ scenario.

**ArticleDraftRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `technologyProfile` | `TechnologyProfileInput` | — |
| `categorySlug` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `180` |
| `slug` | `string` | bắt buộc, độ dài ≥ `1`, mẫu `^[a-z0-9-]{1,200}$` |
| `languageCode` | `string` | bắt buộc, độ dài ≥ `1`, mẫu `^[a-z]{2}(-[A-Za-z]{2})?$` |
| `title` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `300` |
| `summary` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2000` |
| `sections` | `SectionInput[]` | bắt buộc, số mục ≥ `1`, số mục ≤ `100` |
| `claims` | `ClaimInput[]` | bắt buộc, số mục ≤ `200` |
| `resourceRefs` | `ResourceRefs` | bắt buộc |
| `mediaPlacements` | `MediaPlacement[]` | bắt buộc, số mục ≤ `100` |
| `sourceRevisionIds` | `uuid[]` | bắt buộc, số mục ≤ `100` |
| `englishCitations` | `map<string, EnglishCitationDisplay>` | nullable, độ dài ≤ `100` |
| `relatedArticleIds` | `uuid[]` | bắt buộc, số mục ≤ `50` |
| `changeSummary` | `string` | nullable, độ dài ≤ `2000` |

**ClaimInput**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `key` | `string` | — |
| `text` | `string` | — |
| `evidenceType` | `EvidenceType` | — |
| `testEnvironment` | `TestEnvironment` | — |
| `sourceRevisionIds` | `uuid[]` | — |
| `locator` | `map<string, string>` | nullable |

**ConnectorRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `isEnabled` | `boolean` | — |
| `baseUrl` | `string` | — |
| `accessPolicy` | `string` | — |
| `rateLimitPerMinute` | `int32` | nullable |

**EnglishCitationDisplay**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `title` | `string` | — |
| `credit` | `string` | nullable |
| `usageNote` | `string` | nullable |

**EnglishReviewRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `snapshotHash` | `string` | — |
| `allTextReviewed` | `boolean` | — |

**EvidenceType** — `Unspecified`, `Observed`, `Tested`, `Modeled`, `Proposed`.

**FactRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `name` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `300` |
| `value` | `double` | bắt buộc |
| `upperValue` | `double` | nullable |
| `unitId` | `uuid` | bắt buộc |
| `originalValue` | `double` | bắt buộc |
| `originalUnit` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `64` |
| `measurementKind` | `MeasurementKind` | bắt buộc |
| `context` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `4000` |
| `assumptions` | `string` | nullable, độ dài ≤ `4000` |
| `uncertainty` | `string` | nullable, độ dài ≤ `2000` |
| `evidenceType` | `EvidenceType` | bắt buộc |
| `testEnvironment` | `TestEnvironment` | bắt buộc |
| `observedAt` | `date-time` | nullable |

**ImportRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `connectorKey` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `100` |
| `query` | `string` | nullable, độ dài ≤ `200` |
| `recordIds` | `string[]` | số mục ≤ `100` |
| `maxItems` | `int32` | ≥ `1`, ≤ `500` |
| `pageSize` | `int32` | ≥ `1`, ≤ `100` |
| `layerKey` | `string` | nullable, độ dài ≤ `100` |
| `includeMembers` | `boolean` | — |
| `pdsCollection` | `string` | độ dài ≤ `100` |
| `from` | `date-time` | nullable |
| `to` | `date-time` | nullable |

**LocationRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `name` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `slug` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `latitude` | `double` | bắt buộc |
| `longitude` | `double` | bắt buộc |
| `coordinateSystem` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `150` |
| `description` | `string` | nullable, độ dài ≤ `4000` |

**LoginRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `email` | `string` | nullable, độ dài ≤ `254` |
| `password` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `1024` |
| `username` | `string` | nullable, độ dài ≤ `50` |

**ManualSourceRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `url` | `string` | — |
| `title` | `string` | — |
| `authors` | `SourceAuthor[]` | — |
| `organization` | `string` | — |
| `sourceType` | `SourceType` | — |
| `excerpt` | `string` | — |
| `usageNote` | `string` | — |
| `retrievedAt` | `date-time` | — |
| `credit` | `string` | nullable |

**MapLayerRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `name` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `description` | `string` | nullable, độ dài ≤ `2000` |
| `serviceUrl` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2048` |
| `layerIdentifier` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `250` |
| `tileMatrixSet` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `format` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `100` |
| `attribution` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `1000` |
| `style` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `resourceUrlTemplate` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2048` |
| `boundingBoxJson` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `1000` |
| `coordinateSystem` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `150` |
| `locationId` | `uuid` | nullable |

**MeasurementKind** — `Unspecified`, `Single`, `Mean`, `Minimum`, `Maximum`, `Range`, `Estimate`.

**MeasurementRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `factId` | `uuid` | bắt buộc |
| `value` | `double` | bắt buộc |
| `upperValue` | `double` | nullable |
| `unitId` | `uuid` | bắt buộc |
| `originalValue` | `double` | bắt buộc |
| `originalUnit` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `64` |
| `measurementKind` | `MeasurementKind` | bắt buộc |
| `observedAt` | `date-time` | bắt buộc |
| `observationEndAt` | `date-time` | nullable |
| `instrument` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `context` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `4000` |
| `uncertainty` | `string` | nullable, độ dài ≤ `2000` |
| `evidenceType` | `EvidenceType` | bắt buộc |
| `testEnvironment` | `TestEnvironment` | bắt buộc |
| `missionId` | `uuid` | nullable |
| `locationId` | `uuid` | nullable |
| `qualityFlag` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `100` |
| `qualityNotes` | `string` | nullable, độ dài ≤ `2000` |
| `externalObservationKey` | `string` | nullable, độ dài ≤ `450` |

**MediaPlacement**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `mediaId` | `uuid` | — |
| `displayOrder` | `int32` | — |
| `caption` | `string` | nullable |

**MediaRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `nasaId` | `string` | nullable, độ dài ≤ `200` |
| `title` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `300` |
| `mediaType` | `MediaType` | bắt buộc |
| `url` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2048` |
| `originalUrl` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2048` |
| `credit` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `1000` |
| `usageNote` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `4000` |
| `altText` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `2000` |
| `mimeType` | `string` | nullable, độ dài ≤ `100` |
| `width` | `int32` | nullable |
| `height` | `int32` | nullable |
| `fileSizeBytes` | `int64` | nullable |
| `fileChecksum` | `string` | nullable, độ dài ≤ `64` |

**MediaType** — `Image`, `Video`, `Diagram`, `Audio`, `Document`.

**MissionRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `nasaIdentifier` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `150` |
| `name` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `250` |
| `agency` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `description` | `string` | nullable, độ dài ≤ `10000` |
| `missionType` | `MissionType` | bắt buộc |
| `status` | `MissionStatus` | bắt buộc |
| `launchDate` | `date-time` | nullable |
| `endDate` | `date-time` | nullable |
| `statusVerifiedAt` | `date-time` | nullable |

**MissionStatus** — `Planned`, `Active`, `Completed`, `Cancelled`, `Unknown`.

**MissionType** — `Orbiter`, `Lander`, `Rover`, `Flyby`, `HumanResearch`, `TechnologyDemonstration`, `Other`.

**PreviewRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `scenarioKey` | `string` | — |
| `scenarioVersion` | `int32` | — |
| `crewSize` | `int32` | nullable |
| `durationSols` | `int32` | nullable |
| `initialStocks` | `map<string, InitialStock>` | nullable |
| `facilities` | `map<string, int32>` | nullable |
| `powerPriority` | `string[]` | nullable |
| `crewAllocations` | `map<string, double>` | nullable |
| `housingToBuild` | `int32` | — |
| `cropArea` | `double` | — |
| `languageCode` | `string` | nullable |
| `language` | `string` | nullable |

**InitialStock**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `quantity` | `double` | — |
| `unit` | `string` | — |

**ReasonRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `reason` | `string` | — |

**RegisterRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `username` | `string` | bắt buộc, độ dài ≥ `3`, độ dài ≤ `50`, mẫu `^[A-Za-z0-9][A-Za-z0-9._-]*$` |
| `fullName` | `string` | bắt buộc, độ dài ≥ `0`, độ dài ≤ `150` |
| `email` | `email` | bắt buộc, độ dài ≥ `0`, độ dài ≤ `254` |
| `password` | `password` | bắt buộc, độ dài ≥ `12`, độ dài ≤ `128` |

**ResourceRefs**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `factIds` | `uuid[]` | số mục ≤ `100` |
| `measurementIds` | `uuid[]` | số mục ≤ `100` |
| `missionIds` | `uuid[]` | số mục ≤ `100` |
| `locationIds` | `uuid[]` | số mục ≤ `100` |
| `timelineEventIds` | `uuid[]` | số mục ≤ `100` |
| `mediaIds` | `uuid[]` | số mục ≤ `100` |
| `mapLayerIds` | `uuid[]` | số mục ≤ `100` |

**RestoreRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceVersionId` | `uuid` | — |
| `changeSummary` | `string` | — |

**ReviewDecision**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `decision` | `ReviewState` | — |
| `notes` | `string` | — |
| `checklist` | `ReviewChecklist` | — |

**ReviewChecklist**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourcesChecked` | `boolean` | — |
| `claimsChecked` | `boolean` | — |
| `unitsChecked` | `boolean` | — |
| `evidenceChecked` | `boolean` | — |
| `mediaRightsChecked` | `boolean` | — |
| `complete` | `boolean` | — |

**ReviewState** — `Pending`, `InProgress`, `Approved`, `ChangesRequested`, `Rejected`, `Cancelled`.

**SectionInput**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `anchor` | `string` | — |
| `heading` | `string` | — |
| `body` | `string` | — |

**SourceAmendmentRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `title` | `string` | — |
| `authors` | `SourceAuthor[]` | — |
| `organization` | `string` | — |
| `excerpt` | `string` | — |
| `usageNote` | `string` | — |
| `credit` | `string` | nullable |

**SourceAuthor**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `name` | `string` | — |
| `organization` | `string` | — |

**SourceDecision**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `decision` | `VerificationStatus` | — |
| `notes` | `string` | — |

**SourceType** — `WebPage`, `TechnicalReport`, `ResearchPaper`, `Dataset`, `Api`, `Media`, `MapLayer`.

**TechnologyIdentityRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `name` | `string` | — |
| `slug` | `string` | — |
| `description` | `string` | — |

**TechnologyProfileInput**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `technologyId` | `uuid` | — |
| `technologyStatus` | `TechnologyStatus` | bắt buộc |
| `evidenceType` | `EvidenceType` | — |
| `testEnvironment` | `TestEnvironment` | — |
| `capabilities` | `string` | — |
| `limitations` | `string` | — |
| `sourceRevisionIds` | `uuid[]` | — |
| `resources` | `ProfileResource[]` | — |
| `hazards` | `ProfileHazard[]` | — |
| `factLocators` | `map<string, string>` | nullable |

**ProfileHazard**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `hazardId` | `uuid` | — |
| `label` | `string` | — |
| `mitigation` | `string` | — |

**ProfileResource**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `resourceId` | `uuid` | — |
| `direction` | `ResourceDirection` | — |
| `label` | `string` | — |
| `note` | `string` | nullable |

**ResourceDirection** — `Input`, `Output`.

**TechnologyStatus** — `Proposed`, `InResearch`, `LaboratoryTested`, `Demonstrated`, `Operational`, `Retired`.

**TestEnvironment** — `Unspecified`, `Mars`, `Earth`, `InternationalSpaceStation`, `EarthAnalog`, `Simulation`, `DeepSpace`.

**TimelineEventRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `sourceRevisionId` | `uuid` | bắt buộc |
| `title` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `300` |
| `description` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `4000` |
| `eventDate` | `date-time` | bắt buộc |
| `endDate` | `date-time` | nullable |
| `isPlanned` | `boolean` | bắt buộc |
| `agency` | `string` | bắt buộc, độ dài ≥ `1`, độ dài ≤ `200` |
| `missionId` | `uuid` | nullable |

**VerificationStatus** — `Pending`, `Accepted`, `Rejected`.

**VersionRequest**

| Trường JSON | Kiểu | Ràng buộc khai báo trong schema |
|---|---|---|
| `versionId` | `uuid` | — |

</details>

### auth

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `POST /api/auth/login` | [Đăng nhập](#auth) | Công khai | LoginRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/auth/me` | [Khôi phục hồ sơ](#auth) | Viewer/Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `POST /api/auth/register` | [Tạo Viewer](#auth) | Công khai | RegisterRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Render 201 |

### discover-mars

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `GET /api/admin/discover-mars/articles` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 401 (khách) |
| `POST /api/admin/discover-mars/articles` | [Tạo mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/articles/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/articles/{id}/draft` | [Lưu draft mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/articles/{id}/publish` | [Phát hành version](#cms) | Admin | VersionRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/articles/{id}/restore` | [Tạo draft từ lịch sử](#cms) | Admin | RestoreRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/articles/{id}/unpublish` | [Gỡ bài](#cms) | Admin | ReasonRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/articles/{id}/versions` | [Xem lịch sử](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/articles/{id}/versions/{versionId}` | [Đọc snapshot và hash](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/articles/{id}/versions/{versionId}/preview` | [Xem trước](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/articles/{id}/versions/{versionId}/submit` | [Gửi duyệt](#cms) | Admin | — → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/connectors` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/connectors/{key}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/connectors/{key}` | [Lưu thay đổi](#admin) | Admin | ConnectorRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/facts` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/facts` | [Tạo mới](#admin) | Admin | FactRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/facts/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/facts/{id}` | [Lưu thay đổi](#admin) | Admin | FactRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/facts/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/facts/{id}/decision` | [Ghi quyết định](#admin) | Admin | SourceDecision → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/locations` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/locations` | [Tạo mới](#admin) | Admin | LocationRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/locations/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/locations/{id}` | [Lưu thay đổi](#admin) | Admin | LocationRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/locations/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/map-layers` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/map-layers` | [Tạo mới](#admin) | Admin | MapLayerRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/map-layers/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/map-layers/{id}` | [Lưu thay đổi](#admin) | Admin | MapLayerRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/map-layers/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/measurements` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/measurements` | [Tạo mới](#admin) | Admin | MeasurementRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/measurements/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/measurements/{id}` | [Lưu thay đổi](#admin) | Admin | MeasurementRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/measurements/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/media` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/media` | [Tạo mới](#admin) | Admin | MediaRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/media/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/media/{id}` | [Lưu thay đổi](#admin) | Admin | MediaRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/media/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/missions` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/missions` | [Tạo mới](#admin) | Admin | MissionRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/missions/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/missions/{id}` | [Lưu thay đổi](#admin) | Admin | MissionRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/missions/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/timeline-events` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/timeline-events` | [Tạo mới](#admin) | Admin | TimelineEventRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/content/timeline-events/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/discover-mars/content/timeline-events/{id}` | [Lưu thay đổi](#admin) | Admin | TimelineEventRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/content/timeline-events/{id}/archive` | [Lưu trữ tài nguyên](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/health` | [Kiểm tra DB và worker](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/imports` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/imports` | [Tạo mới](#admin) | Admin | ImportRequest → 202 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/imports/{jobId}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/imports/{jobId}/items` | [Xem kết quả từng item](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/imports/{jobId}/retry` | [Xếp hàng lại job](#admin) | Admin | — → 202 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/reviews` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/reviews/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/reviews/{id}/claim` | [Nhận task](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/reviews/{id}/decision` | [Ghi quyết định](#cms) | Admin | ReviewDecision → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/source-revisions/{id}` | [Đọc revision và ETag](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/source-revisions/{id}/amend` | [Sửa metadata thành revision](#admin) | Admin | SourceAmendmentRequest → 201/200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/source-revisions/{id}/decision` | [Ghi quyết định](#admin) | Admin | SourceDecision → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/source-revisions/{id}/raw` | [Tải manifest hoặc blob](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/source-revisions/{id}/revoke` | [Thu hồi revision](#admin) | Admin | ReasonRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/sources` | [Duyệt danh sách](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/discover-mars/sources/manual` | [Nhập nguồn thủ công](#admin) | Admin | ManualSourceRequest → 201/200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/sources/{id}` | [Mở chi tiết](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/discover-mars/sources/{id}/revisions` | [Chọn revision](#admin) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/discover-mars` | [Mở tổng quan](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/discover-mars/articles` | [Duyệt thẻ bài](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/discover-mars/articles/{slug}` | [Mở bài theo slug](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 404 (slug thiếu) |
| `GET /api/discover-mars/articles/{articleSlug}/images` | [Ảnh Discover](#discover) | Công khai | — → 200 JSON | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/discover-mars/categories/{slug}/image` | [Ảnh Discover](#discover) | Công khai | — → 200 JPEG | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/discover-mars/categories/{slug}/images/{nasaId}` | [Ảnh Discover](#discover) | Công khai | — → 200 JPEG | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/discover-mars/categories` | [Chọn danh mục](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 5 mục |
| `GET /api/discover-mars/categories/{slug}` | [Xem danh mục](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/discover-mars/facts` | [Tra số liệu](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/discover-mars/locations` | [Lọc địa điểm](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/discover-mars/locations/{id}` | [Mở điểm bản đồ](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/discover-mars/map-layers` | [Chọn lớp bản đồ](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/discover-mars/map-layers/{id}` | [Xem lớp bản đồ](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/discover-mars/measurements` | [Lọc chuỗi đo](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 400 |
| `GET /api/discover-mars/media/{id}/file` | [Tải file ảnh](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Fixture SQL 200 → 404 |
| `GET /api/discover-mars/missions` | [Lọc nhiệm vụ](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/discover-mars/missions/{id}` | [Mở nhiệm vụ](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/discover-mars/sources/{id}/revisions/{revisionId}` | [Mở trích dẫn](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/discover-mars/timeline` | [Xem sự kiện](#discover) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 0 mục |

### living-on-mars

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `GET /api/admin/living-on-mars/articles` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles` | [Tạo mới](#cms) | Admin | ArticleDraftRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/living-on-mars/articles/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/living-on-mars/articles/{id}/draft` | [Lưu draft mới](#cms) | Admin | ArticleDraftRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles/{id}/publish` | [Phát hành version](#cms) | Admin | VersionRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles/{id}/restore` | [Tạo draft từ lịch sử](#cms) | Admin | RestoreRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles/{id}/unpublish` | [Gỡ bài](#cms) | Admin | ReasonRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/living-on-mars/articles/{id}/versions` | [Xem lịch sử](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/living-on-mars/articles/{id}/versions/{versionId}` | [Đọc snapshot và hash](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles/{id}/versions/{versionId}/english-review` | [Xác nhận hash bản tiếng Anh](#cms) | Admin | EnglishReviewRequest → 204 | [spec](../postman/specs/living-on-mars.openapi.json) | Controller |
| `GET /api/admin/living-on-mars/articles/{id}/versions/{versionId}/preview` | [Xem trước](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/articles/{id}/versions/{versionId}/submit` | [Gửi duyệt](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/living-on-mars/reviews` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/living-on-mars/reviews/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/reviews/{id}/claim` | [Nhận task](#cms) | Admin | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/living-on-mars/reviews/{id}/decision` | [Ghi quyết định](#cms) | Admin | ReviewDecision → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/living-on-mars` | [Mở tổng quan](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 |
| `GET /api/living-on-mars/articles` | [Duyệt thẻ bài](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 |
| `GET /api/living-on-mars/articles/{slug}` | [Mở bài theo slug](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 |
| `GET /api/living-on-mars/categories` | [Chọn danh mục](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 · 6 mục |
| `GET /api/living-on-mars/categories/{slug}` | [Xem danh mục](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 |
| `GET /api/living-on-mars/media/{id}/file` | [Tải file ảnh](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |
| `GET /api/living-on-mars/scenarios` | [Chọn scenario](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Render 200 · 0 mục |
| `GET /api/living-on-mars/scenarios/{key}/versions/{version}` | [Dựng form theo pack](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Chrome local 200 |
| `POST /api/living-on-mars/simulations/preview` | [Tính kết quả theo sol](#living) | Công khai | PreviewRequest → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Chrome local 200, 400 |
| `GET /api/living-on-mars/sources/{id}/revisions/{revisionId}` | [Mở trích dẫn](#living) | Công khai | — → 200 | [spec](../postman/specs/living-on-mars.openapi.json) | Swagger Dev |

### human-survival

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `GET /api/admin/human-survival/articles` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/articles` | [Tạo mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/articles/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `PUT /api/admin/human-survival/articles/{id}/draft` | [Lưu draft mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/articles/{id}/publish` | [Phát hành version](#cms) | Admin | VersionRequest → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/articles/{id}/restore` | [Tạo draft từ lịch sử](#cms) | Admin | RestoreRequest → 201 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/articles/{id}/unpublish` | [Gỡ bài](#cms) | Admin | ReasonRequest → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/articles/{id}/versions` | [Xem lịch sử](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/articles/{id}/versions/{versionId}` | [Đọc snapshot và hash](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/articles/{id}/versions/{versionId}/preview` | [Xem trước](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/articles/{id}/versions/{versionId}/submit` | [Gửi duyệt](#cms) | Admin | — → 201 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/reviews` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/admin/human-survival/reviews/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/reviews/{id}/claim` | [Nhận task](#cms) | Admin | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `POST /api/admin/human-survival/reviews/{id}/decision` | [Ghi quyết định](#cms) | Admin | ReviewDecision → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/human-survival` | [Mở tổng quan](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 |
| `GET /api/human-survival/articles` | [Duyệt thẻ bài](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 |
| `GET /api/human-survival/articles/{slug}` | [Mở bài theo slug](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 |
| `GET /api/human-survival/categories` | [Chọn danh mục](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 · 6 mục |
| `GET /api/human-survival/categories/{slug}` | [Xem danh mục](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 |
| `GET /api/human-survival/media/{id}/file` | [Tải file ảnh](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |
| `GET /api/human-survival/overview` | [Mở bài thực hành theo nhóm](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Render 200 |
| `GET /api/human-survival/sources/{id}/revisions/{revisionId}` | [Mở trích dẫn](#human) | Công khai | — → 200 | [spec](../postman/specs/human-survival.openapi.json) | Swagger Dev |

### science-technology

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `GET /api/admin/science-technology/articles` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/articles` | [Tạo mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/articles/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `PUT /api/admin/science-technology/articles/{id}/draft` | [Lưu draft mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/articles/{id}/publish` | [Phát hành version](#cms) | Admin | VersionRequest → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/articles/{id}/restore` | [Tạo draft từ lịch sử](#cms) | Admin | RestoreRequest → 201 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/articles/{id}/unpublish` | [Gỡ bài](#cms) | Admin | ReasonRequest → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/articles/{id}/versions` | [Xem lịch sử](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/articles/{id}/versions/{versionId}` | [Đọc snapshot và hash](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/articles/{id}/versions/{versionId}/preview` | [Xem trước](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/articles/{id}/versions/{versionId}/submit` | [Gửi duyệt](#cms) | Admin | — → 201 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/catalog` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/catalog` | [Tạo mới](#cms) | Admin | TechnologyIdentityRequest → 201 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/catalog/hazards` | [Chọn mối nguy](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/catalog/resources` | [Chọn tài nguyên](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/catalog/{id}` | [Điền gợi ý hồ sơ](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/reviews` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/admin/science-technology/reviews/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/reviews/{id}/claim` | [Nhận task](#cms) | Admin | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `POST /api/admin/science-technology/reviews/{id}/decision` | [Ghi quyết định](#cms) | Admin | ReviewDecision → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/science-technology` | [Mở tổng quan](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Render 200 |
| `GET /api/science-technology/categories` | [Chọn danh mục](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Render 200 · 5 mục |
| `GET /api/science-technology/categories/{slug}` | [Xem danh mục](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Render 200 |
| `GET /api/science-technology/media/{id}/file` | [Tải file ảnh](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/science-technology/sources/{id}/revisions/{revisionId}` | [Mở trích dẫn](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Swagger Dev |
| `GET /api/science-technology/technologies` | [Lọc thẻ công nghệ](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Render 200 |
| `GET /api/science-technology/technologies/{slug}` | [Mở hồ sơ](#science) | Công khai | — → 200 | [spec](../postman/specs/science-technology.openapi.json) | Render 200 |

### survival-knowledge

| Method + path | Màn hình / thao tác | Quyền | Body JSON → HTTP | Nguồn contract | Kiểm tra |
|---|---|---|---|---|---|
| `GET /api/admin/survival-knowledge/articles` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/articles` | [Tạo mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/articles/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `PUT /api/admin/survival-knowledge/articles/{id}/draft` | [Lưu draft mới](#cms) | Admin | ArticleDraftRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/articles/{id}/publish` | [Phát hành version](#cms) | Admin | VersionRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/articles/{id}/restore` | [Tạo draft từ lịch sử](#cms) | Admin | RestoreRequest → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/articles/{id}/unpublish` | [Gỡ bài](#cms) | Admin | ReasonRequest → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/articles/{id}/versions` | [Xem lịch sử](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/articles/{id}/versions/{versionId}` | [Đọc snapshot và hash](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/articles/{id}/versions/{versionId}/preview` | [Xem trước](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/articles/{id}/versions/{versionId}/submit` | [Gửi duyệt](#cms) | Admin | — → 201 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/reviews` | [Duyệt danh sách](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/admin/survival-knowledge/reviews/{id}` | [Mở chi tiết](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/reviews/{id}/claim` | [Nhận task](#cms) | Admin | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `POST /api/admin/survival-knowledge/reviews/{id}/decision` | [Ghi quyết định](#cms) | Admin | ReviewDecision → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/survival-knowledge` | [Mở tổng quan](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/survival-knowledge/articles` | [Duyệt thẻ bài](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 5 mục |
| `GET /api/survival-knowledge/articles/{slug}` | [Mở bài theo slug](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 × 5 |
| `GET /api/survival-knowledge/categories` | [Chọn danh mục](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 · 5 mục |
| `GET /api/survival-knowledge/categories/{slug}` | [Xem danh mục](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 |
| `GET /api/survival-knowledge/media/{id}/file` | [Tải file ảnh](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Swagger Dev |
| `GET /api/survival-knowledge/sources/{id}/revisions/{revisionId}` | [Mở trích dẫn](#knowledge) | Công khai | — → 200 | [spec](../postman/specs/discover-mars.openapi.json) | Render 200 × 5 |

<a id="public-responses"></a>
## Tra dữ liệu trả về của API public

Bảng gồm 53 thao tác cho phép khách và GET /api/auth/me cần Bearer token. Tên kiểu trong cột cuối được giải thích ngay dưới bảng. Dùng camelCase cho tham số query. Với API đọc bài, dùng slug do danh sách trả về.

| Request | Query theo Swagger | Body thành công |
|---|---|---|
| `POST /api/auth/register` | — | `UserProfile` (application/json) |
| `POST /api/auth/login` | — | `LoginResponse` (application/json) |
| `GET /api/auth/me` | — | `UserProfile` (application/json) |
| `GET /api/discover-mars/articles/{articleSlug}/images` | — | `DiscoverImageResponse[]` (application/json) |
| `GET /api/discover-mars/categories/{slug}/image` | — | `binary` (image/jpeg) |
| `GET /api/discover-mars/categories/{slug}/images/{nasaId}` | — | `binary` (image/jpeg) |
| `GET /api/discover-mars/timeline` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `TimelineEventResponsePageResult` (application/json) |
| `GET /api/discover-mars/missions` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `MissionResponsePageResult` (application/json) |
| `GET /api/discover-mars/locations` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `LocationResponsePageResult` (application/json) |
| `GET /api/discover-mars/facts` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `FactResponsePageResult` (application/json) |
| `GET /api/discover-mars/measurements` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `MeasurementResponsePageResult` (application/json) |
| `GET /api/discover-mars/map-layers` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `MapLayerResponsePageResult` (application/json) |
| `GET /api/discover-mars/missions/{id}` | articleVersionId, page, pageSize, q, language | `MissionResponse` (application/json) |
| `GET /api/discover-mars/locations/{id}` | articleVersionId, page, pageSize, q, language | `LocationResponse` (application/json) |
| `GET /api/discover-mars/map-layers/{id}` | articleVersionId, page, pageSize, q, language | `MapLayerResponse` (application/json) |
| `GET /api/discover-mars/articles` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `ArticleCardPageResult` (application/json) |
| `GET /api/discover-mars/articles/{slug}` | page, pageSize, q, language | `ArticleDetailResponse` (application/json) |
| `GET /api/discover-mars` | page, pageSize, q, language | `DiscoverModuleResponse` (application/json) |
| `GET /api/discover-mars/categories` | page, pageSize, q, language | `DiscoverCategoryResponse[]` (application/json) |
| `GET /api/discover-mars/categories/{slug}` | page, pageSize, q, language | `DiscoverCategoryResponse` (application/json) |
| `GET /api/discover-mars/sources/{id}/revisions/{revisionId}` | — | `Citation` (application/json) |
| `GET /api/discover-mars/media/{id}/file` | articleVersionId, page, pageSize, q, language | `binary` (application/octet-stream) |
| `GET /api/human-survival/overview` | — | `HumanOverviewResponse` (application/json) |
| `GET /api/human-survival/articles` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `ArticleCardPageResult` (application/json) |
| `GET /api/human-survival/articles/{slug}` | page, pageSize, q, language | `ArticleDetailResponse` (application/json) |
| `GET /api/human-survival` | page, pageSize, q, language | `HumanOverviewResponse` (application/json) |
| `GET /api/human-survival/categories` | page, pageSize, q, language | `HumanCategoryResponse[]` (application/json) |
| `GET /api/human-survival/categories/{slug}` | page, pageSize, q, language | `CategoryResponse` (application/json) |
| `GET /api/human-survival/sources/{id}/revisions/{revisionId}` | — | `Citation` (application/json) |
| `GET /api/human-survival/media/{id}/file` | articleVersionId, page, pageSize, q, language | `binary` (application/octet-stream) |
| `GET /api/living-on-mars/scenarios` | — | `JSON` (xem chương nghiệp vụ) |
| `GET /api/living-on-mars/scenarios/{key}/versions/{version}` | — | `ScenarioPack` (text/plain) |
| `POST /api/living-on-mars/simulations/preview` | — | `PreviewResult` (text/plain) |
| `GET /api/living-on-mars/articles` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `ArticleCardPageResult` (application/json) |
| `GET /api/living-on-mars/articles/{slug}` | page, pageSize, q, language | `ArticleDetailResponse` (application/json) |
| `GET /api/living-on-mars` | page, pageSize, q, language | `ModuleResponse` (application/json) |
| `GET /api/living-on-mars/categories` | page, pageSize, q, language | `CategoryResponse[]` (application/json) |
| `GET /api/living-on-mars/categories/{slug}` | page, pageSize, q, language | `CategoryResponse` (application/json) |
| `GET /api/living-on-mars/sources/{id}/revisions/{revisionId}` | — | `Citation` (application/json) |
| `GET /api/living-on-mars/media/{id}/file` | articleVersionId, page, pageSize, q, language | `binary` (application/octet-stream) |
| `GET /api/science-technology/technologies` | page, pageSize, q, language, category | `TechnologyCardPageResult` (application/json) |
| `GET /api/science-technology/technologies/{slug}` | — | `ArticleDetailResponse` (application/json) |
| `GET /api/science-technology` | page, pageSize, q, language | `ModuleResponse` (application/json) |
| `GET /api/science-technology/categories` | page, pageSize, q, language | `CategoryResponse[]` (application/json) |
| `GET /api/science-technology/categories/{slug}` | page, pageSize, q, language | `CategoryResponse` (application/json) |
| `GET /api/science-technology/sources/{id}/revisions/{revisionId}` | — | `Citation` (application/json) |
| `GET /api/science-technology/media/{id}/file` | articleVersionId, page, pageSize, q, language | `binary` (application/octet-stream) |
| `GET /api/survival-knowledge/articles` | category, sort, missionId, locationId, factId, from, to, includePlanned, bbox, status, type, quantityKind, evidenceType, page, pageSize, q, language | `ArticleCardPageResult` (application/json) |
| `GET /api/survival-knowledge/articles/{slug}` | page, pageSize, q, language | `ArticleDetailResponse` (application/json) |
| `GET /api/survival-knowledge` | page, pageSize, q, language | `ModuleResponse` (application/json) |
| `GET /api/survival-knowledge/categories` | page, pageSize, q, language | `CategoryResponse[]` (application/json) |
| `GET /api/survival-knowledge/categories/{slug}` | page, pageSize, q, language | `CategoryResponse` (application/json) |
| `GET /api/survival-knowledge/sources/{id}/revisions/{revisionId}` | — | `Citation` (application/json) |
| `GET /api/survival-knowledge/media/{id}/file` | articleVersionId, page, pageSize, q, language | `binary` (application/octet-stream) |

### Các kiểu dữ liệu public

Dấu `?` biểu thị giá trị có thể null theo schema. Với những kiểu được mô tả là JSON tự do, xem ví dụ theo màn hình ở các chương trên.

- **ArticleCardPageResult** — `items: ArticleCard[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **ArticleCard** — `id: uuid`, `slug: string`, `title: string`, `summary: string`, `categorySlug: string`, `languageCode: string`, `articleVersionId: uuid`, `version: int32`, `reviewedAt: date-time?`, `publishedAt: date-time?`, `thumbnailUrl: string?`.
- **ArticleDetailResponse** — `technologyProfile: TechnologyProfileInput`, `schemaVersion: int32`, `articleId: uuid`, `moduleSlug: string`, `categorySlug: string`, `languageCode: string`, `title: string`, `summary: string`, `sections: SectionInput[]`, `claims: ClaimInput[]`, `facts: FactResponse[]`, `measurements: MeasurementResponse[]`, `missions: MissionResponse[]`, `locations: LocationResponse[]`, `timelineEvents: TimelineEventResponse[]`, `media: MediaResponse[]`, `mapLayers: MapLayerResponse[]`, `sources: Citation[]`, `id: uuid`, `slug: string`, `articleVersionId: uuid`, `version: int32`, `reviewedAt: date-time?`, `publishedAt: date-time?`, `survivalKnowledgeUrl: string?`, `bodyFormat: string`, `tableOfContents: TableOfContentsEntry[]`, `relatedArticles: RelatedArticle[]`.
- **CategoryResponse** — `id: uuid`, `slug: string`, `name: string`, `description: string?`, `publishedArticleCount: int32`.
- **Citation** — `sourceId: uuid`, `sourceRevisionId: uuid`, `provider: SourceProvider`, `externalRecordId: string?`, `title: string`, `url: string`, `authors: SourceAuthor[]`, `organization: string`, `credit: string?`, `usageNote: string?`, `doi: string?`, `retrievedAt: date-time`.
- **ClaimInput** — `key: string`, `text: string`, `evidenceType: EvidenceType`, `testEnvironment: TestEnvironment`, `sourceRevisionIds: uuid[]`, `locator: object?`.
- **DiscoverCategoryResponse** — `id: uuid`, `slug: string`, `name: string`, `description: string?`, `publishedArticleCount: int32`, `imageUrl: string?`, `imageCredit: string?`, `images: DiscoverImageResponse[]`.
- **DiscoverImageResponse** — `nasaId: string`, `title: string`, `credit: string`, `url: string`.
- **DiscoverModuleResponse** — `id: uuid`, `name: string`, `slug: string`, `description: string?`, `categories: DiscoverCategoryResponse[]`, `publishedArticleCount: int32`, `coverUrl: string?`.
- **EvidenceType** — `Unspecified`, `Observed`, `Tested`, `Modeled`, `Proposed`.
- **FactResponse** — `sourceRevisionId: uuid`, `name: string`, `value: double`, `upperValue: double?`, `unitId: uuid`, `originalValue: double`, `originalUnit: string`, `measurementKind: MeasurementKind`, `context: string`, `assumptions: string?`, `uncertainty: string?`, `evidenceType: EvidenceType`, `testEnvironment: TestEnvironment`, `observedAt: date-time?`, `verificationStatus: VerificationStatus`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `unit: UnitResponse`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **FactResponsePageResult** — `items: FactResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **HumanCategoryResponse** — `id: uuid`, `slug: string`, `name: string`, `description: string?`, `publishedArticleCount: int32`, `articleSlug: string?`.
- **HumanOverviewResponse** — `id: uuid`, `name: string`, `slug: string`, `description: string?`, `categories: HumanCategoryResponse[]`, `publishedArticleCount: int32`, `coverUrl: string?`.
- **LocationResponse** — `sourceRevisionId: uuid`, `name: string`, `slug: string`, `latitude: double`, `longitude: double`, `coordinateSystem: string`, `description: string?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **LocationResponsePageResult** — `items: LocationResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **LoginResponse** — `accessToken: string`, `tokenType: string`, `expiresIn: int32`, `profile: UserProfile`.
- **MapLayerResponse** — `sourceRevisionId: uuid`, `name: string`, `description: string?`, `serviceUrl: string`, `layerIdentifier: string`, `tileMatrixSet: string`, `format: string`, `attribution: string`, `style: string`, `resourceUrlTemplate: string`, `boundingBoxJson: string`, `coordinateSystem: string`, `locationId: uuid?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **MapLayerResponsePageResult** — `items: MapLayerResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **MeasurementKind** — `Unspecified`, `Single`, `Mean`, `Minimum`, `Maximum`, `Range`, `Estimate`.
- **MeasurementResponse** — `sourceRevisionId: uuid`, `factId: uuid`, `value: double`, `upperValue: double?`, `unitId: uuid`, `originalValue: double`, `originalUnit: string`, `measurementKind: MeasurementKind`, `observedAt: date-time`, `observationEndAt: date-time?`, `instrument: string`, `context: string`, `uncertainty: string?`, `evidenceType: EvidenceType`, `testEnvironment: TestEnvironment`, `missionId: uuid?`, `locationId: uuid?`, `qualityFlag: string`, `qualityNotes: string?`, `externalObservationKey: string?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `unit: UnitResponse`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **MeasurementResponsePageResult** — `items: MeasurementResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **MediaResponse** — `sourceRevisionId: uuid`, `nasaId: string?`, `title: string`, `mediaType: MediaType`, `url: string`, `originalUrl: string`, `credit: string`, `usageNote: string`, `altText: string`, `mimeType: string?`, `width: int32?`, `height: int32?`, `fileSizeBytes: int64?`, `fileChecksum: string?`, `displayOrder: int32?`, `caption: string?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **MediaType** — `Image`, `Video`, `Diagram`, `Audio`, `Document`.
- **MissionResponse** — `sourceRevisionId: uuid`, `nasaIdentifier: string`, `name: string`, `agency: string`, `description: string?`, `missionType: MissionType`, `status: MissionStatus`, `launchDate: date-time?`, `endDate: date-time?`, `statusVerifiedAt: date-time?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **MissionResponsePageResult** — `items: MissionResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **MissionStatus** — `Planned`, `Active`, `Completed`, `Cancelled`, `Unknown`.
- **MissionType** — `Orbiter`, `Lander`, `Rover`, `Flyby`, `HumanResearch`, `TechnologyDemonstration`, `Other`.
- **ModuleResponse** — `id: uuid`, `name: string`, `slug: string`, `description: string?`, `categories: CategoryResponse[]`, `publishedArticleCount: int32`, `coverUrl: string?`.
- **PreviewResult** — `engineVersion: string`, `scenarioVersion: int32`, `firstShortage: Shortage`, `sols: SolResult[]`.
- **RelatedArticle** — `id: uuid`, `slug: string`, `title: string`, `articleVersionId: uuid`, `url: string?`, `languageCode: string?`.
- **ScenarioPack** — `scenarioKey: string`, `version: int32`, `engineVersion: string`, `languageCode: string`, `title: string`, `defaultCrewSize: int32`, `durationSols: int32`, `maxCrewSize: int32`, `maxDurationSols: int32`, `stocks: object`, `facilities: object`, `powerPriority: string[]`, `activityHoursPerPerson: object`, `parameters: object`, `solRules: string[]`, `minCropArea: double`, `maxCropArea: double`.
- **SectionInput** — `anchor: string`, `heading: string`, `body: string`.
- **Shortage** — `sol: int32`, `need: Demand`, `code: string`.
- **Demand** — `key: string`, `unit: string`, `requested: double`, `supplied: double`, `unmet: double`.
- **SolResult** — `sol: int32`, `balances: object`, `needs: Demand[]`, `facilities: Operation[]`, `generatedPower: double`, `batteryDischarged: double`, `batteryCharged: double`, `surplusPower: double`, `wastewaterCollected: double`, `uncollectedWater: double`, `activeHousing: int32`, `buildHoursRemaining: double`, `cropGrowthSols: double`, `harvest: double`.
- **Operation** — `key: string`, `unit: string`, `requested: double`, `processed: double`, `powerRequested: double`, `powerSupplied: double`, `unrecovered: double`, `limitingFactors: Limit[]`, `message: string?`.
- **Limit** — `code: string`, `unit: string`, `requested: double`, `available: double`.
- **SourceAuthor** — `name: string`, `organization: string`.
- **SourceProvider** — `ManualLegacy`, `Ntrs`, `Images`, `Pds`, `NasaWeb`, `MarsTrek`.
- **TableOfContentsEntry** — `anchor: string`, `heading: string`.
- **TechnologyCardPageResult** — `items: TechnologyCard[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **TechnologyCard** — `id: uuid`, `slug: string`, `title: string`, `summary: string`, `categorySlug: string`, `technologyStatus: string`, `testEnvironment: string`.
- **TechnologyProfileInput** — `technologyId: uuid`, `technologyStatus: TechnologyStatus`, `evidenceType: EvidenceType`, `testEnvironment: TestEnvironment`, `capabilities: string`, `limitations: string`, `sourceRevisionIds: uuid[]`, `resources: ProfileResource[]`, `hazards: ProfileHazard[]`, `factLocators: object?`.
- **ProfileHazard** — `hazardId: uuid`, `label: string`, `mitigation: string`.
- **ProfileResource** — `resourceId: uuid`, `direction: ResourceDirection`, `label: string`, `note: string?`.
- **ResourceDirection** — `Input`, `Output`.
- **TechnologyStatus** — `Proposed`, `InResearch`, `LaboratoryTested`, `Demonstrated`, `Operational`, `Retired`.
- **TestEnvironment** — `Unspecified`, `Mars`, `Earth`, `InternationalSpaceStation`, `EarthAnalog`, `Simulation`, `DeepSpace`.
- **TimelineEventResponse** — `sourceRevisionId: uuid`, `title: string`, `description: string`, `eventDate: date-time`, `endDate: date-time?`, `isPlanned: boolean`, `agency: string`, `missionId: uuid?`, `id: uuid`, `sourceId: uuid?`, `articleVersionId: uuid?`, `articleId: uuid?`, `source: Citation`, `relatedArticles: RelatedArticle[]?`.
- **TimelineEventResponsePageResult** — `items: TimelineEventResponse[]`, `page: int32`, `pageSize: int32`, `total: int32`.
- **UnitResponse** — `id: uuid`, `symbol: string`, `quantityKind: string`.
- **UserProfile** — `id: uuid`, `username: string`, `fullName: string?`, `email: string?`, `role: UserRole`.
- **UserRole** — `Viewer`, `Admin`.
- **VerificationStatus** — `Pending`, `Accepted`, `Rejected`.
