from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parent/'Textures'
regular='C:/Windows/Fonts/consola.ttf';bold='C:/Windows/Fonts/consolab.ttf'
def font(n,b=False):return ImageFont.truetype(bold if b else regular,n)
for kind in ['Control','Access']:
    im=Image.new('RGB',(1024,512),(12,23,29));d=ImageDraw.Draw(im)
    ink=(149,187,195);dim=(70,105,116);white=(194,214,218);line=(35,60,71)
    d.rectangle((0,0,1024,5),fill=(103,156,170))
    d.text((36,28),'NORTHLINE  /  '+('SYSTEM TELEMETRY' if kind=='Control' else 'ACCESS CONTROL'),font=font(28,True),fill=white)
    d.line((34,81,990,81),fill=line,width=2)
    if kind=='Control':
        for yy in [156,207,258,309]:d.line((36,yy,640,yy),fill=line,width=1)
        for xx in range(36,641,75):d.line((xx,118,xx,310),fill=line,width=1)
        points=[(36,251),(78,252),(100,223),(150,229),(178,220),(216,237),(265,228),(301,175),(345,198),(390,186),(435,197),(480,150),(531,160),(586,156),(640,170)]
        d.line(points,fill=ink,width=3)
        d.text((37,337),'LOAD HISTORY  /  60 MIN',font=font(20),fill=dim)
        for i,(title,value) in enumerate([('NETWORK','ONLINE'),('LOAD','41.2 %'),('AIR TEMP','24.8 C')]):
            yy=111+i*88;d.text((704,yy),title,font=font(19),fill=dim);d.text((704,yy+25),value,font=font(29,True),fill=ink)
        d.line((34,398,990,398),fill=line,width=2)
        d.text((36,422),'NO ACTIVE FAULTS',font=font(25,True),fill=ink)
        d.text((36,467),'LOCAL CONTROLLER  /  LINK ENCRYPTED',font=font(17),fill=dim)
        d.rounded_rectangle((744,430,986,480),radius=4,outline=dim,width=2)
        d.text((773,441),'DIAGNOSTICS',font=font(24),fill=white)
    else:
        d.rounded_rectangle((53,119,290,356),radius=17,outline=dim,width=3)
        for off in range(0,61,10):d.arc((90+off,151+off,253-off,319-off),-30,260,fill=ink,width=2)
        d.text((358,142),'PRESENT CREDENTIAL',font=font(33,True),fill=white)
        d.text((360,206),'RFID + BIOMETRIC',font=font(24),fill=ink)
        d.text((360,252),'CLEARANCE  /  LEVEL 02',font=font(23),fill=dim)
        d.text((360,320),'READER READY',font=font(28,True),fill=ink)
        d.line((34,398,990,398),fill=line,width=2)
        d.text((36,435),'SECURE LINK',font=font(24),fill=ink)
        d.text((570,435),'LOGGING ENABLED',font=font(24),fill=dim)
    im.save(root/('NF_UI_'+kind+'.png'))
