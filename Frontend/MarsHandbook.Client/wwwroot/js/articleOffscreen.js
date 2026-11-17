import {afterPaint} from "./viewerScheduling.js";

const sessions=new WeakMap();
let idle,idleTimer,nextKey=0;
function createEndpoint(){
    const canvas=document.createElement("canvas");
    const offscreen=canvas.transferControlToOffscreen();
    const worker=new Worker(new URL("./articleWorker.js",import.meta.url),{type:"module"});
    worker.postMessage({kind:"init",canvas:offscreen},[offscreen]);
    return {worker,canvas};
}
function bindCamera(canvas,send){
    let yaw=.42,pitch=.52,zoom=.78,start;
    const camera=()=>send({kind:"camera",pose:{yaw,pitch,zoom}});
    const tilt=value=>Math.max(.16,Math.min(1.25,value));
    canvas.onpointerdown=e=>{start=[e.clientX,e.clientY,yaw,pitch];canvas.setPointerCapture(e.pointerId)};
    canvas.onpointermove=e=>{
        if(!start || Math.hypot(e.clientX-start[0],e.clientY-start[1])<6)return;
        yaw=start[2]-(e.clientX-start[0])*.009;pitch=tilt(start[3]+(e.clientY-start[1])*.006);camera();
    };
    canvas.onpointerup=canvas.onpointercancel=()=>{start=null};
    canvas.onkeydown=e=>{
        if(!["ArrowLeft","ArrowRight","ArrowUp","ArrowDown"].includes(e.key))return;
        e.preventDefault();
        if(e.key==="ArrowLeft")yaw-=.15;if(e.key==="ArrowRight")yaw+=.15;
        if(e.key==="ArrowUp")pitch=tilt(pitch+.12);if(e.key==="ArrowDown")pitch=tilt(pitch-.12);
        camera();
    };
    return {
        orbit(delta){yaw+=delta;camera()},
        zoom(delta){zoom=Math.max(.45,Math.min(1.6,zoom+delta));camera()},
        reset(){yaw=.42;pitch=.52;zoom=.78;camera()},
        illustrate(value){send({kind:"illustrate",value,reduced:matchMedia("(prefers-reduced-motion: reduce)").matches})}
    };
}
export async function mount(host,id,title,callback){
    unmount(host);
    const key=++nextKey,controller=new AbortController(),session={controller};
    sessions.set(host,session);
    try{
        await afterPaint(controller.signal);
        clearTimeout(idleTimer);
        const endpoint=idle||createEndpoint();idle=null;
        const {worker,canvas}=endpoint;
        session.endpoint=endpoint;
        const send=message=>worker.postMessage({...message,key});
        canvas.tabIndex=0;canvas.setAttribute("role","img");canvas.setAttribute("aria-label",title);
        canvas.style.visibility="hidden";host.replaceChildren(canvas);
        session.controls=bindCamera(canvas,send);
        const ready=new Promise((resolve,reject)=>{
            session.cancel=()=>resolve(null);
            worker.onerror=e=>reject(new Error(e.message||"Worker initialization failed"));
            worker.onmessage=({data})=>{
                if(data.kind==="unsupported"){reject(new Error(data.message));return}
                if(data.key!==key)return;
                if(data.kind==="error"){session.sceneError=true;reject(new Error(data.message));return}
                if(data.kind==="ready"){
                    performance.clearMeasures("living.worker.ready");
                    performance.measure("living.worker.ready",{start:session.started,detail:data});
                    canvas.style.visibility="";
                    canvas.dataset.scene=id;
                    resolve(data.reference);
                }
            };
        });
        session.started=performance.now();
        send({kind:"mount",id,width:host.clientWidth,height:host.clientHeight,dpr:devicePixelRatio});
        session.observer=new ResizeObserver(()=>send({kind:"resize",width:host.clientWidth,height:host.clientHeight}));
        session.observer.observe(host);
        return await ready;
    }catch(error){
        if(controller.signal.aborted)return null;
        // Some browsers expose OffscreenCanvas but cannot create a worker WebGL2 context.
        if(session.endpoint){session.endpoint.worker.terminate();session.endpoint=null}
        session.observer?.disconnect();
        host.replaceChildren();
        if(session.sceneError)throw error;
        const fallback=await import("./articleModels.js");
        if(controller.signal.aborted)return null;
        session.fallback=fallback;
        return fallback.mount(host,id,title,callback);
    }
}
export function unmount(host){
    const s=sessions.get(host);if(!s)return;
    sessions.delete(host);s.controller.abort();s.cancel?.();s.observer?.disconnect();
    s.fallback?.unmount(host);
    if(!s.endpoint)return;
    const {worker,canvas}=s.endpoint;
    worker.postMessage({kind:"detach"});worker.onmessage=worker.onerror=null;
    canvas.onpointerdown=canvas.onpointermove=canvas.onpointerup=canvas.onpointercancel=canvas.onkeydown=null;
    canvas.remove();idle?.worker.terminate();clearTimeout(idleTimer);
    idle=s.endpoint;idleTimer=setTimeout(()=>{idle?.worker.terminate();idle=null},90000);
}
function control(host,name,...args){
    const s=sessions.get(host);
    if(s?.fallback)s.fallback[name](host,...args);else s?.controls?.[name](...args);
}
export const orbit=(host,n)=>control(host,"orbit",n);
export const zoom=(host,n)=>control(host,"zoom",n);
export const reset=host=>control(host,"reset");
export const illustrate=(host,value)=>control(host,"illustrate",value);
