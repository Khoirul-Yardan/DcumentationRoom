"""Optional live read-only sync. Requires DRIVE_ACCESS_TOKEN, never writes credentials.
The only inventory root is the user-authorized RISTEK folder. Follows pagination,
does not follow shortcuts, and hydrates only bounded text/plain files.
"""
import json,os,time,urllib.request,urllib.parse
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
FOLDER='1K0gYFMdzSQlvm8KyYz702XvYIg6pryh_'
def request(path,params=None):
    url='https://www.googleapis.com/drive/v3/'+path
    if params:url+='?'+urllib.parse.urlencode(params)
    req=urllib.request.Request(url,headers={'Authorization':'Bearer '+os.environ['DRIVE_ACCESS_TOKEN']})
    with urllib.request.urlopen(req,timeout=25) as res:return res.read(2_000_000)
def main():
    if not os.environ.get('DRIVE_ACCESS_TOKEN'):raise SystemExit('Set DRIVE_ACCESS_TOKEN for the authorized folder owner; no token is stored in this project.')
    root=json.loads(request('files/'+FOLDER,{'fields':'id,name,mimeType'}))
    items=[{'id':root['id'],'name':root['name'],'kind':'folder','parentId':None,'listingComplete':False}]
    queue=[items[0]];visited=set()
    while queue:
        folder=queue.pop(0)
        if folder['id'] in visited:continue
        visited.add(folder['id']);page=None
        while True:
            params={'q':f"'{folder['id']}' in parents and trashed = false",'pageSize':100,'fields':'nextPageToken,files(id,name,mimeType,size,modifiedTime,webViewLink)','supportsAllDrives':'true','includeItemsFromAllDrives':'true'}
            if page:params['pageToken']=page
            result=json.loads(request('files',params))
            for f in result.get('files',[]):
                kind='folder' if f['mimeType']=='application/vnd.google-apps.folder' else 'shortcut' if f['mimeType']=='application/vnd.google-apps.shortcut' else 'file'
                item={'id':f['id'],'name':f['name'],'kind':kind,'parentId':folder['id'],'mimeType':f['mimeType'],'size':f.get('size'),'modifiedAt':f.get('modifiedTime'),'url':f.get('webViewLink'),'previewText':None}
                if kind=='folder':item['listingComplete']=False;queue.append(item)
                elif f['mimeType']=='text/plain' and int(f.get('size',0))<=16000:
                    item['previewText']=request('files/'+f['id'],{'alt':'media'}).decode('utf-8-sig')
                items.append(item)
            page=result.get('nextPageToken')
            if not page:break
        folder['listingComplete']=True
    output={'rootId':FOLDER,'rootName':root['name'],'fetchedAt':time.strftime('%Y-%m-%d'),'method':'Drive API, recursive paginated read, shortcuts not followed','items':items}
    target=ROOT/'content/drive-inventory.json';temp=target.with_suffix('.pending.json')
    temp.write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8');temp.replace(target)
    print(f'Synced {len(items)} nodes. Run prepare_drive.py, sync_catalog.py, then build_museum.py to update the place.')
if __name__=='__main__':main()
