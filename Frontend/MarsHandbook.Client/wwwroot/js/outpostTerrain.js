import {yieldTask} from "./viewerScheduling.js";
import * as T from "../lib/three/three.module.min.js";

function hash(x,z) {
    let n=Math.imul(x,374761393)+Math.imul(z,668265263);
    n=Math.imul(n^(n>>>13),1274126177);
    return ((n^(n>>>16))>>>0)/4294967296;
}
function noise(x,z) {
    const i=Math.floor(x), j=Math.floor(z);
    const u=T.MathUtils.smoothstep(x-i,0,1), v=T.MathUtils.smoothstep(z-j,0,1);
    return T.MathUtils.lerp(T.MathUtils.lerp(hash(i,j),hash(i+1,j),u),
        T.MathUtils.lerp(hash(i,j+1),hash(i+1,j+1),u),v);
}
function formation(x,z) {
    const warped=x*.25+z*.098+noise(x*.21,z*.21)*1.7;
    const strata=Math.abs(Math.sin(warped));
    return .85*noise(x*.16,z*.16)+.35*noise(x*.54,z*.54)+.45*strata;
}
// The construction bench is level; away from it, benches and fractured ledges emerge.
export function elevation(x,z,ridge=formation(x,z)) {
    const edge=Math.max(Math.abs(x-.1)-6.9,Math.abs(z)-6.7);
    const blend=T.MathUtils.smoothstep(edge,0,3.5);
    const shelf=T.MathUtils.smoothstep(ridge,.79,1.05);
    const distant=T.MathUtils.smoothstep(edge,6,29);
    return -.095+blend*(ridge*.43+shelf*.79-.43+distant*(.65+noise(x*.07,z*.07)*4.1));
}

const serviceRoad=[
    [-6.4,2,-5.1,.35],[-5.1,.35,-3.8,.35],[-3.8,.35,-3.8,5.7],
    [-3.8,5.7,2.9,6.55],[2.9,6.55,5.2,5.6],[5.2,5.6,7.13,4.5],
    [7.13,4.5,7.13,-5.6],[7.13,-5.6,5.8,-5.6],[7.13,3.58,5.4,3.58],
    [7.13,-.62,3.12,-.62],[-5.1,.35,-4.7,-.4]
];
function roadDistance(x,z) {
    let best=Infinity;
    for(const [ax,az,bx,bz] of serviceRoad) {
        const dx=bx-ax,dz=bz-az;
        const t=T.MathUtils.clamp(((x-ax)*dx+(z-az)*dz)/(dx*dx+dz*dz),0,1);
        best=Math.min(best,(x-ax-t*dx)**2+(z-az-t*dz)**2);
    }
    return Math.sqrt(best);
}
function gritTexture(random) {
    const n=256, bytes=new Uint8Array(n*n*4);
    for(let y=0;y<n;y++) for(let x=0;x<n;x++){
        const grains=random();
        const grit=Math.min(255,167+grains*67+noise(x*.12,y*.12)*21);
        const i=4*(y*n+x);
        bytes[i]=bytes[i+1]=bytes[i+2]=grit;bytes[i+3]=255;
    }
    const tex=new T.DataTexture(bytes,n,n,T.RGBAFormat);
    tex.wrapS=tex.wrapT=T.RepeatWrapping;
    tex.magFilter=T.LinearFilter;tex.minFilter=T.LinearMipmapLinearFilter;
    tex.generateMipmaps=true;tex.needsUpdate=true;
    return tex;
}

// Wind-eroded ledges expose successive bedding planes. All faces share one draw.
function exposedBeds(root,random) {
    const positions=[],colors=[],color=new T.Color();
    function triangle(a,b,c,tint,light) {
        color.setHex(tint).multiplyScalar(light);
        positions.push(...a,...b,...c);
        for(let i=0;i<3;i++) colors.push(color.r,color.g,color.b);
    }
    const outcrops=[[-9,-4,2.7],[-9.4,-1,1.8],[-7.8,-8.6,3],
        [-3,-10.7,2.5],[8.8,-7.3,2.6],[9.1,1.8,2.8],[-7.8,6.8,2.1],
        [2,9.5,2.4],[12,8.9,3.1],[1,-18,5],[-15,-14,5.4],[16,-13,5.2]];
    for(const [cx,cz,length] of outcrops) {
        const direction=-.48+random()*.52;
        for(let ledge=0;ledge<5;ledge++) {
            const x=cx+Math.cos(direction)*(ledge-2)*length*.54;
            const z=cz+Math.sin(direction)*(ledge-2)*length*.54;
            if(Math.abs(x)<7.65 && Math.abs(z)<7.15) continue;
            const radius=length*(.32+random()*.31),width=radius*(.34+random()*.25);
            const height=radius*(.16+random()*.21);
            const outline=Array.from({length:10},(_,i)=>{
                const angle=i*Math.PI/5, jitter=.76+random()*.32;
                const dx=Math.cos(angle)*radius*jitter,dz=Math.sin(angle)*width*jitter;
                return [dx*Math.cos(direction)-dz*Math.sin(direction),dx*Math.sin(direction)+dz*Math.cos(direction)];
            });
            let prev;
            for(const [level,scale,tint] of [[-.85,1.08,0x644a3e],[.3,1,0x826554],[.42,1.01,0x977660],[.57,.95,0x735b50],[.91,.91,0x94715e],[1,.82,0x98775f]]) {
                const row=outline.map(([dx,dz])=>{
                    const px=x+dx*scale,pz=z+dz*scale;
                    return [px,elevation(px,pz)+height*level*(.65+.55*noise(px*1.4,pz*1.4))+.05*dx,pz];
                });
                if(prev) for(let i=0;i<row.length;i++){
                    const j=(i+1)%row.length,shade=.91+random()*.12;
                    triangle(prev[i],row[i],prev[j],tint,shade);
                    triangle(prev[j],row[i],row[j],tint,shade);
                }
                if(level===1) for(let i=0;i<row.length;i++){
                    triangle([x,elevation(x,z)+height*1.07,z],row[(i+1)%row.length],row[i],tint,.83+random()*.19);
                }
                prev=row;
            }
        }
    }
    const geometry=new T.BufferGeometry();
    geometry.setAttribute("position",new T.Float32BufferAttribute(positions,3));
    geometry.setAttribute("color",new T.Float32BufferAttribute(colors,3));
    geometry.computeVertexNormals();
    const ledges=new T.Mesh(geometry,new T.MeshStandardMaterial({vertexColors:true,roughness:1,side:T.DoubleSide}));
    ledges.userData.mapGround=true;ledges.castShadow=ledges.receiveShadow=true;root.add(ledges);
}

