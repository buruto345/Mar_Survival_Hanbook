import {acquireRenderer,releaseRenderer,storeScene,resources,disposeResources} from "./viewerResources.js";
import {yieldTask,afterPaint,textureCanvas,markPhase} from "./viewerScheduling.js";
import { buildOutpost } from "./outpostScene.js";
import * as T from "../lib/three/three.module.min.js";

const views = new WeakMap();
const C = { white: 0xe6e3d8, silver: 0x9da9ae, dark: 0x242d35, blue: 0x224457,
    gold: 0xc99b42, red: 0xbc6848, water: 0x49afcb, green: 0x587d42, fabric: 0xb8ae95 };

const nasa = "https://www.nasa.gov/";
const habitat = nasa + "humans-in-space/chapea/habitat/";
const eclss = nasa + "reference/environmental-control-and-life-support-systems-eclss/";
export const references = {
    outpost: [habitat, "CHAPEA habitat", "Outpost composition inspired by the supplied Mars-base illustration. Habitat details reference NASA research."],
    "rooms-for-four": [habitat, "CHAPEA habitat", "Printed wall courses, compact quarters and shared workspaces."],
    "time-off-in-isolation": [nasa + "humans-in-space/chapea/chapea-mission-1/", "CHAPEA crew photographs", "Illustrative interior and crew placement."],
    "waiting-for-a-reply": [nasa + "humans-in-space/chapea/chapea-mission-2/", "CHAPEA mission photographs", "Habitat workstation and remote support desk. Layout is illustrative."],
    "expandable-room": [nasa + "international-space-station/bigelow-expandable-activity-module-beam/", "BEAM photographs", "Fabric envelope, restraint bands and rigid attachment collar."],
    "staging-water-loop": [eclss, "NASA life-support hardware", "Rack frames, process vessels, plumbing and control panels. Flow arrangement is simplified."],
    "water-to-breathing-oxygen": [eclss, "NASA life-support hardware", "Rack-mounted electrolysis equipment with illustrative water and gas lines."],
    "oxygen-from-martian-air": [nasa + "solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/", "MOXIE installation photograph", "Gold instrument enclosure and triangular stiffening ribs, shown enlarged beside the rover."],
    "packing-the-last-meal": [nasa + "directorates/esdmd/hhp/space-food-systems/", "NASA space food photographs", "Sealed pouches, drink fittings and labelled storage bags."],
    "veggie-salad": [nasa + "exploration-research-and-technology/growing-plants-in-space/", "Veggie hardware photographs", "Six plant pillows, LED canopy and flexible chamber enclosure."],
    "dust-and-solar-budget": ["https://science.nasa.gov/resource/insights-power-generation-after-landing-and-spring-2022/", "InSight comparison photographs", "Two circular, ribbed arrays in clean and dusty states."],
    "perseverance-heat-and-power": ["https://science.nasa.gov/mission/mars-2020-perseverance/rover-components/", "Perseverance hardware photographs", "Six treaded wheels, suspension links, camera mast and finned generator."],
    "separating-toilet-waste": [nasa + "humans-in-space/boldly-go-nasas-new-space-toilet-offers-more-comfort-improved-efficiency-for-deep-space-missions/", "NASA UWMS photographs", "Seat ring, collection funnel and hose. Downstream routes are illustrative."],
    "trash-storage-tiles": [nasa + "ames/space-biosciences/what-is-nasas-heat-melt-compactor/", "Heat Melt Compactor photographs", "Metal process chamber, instrumentation and compressed trash tiles."]
};

