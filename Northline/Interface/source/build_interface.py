"""Author the portable NET.runner / 92 interface. Python standard library only."""
from pathlib import Path
from html import escape
import json
import math

ROOT = Path(__file__).resolve().parents[1]
C = dict(bg='#090f13', panel='#0e191f', raised='#15242b', line='#34464d',
         muted='#96aab2', white='#edf3ef', teal='#8ce6cf', amber='#edc181', red='#ef9189')
LAYOUTS = {}


class Board:
    def __init__(self, name, width=1920, height=1080):
        self.name, self.w, self.h = name, width, height
        self.items = []
        self.layer = 'Interface'

    def add(self, kind, **kwargs):
        self.items.append(dict(kind=kind, layer=self.layer, **kwargs))

    def rect(self, x, y, w, h, fill, stroke=None, opacity=1, id=None):
        self.add('rect', x=x, y=y, w=w, h=h, fill=fill, stroke=stroke, opacity=opacity, id=id)

    def line(self, x1, y1, x2, y2, color=None, width=1, **kw):
        self.add('line', x1=x1, y1=y1, x2=x2, y2=y2, stroke=color or C['line'], width=width, **kw)

    def poly(self, points, fill='none', stroke=None, width=1, **kw):
        self.add('poly', points=points, fill=fill, stroke=stroke, width=width, **kw)

    def circle(self, x, y, r, fill='none', stroke=None, width=1, **kw):
        self.add('circle', x=x, y=y, r=r, fill=fill, stroke=stroke, width=width, **kw)

    def text(self, x, y, value, size=18, color=None, weight=400, spacing=0, mono=False, **kw):
        self.add('text', x=x, y=y, value=value, size=size, fill=color or C['white'], weight=weight,
                 spacing=spacing, mono=mono, **kw)

    def group(self, action, x, y, w, h, label, **kw):
        self.add('begin', action=action, label=label, **kw)
        self.rect(x, y, w, h, '#ffffff', opacity=0, id=None)

    def end(self):
        self.add('end')

    def button(self, x, y, w, title, action, primary=False, sub=None, number=None):
        h = 72 if primary else 62
        self.group(action, x, y, w, h, title)
        self.rect(x, y, w, h, C['teal'] if primary else C['panel'], None if primary else C['line'])
        self.text(x+24, y+(44 if primary else 38), title, 20 if primary else 18,
                  C['bg'] if primary else C['white'], 600, .7)
        self.line(x+w-52, y+h/2, x+w-28, y+h/2, C['bg'] if primary else C['teal'], 2)
        self.poly([(x+w-36, y+h/2-7),(x+w-28,y+h/2),(x+w-36,y+h/2+7)],
                  stroke=C['bg'] if primary else C['teal'], width=2)
        self.end()

    def key(self, x, y, label, w=32):
        self.rect(x,y,w,30,C['raised'],C['line'])
        self.text(x+w/2,y+20,label,13,C['white'],500,mono=True,anchor='middle')

    def icon(self, name, x, y, s=24, color=None):
        color=color or C['teal']
        def p(points, close=False):
            pts=[(x+a*s,y+b*s) for a,b in points]
            if close: pts.append(pts[0])
            self.poly(pts,stroke=color,width=1.6)
        if name=='health': p([(0.4,.1),(.6,.1),(.6,.4),(.9,.4),(.9,.6),(.6,.6),(.6,.9),(.4,.9),(.4,.6),(.1,.6),(.1,.4),(.4,.4)],True)
        elif name=='energy': p([(.6,0),(.18,.56),(.48,.56),(.38,1),(.86,.4),(.55,.4)],True)
        elif name=='eye':
            p([(0,.5),(.25,.2),(.7,.2),(1,.5),(.7,.8),(.25,.8)],True)
            self.circle(x+s*.5,y+s*.5,s*.12,color)
        elif name=='pulse':
            p([(0,.55),(.25,.55),(.4,.15),(.6,.85),(.75,.55),(1,.55)])
        elif name=='lock':
            p([(.25,.42),(.25,.2),(.4,.06),(.6,.06),(.75,.2),(.75,.42)])
            self.rect(x+s*.12,y+s*.42,s*.76,s*.5,'none',color)
            self.line(x+s*.5,y+s*.58,x+s*.5,y+s*.76,color,1.6)
        elif name=='map': p([(0,.18),(.3,.04),(.65,.2),(1,.04),(1,.82),(.65,.96),(.3,.8),(0,.96)],True)
        elif name=='arrow': p([(.2,.2),(.8,.2),(.8,.8)]); p([(.8,.2),(.12,.88)])
        elif name=='check': p([(.08,.5),(.38,.8),(.94,.16)])
        elif name=='gear':
            self.circle(x+s*.5,y+s*.5,s*.32,'none',color,1.6)
            self.circle(x+s*.5,y+s*.5,s*.12,'none',color,1.6)
            for a in range(0,360,45):
                a=math.radians(a)
                self.line(x+s*(.5+.34*math.cos(a)),y+s*(.5+.34*math.sin(a)),x+s*(.5+.48*math.cos(a)),y+s*(.5+.48*math.sin(a)),color,1.6)
        elif name=='target':
            self.circle(x+s*.5,y+s*.5,s*.31,'none',color,1.6)
            self.circle(x+s*.5,y+s*.5,s*.08,color)
            for a,b,c,d in [(0,.5,.2,.5),(.8,.5,1,.5),(.5,0,.5,.2),(.5,.8,.5,1)]:
                self.line(x+a*s,y+b*s,x+c*s,y+d*s,color,1.6)
        elif name=='exit': p([(.6,0),(.12,0),(.12,1),(.6,1)]);p([(.45,.5),(1,.5),(.75,.25)]);p([(1,.5),(.75,.75)])


