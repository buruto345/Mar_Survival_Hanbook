export async function yieldTask(signal){
    if(globalThis.scheduler?.yield)await scheduler.yield();
    else await new Promise(resolve=>setTimeout(resolve,0));
    signal.throwIfAborted();
}
export function afterPaint(signal){
    return new Promise((resolve,reject)=>{
        signal.throwIfAborted();
        let frame=requestAnimationFrame(()=>{frame=requestAnimationFrame(()=>{signal.removeEventListener("abort",cancel);resolve()})});
        function cancel(){cancelAnimationFrame(frame);reject(signal.reason)}
        signal.addEventListener("abort",cancel,{once:true});
    });
}

export function textureCanvas(){
    return typeof document==="undefined"?new OffscreenCanvas(1,1):document.createElement("canvas");
}
export function markPhase(name,start,detail){
    const label="living."+name;
    performance.clearMeasures(label);
    performance.measure(label,{start,detail});
}
