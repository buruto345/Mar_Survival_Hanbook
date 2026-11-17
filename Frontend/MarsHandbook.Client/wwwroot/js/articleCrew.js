/** Conceptual crew flight clothing for the pressurized analog and comms interiors.
 * Local -Z faces a console. Dimensions share the chair and desk scale. */
export function flightCrew({box,sphere,rod,cylinder},x,z,uniform,seated,deck=0) {
    const boot=0x283237, cuffs=0x536363, tape=0xe2d3a7, skin=0xb78363;
    const hip=seated ? .51 : .70;
    const point=(dx,h,dz)=>[x+dx,deck+h,z+dz];
    const oval=(dx,h,dz,a,b,c,color)=>sphere(...point(dx,h,dz),a,b,c,color);
    const patch=(dx,h,dz,w,t,d,color)=>box(...point(dx,h,dz),w,t,d,color);
    const seam=(a,b,r,color)=>rod(point(...a),point(...b),r,color);
    // Soft coverall over the waist, boots and radio harness.
    oval(0,hip,.012,.157,.136,.118,uniform);
    oval(0,hip+.22,0,.185,.255,.121,uniform);
    patch(0,hip+.035,-.008,.31,.049,.226,boot);
    patch(0,hip+.041,-.124,.069,.037,.018,tape);
    patch(-.082,hip+.21,-.106,.082,.094,.024,cuffs);
    patch(.084,hip+.29,-.11,.073,.032,.018,tape);
    seam([0,hip+.075,-.119],[0,hip+.38,-.092],.009,tape);
    cylinder(...point(0,hip+.423,0),.093,.063,cuffs);
    oval(0,hip+.55,-.01,.113,.143,.109,skin);
    oval(0,hip+.635,.003,.116,.08,.112,uniform);
    oval(0,hip+.54,-.113,.026,.025,.026,skin);
    for(const side of [-1,1]){
        oval(side*.109,hip+.56,0,.024,.055,.044,boot);
        const knee=[side*.102,seated ? .43 : .37,seated?-.245:-.013];
        const ankle=[side*.104,.107,seated?-.255:.018];
        seam([side*.09,hip,0],knee,.087,uniform);
        oval(...knee,.083,.082,.088,cuffs);
        seam(knee,ankle,.068,uniform);
        oval(side*.104,.075,ankle[2]-.045,.084,.076,.129,boot);
        patch(side*.104,.023,ankle[2]-.054,.15,.035,.218,boot);
        const shoulder=[side*.157,hip+.34,0];
        const elbow=[side*.22,hip+.12,seated?-.08:.025];
        const wrist=[side*.17,hip+(seated ? .29 : .01),seated?-.34:-.13];
        oval(...shoulder,.083,.09,.08,uniform);
        seam(shoulder,elbow,.069,uniform);
        oval(...elbow,.064,.068,.067,cuffs);
        seam(elbow,wrist,.052,uniform);
        oval(...wrist,.049,.042,.056,cuffs);
        oval(wrist[0],wrist[1]-.005,wrist[2]-.049,.042,.036,.061,skin);
    }
    seam([-.11,hip+.53,-.023],[-.056,hip+.49,-.118],.009,boot);
    patch(.145,hip+.035,.02,.07,.124,.077,cuffs);
}
