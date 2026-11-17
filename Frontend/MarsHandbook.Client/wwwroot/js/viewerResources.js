import * as T from "../lib/three/three.module.min.js";

// One idle context per tab. Overlapping mounts lease independent contexts until the old view releases.
let idle=null, idleTimer;
export function acquireRenderer(){
    clearTimeout(idleTimer);
    const slot=idle;idle=null;
    if(slot && !slot.renderer.getContext().isContextLost())return slot;
    if(slot)destroy(slot);
    const fresh={renderer:new T.WebGLRenderer({antialias:true}), scenes:new Map(), environments:new Map()};
    fresh.restored=()=>{
        clearEnvironments(fresh);
        for(const scene of fresh.scenes.values())scene.shadowDirty=true;
        fresh.onRestore?.();
    };
    fresh.renderer.domElement.addEventListener("webglcontextrestored",fresh.restored);
    return fresh;
}
export function clearEnvironments(slot){
    for(const target of slot.environments.values())target.dispose();
    slot.environments.clear();
}
export function destroy(slot){
    for(const entry of slot.scenes.values())entry.dispose();
    slot.scenes.clear();clearEnvironments(slot);
    slot.renderer.domElement.removeEventListener("webglcontextrestored",slot.restored);
    slot.renderer.dispose();slot.renderer.forceContextLoss();
}
export function releaseRenderer(slot){
    if(idle && idle!==slot)destroy(idle);
    idle=slot;clearTimeout(idleTimer);
    idleTimer=setTimeout(()=>{if(idle===slot){idle=null;destroy(slot)}},90000);
}
export function storeScene(slot,id,entry){
    slot.scenes.delete(id);slot.scenes.set(id,entry);
    if(slot.scenes.size>3){
        const [oldest,scene]=slot.scenes.entries().next().value;
        slot.scenes.delete(oldest);scene.dispose();
    }
}
export function resources(root){
    const items=new Set();
    root.traverse(o=>{
        if(o.geometry)items.add(o.geometry);
        if(o.isInstancedMesh)items.add(o);
        if(o.skeleton)items.add(o.skeleton);
        for(const mat of [].concat(o.material||[])){
            items.add(mat);
            for(const value of Object.values(mat))if(value?.isTexture)items.add(value);
        }
    });
    return items;
}
export function disposeResources(items,shared){
    for(const item of items)if(!shared?.has(item)){item.dispose();item.image?.close?.()}
}
