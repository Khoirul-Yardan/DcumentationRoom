"""Validate curated public JSON, then update the embedded module. Never creates sample data."""
import json
import datetime
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent

def validate(catalog):
    assert catalog['schemaVersion']==1,'Unsupported schema'
    assert isinstance(catalog['categories'],list) and len(set(catalog['categories']))==len(catalog['categories'])
    ids=set();docs=set()
    for project in catalog['projects']:
        pid=project['id']
        assert isinstance(pid,str) and pid and pid not in ids,f'Duplicate/empty project ID: {pid}'
        ids.add(pid)
        for field in ['title','summary','problem','solution','updatedAt']:
            assert isinstance(project[field],str) and project[field],f'{pid}: missing {field}'
        assert project['publicationState']=='published' and project['visibility']=='public',f'{pid}: private or draft data cannot be replicated'
        assert project['category'] in catalog['categories'],f'{pid}: unknown category'
        assert project['year'] is None or (type(project['year']) is int and 1900<=project['year']<=2100)
        assert project['developmentStatus'] in ['planned','in_progress','completed','archived','documented']
        assert isinstance(project['sortOrder'],(float,int))
        assert len(project['color'])==3 and all(type(v)is int and 0<=v<=255 for v in project['color'])
        assert not project['isSample'] or pid.startswith('sample-')
        for field in ['tags','outcomes','features','techStack']:
            assert isinstance(project[field],list) and all(isinstance(x,str) for x in project[field])
        assert isinstance(project['team'],list)
        datetime.date.fromisoformat(project['updatedAt'])
        for doc in project['documents']:
            assert doc['id'] not in docs,f'Duplicate document: {doc["id"]}'
            docs.add(doc['id'])
            assert doc['kind']=='text' and doc['title'] and doc['pages']
            assert all(isinstance(page,str) and 0<len(page)<=16000 for page in doc['pages'])
        for item in project['milestones']:
            datetime.date.fromisoformat(item['date']);assert item['title'] and item['description']
        assert isinstance(project['gallery'],list) and isinstance(project['videos'],list)
    return len(ids),len(docs)

def sync():
    catalog=json.loads((ROOT/'src/catalog.json').read_text(encoding='utf-8'))
    counts=validate(catalog)
    raw=json.dumps(catalog,ensure_ascii=False,indent=2)
    # Choose a long-string delimiter that cannot be closed by content.
    equals='===='
    while ']'+equals+']' in raw:equals+='='
    source='-- Generated from validated public catalog.json by tools/sync_catalog.py.\nreturn game:GetService("HttpService"):JSONDecode(['+equals+'[\n'+raw+'\n]'+equals+'])\n'
    (ROOT/'src/Catalog.luau').write_text(source,encoding='utf-8')
    print(f'Validated and synchronized {counts[0]} public projects, {counts[1]} text documents')
if __name__=='__main__':sync()