async function build(root, id, article, signal) {
    const materials = new Map();
    const finish=article?.finishes(C);
    function material(color) {
        if (!materials.has(color)) {
            const surface = new T.MeshStandardMaterial({
                color, roughness: color === C.silver ? .34 : .77,
                metalness: [C.silver, C.gold].includes(color) ? .65 : .08
            });
            if (id === "outpost" || article) {
                if (color===C.blue || color===0x183c55) {
                    surface.roughness=.28; surface.metalness=.33;
                } else if (color===C.fabric) {
                    surface.roughness=.91;
                } else if (color===C.dark) {
                    surface.roughness=.68; surface.metalness=.24;
                }
            }
            finish?.(surface,color);
            materials.set(color,surface);
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
    const shared = id === "outpost" || article ? {
        box: new T.BoxGeometry(), cylinder: new T.CylinderGeometry(1,1,1,16),
        sphere: new T.SphereGeometry(1,14,10)
    } : null;
    function box(x,y,z,w,h,d,c) {
        const part = mesh(shared?.box ?? new T.BoxGeometry(w,h,d),c,x,y,z);
        if (shared) part.scale.set(w,h,d);
        return part;
    }
    function cylinder(x,y,z,r,h,c) {
        const part = mesh(shared?.cylinder ?? new T.CylinderGeometry(r,r,h,24),c,x,y,z);
        if (shared) part.scale.set(r,h,r);
        return part;
    }
    function sphere(x,y,z,a,b,c,color) {
        const object = mesh(shared?.sphere ?? new T.SphereGeometry(1,24,16),color,x,y,z);
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
        const canvas = textureCanvas();
        canvas.width = 512; canvas.height = 96;
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#e8e5da"; ctx.fillRect(0,0,512,96);
        ctx.fillStyle = "#26353d"; ctx.font = "bold 36px sans-serif";
        ctx.textAlign = "center"; ctx.fillText(text,256,60,480);
        const map = new T.CanvasTexture(canvas); map.colorSpace = T.SRGBColorSpace;
        const object = new T.Mesh(new T.PlaneGeometry(width,width*96/512),
            new T.MeshBasicMaterial({map,side:T.DoubleSide,toneMapped:!article}));
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
        display.material = material(0x244c59);display.material.emissive.setHex(0x327d88);display.material.emissiveIntensity=.4;
        if(id==="waiting-for-a-reply"){
            const canvas=textureCanvas();canvas.width=320;canvas.height=160;
            const ctx=canvas.getContext("2d");ctx.fillStyle=x<0?"#17343a":"#1a2a38";ctx.fillRect(0,0,320,160);
            ctx.fillStyle="#daece6";ctx.font="19px monospace";ctx.fillText(x<0?"HAB / OUTBOX":"EARTH / INBOX",13,26);
            ctx.strokeStyle="#487075";for(let i=0;i<8;i++){ctx.beginPath();ctx.moveTo(i*44,47);ctx.lineTo(i*44,140);ctx.stroke()}
            ctx.strokeStyle="#9ce2ca";ctx.lineWidth=3;ctx.beginPath();
            for(let i=0;i<295;i++)ctx.lineTo(12+i,95+19*Math.sin(i*.07+x)+7*Math.cos(i*.29));ctx.stroke();
            const texture=new T.CanvasTexture(canvas);texture.colorSpace=T.SRGBColorSpace;
            const panel=new T.Mesh(new T.PlaneGeometry(w,h),new T.MeshBasicMaterial({map:texture}));panel.position.set(x,y,z+.056);root.add(panel);
        }else for (let i=0;i<3;i++) box(x-w*.28,y-h*.27+i*h*.23,z+.058,w*(.6-i*.12),.012,.005,C.white);
    }
    function rack(x,name) {
        for (const dx of [-.69,.69]) for (const z of [-.48,.48])
            box(x+dx,1.34,z,.08,2.6,.08,C.silver);
        for (const y of [.08,2.6]) {
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
    const tools={mesh,box,cylinder,rod,sphere,ring,plaque,tube};
    const forms=article?.makeForms(tools,C);
    function crew(x,z,color,seated=false,deck=0) {
        article.updates.push(article.flightCrew(root,article.asset,x,z,color,seated,deck,id));
    }
    function chair(x,z){forms.chair(x,z)}
    function habitatInterior(privateRooms) {
        box(0,.02,0,6.6,.16,3.65,C.fabric);
        // Exposed courses recall the layered walls visible in CHAPEA construction photographs.
        box(0,.88,-1.84,6.62,1.58,.10,C.fabric);
        box(-3.29,.88,0,.10,1.58,3.61,C.fabric);
        for(let i=0;i<24;i++) {
            box(0,.16+i*.064,-1.8,6.6,.045,.16,C.fabric);
            box(-3.25,.16+i*.064,0,.16,.045,3.6,C.fabric);
        }
        for(let x=-3;x<3.3;x+=.55)box(x,.109,0,.012,.012,3.5,0x928b7b);
        const count = privateRooms ? 4 : 2;
        for(let i=0;i<count;i++) {
            const x=-2.45+i*1.55;
            box(x,.34,-.85,1.18,.4,1.45,C.white);
            forms.berth(x);
            if(i<count-1)box(x+.72,.83,-.95,.09,1.5,1.65,C.fabric);
            box(x,.89,-1.69,.6,.42,.06,C.dark);screen(x,.89,-1.64,.48,.3);
        }
        if(!privateRooms) {
            box(1.7,.77,.5,1.9,.12,.85,C.white);
            for(const x of [1.2,2.2]){chair(x,1.13);crew(x,1.13,C.blue,true,.10)}
            box(2.68,.48,-.8,.8,.8,1,C.white);
            box(2.68,.93,-.8,.9,.08,1.1,C.dark);
            cylinder(2.68,1.02,-.8,.2,.1,C.silver);
            for(let i=0;i<4;i++)for(let j=0;j<4;j++)
                box(1.7+i*.07,.846,.35+j*.07,.065,.01,.065,(i+j)%2?C.dark:C.white);
            chair(-2.2,.82);crew(-2.2,.82,C.red,true,.10);
        } else {crew(.2,.89,C.red,false,.10);crew(2,1.04,C.blue,false,.10)}
    }
    switch(id) {
    case "rooms-for-four": habitatInterior(true);break;
    case "time-off-in-isolation": habitatInterior(false);break;
    case "outpost": {
        await buildOutpost(root, { mesh, box, cylinder, sphere, tube, rod, ring, vessel, plaque }, C,signal);
        break;
    }
    case "expandable-room":
        for(const [x,length,r] of [[-1.9,.6,.6],[.9,2.2,1]]) {
            const shell=forms.shell(x,length+.52,r,length<1);
            const collars=[],supports=[];
            const left=x-(length+.52)/2, radius=.31;
            for(const sign of [-1,1]){
                const cx=x+sign*(length+.52)/2;
                const collar=cylinder(cx,1.12,0,radius,.12,C.silver);
                collar.rotation.z=Math.PI/2;
                const hatch=cylinder(cx+sign*.067,1.12,0,.243,.045,C.dark);
                hatch.rotation.z=Math.PI/2;
                const trim=ring(cx+sign*.093,1.12,0,.186,C.silver);
                trim.rotation.y=Math.PI/2;
                const foot=box(cx,.10,0,.19,.14,.76,C.dark);
                const post=rod([cx,.15,0],[cx,.86,0],.075,C.silver);
                for(const o of [collar,hatch,trim,foot,post])o.userData.movable=true;
                collars.push([collar,hatch,trim]);supports.push([foot,post]);
            }
            for(const z of [-.26,.26])box(x,.035,z,length+.82,.07,.09,C.silver);
            if(length>1)article.updates.push(t=>{
                const span=(length+.52)*(1-.62*t);
                shell.position.x=left+span/2;
                shell.scale.set(span,r*(1-.38*t),r*(1-.38*t));
                const end=left+span;
                collars[1].forEach((o,i)=>o.position.x=end+[0,.067,.093][i]);
                supports[1].forEach(o=>o.position.x=end);
            });
        }break;
    case "staging-water-loop":
        for(const [x,name] of [[-1.8,"CONDENSATE"],[0,"PROCESSOR"],[1.8,"CLEAN WATER"]]){
            rack(x,name);
            if(x<0){
                forms.vessel(x,.94,0,.47,1.14,C.silver,true);
                for(let j=0;j<12;j++)forms.plate(x,.51+j*.064,0,.62,.022,.54,C.blue);
            }else if(x>0)forms.vessel(x,1.10,0,.46,1.56,C.silver);
            else for(let j=-1;j<=1;j++)forms.vessel(x+j*.39,1.09,0,.177,1.28,j===0?C.blue:C.white);
            for(let i=0;i<3;i++)tube([[x-.45+i*.4,1.65,0],[x-.45+i*.4,1.85,.2],[x+.45,1.85-i*.12,.35],[x+.45,.45,.35]],i===1?C.water:C.silver,.025);
            for(let i=0;i<3;i++)box(x-.42+i*.4,1.95,.48,.2,.07,.08,C.gold);
        }
        {
            const curve=new T.CatmullRomCurve3([[-1.8,.35,.55],[-1,.35,.7],[0,.40,.7],[1.8,.35,.55]].map(p=>new T.Vector3(...p)));
            mesh(new T.TubeGeometry(curve,36,.055,10,false),C.water,0,0,0);
            const indicator=sphere(-1.8,.35,.55,.084,.084,.084,C.white);
            indicator.userData.movable=true;
            article.updates.push(t=>indicator.position.copy(curve.getPointAt(t)));
        }break;
    case "water-to-breathing-oxygen":
        rack(-1.3,"OXYGEN GENERATOR");rack(1.2,"GAS PROCESSING");
        for(let i=0;i<15;i++){
            const plate=forms.plate(-1.3,.64+i*.061,.05,.95,.039,.66,i%2?C.silver:C.dark);
            plate.userData.movable=true;article.updates.push(t=>plate.position.y=.64+i*(.061+.033*t));
        }
        for(const x of [-1.72,-.88])rod([x,.56,.39],[x,1.56,.39],.025,C.gold);
        vessel(1,1.1,0,.21,1.1);vessel(1.5,1.1,0,.14,.9);
        tube([[-2.25,.2,.1],[-2.25,.5,.6],[-1.3,.5,.6]],C.water,.05);
        for(const [i,c] of [[0,C.green],[1,C.gold]])tube([[-1.3,1.55,.3],[-.6,1.8+i*.2,.3],[1+i*.5,1.8+i*.2,.3],[1+i*.5,1.5,0]],c);
        plaque(-1.3,1.75,.5,"ELECTROLYSIS",.85);plaque(1.2,1.78,.5,"O2 / H2",.7);break;
    case "oxygen-from-martian-air":
        article.placeRover(root,article.asset,id,tools);
        // Enlarged, detached instrument shows the triangular wall reinforcement in NASA's installation photo.
        box(-2.45,1.08,.64,1.05,1.12,.08,C.gold);
        for(const dx of [-.49,.49])box(-2.45+dx,1.08,1,.07,1.12,.8,C.gold);
        for(const h of [.55,1.61])box(-2.45,h,1,1.04,.06,.80,C.gold);
        for(let j=0;j<8;j++)forms.plate(-2.60,.78+j*.063,1,.42,.036,.46,C.silver);
        forms.vessel(-2.15,1.03,1,.15,.56,C.dark);
        tube([[-2.15,1.36,1],[-2.18,1.47,1],[-2.60,1.44,1],[-2.6,1.25,1]],C.silver,.035);
        for(const z of [.59,1.41]) {
            for(let j=0;j<3;j++){
                const x=-2.92+j*.31;
                rod([x,.58,z],[x+.16,1.57,z],.016,C.gold);
                rod([x+.16,1.57,z],[x+.31,.58,z],.016,C.gold);
            }
            bolts(-2.45,1.08,z,1.02,1.08);
        }
        for(const x of [-2.76,-2.15])box(x,1.72,1,.13,.18,.13,C.silver);
        plaque(-2.45,.38,1.42,"MOXIE / ENLARGED",1.4);break;
    case "perseverance-heat-and-power": article.placeRover(root,article.asset,id,tools);break;
    case "dust-and-solar-budget":
        article.placeLanders(root,article.asset,tools);
        break;
    case "packing-the-last-meal":
        for(let i=0;i<3;i++){
            const x=(i-1)*1.7;
            box(x,.12,0,1.5,.15,1.25,C.white);
            for(const dx of [-.7,.7])box(x+dx,1,0,.055,1.9,1.2,C.silver);
            for(const y of [.15,1.05,1.95])box(x,y,0,1.48,.055,1.2,C.silver);
            for(const y of [.47,1.37])tube([[x-.65,y,.50],[x-.4,y-.045,.43],[x+.4,y-.045,.43],[x+.65,y,.50]],0x776b59,.022);
            for(let j=0;j<5;j++)forms.pouch(x-.46+(j%3)*.46,.445+Math.floor(j/3)*.90,.25,j+i);
            plaque(x,2.2,.65,["EARLY","MID-MISSION","LATE"][i],1.3);
        }break;
    case "veggie-salad": {
        const leafGeometry=article.leafShape();
        box(0,.24,0,3.3,.35,1.9,C.white);
        for(const x of [-1,0,1])for(const z of [-.43,.43]){
            forms.soft(x,.53,z,.80,.70,.23,C.fabric,1).rotation.x=-Math.PI/2;
            cylinder(x,.65,z,.08,.18,C.silver);
            article.leafyCanopy({mesh},x,z,leafGeometry);
        }
        for(const x of [-1.65,1.65])for(const z of [-.9,.9])box(x,1.15,z,.055,1.9,.055,C.silver);
        for(let i=0;i<10;i++)box(0,.5+i*.14,-.93,3.3,.04,.07,C.white);
        for(const z of [-.91,.91])box(0,2.13,z,3.5,.20,.18,C.white);
        for(const x of [-1.64,1.64])box(x,2.13,0,.18,.20,1.72,C.white);
        for(const x of [-1.2,-.6,0,.6,1.2])box(x,2.08,0,.19,.12,1.68,C.silver);
        for(const x of [-1.2,-.6,0,.6,1.2])for(const z of [-.6,0,.6]){
            const led=cylinder(x,1.999,z,.06,.015,C.white);
            led.material=material(0xffa3ca);led.material.emissive.setHex(0xff68a1);led.material.emissiveIntensity=.8;
        }
        plaque(0,2.14,1.01,"VEGGIE",1);break;
    }
    case "separating-toilet-waste": {
        const casing=forms.vessel(-1.1,.64,0,.54,1.15,C.white,true);
        casing.rotation.y=.22;
        forms.vessel(-1.1,.56,0,.29,.78,C.fabric);
        tube([[-1.1,1.15,.02],[-1.03,.94,.02],[-.94,.54,.08],[-.76,.35,.18]],C.gold,.085);
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
        const chamber=forms.vessel(0,1.03,0,.53,1.51,C.silver,true);
        chamber.rotation.y=.22;
        const ram=cylinder(0,1.56,0,.084,.70,C.silver);
        const piston=cylinder(0,1.24,0,.37,.13,C.dark);
        for(let j=0;j<5;j++)ring(0,.62+j*.095,0,.40,C.gold,Math.PI/2);
        ram.userData.movable=piston.userData.movable=true;
        article.updates.push(t=>{ram.position.y=1.56-.42*t;piston.position.y=1.24-.42*t});
        for(let i=0;i<4;i++)tube([[.5,.4+i*.22,.2],[.92,.4+i*.22,.2],[.92,1.8,.1],[.2,2,.1]],i%2?C.silver:C.gold,.017);
        for(let i=0;i<7;i++)forms.pouch(-2+(i%3)*.3,.3+Math.floor(i/3)*.17,.1+(i%2)*.2,i);
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
                for(let row=0;row<4;row++)for(let col=0;col<12;col++)
                    box(x+dx+(col-5.5)*.049,.865,.15+(row-1.5)*.048,.039,.01,.036,C.silver);
            }
            chair(x,.65);crew(x,.65,x<0?C.red:C.blue,true);
            plaque(x,1.82,-.211,x<0?"HABITAT":"MISSION CONTROL",1.7);
        }
        break;
    default: throw new Error("Unknown handbook model");
    }
    if(article) {
        article.detailing(root,id,tools,C);
        article.batch(root);
    }
}

export function applyEnvironment(slot,scene,article,id){
    const outdoor=article.outdoor(id);
    if(!slot.environments.has(outdoor)){
        const start=performance.now();
        slot.environments.set(outdoor,article.reflections(slot.renderer,scene,outdoor));
        markPhase("pmrem",start,{outdoor});
    }
    scene.environment=slot.environments.get(outdoor).texture;
    scene.environmentIntensity=outdoor?.83:.54;
}

export async function prepareScene(slot,id,article,wide,signal){
    const scene = new T.Scene(), root = new T.Group();scene.add(root);
    const started=performance.now();
    try{
        if(article)article.asset=await article.loadArticleAsset(id,signal);
        signal.throwIfAborted();
        const buildStart=performance.now();
        await build(root,id,article,signal);
        markPhase("scene.build",buildStart,{id});
        await yieldTask(signal);
        const bounds = new T.Box3();
        for (const object of root.children)
            if (!object.userData.mapGround) bounds.union(new T.Box3().setFromObject(object));
        const center = bounds.getCenter(new T.Vector3());
        const size = bounds.getSize(new T.Vector3());
        if(article)article.surroundings(scene,id,bounds);
        if(article)applyEnvironment(slot,scene,article,id);
        const overview=id==="outpost";
        if (overview) {
            scene.background=new T.Color(0xcda58e);
            scene.fog=new T.Fog(0xcda58e,29,80);
        }
        scene.add(new T.HemisphereLight(0xdaeafa,0x7b6d5f,overview ? 1.38 : article ? 1.53 : 2));
        const key = new T.DirectionalLight(article && !article.outdoor(id)?0xfff0da:0xffe1b9,article?2.6:3.2);if(id==="outpost" || article?.outdoor(id)) key.position.set(-9,10,5); else key.position.set(-4,8,6);key.castShadow=true;
        key.shadow.mapSize.setScalar(overview && wide ? 2048 : 1024);key.shadow.camera.left=-8;key.shadow.camera.right=8;
        key.shadow.camera.top=8;key.shadow.camera.bottom=-8;key.shadow.bias=-.001;scene.add(key);
        const fill=new T.DirectionalLight(0xa9d5ff,overview ? .48 : article ? .65 : 1.2);fill.position.set(5,3,-5);scene.add(fill);
        markPhase("scene.prepare",started,{id});
        return {scene,root,bounds,center,size,key,article,dispose(){
            disposeResources(resources(scene),article?.asset?.shared);
            key.shadow.dispose();article?.asset?.release();
        }};
    }catch(error){
        disposeResources(resources(scene),article?.asset?.shared);
        article?.asset?.release();throw error;
    }
}

const pending=new WeakMap();
export async function mount(host,id,title,callback,design=null){
    unmount(host);
    const controller=new AbortController(), signal=controller.signal;
    pending.set(host,controller);
    let slot,entry,attached=false;
    try{
        await afterPaint(signal);
        slot=acquireRenderer();
        entry=slot.scenes.get(id);
        if(!entry){
            entry=await prepareScene(slot,id,design,host.clientWidth>800,signal);
            signal.throwIfAborted();
            await yieldTask(signal);
        }
        const cached=slot.scenes.has(id);
        await attach(host,id,title,callback,slot,entry,signal,cached);
        attached=true;
        storeScene(slot,id,entry);
        return references[id];
    }catch(error){
        if(slot && !attached){
            if(entry && !slot.scenes.has(id))entry.dispose();
            slot.renderer.domElement.remove();releaseRenderer(slot);
        }
        if(signal.aborted)return null;
        throw error;
    }finally{if(pending.get(host)===controller)pending.delete(host)}
}

export function configureRenderer(renderer,entry,overview,dpr,width,cached){
    const {key}=entry;
    const ratio=Math.min(dpr,overview?1.5:2);
    if(renderer.getPixelRatio()!==ratio)renderer.setPixelRatio(ratio);
    renderer.setClearColor(0x111a22);
    renderer.shadowMap.enabled=true;renderer.shadowMap.autoUpdate=false;
    renderer.shadowMap.needsUpdate=!cached || !!entry.shadowDirty;
    entry.shadowDirty=false;
    renderer.shadowMap.type=T.PCFSoftShadowMap;
    renderer.toneMapping=T.ACESFilmicToneMapping;
    renderer.toneMappingExposure=overview?1.07:1.04;
    const shadowSize=overview && width>800?2048:1024;
    if(key.shadow.mapSize.x!==shadowSize){
        key.shadow.mapSize.setScalar(shadowSize);
        key.shadow.dispose();key.shadow.map=null;renderer.shadowMap.needsUpdate=true;
    }
}

export async function prepareGPU(renderer,scene,camera,signal,id){
    const start=performance.now();
    await renderer.compileAsync(scene,camera);
    markPhase("shader.prepare",start,{id});
    signal.throwIfAborted();
    // Slicing uploads prevents many decoded textures from monopolizing the first draw.
    let slice=performance.now();
    for(const item of resources(scene))if(item.isTexture && !item.isRenderTargetTexture){
        renderer.initTexture(item);
        if(performance.now()-slice>8){await yieldTask(signal);slice=performance.now()}
    }
}

async function attach(host,id,title,callback,slot,entry,signal,cached){
    const {scene,root,bounds,center,size,key,article}=entry;
    const renderer=slot.renderer, overview=id==="outpost", target=center.clone();
    if(article && (scene.environment!==slot.environments.get(article.outdoor(id))?.texture))applyEnvironment(slot,scene,article,id);
    configureRenderer(renderer,entry,overview,devicePixelRatio,host.clientWidth,cached);
    const canvas=renderer.domElement;
    const camera = new T.OrthographicCamera(-4,4,3,-3,.1,overview ? 160 : 80);
    if(!cached)await prepareGPU(renderer,scene,camera,signal,id);
    signal.throwIfAborted();
    host.replaceChildren(canvas);
    canvas.tabIndex=0;canvas.setAttribute("role","img");canvas.setAttribute("aria-label",title);
    const initialPitch = id === "outpost" ? .76 : .52;
    const initialZoom = id === "outpost" ? .97 : article ? .78 : 1;
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
    const previousSize=renderer.getSize(new T.Vector2());
    let renderedWidth=previousSize.x, renderedHeight=previousSize.y;
    // Coalesce intermediate entrance/pointer poses while the GPU is still drawing the last one.
    const gl=renderer.getContext();
    let fence=null,queuedDraw=null;
    function draw(){
        if(overview && fence){
            if(gl.clientWaitSync(fence,0,0)===gl.TIMEOUT_EXPIRED){
                if(queuedDraw===null)queuedDraw=requestAnimationFrame(()=>{queuedDraw=null;draw()});
                return;
            }
            gl.deleteSync(fence);fence=null;
            if(queuedDraw!==null){cancelAnimationFrame(queuedDraw);queuedDraw=null}
        }
        const w=host.clientWidth,h=host.clientHeight;if(!w||!h)return;
        if(w !== renderedWidth || h !== renderedHeight){
            renderer.setSize(w,h,false);
            renderedWidth=w;renderedHeight=h;
        }
        // Fit the bounding sphere for every angle, including portrait viewports.
        // At grazing angles the near plane must clear the foreground escarpments.
        // Moving an orthographic camera along its sightline preserves composition.
        const distance = overview ? Math.max(25,13/Math.sin(pitch)) : 25;
        if (overview) {
            scene.fog.near=distance+4;
            scene.fog.far=distance+55;
        }
        camera.position.set(target.x+distance*Math.sin(yaw)*Math.cos(pitch),
            target.y+distance*Math.sin(pitch),target.z+distance*Math.cos(yaw)*Math.cos(pitch));
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
        if(overview){fence=gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE,0);gl.flush()}
        positionLabels(w,h);
    }
    const tilt=v=>Math.max(.16,Math.min(1.25,v));
    let start=null, selectedTopic=null, disposed=false, dragged=false;
    const explorer = host.parentElement, view = explorer.parentElement;
    canvas.style.touchAction=overview?"none":"";
    function trapFocus(e){
        if(!expanded || e.key !== "Tab") return;
        const nodes = [...view.querySelectorAll("button:not([tabindex='-1']),a,input,canvas[tabindex]")].filter(n=>n.getClientRects().length);
        if(e.shiftKey && document.activeElement === nodes[0]){e.preventDefault();nodes.at(-1)?.focus()}
        else if(!e.shiftKey && document.activeElement === nodes.at(-1)){e.preventDefault();nodes[0]?.focus()}
    }
    view.addEventListener("keydown",trapFocus);
    const groups = new Map(), labels = [], originals = new Map(), highlights = new Map();
    let hovered = null, showRoutes = false;
    const ray = new T.Raycaster(), pointer = new T.Vector2();
    const projected = new T.Vector3();
    for (const part of root.children) {
        const slug = part.userData.topic;
        if (!slug) continue;
        if (!groups.has(slug)) groups.set(slug,{ bounds: new T.Box3(), parts: [] });
        const group = groups.get(slug);
        if (part.userData.label) {
            const label = document.createElement("button");
            label.className = "facility-label";
            label.textContent = part.userData.label;
            label.type = "button";
            label.tabIndex = -1; // The system index exposes the same actions in keyboard order.
            label.onclick = () => requestTopic(slug);
            host.append(label);
            labels.push({ slug, label, anchor: part.position });
        } else {
            group.bounds.expandByObject(part);
            group.parts.push(part);
            if (part.material?.emissive) originals.set(part,part.material);
        }
    }
    function positionLabels(w,h) {
        const occupied = [];
        const ordered = [...labels].sort((a,b) => Number(b.slug===selectedTopic)-Number(a.slug===selectedTopic));
        for (const { slug, label, anchor } of ordered) {
            projected.copy(anchor).project(camera);
            const x = (projected.x+1)*w/2, y = (1-projected.y)*h/2;
            const width = label.offsetWidth, height = label.offsetHeight;
            const rect = { l:x-width/2, r:x+width/2, t:y-height, b:y };
            const visible = rect.l>2 && rect.r<w-2 && rect.t>2 && rect.b<h-2 &&
                !occupied.some(other => rect.l<other.r+5 && rect.r>other.l-5 && rect.t<other.b+7 && rect.b>other.t-7);
            label.style.visibility = visible ? "visible" : "hidden";
            if (!visible) continue;
            occupied.push(rect);
            label.style.left = x+"px"; label.style.top = y+"px";
            label.classList.toggle("is-active",slug===selectedTopic);
            label.classList.toggle("is-hovered",slug===hovered);
        }
    }
    function requestTopic(slug) {
        if (!disposed) callback.invokeMethodAsync("SelectTopic",slug).catch(()=>{});
    }
    function updateMaterials() {
        for (const [part,original] of originals) {
            const slug = part.userData.topic;
            if (slug!==selectedTopic && slug!==hovered) { part.material=original; continue; }
            const selected = slug===selectedTopic;
            const route = selected && showRoutes && part.userData.route;
            const key = original.uuid + (route ? "route" : selected);
            if (!highlights.has(key)) {
                const bright = original.clone();
                bright.emissive.set(route ? 0xffc468 : selected ? 0xda8b42 : 0xe2ba80);
                bright.emissiveIntensity = route ? 1.15 : selected ? .28 : .14;
                highlights.set(key,bright);
            }
            part.material=highlights.get(key);
        }
    }
    function selectTopic(slug) {
        if (slug && !groups.has(slug)) return;
        selectedTopic=slug; showRoutes=false; hovered=null; updateMaterials();
        if (id!=="outpost") return;
        stopEntry();
        const finish = slug ? groups.get(slug).bounds.getCenter(new T.Vector3()) : center;
        const from = target.clone(), fromZoom = zoom, toZoom = slug ? .58 : initialZoom;
        const begin = performance.now(), reduced = matchMedia("(prefers-reduced-motion: reduce)").matches;
        function frame(now) {
            const t = reduced ? 1 : Math.min(1,(now-begin)/360);
            const ease = 1-(1-t)**3;
            target.lerpVectors(from,finish,ease);
            zoom=T.MathUtils.lerp(fromZoom,toZoom,ease);
            draw();
            entryFrame=t<1 ? requestAnimationFrame(frame) : null;
        }
        frame(begin);
    }
    function pick(event) {
        const rect=canvas.getBoundingClientRect();
        pointer.set(2*(event.clientX-rect.left)/rect.width-1,1-2*(event.clientY-rect.top)/rect.height);
        ray.setFromCamera(pointer,camera);
        const candidates=[];
        for(const group of groups.values())
            if(ray.ray.intersectsBox(group.bounds)) candidates.push(...group.parts);
        return ray.intersectObjects(candidates,false)[0]?.object.userData.topic ?? null;
    }
    function hover(slug) {
        if (hovered===slug) return;
        hovered=slug; updateMaterials(); draw();
    }
    canvas.onpointerleave=()=>hover(null);
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
            if(id === "outpost" && !e.shiftKey){
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
            hover(slug);
            canvas.style.cursor = slug ? "pointer" : "grab";

        }
    };
    canvas.onpointercancel=e=>{start=null;pinchDistance=null;pointers.delete(e.pointerId)};
    canvas.onpointerup=e=>{
        const tap=!dragged&&start&&Math.hypot(e.clientX-start[0],e.clientY-start[1])<6;start=null;pinchDistance=null;pointers.delete(e.pointerId);
        if(!tap||id!=="outpost")return;
        const slug = pick(e);
        if(slug) {
            requestTopic(slug);
        }
    };
    canvas.onwheel=e=>{
        if(id !== "outpost" || !expanded)return;
        e.preventDefault();stopEntry();zoom=T.MathUtils.clamp(zoom*Math.exp(e.deltaY*.001),.45,1.6);draw();
    };
    canvas.onkeydown=e=>{

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
    let illustrationFrame=null;
    const observer=new ResizeObserver(()=>{
        if(host.clientWidth!==renderedWidth || host.clientHeight!==renderedHeight)draw();
    });
    slot.onRestore=()=>{
        fence=null;
        for(const state of slot.scenes.values()){
            if(state.article)applyEnvironment(slot,state.scene,state.article,state.article.id);
        }
        renderer.shadowMap.needsUpdate=true;draw();
    };
    observer.observe(host);
    const first=performance.now();
    draw();
    markPhase("first.submit",first,{id,cached});


    views.set(host,{
        selectTopic,
        illustrate(value){
            if(!article)return;
            cancelAnimationFrame(illustrationFrame);
            const begin=performance.now(),from=article.progress,to=value?1:0;
            const reduce=matchMedia("(prefers-reduced-motion: reduce)").matches;
            function frame(now){
                const t=reduce?1:Math.min(1,(now-begin)/560);
                article.progress=T.MathUtils.lerp(from,to,t*t*(3-2*t));
                for(const update of article.updates)update(article.progress);
                renderer.shadowMap.needsUpdate=true;draw();
                illustrationFrame=t<1?requestAnimationFrame(frame):null;
            }
            frame(begin);
        },
        routes(value){showRoutes=value;updateMaterials();draw()},
        orbit(delta){stopEntry(); yaw+=delta; draw()},
        setExpanded(value){
            expanded=value;
            if(value) enterMap(); else { stopEntry(); draw(); }
            if(value) view.querySelector(".map-exit")?.focus({preventScroll:true});
        },
        reset(){stopEntry();yaw=.42;pitch=initialPitch;zoom=initialZoom;target.copy(center);draw()},
        zoom(delta){stopEntry();zoom=Math.max(.45,Math.min(1.6,zoom+delta));draw()},
        dispose(){
            disposed = true;
            if(queuedDraw!==null)cancelAnimationFrame(queuedDraw);
            if(fence)gl.deleteSync(fence);
            cancelAnimationFrame(illustrationFrame);
            stopEntry();
            view.removeEventListener("keydown",trapFocus);
            observer.disconnect();
            for (const [object, original] of originals) object.material = original;
            highlights.forEach(material => material.dispose());
            labels.forEach(({label})=>label.remove());
            if(article?.progress){
                for(const update of article.updates)update(0);
                article.progress=0;entry.shadowDirty=true;
            }
            slot.onRestore=null;
            canvas.onpointerleave=canvas.onpointerdown=canvas.onpointermove=canvas.onpointercancel=null;
            canvas.onpointerup=canvas.onwheel=canvas.onkeydown=null;
            canvas.style.cursor="";canvas.remove();
            releaseRenderer(slot);
        }
    });
    return references[id];
}
export function setExpanded(host,value){views.get(host)?.setExpanded(value)}
export function setTopic(host,slug){views.get(host)?.selectTopic(slug)}
export function setRoutes(host,value){views.get(host)?.routes(value)}
export function orbit(host,delta){views.get(host)?.orbit(delta)}
export function reset(host){views.get(host)?.reset()}
export function zoom(host,delta){views.get(host)?.zoom(delta)}
export function unmount(host){pending.get(host)?.abort();views.get(host)?.dispose();views.delete(host)}

export function illustrate(host,value){views.get(host)?.illustrate(value)}