def svg(board):
    out=[f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="{board.w}" height="{board.h}" viewBox="0 0 {board.w} {board.h}" role="img" aria-label="NET.runner 92 — {board.name}">',
         '<style>text{font-family:"Segoe UI",Arial,sans-serif} .action{cursor:pointer;outline:none}.action:hover>rect:not(:first-child),.action:focus>rect:not(:first-child){filter:brightness(1.22);stroke:#edf3ef;stroke-width:2}.action:focus>rect:first-child{stroke:#edf3ef;stroke-width:2;fill:none}</style>']
    layer=None
    layer_counts={}
    for i in board.items:
        if i['layer']!=layer:
            if layer is not None: out.append('</g>')
            layer=i['layer']
            layer_counts[layer]=layer_counts.get(layer,0)+1
            out.append(f'<g id="layer-{escape(layer)}-{layer_counts[layer]}" data-layer="{escape(layer)}">')
        k=i['kind']
        ident=f' id="{i["id"]}"' if i.get('id') else ''
        opacity=f' opacity="{i["opacity"]}"' if i.get('opacity',1)!=1 else ''
        st=f' stroke="{i["stroke"]}" stroke-width="{i.get("width",1)}"' if i.get('stroke') else ''
        if k=='rect': out.append(f'<rect{ident} x="{i["x"]}" y="{i["y"]}" width="{i["w"]}" height="{i["h"]}" fill="{i["fill"]}"{st}{opacity}/>')
        elif k=='line': out.append(f'<line{ident} x1="{i["x1"]}" y1="{i["y1"]}" x2="{i["x2"]}" y2="{i["y2"]}"{st}{opacity}/>')
        elif k=='poly': out.append(f'<polyline{ident} points="'+ ' '.join(f'{x},{y}' for x,y in i['points'])+f'" fill="{i["fill"]}"{st}{opacity} stroke-linejoin="miter"/>')
        elif k=='circle': out.append(f'<circle{ident} cx="{i["x"]}" cy="{i["y"]}" r="{i["r"]}" fill="{i["fill"]}"{st}{opacity}/>')
        elif k=='text':
            font=' font-family="Consolas,monospace" style="font-family:Consolas,monospace"' if i['mono'] else ''
            out.append(f'<text{ident} x="{i["x"]}" y="{i["y"]}" font-size="{i["size"]}" fill="{i["fill"]}" font-weight="{i["weight"]}" letter-spacing="{i["spacing"]}" text-anchor="{i.get("anchor","start")}"{font}{opacity}>{escape(i["value"])}</text>')
        elif k=='begin':
            role=i.get('role','button')
            extra=f' aria-valuemin="0" aria-valuemax="100" aria-valuenow="{i.get("value",70)}"' if role=='slider' else ''
            out.append(f'<g class="action" data-action="{i["action"]}" role="{role}" tabindex="0" aria-label="{escape(i["label"])}"{extra}>')
        elif k=='end': out.append('</g>')
    if layer is not None: out.append('</g>')
    out.append('</svg>')
    return '\n'.join(out)


def brand(b,x=64,y=58):
    b.poly([(x,y-18),(x+14,y-26),(x+28,y-18),(x+28,y),(x+14,y+8),(x,y),(x,y-18)],stroke=C['teal'],width=2)
    b.poly([(x+7,y-11),(x+14,y-16),(x+21,y-11),(x+21,y-3),(x+14,y+1)],stroke=C['teal'],width=1.5)
    b.text(x+45,y,'NET.runner',23,weight=600,spacing=1)
    b.text(x+193,y,'/ 92',20,C['teal'],mono=True)


