# Living on Mars article model review

## Baseline and scope

The starting checkout was clean at `e9e37f3016e64f13e061e60b8848fa36dbeac5a5` on `feature(living-on-mar)--update-more-data`. The branch and its local origin tracking ref agreed. The 13 guide slugs still select 13 model builders through `ReadingFigure` and its keyed `HabitatModel`. The outpost has its own builder, terrain, camera and entry point. The starting wwwroot contained the Three.js r180 core and renderer, procedural article geometry, a shared materials cache, static instancing, an orthographic orbit camera and on-demand drawing. It had no local GLB, glTF loader, character skin or animation clips for articles.

## Inventory and visual assessment

Each slug below is served by `/handbook/living-on-mars/{slug}`. The ratings reflect the observed desktop renders, including an alternate orbit and zoom. Columns are shape fidelity, physical integration, material realism, environment, human realism, functional visualization and technical quality on a five-point scale. A 3 denotes a visible limitation at inspection distance. The latter columns do not certify the scientific accuracy of undocumented internals.

| Article / primary model | Changes visible in the viewer | Shape | Integration | Materials | Environment | Human | Function | Technical |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| `rooms-for-four` · CHAPEA berths | Fitted quilts, pillows, folded curtains, drawers, ventilation, backed printed walls and standing crew. The exercise station is still sparse. | 4 | 4 | 3 | 3 | 3 | 4 | 4 |
| `time-off-in-isolation` · recreation bay | Curved seating and runner frames, reading pose and page, paired game seats and soft furnishings. The game players still share similar postures. | 4 | 3 | 3 | 3 | 3 | 4 | 4 |
| `waiting-for-a-reply` · two communication stations | Separate Earth and habitat back panels, patterned telemetry displays, individual keys, headsets and seated, rigged staff. Waveforms illustrate communication. | 4 | 4 | 3 | 3 | 3 | 4 | 4 |
| `staging-water-loop` · water recovery racks | Sectioned condensate exchanger, three filter cartridges, domed reservoir and an interactive marker along the transfer manifold. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |
| `water-to-breathing-oxygen` · electrolyser | Rounded ported plates, clamping members, separated output plumbing and a reversible exploded-stack view. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |
| `separating-toilet-waste` · waste collector | Open curved casing reveals the collection liner and internal passage beside the separate liquid hose and solid storage route. | 4 | 4 | 3 | 3 | N/A | 4 | 4 |
| `trash-storage-tiles` · heat melt compactor | Sectioned press chamber, ram, platen and heating rings with a reversible stroke. Flexible input packets contrast with the stored tiles. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |
| `perseverance-heat-and-power` · Perseverance and MMRTG | Authored mission geometry for suspension, treads, deck, mast, arm and generator. The leader uses the source RTG bounds. | 5 | 4 | 4 | 4 | N/A | 4 | 4 |
| `oxygen-from-martian-air` · rover and MOXIE | Same mission asset and a separate enlargement stand with a framed section, processing stack and enlarged-installation caption. Internal layout is illustrative. | 4 | 4 | 4 | 4 | N/A | 4 | 4 |
| `dust-and-solar-budget` · deployed InSight | NASA lander, radial array structures, deployed instruments, tethers and nonuniform dust on upward-facing panel surfaces. Two snapshots retain the article's Wh/sol comparison. | 5 | 4 | 4 | 4 | N/A | 4 | 4 |
| `veggie-salad` · Veggie chamber | Six flexible pillows, rosettes with curved, ruffled blades, branching veins and varied scale. The open lighting frame exposes the canopy. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |
| `packing-the-last-meal` · ration shelves | Bulging sealed film with wrinkles, printed labels, foil roughness and restraints across the three mission-stage shelves. Pouch silhouettes repeat. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |
| `expandable-room` · packed and deployed module | Profiled and pleated membrane, a rigid collar at each end and a reversible illustrative compression. A sliding collar and support preserve contact along the rails; woven bands make the shell layers visible. | 4 | 4 | 4 | 3 | N/A | 4 | 4 |

## Asset provenance and redistribution

