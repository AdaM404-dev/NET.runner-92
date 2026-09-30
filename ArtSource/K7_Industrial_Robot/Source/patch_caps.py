from PIL import Image,ImageDraw
from pathlib import Path
import json
B=Path(__file__).resolve().parents[2];T=B/'outputs'/'K7_Industrial_Robot'/'Textures'
patches=json.loads((B/'work'/'cap_patches.json').read_text())
for path in (T/'4K').glob('*.png'):
    img=Image.open(path);d=ImageDraw.Draw(img)
    for p in patches:
        x,y=p['cx'],4096-p['cy'];u,v=p['sample_uv'];color=img.getpixel((int(u*4096),int((1-v)*4096)))
        if path.stem=='K7_Normal':color=(128,128,255)
        d.rectangle((x-12,y-12,x+12,y+12),fill=color)
    img.save(path);img.resize((2048,2048),Image.Resampling.LANCZOS).save(T/'2K'/path.name)
print('CAP_TEXTURE_PATCHES_COMPLETE')