def chrome(b, section):
    b.layer='Identity'
    brand(b)
    b.text(64,99,section,11,C['muted'],600,2.4)
    b.circle(1623,53,3,C['teal'])
    b.text(1640,58,'HELYI KAPCSOLAT',12,C['muted'],500,1.2)
    b.text(1856,98,'NR—OS  /  V.01',11,C['muted'],mono=True,anchor='end')
    b.line(64,119,1856,119)
    b.line(64,1018,1856,1018)
    b.text(64,1049,'NODE_92  /  TITKOSÍTOTT CSATORNA',11,C['muted'],mono=True,spacing=1)
    b.text(1856,1049,'INTERAKTÍV FELÜLETTERV  •  2026',11,C['muted'],mono=True,anchor='end')


def environment(b):
    """Original vector staging background; not a copy of the game's warehouse."""
    b.layer='Context-background'
    b.rect(0,0,1920,1080,C['bg'])
    # Deliberately quiet, one-point-perspective industrial corridor.
    b.poly([(0,0),(1920,0),(1260,350),(740,350),(0,0)],'#101b21')
    b.poly([(0,1080),(1920,1080),(1260,714),(740,714),(0,1080)],'#142127')
    b.rect(740,350,520,364,'#1b2c32')
    b.poly([(0,0),(740,350),(740,714),(0,1080),(0,0)],'#132127')
    b.poly([(1920,0),(1260,350),(1260,714),(1920,1080),(1920,0)],'#17282e')
    for x in [-800,-300,200,700,1300,1800,2300,2800]:
        b.line(1000,520,x,1080,'#26373b')
    for y in [742,785,845,933,1055]: b.line(0,y,1920,y,'#25353a')
    for x,y,w,h in [(160,84,140,864),(467,231,79,574),(645,315,37,427),
                     (1630,84,140,864),(1380,231,79,574),(1298,315,37,427)]:
        b.rect(x,y,w,h,'#0c181e', '#2c3c42')
        b.rect(x+9,y+7,4,h-10,'#34484d')
    for x in [560,1350]:
        b.poly([(x,0),(x+20,0),(1035+(x-960)*.2,350),(1028+(x-960)*.2,350)],'#6d8b8b')
    b.rect(828,390,344,322,'#0b171c','#405259')
    b.rect(843,406,314,305,'#16272c')
    b.line(1000,406,1000,711,'#3b5257',2)
    b.line(864,412,1136,412,C['teal'],3)
    b.text(1000,455,'ARCHÍVUM  /  B–07',14,'#a3b8b9',600,1,anchor='middle')
    b.poly([(936,700),(1000,666),(1064,700)],stroke='#7e8977',width=3)
    b.rect(1180,480,46,83,'#071115','#415b60')
    b.rect(1186,487,34,39,'#38615d')
    b.line(1192,496,1215,496,C['teal'],2)
    b.line(1192,503,1207,503,'#91b5ac',1)
    b.rect(57,590,170,185,'#1b292f','#3c4a4f')
    b.rect(65,600,154,6,'#3d4a4d')
    for y in [630,644,658]: b.line(85,y,200,y,'#37494e')
    b.rect(1490,610,290,150,'#18282d','#3b4b50')
    b.rect(1530,540,195,69,'#233137','#3b4b50')
    b.line(1500,625,1770,625,'#5e6a62',3)
    # Dark peripheral framing keeps the typography and status marks foremost.
    b.rect(0,0,1920,150,C['bg'],opacity=.42)
    b.rect(0,880,1920,200,C['bg'],opacity=.5)


