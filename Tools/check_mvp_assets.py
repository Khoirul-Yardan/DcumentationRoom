"""Read-only asset, fixture and dependency checks; does not claim Unity import/Play."""
import hashlib, json, pathlib, subprocess
from PIL import Image

ROOT=pathlib.Path(__file__).resolve().parents[1]
report=[]
def check(condition,name):
    if not condition: raise AssertionError(name)
    report.append('PASS '+name)

c=json.loads((ROOT/'Assets/StreamingAssets/Catalog/catalog.json').read_text(encoding='utf-8'))
check(len(c['projects'])==9 and all(p['isSample'] and p['id'].startswith('sample-') for p in c['projects']), '9 labeled synthetic projects')
check(len({p['year'] for p in c['projects']})==3 and len(c['categories'])==3, '3 years and 3 categories for intersecting filters')
for table in ['projects','documents','documentVersions','media','milestones','exhibitions','categories']:
    check(len({x['id'] for x in c[table]})==len(c[table]),table+' IDs unique')
for media in c['media']:
    path=ROOT/'Assets/StreamingAssets'/media['locator']['path']
    check(path.is_file(),'media file exists: '+media['id'])
    if media.get('checksum'): check(hashlib.sha256(path.read_bytes()).hexdigest()==media['checksum'],'SHA256: '+media['id'])
    if media['kind']!='video':
        with Image.open(path) as im: check(im.size==(media['width'],media['height']),'decoded image dimensions: '+media['id']); im.verify()
probe=subprocess.run(['ffprobe','-v','quiet','-show_streams','-of','json',str(ROOT/'Assets/StreamingAssets/Media/sample-mvp-demo.mp4')],capture_output=True,text=True,check=True)
streams=json.loads(probe.stdout)['streams']
check(any(s['codec_name']=='h264' and s['width']==540 and s['height']==720 for s in streams),'MP4 fixture H.264 540x720')
check(any(s['codec_name']=='aac' for s in streams),'MP4 fixture AAC audio; Unity playback remains untested')
baseline=ROOT/'Temp/AtlasMvp/preserved.json'
if baseline.exists():
    preserved=json.loads(baseline.read_text())
    check(all((ROOT/p).is_file() and hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha for p,sha in preserved.items()),'pre-existing scenes/prefabs/materials and pinned package/version files unchanged')
else:
    report.append('NOT MEASURED initial asset hash baseline: Unity removed Temp during startup; use Editor builder idempotency check for byte comparison.')
check((ROOT/'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0]=='m_EditorVersion: 6000.3.23f1','Unity 6000.3.23f1 retained')
manifest=json.loads((ROOT/'Packages/manifest.json').read_text())['dependencies']
check(manifest['com.unity.render-pipelines.universal']=='17.3.0' and manifest['com.unity.inputsystem']=='1.20.0','URP 17.3.0 and Input System 1.20.0 retained')
assets=list((ROOT/'Assets/_Project').rglob('*'))+list((ROOT/'Assets/StreamingAssets').rglob('*'))
check(all(pathlib.Path(str(p)+'.meta').is_file() for p in assets if p.suffix!='.meta'),'metadata present for project code and fixtures')
assemblies={}
for p in (ROOT/'Assets/_Project/Scripts').rglob('*.asmdef'):
    data=json.loads(p.read_text()); assemblies[data['name']]=[r for r in data['references'] if r.startswith('Atlas.')]
visited=set()
def visit(name,stack):
    if name in stack: raise AssertionError('Assembly cycle: '+' -> '.join(stack+[name]))
    if name in visited: return
    for dep in assemblies[name]: visit(dep,stack+[name])
    visited.add(name)
    check(True,'assembly acyclic: '+name)
for name in assemblies: visit(name,[])
output=ROOT/'Docs/Evidence/MVP'; output.mkdir(parents=True,exist_ok=True)
(output/'asset-checks.txt').write_text('\n'.join(report)+'\n',encoding='utf-8')
print('\n'.join(report[:30])); print('TOTAL PASS',sum(line.startswith('PASS ') for line in report))