| Shipped asset | Attribution, permitted use and adaptation |
|---|---|
| `rover.glb` | Courtesy NASA/JPL-Caltech. The authoring source is NASA/JPL. The GLB includes its material images. The web derivative samples the mission asset's mast tracks, joins static meshes, retains the RTG anchor, downsizes images to at most 1024 pixels, and uses WebP and meshoptimizer compression. The source coordinate system is Y-up, with front toward +Z. Wheel and rear-generator bounds were checked before display rotation. [NASA source](https://science.nasa.gov/3d-resources/mars-2020-perseverance-rover/) |
| `insight.glb` | NASA Visualization Technology Applications and Development. The GLB includes the source textures and the deployed instruments. The web derivative merges meshes, simplifies geometry and uses 1024-pixel-or-smaller WebP textures and meshoptimizer. The source spans approximately 6.14 native units along X, with its lowest point at Y = −0.664. Both copies use the same display scale and ground offset. Dust is an original shader. [NASA source](https://science.nasa.gov/resource/insight-3d-model/) |
| `workwear.glb` | MakeHuman base mesh and clothing helper, CC0 1.0. Web derivative with a 15-joint skeleton, four skin weights per vertex, a clothed body, per-region smooth normals, hair and skin materials. Body normalized from 16.66 native units to 1.67 scene units before fitting to furniture. Source +Z forward is rotated toward each station. [MakeHuman mesh and rights notice](https://github.com/makehumancommunity/makehuman/blob/master/makehuman/data/3dobjs/base.obj), [asset license](https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md) |
| `lib/three/gltf.min.js` | Local bundle of GLTFLoader, SkeletonUtils clone, required geometry utilities and MeshoptDecoder from Three.js 0.180.0. Three.js and meshoptimizer are MIT-licensed. Their notices accompany the viewer under `wwwroot/lib/three`. |

NASA's media terms expressly cover educational web use of polygon data and texture maps with source acknowledgement. Both mission pages identify NASA sources. Their credits are also available in the published model notes. Original materials for leaves, foil, cloth, terrain, displays, sky reflections and dust were generated for the viewer. [NASA media terms](https://www.nasa.gov/nasa-brand-center/images-and-media/)

## Implementation and limits

- `articleAssets.js` loads only the selected article's GLB, resolves resources locally, supports cancelling pending fetches and places the spacecraft using measured source coordinates. The browser can cache the files. Parsed GPU resources belong to the mounted viewer.
- `articleCrew.js` clones the skeleton for each of seven people. Joint directions place the pelvis, thighs, knees, shoulders and forearms in different standing, reading and console poses. A short user-triggered head and arm movement uses the same joints across characters. Indoor crew wear work clothes. The Earth control operator has a distinct uniform and bay. The two robotic missions have no surface crew. Facial performance, finger articulation and exact hand contacts remain limited.
- `articleForms.js` generates reusable sewn and sealed surfaces, seats, turned vessels with open sections, ported plates, scalloped leaves and pleated envelopes. `articleModels.js` supplies article-specific lighting reflections, equipment, material variation, environments and batching.
- `livingModels.js` connects builders to the existing camera and renderer, excludes movable parts from instancing, and redraws on interaction and during bounded transitions. Unmount cancels animation, removes the canvas, releases instance buffers, textures, bitmap images, skeletons and reflection targets, and disposes the renderer. `HabitatModel.razor` exposes keyboard-accessible reversible controls and reset. `ReadingFigure.razor` provides asset credits alongside existing model notes.
- The fixed inspection decks preserve access to open equipment. They still look like display stands. The habitat, crew, plant and engineering scenes have one or more ratings below the requested 4/5. The shelf geometry, fabric folds, furniture and environmental backgrounds would need more specific references and higher-resolution art to meet that target consistently.

## Verification and rendering cost

Chrome rendered all 13 article builders at 860 × 500 using SwiftShader. Each comparison contains the baseline, current default yaw/pitch and a closer alternate orbit. The harness exercised pointer drag, keyboard orbit, zoom, reset, illustration state and unmount. Reduced-motion state changes were exercised. Removing the canvas was asserted on each unmount. Hardware GPU frame times, memory in bytes and long-running leak profiles were not measured.

The table counts WebGL draw submissions for one orbit redraw. Submission time is a single JavaScript wall-clock observation under concurrent system load, including driver backpressure. The memory columns are resident geometry and texture counts reported by Three.js.

| Slug | Calls before | Calls after | Triangles | Geometry | Textures | Submit ms |
|---|---:|---:|---:|---:|---:|---:|
| `outpost` | 152 | 152 | 326,970 | 42 | 4 | 24.0 |
| `rooms-for-four` | 26 | 33 | 62,922 | 17 | 18 | 18.4 |
| `expandable-room` | 22 | 25 | 24,174 | 10 | 6 | 4.7 |
| `oxygen-from-martian-air` | 21 | 94 | 330,618 | 44 | 31 | 42.2 |
| `water-to-breathing-oxygen` | 28 | 43 | 15,506 | 17 | 9 | 3.6 |
| `packing-the-last-meal` | 15 | 37 | 35,630 | 30 | 24 | 3.6 |
| `veggie-salad` | 21 | 21 | 98,936 | 13 | 8 | 1.9 |
| `dust-and-solar-budget` | 25 | 40 | 295,740 | 22 | 20 | 4.6 |
| `perseverance-heat-and-power` | 19 | 76 | 316,982 | 38 | 28 | 18.5 |
| `separating-toilet-waste` | 28 | 31 | 7,622 | 17 | 8 | 5.1 |
| `trash-storage-tiles` | 20 | 37 | 24,262 | 26 | 15 | 4.1 |
| `waiting-for-a-reply` | 29 | 45 | 49,770 | 29 | 23 | 4.4 |
| `time-off-in-isolation` | 29 | 52 | 80,120 | 27 | 25 | 8.1 |
| `staging-water-loop` | 49 | 34 | 22,536 | 21 | 8 | 4.0 |

Both reference outpost captures have SHA-256 `94784da5b1aa83ea1870810e7bc17694ab2ead0fa7831ed3ad12fdd0a72e5c31`. Its render used 152 draw calls in both runs. The frontend Release build returned 0 warnings and 0 errors. The backend suite returned 35 passed, 52 skipped and 0 failed.

The asset payloads are 1,617,048 bytes for Perseverance, 2,906,356 bytes for InSight and 120,896 bytes for the rigged workwear GLB. A current article requests at most one of those files.

The Blazor UI loaded all 13 article routes with 200 detail responses from the public Render API across two runs. Two initial detail requests returned HTTP 500. Both passed on retry using a 390-pixel viewport. The landing globe rendered with the local Three.js modules. Controlled comparisons and per-route captures are in the local `article-fidelity` evidence directory.
