import * as T from "../lib/three/three.module.min.js";
import {mount as mountViewer} from "./livingModels.js";
import {flightCrew} from "./articleCrew.js";
import {makeForms,leafShape} from "./articleForms.js";
import {loadArticleAsset,placeRover,placeLanders} from "./articleAssets.js";
import {unmount as releaseViewer} from "./livingModels.js";
export {zoom,reset,orbit,illustrate} from "./livingModels.js";

const surfaceScenes=new Set(["dust-and-solar-budget","oxygen-from-martian-air","perseverance-heat-and-power"]);
const alloy=0x8e9b9d, graphite=0x344143, textile=0xc5b298, copper=0xbb7751, lamp=0xffe7ba;
// A shallow folded blade, modelled from stem to tip. Reuse the same topology
// for the six plant pillows, with a different azimuth and pitch for each leaf.
function leafyCanopy(parts,x,z,geometry) {
    for(let i=0;i<15;i++){
        const angle=i*2.39996+x*.61+z*.88;
        const blade=parts.mesh(geometry,i%3===0?0x839e46:i%3===1?0x537733:0x729140,x,.685,z);
        blade.material.side=T.DoubleSide;
        blade.rotation.order="YXZ";blade.rotation.y=angle;
        blade.rotation.x=-(.17+.38*(i%3));
        const length=.80+.21*Math.sin(i*2.17+x-z);
        blade.scale.set(length*(.67+.35*Math.cos(angle)**2),length*(.86+.19*Math.sin(angle)),length);
    }
}
// Tiny, neutral relief maps retain the original colours. Create each finish
// only when a material in the current article actually requests it.
function finishes(C) {
    const textures=new Map();
    function relief(kind) {
        if(textures.has(kind))return textures.get(kind);
        const n=64,data=new Uint8Array(n*n);
        for(let y=0;y<n;y++)for(let x=0;x<n;x++){
            const thread=((x>>2)+(y>>2))%2===0;
            data[y*n+x]=kind==="woven" ? 122+48*Math.cos((thread?x:y)*Math.PI/2)
                : 145+11*Math.sin(y*Math.PI/2)+5*Math.cos((x+y)*Math.PI/8);
        }
        const map=new T.DataTexture(data,n,n,T.RedFormat);map.wrapS=map.wrapT=T.RepeatWrapping;
        map.repeat.set(4,4);map.magFilter=T.LinearFilter;map.minFilter=T.LinearMipmapLinearFilter;
        map.generateMipmaps=true;map.needsUpdate=true;textures.set(kind,map);return map;
    }
    function roughness(kind){
        if(textures.has(kind))return textures.get(kind);
        const n=96,data=new Uint8Array(n*n*4);
        for(let y=0;y<n;y++)for(let x=0;x<n;x++){
            const value=kind==="foil" ? 162+25*Math.sin(18*x/n+4*Math.cos(12*y/n))+11*Math.sin(.56*y+.13*x)
                : 183+19*Math.sin(Math.PI*y*.5)+8*Math.cos(Math.PI*x/8);
            data.set([value,value,value,255],4*(y*n+x));
        }
        const map=new T.DataTexture(data,n,n);map.wrapS=map.wrapT=T.RepeatWrapping;
        map.generateMipmaps=true;map.minFilter=T.LinearMipmapLinearFilter;map.magFilter=T.LinearFilter;
        map.needsUpdate=true;textures.set(kind,map);return map;
    }
    return (surface,color)=>{
        if([0x839e46,0x537733,0x729140].includes(color)){
            if(!textures.has("leaf")){
                const c=document.createElement("canvas");c.width=128;c.height=192;const ctx=c.getContext("2d");
                const base=ctx.createLinearGradient(0,0,128,0);
                base.addColorStop(0,"#b8c887");base.addColorStop(.5,"#e5ecad");base.addColorStop(1,"#adc582");
                ctx.fillStyle=base;ctx.fillRect(0,0,128,192);
                ctx.strokeStyle="#eff0bb";ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(64,0);ctx.lineTo(64,192);ctx.stroke();
                for(let y=8;y<198;y+=18)for(const edge of [0,128]){
                    ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(64,y);ctx.quadraticCurveTo((64+edge)/2,y-6,edge,y-27);ctx.stroke();
                }
                const map=new T.CanvasTexture(c);map.colorSpace=T.SRGBColorSpace;textures.set("leaf",map);
            }
            surface.map=textures.get("leaf");surface.roughness=.65;
        }else if([C.fabric,0xd4cebd,0x536e78,0xe2ded0,0xaf9c7e,C.red].includes(color)){
            surface.bumpMap=relief("woven");surface.bumpScale=.004;
        }else if([0x91785a,0x959b97,0xab7950].includes(color)){
            surface.metalness=.44;surface.roughness=.85;surface.roughnessMap=roughness("foil");
        }else if(color===C.silver || color===alloy){
            surface.bumpMap=relief("brushed");surface.bumpScale=.002;
            surface.roughnessMap=roughness("alloy");surface.roughness=.66;
        }
    };
}
function detailing(root,id,parts,C) {
    const {box,cylinder,rod,sphere,ring,plaque}=parts;
    const line=(a,b,r=.034,color=alloy)=>rod(a,b,r,color);
    function bulkhead(x,z,width,height) {
        box(x,height/2,z,width,height,.10,0x9a9d94);
        const panels=Math.ceil(width/.96);
        for(let i=0;i<panels;i++){
            const px=x-width/2+(i+.5)*width/panels;
            box(px,height*.53,z+.064,width/panels-.045,height*.82,.055,0xc0c1b4);
            box(px,height*.85,z+.103,width/panels*.66,.042,.034,lamp);
        }
        for(const h of [.16,height*.94])box(x,h,z+.105,width,.07,.09,graphite);
    }
    function gauge(x,y,z) {
        cylinder(x,y,z,.097,.055,graphite).rotation.x=Math.PI/2;
        cylinder(x,y,z+.032,.078,.017,0xf0e7ca).rotation.x=Math.PI/2;
        line([x-.026,y-.018,z+.044],[x+.035,y+.042,z+.044],.008,copper);
    }
    function tray(x,z,w,d) {
        box(x,.06,z,w,.12,d,alloy);
        for(const sign of [-1,1])box(x+sign*(w/2-.025),.15,z,.05,.18,d,graphite);
    }
    function rackFeet(x){
        for(const dx of [-.68,.68]) for(const dz of [-.45,.45]) {
            box(x+dx,.035,dz,.22,.075,.19,graphite);
        }
    }
    function berthServices(n) {
        for(let i=0;i<n;i++){
            const x=-2.45+i*1.55;
            box(x,1.41,-1.67,.72,.09,.16,graphite);
            box(x,1.355,-1.65,.63,.022,.09,lamp);
            box(x,.72,-.22,.83,.07,.14,textile);
            for(let j=0;j<4;j++)box(x-.23+j*.15,1.53,-1.69,.084,.045,.05,alloy);
            rod([x-.55,1.52,-.15],[x+.55,1.52,-.15],.024,alloy);

        }
    }
    if(surfaceScenes.has(id)){
        if(id==="oxygen-from-martian-air"){
            box(-2.45,.18,1,1.58,.16,1.20,graphite);
            plaque(-2.45,.18,1.62,"SECTION / INSTALLED INSIDE ROVER",1.58);
            for(const dx of [-.45,.45])rod([-2.45+dx,.24,1],[-2.45+dx,.60,1],.052,alloy);
        }
    } else switch(id){
        case "rooms-for-four":
            berthServices(4);
            for(let i=0;i<4;i++){
                const x=-2.45+i*1.55;
                box(x,.38,-.11,.96,.22,.05,0x909a93);
                line([x-.15,.41,-.07],[x+.15,.41,-.07],.017,graphite);
            }
            for(const x of [-1.6,1.42]){
                box(x,1.51,-1.685,.54,.18,.08,graphite);
                for(let j=0;j<5;j++)box(x,1.45+j*.027,-1.635,.47,.01,.024,alloy);
            }
            box(1.25,.14,.96,.68,.065,.95,graphite);
            for(const side of [-1,1])line([1.25+side*.29,.15,.69],[1.25+side*.29,.90,.59],.035,alloy);
            line([.96,.90,.59],[1.54,.90,.59],.036,alloy);
            break;
        case "time-off-in-isolation":
            berthServices(2);
            const page=box(-2.20,.91,.56,.29,.025,.18,0xd9d4b5);page.rotation.x=.34;
            for(const x of [1.1,2.30])box(x,1.14,-1.72,.82,.56,.07,0x51616a);
            plaque(1.70,1.18,-1.662,"CREW COMMON ROOM",1.27);
            for(const x of [.91,2.48])for(const z of [.18,.78]) box(x,.43,z,.085,.69,.085,alloy);
            for(const x of [1.38,2.02]){cylinder(x,.89,.42,.064,.095,graphite);cylinder(x,.944,.42,.047,.012,0x86614c);}
            box(-2.2,.83,.30,.43,.045,.28,graphite);
            line([-2.20,.11,.30],[-2.20,.80,.30],.049,alloy);
            break;
        case "waiting-for-a-reply":
            for(const x of [-1.6,1.6]){
                bulkhead(x,-.68,2.42,1.99);
                if(x>0){
                    box(x,.88,-.55,2.28,1.62,.085,0x475966);
                    for(let j=0;j<15;j++)box(x-.99+j*.14,1.04,-.493,.065,1.19,.045,0x596b74);
                }
                for(const dx of [-.45,.45]){
                    box(x+dx,1.02,-.27,.07,.44,.09,alloy);
                    box(x+dx,.831,-.24,.32,.031,.20,graphite);
                }
                box(x,.38,-.33,.57,.70,.48,graphite);
                for(let i=0;i<5;i++)box(x,.20+i*.085,-.076,.40,.023,.034,alloy);
                box(x,1.82,-.27,1.76,.23,.10,graphite);
                line([x,.75,-.32],[x,1.82,-.32],.033,alloy);
            }break;
        case "staging-water-loop":
            // A pump body sits on its skid between treatment and storage.
            tray(.91,.45,.46,.42);
            cylinder(.91,.29,.45,.126,.36,graphite).rotation.z=Math.PI/2;
            line([.91,.30,.45],[.91,.36,.66],.065,C.water);
            for(const x of [-1.8,0,1.8]){
                rackFeet(x);gauge(x+.38,1.75,.47);
                for(const dx of [-.4,0,.4]){
                    box(x+dx,.32,0,.23,.47,.22,graphite);
                    line([x+dx,.38,0],[x+dx,.36,.55],.039,C.water);
                }
            }
            for(const x of [-.9,.9])box(x,.36,.65,.16,.15,.16,alloy);
            break;
        case "water-to-breathing-oxygen":
            rackFeet(-1.3);rackFeet(1.2);
            box(-1.3,.36,.05,1.04,.50,.76,graphite);
            for(const [x,h] of [[1,.54],[1.5,.64]])box(x,h/2,0,.25,h,.28,graphite);
            box(-2.25,.28,.1,.42,.51,.42,alloy);
            line([-1.30,.5,.60],[-1.30,.64,.31],.052,C.water);
            gauge(1.20,2.01,.47);break;
        case "packing-the-last-meal":
            for(const x of [-1.7,0,1.7]){
                bulkhead(x,-.64,1.49,2.29);
                for(const y of [.24,1.14])box(x,y,.54,1.36,.13,.045,graphite);
                box(x,2.19,.62,1.42,.24,.05,graphite);
                for(const dx of [-.58,.58])box(x+dx,2.07,.60,.043,.27,.045,alloy);
            }break;
        case "veggie-salad": {
            const film=new T.MeshStandardMaterial({color:0xb3d3d1,roughness:.32,transparent:true,opacity:.19,depthWrite:false});
            for(const x of [-1,0,1]){
                box(x,.39,0,.77,.065,1.51,graphite);
                for(const z of [-.43,.43])ring(x,.51,z,.24,textile,Math.PI/2);
            }
            box(1.41,1.16,-.94,.40,.81,.22,graphite);
            line([1.41,1.53,-.94],[1.41,2.08,-.94],.041,copper);
            for(const side of [-1,1]){
                const skin=box(side*1.64,1.22,0,.025,1.54,1.78,0xa6caca);
                skin.material=film;skin.castShadow=false;
            }break;
        }
        case "separating-toilet-waste":
            bulkhead(.10,-1.04,4.78,1.89);
            for(const x of [-1.49,-.75])line([x,1.21,-.97],[x,1.21,-.51],.035,alloy);
            box(2,.55,1,.77,.63,.045,graphite);
            for(const dx of [-.29,.29])box(2+dx,.57,1.04,.042,.30,.08,alloy);
            line([2,.96,.66],[2,1.65,.66],.019,alloy);
            line([1,.83,-.38],[1,1.65,-.38],.019,alloy);
            tray(1,-.4,.83,.87);tray(2,.50,1.08,1.12);
            box(1,.19,-.4,.40,.24,.40,graphite);
            line([-1.64,.60,.31],[-1.55,.60,.31],.085,alloy);
            for(const x of [-1.56,-.64])line([x,.36,.53],[x,.98,.53],.04,alloy);
            gauge(1,.91,-.065);break;
        case "trash-storage-tiles":
            rackFeet(0);tray(-1.77,.18,1.68,.94);tray(2,0,1.24,1.29);
            for(const x of [-.42,.42]) {
                box(x,1.14,.57,.1,.23,.13,graphite);
                cylinder(x,1.14,.65,.046,.16,copper);
            }
            box(0,.25,.49,1.12,.10,.65,graphite);
            break;
    }
}
/** Build only the environment of the currently mounted article. Bounds are those
 * of the subject, so a surrounding landscape cannot push it away from the reader. */
