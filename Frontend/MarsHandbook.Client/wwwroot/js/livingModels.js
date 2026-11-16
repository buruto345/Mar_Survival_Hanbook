import * as T from "../lib/three/three.module.min.js";

const views = new WeakMap();
const C = { white: 0xe6e3d8, silver: 0x9da9ae, dark: 0x242d35, blue: 0x224457,
    gold: 0xc99b42, red: 0xbc6848, water: 0x49afcb, green: 0x587d42, fabric: 0xb8ae95 };

const nasa = "https://www.nasa.gov/";
const habitat = nasa + "humans-in-space/chapea/habitat/";
const eclss = nasa + "reference/environmental-control-and-life-support-systems-eclss/";
const references = {
    outpost: [habitat, "CHAPEA habitat", "Outpost composition inspired by the supplied Mars-base illustration. Habitat details reference NASA research."],
    "rooms-for-four": [habitat, "CHAPEA habitat", "Printed wall courses, compact quarters and shared workspaces."],
    "time-off-in-isolation": [nasa + "humans-in-space/chapea/chapea-mission-1/", "CHAPEA crew photographs", "Illustrative interior and crew placement."],
    "waiting-for-a-reply": [nasa + "humans-in-space/chapea/chapea-mission-2/", "CHAPEA mission photographs", "Habitat workstation and remote support desk. Layout is illustrative."],
    "expandable-room": [nasa + "international-space-station/bigelow-expandable-activity-module-beam/", "BEAM photographs", "Fabric envelope, restraint bands and rigid attachment collar."],
    "staging-water-loop": [eclss, "NASA life-support hardware", "Rack frames, process vessels, plumbing and control panels. Flow arrangement is simplified."],
    "water-to-breathing-oxygen": [eclss, "NASA life-support hardware", "Rack-mounted electrolysis equipment with illustrative water and gas lines."],
    "oxygen-from-martian-air": [nasa + "solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/", "MOXIE installation photograph", "Gold instrument enclosure and triangular stiffening ribs, shown enlarged beside the rover."],
    "packing-the-last-meal": [nasa + "directæ€ªorates/esdmd/hhp/space-food-systems/", "NASA space food photographs", "Sealed pouches, drink fittings and labelled storage bags."],
    "veggie-salad": [nasa + "exploration-research-and-technology/growing-plants-in-space/", "Veggie hardware photographs", "Six plant pillows, LED canopy and flexible chamber enclosure."],
    "dust-and-solar-budget": ["https://science.nasa.gov/resource/insights-power-generation-after-landing-and-spring-2022/", "InSight comparison photographs", "Two circular, ribbed arrays in clean and dusty states."],
    "perseverance-heat-and-power": ["https://science.nasa.gov/mission/mars-2020-perseverance/rover-components/", "Perseverance hardware photographs", "Six treaded wheels, suspension links, camera mast and finned generator."],
    "separating-toilet-waste": [nasa + "humans-in-space/boldly-go-nasas-new-space-toilet-offers-more-comfort-improved-efficiency-for-deep-space-missions/", "NASA UWMS photographs", "Seat ring, collection funnel and hose. Downstream routes are illustrative."],
    "trash-storage-tiles": [nasa + "ames/space-biosciences/what-is-nasas-heat-melt-compactor/", "Heat Melt Compactor photographs", "Metal process chamber, instrumentation and compressed trash tiles."]
};