export async function buildLandscape(root,random,signal) {
    const resolution=224;
    const ground=new T.PlaneGeometry(2,2,resolution,resolution);
    ground.rotateX(-Math.PI/2);
    const pos=ground.attributes.position,uv=ground.attributes.uv;
    const colors=[],color=new T.Color(),roadColor=new T.Color(0xb1967b);
    // Concentrate vertices at the installation; distant folds share the same continuous mesh.
    const spread=u=>u*19+Math.sign(u)*Math.abs(u)**5*91;
    let slice=performance.now();
    for(let i=0;i<pos.count;i++){
        if((i&511)===0 && performance.now()-slice>8){await yieldTask(signal);slice=performance.now()}
        const x=spread(pos.getX(i)),z=spread(pos.getZ(i));
        const strata=formation(x,z);
        const y=elevation(x,z,strata);
        pos.setXYZ(i,x,y,z);uv.setXY(i,x*.76,z*.76);
        const patch=noise(x*.32,z*.32), fine=noise(x*1.6,z*1.6);
        color.setRGB(.235+.13*patch+.036*fine,.096+.075*patch+.036*fine,.053+.049*patch+.029*fine);
        if (y>.16) color.multiplyScalar(.72+.31*strata);
        if(Math.abs(x)<9&&Math.abs(z)<9){
            const d=roadDistance(x,z);
            color.lerp(roadColor,(1-T.MathUtils.smoothstep(d,.37,.77))*(.80+.15*fine));
            // Slightly darker parallel wheel lines within the compacted lane.
            if (d<.54) color.multiplyScalar(1-.17*Math.exp(-((d-.29)**2)/.006));
        }
        colors.push(color.r,color.g,color.b);
    }
    ground.setAttribute("color",new T.Float32BufferAttribute(colors,3));
    ground.computeVertexNormals();ground.computeBoundingSphere();
    await yieldTask(signal);
    const texture=gritTexture(random);
    const surface=new T.Mesh(ground,new T.MeshStandardMaterial({vertexColors:true,roughness:.95,
        map:texture,roughnessMap:texture,bumpMap:texture,bumpScale:.032}));
    surface.receiveShadow=true;surface.userData.mapGround=true;root.add(surface);

    await yieldTask(signal);
    exposedBeds(root,random);
    const basalt=new T.MeshStandardMaterial({roughness:.96,flatShading:true});
    // Flattened, chipped fragments follow the same bedding direction in each outcrop.
    for(let variety=0;variety<3;variety++) {
        const geo=new T.IcosahedronGeometry(1,1);
        const p=geo.attributes.position;
        for(let i=0;i<p.count;i++){
            const x=p.getX(i),y=p.getY(i),z=p.getZ(i);
            const chip=.77+.33*noise(x*4+variety*11,z*4+y);
            p.setXYZ(i,x*chip+.18*y,Math.max(-.68,Math.min(.68,y))*chip,z*chip);
        }
        geo.computeVertexNormals();
        const stones=[];
        for(let i=0;i<950;i++){
            const x=(random()-.5)*56,z=(random()-.5)*48;
            const edge=Math.max(Math.abs(x)-7.6,Math.abs(z)-7.1);
            if(edge<0 || noise(x*.38,z*.38)<.43 || roadDistance(x,z)<.88)continue;
            const size=.035+random()**3*(.20+.74*T.MathUtils.smoothstep(edge,1,8));
            stones.push([x,z,size]);
        }
        const field=new T.InstancedMesh(geo,basalt,stones.length);
        const transform=new T.Object3D();
        stones.forEach(([x,z,r],i)=>{
            transform.position.set(x,elevation(x,z)+.10*r,z);
            transform.rotation.set(.12*random(),.45+noise(x*.2,z*.2)*1.7,.19*random());
            transform.scale.set(r*1.75,r*.72,r*(.71+random()*.55));
            transform.updateMatrix();field.setMatrixAt(i,transform.matrix);
            color.setHex(variety===1?0x926f5b:0x785c52).multiplyScalar(.72+random()*.46);
            field.setColorAt(i,color);
        });
        field.computeBoundingSphere();field.receiveShadow=true;field.castShadow=true;
        field.userData.mapGround=true;root.add(field);
    }
}
