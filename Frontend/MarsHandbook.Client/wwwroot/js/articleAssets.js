import * as T from "../lib/three/three.module.min.js";

import {resources,disposeResources} from "./viewerResources.js";

import {markPhase} from "./viewerScheduling.js";

const files={
    "rooms-for-four":"workwear","time-off-in-isolation":"workwear","waiting-for-a-reply":"workwear",
    "perseverance-heat-and-power":"rover","oxygen-from-martian-air":"rover","dust-and-solar-budget":"insight"
};
const assets=new Map();

async function readAsset(url,signal){
    const [{GLTFLoader,MeshoptDecoder,cloneRig},response]=await Promise.all([
        import("../lib/three/gltf.min.js"),fetch(url,{signal})
    ]);
    if(!response.ok)throw Error("Model request failed ("+response.status+")");
    const bytes=await response.arrayBuffer();
    const start=performance.now();
    const data=await new GLTFLoader().setMeshoptDecoder(MeshoptDecoder).parseAsync(bytes,"");
    markPhase("glb.parse",start,{url});
    data.scene.traverse(o=>{if(o.isMesh)o.castShadow=o.receiveShadow=true});
    return {data,cloneRig,shared:resources(data.scene)};
}
function evict(url,entry){
    if(entry.users || assets.get(url)!==entry)return;
    clearTimeout(entry.timer);assets.delete(url);entry.controller.abort();
    entry.promise.then(asset=>disposeResources(asset.shared),()=>{});
}
export async function loadArticleAsset(id,signal){
    const file=files[id];if(!file)return null;
    signal.throwIfAborted();
    const url=new URL("../models/living/"+file+".glb",import.meta.url).href;
    let entry=assets.get(url);
    if(!entry){
        entry={controller:new AbortController(),users:0,ready:false};
        entry.promise=readAsset(url,entry.controller.signal).then(asset=>{entry.ready=true;return asset});
        assets.set(url,entry);
    }
    clearTimeout(entry.timer);entry.users++;
    let released=false;
    function release(){
        if(released)return;
        released=true;
        if(--entry.users===0){
            if(!entry.ready)evict(url,entry);
            else entry.timer=setTimeout(()=>evict(url,entry),90000);
        }
    }
    signal.addEventListener("abort",release,{once:true});
    try{
        const source=await entry.promise;
        signal.throwIfAborted();
        // Only object transforms and skeletons are mutable. Scene-owned coatings are cloned separately.
        const scene=file==="workwear"?source.data.scene:source.cloneRig(source.data.scene);
        return {data:{scene},cloneRig:source.cloneRig,shared:source.shared,release};
    }catch(error){release();throw error}
    finally{signal.removeEventListener("abort",release)}
}

export function placeRover(root, asset, id, parts) {
    const model=asset.data.scene;
    model.rotation.y=1.12;model.scale.setScalar(1.24);root.add(model);
    if(id==="perseverance-heat-and-power"){
        const anchor=model.localToWorld(new T.Vector3(...model.userData.rtgCenter));
        const top=anchor.clone().add(new T.Vector3(-.36,.98,.30));
        parts.rod(anchor.toArray(),top.toArray(),.012,0x786453);
        parts.plaque(top.x,top.y+.08,top.z,"MMRTG",.75);
    }
}

export function placeLanders(root,asset,parts) {
    for(const [x,dust] of [[-2.5,false],[2.5,true]]){
        const model=dust?asset.data.scene.clone(true):asset.data.scene;
        model.scale.setScalar(.73);model.position.set(x,.54,-.19);root.add(model);
        if(dust){
            const coatings=new Map();
            model.traverse(o=>{
                if(!o.isMesh)return;
                if(!coatings.has(o.material)){
                    const mat=o.material.clone();
                    mat.onBeforeCompile=s=>{
                        s.vertexShader="varying vec3 dustWorld;\n"+s.vertexShader.replace("#include <worldpos_vertex>","#include <worldpos_vertex>\ndustWorld=(modelMatrix*vec4(transformed,1.)).xyz;");
                        s.fragmentShader="varying vec3 dustWorld;\n"+s.fragmentShader.replace("#include <color_fragment>",`#include <color_fragment>
                            vec2 p=dustWorld.xz;
                            float streak=.53+.22*sin(6.*p.x+2.*sin(4.*p.y))+.13*sin(18.*p.y+p.x)+.06*sin(139.*p.x+57.*p.y);
                            vec3 n=normalize(cross(dFdx(dustWorld),dFdy(dustWorld)));
                            float deposit=smoothstep(.3,.86,n.y)*smoothstep(.45,.84,abs(p.x-2.5))*clamp(streak+.21,0.,.92);
                            diffuseColor.rgb=mix(diffuseColor.rgb,vec3(.51,.286,.158),deposit);`);
                    };
                    coatings.set(o.material,mat);
                }
                o.material=coatings.get(o.material);
            });
        }
        parts.plaque(x,1.99,-.1,dust?"500 Wh/sol":"5,000 Wh/sol",1.55);
    }
}
