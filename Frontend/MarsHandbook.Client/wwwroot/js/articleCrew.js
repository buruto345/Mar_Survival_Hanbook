import * as T from "../lib/three/three.module.min.js";

// Pose the weighted body in its authored (+Z forward) space before placing it at a station.
export function flightCrew(root,asset,x,z,uniform,seated,deck,id){
    const person=asset.cloneRig(asset.data.scene);
    const bones={};
    person.traverse(o=>{
        if(o.isBone)bones[o.name]=o;
        if(o.isMesh && o.material.name==="suit"){
            o.material=o.material.clone();
            o.material.color.setHex(id==="waiting-for-a-reply" && x>0 ? 0x50626a : uniform);
        }
    });
    const position=o=>o.getWorldPosition(new T.Vector3());
    function aim(name,child,dir){
        const joint=bones[name];
        const delta=new T.Quaternion().setFromUnitVectors(position(bones[child]).sub(position(joint)).normalize(),new T.Vector3(...dir).normalize());
        const parent=joint.parent.getWorldQuaternion(new T.Quaternion());
        joint.quaternion.premultiply(parent.clone().invert().multiply(delta).multiply(parent));
        person.updateMatrixWorld(true);
    }
    for(const [side,sign] of [["l",1],["r",-1]]){
        aim(side+"thigh",side+"shin",seated?[sign*.18,-.12,.98]:[sign*.09,-1,.04*sign]);
        aim(side+"shin",side+"foot",[sign*.03,-1,seated?-.12:0]);
        const reading=id==="time-off-in-isolation"&&x<0;
        aim(side+"arm",side+"forearm",[sign*.19,-1,seated?.32:.08]);
        aim(side+"forearm",side+"hand",[sign*(seated?-.32:.02),reading?.60:seated?.03:-.89,seated?1:.26]);
    }
    bones.head.rotation.y=x<0?.13:-.19;bones.head.rotation.x=seated?.12:0;
    const headStart=bones.head.rotation.x;
    // The chest joint is at the waist on this rig. Put the zipper and pocket on its front.
    const trim=new T.MeshStandardMaterial({color:0x737b74,metalness:.26,roughness:.68});
    const seam=new T.Mesh(new T.TubeGeometry(new T.CatmullRomCurve3([
        new T.Vector3(0,.03,.13),new T.Vector3(0,.20,.148),new T.Vector3(0,.34,.143),
        new T.Vector3(0,.44,.115)]),16,.0045,5,false),trim);
    bones.chest.add(seam);
    const pocket=new T.Mesh(new T.BoxGeometry(.065,.056,.009),trim);
    pocket.position.set(.073,.31,.143);pocket.rotation.y=.19;bones.chest.add(pocket);
    const wrapper=new T.Group();wrapper.add(person);wrapper.rotation.y=Math.PI+(seated?0:x<1?.48:-.42);
    wrapper.scale.setScalar(.87);
    person.updateMatrixWorld(true);
    wrapper.position.set(x,deck+(seated?.53-.87*position(bones.hips).y:0),z);
    if(!seated)wrapper.position.y-=.87*(Math.min(position(bones.lfoot).y,position(bones.rfoot).y)-.075);
    if(id==="waiting-for-a-reply"){
        const band=new T.Mesh(new T.TorusGeometry(.093,.009,6,28,Math.PI),new T.MeshStandardMaterial({color:0x27353c}));
        band.position.set(0,.149,.048);bones.head.add(band);
        const boom=new T.Mesh(new T.TubeGeometry(new T.CatmullRomCurve3([new T.Vector3(-.088,.13,.05),new T.Vector3(-.091,.081,.116),new T.Vector3(-.040,.075,.146)]),12,.006,6,false),band.material);
        bones.head.add(boom);
    }
    root.add(wrapper);
    const elbow=bones.lforearm.quaternion.clone();
    const reach=elbow.clone().multiply(new T.Quaternion().setFromAxisAngle(new T.Vector3(1,0,0),seated?.24:.38));
    return t=>{bones.lforearm.quaternion.slerpQuaternions(elbow,reach,t);bones.head.rotation.x=headStart+.12*t;bones.head.rotation.y=(x<0?.13:-.19)+.19*t;};
}
