"""Rasterize the local vector artwork and exercise the offline prototype with headless Chrome."""
from pathlib import Path
from html import unescape
import subprocess
import tempfile
import re
import json
import base64
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
CHROME=Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
if not CHROME.exists():CHROME=Path('C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe')


def browser(url,extra=None,budget=4000):
    # Isolated temporary profile. No connection to an existing user browser.
    with tempfile.TemporaryDirectory(prefix='netrunner-ui-') as profile:
        result=subprocess.run([str(CHROME),'--headless','--disable-gpu','--no-first-run',
            '--disable-extensions','--disable-background-networking','--no-default-browser-check',
            '--hide-scrollbars',f'--user-data-dir={profile}',f'--virtual-time-budget={budget}',
            '--window-size=1920,1080',*(extra or []),url],capture_output=True,timeout=80)
        if result.returncode:raise RuntimeError(result.stderr.decode('utf-8',errors='replace')[-2000:])
        return result.stdout.decode('utf-8',errors='replace')


def exports():
    assets=[]
    for kind in ['screens','components']:
        for path in sorted((ROOT/kind).glob('*.svg')):
            xml=ET.fromstring(path.read_text(encoding='utf-8'));w=int(xml.attrib['width']);h=int(xml.attrib['height'])
            scales=[1,2] if kind=='components' else [1]
            for scale in scales:
                assets.append(dict(name=path.stem,kind=kind,width=w,height=h,scale=scale,svg=path.read_text(encoding='utf-8')))
    page=ROOT/'review'/'rasterize.html'
    page.write_text('''<!doctype html><meta charset="utf-8"><pre id="result"></pre><script>
const assets='''+json.dumps(assets,ensure_ascii=False)+''';
(async()=>{const out=[];for(const a of assets){
const img=new Image();const url=URL.createObjectURL(new Blob([a.svg],{type:'image/svg+xml;charset=utf-8'}));
await new Promise((resolve,reject)=>{img.onload=resolve;img.onerror=reject;img.src=url;});
const canvas=document.createElement('canvas');canvas.width=a.width*a.scale;canvas.height=a.height*a.scale;
canvas.getContext('2d').drawImage(img,0,0,canvas.width,canvas.height);URL.revokeObjectURL(url);
const doc=new DOMParser().parseFromString(a.svg,'image/svg+xml');const ctx=canvas.getContext('2d');
const metrics=[...doc.querySelectorAll('text')].map(t=>{ctx.font=`${t.getAttribute('font-weight')} ${t.getAttribute('font-size')}px ${t.getAttribute('style')?.includes('Consolas')?'Consolas':'Segoe UI'}`;
ctx.letterSpacing=t.getAttribute('letter-spacing')+'px';const m=ctx.measureText(t.textContent);
return {text:t.textContent,width:m.actualBoundingBoxLeft+m.actualBoundingBoxRight,ascent:m.actualBoundingBoxAscent,descent:m.actualBoundingBoxDescent};});
out.push({name:a.name,kind:a.kind,scale:a.scale,png:canvas.toDataURL('image/png').split(',')[1],metrics});}
document.getElementById('result').textContent=JSON.stringify(out);document.body.dataset.done='true';})();
</script>''',encoding='utf-8')
    dom=browser(page.as_uri(),['--dump-dom'],budget=15000)
    match=re.search(r'<pre id="result">(.*?)</pre>',dom,re.S)
    if not match or not match.group(1):raise RuntimeError('Browser produced no raster export payload.')
    outputs=json.loads(unescape(match.group(1)))
    for item in outputs:
        suffix='@2x' if item['scale']==2 else ''
        (ROOT/'png'/item['kind']/(item['name']+suffix+'.png')).write_bytes(base64.b64decode(item['png']))
    (ROOT/'source'/'text-metrics.json').write_text(json.dumps({item['name']:item['metrics'] for item in outputs},ensure_ascii=False,indent=2),encoding='utf-8')
    page.unlink()
    return outputs


def test():
    dom=browser((ROOT/'NET-runner-92-preview.html').as_uri()+'?verify=1',['--dump-dom'],budget=12000)
    match=re.search(r'<pre id="verification">(.*?)</pre>',dom,re.S)
    if not match or not match.group(1):raise RuntimeError('Prototype verification did not finish.')
    report=json.loads(unescape(match.group(1)))
    (ROOT/'review'/'interaction-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    if not report['passed']:raise RuntimeError(json.dumps(report,ensure_ascii=False))
    return report


def contact_sheet():
    sheet=Image.new('RGB',(1600,1548),'#090f13');draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
    title=ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',30)
    draw.text((40,24),'NET.runner / 92',font=title,fill='#edf3ef')
    draw.text((40,72),'Önálló, szerkeszthető játékfelület  /  1920 × 1080',font=font,fill='#96aab2')
    labels=['01  Főmenü','02  Játék közbeni HUD','03  Szünet','04  Beállítások','05  Hozzáférési protokoll','06  Műveleti térkép']
    for index,(path,label) in enumerate(zip(sorted((ROOT/'png'/'screens').glob('*.png')),labels)):
        x=40+(index%2)*780;y=140+(index//2)*466
        image=Image.open(path).convert('RGB');image.thumbnail((740,417),Image.Resampling.LANCZOS)
        sheet.paste(image,(x,y));draw.rectangle((x,y,x+739,y+416),outline='#34464d')
        draw.text((x,y+425),label,font=font,fill='#96aab2')
    sheet.save(ROOT/'review'/'interface-overview.jpg',quality=94)


def audit():
    checks=[]
    for path in list((ROOT/'screens').glob('*.svg'))+list((ROOT/'components').glob('*.svg')):
        tree=ET.fromstring(path.read_text(encoding='utf-8'))
        ids=[node.attrib['id'] for node in tree.iter() if 'id' in node.attrib]
        checks.append(dict(file=str(path.relative_to(ROOT)),validXml=True,uniqueIds=len(ids)==len(set(ids))))
    alpha=[]
    for path in (ROOT/'png'/'components').glob('*.png'):
        im=Image.open(path)
        transparent=im.mode=='RGBA' and im.getchannel('A').getextrema()[0]==0
        alpha.append(dict(file=path.name,size=im.size,transparent=transparent))
    report=dict(vectorChecks=checks,pngAlphaChecks=alpha,passed=all(c['uniqueIds'] for c in checks) and all(c['transparent'] for c in alpha))
    (ROOT/'review'/'asset-checks.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if not report['passed']:raise RuntimeError('Asset validation failed; inspect review/asset-checks.json')
    return report


if __name__=='__main__':
    import sys
    mode=sys.argv[1] if len(sys.argv)>1 else 'all'
    if mode in ['all','export']:
        result=exports();print(f'Raster exports: {len(result)}');contact_sheet();audit()
    if mode in ['all','test']:
        report=test();print(f'Interaction checks: {len(report["checks"])} passed.')
