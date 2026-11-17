import * as T from "../lib/three/three.module.min.js";
import {articleDesign} from "./articleModels.js";
import {prepareScene,prepareGPU,configureRenderer,references,applyEnvironment} from "./livingModels.js";
import {storeScene,clearEnvironments} from "./viewerResources.js";

let slot,active,request,sequence=Promise.resolve(),frame;
function detach(){
    request?.abort();
    cancelAnimationFrame(frame);
    if(active?.entry.article.progress){
        const article=active.entry.article;
        for(const update of article.updates)update(0);
        article.progress=0;active.entry.shadowDirty=true;
    }
    active=null;
}
function draw(){
    const {entry,camera,width,height,yaw,pitch,zoom}=active;
    if(!width || !height)return;
    const c=entry.center;
    camera.position.set(c.x+25*Math.sin(yaw)*Math.cos(pitch),c.y+25*Math.sin(pitch),c.z+25*Math.cos(yaw)*Math.cos(pitch));
    camera.lookAt(c);camera.updateMatrixWorld();
    const half=entry.size.length()*.54*Math.max(1,height/width)*zoom;
    camera.left=-half*width/height;camera.right=-camera.left;camera.top=half;camera.bottom=-half;
    camera.updateProjectionMatrix();slot.renderer.render(entry.scene,camera);
}
function resize(width,height){
    if(width>0 && height>0){
        active.width=width;active.height=height;
        slot.renderer.setSize(width,height,false);
    }
}
async function start(m,signal){
    let entry;
    const cached=slot.scenes.has(m.id);
    try{
        signal.throwIfAborted();
        performance.clearMeasures();
        entry=slot.scenes.get(m.id) ?? await prepareScene(slot,m.id,articleDesign(m.id),false,signal);
        signal.throwIfAborted();
        configureRenderer(slot.renderer,entry,false,m.dpr,m.width,cached);
        const camera=new T.OrthographicCamera(-4,4,3,-3,.1,80);
        if(!cached)await prepareGPU(slot.renderer,entry.scene,camera,signal,m.id);
        signal.throwIfAborted();
        active={key:m.key,entry,camera,yaw:.42,pitch:.52,zoom:.78,width:0,height:0};
        resize(m.width,m.height);const first=performance.now();draw();
        performance.measure("living.first.submit",{start:first});
        storeScene(slot,m.id,entry);
        postMessage({key:m.key,kind:"ready",reference:references[m.id],cached,
            memory:slot.renderer.info.memory,scenes:slot.scenes.size,phases:Object.fromEntries(performance.getEntriesByType("measure").map(m=>[m.name,m.duration]))});
    }catch(error){
        if(entry && !cached && !slot.scenes.has(m.id))entry.dispose();
        if(!signal.aborted)postMessage({key:m.key,kind:"error",message:String(error)});
    }
}
function illustrate(value,reduced){
    cancelAnimationFrame(frame);
    const article=active.entry.article,from=article.progress,to=value?1:0,start=performance.now();
    function step(now){
        const t=reduced?1:Math.min(1,(now-start)/560);
        article.progress=T.MathUtils.lerp(from,to,t*t*(3-2*t));
        for(const update of article.updates)update(article.progress);
        slot.renderer.shadowMap.needsUpdate=true;draw();
        if(t<1)frame=requestAnimationFrame(step);
    }
    step(start);
}
onmessage=({data:m})=>{
    if(m.kind==="init"){
        try{
            slot={renderer:new T.WebGLRenderer({canvas:m.canvas,antialias:true}),scenes:new Map(),environments:new Map()};
            m.canvas.addEventListener("webglcontextrestored",()=>{
                clearEnvironments(slot);
                for(const state of slot.scenes.values()){
                    applyEnvironment(slot,state.scene,state.article,state.article.id);state.shadowDirty=true;
                }
                if(active){slot.renderer.shadowMap.needsUpdate=true;draw()}
            });
        }catch(error){postMessage({kind:"unsupported",message:String(error)})}
    }else if(m.kind==="mount" && slot){
        detach();
        request=new AbortController();
        const signal=request.signal;
        sequence=sequence.then(()=>start(m,signal));
    }else if(m.kind==="detach"){
        detach();
    }else if(active && active.key===m.key){
        if(m.kind==="resize"){
            if(active.width===m.width && active.height===m.height)return;
            resize(m.width,m.height);
        }
        else if(m.kind==="camera")Object.assign(active,m.pose);
        else if(m.kind==="illustrate"){illustrate(m.value,m.reduced);return}
        draw();
    }
};