function build(root, id) {
    const materials = new Map();
    function material(color) {
        if (!materials.has(color)) {
            materials.set(color, new T.MeshStandardMaterial({
                color, roughness: color === C.silver ? .34 : .77,
                metalness: [C.silver, C.gold].includes(color) ? .65 : .08
            }));
        }
        return materials.get(color);
    }
    function mesh(geometry, color, x, y, z) {
        const object = new T.Mesh(geometry, material(color));
        object.position.set(x, y, z);
        object.castShadow = true;
        object.receiveShadow = true;
        root.add(object);
        return object;
    }
    const box = (x,y,z,w,h,d,c) => mesh(new T.BoxGeometry(w,h,d),c,x,y,z);
    const cylinder = (x,y,z,r,h,c) => mesh(new T.CylinderGeometry(r,r,h,24),c,x,y,z);
    function sphere(x,y,z,a,b,c,color) {
        const object = mesh(new T.SphereGeometry(1,24,16),color,x,y,z);
        object.scale.set(a,b,c);
        return object;
    }
    function tube(points, color, radius = .035) {
        const curve = new T.CatmullRomCurve3(points.map(p => new T.Vector3(...p)));
        return mesh(new T.TubeGeometry(curve,32,radius,8,false),color,0,0,0);
    }
    function rod(a,b,r,color) {
        const first = new T.Vector3(...a), last = new T.Vector3(...b);
        const axis = last.clone().sub(first);
        const object = cylinder(0,0,0,r,axis.length(),color);
        object.position.copy(first).lerp(last,.5);
        object.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),axis.normalize());
        return object;
    }
    function ring(x,y,z,r,color,rotation=0) {
        const object = mesh(new T.TorusGeometry(r,.035,8,40),color,x,y,z);
        object.rotation.x = rotation;
        return object;
    }
    function plaque(x,y,z,text,width=1.2) {
        const canvas = document.createElement("canvas");
        canvas.width = 512; canvas.height = 96;
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#e8e5da"; ctx.fillRect(0,0,512,96);
        ctx.fillStyle = "#26353d"; ctx.font = "bold 36px sans-serif";
        ctx.textAlign = "center"; ctx.fillText(text,256,60,480);
        const map = new T.CanvasTexture(canvas); map.colorSpace = T.SRGBColorSpace;
        const object = new T.Mesh(new T.PlaneGeometry(width,width*96/512),
            new T.MeshBasicMaterial({map,side:T.DoubleSide}));
        object.position.set(x,y,z);root.add(object);
        return object;
    }
    function bolts(x,y,z,w,h) {
        for (const dx of [-1,1]) for (const dy of [-1,1])
            sphere(x+dx*w/2,y+dy*h/2,z,.025,.025,.014,C.dark);
    }
    function screen(x,y,z,w=.38,h=.22) {
        box(x,y,z,w+.07,h+.07,.08,C.dark);
        const display = box(x,y,z+.045,w,h,.015,C.water);
        display.material = new T.MeshStandardMaterial({color:0x244c59,emissive:0x327d88,emissiveIntensity:.4});
        for (let i=0;i<3;i++) box(x-w*.28,y-h*.27+i*h*.23,z+.058,w*(.6-i*.12),.012,.005,C.white);
    }
    function rack(x,name) {
        for (const dx of [-.69,.69]) for (const z of [-.48,.48])
            box(x+dx,1.34,z,.08,2.6,.08,C.silver);
        for (const y of [.08,1.2,2.6]) {
            box(x,y,0,1.46,.07,1.03,C.silver);
            bolts(x,y,.53,1.3,.03);
        }
        box(x,1.3,-.48,1.3,2.55,.035,C.white);
        box(x,2.31,0,1.28,.42,.9,C.white);
        screen(x-.22,2.31,.47);
        plaque(x+.32,2.31,.475,name,.48);
        for(let j=0;j<4;j++)cylinder(x-.45+j*.3,.21,0,.08,.2,C.dark);
    }
    function vessel(x,y,z,r=.2,h=.7) {
        cylinder(x,y,z,r,h,C.silver);
        sphere(x,y+h/2,z,r,.12,r,C.silver);
        sphere(x,y-h/2,z,r,.12,r,C.silver);
        for (const dy of [-h*.3,h*.3])ring(x,y+dy,z,r+.012,C.dark,Math.PI/2);
        rod([x,y+h/2,z],[x,y+h/2+.18,z],.045,C.gold);
    }
    function person(x,z,color,seated=false) {
        const y = seated ? -.23 : 0;
        sphere(x,1.12+y,z,.13,.16,.13,0xbc977d);
        sphere(x,1.23+y,z-.03,.13,.08,.12,C.dark);
        box(x,.8+y,z,.32,.4,.22,color);
        for(const dx of [-.105,.105]) {
            rod([x+dx,.61+y,z],[x+dx,.35,z+(seated?.24:0)],.07,C.blue);
            rod([x+dx,.35,z+(seated?.24:0)],[x+dx,.08,z+(seated?.24:0)],.065,C.blue);
            box(x+dx,.055,z+.07+(seated?.24:0),.14,.1,.25,C.dark);
            rod([x+dx*1.9,.95+y,z],[x+dx*2,.66+y,z+.15],.055,color);
        }
    }
    function chair(x,z) {
        box(x,.42,z,.5,.1,.5,C.blue);box(x,.75,z-.23,.5,.62,.06,C.blue);
        for(const dx of [-.2,.2])for(const dz of [-.2,.2])rod([x+dx,.4,z+dz],[x+dx,.04,z+dz],.025,C.silver);
    }
    function habitatInterior(privateRooms) {
        box(0,.02,0,6.6,.16,3.65,C.fabric);
        // Exposed courses recall the layered walls visible in CHAPEA construction photographs.
        for(let i=0;i<24;i++) {
            box(0,.16+i*.064,-1.8,6.6,.045,.16,C.fabric);
            box(-3.25,.16+i*.064,0,.16,.045,3.6,C.fabric);
        }
        for(let x=-3;x<3.3;x+=.55)box(x,.109,0,.012,.012,3.5,0x928b7b);
        const count = privateRooms ? 4 : 2;
        for(let i=0;i<count;i++) {
            const x=-2.45+i*1.55;
            box(x,.34,-.85,1.18,.4,1.45,C.white);
            box(x,.58,-.6,1.1,.12,.9,C.blue);
            box(x,.6,-1.35,.85,.13,.32,C.white);
            if(i<count-1)box(x+.72,.83,-.95,.09,1.5,1.65,C.fabric);
            box(x,.89,-1.69,.6,.42,.06,C.dark);screen(x,.89,-1.64,.48,.3);
        }
        if(!privateRooms) {
            box(1.7,.77,.5,1.9,.12,.85,C.white);
            for(const x of [1.2,2.2]){chair(x,.99);person(x,.99,C.blue,true)}
            box(2.68,.48,-.8,.8,.8,1,C.white);
            box(2.68,.93,-.8,.9,.08,1.1,C.dark);
            cylinder(2.68,1.02,-.8,.2,.1,C.silver);
            for(let i=0;i<4;i++)for(let j=0;j<4;j++)
                box(1.7+i*.07,.846,.35+j*.07,.065,.01,.065,(i+j)%2?C.dark:C.white);
            chair(-2.2,.82);person(-2.2,.82,C.red,true);
        } else {person(.2,1.04,C.red);person(2,1.04,C.blue)}
    }
    function rover() {
        box(0,.99,0,2.45,.62,1.45,C.white);
        box(0,1.32,0,2.6,.07,1.65,C.silver);
        for(const side of [-1,1]) {
            for(const x of [-1.05,0,1.05]) {
                const z=side*1.05;
                cylinder(x,.43,z,.43,.28,C.dark).rotation.x=Math.PI/2;
                cylinder(x,.43,z+side*.15,.2,.028,C.silver).rotation.x=Math.PI/2;
                for(let i=0;i<18;i++) {
                    const a=i*Math.PI/9;
                    const tread=box(x+.423*Math.cos(a),.43+.423*Math.sin(a),z,.05,.05,.3,C.silver);
                    tread.rotation.z=a;
                }
                rod([x,.44,z],[x*.68,.95,side*.72],.045,C.silver);
            }
            rod([-1.05,.74,side*.86],[.5,1.12,side*.8],.06,C.dark);
        }
        box(.75,1.95,-.25,.18,1.2,.18,C.white);
        box(.75,2.56,-.25,.74,.28,.33,C.white);
        for(const x of [.52,.96]) {
            cylinder(x,2.56,-.055,.095,.09,C.dark).rotation.x=Math.PI/2;
            cylinder(x,2.56,0,.055,.02,C.blue).rotation.x=Math.PI/2;
        }
        rod([.3,1.2,.85],[1.5,.67,1.25],.09,C.white);
        rod([1.5,.67,1.25],[1.95,.35,.75],.07,C.silver);
        cylinder(1.95,.3,.75,.18,.2,C.dark);
        cylinder(-.6,1.6,-.35,.3,.04,C.white);
        // Rear MMRTG fin stack and gold thermal housing.
        cylinder(-1.5,1.25,0,.3,.8,C.gold).rotation.z=Math.PI/2;
        for(let i=0;i<10;i++)box(-1.9+i*.08,1.25,0,.025,.88,.9,C.silver);
        for(let i=0;i<3;i++)tube([[-.8+i*.15,1.37,.65],[0,1.4,.65],[.4,1.35,.4]],C.gold,.016);
    }
    function array(x,dust) {
        cylinder(x,.7,0,1.03,.05,C.dark);
        for(let sector=0;sector<10;sector++) {
            const angle=sector*Math.PI/5;
            // Wedge-shaped cell sectors match InSight's fan-like circular arrays.
            const shape=new T.Shape();
            shape.moveTo(.12,0);
            for(let j=0;j<=8;j++){const a=.025+j*(Math.PI/5-.05)/8;shape.lineTo(Math.cos(a),Math.sin(a))}
            shape.lineTo(.12*Math.cos(Math.PI/5),.12*Math.sin(Math.PI/5));shape.closePath();
            const wedge=mesh(new T.ShapeGeometry(shape),dust?0x9e8060:0x253951,x,.735,0);
            wedge.rotation.x=-Math.PI/2;wedge.rotation.z=angle;
            const edgeX=x+Math.cos(angle),edgeZ=-Math.sin(angle);
            rod([x,.75,0],[edgeX,.75,edgeZ],.012,C.silver);
            for(let r=.25;r<1;r+=.16) {
                const arc=[];
                for(let j=0;j<=8;j++){const a=angle+j*Math.PI/40;arc.push([x+r*Math.cos(a),.75,-r*Math.sin(a)])}
                tube(arc,dust?0xb39473:0x52738d,.004);
            }
        }
        cylinder(x,.46,0,.12,.5,C.silver);
    }
    function lander(x,dust) {
        box(x,.6,0,.8,.36,.85,C.gold);
        sphere(x,.91,0,.37,.22,.36,C.white);
        for(const a of [0,2.1,4.2]) {
            const dx=Math.cos(a),dz=Math.sin(a);
            rod([x+dx*.3,.55,dz*.3],[x+dx*.8,.1,dz*.8],.035,C.silver);
            cylinder(x+dx*.8,.08,dz*.8,.15,.05,C.dark);
        }
        for(const sign of [-1,1]){rod([x,.65,0],[x+sign*1.2,.72,0],.04,C.silver);array(x+sign*1.24,dust)}
        rod([x,.9,0],[x,1.45,0],.035,C.white);
    }
    function pouch(x,y,z,index) {
        box(x,y,z,.4,.53,.09,index%3?C.fabric:C.silver);
        box(x,y+.24,z,.4,.035,.11,C.dark);
        box(x,y,z+.052,.28,.2,.01,C.white);
        for(let i=0;i<3;i++)box(x,y-.05+i*.045,z+.062,.21,.012,.005,C.dark);
        cylinder(x+.12,y+.3,z,.033,.12,C.white);
    }

    switch(id) {
    case "rooms-for-four": habitatInterior(true);break;
    case "time-off-in-isolation": habitatInterior(false);break;
    case "outpost": {
        // Composition follows the user's supplied Mars-base illustration.
        let seed = 41;
        function random() {
            seed = (seed * 1664525 + 1013904223) >>> 0;
            return seed / 4294967296;
        }
        function topic(slug, create) {
            const first = root.children.length;
            create();
            for (const object of root.children.slice(first)) object.userData.topic = slug;
        }
        function arch(x, z, radius, length, glass = false) {
            const points = [], indices = [];
            for (let row = 0; row < 2; row++) for (let j = 0; j <= 40; j++) {
                const angle = j * Math.PI / 40;
                points.push(x + radius * Math.cos(angle), .12 + radius * Math.sin(angle),
                    z + (row - .5) * length);
            }
            for (let j = 0; j < 40; j++) {
                indices.push(j, j + 1, j + 41, j + 1, j + 42, j + 41);
            }
            const geometry = new T.BufferGeometry();
            geometry.setAttribute("position", new T.Float32BufferAttribute(points, 3));
            geometry.setIndex(indices); geometry.computeVertexNormals();
            const skin = new T.Mesh(geometry, new T.MeshStandardMaterial({
                color: glass ? 0xc4ddd9 : C.white, side: T.DoubleSide,
                transparent: glass, opacity: glass ? .19 : 1,
                roughness: glass ? .24 : .82, depthWrite: !glass
            }));
            skin.castShadow = !glass; skin.receiveShadow = true;
            root.add(skin);
            for (let rib = 0; rib <= 6; rib++) {
                const line = [];
                for (let j = 0; j <= 32; j++) {
                    const angle = j * Math.PI / 32;
                    line.push([x + radius * Math.cos(angle), .12 + radius * Math.sin(angle),
                        z + (rib / 6 - .5) * length]);
                }
                tube(line, C.white, .045);
            }
            for (const side of [-1, 1]) {
                box(x + side * radius, .15, z, .13, .22, length, C.white);
            }
        }
        function airlock(x, z) {
            box(x, .5, z, .82, .85, .65, C.white);
            cylinder(x, .58, z + .36, .31, .11, C.silver).rotation.x = Math.PI / 2;
            cylinder(x, .58, z + .43, .22, .03, C.dark).rotation.x = Math.PI / 2;
            box(x, .08, z + .75, .85, .08, .7, C.silver);
            for (const side of [-1, 1]) rod([x + side * .4, .12, z + 1],
                [x + side * .4, .52, z + .4], .025, C.silver);
        }
        function passage(a, b) {
            rod(a, b, .27, C.white);
            const axis = new T.Vector3(...b).sub(new T.Vector3(...a)).normalize();
            for (let j = 0; j <= 7; j++) {
                const point = new T.Vector3(...a).lerp(new T.Vector3(...b), j / 7);
                const collar = ring(point.x, point.y, point.z, .28, C.silver);
                collar.quaternion.setFromUnitVectors(new T.Vector3(0, 0, 1), axis);
            }
        }
        function sign(x, z, text) {
            const canvas = document.createElement("canvas");
            canvas.width = 512; canvas.height = 80;
            const ctx = canvas.getContext("2d");
            ctx.fillStyle = "#18252de8"; ctx.fillRect(0, 0, 512, 80);
            ctx.fillStyle = "#edf3ed"; ctx.font = "32px sans-serif";
            ctx.textAlign = "center"; ctx.fillText(text, 256, 51, 490);
            const map = new T.CanvasTexture(canvas); map.colorSpace = T.SRGBColorSpace;
            const marker = new T.Sprite(new T.SpriteMaterial({map, depthTest: false}));
            marker.position.set(x, 1.7, z); marker.scale.set(2.1, .328, 1);
            root.add(marker);
            rod([x, .1, z], [x, 1.5, z], .018, C.silver);
        }
        function solarField(x, z) {
            for (let row = 0; row < 3; row++) for (let col = 0; col < 6; col++) {
                const px = x + col * .65, pz = z + row * .8;
                box(px, .32, pz, .6, .06, .7, C.blue).rotation.x = -.18;
                for (let cell = 0; cell < 3; cell++)
                    box(px - .19 + cell * .19, .36, pz, .009, .008, .65, C.silver).rotation.x = -.18;
                rod([px, .08, pz], [px, .32, pz], .025, C.silver);
            }
        }

        const groundGeometry = new T.PlaneGeometry(180, 180, 180, 180);
        groundGeometry.rotateX(-Math.PI / 2);
        const positions = groundGeometry.attributes.position;
        const colors = [];
        const soil = new T.Color();
        for (let i = 0; i < positions.count; i++) {
            const x = positions.getX(i), z = positions.getZ(i);
            const edge = Math.max(Math.abs(x) - 6.7, Math.abs(z) - 5.8, 0);
            positions.setY(i, -.08 + random() * .025 + Math.min(edge, 2) * (.04 + random() * .02));
            soil.setHSL(.025 + random() * .01, .69 + random() * .1, .18 + random() * .025);
            colors.push(soil.r, soil.g, soil.b);
        }
        groundGeometry.setAttribute("color", new T.Float32BufferAttribute(colors, 3));
        groundGeometry.computeVertexNormals();
        const ground = new T.Mesh(groundGeometry,
            new T.MeshStandardMaterial({vertexColors: true, roughness: 1}));
        ground.receiveShadow = true; ground.userData.mapGround = true; root.add(ground);
        for (let i = 0; i < 140; i++) {
            const x = (random() - .5) * 16.5, z = (random() - .5) * 14.5;
            // Keep the central buildings and greenhouse beds clear of rocks.
            if (Math.abs(x) < 6.5 && Math.abs(z) < 5.6) continue;
            const scale = .035 + random() * .14;
            const rock = mesh(new T.DodecahedronGeometry(1, 0), i % 2 ? 0x744631 : 0x9d6041,
                x, .03, z);
            rock.scale.set(scale * 1.6, scale, scale);
            rock.rotation.set(random(), random(), random());
        }
        // Two long greenhouses occupy the foreground, as in the supplied reference.
        topic("food", () => {
            for (const x of [-2, 1.1]) {
                const z = 2.25, radius = 1.32, length = 5.2;
                box(x, .03, z, 2.7, .12, 5.3, C.fabric);
                for (const dx of [-.8, .8]) {
                    box(x + dx, .22, z, .65, .28, 4.6, C.silver);
                    box(x + dx, .38, z, .58, .04, 4.5, 0x553e29);
                    for (let plant = 0; plant < 12; plant++) {
                        const pz = z - 2.05 + plant * .37;
                        for (const side of [-1, 1]) {
                            const leaf = sphere(x + dx + side * .09, .51, pz,
                                .19, .12, .17, plant % 3 ? C.green : 0x91a45c);
                            leaf.rotation.z = side * .35;
                        }
                    }
                }
                for (let j = 0; j < 9; j++)
                    box(x, .1, z - 2.1 + j * .52, .55, .035, .16, C.white);
                arch(x, z, radius, length, true);
                for (const side of [-1, 1])
                    rod([x + side * .88, 1.1, z - 2.6], [x + side * .88, 1.1, z + 2.6], .025, C.silver);
                airlock(x, z - length / 2 - .3);
            }
            sign(-2, 5.3, "Food · greenhouses");
        });
        topic("housing", () => {
            arch(-4.7, -2.8, 1.05, 3.5);
            for (const z of [-4.55, -1.05])
                box(-4.7, .54, z, 2.05, 1.05, .08, C.white);
            airlock(-4.7, -.8);
            for (const x of [-5.2, -4.3])
                cylinder(x, .68, -.99, .16, .03, C.blue).rotation.x = Math.PI / 2;
            passage([-4.3, .48, -2.8], [-1.6, .48, -2.8]);
            sign(-5, -.05, "Housing");
        });
        topic("community", () => {
            arch(-.3, -3.25, 1.05, 2.6);
            box(-.3, .5, -1.93, 2, 1, .08, C.white);
            airlock(-.3, -1.7);
            passage([-.3, .45, -1.5], [-.3, .45, -.6]);
            sign(-.3, -.4, "Community");
        });
        topic("water-and-oxygen", () => {
            arch(3.1, -2.4, .72, 2.6);
            airlock(3.1, -.9);
            for (const x of [4.4, 5.3, 6.2]) vessel(x, .72, -2.3, .33, 1.1);
            for (const z of [-2.9, -1.5])
                tube([[3.8, .2, z], [4.5, .2, z], [6.2, .2, z]], C.water, .045);
            passage([.7, .45, -3.3], [2.4, .45, -3.3]);
            sign(4.8, -.8, "Water & oxygen");
        });
        topic("energy", () => {
            solarField(1.65, -6.35);
            box(5.8, .35, -5.5, 1.3, .7, 1.3, C.white);
            for (let j = 0; j < 6; j++) box(5.8, .3 + j * .07, -4.83, 1.1, .025, .04, C.dark);
            tube([[5.8, .1, -4.9], [5.8, .1, -3.4], [3, .1, -3.4]], C.dark, .035);
            sign(5.7, -4.2, "Energy");
        });
        topic("sanitation-and-waste", () => {
            arch(5.4, 2, .7, 1.8);
            airlock(5.4, 3.1);
            for (const x of [4.35, 6.5]) {
                cylinder(x, .35, 2.3, .24, .7, C.silver);
                cylinder(x, .73, 2.3, .25, .07, C.dark);
            }
            tube([[5.4, .1, 1], [5.4, .1, .1], [4.5, .1, -1.5]], C.gold, .04);
            sign(5.4, 4.15, "Waste");
        });
        // A dish and a parked utility rover give the outpost a working surface scale.
        rod([-6.5, .05, 2], [-6.5, 1.3, 2], .06, C.silver);
        const dish = mesh(new T.SphereGeometry(.55, 24, 12, 0, Math.PI * 2, 0, .95),
            C.white, -6.5, 1.35, 2);
        dish.rotation.z = -.6;
        rod([-6.5, 1.35, 2], [-6.15, 1.9, 2], .025, C.dark);
        box(5.2, .32, 5.6, 1.1, .32, .7, C.white);
        for (const x of [4.8, 5.6]) for (const z of [5.18, 6.02])
            cylinder(x, .17, z, .19, .16, C.dark).rotation.x = Math.PI / 2;
        break;
    }
    case "expandable-room":
        for(const [x,length,r] of [[-1.9,.6,.6],[.9,2.2,1]]) {
            const body=cylinder(x,1.1,0,r,length,C.white);body.rotation.z=Math.PI/2;
            for(const sign of [-1,1]) {
                sphere(x+sign*length/2,1.1,0,.3,r,r,C.white);
                cylinder(x+sign*(length/2+.22),1.1,0,r*.48,.12,C.silver).rotation.z=Math.PI/2;
            }
            for(let i=0;i<7;i++){
                const band=ring(x+(i/6-.5)*length,1.1,0,r+.018,C.fabric);
                band.rotation.y=Math.PI/2;
            }
            for(let i=0;i<10;i++){
                const a=i*Math.PI/5;rod([x-length/2,1.1+r*Math.cos(a),r*Math.sin(a)],[x+length/2,1.1+r*Math.cos(a),r*Math.sin(a)],.014,C.fabric);
            }
        }break;
    case "staging-water-loop":
        for(const [x,name] of [[-1.8,"CONDENSATE"],[0,"PROCESSOR"],[1.8,"CLEAN WATER"]]){
            rack(x,name);
            for(let i=0;i<3;i++)vessel(x-.4+i*.4,.95,0,.16,.95);
            for(let i=0;i<3;i++)tube([[x-.45+i*.4,1.65,0],[x-.45+i*.4,1.85,.2],[x+.45,1.85-i*.12,.35],[x+.45,.45,.35]],i===1?C.water:C.silver,.025);
            for(let i=0;i<3;i++)box(x-.42+i*.4,1.95,.48,.2,.07,.08,C.gold);
        }
        tube([[-1.8,.35,.55],[-1,.35,.7],[0,.4,.7],[1.8,.35,.55]],C.water,.055);break;
    case "water-to-breathing-oxygen":
        rack(-1.3,"OXYGEN GENERATOR");rack(1.2,"GAS PROCESSING");
        for(let i=0;i<13;i++)box(-1.3,.65+i*.07,.05,.9,.035,.65,i%2?C.silver:C.dark);
        for(const x of [-1.72,-.88])rod([x,.56,.39],[x,1.56,.39],.025,C.gold);
        vessel(1,1.1,0,.21,1.1);vessel(1.5,1.1,0,.14,.9);
        tube([[-2.25,.2,.1],[-2.25,.5,.6],[-1.3,.5,.6]],C.water,.05);
        for(const [i,c] of [[0,C.green],[1,C.gold]])tube([[-1.3,1.55,.3],[-.6,1.8+i*.2,.3],[1+i*.5,1.8+i*.2,.3],[1+i*.5,1.5,0]],c);
        plaque(-1.3,1.75,.5,"ELECTROLYSIS",.85);plaque(1.2,1.78,.5,"O2 / H2",.7);break;
    case "oxygen-from-martian-air":
        rover();
        // Enlarged, detached instrument shows the triangular wall reinforcement in NASA's installation photo.
        box(-2.45,1.08,1,1.05,1.12,.8,C.gold);
        for(const z of [.59,1.41]) {
            for(let j=0;j<3;j++){
                const x=-2.92+j*.31;
                rod([x,.58,z],[x+.16,1.57,z],.016,C.gold);
                rod([x+.16,1.57,z],[x+.31,.58,z],.016,C.gold);
            }
            bolts(-2.45,1.08,z,1.02,1.08);
        }
        for(const x of [-2.76,-2.15])box(x,1.72,1,.13,.18,.13,C.silver);
        plaque(-2.45,.38,1.42,"MOXIE Â· ENLARGED",1.4);break;
    case "perseverance-heat-and-power": rover();plaque(-1.7,2.05,0,"MMRTG",.9);break;
    case "dust-and-solar-budget":
        lander(-2.5,false);lander(2.5,true);
        plaque(-2.5,1.9,.4,"5,000 Wh/sol",1.6);plaque(2.5,1.9,.4,"500 Wh/sol",1.6);break;
    case "packing-the-last-meal":
        for(let i=0;i<3;i++){
            const x=(i-1)*1.7;
            box(x,.12,0,1.5,.15,1.25,C.white);
            for(const dx of [-.7,.7])box(x+dx,1,0,.055,1.9,1.2,C.silver);
            for(const y of [.15,1.05,1.95])box(x,y,0,1.48,.055,1.2,C.silver);
            for(let j=0;j<5;j++)pouch(x-.46+(j%3)*.46,.47+Math.floor(j/3)*.91,.25,j+i);
            plaque(x,2.2,.65,["EARLY","MID-MISSION","LATE"][i],1.3);
        }break;
    case "veggie-salad":
        box(0,.24,0,3.3,.35,1.9,C.white);
        for(const x of [-1,0,1])for(const z of [-.43,.43]){
            sphere(x,.52,z,.4,.13,.35,C.fabric);
            cylinder(x,.65,z,.08,.18,C.silver);
            for(let i=0;i<7;i++){
                const a=i*Math.PI*2/7;
                const leaf=sphere(x+.18*Math.cos(a),.78+(i%2)*.12,z+.18*Math.sin(a),.34,.06,.17,i%2?0x748f43:C.green);
                leaf.rotation.y=-a;leaf.rotation.z=.3*Math.cos(a);
                tube([[x,.7,z],[x+.25*Math.cos(a),.81,z+.25*Math.sin(a)],[x+.45*Math.cos(a),.82,z+.45*Math.sin(a)]],0x9da763,.008);
            }
        }
        for(const x of [-1.65,1.65])for(const z of [-.9,.9])box(x,1.15,z,.055,1.9,.055,C.silver);
        for(let i=0;i<10;i++)box(0,.5+i*.14,-.93,3.3,.04,.07,C.white);
        box(0,2.13,0,3.5,.22,2,C.white);
        for(const x of [-1.2,-.6,0,.6,1.2])for(const z of [-.6,0,.6]){
            const led=cylinder(x,1.999,z,.06,.015,C.white);
            led.material=new T.MeshStandardMaterial({color:0xff9fda,emissive:0xff56b8,emissiveIntensity:1.4});
        }
        plaque(0,2.14,1.01,"VEGGIE",1);break;
    case "separating-toilet-waste": {
        box(-1.1,.62,0,1.15,1.1,1.1,C.white);
        cylinder(-1.1,1.2,0,.41,.1,C.silver);
        ring(-1.1,1.27,0,.29,C.dark,Math.PI/2);
        cylinder(-1.1,1.21,0,.24,.04,C.dark);
        screen(-1.5,1.1,.58,.2,.15);
        tube([[-1.65,.6,.3],[-1.95,.7,.5],[-2,1.35,.2]],C.dark,.065);
        mesh(new T.CylinderGeometry(.2,.06,.3,24),C.silver,-2,1.5,.2);
        for(const x of [-1.5,-.7])rod([x,.2,.5],[x,.15,.95],.04,C.silver);
        vessel(1,.7,-.4,.32,1.05);box(2,.55,.5,.9,.95,.9,C.silver);
        tube([[-1.1,.4,0],[0,.3,-.5],[1,.3,-.5]],C.water,.045);
        tube([[-1.1,.25,.3],[0,.2,.7],[2,.2,.7]],C.gold,.05);
        plaque(1,1.65,-.4,"LIQUID",.8);plaque(2,1.4,.5,"SOLID",.8);break;
    }
    case "trash-storage-tiles":
        rack(0,"HEAT MELT COMPACTOR");
        box(0,1.1,0,1.1,1.3,.85,C.silver);
        box(0,1.1,.46,.88,.9,.08,C.white);bolts(0,1.1,.52,.97,1.1);
        box(0,1.1,.52,.65,.57,.04,C.dark);
        for(const x of [-.46,.46])box(x,1.1,.57,.09,.4,.1,C.silver);
        for(let i=0;i<4;i++)tube([[.5,.4+i*.22,.2],[.92,.4+i*.22,.2],[.92,1.8,.1],[.2,2,.1]],i%2?C.silver:C.gold,.017);
        for(let i=0;i<7;i++)pouch(-2+(i%3)*.3,.3+Math.floor(i/3)*.17,.1+(i%2)*.2,i);
        for(let i=0;i<3;i++) {
            cylinder(2,.14+i*.14,0,.52,.12,C.fabric);
            for(let j=0;j<7;j++)box(2+Math.cos(j)*.28,.205+i*.14,Math.sin(j)*.28,.12,.01,.09,j%2?C.silver:C.gold);
        }break;
    case "waiting-for-a-reply":
        for(const x of [-1.6,1.6]){
            box(x,.75,0,2.2,.1,1,C.white);
            for(const dx of [-.9,.9])box(x+dx,.37,0,.07,.75,.8,C.silver);
            for(const dx of [-.45,.45]){
                screen(x+dx,1.23,-.25,.7,.44);
                box(x+dx,.84,.15,.66,.04,.23,C.dark);
            }
            chair(x,.65);person(x,.65,x<0?C.red:C.blue,true);
            plaque(x,1.82,-.22,x<0?"HABITAT":"MISSION CONTROL",1.7);
        }
        tube([[-.6,.8,-.45],[0,1.35,-.45],[.6,.8,-.45]],C.water,.025);break;
    default: throw new Error("Unknown handbook model");
    }
}

