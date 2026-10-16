"""Build museum content only from the observed RISTEK inventory (no invented projects)."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
def module(name,data):
    raw=json.dumps(data,ensure_ascii=False,indent=2);eq='===='
    while ']'+eq+']' in raw:eq+='='
    (ROOT/'src'/name).write_text('return game:GetService("HttpService"):JSONDecode(['+eq+'[\n'+raw+'\n]'+eq+'])\n',encoding='utf-8')
def main():
    data=json.loads((ROOT/'content/drive-inventory.json').read_text(encoding='utf-8'))
    lookup={n['id']:n for n in data['items']}
    def path(node):
        names=[node['name']];parent=node.get('parentId')
        while parent:node=lookup[parent];names.append(node['name']);parent=node.get('parentId')
        return '/'.join(reversed(names))
    for node in data['items']:
        node['path']=path(node)
        # Keep the public runtime snapshot to names, structure and small requested text previews.
        if node.get('url'):node['url']=node['url'].split('?')[0]
    module('DriveArchive.luau',data)
    picks=[('Website HIMA','PROJECT'),('Scraping Job, Event, dll','PROJECT'),('Tech Insight','Tech Insight'),('Mini Showcase','Mini Showcase'),('Tech Corner','Tech Corner'),('Administrasi','Administrasi'),('TEMPLATE DOKUMENTASI','Administrasi'),('Informatika','Tech Insight')]
    palette=[[94,166,228],[61,206,190],[166,145,224],[236,133,115],[242,185,85]]
    projects=[]
    for i,(name,category) in enumerate(picks):
        folder=next(n for n in data['items'] if n['name']==name and n['kind']=='folder')
        children=[n for n in data['items'] if n['path'].startswith(folder['path']+'/')]
        files=[n for n in children if n['kind']!='folder']
        docs=[]
        for node in files:
            body=node.get('previewText') or 'Isi berkas belum dikonversi menjadi preview Roblox.\n\nJenis: '+node.get('mimeType','unknown')+'\nLokasi: '+node['path']+'\n\nIni adalah metadata dari Drive, bukan ringkasan isi dokumen.'
            docs.append({'id':'drive-doc-'+node['id'],'title':node['name'],'kind':'text','pages':[body],'sourceRef':node['id'],'previewKind':'text' if node.get('previewText') else 'metadata'})
        # The same Drive file can occur in a collection and a nested collection: scoped document IDs.
        for doc in docs:doc['id']='drive-doc-'+folder['id']+'-'+doc['sourceRef']
        description=next((n['previewText'] for n in files if n.get('previewText') and n['name']==name+'.txt'),None)
        projects.append({'id':'drive-'+folder['id'],'title':name,'category':category,'year':None,'developmentStatus':'documented','publicationState':'published','visibility':'public','isSample':False,'sortOrder':i+1,'color':palette[i%5],
            'summary':description or f'Koleksi dari {folder["path"]}; {len(files)} berkas terinventarisasi.',
            'problem':'Belum tercantum sebagai narasi proyek pada metadata Drive yang dibaca.','solution':'Buka Dokumentasi untuk melihat file dan preview yang tersedia.',
            'outcomes':['Keberadaan folder dan daftar file telah diverifikasi. Status penyelesaian proyek belum disimpulkan.'],'features':[f'{len(files)} berkas terinventarisasi','Sumber tunggal: folder Drive RISTEK'],
            'team':[],'techStack':[],'tags':[category,name,'Drive'],'updatedAt':data['fetchedAt'],'sourceLabel':folder['path'],'sourceFolderId':folder['id'],
            'documents':docs,'milestones':[],'gallery':[],'videos':[]})
    catalog={'schemaVersion':1,'isSample':False,'catalogVersion':'drive-'+data['fetchedAt'],'categories':['PROJECT','Tech Insight','Mini Showcase','Tech Corner','Administrasi'],'projects':projects}
    (ROOT/'src/catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Prepared {len(projects)} real Drive collections; {len(data["items"])} archive nodes')
if __name__=='__main__':main()
