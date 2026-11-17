/* Dependency-free shaded 3D mesh renderer. All gallery objects are actual meshes. */
window.Moldrilla3D=(()=>{
 const mesh=()=>({v:[],f:[]});
 function add(m,part,offset=[0,0,0],scale=[1,1,1]){const n=m.v.length;m.v.push(...part.v.map(v=>v.map((x,i)=>x*scale[i]+offset[i])));m.f.push(...part.f.map(f=>({i:f.i.map(i=>i+n),c:f.c})));}
 function sphere(color,lat=16,lon=24){const m=mesh();for(let a=0;a<=lat;a++){const p=Math.PI*a/lat;for(let b=0;b<=lon;b++){const q=2*Math.PI*b/lon;m.v.push([Math.sin(p)*Math.cos(q),Math.cos(p),Math.sin(p)*Math.sin(q)]);}}for(let a=0;a<lat;a++)for(let b=0;b<lon;b++){const i=a*(lon+1)+b;m.f.push({i:[i,i+lon+1,i+lon+2],c:color},{i:[i,i+lon+2,i+1],c:color});}return m;}
 function lathe(profile,color,segments=48){const m=mesh();profile.forEach(([r,y])=>{for(let j=0;j<=segments;j++){const a=2*Math.PI*j/segments;m.v.push([r*Math.cos(a),y,r*Math.sin(a)]);}});for(let k=0;k<profile.length-1;k++)for(let j=0;j<segments;j++){const i=k*(segments+1)+j;m.f.push({i:[i,i+segments+1,i+segments+2],c:color},{i:[i,i+segments+2,i+1],c:color});}return m;}
 function box(c){const m=mesh();m.v=[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]];[[0,3,2,1],[4,5,6,7],[0,1,5,4],[3,7,6,2],[0,4,7,3],[1,2,6,5]].forEach(a=>{m.f.push({i:[a[0],a[1],a[2]],c},{i:[a[0],a[2],a[3]],c});});return m;}
 function torus(c,major=1,minor=.18,steps=48){const m=mesh();for(let i=0;i<=steps;i++)for(let j=0;j<=12;j++){const a=i/steps*2*Math.PI,b=j/12*2*Math.PI;m.v.push([(major+minor*Math.cos(b))*Math.cos(a),minor*Math.sin(b),(major+minor*Math.cos(b))*Math.sin(a)]);}for(let i=0;i<steps;i++)for(let j=0;j<12;j++){const a=i*13+j;m.f.push({i:[a,a+13,a+14],c},{i:[a,a+14,a+1],c});}return m;}
 function ico(c){const t=(1+Math.sqrt(5))/2;const m=mesh();m.v=[[-1,t,0],[1,t,0],[-1,-t,0],[1,-t,0],[0,-1,t],[0,1,t],[0,-1,-t],[0,1,-t],[t,0,-1],[t,0,1],[-t,0,-1],[-t,0,1]].map(v=>v.map(x=>x/1.9));[[0,11,5],[0,5,1],[0,1,7],[0,7,10],[0,10,11],[1,5,9],[5,11,4],[11,10,2],[10,7,6],[7,1,8],[3,9,4],[3,4,2],[3,2,6],[3,6,8],[3,8,9],[4,9,5],[2,4,11],[6,2,10],[8,6,7],[9,8,1]].forEach(i=>m.f.push({i,c}));return m;}
 function cavitySlab(color,wells,half=1){const m=mesh(),n=46,depth=-.38;
  function level(x,z){let y=.18;for(const well of wells){const dx=x-well.x,dz=z-well.z,a=Math.atan2(dz,dx),r=well.r*(well.star?1+.22*Math.cos(5*a):1),d=Math.hypot(dx,dz)/r;y-=well.depth*Math.max(0,1-Math.pow(d,8));}return y;}
  for(let i=0;i<=n;i++)for(let j=0;j<=n;j++){const x=(i/n*2-1)*half,z=j/n*2-1;m.v.push([x,level(x,z),z]);}
  for(let i=0;i<n;i++)for(let j=0;j<n;j++){const k=i*(n+1)+j;m.f.push({i:[k,k+1,k+n+2],c:color},{i:[k,k+n+2,k+n+1],c:color});}
  const corners=[[-half,depth,-1],[half,depth,-1],[half,depth,1],[-half,depth,1],[-half,.18,-1],[half,.18,-1],[half,.18,1],[-half,.18,1]],base=m.v.length;m.v.push(...corners);
  [[0,1,5,4],[1,2,6,5],[2,3,7,6],[3,0,4,7],[0,3,2,1]].forEach(f=>m.f.push({i:[base+f[0],base+f[1],base+f[2]],c:color},{i:[base+f[0],base+f[2],base+f[3]],c:color}));return m;
 }
 function star(color){const m=mesh(),n=10;for(let y of [-.07,.07])for(let i=0;i<n;i++){const a=i*Math.PI/5,r=i%2?.3:.6;m.v.push([Math.cos(a)*r,y,Math.sin(a)*r]);}m.v.push([0,-.07,0],[0,.07,0]);for(let i=0;i<n;i++){const j=(i+1)%n;m.f.push({i:[20,j,i],c:color},{i:[21,i+n,j+n],c:color},{i:[i,j,j+n],c:color},{i:[i,j+n,i+n],c:color});}return m;}
 function model(type){const m=mesh();const gold='#e6bd37',cream='#e6e2d8',green='#38663d',blue='#455391';const ball=(p,s,c)=>add(m,sphere(c),p,s);
  if(type==='mold-open'){
   add(m,cavitySlab(blue,[{x:0,z:0,r:.62,depth:.4,star:true}]),[-.15,-.32,0],[1,.9,.85]);
   add(m,star(gold),[.36,.62,.1],[.85,1,.85]);
  } else if(type==='mold-flexible'){
   const sleeve=lathe([[0,-1],[.59,-1],[.69,-.8],[.7,.65],[.58,.78],[.49,.65],[.49,-.72],[0,-.72]],blue);
   sleeve.v=sleeve.v.map(([x,y,z])=>{const bend=Math.max(0,y-.05)*Math.max(0,x)*1.6;return[x+bend,y-bend*.65,z];});add(m,sleeve,[-.12,0,0]);
   add(m,lathe([[0,-.5],[.46,-.5],[.46,.8],[.39,.93],[0,.93]],gold),[-.12,.5,0]);
   add(m,lathe([[.025,0],[.025,.15],[0,.15]],green),[-.12,1.43,0]);
  } else if(type==='mold-parts'){
   // Facing cavity surfaces split along the horizontal X axis.
   for(const side of [-1,1]){const half=cavitySlab(blue,[{x:0,z:0,r:.45,depth:.42}],.75);half.v=half.v.map(([x,y,z])=>[-side*y,x,z]);add(m,half,[side*.95,0,0],[1,.9,.75]);}
   for(const z of [-.52,.52])ball([-.72,.5,z],[.08,.08,.08],blue);
   ball([0,0,0],[.4,.4,.4],gold);
  } else if(type==='mold-multi'){
   const wells=[];for(const x of [-.5,.5])for(const z of [-.57,0,.57])wells.push({x,z,r:.26,depth:.32});
   add(m,cavitySlab(blue,wells),[0,-.3,0],[1,1,1]);
   for(const x of [-.5,.5])for(const z of [-.57,0,.57])add(m,lathe([[0,-.04],[.19,-.04],[.22,.02],[.22,.14],[0,.14]],gold),[x,.5,z]);
  } else if(type==='mold-tooling'){
   // Rigid casting box and positive master; the silicone negative lifts away.
   add(m,box(cream),[-.48,-.57,0],[.72,.09,.74]);
   for(const x of [-1.16,.2])add(m,box(cream),[x,-.3,0],[.045,.21,.74]);
   for(const z of [-.7,.7])add(m,box(cream),[-.48,-.3,z],[.72,.21,.045]);
   add(m,star(gold),[-.48,-.35,0],[.92,2.1,.92]);
   add(m,cavitySlab(blue,[{x:0,z:0,r:.58,depth:.32,star:true}]),[.64,.55,0],[.57,.7,.57]);
  } else if(type==='print-model'){
   // A print bed, gantry, nozzle, and visibly layered wax prototype.
   add(m,box(blue),[0,-.95,0],[.95,.09,.8]);
   for(const x of [-.9,.9])add(m,box(cream),[x,.05,-.6],[.07,1.02,.07]);
   add(m,box(cream),[0,1.03,-.6],[.97,.07,.07]);
   add(m,box(blue),[0,.62,-.4],[.2,.21,.19]);
   add(m,lathe([[.09,.12],[.09,0],[.025,-.18]],gold),[0,.3,-.25]);
   const profile=[[0,-.84]];
   for(let i=0;i<=24;i++){const y=-.82+i*.035,r=.43+.08*Math.cos(i*.18);profile.push([r,y],[r+.012,y+.009]);}
   profile.push([.3,.03],[.3,-.15],[0,-.15]);add(m,lathe(profile,gold),[0,0,.12]);
  }
  else if(type==='figurine'){
   ball([0,-.35,0],[.65,.8,.4],cream);ball([0,.65,0],[.59,.57,.46],cream);
   ball([-.53,.87,0],[.19,.23,.18],cream);ball([.53,.87,0],[.19,.23,.18],cream);
   ball([0,.46,.37],[.42,.29,.22],'#b5ae9f');ball([-.23,.74,.418],[.1,.12,.055],blue);ball([.23,.74,.418],[.1,.12,.055],blue);
   ball([0,.5,.58],[.18,.12,.08],blue);ball([-.68,-.3,.03],[.23,.58,.24],cream);ball([.68,-.3,.03],[.23,.58,.24],cream);
   ball([-.35,-1.02,.15],[.29,.24,.4],cream);ball([.35,-1.02,.15],[.29,.24,.4],cream);
  } else if(type==='candle'){
   for(let y=0;y<3;y++)for(let x=0;x<3;x++)for(let z=0;z<3;z++)ball([(x-1)*.56,(y-1)*.56,(z-1)*.56],[.33,.33,.33],gold);
   add(m,lathe([[.035,0],[.035,.18],[0,.18]],blue),[0,.9,0]);
  } else if(type==='soap'){
   const bar=c=>lathe([[0,-.28],[.68,-.28],[.88,-.23],[.96,-.12],[.96,.1],[.87,.2],[.65,.23],[0,.23]],c);
   add(m,bar(cream),[-.12,-.45,.15],[1,.8,.72]);
   add(m,bar(green),[.12,.02,-.02],[1,.8,.72]);
   // A raised botanical sprig on the top face.
   add(m,box(gold),[.12,.215,-.02],[.025,.018,.35]);
   for(const side of [-1,1])for(let i=0;i<3;i++)ball([.12+side*.14,.225,-.25+i*.2],[.16,.025,.09],gold);
  } else if(type==='planter'){
   add(m,lathe([[0,-.95],[.65,-.95],[.87,.75],[.88,.85],[.71,.85],[.68,.72],[.51,-.75],[0,-.75]],green));
   for(let i=0;i<24;i++){const a=i/24*Math.PI*2;ball([.73*Math.cos(a),-.08,.73*Math.sin(a)],[.045,.71,.045],green);}
  } else if(type==='dice'){
   add(m,ico(blue),[0,.15,0],[1.18,1.18,1.18]);
  } else if(type==='chocolate'){
   add(m,box('#6d4437'),[0,-.2,0],[.9,.11,1.15]);for(let x=0;x<3;x++)for(let z=0;z<4;z++)add(m,box('#845744'),[(x-1)*.58,-.04,(z-1.5)*.55],[.265,.12,.25]);
  } else if(type==='keychain'){
   add(m,lathe([[0,-.18],[.8,-.18],[.87,-.12],[.87,.08],[.8,.15],[0,.15]],gold),[0,-.2,0]);
   add(m,torus(blue,.32,.055),[0,.05,-.98]);
   // Embossed monogram made from raised geometric strokes.
   add(m,box(blue),[-.28,.19,0],[.07,.04,.34]);add(m,box(blue),[.28,.19,0],[.07,.04,.34]);
   ball([-.15,.19,-.08],[.12,.05,.16],blue);ball([.15,.19,-.08],[.12,.05,.16],blue);
  } else if(type==='ring'){
   add(m,torus('#4b9872',.73,.15),[0,-.05,0]);add(m,ico('#4b9872'),[0,.34,-.68],[.37,.45,.37]);
  } else {add(m,ico(gold),[0,.2,0],[1.1,1.1,1.1]);}
  if(type==='mold-flexible')m.v=m.v.map(([x,y,z])=>[x*.83,y*.83-.12,z*.83]);
  return m;
 }
 function mount(canvas,type,interactive=true){const flat=type==='soap'||type==='chocolate'||type==='keychain'||type.startsWith('mold-')&&!['mold-flexible','mold-parts'].includes(type);let shape=model(type),angle=type==='mold-parts'?-.45:flat?.35:-.4,tilt=type==='mold-parts'?.32:flat?.65:.23;let drag=null,moved=false,active=true;const ctx=canvas.getContext('2d');
 const rotate=v=>{let x=v[0]*Math.cos(angle)+v[2]*Math.sin(angle),z=-v[0]*Math.sin(angle)+v[2]*Math.cos(angle);return[x,v[1]*Math.cos(tilt)-z*Math.sin(tilt),v[1]*Math.sin(tilt)+z*Math.cos(tilt)];};
 function draw(){if(!active)return;const rect=canvas.getBoundingClientRect();if(!rect.width||!rect.height)return;const dpr=Math.min(devicePixelRatio||1,2);canvas.width=rect.width*dpr;canvas.height=rect.height*dpr;ctx.scale(dpr,dpr);const w=rect.width,h=rect.height,s=Math.min(w,h)*.315,cx=w/2,cy=h*.5;
  ctx.save();ctx.translate(cx,h*.83);ctx.scale(1,.25);const shadow=ctx.createRadialGradient(0,0,0,0,0,s*1.3);shadow.addColorStop(0,'#20294530');shadow.addColorStop(1,'#20294500');ctx.fillStyle=shadow;ctx.beginPath();ctx.arc(0,0,s*1.3,0,Math.PI*2);ctx.fill();ctx.restore();
  const verts=shape.v.map(rotate);const projected=verts.map(v=>[cx+v[0]*s*5/(5-v[2]),cy-v[1]*s*5/(5-v[2])]);
  const faces=shape.f.map((f,index)=>{const a=verts[f.i[0]],b=verts[f.i[1]],c=verts[f.i[2]],u=b.map((x,i)=>x-a[i]),v=c.map((x,i)=>x-a[i]);const n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];let len=Math.hypot(...n)||1;let light=.55+.45*Math.abs((n[0]*-.45+n[1]*.75+n[2]*.55)/len);return{f,z:(a[2]+b[2]+c[2])/3,light,index};}).sort((a,b)=>a.z-b.z);
  faces.forEach(({f,light,index})=>{const points=f.i.map(i=>projected[i]);const rgb=f.c.match(/\w\w/g).map(x=>Math.min(255,Math.round(parseInt(x,16)*light)));ctx.fillStyle=`rgb(${rgb.join(',')})`;ctx.beginPath();points.forEach((p,i)=>i?ctx.lineTo(...p):ctx.moveTo(...p));ctx.closePath();ctx.fill();ctx.strokeStyle=ctx.fillStyle;ctx.lineWidth=.45;ctx.stroke();
   if(type==='dice'){const a=verts[f.i[0]],b=verts[f.i[1]],c=verts[f.i[2]];const area=(points[1][0]-points[0][0])*(points[2][1]-points[0][1])-(points[1][1]-points[0][1])*(points[2][0]-points[0][0]);if(area< -120&&((a[2]+b[2]+c[2])/3)>.15){ctx.fillStyle='#e6e2d8';ctx.font=`500 ${Math.max(9,s*.17)}px Georgia`;ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText(String(index+1),points.reduce((s,p)=>s+p[0],0)/3,points.reduce((s,p)=>s+p[1],0)/3);}}
  });
 }
 const observer=new ResizeObserver(draw);observer.observe(canvas);if(interactive){canvas.addEventListener('pointerdown',e=>{drag={x:e.clientX,angle};moved=false;canvas.setPointerCapture(e.pointerId);});canvas.addEventListener('pointermove',e=>{if(!drag)return;const diff=e.clientX-drag.x;if(Math.abs(diff)>5)moved=true;angle=drag.angle+diff*.015;draw();});canvas.addEventListener('pointerup',()=>{drag=null;});canvas.addEventListener('click',e=>{if(moved){e.stopImmediatePropagation();moved=false;}},true);}
 draw();return{draw,dispose(){active=false;observer.disconnect();},setType(t){type=t;shape=model(t);draw();}};
 }
 return{mount};
})();