export function mount(host,id,title,callback) {
    unmount(host);
    const renderer = new T.WebGLRenderer({antialias:true});
    renderer.setPixelRatio(Math.min(devicePixelRatio,2));
    renderer.setClearColor(0x111a22);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.autoUpdate = false;
    renderer.shadowMap.needsUpdate = true;
    renderer.shadowMap.type = T.PCFSoftShadowMap;
    renderer.toneMapping = T.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.25;
    const canvas = renderer.domElement;
    host.replaceChildren(canvas);
    canvas.tabIndex=0;canvas.setAttribute("role","img");canvas.setAttribute("aria-label",title);
    const scene = new T.Scene(), root = new T.Group();scene.add(root);
    build(root,id);
    const bounds = new T.Box3();
    for (const object of root.children)
        if (!object.userData.mapGround) bounds.union(new T.Box3().setFromObject(object));
    const center = bounds.getCenter(new T.Vector3()), target = center.clone();
    const size = bounds.getSize(new T.Vector3());
    const base = new T.Mesh(new T.BoxGeometry(size.x+.8,.14,size.z+.8),
        new T.MeshStandardMaterial({color:[ "outpost","dust-and-solar-budget","perseverance-heat-and-power","oxygen-from-martian-air"].includes(id)?0x87654f:0x3f4b53,roughness:.95}));
    base.position.set(center.x,-.2,center.z);base.receiveShadow=true;
    if (id !== "outpost") scene.add(base);
    else { base.geometry.dispose(); base.material.dispose(); }
    scene.add(new T.HemisphereLight(0xdaeafa,0x7b6d5f,2));
    const key = new T.DirectionalLight(0xffe1b9,3.2);key.position.set(-4,8,6);key.castShadow=true;
    key.shadow.mapSize.set(1024,1024);key.shadow.camera.left=-8;key.shadow.camera.right=8;
    key.shadow.camera.top=8;key.shadow.camera.bottom=-8;key.shadow.bias=-.001;scene.add(key);
    const fill=new T.DirectionalLight(0xa9d5ff,1.2);fill.position.set(5,3,-5);scene.add(fill);
    const camera = new T.OrthographicCamera(-4,4,3,-3,.1,80);
    const initialPitch = id === "outpost" ? .76 : .52;
    const initialZoom = id === "outpost" ? .84 : 1;
    let yaw=.42,pitch=initialPitch,zoom=initialZoom,expanded=false;
    let entryFrame = null;
    function stopEntry() {
        if (entryFrame !== null) cancelAnimationFrame(entryFrame);
        entryFrame = null;
    }
    function enterMap() {
        stopEntry();
        if (matchMedia("(prefers-reduced-motion: reduce)").matches) {
            zoom=initialZoom; target.copy(center); draw(); return;
        }
        const begun=performance.now();
        function frame(now) {
            const progress=Math.min((now-begun)/1400,1);
            const eased=1-Math.pow(1-progress,3);
            zoom=initialZoom+.3*(1-eased);
            target.copy(center);
            target.z+=1.1*(1-eased);
            draw();
            entryFrame=progress<1?requestAnimationFrame(frame):null;
        }
        entryFrame=requestAnimationFrame(frame);
    }
    const pointers = new Map();
    let pinchDistance = null;
    function limitPan(){
        target.x = T.MathUtils.clamp(target.x,center.x-14,center.x+14);
        target.z = T.MathUtils.clamp(target.z,center.z-14,center.z+14);
    }
    let renderedWidth=0, renderedHeight=0;
    function draw(){
        const w=host.clientWidth,h=host.clientHeight;if(!w||!h)return;
        if(w !== renderedWidth || h !== renderedHeight){
            renderer.setSize(w,h,false);
            renderedWidth=w;renderedHeight=h;
        }
        // Fit the bounding sphere for every angle, including portrait viewports.
        camera.position.set(target.x+25*Math.sin(yaw)*Math.cos(pitch),target.y+25*Math.sin(pitch),target.z+25*Math.cos(yaw)*Math.cos(pitch));
        camera.lookAt(target); camera.updateMatrixWorld();
        let half;
        if (id === "outpost") {
            let horizontal = 0, vertical = 0;
            for (const x of [bounds.min.x, bounds.max.x])
                for (const y of [bounds.min.y, bounds.max.y])
                    for (const z of [bounds.min.z, bounds.max.z]) {
                        const point = new T.Vector3(x+target.x-center.x,y,z+target.z-center.z).applyMatrix4(camera.matrixWorldInverse);
                        horizontal = Math.max(horizontal, Math.abs(point.x));
                        vertical = Math.max(vertical, Math.abs(point.y));
                    }
            half = Math.max(vertical, horizontal * h / w) * 1.06 * zoom;
        } else {
            const extent = Math.hypot(size.x,size.y,size.z)*.54;
            half = extent*Math.max(1,h/w)*zoom;
        }
        camera.left=-half*w/h;camera.right=-camera.left;camera.top=half;camera.bottom=-half;
        camera.updateProjectionMatrix();renderer.render(scene,camera);
    }
    const tilt=v=>Math.max(.16,Math.min(1.25,v));
    let start=null, selectedTopic=null, disposed=false, dragged=false;
    const explorer = host.parentElement, view = explorer.parentElement;
    if(id === "outpost") canvas.style.touchAction = "none";
    function trapFocus(e){
        if(!expanded || e.key !== "Tab") return;
        const nodes = [...view.querySelectorAll("button,a,canvas[tabindex]")].filter(n=>n.getClientRects().length);
        if(e.shiftKey && document.activeElement === nodes[0]){e.preventDefault();nodes.at(-1)?.focus()}
        else if(!e.shiftKey && document.activeElement === nodes.at(-1)){e.preventDefault();nodes[0]?.focus()}
    }
    view.addEventListener("keydown",trapFocus);
    const topicObjects = root.children.filter(object => object.userData.topic);
    const originals = new Map(), highlights = new Map();
    for (const object of topicObjects) {
        if (!object.material?.emissive) continue;
        originals.set(object, object.material);
        const bright = object.material.clone();
        bright.emissive.set(0xffaa42); bright.emissiveIntensity = .48;
        highlights.set(object, bright);
    }
    function notify(method, value) {
        if (!disposed) callback.invokeMethodAsync(method, value).catch(() => {});
    }
    function selectTopic(slug) {
        if (selectedTopic === slug) return;

        selectedTopic = slug;
        for (const [object, original] of originals)
            object.material = object.userData.topic === slug ? highlights.get(object) : original;
        draw();
        notify("PreviewTopic", slug);
    }
    function pick(event) {
        const rect=canvas.getBoundingClientRect(), ray=new T.Raycaster();
        ray.setFromCamera(new T.Vector2(2*(event.clientX-rect.left)/rect.width-1,
            1-2*(event.clientY-rect.top)/rect.height),camera);
        return ray.intersectObjects(root.children).find(hit=>hit.object.userData.topic)?.object.userData.topic ?? null;
    }
    canvas.onpointerdown=e=>{
        stopEntry();

        dragged = false;
        pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
        start=[e.clientX,e.clientY,yaw,pitch,target.x,target.z];
        canvas.setPointerCapture(e.pointerId);
        if(id === "outpost" && pointers.size === 2){
            const [a,b]=[...pointers.values()];pinchDistance=Math.hypot(a.x-b.x,a.y-b.y);start=null;dragged=true;
        }
    };
    canvas.onpointermove=e=>{
        if(pointers.has(e.pointerId)) pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
        if(id === "outpost" && pointers.size === 2){
            const [a,b]=[...pointers.values()],distance=Math.hypot(a.x-b.x,a.y-b.y);
            if(pinchDistance>0) zoom=T.MathUtils.clamp(zoom*pinchDistance/Math.max(1,distance),.45,1.6);
            pinchDistance=distance;draw();return;
        }
        if (start) {
            if (!dragged && Math.hypot(e.clientX-start[0],e.clientY-start[1]) < 6) return;
            dragged = true;
            if(id === "outpost"){
                const scale=(camera.top-camera.bottom)/canvas.clientHeight;
                const dx=(e.clientX-start[0])*scale,dz=(e.clientY-start[1])*scale/Math.sin(pitch);
                target.x=start[4]-dx*Math.cos(yaw)-dz*Math.sin(yaw);
                target.z=start[5]+dx*Math.sin(yaw)-dz*Math.cos(yaw);limitPan();
            }else{
                yaw=start[2]-(e.clientX-start[0])*.009;
                pitch=tilt(start[3]+(e.clientY-start[1])*.006);
            }
            draw();
        } else if (id === "outpost" && e.pointerType !== "touch") {
            const slug = pick(e);
            canvas.style.cursor = slug ? "pointer" : "grab";

        }
    };
    canvas.onpointercancel=e=>{start=null;pinchDistance=null;pointers.delete(e.pointerId)};
    canvas.onpointerup=e=>{
        const tap=!dragged&&start&&Math.hypot(e.clientX-start[0],e.clientY-start[1])<6;start=null;pinchDistance=null;pointers.delete(e.pointerId);
        if(!tap||id!=="outpost")return;
        const slug = pick(e);
        if(slug) {
            selectTopic(slug);
            notify("SelectTopic",slug);
        }
    };
    canvas.onwheel=e=>{
        if(id !== "outpost" || !expanded)return;
        e.preventDefault();stopEntry();zoom=T.MathUtils.clamp(zoom*Math.exp(e.deltaY*.001),.45,1.6);draw();
    };
    canvas.onkeydown=e=>{
        if(e.key === "Escape") { selectTopic(null); return; }
        if(!["ArrowLeft","ArrowRight","ArrowUp","ArrowDown"].includes(e.key))return;
        e.preventDefault();
        stopEntry();
        if(id === "outpost"){
            const dx=e.key==="ArrowLeft"?-.6:e.key==="ArrowRight"?.6:0;
            const dz=e.key==="ArrowUp"?.6:e.key==="ArrowDown"?-.6:0;
            target.x+=dx*Math.cos(yaw)+dz*Math.sin(yaw);
            target.z+=-dx*Math.sin(yaw)+dz*Math.cos(yaw);limitPan();
        }else{
            if(e.key==="ArrowLeft")yaw-=.15;if(e.key==="ArrowRight")yaw+=.15;
            if(e.key==="ArrowUp")pitch=tilt(pitch+.12);if(e.key==="ArrowDown")pitch=tilt(pitch-.12);
        }
        draw();
    };
    const observer=new ResizeObserver(draw);observer.observe(host);draw();
    const textures=new Set(),geometries=new Set(),materials=new Set();
    views.set(host,{
        clearTopic(){selectTopic(null)},
        setExpanded(value){
            expanded=value;selectTopic(null);
            if(value) enterMap(); else { stopEntry(); draw(); }
            if(value) view.querySelector(".map-exit")?.focus({preventScroll:true});
        },
        reset(){stopEntry();yaw=.42;pitch=initialPitch;zoom=initialZoom;target.copy(center);draw()},
        zoom(delta){stopEntry();zoom=Math.max(.45,Math.min(1.6,zoom+delta));draw()},
        dispose(){
            disposed = true;
            stopEntry();
            view.removeEventListener("keydown",trapFocus);
            observer.disconnect();
            for (const [object, original] of originals) object.material = original;
            highlights.forEach(material => material.dispose());
            scene.traverse(o=>{if(o.geometry)geometries.add(o.geometry);if(o.material){materials.add(o.material);if(o.material.map)textures.add(o.material.map)}});
            textures.forEach(t=>t.dispose());geometries.forEach(g=>g.dispose());materials.forEach(m=>m.dispose());
            renderer.dispose();renderer.forceContextLoss();canvas.remove();
        }
    });
    return references[id];
}
export function setExpanded(host,value){views.get(host)?.setExpanded(value)}
export function clearTopic(host){views.get(host)?.clearTopic()}
export function reset(host){views.get(host)?.reset()}
export function zoom(host,delta){views.get(host)?.zoom(delta)}
export function unmount(host){views.get(host)?.dispose();views.delete(host)}
