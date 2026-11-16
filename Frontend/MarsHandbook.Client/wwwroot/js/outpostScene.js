import * as T from "../lib/three/three.module.min.js";

export function buildOutpost(root, primitives, C) {
    const { mesh, box, cylinder, sphere, tube, rod, ring, vessel, plaque } = primitives;
    // Stable layout and variation across visits to the same outpost.
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
        if (!glass) {
            for (const end of [-1,1]) {
                const bulkhead = mesh(new T.CircleGeometry(radius,32,0,Math.PI),
                    C.white,x,.12,z+end*length/2);
                if(end<0) bulkhead.rotation.y=Math.PI;
                box(x,.14,z+end*length/2,2*radius,.22,.09,C.fabric);
            }
            for (const angle of [.23,.77,2.37,2.91]) {
                const px=x+radius*Math.cos(angle), py=.12+radius*Math.sin(angle);
                rod([px,py,z-length/2],[px,py,z+length/2],.018,C.fabric);
            }
        } else {
            skin.material.forceSinglePass = true;
        }
        for (let rib = 0; rib <= 6; rib++) {
            const line = [];
            for (let j = 0; j <= 32; j++) {
                const angle = j * Math.PI / 32;
                line.push([x + radius * Math.cos(angle), .12 + radius * Math.sin(angle),
                    z + (rib / 6 - .5) * length]);
            }
            tube(line, glass ? C.silver : C.fabric, glass ? .027 : .055);
        }
        for (const side of [-1, 1]) {
            box(x + side * radius, .15, z, .13, .22, length, C.white);
        }
    }
    function utilityCabinet(x,z,accent) {
        box(x,.33,z,.52,.64,.48,C.silver);
        box(x,.34,z+.25,.44,.51,.035,C.dark);
        for(let i=0;i<4;i++) box(x,.21+i*.075,z+.28,.32,.025,.035,accent);
    }
    function airlock(x, z) {
        box(x, .5, z, .82, .85, .65, C.white);
        cylinder(x, .58, z + .36, .31, .11, C.silver).rotation.x = Math.PI / 2;
        cylinder(x, .58, z + .43, .22, .03, C.dark).rotation.x = Math.PI / 2;
        for (const side of [-1,1]) {
            box(x+side*.35,.58,z+.34,.075,.73,.08,C.silver);
            box(x+side*.28,.52,z+.40,.06,.13,.08,C.gold);
        }
        const lamp = box(x,.93,z+.34,.42,.055,.055,0xffdc9f);
        lamp.material.emissive.set(0xffaf54);
        box(x, .08, z + .75, .85, .08, .7, C.silver);
        for (const side of [-1, 1]) rod([x + side * .4, .12, z + 1],
            [x + side * .4, .52, z + .4], .025, C.silver);
    }
    function passage(a, b) {
        rod(a, b, .27, C.white).userData.route = true;
        const axis = new T.Vector3(...b).sub(new T.Vector3(...a)).normalize();
        for (let j = 0; j <= 7; j++) {
            const point = new T.Vector3(...a).lerp(new T.Vector3(...b), j / 7);
            const collar = ring(point.x, point.y, point.z, .28, C.silver);
            collar.quaternion.setFromUnitVectors(new T.Vector3(0, 0, 1), axis);
        }
    }
    function sign(x, z, text) {
        const anchor = new T.Object3D();
        anchor.position.set(x, 1.65, z);
        anchor.userData.label = text;
        root.add(anchor);
    }
    function solarField(x, z) {
        for (let row = 0; row < 3; row++) for (let col = 0; col < 6; col++) {
            const px = x + col * .65, pz = z + row * .8;
            const angle = -.23;
            const panel = (dx,dy,dz,w,h,d,color) => {
                const part = box(px+dx,.38+dy*Math.cos(angle)-dz*Math.sin(angle),
                    pz+dy*Math.sin(angle)+dz*Math.cos(angle),w,h,d,color);
                part.rotation.x = angle;
            };
            panel(0,0,0,.63,.055,.73,C.silver);
            panel(0,.034,0,.59,.018,.69,0x183c55);
            for (const dx of [-.15,0,.15]) panel(dx,.045,0,.008,.005,.68,0x91a5ac);
            for (const dz of [-.18,0,.18]) panel(0,.045,dz,.58,.005,.009,0x7393a0);
            rod([px, .08, pz], [px, .32, pz], .025, C.silver);
        }
    }

    function elevation(x,z) {
        const edge = Math.max(Math.abs(x)-6.7, Math.abs(z)-5.8, 0);
        const wind = Math.sin(.72*x + .24*z + .8*Math.sin(z*.48));
        return -.075 + .014*wind + Math.min(edge*.09,.8) * (.6+.4*wind);
    }
    const groundGeometry = new T.PlaneGeometry(120, 120, 112, 112);
    groundGeometry.rotateX(-Math.PI / 2);
    const positions = groundGeometry.attributes.position;
    const colors = [];
    const soil = new T.Color();
    for (let i = 0; i < positions.count; i++) {
        const x = positions.getX(i), z = positions.getZ(i);
        const wind = Math.sin(.72*x + .24*z + .8*Math.sin(z*.48));
        positions.setY(i,elevation(x,z));

        soil.setHSL(.034 + .006*wind, .58 + random()*.06, .205 + .021*wind + random()*.018);
        colors.push(soil.r, soil.g, soil.b);
    }
    groundGeometry.setAttribute("color", new T.Float32BufferAttribute(colors, 3));
    groundGeometry.computeVertexNormals();
    const grainCanvas = document.createElement("canvas");
    grainCanvas.width = grainCanvas.height = 256;
    const ctx = grainCanvas.getContext("2d");
    const pixels = ctx.createImageData(256,256);
    for(let y=0;y<256;y++) for(let x=0;x<256;x++) {
        const offset=4*(y*256+x);
        const ripple=Math.sin(2*Math.PI*(y/32+Math.sin(x*Math.PI/128)*.38));
        const grain=218+random()*27+ripple*(2.5+Math.sin(x*Math.PI/128));
        pixels.data.set([grain,grain,grain,255],offset);
    }
    ctx.putImageData(pixels,0,0);
    const dust = new T.CanvasTexture(grainCanvas);
    dust.colorSpace=T.SRGBColorSpace;
    dust.wrapS=dust.wrapT=T.RepeatWrapping;
    dust.repeat.set(52,52);
    const ground = new T.Mesh(groundGeometry,
        new T.MeshStandardMaterial({vertexColors: true, map:dust, roughness: 1}));
    ground.receiveShadow = true; ground.userData.mapGround = true; root.add(ground);
    const stone = new T.DodecahedronGeometry(1,0);
    for (let i = 0; i < 280; i++) {
        const x = (random() - .5) * 16.5, z = (random() - .5) * 14.5;
        // Keep the central buildings and greenhouse beds clear of rocks.
        if (Math.abs(x) < 6.5 && Math.abs(z) < 5.6) continue;
        const scale = .035 + random() * .14;
        const rock = mesh(stone, i % 2 ? 0x744631 : 0x9d6041,
            x, elevation(x,z)+scale*.27, z);
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
                rod([x+dx+.30,.37,z-2.29],[x+dx+.30,.37,z+2.29],.027,C.water).userData.route = true;
                const light=box(x+dx*.7,1.22,z,.048,.032,4.65,0xffdc9f);
                light.material.emissive.set(0xe9ac67);
                for (let plant = 0; plant < 12; plant++) {
                    const pz = z - 2.05 + plant * .37;
                    const height = .10 + random()*.14, rotation = random()*Math.PI;
                    for (let leaf=0;leaf<5;leaf++) {
                        const angle = rotation + leaf * 2.4;
                        const blade = sphere(x+dx+.09*Math.sin(angle),.43+height*.55,pz+.09*Math.cos(angle),
                            .052+random()*.035,.045,.10+height*.45,[0x426343,0x668347,0x90a65c][plant%3]);
                        blade.rotation.order = "YXZ";
                        blade.rotation.set(-.48-random()*.25,angle,0);
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
        sign(-2, 5.3, "Greenhouses");
    });
    topic("housing", () => {
        arch(-4.7, -2.8, 1.05, 3.5);
        airlock(-4.7, -.8);
        for (const x of [-5.2, -4.3])
            cylinder(x, .68, -.99, .16, .03, C.blue).rotation.x = Math.PI / 2;
        passage([-4.3, .48, -2.8], [-1.6, .48, -2.8]);
        utilityCabinet(-5.9,-1.55,C.fabric);
        plaque(-4.7,.93,-.45,"HAB  /  01",.64);
        sign(-5, -.05, "Housing");
    });
    topic("community", () => {
        arch(-.3, -3.25, 1.05, 2.6);
        airlock(-.3, -1.7);
        passage([-.3, .45, -1.5], [-.3, .45, -.6]);
        for(const offset of [-.72,.72]) {
            box(-.3+offset,.71,-1.87,.39,.29,.07,C.silver);
            box(-.3+offset,.71,-1.82,.33,.22,.035,C.blue);
        }
        utilityCabinet(.97,-2.0,C.fabric);
        sign(-.3, -.4, "Community");
    });
    topic("water-and-oxygen", () => {
        arch(3.1, -2.4, .72, 2.6);
        airlock(3.1, -.9);
        for (const x of [4.4, 5.3, 6.2]) vessel(x, .72, -2.3, .33, 1.1);
        for (const z of [-2.9, -1.5])
            tube([[3.8, .2, z], [4.5, .2, z], [6.2, .2, z]], C.water, .045).userData.route = true;
        passage([.7, .45, -3.3], [2.4, .45, -3.3]);
        for (const [i,x] of [4.4,5.3,6.2].entries()) {
            const color = i===2 ? 0x87b69b : C.water;
            cylinder(x,.88,-2.3,.34,.13,color);
            rod([x,.19,-2.3],[x,.19,-1.53],.06,color);
            ring(x,.35,-1.52,.115,C.red,Math.PI/2);
            cylinder(x,1.02,-1.96,.083,.045,C.white).rotation.x=Math.PI/2;
        }
        utilityCabinet(3.95,-.93,C.water);
        sign(4.8, -.8, "Water & oxygen");
    });
    topic("energy", () => {
        solarField(1.65, -6.35);
        box(5.8, .35, -5.5, 1.3, .7, 1.3, C.white);
        for (let j = 0; j < 6; j++) box(5.8, .3 + j * .07, -4.83, 1.1, .025, .04, C.dark);
        tube([[5.8, .1, -4.9], [5.8, .1, -3.4], [3, .1, -3.4]], C.dark, .035).userData.route = true;
        utilityCabinet(4.65,-4.15,C.gold);
        sign(5.7, -4.2, "Energy");
    });
    topic("sanitation-and-waste", () => {
        arch(5.4, 2, .7, 1.8);
        airlock(5.4, 3.1);
        for (const x of [4.35, 6.5]) {
            cylinder(x, .35, 2.3, .24, .7, C.silver);
            cylinder(x, .73, 2.3, .25, .07, C.dark);
        }
        tube([[5.4, .1, 1], [5.4, .1, .1], [4.5, .1, -1.5]], C.gold, .04).userData.route = true;
        for(const x of [4.35,6.5]) {
            cylinder(x,.46,2.3,.247,.11,C.gold);
            rod([x,.21,2.3],[x,.21,1.25],.065,C.dark);
        }
        utilityCabinet(6.45,1.14,C.gold);
        plaque(5.4,.91,3.45,"RECOVERY",.62);
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
    for (let i=0;i<20;i++) {
        const x=2.85+i*.105;
        for (const z of [5.18,6.02]) {
            const track=box(x,elevation(x,z)+.017,z,.047,.018,.16,0x845941);
            track.rotation.y=.22;
            track.castShadow=false;
        }
    }
    batchStaticParts(root);
}

// Batch within a facility so a picked instance retains its category.
function batchStaticParts(root) {
    const batches = new Map();
    for (const part of root.children) {
        if (!part.isMesh || part.material.transparent) continue;
        const key = [part.userData.topic,part.userData.route,part.geometry.uuid,part.material.uuid].join("/");
        if (!batches.has(key)) batches.set(key,[]);
        batches.get(key).push(part);
    }
    for (const parts of batches.values()) {
        if (parts.length < 2) continue;
        const first = parts[0];
        const batch = new T.InstancedMesh(first.geometry,first.material,parts.length);
        batch.userData = { ...first.userData };
        batch.castShadow = first.castShadow;
        batch.receiveShadow = first.receiveShadow;
        parts.forEach((part,index) => {
            part.updateMatrix();
            batch.setMatrixAt(index,part.matrix);
            root.remove(part);
        });
        batch.computeBoundingSphere();
        root.add(batch);
    }
}
