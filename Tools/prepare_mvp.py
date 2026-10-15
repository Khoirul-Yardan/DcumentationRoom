"""Append clearly synthetic MVP fixtures; preserve existing records and media."""
import copy, hashlib, json, pathlib, subprocess, wave, math, struct
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
MEDIA = ROOT / 'Assets/StreamingAssets/Media'
MANIFEST = ROOT / 'Assets/StreamingAssets/Catalog/catalog.json'

def page(path, title, lines, number):
    if path.exists(): return
    im = Image.new('RGB', (960, 1280), '#F5F2EA'); d = ImageDraw.Draw(im)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 30)
    heading = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 46)
    d.rectangle((0, 0, 960, 220), fill='#223549')
    d.text((64, 55), 'ATLAS / CONTOH SINTETIS', font=font, fill='#9BB79B')
    d.text((64, 115), title, font=heading, fill='white')
    for i, line in enumerate(lines): d.text((64, 280+i*65), line, font=font, fill='#223549')
    d.rounded_rectangle((64, 790, 896, 1080), radius=20, fill='#2F8F91')
    d.text((100, 840), 'IDE  >  PROSES  >  HASIL', font=heading, fill='white')
    d.text((100, 930), 'Seluruh informasi adalah data uji.', font=font, fill='white')
    d.text((64, 1160), f'Fixture lokal / bukan dokumen pengguna / {number}', font=font, fill='#223549')
    im.save(path)

def main():
    catalog = json.loads(MANIFEST.read_text(encoding='utf-8'))
    if not catalog.get('isSample'): raise RuntimeError('Only a labeled synthetic catalog may be extended by this tool.')
    def add(table, record):
        if not any(x['id'] == record['id'] for x in catalog[table]): catalog[table].append(record)
    for i, category in enumerate(['research','hardware']): add('categories',dict(id='sample-'+category,label='Contoh '+('Penelitian' if i==0 else 'Perangkat'),sortOrder=20+i*10))
    for i, title in enumerate(['Peta Pengetahuan','Stasiun Cuaca','Jurnal Eksperimen','Panel Energi','Rak Digital','Dokumentasi Aksesibel'],4):
        record = copy.deepcopy(catalog['projects'][0]); suffix=f'{i:03}'
        record.update(id='sample-project-'+suffix,slug='contoh-'+suffix,title='Contoh '+title,summary='Data sintetis untuk menguji katalog, paging, dan dokumentasi '+title.lower()+'.',year=2024+i%3,sortOrder=i*10,categoryIds=['sample-research' if i%2==0 else 'sample-hardware'],developmentStatus=['planned','in_progress','completed','archived'][i%4],displayType=['screen','pedestal','document'][i%3],documentIds=[],demoMediaIds=[],relatedProjectIds=[],isSample=True)
        add('projects',record)
    for number in [1,2]:
        name=f'sample-mvp-page-{number:03}'
        page(MEDIA/(name+'.png'),'Laporan contoh / '+str(number),['Tujuan: menguji dokumentasi offline.','Bukan laporan proyek asli pengguna.','', 'Halaman dapat diganti, diperbesar,', 'dan digeser tanpa memuat semua', 'halaman sekaligus.'],number)
        add('media',dict(id=name,kind='document_page',locator=dict(type='bundle',path='Media/'+name+'.png'),contentType='image/png',width=960,height=1280,offlineAllowed=True))
    page(MEDIA/'sample-mvp-video-card.png','Demo museum lokal',['VIDEO SINTETIS / 6 DETIK','', 'Klip ini dibuat untuk pengujian', 'play, pause, seek, volume dan', 'pembersihan decoder saat ditutup.'],1)
    video=MEDIA/'sample-mvp-demo.mp4'
    if not video.exists():
        temp=ROOT/'Temp/AtlasMvp'; temp.mkdir(parents=True,exist_ok=True)
        wav=temp/'sample-tone.wav'
        with wave.open(str(wav),'wb') as w:
            w.setparams((1,2,44100,0,'NONE','not compressed'))
            w.writeframes(b''.join(struct.pack('<h',int(1800*math.sin(2*math.pi*330*t/44100))) for t in range(6*44100)))
        subprocess.run(['ffmpeg','-y','-loop','1','-i',str(MEDIA/'sample-mvp-video-card.png'),'-i',str(wav),'-t','6','-r','24','-vf','scale=540:720','-c:v','libx264','-pix_fmt','yuv420p','-profile:v','baseline','-c:a','libvo_aacenc','-b:a','96k','-movflags','+faststart',str(video)],check=True,capture_output=True)
    add('media',dict(id='sample-mvp-demo',kind='video',locator=dict(type='bundle',path='Media/sample-mvp-demo.mp4'),contentType='video/mp4',width=540,height=720,offlineAllowed=True))
    for suffix, ready, mime in [('001',True,'application/pdf'),('002',False,'application/vnd.openxmlformats-officedocument.wordprocessingml.document')]:
        doc='sample-mvp-doc-'+suffix; version='sample-mvp-version-'+suffix
        add('documents',dict(id=doc,title='Contoh Laporan Dua Halaman' if ready else 'Contoh Dokumen Tanpa Preview',currentVersionId=version))
        add('documentVersions',dict(id=version,documentId=doc,versionLabel='v1 sintetis',mimeType=mime,modifiedAt='2026-10-14T08:00:00Z',sourceRef=dict(provider='fixture',fileId='sample-source-'+suffix,webUrl=None),previewMediaIds=['sample-mvp-page-001','sample-mvp-page-002'] if ready else [],previewState='ready' if ready else 'unavailable'))
    # Extend only this known synthetic fixture, without replacing its existing content.
    project=catalog['projects'][0]
    for field, values in [('documentIds',['sample-mvp-doc-001','sample-mvp-doc-002']),('demoMediaIds',['sample-mvp-demo']),('relatedProjectIds',['sample-project-009'])]:
        for value in values:
            if value not in project[field]: project[field].append(value)
    for i, date in enumerate(['2026-10-10','2026-10-14'],1): add('milestones',dict(id='sample-mvp-milestone-'+str(i),projectId=project['id'],date=date,title='Milestone sintetis '+str(i),description='Kejadian contoh untuk menguji pengurutan timeline, bukan capaian pengguna.',evidenceDocumentIds=['sample-mvp-doc-001']))
    add('exhibitions',dict(id='sample-mvp-exhibition',title='Pameran Contoh Sintetis',projectIds=[project['id'],'sample-project-009'],presentationMediaIds=['sample-mvp-demo']))
    for media in catalog['media']:
        if media['id'].startswith('sample-mvp-'): media['checksum']=hashlib.sha256((ROOT/'Assets/StreamingAssets'/media['locator']['path']).read_bytes()).hexdigest()
    catalog['catalogVersion']='sample-mvp-2026-10-14'; catalog['publishedAt']='2026-10-14T08:00:00Z'
    MANIFEST.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f"Synthetic MVP: {len(catalog['projects'])} projects, {len(catalog['documents'])} documents, {len(catalog['media'])} media.")

if __name__=='__main__': main()