def floorplan(b,x,y,scale=1,interactive=False):
    b.layer='Floorplan'
    def rect(a,c,w,h): b.rect(x+a*scale,y+c*scale,w*scale,h*scale,'#142329','#365057')
    for a,c,w,h in [(0,55,176,140),(0,226,176,150),(213,0,210,195),(213,226,210,150),
                     (460,0,152,135),(460,166,152,210),(0,414,268,118),(305,414,307,118)]: rect(a,c,w,h)
    for a,c,w,h in [(176,118,37,26),(330,195,28,31),(423,270,37,26),(108,376,25,38),(475,376,25,38)]:
        b.rect(x+a*scale,y+c*scale,w*scale,h*scale,'#253e43')
    for a,c,v in [(19,85,'SKLAD 01'),(235,30,'ARCHÍVUM'),(477,31,'SÍŤ'),(19,257,'SERVIS'),
                  (231,256,'JÁDRO'),(477,196,'PŘÍSTUP'),(19,444,'DOK 01'),(324,444,'VÝSTUP')]:
        # Hungarian labels on the final board.
        v={'SKLAD 01':'RAKTÁR 01','SÍŤ':'HÁLÓZAT','PŘÍSTUP':'ÁTJÁRÓ','JÁDRO':'MAG','VÝSTUP':'KIJÁRAT'}.get(v,v)
        b.text(x+a*scale,y+c*scale,v,10*scale,C['muted'],mono=True,spacing=.6*scale)
    route=[(116,470),(116,300),(320,300),(320,116)]
    b.poly([(x+a*scale,y+c*scale) for a,c in route],stroke=C['teal'],width=2*scale)
    for a,c in [(116,300),(320,300)]:
        b.circle(x+a*scale,y+c*scale,4*scale,C['teal'])
    b.circle(x+116*scale,y+470*scale,13*scale,'#163b36',C['teal'],1.5*scale)
    b.poly([(x+116*scale,y+461*scale),(x+122*scale,y+477*scale),(x+116*scale,y+474*scale),(x+110*scale,y+477*scale),(x+116*scale,y+461*scale)],C['teal'])
    b.circle(x+320*scale,y+116*scale,20*scale,'#183c37',C['teal'],1.5*scale)
    b.icon('target',x+308*scale,y+104*scale,24*scale)
    b.circle(x+535*scale,y+291*scale,7*scale,C['amber'])
    b.line(x+530*scale,y+286*scale,x+540*scale,y+296*scale,C['bg'],1.4*scale)
    if interactive:
        for a,c,label,action in [(320,116,'Archívum','node-archive'),(116,470,'Belépési pont','node-entry'),(535,291,'K7 járőr','node-patrol')]:
            b.group(action,x+(a-28)*scale,y+(c-28)*scale,56*scale,56*scale,label)
            b.end()


def main_menu():
    b=Board('01-main-menu')
    environment(b)
    b.layer='Main-menu'
    b.rect(0,0,1920,1080,C['bg'],opacity=.89)
    chrome(b,'MŰVELETI TERMINÁL')
    b.layer='Main-menu'
    b.text(120,236,'MŰVELET  /  001',12,C['teal'],600,2)
    b.text(116,331,'LÉPJ BE.',82,weight=600,spacing=-2)
    b.text(116,425,'MARADJ REJTVE.',68,weight=300,spacing=-2)
    b.line(120,466,180,466,C['teal'],2)
    b.text(120,505,'Egy hozzáférési pont. Egyetlen nyom nélkül.',19,C['muted'])
    b.button(120,568,532,'MŰVELET INDÍTÁSA','start',True)
    b.button(120,656,532,'MŰVELETI TÉRKÉP','map')
    b.button(120,730,532,'BEÁLLÍTÁSOK','settings')
    b.group('about',120,824,532,44,'A felület használata')
    b.text(120,851,'IRÁNYÍTÁS ÉS A FELÜLET HASZNÁLATA',12,C['muted'],600,1.2)
    b.end()
    b.text(120,950,'NEXUS  /  OPERÁTOR 092',12,C['muted'],mono=True)
    b.layer='Mission-preview'
    b.line(779,179,779,952)
    b.text(844,208,'KÖVETKEZŐ CÉLPONT',11,C['muted'],600,2)
    b.text(844,254,'Csendes behatolás',32,weight=400)
    b.text(844,286,'B–07  /  RAKTÁRKOMPLEXUM',12,C['teal'],mono=True,spacing=1)
    floorplan(b,900,326,1.05)
    b.layer='Mission-preview'
    b.rect(1655,332,123,31,C['raised'],C['line'])
    b.text(1716,353,'B SZINT',11,C['muted'],mono=True,anchor='middle')
    b.text(1624,849,'N',11,C['muted'],mono=True,anchor='middle')
    b.line(1624,862,1624,892,C['muted'])
    b.poly([(1619,870),(1624,862),(1629,870)],stroke=C['muted'])
    b.line(844,919,1780,919)
    b.circle(853,950,4,C['teal'])
    b.text(869,955,'BELÉPÉSI PONT',11,C['muted'],mono=True)
    b.icon('target',1070,939,22)
    b.text(1104,955,'ARCHÍVUM',11,C['muted'],mono=True)
    b.circle(1322,950,5,C['amber'])
    b.text(1338,955,'K7 JÁRŐR',11,C['muted'],mono=True)
    b.text(1780,955,'07 / 18',12,C['white'],mono=True,anchor='end')
    return b


