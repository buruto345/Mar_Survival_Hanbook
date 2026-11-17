import {yieldTask} from "./viewerScheduling.js";
import { buildLandscape, elevation } from "./outpostTerrain.js";
import * as T from "../lib/three/three.module.min.js";

export async function buildOutpost(root, primitives, C,signal) {
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
    const archGlass=new T.MeshStandardMaterial({color:0x98bfbc,transparent:true,opacity:.32,
        side:T.DoubleSide,depthWrite:false,roughness:.27,metalness:.16});
    archGlass.forceSinglePass=true;
    const thermalShell=new T.MeshStandardMaterial({color:0xc4b39a,roughness:.74,metalness:.18,side:T.DoubleSide});
    const ribProfiles=new Map();
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
        const skin = new T.Mesh(geometry, glass ? archGlass : thermalShell);
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
            for (const angle of [.58,1.57,2.56]) {
                const px=x+radius*Math.cos(angle),py=.12+radius*Math.sin(angle);
                rod([px,py,z-length/2],[px,py,z+length/2],.023,C.silver);
            }
        }
        const profile=[radius,glass].join("/");
        if(!ribProfiles.has(profile)) {
            ribProfiles.set(profile,new T.TorusGeometry(radius,glass ? .032 : .055,6,32,Math.PI));
        }
        for(let rib=0;rib<=6;rib++){
            mesh(ribProfiles.get(profile),glass ? C.silver : 0x9b8d79,x,.12,z+(rib/6-.5)*length);
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
    function airlock(x, z, external = true) {
        box(x, .5, z, .82, .85, .65, C.white);
        cylinder(x, .58, z + .36, .31, .11, C.silver).rotation.x = Math.PI / 2;
        cylinder(x, .58, z + .43, .22, .03, C.dark).rotation.x = Math.PI / 2;
        for (const side of [-1,1]) {
            box(x+side*.35,.58,z+.34,.075,.73,.08,C.silver);
            box(x+side*.28,.52,z+.40,.06,.13,.08,C.gold);
        }
        const lamp = box(x,.93,z+.34,.42,.055,.055,0xffdc9f);
        lamp.material.emissive.set(0xffaf54);
        if (!external) return;
        box(x, .08, z + .75, .85, .08, .7, C.silver);
        for (const side of [-1, 1]) rod([x + side * .4, .12, z + 1],
            [x + side * .4, .52, z + .4], .025, C.silver);
    }
    function passage(a, b) {
        // Both ends enter either a pressure bulkhead or a junction enclosure.
        rod(a,b,.38,0xa49a83).userData.route = true;
        const start = new T.Vector3(...a), finish = new T.Vector3(...b);
        const axis = finish.clone().sub(start).normalize();
        const stations = Math.ceil(start.distanceTo(finish)/.54);
        for(let i=0;i<=stations;i++) {
            const p=start.clone().lerp(finish,i/stations);
            const end=i===0 || i===stations;
            const collar=rod(p.clone().addScaledVector(axis,-.045).toArray(),
                p.clone().addScaledVector(axis,.045).toArray(),end ? .445 : .4,end ? C.dark : C.silver);
            collar.userData.route=true;
            if(i%2===0) box(p.x,.10,p.z,.44,.25,.43,0x777269);
        }
    }
    function foundation(x,z,width,length) {
        box(x,-.038,z,width,.16,length,0x8c8271);
        for (const side of [-1,1]) {
            box(x+side*(width/2-.10),.055,z,.13,.09,length-.12,0x5e6260);
            for (const end of [-1,1]) cylinder(x+side*(width/2-.1),.112,z+end*(length/2-.15),.058,.034,C.silver);
        }
    }
    // Hard elbows and saddles keep each run inspectable above the service bed.
    function utilityRun(points,color,radius) {
        for(let i=1;i<points.length;i++) {
            const a=points[i-1], b=points[i];
            rod(a,b,radius,color).userData.route=true;
            const length=Math.hypot(b[0]-a[0],b[2]-a[2]);
            const bays=Math.ceil(length/1.15);
            for(let j=1;j<bays;j++) {
                const t=j/bays, x=a[0]+(b[0]-a[0])*t, z=a[2]+(b[2]-a[2])*t;
                box(x,(a[1]-.06)/2,z,radius*3,a[1]+.06,radius*3,C.dark);
            }
            if(i<points.length-1) sphere(...b,radius*1.18,radius*1.18,radius*1.18,color);
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

    await buildLandscape(root,random,signal);
    // Two long greenhouses occupy the foreground, as in the supplied reference.
    await yieldTask(signal);
    topic("food", () => {
        for (const x of [-2, 1.1]) {
            const z = 3.15, radius = 1.32, length = 5.2;
            foundation(x,z,2.92,5.47);
            box(x, .07, z, 2.7, .12, 5.3, C.fabric);
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
            // A reinforced end wall receives the north-facing transfer sleeve.
            for(const end of [-1,1]) {
                const face=mesh(new T.CircleGeometry(radius,28,0,Math.PI),C.white,x,.12,z+end*length/2);
                face.material=archGlass;
                face.material.side=T.DoubleSide;
            }
            box(x,.49,.54,.96,.93,.23,C.fabric);
            passage([x,.52,-.80],[x,.52,.59]);
        }
        sign(-2, 6.05, "Greenhouses");
    });
    await yieldTask(signal);
    topic("housing", () => {
        foundation(-4.7,-2.72,2.52,4.16);
        arch(-4.7, -2.8, 1.05, 3.5);
        // Replaceable outer insulation blankets, with openings at the side transfer port.
        for(const side of [-1,1]) for(let bay=0;bay<6;bay++){
            if(side===1 && (bay===2 || bay===3)) continue;
            const z=-4.25+bay*.58;
            const panel=box(-4.7+side*.88,.62,z,.14,.61,.52,0xb69e80);
            panel.rotation.z=side*.39;
        }
        airlock(-4.7, -.8);
        for (const x of [-5.2, -4.3])
            cylinder(x, .68, -.99, .16, .03, C.blue).rotation.x = Math.PI / 2;
        passage([-3.88,.52,-2.85],[-1.10,.52,-2.85]);
        utilityCabinet(-5.9,-1.55,C.fabric);
        plaque(-4.7,.93,-.45,"HAB  /  01",.64);
        sign(-5, -.05, "Housing");
    });
    await yieldTask(signal);
    topic("community", () => {
        foundation(-.3,-3.18,2.58,3.19);
        arch(-.3, -3.25, 1.05, 2.6);
        // Raised observation bay has a continuous sill and individual mullions.
        box(-.3,1.04,-3.34,1.28,.39,1.37,C.dark);
        box(-.3,1.26,-3.34,1.42,.10,1.52,C.fabric);
        for(const side of [-1,1]) for(let bay=0;bay<3;bay++) {
            box(-.3+side*.65,1.09,-3.77+bay*.43,.036,.22,.35,C.blue);
        }
        airlock(-.3, -1.7,false);
        passage([-.3,.52,-1.46],[-.3,.52,-.80]);
        passage([-2,.52,-.80],[1.1,.52,-.80]);
        for(const x of [-2,-.3,1.1]){
            box(x,.51,-.8,.84,.93,.83,C.fabric);
            box(x,.99,-.8,.86,.075,.85,C.silver);
        }
        for(const offset of [-.72,.72]) {
            box(-.3+offset,.71,-1.87,.39,.29,.07,C.silver);
            box(-.3+offset,.71,-1.82,.33,.22,.035,C.blue);
        }
        utilityCabinet(.97,-2.0,C.fabric);
        sign(-.3, -.4, "Community");
    });
    await yieldTask(signal);
    topic("water-and-oxygen", () => {
        foundation(3.1,-2.35,2.05,3.12);
        foundation(5.3,-2.25,2.88,2.02);
        arch(3.1, -2.4, .88, 2.6);
        box(2.42,.50,-3.30,.58,1,.92,C.fabric);
        airlock(3.1, -.9);
        for (const x of [4.4, 5.3, 6.2]) vessel(x, .72, -2.3, .33, 1.1);
        // The treatment station feeds separate liquid and gas headers.
        utilityRun([[3.55,.28,-1.50],[5.30,.28,-1.50]],C.water,.065);
        utilityRun([[6.20,.38,-2.30],[6.20,.38,-3.06],[3.45,.38,-3.06]],0x709889,.057);
        utilityRun([[3.05,.22,-1.3],[3.05,.22,.42],[1.9,.22,.42],[2.2,.22,.42],[2.2,.22,1.12],[2.2,.37,1.12]],C.water,.054);
        passage([.52,.52,-3.3],[2.62,.52,-3.3]);
        for (const [i,x] of [4.4,5.3,6.2].entries()) {
            const color = i===2 ? 0x87b69b : C.water;
            cylinder(x,.88,-2.3,.34,.13,color);
            if(i<2) utilityRun([[x,.28,-2.3],[x,.28,-1.50]],color,.06);
            ring(x,i<2 ? .35 : .47,i<2?-1.50:-3.06,.115,C.red,Math.PI/2);
            cylinder(x,1.02,-1.96,.083,.045,C.white).rotation.x=Math.PI/2;
        }
        utilityCabinet(3.95,-.93,C.water);
        sign(4.8, -.8, "Water & oxygen");
    });
    await yieldTask(signal);
    topic("energy", () => {
        foundation(5.8,-5.5,1.53,1.56);
        for(let row=0;row<3;row++) {
            const z=-6.35+row*.8;
            box(3.28,.025,z,4.12,.18,.20,0x787366);
            utilityRun([[1.64,.15,z],[5.12,.15,z]],C.dark,.032);
        }
        utilityRun([[5.12,.15,-6.35],[5.12,.15,-4.75]],C.dark,.052);
        utilityRun([[5.12,.15,-5.5],[5.38,.15,-5.5]],C.dark,.052);
        solarField(1.65, -6.35);
        box(5.8, .35, -5.5, 1.3, .7, 1.3, C.white);
        box(5.8,.74,-5.5,1.43,.10,1.42,C.fabric);
        for(const side of [-1,1]) box(5.8+side*.58,.35,-4.82,.07,.66,.09,C.silver);
        for (let j = 0; j < 6; j++) box(5.8, .3 + j * .07, -4.83, 1.1, .025, .04, C.dark);
        utilityRun([[5.8,.22,-4.9],[5.8,.22,-4.15],[4.65,.22,-4.15]],C.dark,.082);
        utilityRun([[4.65,.18,-4.15],[3.10,.18,-4.15],[3.10,.18,-3.62]],C.dark,.072);
        utilityCabinet(4.65,-4.15,C.gold);
        sign(5.7, -4.2, "Energy");
    });
    await yieldTask(signal);
    topic("sanitation-and-waste", () => {
        foundation(5.4,2.2,2.86,2.94);
        arch(5.4, 2, .7, 1.8);
        airlock(5.4, 3.1);
        for (const x of [4.35, 6.5]) {
            cylinder(x, .35, 2.3, .24, .7, C.silver);
            cylinder(x, .73, 2.3, .25, .07, C.dark);
        }
        // A dedicated return terminates at the recovery unit and a service vault.
        utilityRun([[5.4,.24,1.19],[5.4,.24,.16],[4.20,.24,.16],[4.20,.24,-.41]],0xa57947,.085);
        box(4.20,.22,-.52,.53,.53,.43,C.dark);
        box(4.20,.52,-.52,.56,.09,.46,0xa57947);
        for(const x of [4.35,6.5]) utilityRun([[x,.26,2.3],[x,.26,1.84],[5.4,.26,1.84]],0xa57947,.058);
        for(const x of [4.35,6.5]) {
            cylinder(x,.46,2.3,.247,.11,C.gold);

        }
        utilityCabinet(6.45,1.14,C.gold);
        plaque(5.4,.91,3.45,"RECOVERY",.62);
        sign(5.4, 4.15, "Waste");
    });
    // A dish and a parked utility rover give the outpost a working surface scale.
    foundation(-6.5,2,.94,.94);
    rod([-6.5, .05, 2], [-6.5, 1.3, 2], .06, C.silver);
    const dish = mesh(new T.SphereGeometry(.55, 24, 12, 0, Math.PI * 2, 0, .95),
        C.white, -6.5, 1.35, 2);
    dish.rotation.z = -.6;
    rod([-6.5, 1.35, 2], [-6.15, 1.9, 2], .025, C.dark);
    foundation(5.2,5.6,2.85,1.52);
    box(5.40,.62,5.6,.59,.35,.64,C.fabric);
    box(5.65,.65,5.6,.11,.22,.53,C.blue);
    box(4.79,.49,5.6,.37,.15,.56,C.silver);
    box(5.2, .32, 5.6, 1.1, .32, .7, C.white);
    for (const x of [4.8, 5.6]) for (const z of [5.18, 6.02])
        cylinder(x, .23, z, .19, .16, C.dark).rotation.x = Math.PI / 2;
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
        if (!part.isMesh || part.isInstancedMesh || part.material.transparent) continue;
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
