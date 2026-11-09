/* Offline UI demonstration only. No engine, network, telemetry, or game-save access. */
const art = document.getElementById('art');
const dialog = document.getElementById('dialog');
const toastElement = document.getElementById('toast');
const keys = {menu:'01-main-menu',hud:'02-gameplay-hud',pause:'03-pause',settings:'04-settings',hack:'05-hacking',map:'06-mission-map'};
const defaults = {volume:70,scale:100,contrast:false,motion:matchMedia('(prefers-reduced-motion: reduce)').matches};
let preferences = {...defaults};
let storageAvailable = true;
try { Object.assign(preferences,JSON.parse(localStorage.getItem('netrunner92-ui-preferences') || '{}')); } catch {storageAvailable=false;}
preferences.volume=Math.min(100,Math.max(0,Number(preferences.volume)||0));
preferences.scale=[90,100,110,120].includes(preferences.scale)?preferences.scale:100;
preferences.contrast=!!preferences.contrast; preferences.motion=!!preferences.motion;
let state='menu', stack=[], active=false, completed=false, energy=72, trace=12, pulseRemaining=0;
let hackTimer=null, hackProgress=0, toastTimer=null, tickTime=performance.now();
let lastFocus=null, audio=null, returnFocus=null;
const byId=id=>art.querySelector('#'+id);
const setText=(id,value)=>{const el=byId(id);if(el)el.textContent=value;};
const setAttr=(id,name,value)=>{const el=byId(id);if(el)el.setAttribute(name,value);};

