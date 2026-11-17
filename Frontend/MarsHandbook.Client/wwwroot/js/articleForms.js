// Original parametric surfaces for folded film, upholstery, multilayer shells and sectioned equipment.
import * as T from "../lib/three/three.module.min.js";

function surface(cols,rows,point) {
    const vertices=[],indices=[],uv=[];
    for(let j=0;j<=rows;j++)for(let i=0;i<=cols;i++){
        const u=i/cols,v=j/rows;vertices.push(...point(u,v));uv.push(u,v);
        if(i<cols&&j<rows){const k=j*(cols+1)+i;indices.push(k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2);}
    }
    const g=new T.BufferGeometry();g.setAttribute("position",new T.Float32BufferAttribute(vertices,3));
    g.setAttribute("uv",new T.Float32BufferAttribute(uv,2));g.setIndex(indices);g.computeVertexNormals();return g;
}
const rounded=(x,p)=>Math.sign(x)*Math.abs(x)**p;

export function makeForms({mesh,box,rod,plaque,tube},C){
    const geometries=new Map();
    function part(key,f,color,x,y,z,w=1,h=1,d=1){
        if(!geometries.has(key))geometries.set(key,f());
        const o=mesh(geometries.get(key),color,x,y,z);o.scale.set(w,h,d);
        o.material.side=T.DoubleSide;return o;
    }
    // A sealed perimeter encloses two bowed, wrinkled faces. Both faces meet along the same seam.
    function soft(x,y,z,w,h,d,color,variant=0){
        return part("soft"+variant,()=>surface(48,22,(u,v)=>{
            const a=u*2*Math.PI,b=(v-.5)*Math.PI;
            const px=rounded(Math.cos(a),.32)*rounded(Math.cos(b),.32),py=rounded(Math.sin(a),.32)*rounded(Math.cos(b),.32);
            const wrinkle=.13*Math.sin(17*px+8*py+variant)*Math.cos(11*py-variant)+.08*Math.sin(29*py+6*px);
            return [px*.5,py*.5,.5*rounded(Math.sin(b),.90)*(1+wrinkle)];
        }),color,x,y,z,w,h,d);
    }
    function pouch(x,y,z,i){
        const color=[0x91785a,0x959b97,0xab7950][i%3];
        soft(x,y,z,.42,.52,.19,color,i%3);
        box(x,y+.249,z,.405,.032,.035,0x887a65);
        plaque(x,y+.018,z+.103,["MEAL","DRINK","RATION"][i%3],.234);
    }
    function berth(x){
        const quilt=soft(x,.59,-.66,1.12,1.17,.17,0x536e78,1);quilt.rotation.x=-Math.PI/2;
        const pillow=soft(x,.625,-1.40,.83,.32,.21,0xe2ded0,2);pillow.rotation.x=-Math.PI/2;
        const curtain=part("curtain",()=>surface(38,10,(u,v)=>[(u-.5)*.45,(v-.5)*1.03,.051*Math.sin(9*Math.PI*u)+.012*Math.sin(8*v+16*u)]),
            0xaf9c7e,x-.33,1.0,-.13);curtain.rotation.z=-.025;
    }
    function chair(x,z){
        part("chair",()=>surface(22,32,(u,v)=>{
            const a=(u-.5)*2;
            const k=Math.min(1,v/.53);
            const rise=Math.max(0,(v-.53)/.47);
            return [a*(.245+.025*Math.sin(v*Math.PI)),.42+.052*a*a+.51*rise,.48*(k-.52)+.10*rise*rise-.032*a*a];
        }),0x39505b,x,0,z);
        for(const sign of [-1,1])tube([[x+sign*.21,.43,z-.19],[x+sign*.23,.065,z-.22],[x+sign*.23,.05,z+.18],[x+sign*.20,.44,z+.17]],C.silver,.023);
    }
    function shell(x,len,r,compact){
        const o=part(compact?"packed":"deployed",()=>surface(72,72,(u,v)=>{
            const a=u*2*Math.PI,ax=2*v-1;
            const taper=.46+.54*Math.sin(Math.PI*v)**.52;
            const ribs=1-(compact?.103:.038)*(.5+.5*Math.cos(v*2*Math.PI*(compact?12:9)))**3;
            const weave=1-.018*Math.cos(a*16)**10;
            const radius=taper*ribs*weave;
            return [ax*.5,radius*Math.sin(a),radius*Math.cos(a)];
        }),0xd4cebd,x,1.12,0,len,r,r);
        const uv=o.geometry.attributes.uv,colors=[];
        for(let i=0;i<uv.count;i++){
            const u=uv.getX(i),v=uv.getY(i);
            const taped=Math.abs(Math.sin(Math.PI*v*(compact?12:9)))<.16 ||
                Math.abs(Math.sin(Math.PI*u*12))<.14;
            colors.push(...(taped?[.62,.56,.43]:[1,1,1]));
        }
        o.geometry.setAttribute("color",new T.Float32BufferAttribute(colors,3));
        o.material.vertexColors=true;
        return o;
    }
    function vessel(x,y,z,r,h,color,section=false){
        const profile=[[.01,0],[.61,.025],[.88,.09],[1,.19],[1,.79],[.91,.9],[.60,.96],[.21,1],[.01,1]];
        return part("vessel"+section,()=>new T.LatheGeometry(profile.map(([rad,height])=>new T.Vector2(rad,height-.5)),52,section?.63:0,section?4.74:Math.PI*2),color,x,y,z,r,h,r);
    }
    function plate(x,y,z,w,h,d,color){
        const shape=new T.Shape(),r=.18;
        shape.moveTo(-.5+r,-.5);shape.lineTo(.5-r,-.5);shape.quadraticCurveTo(.5,-.5,.5,-.5+r);
        shape.lineTo(.5,.5-r);shape.quadraticCurveTo(.5,.5,.5-r,.5);shape.lineTo(-.5+r,.5);
        shape.quadraticCurveTo(-.5,.5,-.5,.5-r);shape.lineTo(-.5,-.5+r);shape.quadraticCurveTo(-.5,-.5,-.5+r,-.5);
        for(const a of [-.33,.33])for(const b of [-.30,.30]){
            const hole=new T.Path();hole.absarc(a,b,.042,0,Math.PI*2,true);shape.holes.push(hole);
        }
        const o=part("portPlate",()=>new T.ExtrudeGeometry(shape,{depth:1,bevelEnabled:false,curveSegments:6}),color,x,y,z,w,d,h);
        o.rotation.x=-Math.PI/2;return o;
    }
    return {soft,pouch,berth,chair,shell,vessel,plate};
}

export function leafShape(){
    return surface(14,32,(u,v)=>{
        const s=2*u-1, envelope=Math.sin(Math.PI*v)**.71;
        const scallop=1+.13*Math.sin(37*v+.65*s)+.05*Math.sin(71*v);
        return [.185*s*envelope*scallop+.033*v*v,.12*v+.19*envelope-.064*s*s*envelope
            +.035*Math.abs(s)**2*Math.sin(37*v+1.2*s)*envelope+.011*Math.cos(21*v-4*Math.abs(s))*envelope,.52*v];
    });
}