function surroundings(scene,id,bounds){
    const size=bounds.getSize(new T.Vector3()), center=bounds.getCenter(new T.Vector3());
    const outdoor=surfaceScenes.has(id);
    const w=outdoor ? 48 : size.x+.58, d=outdoor ? 48 : size.z+.58;
    const baseY=id==="dust-and-solar-budget" ? .056 : outdoor ? -.002 :
        (id==="rooms-for-four" || id==="time-off-in-isolation" ? -.061 : Math.min(.01,bounds.min.y));
    const canvas=document.createElement("canvas");canvas.width=canvas.height=256;
    const ctx=canvas.getContext("2d");
    let seed=71;const rand=()=>((seed=Math.imul(seed,1664525)+1013904223>>>0)/4294967296);
    ctx.fillStyle=outdoor?"#b09686":"#728181";ctx.fillRect(0,0,256,256);
    for(let i=0;i<15400;i++){
        const gray=rand()>.48?"255,244,216":"22,22,20";
        ctx.fillStyle="rgba("+gray+","+(outdoor ? .17 : .095)*rand()+")";
        ctx.fillRect(rand()*256,rand()*256,1+rand()*2,1+rand()*1.7);
    }
    if(!outdoor){
        ctx.strokeStyle="#49595a";ctx.lineWidth=3;ctx.strokeRect(2,2,252,252);
        for(const x of [10,246])for(const y of [10,246]){
            ctx.fillStyle="#3d4748";ctx.fillRect(x-2,y-2,4,4);
        }
    }
    const map=new T.CanvasTexture(canvas);map.wrapS=map.wrapT=T.RepeatWrapping;
    map.repeat.set(w/(outdoor?1.76:1.18),d/(outdoor?1.76:1.18));map.colorSpace=T.SRGBColorSpace;
    const geo=new T.PlaneGeometry(w,d,outdoor?94:1,outdoor?94:1);geo.rotateX(-Math.PI/2);
    const pos=geo.attributes.position, colors=[], color=new T.Color();
    if(outdoor){
        for(let i=0;i<pos.count;i++){
            const x=pos.getX(i),z=pos.getZ(i);
            const edge=Math.max(Math.abs(x)-size.x*.5-.25,Math.abs(z)-size.z*.5-.25,0);
            const wave=Math.sin(x*.51+Math.sin(z*.42))+Math.cos(z*.73+x*.19);
            pos.setY(i,Math.min(edge,2)*(.13*wave+.045*Math.sin(2*x-z)));
            color.setRGB(.74+.07*wave,.48+.052*wave,.36+.04*wave);
            colors.push(color.r,color.g,color.b);
        }
        geo.setAttribute("color",new T.Float32BufferAttribute(colors,3));geo.computeVertexNormals();
    }
    const floor=new T.Mesh(geo,new T.MeshStandardMaterial({map,roughness:outdoor ? .98 : .72,
        metalness:outdoor?0:.20,vertexColors:outdoor}));
    floor.position.set(center.x,baseY,center.z);floor.receiveShadow=true;scene.add(floor);
    if(outdoor){
        const stone=new T.InstancedMesh(new T.IcosahedronGeometry(1,1),new T.MeshStandardMaterial({color:0x88614e,roughness:.97}),130);
        const rockPositions=stone.geometry.attributes.position;
        for(let i=0;i<rockPositions.count;i++){
            const x=rockPositions.getX(i),y=rockPositions.getY(i),z=rockPositions.getZ(i);
            const r=.87+.14*Math.sin(4*x+2*z)+.10*Math.cos(5*z-3*y);
            rockPositions.setXYZ(i,r*x+.12*y,r*y,r*z);
        }
        stone.geometry.computeVertexNormals();
        const pose=new T.Object3D();
        for(let i=0;i<stone.count;i++){
            let x,z;do{x=(rand()-.5)*17;z=(rand()-.5)*12}while(Math.abs(x)<size.x*.5+.1&&Math.abs(z)<size.z*.5+.1);
            const edge=Math.max(Math.abs(x)-size.x*.5-.25,Math.abs(z)-size.z*.5-.25,0);
            const y=Math.min(edge,2)*(.13*(Math.sin(x*.51+Math.sin(z*.42))+Math.cos(z*.73+x*.19))+.045*Math.sin(2*x-z));
            const r=.028+rand()**3*.17;pose.position.set(center.x+x,baseY+y,center.z+z);
            pose.rotation.set(rand(),rand()*5,rand());pose.scale.set(r*1.5,r*.65,r);
            pose.updateMatrix();stone.setMatrixAt(i,pose.matrix);
        }
        stone.castShadow=stone.receiveShadow=true;
        const distant=new T.InstancedMesh(new T.IcosahedronGeometry(1,0),stone.material,stone.count);
        distant.instanceMatrix.copy(stone.instanceMatrix);distant.receiveShadow=true;
        const lod=new T.LOD();lod.addLevel(stone,0);lod.addLevel(distant,27);scene.add(lod);
        scene.background=new T.Color(0xc29b83);scene.fog=new T.Fog(0xc29b83,29,49);
    }else{
        const plinth=new T.Mesh(new T.BoxGeometry(w,.14,d),new T.MeshStandardMaterial({color:0x536067,metalness:.38,roughness:.58}));
        plinth.position.set(center.x,baseY-.071,center.z);plinth.receiveShadow=true;scene.add(plinth);
        scene.background=new T.Color(0x222e36);
    }
}
function batch(root){
    const groups=new Map();
    for(const part of root.children){
        if(!part.isMesh || part.material.transparent || part.userData.movable)continue;
        const key=part.geometry.uuid+part.material.uuid;
        if(!groups.has(key))groups.set(key,[]);
        groups.get(key).push(part);
    }
    for(const parts of groups.values()){
        if(parts.length<2)continue;
        const first=parts[0],mesh=new T.InstancedMesh(first.geometry,first.material,parts.length);
        parts.forEach((part,i)=>{part.updateMatrix();mesh.setMatrixAt(i,part.matrix);root.remove(part)});
        mesh.castShadow=mesh.receiveShadow=true;mesh.computeBoundingSphere();root.add(mesh);
    }
}
// Low-resolution sky and softbox reflections supply light to imported metal/rough materials.
function reflections(renderer,scene,outdoor){
    const canvas=document.createElement("canvas");canvas.width=256;canvas.height=128;
    const ctx=canvas.getContext("2d"),gradient=ctx.createLinearGradient(0,0,0,128);
    gradient.addColorStop(0,outdoor?"#ddd4ca":"#aebdc2");
    gradient.addColorStop(.47,outdoor?"#eed4bb":"#b5c6c8");
    gradient.addColorStop(.51,outdoor?"#b48b71":"#566671");
    gradient.addColorStop(1,outdoor?"#86634b":"#475458");
    ctx.fillStyle=gradient;ctx.fillRect(0,0,256,128);
    if(!outdoor){ctx.fillStyle="#fff7e5";for(const x of [18,77,179])ctx.fillRect(x,17,34,12)}
    const tex=new T.CanvasTexture(canvas);tex.colorSpace=T.SRGBColorSpace;tex.mapping=T.EquirectangularReflectionMapping;
    const generator=new T.PMREMGenerator(renderer),target=generator.fromEquirectangular(tex);
    tex.dispose();generator.dispose();scene.environment=target.texture;
    scene.environmentIntensity=outdoor?.83:.54;return target;
}
const pending=new WeakMap();
export function unmount(host){pending.get(host)?.abort();pending.delete(host);releaseViewer(host)}
export async function mount(host,id,title,callback){
    unmount(host);
    const request=new AbortController();pending.set(host,request);
    try {
        const asset=await loadArticleAsset(id,request.signal);
        if(request.signal.aborted)return null;
        const design={asset,updates:[],progress:0,reflections,makeForms,placeRover,placeLanders,finishes,flightCrew,
            leafyCanopy,leafShape,detailing,batch,surroundings,outdoor:scene=>surfaceScenes.has(scene)};
        return mountViewer(host,id,title,callback,design);
    }catch(error){if(request.signal.aborted)return null;throw error}
    finally{if(pending.get(host)===request)pending.delete(host)}
}
