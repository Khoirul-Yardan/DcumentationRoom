"""Structural and content checks. These do NOT replace Roblox Studio Play tests."""
import copy, hashlib, json, math, struct, sys
from pathlib import Path
from rbxl import Place, decode, encode, read_refs, refs
from sync_catalog import validate
ROOT=Path(__file__).resolve().parent.parent

def run():
    p=Place(ROOT/'Place1.rbxl');original=Place(ROOT/'backups/Place1.original.rbxl')
    checks=[]
    def check(name,condition):
        assert condition,name
        checks.append(name)
    check('Binary RBXL header instance/class counts',struct.unpack_from('<II',p.header,16)==(len(p.classes),len(p.parents)))
    allrefs=[r for _,_,rs in p.classes.values() for r in rs]
    check('Unique referents and one parent per instance',len(allrefs)==len(set(allrefs)) and set(allrefs)==set(p.parents))
    check('Every parent resolves',all(parent==-1 or parent in p.parents for parent in p.parents.values()))
    for ref in allrefs:
        seen=set();at=ref
        while at!=-1:
            assert at not in seen,'Parent cycle';seen.add(at);at=p.parents[at]
    checks.append('No hierarchy cycles')
    check('All 79 original instance referents retained',set(original.parents)<=set(p.parents))
    original_part=original.service('Part')
    check('Original parents unchanged except adapted baseplate',all(p.parents[r]==parent for r,parent in original.parents.items() if r!=original_part))
    check('Workspace streaming explicitly disabled',p.property('Workspace','StreamingEnabled')==[False])
    names={}
    for cid,(cls,_,rs) in p.classes.items():
        if (cid,'Name') in p.props:
            for r,n in zip(rs,p.property(cls,'Name')):names[r]=n
    root=next(r for r,n in names.items() if n=='AtlasMuseum')
    children={names[r] for r,parent in p.parents.items() if parent==root}
    check('Five zones and landscape/bridges present',{'Atrium','Gallery','Archive','Auditorium','Garden','Landscape','Bridges'}<=children)
    check('Eight named exhibit stands',sum(n.startswith('Stand') and len(n)==7 for n in names.values())==8)
    prompts=p.property('ProximityPrompt','Name')
    check('28 exhibit, NPC, pickup and terminal prompts',len(prompts)==28 and all('Project_'+str(i) in prompts for i in range(1,9)))
    check('Six NPC prompts',all('NPC_'+n in prompts for n in ['Nara','Bima','Tika','Sari','Dimas','Lumi']))
    check('Five garden collection prompts',all('Collect_'+str(i) in prompts for i in range(1,6)))
    check('Mission board and audience screen interactions','Missions' in prompts and 'AudienceView' in prompts)
    check('Prompts use LOS and 12-stud activation',all(p.property('ProximityPrompt','RequiresLineOfSight')) and all(x==12 for x in p.property('ProximityPrompt','MaxActivationDistance')))
    check('32 auditorium seats',len(p.classes[p.byname['Seat']][2])==32)
    expected={'AtlasClient':'Client.client.luau','AtlasServer':'Server.server.luau','Catalog':'Catalog.luau','CatalogService':'CatalogService.luau','AtlasValidation':'StudioValidation.luau','AtlasQuests':'Quests.server.luau','AtlasPresentation':'Presentation.server.luau'}
    for module in ['DriveArchive','GameConfig','FeatureUI','QuestEngine','PresentationConfig','PresentationRules','CanvaDecks']:expected[module]=module+'.luau'
    for cls in ['Script','LocalScript','ModuleScript']:
        for name,source in zip(p.property(cls,'Name'),p.property(cls,'Source')):
            check('Embedded source matches '+name,source==(ROOT/'src'/expected[name]).read_text(encoding='utf-8'))
    check('Three distinct remotes',set(p.property('RemoteEvent','Name'))=={'Travel','Quest','Presentation'})
    server=p.service('ServerScriptService')
    check('Quest authority and presentation config are server-only',all(p.parents[next(r for r,n in names.items() if n==name)]==server for name in ['QuestEngine','PresentationConfig','PresentationRules','CanvaDecks','AtlasQuests','AtlasPresentation']))
    for group,count in [('NPCs',6),('Visitors',2),('Pickups',5)]:
        groupref=next(r for r,n in names.items() if n==group)
        check(group+' count',sum(parent==groupref for parent in p.parents.values())==count)
    cf=p.property('Part','CFrame');sizes=p.property('Part','size')
    check('Every new Part is anchored',all(p.property('Part','Anchored')))
    check('Positive finite geometry',all(all(math.isfinite(x) and x>0 for x in size) for size in sizes))
    for c in cf:
        assert len(c)==12 and all(math.isfinite(v) for v in c)
        rows=[c[3:6],c[6:9],c[9:12]]
        for i in range(3):
            for j in range(3):assert abs(sum(rows[i][k]*rows[j][k] for k in range(3))-(1 if i==j else 0))<1e-5
    checks.append('Every generated CFrame rotation is orthonormal')
    # Verify real floors span all four bridge joins at the shared surface height.
    partnames=p.property('Part','Name')
    screens={n:s for n,s in zip(partnames,sizes) if n in ['ForumFront','AudienceScreen']}
    check('Two 16:9 presentation surfaces',len(screens)==2 and all(abs(s[0]/s[1]-16/9)<.001 for s in screens.values()))
    floors=[(c,s) for n,c,s in zip(partnames,cf,sizes) if n in ['Floor','Bridge']]
    def has_floor(x,z):
        return any(abs(x-c[0])<=s[0]/2+.001 and abs(z-c[2])<=s[2]/2+.001 and abs(c[1]+s[1]/2-4)<.001 for c,s in floors)
    check('All four walking routes have continuous floors',all(has_floor(x,z) for x,z in [(v,0) for v in range(-100,101)]+[(0,v) for v in range(-100,101)]))
    catalog=json.loads((ROOT/'src/catalog.json').read_text(encoding='utf-8'))
    check('Eight Drive collections and 35 document entries validate',validate(catalog)==(8,35))
    check('No synthetic project claims',all(not x['isSample'] and x['year'] is None and x['developmentStatus']=='documented' for x in catalog['projects']))
    inventory=json.loads((ROOT/'content/drive-inventory.json').read_text(encoding='utf-8'))
    nodes=inventory['items'];nodeids={x['id'] for x in nodes}
    check('Authorized Drive root only',inventory['rootId']=='1K0gYFMdzSQlvm8KyYz702XvYIg6pryh_')
    check('44 unique Drive nodes',len(nodes)==len(nodeids)==44)
    check('16 completely listed folders',sum(x['kind']=='folder' and x['listingComplete'] for x in nodes)==16)
    check('Drive parents resolve within inventory',all(x['parentId'] is None or x['parentId'] in nodeids for x in nodes))
    check('Five actual text previews; other files metadata only',sum(bool(x.get('previewText')) for x in nodes)==5 and all(not x.get('previewText') or x['mimeType']=='text/plain' for x in nodes))
    check('Shortcuts not traversed',all(not any(y['parentId']==x['id'] for y in nodes) for x in nodes if x['kind']=='shortcut'))
    for corruption in ['duplicate','private','category','date']:
        bad=copy.deepcopy(catalog)
        if corruption=='duplicate':bad['projects'][1]['id']=bad['projects'][0]['id']
        elif corruption=='private':bad['projects'][0]['visibility']='private'
        elif corruption=='category':bad['projects'][0]['category']='unknown'
        else:bad['projects'][0]['updatedAt']='invalid'
        rejected=False
        try:validate(bad)
        except (AssertionError,ValueError):rejected=True
        check('Catalog rejects '+corruption,rejected)
    values=[0,2,-1,78,2400,-1,33]
    check('Reference delta/zigzag roundtrip',read_refs(refs(values),len(values))==values)
    for t,values in [(1,['Hello','Museum — Indonesia']),(2,[True,False]),(3,[-20,0,1,200]),(4,[-.5,0,1.25]),(7,[(1,-12,.5,42)]),(14,[(1,2,3),(-3,0,.5)]),(16,[(1,2,3,1,0,0,0,1,0,0,0,1)]),(18,[0,256,1002]),(26,[(255,0,128),(0,42,255)])]:
        check('Typed property codec '+str(t),decode(t,encode(t,values),len(values))==values)
    check('BasePart budget under 1500',len(cf)+32<1500)
    result={'status':'PASS — structural/content checks only','checks':checks,'count':len(checks),'instances':len(p.parents),'baseParts':len(cf)+33,'fileBytes':(ROOT/'Place1.rbxl').stat().st_size,'sha256':hashlib.sha256((ROOT/'Place1.rbxl').read_bytes()).hexdigest(),'notTested':['Studio load/save','Luau compilation','Play interactions','Visual rendering','Mobile/gamepad','Two-player session','Performance']}
    (ROOT/'docs/validation-results.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='checks'},indent=2))
if __name__=='__main__':run()
