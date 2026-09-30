from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np, json, random
BASE=Path(__file__).resolve().parents[2];OUT=BASE/'outputs'/'K7_Industrial_Robot';WORK=BASE/'work';T=OUT/'Textures'/'4K'
random.seed(117)
base=Image.open(T/'K7_BaseColor.png').convert('RGB')
for tile in json.loads((WORK/'tiles.json').read_text()):
    if tile['kind']=='StatusLight':
        x,y,size=tile['tile'];ImageDraw.Draw(base).rectangle((x,4096-y-size,x+size-1,4096-y-1),fill=(225,225,225))
rough=Image.open(T/'K7_Roughness.png').convert('L')
metal=Image.open(T/'K7_Metallic.png').convert('L')
normal=Image.open(T/'K7_Normal.png').convert('RGB')
height=Image.new('L',(4096,4096),128)
def font(size,bold=False):return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if bold else 'bahnschrift.ttf'),size)

def decal(kind):
    im=Image.new('RGBA',(1024,1024));d=ImageDraw.Draw(im)
    h=Image.new('L',(1024,1024),128);hd=ImageDraw.Draw(h)
    ink=(187,191,181,225);muted=(135,147,148,205);yellow=(171,120,35,240)
    def text(x,y,s,sz=36,col=ink):d.text((x,y),s,font=font(sz),fill=col)
    def screw(x,y):
        d.ellipse((x-11,y-11,x+11,y+11),fill=(16,22,27,255),outline=(107,117,119,255),width=3)
        d.line((x-5,y,x+5,y),fill=(102,110,112,255),width=2)
        hd.ellipse((x-11,y-11,x+11,y+11),fill=60,outline=155,width=3)
    def triangle(x,y,sz):
        d.line([(x,y+sz),(x+sz*.5,y),(x+sz,y+sz),(x,y+sz)],fill=ink,width=5)
        d.line([(x+sz*.5,y+sz*.28),(x+sz*.5,y+sz*.65)],fill=ink,width=5)
        d.ellipse((x+sz*.5-3,y+sz*.80-3,x+sz*.5+3,y+sz*.80+3),fill=ink)
    if kind=='chest':
        text(155,175,'K-7',125);text(165,310,'INDUSTRIAL SYSTEMS',20)
        text(165,785,'HXC  /  07-2149',25,muted)
        triangle(698,735,99)
        for x,y in [(100,110),(907,110),(185,814),(835,814)]:screw(x,y)
        for x in [70,940]:d.line((x,210,x,710),fill=(10,15,20,210),width=5);hd.line((x,210,x,710),fill=105,width=3)
        d.rectangle((770,230,851,259),fill=yellow);text(772,276,'LIFT',21,muted)
        for j in range(9):d.rectangle((168+j*8,854,172+j*8,891),fill=muted)
    elif kind in ('back','service'):
        text(170,110,'SERVICE' if kind=='service' else 'K-7',88)
        triangle(411,328,168)
        text(212,601,'AUTHORIZED ACCESS',38)
        text(234,674,'NETWORK  /  DIAGNOSTICS',25,muted)
        d.rectangle((157,768,850,773),fill=muted)
        text(170,802,'24V DC       HXC-714',28,muted)
        for x in [90,931]:
            for y in [85,932]:screw(x,y)
    elif kind=='head':
        text(235,25,'K-7',60)
    else:
        for x,y in [(310,145),(743,150),(450,780)]:screw(x,y)
        text(340,229,{'forearm':'07','thigh':'K7','shin':'HXC'}[kind],86)
        text(348,335,'ACTUATOR',26,muted)
        text(350,375,'HIGH LOAD',24,muted)
        d.rectangle((330,440,417,565),fill=yellow)
        for y in range(442,554,30):d.polygon([(333,y),(415,y+24),(415,y+37),(333,y+13)],fill=(32,39,41,240))
        text(469,700,'714-A',27,muted)
        d.line((620,457,620,779),fill=(10,15,20,180),width=5);hd.line((620,457,620,779),fill=90,width=3)
    # Worn, imperfect print without obliterating the markings.
    arr=np.array(im);rng=np.random.default_rng(47)
    arr[:,:,3]=np.where(rng.random(arr.shape[:2])>.035,arr[:,:,3],0)
    return Image.fromarray(arr),h

