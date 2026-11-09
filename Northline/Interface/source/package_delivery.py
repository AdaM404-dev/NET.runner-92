"""Create a verified, reproducible standalone handoff archive."""
from pathlib import Path
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT.parent/'NET-runner-92-interface.zip'
files=sorted(p for p in ROOT.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix not in ['.blend1','.pyc'] and p.name!='SHA256SUMS.json')
manifest={str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
(ROOT/'SHA256SUMS.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
files.append(ROOT/'SHA256SUMS.json')
with zipfile.ZipFile(DEST,'w',zipfile.ZIP_DEFLATED,compresslevel=8) as archive:
    for path in files:archive.write(path,'NET-runner-92-interface/'+str(path.relative_to(ROOT)).replace('\\','/'))
with zipfile.ZipFile(DEST) as archive:
    assert archive.testzip() is None
    for relative,expected in manifest.items():
        assert hashlib.sha256(archive.read('NET-runner-92-interface/'+relative)).hexdigest()==expected
print(json.dumps(dict(archive=str(DEST),files=len(files),bytes=DEST.stat().st_size,sha256=hashlib.sha256(DEST.read_bytes()).hexdigest(),verified=True)))