def hud_overlay():
    b=Board('hud-overlay')
    b.layer='Hud-identity'
    brand(b)
    b.text(64,93,'B–07 / RAKTÁRKOMPLEXUM',11,C['muted'],mono=True,spacing=1)
    b.layer='Objective'
    b.rect(64,142,424,124,C['bg'],opacity=.88)
    b.rect(64,142,2,124,C['teal'])
    b.text(86,169,'01 / AKTÍV CÉL',11,C['teal'],600,1.5)
    b.text(86,206,'Érd el a központi terminált.',21,weight=500,id='objective')
    b.icon('target',86,226,17)
    b.text(115,240,'ARCHÍVUMSZINT  /  38 M',11,C['muted'],mono=True,id='distance')
    b.layer='Compass'
    for offset in [-240,-180,-120,-60,0,60,120,180,240]:
        x=960+offset
        b.line(x,49,x,57 if offset%120 else 63,C['muted'] if abs(offset)<180 else C['line'])
    b.text(960,36,'É',12,C['teal'],600,anchor='middle')
    b.text(840,36,'315',10,C['muted'],mono=True,anchor='middle')
    b.text(1080,36,'045',10,C['muted'],mono=True,anchor='middle')
    b.poly([(956,75),(964,75),(960,80),(956,75)],C['teal'])
    b.layer='Stealth'
    b.rect(1536,34,320,94,C['bg'],opacity=.88)
    b.icon('eye',1556,53,23)
    b.text(1595,72,'REJTETT',19,C['teal'],500,1,id='stealth')
    b.text(1836,72,'12%',12,C['muted'],mono=True,anchor='end',id='trace-value')
    b.rect(1556,92,280,2,C['line'])
    b.rect(1556,92,34,2,C['teal'],id='trace-bar')
    b.layer='Reticle'
    b.circle(960,540,1.5,C['white'])
    for a,c,d,e in [(945,540,951,540),(969,540,975,540),(960,525,960,531),(960,549,960,555)]:
        b.line(a,c,d,e,C['white'],1,opacity=.6)
    b.layer='Interaction'
    b.rect(739,759,442,88,C['bg'],opacity=.88)
    b.group('hack',739,759,442,88,'Terminál elérése, E')
    b.key(760,780,'E',36)
    b.text(814,793,'Terminál elérése',21,weight=500,id='interaction-title')
    b.text(814,820,'KARBANTARTÁSI CSOMÓPONT · 2,4 M',10,C['muted'],mono=True)
    b.end()
    b.layer='Vitals'
    b.rect(64,920,388,96,C['bg'],opacity=.9)
    b.icon('health',84,940,21,C['white'])
    b.text(120,958,'ÁLLAPOT',10,C['muted'],600,1)
    b.text(428,960,'100',22,weight=500,mono=True,anchor='end')
    b.rect(84,974,344,3,C['line'])
    b.rect(84,974,344,3,C['white'])
    b.icon('energy',84,990,17)
    b.text(120,1006,'ENERGIA',10,C['muted'],600,1)
    b.rect(208,998,160,3,C['line'])
    b.rect(208,998,115,3,C['teal'],id='energy-bar')
    b.text(428,1008,'72',17,C['teal'],mono=True,anchor='end',id='energy-value')
    b.layer='Abilities'
    for x,key,title,icon,action in [(1292,'Q','IMPULZUS','pulse','pulse'),(1488,'TAB','TÉRKÉP','map','map'),(1684,'ESC','SZÜNET','gear','pause')]:
        b.group(action,x,936,172,80,title)
        b.rect(x,936,172,80,C['bg'],C['line'],opacity=.94)
        b.icon(icon,x+16,951,21)
        b.text(x+156,969,key,11,C['teal'],mono=True,anchor='end')
        b.text(x+16,998,title,11,C['muted'],600,1,id='pulse-label' if action=='pulse' else None)
        b.end()
    return b


def gameplay():
    b=Board('02-gameplay-hud')
    environment(b)
    b.items.extend(hud_overlay().items)
    return b


def modal_base(name):
    b=gameplay()
    b.name=name
    b.layer='Modal-backdrop'
    b.rect(0,0,1920,1080,C['bg'],opacity=.91)
    return b


def pause():
    b=modal_base('03-pause')
    b.layer='Pause-panel'
    b.rect(610,177,700,726,C['panel'],C['line'])
    b.rect(610,177,700,2,C['teal'])
    b.text(662,232,'MŰVELET / 001',11,C['teal'],600,1.8)
    b.text(662,297,'Szünet.',56,weight=300,spacing=-1)
    b.text(662,336,'A kapcsolat biztonságban vár.',17,C['muted'])
    b.line(662,373,1258,373)
    b.button(662,410,596,'MŰVELET FOLYTATÁSA','resume',True)
    b.button(662,496,596,'MŰVELETI TÉRKÉP','map')
    b.button(662,570,596,'BEÁLLÍTÁSOK','settings')
    b.button(662,644,596,'VISSZA A FŐMENÜBE','home-confirm')
    b.text(662,773,'B–07 / ARCHÍVUMSZINT',11,C['muted'],mono=True)
    b.text(662,801,'01 / Érd el a központi terminált.',15,C['white'],id='pause-objective')
    b.key(662,838,'ESC',42)
    b.text(718,859,'Vissza a művelethez',12,C['muted'])
    return b