function stopHack(){if(hackTimer){clearInterval(hackTimer);hackTimer=null;}}
function save(){
  try{localStorage.setItem('netrunner92-ui-preferences',JSON.stringify(preferences));storageAvailable=true;}
  catch{storageAvailable=false;}
  applyPreferences();
}
function applyPreferences(){
  document.body.classList.toggle('contrast',preferences.contrast);
  document.body.classList.toggle('reduced',preferences.motion);
  setText('volume-value',preferences.volume+'%');
  setAttr('volume-bar','width',342*preferences.volume/100);
  setAttr('volume-thumb','cx',1000+342*preferences.volume/100);
  const slider=art.querySelector('[role=slider]');if(slider)slider.setAttribute('aria-valuenow',preferences.volume);
  setText('scale-value',preferences.scale+'%');
  for(const [name,value] of [['contrast',preferences.contrast],['motion',preferences.motion]]){
    setText(name+'-value',value?'BE':'KI');setAttr(name+'-thumb','cx',value?1425:1389);
    setAttr(name+'-thumb','fill',value?'#8ce6cf':'#96aab2');
    const el=art.querySelector(`[data-action="${name}"]`);if(el)el.setAttribute('aria-pressed',String(value));
  }
  setText('saved-status',storageAvailable?'HELYBEN MENTVE / OFFLINE ELŐNÉZET':'MENTÉS NEM ELÉRHETŐ / CSAK ERRE A MUNKAMENETRE');
  if(state==='hud'){
    const s=preferences.scale/100;
    const anchors={'Hud-identity':[64,34],Objective:[64,142],Stealth:[1856,34],Vitals:[64,1016],Abilities:[1856,1016],Interaction:[960,847]};
    for(const [name,[x,y]]of Object.entries(anchors)){
      const g=art.querySelector(`[data-layer="${name}"]`);
      if(g)g.setAttribute('transform',`translate(${x} ${y}) scale(${s}) translate(${-x} ${-y})`);
    }
  }
}
function notify(text){
  clearTimeout(toastTimer);toastElement.textContent=text;toastElement.style.display='block';
  toastTimer=setTimeout(()=>toastElement.style.display='none',2800);
}
function sound(){
  if(!preferences.volume)return;
  try{
    audio ||= new (window.AudioContext||window.webkitAudioContext)();
    const oscillator=audio.createOscillator(),gain=audio.createGain();
    oscillator.type='sine';oscillator.frequency.setValueAtTime(520,audio.currentTime);
    gain.gain.setValueAtTime(.025*preferences.volume/100,audio.currentTime);
    gain.gain.exponentialRampToValueAtTime(.001,audio.currentTime+.055);
    oscillator.connect(gain).connect(audio.destination);oscillator.start();oscillator.stop(audio.currentTime+.06);
  }catch{}
}
function render(focus=true){
  art.innerHTML=SCREENS[keys[state]];
  art.classList.remove('animate');void art.offsetWidth;art.classList.add('animate');
  // SVG has a document role in the interactive preview; exported SVGs remain images.
  art.firstElementChild.setAttribute('role','group');
  art.firstElementChild.setAttribute('aria-label',`NET.runner felület: ${state}`);
  art.querySelectorAll('.action').forEach(el=>{
    if((el.closest('[data-layer="Abilities"]')||el.closest('[data-layer="Interaction"]'))&&state!=='hud'){
      el.setAttribute('tabindex','-1');el.style.pointerEvents='none';
    }
  });
  applyPreferences(); updateHud();
  if(focus){const target=art.querySelector(`[data-action="${returnFocus}"]`)||art.querySelector('.action[tabindex="0"]');target?.focus({preventScroll:true});returnFocus=null;}
}
function show(next,{push=true}={}){
  if(state==='hack'&&next!=='hack')stopHack();
  if(push)stack.push({state,focus:document.activeElement?.dataset?.action});
  state=next;render();
}
function back(){
  if(dialog.open){dialog.close();return;}
  if(state==='hud'){show('pause');return;}
  if(state==='menu')return;
  stopHack();
  const prior=stack.pop()||{state:active?'hud':'menu'};
  state=prior.state;returnFocus=prior.focus;render();
}
function modal(title,body,buttons){
  document.getElementById('dialog-title').textContent=title;
  document.getElementById('dialog-content').innerHTML=body;
  const actions=document.getElementById('dialog-actions');actions.replaceChildren();
  for(const [label,callback,secondary]of buttons){const button=document.createElement('button');button.textContent=label;if(secondary)button.className='secondary';button.onclick=()=>{dialog.close();callback?.();};actions.append(button);}
  lastFocus=document.activeElement;dialog.showModal();actions.firstElementChild?.focus();
}
dialog.addEventListener('close',()=>{if(lastFocus?.isConnected)lastFocus.focus();});
function updateHud(){
  setText('energy-value',Math.round(energy).toString());setAttr('energy-bar','width',160*energy/100);
  setText('trace-value',trace+'%');setAttr('trace-bar','width',280*trace/100);
  setText('stealth',trace>50?'KERESÉS ALATT':'REJTETT');setAttr('stealth','fill',trace>50?'#edc181':'#8ce6cf');
  setAttr('trace-bar','fill',trace>50?'#edc181':'#8ce6cf');
  if(completed){setText('objective','Érd el az északi kijáratot.');setText('pause-objective','02 / Érd el az északi kijáratot.');setText('distance','KIJÁRATI SZINT / 64 M');setText('interaction-title','Archívum elérhető');}
  const cool=Math.max(0,Math.ceil(pulseRemaining));setText('pulse-label',cool?`TÖLTÉS / ${cool} S`:'IMPULZUS');
  if(state==='hud'&&cool&&!preferences.motion){
    const g=art.querySelector('[data-layer="Reticle"]');g?.setAttribute('opacity',String(.5+.5*Math.sin(performance.now()/300)**2));
  }
}
function selectNode(which){
  const data={archive:['Központi archívum','01 / ELSŐDLEGES CÉL','Érd el a karbantartási terminált,','majd nyiss hozzáférést az archívumhoz.','38 M','MÉRSÉKELT'],entry:['Belépési pont','00 / BIZTONSÁGOS ZÓNA','Az operátor kiindulási helye.','A kijelölt útvonal innen indul.','0 M','ALACSONY'],patrol:['K7 járőr','02 / MOZGÓ FENYEGETÉS','Ipari felügyeleti egység az átjárónál.','A térképen jelzett hely bemutatóadat.','21 M','MAGAS']}[which];
  ['node-title','node-type','node-line1','node-line2','node-distance','node-risk'].forEach((id,i)=>setText(id,data[i]));
}
function connect(){
  if(hackTimer)return;
  hackProgress=0;setText('hack-status','ÁTJÁTSZÓ HITELESÍTÉSE...');
  const button=art.querySelector('[data-action="connect"]');button?.setAttribute('aria-disabled','true');
  hackTimer=setInterval(()=>{
    hackProgress=Math.min(100,hackProgress+4);setText('hack-percent',String(hackProgress).padStart(2,'0')+'%');
    setText('hack-status',hackProgress<40?'ÁTJÁTSZÓ HITELESÍTÉSE...':hackProgress<80?'HOZZÁFÉRÉSI KULCS ÁTVITELE...':'BIZTONSÁGOS CSATORNA MEGNYITÁSA...');
    for(let i=0;i<48;i++)setAttr('hack-segment-'+i,'fill',i<48*hackProgress/100?'#8ce6cf':'#34464d');
    if(hackProgress===100){stopHack();completed=true;trace=Math.min(100,trace+8);stack=[];state='hud';render();notify('Hozzáférés engedélyezve. Új cél: északi kijárat.');}
  },80);
}
function action(name){
  sound();
  if(name==='start'){active=true;completed=false;energy=72;trace=12;pulseRemaining=0;stack=[];state='hud';render();notify('Bemutató művelet elindítva. E: terminál • Q: impulzus • Esc: szünet');}
  else if(name==='pause'&&state==='hud')show('pause');
  else if(name==='resume'){stack=[];state=active?'hud':'menu';render();}
  else if(name==='settings'||name==='map')show(name);
  else if(name==='hack'&&state==='hud'){if(completed)notify('Az archívum már elérhető.');else{hackProgress=0;show('hack');}}
  else if(name==='back')back();
  else if(name==='connect'&&state==='hack')connect();
  else if(name==='pulse'&&state==='hud'){
    if(pulseRemaining>0)notify('Az impulzus még töltődik.');
    else if(energy<20)notify('Nincs elegendő energia.');
    else{energy-=20;pulseRemaining=6;updateHud();notify('Impulzus aktív. A bemutatóban egy K7 jel található: 21 m.');}
  }
  else if(name==='scale-up'||name==='scale-down'){
    preferences.scale=Math.min(120,Math.max(90,preferences.scale+(name==='scale-up'?10:-10)));save();
  }else if(name==='contrast'||name==='motion'){preferences[name]=!preferences[name];save();}
  else if(name.startsWith('node-'))selectNode(name.slice(5));
  else if(name==='home-confirm')modal('Visszatérsz a főmenübe?','Az előnézeti művelet állapota törlődik. A felületbeállításaid megmaradnak.',[
    ['Művelet folytatása',null],['Vissza a főmenübe',()=>{stopHack();stack=[];active=false;completed=false;state='menu';render();},true]]);
  else if(name==='about')modal('Önálló felület, szabadon továbbépíthető.',
    '<p>Ez a NET.runner / 92 kattintható felületterve, helyi bemutatóadatokkal. Nem kapcsolódik a játékhoz.</p><p><kbd>E</kbd> terminál · <kbd>Q</kbd> impulzus · <kbd>Tab</kbd> térkép a HUD-on · <kbd>M</kbd> térkép · <kbd>Esc</kbd> szünet / vissza · <kbd>V</kbd> rejtettségi állapot.</p><p>Egérrel, illetve tabulátorral és Enterrel is kezelhető. A képernyők SVG-ként, a rétegek átlátszó PNG-ként és a teljes terv Blender-forrásként is mellékelve vannak.</p>',[['Értem',null]]);
}
art.addEventListener('click',event=>{const target=event.target.closest('[data-action]');if(target&&target.dataset.action!=='volume')action(target.dataset.action);});
let drag=false;
function volumeAt(event){const box=art.firstElementChild.getBoundingClientRect();const x=(event.clientX-box.left)*1920/box.width;preferences.volume=Math.round(Math.max(0,Math.min(100,(x-1000)/342*100)));save();}
art.addEventListener('pointerdown',event=>{if(event.target.closest('[data-action="volume"]')){drag=true;volumeAt(event);art.setPointerCapture(event.pointerId);}});
art.addEventListener('pointermove',event=>{if(drag)volumeAt(event);});
art.addEventListener('pointerup',()=>drag=false);art.addEventListener('pointercancel',()=>drag=false);
document.addEventListener('keydown',event=>{
  if(dialog.open)return;
  const el=document.activeElement;
  if(el?.getAttribute('role')==='slider'&&['ArrowLeft','ArrowRight','Home','End'].includes(event.key)){
    event.preventDefault();preferences.volume=event.key==='Home'?0:event.key==='End'?100:Math.min(100,Math.max(0,preferences.volume+(event.key==='ArrowRight'?5:-5)));save();return;
  }
  if((event.key==='Enter'||event.key===' ')&&el?.dataset?.action){event.preventDefault();action(el.dataset.action);return;}
  if(event.key==='Escape'){event.preventDefault();back();return;}
  // Tab remains normal focus navigation until focus explicitly leaves the HUD controls.
  if(state==='hud'&&event.key==='Tab'&&!event.shiftKey){event.preventDefault();show('map');return;}
  if(state==='hud'&&event.key.toLowerCase()==='e'){event.preventDefault();action('hack');}
  else if(state==='hud'&&event.key.toLowerCase()==='q')action('pulse');
  else if(state==='hud'&&event.key.toLowerCase()==='v'){trace=trace>50?12:68;updateHud();notify(trace>50?'Állapotminta: keresés alatt.':'Állapotminta: rejtett.');}
  else if(event.key.toLowerCase()==='m'){event.preventDefault();if(state==='map')back();else if(state==='hud')show('map');}
});
document.addEventListener('visibilitychange',()=>{if(document.hidden&&active&&state==='hud')show('pause');});
setInterval(()=>{const now=performance.now(),dt=Math.min(.5,(now-tickTime)/1000);tickTime=now;if(state==='hud'&&!dialog.open){energy=Math.min(100,energy+dt*1.2);pulseRemaining=Math.max(0,pulseRemaining-dt);updateHud();}},100);

