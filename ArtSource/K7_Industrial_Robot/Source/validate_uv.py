import numpy as np,json
from pathlib import Path
B=Path(__file__).resolve().parents[2];O=B/'outputs'/'K7_Industrial_Robot'
t=np.load(B/'work'/'lod0_uv_triangles.npy')*4096
coverage=np.zeros((4096,4096),np.uint8);tested=tiny=0;overlap_triangles=0
for tri in t:
    lo=np.maximum(0,np.floor(tri.min(axis=0)).astype(int));hi=np.minimum(4095,np.ceil(tri.max(axis=0)).astype(int))
    if np.any(hi<lo):continue
    a,b,c=tri;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-5:tiny+=1;continue
    yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];xx=xx+.5;yy=yy+.5
    w1=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
    w2=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;w3=1-w1-w2
    inside=(w1>1e-4)&(w2>1e-4)&(w3>1e-4)
    area=coverage[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
    if np.any((area>0)&inside):overlap_triangles+=1
    area[inside]=np.minimum(area[inside]+1,250);tested+=1
report={'resolution':4096,'triangles_tested':tested,'subpixel_or_degenerate_uv_triangles':tiny,'occupied_interior_texels':int(np.sum(coverage>0)),'overlapping_interior_texels':int(np.sum(coverage>1)),'triangles_with_overlapping_interior_texels':overlap_triangles,'method':'Barycentric interior raster at 4K; shared edges excluded. Subpixel overlap is below this test resolution. Lower LODs intentionally reuse the LOD0 atlas.'}
(O/'Validation'/'uv_validation.json').write_text(json.dumps(report,indent=2));print(report)
