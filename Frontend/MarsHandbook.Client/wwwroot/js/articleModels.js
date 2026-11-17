import * as T from "../lib/three/three.module.min.js";
import {mount as mountViewer} from "./livingModels.js";
import {flightCrew} from "./articleCrew.js";
export {zoom,reset,orbit,unmount} from "./livingModels.js";

const surfaceScenes=new Set(["dust-and-solar-budget","oxygen-from-martian-air","perseverance-heat-and-power"]);
const alloy=0x8e9b9d, graphite=0x344143, textile=0xc5b298, copper=0xbb7751, lamp=0xffe7ba;
// A shallow folded blade, modelled from stem to tip. Reuse the same topology
// for the six plant pillows, with a different azimuth and pitch for each leaf.
function leafyCanopy(parts,x,z,geometry) {
    for(let i=0;i<9;i++){
        const angle=i*2.39996+x*.61+z*.88;
        const blade=parts.mesh(geometry,i%3===0?0x839e46:i%3===1?0x537733:0x729140,x,.685,z);
        blade.material.side=T.DoubleSide;
        blade.rotation.order="YXZ";blade.rotation.y=angle;
        blade.rotation.x=-(.17+.38*(i%3));
        const length=.80+.21*Math.sin(i*2.17+x-z);
        blade.scale.set(length,length,length);
    }
}
function leafShape() {
    const positions=[],indices=[];
    for(let i=0;i<=12;i++){
        const t=i/12,span=.167*Math.sin(Math.PI*t)**.76*(i%2 ? .91 : 1);
        for(const side of [-1,0,1]){
            const y=.14*Math.sin(Math.PI*t)+.12*t+Math.abs(side)*(.024*Math.sin(16*t)-.047)*Math.sin(Math.PI*t);
            positions.push(side*span+.036*t*t,y,.54*t);
        }
        if(i<12)for(let j=0;j<2;j++){
            const k=i*3+j;indices.push(k,k+3,k+1,k+1,k+3,k+4);
        }
    }
    const g=new T.BufferGeometry();g.setAttribute("position",new T.Float32BufferAttribute(positions,3));
    g.setIndex(indices);g.computeVertexNormals();return g;
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
    return (surface,color)=>{
        if([C.fabric,C.white,C.red,C.blue].includes(color)){
            surface.bumpMap=relief("woven");surface.bumpScale=.004;
        }else if(color===C.silver || color===alloy){
            surface.bumpMap=relief("brushed");surface.bumpScale=.002;
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
            box(x-.47,1.02,-.15,.14,.92,.045,textile);
        }
    }
    if(surfaceScenes.has(id)){
        if(id==="dust-and-solar-budget"){
            for(const x of [-2.5,2.5]){
                box(x,.79,.39,.56,.12,.12,graphite);
                for(const sign of [-1,1]){
                    line([x+sign*.36,.56,.28],[x+sign*1.24,.68,.04],.028,copper);
                    box(x+sign*.48,.65,.18,.15,.12,.19,alloy);
                }
                line([x,.86,.15],[x,1.93,.39],.022,alloy);
            }
        }else{
            for(const side of [-1,1]){
                for(const x of [-.74,0,.74]){
                    box(x,1.04,side*.746,.58,.37,.035,textile);
                    for(const dx of [-.22,.22]) box(x+dx,1.04,side*.774,.03,.32,.025,alloy);
                }
                line([-1.05,.43,side*1.05],[-.51,.81,side*.91],.057,alloy);
                line([-.51,.81,side*.91],[0,.43,side*1.05],.057,alloy);
            }
            // Mast optics, deck access covers, and the two arm joint housings.
            for(const [ax,ay,az] of [[1.50,.67,1.25],[1.95,.35,.75]])
                cylinder(ax,ay,az,.094,.17,alloy).rotation.x=Math.PI/2;
            for(const px of [-.30,.22,.76]){
                box(px,1.371,.39,.39,.033,.36,textile);
                for(const dx of [-.14,.14])cylinder(px+dx,1.397,.40,.022,.016,graphite);
            }
            for(let i=0;i<3;i++)box(.74,2.51+i*.055,-.424,.50,.016,.029,graphite);
            if(id==="oxygen-from-martian-air"){
                box(-2.45,.18,1,1.36,.12,1.04,graphite);
                for(const dx of [-.42,.42])for(const dz of [-.33,.33])
                    box(-2.45+dx,.27,1+dz,.10,.54,.10,alloy);
                for(let i=0;i<5;i++)box(-2.45,1.42-i*.10,1.42,.69,.034,.05,copper);
            }else{
                for(const side of [-1,1])line([-1.53,1.25,side*.31],[-.88,1.11,side*.60],.049,copper);
                box(-.89,1.32,0,.50,.20,.67,graphite);
                line([-1.7,1.43,0],[-1.7,2.05,0],.017,alloy);
            }
        }
    } else switch(id){
        case "rooms-for-four":
            berthServices(4);
            box(1.25,.14,.96,.68,.065,.95,graphite);
            for(const side of [-1,1])line([1.25+side*.29,.15,.69],[1.25+side*.29,.90,.59],.035,alloy);
            line([.96,.90,.59],[1.54,.90,.59],.036,alloy);
            break;
        case "time-off-in-isolation":
            berthServices(2);
            for(const x of [.91,2.48])for(const z of [.18,.78]) box(x,.43,z,.085,.69,.085,alloy);
            for(const x of [1.38,2.02]){cylinder(x,.89,.42,.064,.095,graphite);cylinder(x,.944,.42,.047,.012,0x86614c);}
            box(-2.2,.83,.30,.43,.045,.28,graphite);
            line([-2.20,.11,.30],[-2.20,.80,.30],.049,alloy);
            break;
        case "waiting-for-a-reply":
            for(const x of [-1.6,1.6]){
                bulkhead(x,-.68,2.42,1.99);
                for(const dx of [-.45,.45]){
                    box(x+dx,1.02,-.27,.07,.44,.09,alloy);
                    box(x+dx,.831,-.24,.32,.031,.20,graphite);
                }
                box(x,.38,-.33,.57,.70,.48,graphite);
                for(let i=0;i<5;i++)box(x,.20+i*.085,-.076,.40,.023,.034,alloy);
                box(x,1.82,-.27,1.76,.23,.10,graphite);
                line([x,.75,-.32],[x,1.82,-.32],.033,alloy);
            }break;
        case "expandable-room":
            for(const [x,r] of [[-1.9,.6],[.9,1]]){
                for(const side of [-1,1]){
                    const px=x+side*r*.65;
                    box(px,.08,0,.24,.16,r*1.42,graphite);
                    for(const dz of [-.36,.36])line([px,.10,dz*r],[px,1.1-r*.78,dz*r],.069,alloy);
                }
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
    for(let i=0;i<9400;i++){
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
        const stone=new T.InstancedMesh(new T.IcosahedronGeometry(1,0),new T.MeshStandardMaterial({color:0x88614e,roughness:.97}),130);
        const pose=new T.Object3D();
        for(let i=0;i<stone.count;i++){
            let x,z;do{x=(rand()-.5)*17;z=(rand()-.5)*12}while(Math.abs(x)<size.x*.5+.1&&Math.abs(z)<size.z*.5+.1);
            const edge=Math.max(Math.abs(x)-size.x*.5-.25,Math.abs(z)-size.z*.5-.25,0);
            const y=Math.min(edge,2)*(.13*(Math.sin(x*.51+Math.sin(z*.42))+Math.cos(z*.73+x*.19))+.045*Math.sin(2*x-z));
            const r=.028+rand()**3*.17;pose.position.set(center.x+x,baseY+y,center.z+z);
            pose.rotation.set(rand(),rand()*5,rand());pose.scale.set(r*1.5,r*.65,r);
            pose.updateMatrix();stone.setMatrixAt(i,pose.matrix);
        }
        stone.castShadow=stone.receiveShadow=true;scene.add(stone);
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
        if(!part.isMesh || part.material.transparent)continue;
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
const design={finishes,flightCrew,leafyCanopy,leafShape,detailing,batch,surroundings,outdoor:id=>surfaceScenes.has(id)};
export function mount(host,id,title,callback){return mountViewer(host,id,title,callback,design)}