const params=new URLSearchParams(location.search);
if(params.has('screen')){
  const requested=params.get('screen');if(Object.hasOwn(keys,requested)){state=requested;active=state!=='menu';}
}
render(!params.has('capture'));
if(params.has('capture')){document.querySelectorAll(':focus').forEach(el=>el.blur());document.body.dataset.capture='true';}

async function verify(){
  const checks=[];const check=(name,ok)=>checks.push({name,passed:!!ok});
  try{
    check('Six authored SVG screens',Object.keys(SCREENS).length===6);
    action('start');check('Start enters HUD',state==='hud'&&active);
    const before=energy;action('pulse');check('Pulse consumes energy',energy===before-20);
    action('pulse');check('Cooldown prevents repeated consumption',energy===before-20);
    document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));check('Escape pauses',state==='pause');
    const frozenEnergy=energy,frozenCooldown=pulseRemaining;await new Promise(resolve=>setTimeout(resolve,250));check('Pause freezes telemetry and cooldown',energy===frozenEnergy&&pulseRemaining===frozenCooldown);
    action('settings');check('Settings opens over pause',state==='settings');
    const original=preferences.scale;action('scale-up');check('HUD scale is adjustable',preferences.scale===Math.min(120,original+10));
    action('contrast');check('Contrast applied',document.body.classList.contains('contrast')===preferences.contrast);
    action('motion');check('Reduced motion applied',document.body.classList.contains('reduced')===preferences.motion);
    const vol=art.querySelector('[role=slider]');vol.focus();document.dispatchEvent(new KeyboardEvent('keydown',{key:'End',bubbles:true}));check('Keyboard slider works',preferences.volume===100);
    action('back');check('Settings returns to pause',state==='pause');action('resume');check('Resume returns to HUD',state==='hud');
    document.dispatchEvent(new KeyboardEvent('keydown',{key:'Tab',bubbles:true}));check('HUD Tab opens map',state==='map');action('back');
    action('map');action('node-patrol');check('Map node selection',byId('node-title').textContent==='K7 járőr');action('back');check('Map restores HUD',state==='hud');
    action('hack');action('connect');check('Hacking starts',!!hackTimer);action('back');check('Cancel stops hacking timer',state==='hud'&&!hackTimer);
    action('hack');action('connect');await new Promise(resolve=>setTimeout(resolve,2200));
    check('Hacking completes and updates objective',completed&&state==='hud'&&byId('objective').textContent.includes('kijáratot'));
    action('pause');action('home-confirm');check('Home confirmation is a real modal',dialog.open);dialog.querySelectorAll('button')[1].click();check('Home clears demo state',state==='menu'&&!active);
    preferences={...defaults};save();render(false);
    check('No duplicate SVG IDs',new Set([...art.querySelectorAll('[id]')].map(el=>el.id)).size===art.querySelectorAll('[id]').length);
  }catch(error){checks.push({name:'Unexpected runtime error',passed:false,error:String(error)});}
  document.getElementById('verification').textContent=JSON.stringify({passed:checks.every(c=>c.passed),checks},null,2);
  document.body.dataset.verification=checks.every(c=>c.passed)?'passed':'failed';
}
if(params.has('verify'))verify();
