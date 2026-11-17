import {afterPaint,markPhase} from "./viewerScheduling.js";
const mounts=new Map();

// The stable component key survives removal of its Blazor-owned DOM element.
export async function mount(host,id,title,callback,key){
    const state={host,module:null,controller:new AbortController()};
    mounts.set(key,state);
    const start=performance.now();
    const workerCapable=typeof Worker==="function" && "transferControlToOffscreen" in HTMLCanvasElement.prototype;
    const module=await (id==="outpost"?import("./livingModels.js"):
        workerCapable?import("./articleOffscreen.js"):import("./articleModels.js"));
    if(mounts.get(key)!==state)return null;
    state.module=module;
    markPhase("modules",start,{id});
    const reference=await module.mount(host,id,title,callback);
    if(mounts.get(key)!==state)return null;
    try{await afterPaint(state.controller.signal)}
    catch(error){if(state.controller.signal.aborted)return null;throw error}
    markPhase("paintOpportunity",start,{id});
    return reference;
}
export function unmount(key){
    const state=mounts.get(key);
    mounts.delete(key);
    state?.controller.abort();
    state?.module?.unmount(state.host);
}
export function command(key,name,...args){
    const state=mounts.get(key);
    state?.module?.[name]?.(state.host,...args);
}