def settings():
    b=modal_base('04-settings')
    b.layer='Settings-panel'
    b.rect(424,156,1072,768,C['panel'],C['line'])
    b.rect(424,156,1072,2,C['teal'])
    b.text(480,211,'RENDSZER / KONFIGURÁCIÓ',11,C['teal'],600,1.8)
    b.text(480,272,'A te beállításaid.',48,weight=300,spacing=-1)
    b.text(480,309,'A változtatások az előnézetben azonnal láthatók.',16,C['muted'])
    b.line(480,343,1440,343)
    b.text(480,388,'HANG ÉS KÉP',11,C['muted'],600,1.5)
    b.text(480,438,'Hangerő',21)
    b.text(480,465,'Az előnézet válaszjelzéseinek hangereje.',13,C['muted'])
    b.group('volume',1000,407,440,70,'Hangerő',role='slider',value=70)
    b.rect(1000,439,342,3,C['line'])
    b.rect(1000,439,239,3,C['teal'],id='volume-bar')
    b.circle(1239,440.5,7,C['teal'],id='volume-thumb')
    b.text(1440,447,'70%',16,C['white'],mono=True,anchor='end',id='volume-value')
    b.end()
    b.line(480,495,1440,495)
    b.text(480,538,'HUD mérete',21)
    b.text(480,565,'A játék közbeni jelzések mérete.',13,C['muted'])
    for x,action,label in [(1000,'scale-down','−'),(1392,'scale-up','+')]:
        b.group(action,x,514,48,48,'HUD kicsinyítése' if action=='scale-down' else 'HUD nagyítása')
        b.rect(x,514,48,48,C['raised'],C['line'])
        b.text(x+24,546,label,24,C['teal'],anchor='middle')
        b.end()
    b.text(1230,545,'100%',18,C['white'],mono=True,anchor='middle',id='scale-value')
    b.line(480,593,1440,593)
    for y,title,sub,action in [(633,'Erős kontraszt','Erősebb panelhátterek és vonalak.','contrast'),(731,'Mozgáscsökkentés','Az átmenetek és a pásztázás kikapcsolása.','motion')]:
        b.text(480,y,title,21)
        b.text(480,y+27,sub,13,C['muted'])
        b.group(action,1300,y-28,140,52,title)
        b.rect(1374,y-18,66,30,C['raised'],C['line'],id=action+'-track')
        b.circle(1389,y-3,9,C['muted'],id=action+'-thumb')
        b.text(1354,y+3,'KI',12,C['muted'],mono=True,anchor='end',id=action+'-value')
        b.end()
        if y==633:b.line(480,691,1440,691)
    b.line(480,794,1440,794)
    b.button(1120,824,320,'KÉSZ','back',True)
    b.text(480,864,'HELYBEN MENTVE  /  OFFLINE ELŐNÉZET',11,C['muted'],mono=True,id='saved-status')
    return b


def hacking():
    b=modal_base('05-hacking')
    b.layer='Hacking-panel'
    b.rect(380,202,1160,676,C['panel'],C['line'])
    b.rect(380,202,1160,2,C['teal'])
    b.icon('lock',436,245,24)
    b.text(477,266,'HOZZÁFÉRÉSI PROTOKOLL',11,C['teal'],600,1.8)
    b.text(436,330,'Nyisd meg az utat.',48,weight=300,spacing=-1)
    b.text(436,369,'Karbantartási terminál  /  NODE_92 → ARCHIVE_B07',16,C['muted'])
    b.text(1484,265,'AES / 256',12,C['muted'],mono=True,anchor='end')
    b.line(436,403,1484,403)
    for a,z in [(574,960),(960,1346)]:
        b.line(a+34,488,z-34,488,C['line'],2)
    for x,title,sub,icon in [(574,'OPERÁTOR','NEXUS_092','check'),(960,'ÁTJÁTSZÓ','NODE_92','pulse'),(1346,'ARCHÍVUM','B07 / ZÁROLT','lock')]:
        b.circle(x,488,31,C['raised'],C['teal'] if x==574 else C['line'],1.5)
        b.icon(icon,x-12,476,24,C['teal'] if x==574 else C['muted'])
        b.text(x,550,title,12,C['white'],600,1.5,anchor='middle')
        b.text(x,575,sub,10,C['muted'],mono=True,anchor='middle')
    b.text(436,635,'BIZTONSÁGOS CSATORNA FELÉPÍTÉSE',11,C['muted'],mono=True,id='hack-status')
    b.text(1484,635,'00%',18,C['teal'],mono=True,anchor='end',id='hack-percent')
    for i in range(48): b.rect(436+i*22,659,16,10,C['line'],id=f'hack-segment-{i}')
    b.text(436,716,'A kapcsolat felépítése helyi bemutató.',13,C['muted'])
    b.text(436,739,'Nincs tényleges hálózati vagy játékkapcsolat.',13,C['muted'])
    b.button(1120,756,364,'CSATLAKOZÁS','connect',True)
    b.group('back',436,781,400,40,'Megszakítás')
    b.key(436,783,'ESC',42)
    b.text(492,805,'Megszakítás',14,C['muted'])
    b.end()
    return b


