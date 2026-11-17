# Living on Mars article 3D upgrade

This report records the previous procedural implementation. The subsequent [article fidelity review](living-on-mars-article-fidelity.md) documents the current assets, measurements and remaining limitations.

## Coverage and reference

The approved outpost at commit `74cc712` is the visual baseline. Its fixtures meet their supporting structures, different finishes distinguish insulation from equipment, the construction bench sits within the terrain, and the camera fits the subject independently of its surroundings. Repeated geometry is instanced, shadows are cached and the renderer redraws on interaction. These principles apply to the article viewers at the scale of a crew member, a rack or a vehicle.

The inventory is the intersection of the Living reading-guide slugs and procedural scene builders, checked against the published Render article list. There are **13 scenes**. The local supplementary handbook JSON lists 12. The water recovery guide is registered separately. All entries below use the route `/handbook/living-on-mars/{slug}`, the `ReadingFigure` / `HabitatModel` components and the same Three.js viewer. The repository contains no imported GLB/GLTF astronaut or facility model for these articles.

## Inventory and completed changes

All rows retain drag and keyboard orbit, zoom, reset and the source and model notes. The last column is WebGL draw calls during one orbit redraw at 860 × 500 in Chrome using SwiftShader, before and after. It is not a frame-rate benchmark.

| Article | Slug | Crew | Original limitation | Implemented change | Draw calls |
|---|---|---|---|---|---:|
| What a day off looks like in an isolated habitat | `time-off-in-isolation` | Three seated crew | Figures faced away from the activity. The table lacked feet. | Flight clothing and bent limbs at the table and reading station, mug rims, reading rest, table supports and berth services. | 152 → 29 |
| Working together when Earth cannot answer immediately | `waiting-for-a-reply` | Two seated crew | Monitors and plaques hovered above desks and a curved tube linked the two sites. | Separate habitat and control-room bays with monitor feet, workstation cabinets, bulkhead-mounted signs and headset-equipped crew. | 68 → 29 |
| Turning loose trash into tiles | `trash-storage-tiles` | None | Feed pouches and tiles were placed beside a simple chamber without handling surfaces. | Input and output trays, rack feet, door latches, hinge pins and a discharge shelf distinguish loose feed from the stored tiles. | 118 → 20 |
| Why a space toilet separates waste streams | `separating-toilet-waste` | None | Collection and waste equipment appeared freestanding with incomplete support. | A service bulkhead, fixture mounting rails, two equipment trays, inspection cover, gauge and identification mounts distinguish the two streams. | 25 → 28 |
| Where Perseverance gets power after sunset | `perseverance-heat-and-power` | None | Rover on a rectangular slab with little structural context at the generator. | Generator interfaces, electrical enclosure, supported identification plate, suspension linkages and instrument deck on terrain. | 156 → 19 |
| What dust cost InSight’s solar panels | `dust-and-solar-budget` | None | Arrays used many separate arc meshes and lacked visible cable ties to the landers. | Cell sectors and curved grid lines share geometry. Each array connects to a lander, with readable clean and dusty states on the same surface. | 313 → 25 |
| Growing a salad inside a spacecraft | `veggie-salad` | None | Identical ellipsoids represented leaves and the growing volume lacked side walls. | Curved, folded blades with varied orientation, pillow trays, emitter canopy, side film, control enclosure and power feed. | 129 → 21 |
| Packing a meal for the last month on Mars | `packing-the-last-meal` | None | Pouches, category signs and shelf rows lacked a supporting stowage bay. | Corrected pouch elevations, shelf restraints, rear panels, sign brackets and differentiated foil and textile finishes. | 127 → 15 |
| How station water becomes breathing oxygen | `water-to-breathing-oxygen` | None | Water line terminated short of the stack and vessels were suspended. | Supply enclosure and feed coupling at the electrolyser, stack plinth, vessel saddles and gas-processing gauge. | 95 → 28 |
| How MOXIE made oxygen from Martian air | `oxygen-from-martian-air` | None | Detached enlarged MOXIE instrument stood in space beside the rover. | Instrument stand and housing reinforcement, rover suspension links, deck access covers, mast details and regolith. The enlarged instrument is an inset. | 179 → 21 |
| A room that travels folded | `expandable-room` | None | Two unsupported configurations over an empty slab. | Two sets of structural cradles show the compact and expanded envelopes at the same scale on a technical display deck. | 45 → 22 |
| Why a Mars analog gives each crewmate a door | `rooms-for-four` | Two standing crew | Compartments shared a single finish and had bare openings. | Berth lights, privacy curtains, vent grilles, a separate exercise station and two astronauts in the aisle. | 123 → 26 |
| Following water through a habitat | `staging-water-loop` | None | Collectors and visible plumbing lacked supports and a recognisable transfer pump. | Supported collectors, intake branches, manifold joints, gauges and an inline pump on a skid between the three racks. | 167 → 49 |

## Rendering and crew

The three exterior scenes use a graded equipment area, irregular terrain and instanced stone fragments, warm sunlight, shadows and distance fog. The other ten are indoor or technical cutaways on modular metal decking. Cabinet faces, structural metal, cloth and insulation use different roughness values and reusable procedural relief textures. The selected article alone allocates scene resources. Static duplicates are instanced by geometry and material, and instance buffers, textures and shadow resources are disposed on exit.

The seven crew placements use one original procedural flight-suit model with a soft communications cap, headset, microphone, collar, mission patch, coverall seams, belt, radio, cuffs and boots. Seated poses bend toward the desk and standing poses rest on the habitat floor. The figures are presented in stationary inspection poses. The three outdoor scenes show uncrewed robotic missions. The suit design, technical display mounts and simplified fluid paths are illustrative. The article text and linked NASA references remain the scientific sources.

## Source files

- `Frontend/MarsHandbook.Client/Models/LivingReadingGuides.cs` and `Components/ReadingFigure.razor` in the same client define the article mapping and figure captions.
- `Frontend/MarsHandbook.Client/Components/HabitatModel.razor` chooses the article or overview JavaScript entry point.
- `Frontend/MarsHandbook.Client/wwwroot/js/livingModels.js` owns the subject builders, reusable geometric primitives, camera, controls and disposal.
- `Frontend/MarsHandbook.Client/wwwroot/js/articleModels.js` supplies article-specific geometry, structural detailing, materials, environments and batching.
- `Frontend/MarsHandbook.Client/wwwroot/js/articleCrew.js` supplies the interior astronaut model.
- `Frontend/MarsHandbook.Client/wwwroot/js/story.js` independently renders the landing-page planet. Its renderer now uses the project's bundled Three.js modules.

## Verification

The 13 published article routes were opened using the Blazor frontend and the public Render API. Each rendered its figure and accepted zoom and reset. The renderer was also mounted individually for paired baseline and updated screenshots at the same yaw and pitch, plus a second orbit angle. Default framing was tightened for articles. The outpost overview screenshot was byte-identical in the controlled comparison and retained 152 draw calls. The home planet was checked independently for WebGL draws and local Three.js resource loading.

Release frontend build, JS syntax checks and whitespace checks passed. The backend suite reported 35 passes and 52 skips. The client has no TypeScript or npm lint script. Timing on physical laptop/mobile GPUs, full mechanical clearance and garment fit have not been measured.

For local article testing, Render returned HTTP 200 and the matching CORS header for `http://localhost:5173`. Requests with origins `http://127.0.0.1:5173` and `https://localhost:7031` returned HTTP 200 without that header, so a browser cannot read those cross-origin responses. This establishes a reproducible cause of a fetch failure on those origins. The origin of the originally reported failure was not recorded.
