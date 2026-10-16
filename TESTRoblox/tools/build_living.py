"""Native editable NPCs, mission props and shared presentation displays."""
def build(ctx):
    p=ctx['p'];model=ctx['model'];part=ctx['part'];ball=ctx['ball'];cylinder=ctx['cylinder'];prompt=ctx['prompt'];textpanel=ctx['textpanel'];world=ctx['world'];zones=ctx['zones']
    ink=ctx['INK'];white=ctx['WHITE'];gold=ctx['GOLD'];teal=ctx['TEAL']
    root=model('NPCs',world)
    specs=[('Nara','PEMANDU',(-10,4,22),teal),('Bima','TEKNISI',(-61,4,24),ctx['BLUE']),('Tika','ARSIP',(73,4,23),gold),('Sari','TAMAN',(10,4.4,87),(108,184,113)),('Dimas','PRESENTASI',(31,4,-116),ctx['PURPLE']),('Lumi','REWARD',(19,4,-18),ctx['CORAL'])]
    for name,role,(x,y,z),col in specs:
        npc=model(name,root)
        for dx in [-.6,.6]:
            part('Leg',(x+dx,y+1,z),(.8,1.9,.85),ink,parent=npc,collide=False)
            part('Shoe',(x+dx,y+.25,z+.2),(1,.5,1.3),white,parent=npc,collide=False)
        part('Torso',(x,y+3.1,z),(2.5,2.4,1.3),col,parent=npc,collide=False)
        for dx in [-1.7,1.7]:
            part('Arm',(x+dx,y+3.05,z),(.75,2.2,.85),col,parent=npc,collide=False)
            ball('Hand',(x+dx,y+1.9,z),.42,(234,187,145),parent=npc)
        head=ball('Head',(x,y+5.1,z),1,(234,187,145),parent=npc)
        cylinder('Cap',(x,y+5.95,z),1.12,.35,col,parent=npc,collide=False)
        for dx in [-.35,.35]:ball('Eye',(x+dx,y+5.2,z+.9),.12,ink,parent=npc)
        part('Smile',(x,y+4.75,z+.92),(.55,.1,.1),ink,parent=npc,collide=False)
        ball('Badge',(x+.65,y+3.4,z+.7),.22,gold,'neon',npc)
        textpanel('Nameplate',name+' / '+role,(x,y+7.6,z),(9,1.8),ink,col,parent=npc)
        prompt(head,'NPC_'+name,'Bicara',name+' · '+role)
    board=textpanel('MissionBoard','MISI HARI INI\nBantu NPC / kumpulkan inspirasi',(-19,10,27),(15,5),ink,teal,ry=180,parent=zones['Atrium'])
    prompt(board,'Missions','Lihat misi','Petualangan RISTEK')
    pickups=model('Pickups',world)
    for i,(x,y,z) in enumerate([(-12,5.5,84),(-27,5.5,103),(26,5.5,98),(8,5.5,120),(25,5.5,136)],1):
        item=model('Recycle'+str(i),pickups)
        cube=part('Item',(x,y,z),(1.3,1.5,1.3),teal,'neon',item,rot=(i*24,0,0),collide=False)
        prompt(cube,'Collect_'+str(i),'Kumpulkan','Sampah daur ulang '+str(i))
        cylinder('Marker',(x,4.75,z),1.6,.1,gold,'neon',item,False)
    # Visitors drift locally along short paths; no collision or network physics load.
    visitors=model('Visitors',world)
    for i,(x,z) in enumerate([(-18,-12),(17,7)],1):
        v=model('Visitor'+str(i),visitors)
        part('Body',(x,7,z),(2,3,1.2),ctx['BLUE'] if i==1 else ctx['CORAL'],parent=v,collide=False)
        ball('Head',(x,9.1,z),.9,gold,parent=v)
        for dx in [-.55,.55]:part('Leg',(x+dx,4.9,z),(.7,1.2,.8),ink,parent=v,collide=False)
    # Ensure the main surface is a 16:9 screen visible from audience (+Z side).
    for obj in p.added:
        if obj['props']['Name'][1]=='ForumFront':obj['props']['size']=(14,(48,27,.35))
        if obj['props']['Name'][1]=='ForumSign':continue
    side=textpanel('AudienceScreen','PRESENTASI BERSAMA\nGunakan podium untuk mulai',(-37.8,15,-108),(24,13.5),ink,white,ry=90,parent=zones['Auditorium'])
    prompt(side,'AudienceView','Perbesar slide','Layar penonton')