def mission_map():
    b=modal_base('06-mission-map')
    b.layer='Map-panel'
    b.rect(180,142,1560,796,C['panel'],C['line'])
    b.rect(180,142,1560,2,C['teal'])
    b.text(236,197,'MŰVELETI TÉRKÉP',11,C['teal'],600,1.8)
    b.text(236,250,'Ismerd az útvonalat.',40,weight=300,spacing=-1)
    b.text(1684,197,'B–07 / B SZINT',12,C['muted'],mono=True,anchor='end')
    b.line(236,282,1684,282)
    floorplan(b,267,321,.98,True)
    b.layer='Map-detail'
    b.line(1040,321,1040,859)
    b.text(1100,350,'KIJELÖLT CSOMÓPONT',11,C['muted'],600,1.5)
    b.text(1100,403,'Központi archívum',29,weight=400,id='node-title')
    b.text(1100,443,'01 / ELSŐDLEGES CÉL',12,C['teal'],mono=True,id='node-type')
    b.text(1100,507,'Érd el a karbantartási terminált,',18,C['muted'],id='node-line1')
    b.text(1100,537,'majd nyiss hozzáférést az archívumhoz.',18,C['muted'],id='node-line2')
    b.line(1100,577,1684,577)
    b.text(1100,616,'TÁVOLSÁG',11,C['muted'],600,1.5)
    b.text(1684,618,'38 M',18,C['white'],mono=True,anchor='end',id='node-distance')
    b.text(1100,661,'KOCKÁZAT',11,C['muted'],600,1.5)
    b.text(1684,663,'MÉRSÉKELT',15,C['amber'],mono=True,anchor='end',id='node-risk')
    b.button(1100,740,584,'VISSZA','back',True)
    b.text(1100,852,'VÁLASSZ CSOMÓPONTOT A TÉRKÉPEN.',11,C['muted'],mono=True)
    return b


def components():
    result={'hud-overlay':hud_overlay()}
    for name,layer,b in [('objective-panel','Objective',hud_overlay()),('vitals-panel','Vitals',hud_overlay()),
                         ('stealth-panel','Stealth',hud_overlay()),('interaction-prompt','Interaction',hud_overlay()),
                         ('ability-bar','Abilities',hud_overlay()),('pause-panel','Pause-panel',pause()),
                         ('settings-panel','Settings-panel',settings()),('hacking-panel','Hacking-panel',hacking()),
                         ('main-menu-panel','Main-menu',main_menu())]:
        q=Board(name)
        q.items=[i for i in b.items if i['layer']==layer and not (i['kind']=='rect' and i.get('w')==1920)]
        bounds={'objective-panel':(64,142,424,124),'vitals-panel':(64,920,388,96),
                'stealth-panel':(1536,34,320,94),'interaction-prompt':(739,759,442,88),
                'ability-bar':(1292,936,564,80),'pause-panel':(610,177,700,726),
                'settings-panel':(424,156,1072,768),'hacking-panel':(380,202,1160,676),
                'main-menu-panel':(110,214,552,750)}
        ox,oy,q.w,q.h=bounds[name]
        # Four transparent pixels retain antialiased strokes and provide usable RGBA cutouts.
        ox-=4;oy-=4;q.w+=8;q.h+=8
        q.source_position=[ox,oy]
        q.items=[dict(i) for i in q.items]
        for i in q.items:
            for key in ['x','x1','x2']:
                if key in i:i[key]-=ox
            for key in ['y','y1','y2']:
                if key in i:i[key]-=oy
            if 'points' in i:i['points']=[(x-ox,y-oy) for x,y in i['points']]
        result[name]=q
    for name in ['health','energy','eye','pulse','lock','map','arrow','check','gear','target','exit']:
        q=Board('icon-'+name,48,48)
        q.icon(name,8,8,32)
        result[q.name]=q
    for style,primary in [('primary',True),('secondary',False)]:
        q=Board('button-'+style,540,80)
        q.button(4,4,532,'FOLYTATÁS','resume',primary)
        result[q.name]=q
    for state,color,text in [('hover','#b2f3e1',C['bg']),('pressed','#66bfa8',C['bg']),('disabled','#25383d','#8a9ba0'),('focus',C['teal'],C['bg'])]:
        q=Board('button-primary-'+state,540,80)
        q.rect(4,4,532,72,color,C['white'] if state=='focus' else None)
        q.text(28,48,'FOLYTATÁS',20,text,600,.7)
        q.icon('arrow',488,28,24,text)
        if state=='focus':q.rect(1,1,538,78,'none',C['white'])
        result[q.name]=q
    q=Board('panel-blank',540,240)
    q.rect(4,4,532,232,C['panel'],C['line']);q.rect(4,4,532,2,C['teal'])
    result[q.name]=q
    q=Board('progress-track',540,24)
    q.rect(4,10,532,4,C['line']);result[q.name]=q
    q=Board('progress-fill',540,24)
    q.rect(4,10,532,4,C['teal']);result[q.name]=q
    return result