for item in json.loads((WORK/'decals.json').read_text()):
    im,h=decal(item['type']);bounds=item['bounds'];xmin,xmax=bounds[0];zmin,zmax=bounds[2]
    for face in item['faces']:
        uv=np.array([[u*4096,(1-v)*4096] for u,v in face['uv']]);pos=face['pos']
        src=np.array([[(p[0]-xmin)/(xmax-xmin)*1024,(1-(p[2]-zmin)/(zmax-zmin))*1024] for p in pos])
        if item['back']:src[:,0]=1024-src[:,0]
        mat=np.c_[uv,np.ones(3)]
        if abs(np.linalg.det(mat))<.1:continue
        coeff=np.linalg.solve(mat,src)
        x0,y0=np.maximum(0,np.floor(uv.min(axis=0)).astype(int)-1);x1,y1=np.minimum(4096,np.ceil(uv.max(axis=0)).astype(int)+2)
        if x1<=x0 or y1<=y0:continue
        size=(int(x1-x0),int(y1-y0)); a,b,c=coeff[:,0];dd,e,f=coeff[:,1]
        affine=(a,b,c+a*x0+b*y0,dd,e,f+dd*x0+e*y0)
        piece=im.transform(size,Image.Transform.AFFINE,affine,Image.Resampling.BICUBIC)
        hp=h.transform(size,Image.Transform.AFFINE,affine,Image.Resampling.BILINEAR,fillcolor=128)
        mask=Image.new('L',size);md=ImageDraw.Draw(mask);md.polygon([tuple(xy-[x0,y0]) for xy in uv],fill=255)
        alpha=Image.fromarray((np.asarray(piece.getchannel('A'),dtype=np.uint16)*np.asarray(mask,dtype=np.uint16)//255).astype('uint8'))
        base.paste(piece.convert('RGB'),(x0,y0),alpha)
        rough.paste(170,(x0,y0,x1,y1),alpha);metal.paste(40,(x0,y0,x1,y1),alpha)
        height.paste(hp,(x0,y0),mask)

# Very light maintenance scratches, constrained to occupied non-rubber surfaces.
arr=np.array(base);occupied=(np.array(metal)>80)&(arr.mean(axis=2)>12)
scr=Image.new('RGBA',base.size);sd=ImageDraw.Draw(scr)
for i in range(4200):
    x=random.randrange(4096);y=random.randrange(4096)
    if not occupied[y,x]:continue
    length=random.randint(2,11)
    sd.line((x,y,x+random.randint(-2,2),y+length),fill=(134,143,147,random.randrange(15,62)),width=1)
base=Image.alpha_composite(base.convert('RGBA'),scr).convert('RGB')
base.save(T/'K7_BaseColor.png');rough.save(T/'K7_Roughness.png');metal.save(T/'K7_Metallic.png')

h=np.asarray(height,dtype=np.float32)/255
dy,dx=np.gradient(h)
n=np.asarray(normal,dtype=np.float32)/127.5-1
n[:,:,0]-=dx*2;n[:,:,1]+=dy*2;n/=np.maximum(np.linalg.norm(n,axis=2,keepdims=True),.001)
Image.fromarray(np.uint8(np.clip((n+1)*127.5,0,255))).save(T/'K7_Normal.png')
# Unity metallic workflow: metallic R, smoothness A. AO is supplied separately.
m=np.array(metal);r=np.array(rough);packed=np.zeros((4096,4096,4),dtype=np.uint8);packed[:,:,0]=m;packed[:,:,3]=255-r
Image.fromarray(packed).save(T/'K7_MetallicSmoothness.png')
for p in T.glob('*.png'):
    img=Image.open(p);small=img.resize((2048,2048),Image.Resampling.LANCZOS)
    if p.stem=='K7_Normal':
        nv=np.asarray(small,dtype=np.float32)/127.5-1;nv/=np.maximum(np.linalg.norm(nv,axis=2,keepdims=True),.001);small=Image.fromarray(np.uint8(np.clip((nv+1)*127.5,0,255)))
    small.save(OUT/'Textures'/'2K'/p.name)
print('TEXTURES_COMPLETE')
