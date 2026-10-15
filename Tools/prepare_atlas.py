"""Reproducible synthetic fixtures and deterministic .meta files (standard library only)."""
import json, pathlib, struct, zlib, uuid

ROOT = pathlib.Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets'

def png(path, accent):
    width, height = 1280, 720
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            color = (245, 242, 234)
            if 65 < x < 1215 and 60 < y < 660: color = (34, 53, 73)
            if 100 < x < 1180 and 95 < y < 160: color = accent
            # Deliberately synthetic wireframe diagram, no claim of a real project screenshot.
            for col in range(3):
                if 110 + col * 365 < x < 440 + col * 365 and 220 < y < 555:
                    color = (255, 255, 255)
                    if 245 < y < 355: color = accent
                    if 385 < y < 403 or 430 < y < 442 or 475 < y < 487: color = (155, 183, 155)
            raw.extend(color)
    def chunk(tag, data): return struct.pack('!I', len(data)) + tag + data + struct.pack('!I', zlib.crc32(tag + data) & 0xffffffff)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', width, height, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(bytes(raw))) + chunk(b'IEND', b''))

def fixtures():
    target = ASSETS / 'StreamingAssets/Catalog/catalog.json'
    if target.exists(): return
    catalog = dict(schemaVersion=1, catalogVersion='sample-2026-10-10-001', publishedAt='2026-10-10T08:00:00Z', isSample=True,
                   categories=[dict(id='sample-digital', label='Contoh Karya Digital', sortOrder=10)], projects=[], media=[], documents=[], documentVersions=[], milestones=[], exhibitions=[])
    titles = ['Contoh Atlas Catatan', 'Contoh Kebun Sensor', 'Contoh Arsip Cerita']
    summaries = ['Prototype sintetis pencatatan dan pencarian informasi.', 'Simulasi contoh pemantauan taman; tidak terhubung perangkat nyata.', 'Demonstrasi pengelompokan dokumentasi berdasarkan perjalanan proyek.']
    for i, (title, summary, accent) in enumerate(zip(titles, summaries, [(47,143,145),(155,183,155),(236,163,92)]), 1):
        suffix = f'{i:03}'
        media_id = 'sample-cover-' + suffix
        catalog['projects'].append(dict(id='sample-project-'+suffix, slug='contoh-'+suffix, title=title, summary=summary, year=2026,
            developmentStatus=['in_progress','completed','archived'][i-1], publicationState='published', visibility='public', categoryIds=['sample-digital'],
            tags=['contoh','prototype',['catatan','sensor','arsip'][i-1]], problem='Contoh kebutuhan menyusun informasi agar mudah ditemukan dan dipahami.',
            solution='Prototype sintetis dengan daftar terstruktur, pencarian, dan tampilan ringkasan.', outcomes=['Hasil demonstrasi untuk pengujian museum. Bukan capaian proyek asli pengguna.'],
            features=['Daftar informasi','Pencarian'], team=[dict(displayName='Tim Contoh',role='Perancang prototype sintetis')], techStack=['Unity','C#','JSON'],
            coverMediaId=media_id, galleryMediaIds=[media_id], demoMediaIds=[], documentIds=[], relatedProjectIds=[], displayType='screen', sortOrder=i*10, updatedAt='2026-10-10T08:00:00Z', isSample=True))
        catalog['media'].append(dict(id=media_id,kind='image',locator=dict(type='bundle',path='Media/'+media_id+'.png'),contentType='image/png',checksum=None,width=1280,height=720,byteSize=None,offlineAllowed=True))
        png(ASSETS / 'StreamingAssets/Media' / (media_id+'.png'), accent)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding='utf-8')

def assemblies():
    deps = {'Domain': [], 'Application':['Atlas.Domain'], 'Infrastructure':['Atlas.Domain','Atlas.Application'],
            'Presentation':['Atlas.Domain','Atlas.Application','Unity.TextMeshPro','UnityEngine.UI'],
            'Runtime':['Atlas.Domain','Atlas.Application','Atlas.Infrastructure','Atlas.Presentation','Unity.InputSystem','UnityEngine.UI'],
            'Editor':['Atlas.Domain','Atlas.Application','Atlas.Infrastructure','Atlas.Presentation','Atlas.Runtime','Unity.TextMeshPro','UnityEngine.UI','Unity.InputSystem']}
    for name, refs in deps.items():
        path = ASSETS / '_Project/Scripts' / name / ('Atlas.'+name+'.asmdef')
        path.write_text(json.dumps(dict(name='Atlas.'+name,rootNamespace='ProjectAtlas.'+name,references=refs,includePlatforms=['Editor'] if name=='Editor' else [],noEngineReferences=name in ['Domain','Application']),indent=2),encoding='utf-8')

def metas():
    targets = list((ASSETS / '_Project').rglob('*')) + list((ASSETS / 'StreamingAssets').rglob('*')) + [ASSETS/'_Project',ASSETS/'StreamingAssets']
    for path in targets:
        if path.suffix == '.meta': continue
        meta = pathlib.Path(str(path)+'.meta')
        if meta.exists(): continue
        guid = uuid.uuid5(uuid.NAMESPACE_URL, 'project-atlas/'+path.relative_to(ASSETS).as_posix()).hex
        text = 'fileFormatVersion: 2\nguid: '+guid+'\n'
        if path.is_dir(): text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
        elif path.suffix == '.cs': text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        elif path.suffix == '.asmdef': text += 'AssemblyDefinitionImporter:\n  externalObjects: {}\n'
        else: text += 'DefaultImporter:\n  externalObjects: {}\n'
        meta.write_text(text,encoding='utf-8')

if __name__ == '__main__':
    fixtures(); assemblies(); metas()
    print('Prepared synthetic catalog, 3 PNG fixtures, module assemblies, and missing metadata.')