def main():
    screens={b.name:b for b in [main_menu(),gameplay(),pause(),settings(),hacking(),mission_map()]}
    comps=components()
    for directory,boards in [('screens',screens),('components',comps)]:
        for name,b in boards.items():
            (ROOT/directory/(name+'.svg')).write_text(svg(b),encoding='utf-8')
    # Readable, engine-independent display lists also drive the Blender authoring source.
    layouts={name:dict(width=b.w,height=b.h,sourcePosition=getattr(b,'source_position',[0,0]),elements=b.items) for name,b in {**screens,**comps}.items()}
    (ROOT/'source'/'layouts.json').write_text(json.dumps(layouts,ensure_ascii=False,indent=2),encoding='utf-8')
    (ROOT/'design-tokens.json').write_text(json.dumps(dict(colors=C,referenceResolution=[1920,1080],
        safeArea=64,spacing=[4,8,12,16,24,32,48,64],fonts=dict(ui='Segoe UI',technical='Consolas',fallback='sans-serif'),
        fontSizes=[10,11,12,13,14,16,18,21,24,32,40,48,68,82],motion=dict(transitionMs=160,reducedMotionSupported=True),
        export=dict(svg='Editable text and shapes; no embedded raster images',png='RGBA, native resolution and 2× component export')),
        ensure_ascii=False,indent=2),encoding='utf-8')
    data=json.dumps({name:svg(b) for name,b in screens.items()},ensure_ascii=False)
    runtime=(ROOT/'source'/'prototype.js').read_text(encoding='utf-8')
    html='''<!doctype html>
<html lang="hu"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>NET.runner / 92 — önálló felület</title>
<style>
*{box-sizing:border-box}html,body{margin:0;width:100%;height:100%;background:#090f13;color:#edf3ef;font-family:'Segoe UI',sans-serif;overflow:hidden}
body{display:grid;place-items:center}#stage{width:min(100vw,177.7778vh);aspect-ratio:16/9;position:relative;isolation:isolate}#art{width:100%;height:100%}#art>svg{display:block;width:100%;height:100%}
#art.animate{animation:appear .16s ease-out}@keyframes appear{from{opacity:.5}to{opacity:1}}
#toast{position:absolute;left:50%;top:15%;transform:translateX(-50%);border:1px solid #8ce6cf;background:#0e191f;padding:14px 24px;font-size:clamp(12px,1.1vw,18px);display:none;max-width:75%;text-align:center;pointer-events:none}
dialog{color:#edf3ef;background:#0e191f;border:1px solid #8ce6cf;max-width:min(680px,90vw);padding:32px;line-height:1.6;box-shadow:0 20px 100px #0008}dialog::backdrop{background:#090f13e8}dialog h2{font-weight:400;margin-top:0}dialog button{cursor:pointer;font:inherit;background:#8ce6cf;color:#090f13;border:1px solid #8ce6cf;padding:12px 18px;margin:16px 8px 0 0}dialog button.secondary{background:transparent;color:#edf3ef}kbd{border:1px solid #34464d;padding:2px 6px}#verification{display:none}
body.contrast #art g[data-layer] rect[opacity]{opacity:1}body.contrast #art line{stroke-width:2}body.contrast #art text{paint-order:stroke;stroke:#090f13;stroke-width:.35}
@media(prefers-reduced-motion:reduce){#art.animate{animation:none}}body.reduced #art.animate{animation:none}
</style></head><body>
<main id="stage" aria-label="NET.runner felület előnézet"><div id="art"></div><div id="toast" role="status" aria-live="polite"></div></main>
<dialog id="dialog" aria-labelledby="dialog-title"><h2 id="dialog-title"></h2><div id="dialog-content"></div><div id="dialog-actions"></div></dialog>
<pre id="verification"></pre>
<script>const SCREENS='''+data+''';
'''+runtime+'''</script></body></html>'''
    (ROOT/'NET-runner-92-preview.html').write_text(html,encoding='utf-8')
    print(f'Created {len(screens)} screens, {len(comps)} portable components and the offline preview.')


if __name__=='__main__': main()
