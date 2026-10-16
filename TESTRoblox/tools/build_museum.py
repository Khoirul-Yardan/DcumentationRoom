"""Build the editable museum into the supplied template, retaining its services.
Run: python tools/build_museum.py. Output is genuine binary RBXL, not renamed XML.
"""
import json, math, random, hashlib
from pathlib import Path
from rbxl import Place

ROOT=Path(__file__).resolve().parent.parent
random.seed(37)
p=Place(ROOT/'backups/Place1.original.rbxl')
WHITE=(235,232,215); INK=(27,43,58); TEAL=(61,206,190); GOLD=(242,185,85)
BLUE=(94,166,228); CORAL=(236,133,115); PURPLE=(166,145,224); GREEN=(73,134,99)
MAT={'smooth':256,'neon':288,'wood':512,'marble':784,'concrete':816,'glass':1568,'metal':1088,'grass':1280}
I=(1,0,0,0,1,0,0,0,1)
def cf(pos,ry=0,rx=0,rz=0):
    a,b,c=map(math.radians,(rx,ry,rz));ca,sa,cb,sb,cc,sc=math.cos(a),math.sin(a),math.cos(b),math.sin(b),math.cos(c),math.sin(c)
    return tuple(pos)+(cb*cc, -cb*sc, sb, ca*sc+sa*sb*cc,ca*cc-sa*sb*sc,-sa*cb,sa*sc-ca*sb*cc,sa*cc+ca*sb*sc,ca*cb)
def folder(name,parent):return p.add('Folder',name,parent)
def model(name,parent):return p.add('Model',name,parent)
world=model('AtlasMuseum',p.service('Workspace'))
zones={name:model(name,world) for name in ['Atrium','Gallery','Archive','Auditorium','Garden','Landscape','Bridges']}
def part(name,pos,size,color=WHITE,mat='smooth',parent=None,rot=(0,0,0),shape=1,collide=True,alpha=0,ref=None):
    return p.add('Part',name,parent if parent is not None else world,{
        'CFrame':(16,cf(pos,*rot)),'size':(14,size),'Color3uint8':(26,color),
        'Material':(18,MAT[mat]),'Anchored':(2,True),'CanCollide':(2,collide),
        'CanTouch':(2,False),'CanQuery':(2,True),'Transparency':(4,alpha),
        'CastShadow':(2,mat!='neon' and alpha==0),'shape':(18,shape),
        'TopSurface':(18,0),'BottomSurface':(18,0),'Reflectance':(4,0),
    },ref)
def cylinder(name,pos,r,h,color=WHITE,mat='smooth',parent=None,collide=True):
    return part(name,pos,(h,2*r,2*r),color,mat,parent,(0,0,90),2,collide)
def ball(name,pos,r,color,mat='smooth',parent=None):return part(name,pos,(r*2,r*2,r*2),color,mat,parent,shape=0,collide=False)
def textpanel(name,text,pos,size,color=INK,accent=WHITE,ry=0,parent=None,font=19):
    board=part(name,pos,(size[0],size[1],.35),color,parent=parent,rot=(ry,0,0),collide=False)
    gui=p.add('SurfaceGui','Sign',board,{'Face':(18,2),'CanvasSize':(13,(size[0]*45,size[1]*45)), 'LightInfluence':(4,0),'AlwaysOnTop':(2,False)})
    p.add('TextLabel','Text',gui,{'Size':(7,(1,0,1,0)),'Position':(7,(0,0,0,0)),
        'BackgroundTransparency':(4,1),'Text':(1,text),'TextColor3':(12,tuple(x/255 for x in accent)),
        'TextScaled':(2,True),'TextWrapped':(2,True),'Font':(18,font),'TextSize':(4,32),
        'BorderSizePixel':(3,0),'RichText':(2,False)})
    return board
def prompt(parent,name,action,label):
    return p.add('ProximityPrompt',name,parent,{'ActionText':(1,action),'ObjectText':(1,label),
        'MaxActivationDistance':(4,12),'RequiresLineOfSight':(2,True),'HoldDuration':(4,0),
        'KeyboardKeyCode':(18,101),'GamepadKeyCode':(18,1002),'ClickablePrompt':(2,True)})
def ring(name,center,r,color,parent,tilt=0,n=48,width=.3):
    cx,cy,cz=center;angle=math.radians(tilt)
    for i in range(n):
        t=2*math.pi*i/n
        # Horizontal segmented ring, optionally tilted about X.
        pos=(cx+r*math.cos(t),cy+r*math.sin(t)*math.sin(angle),cz+r*math.sin(t)*math.cos(angle))
        part(name,pos,(2*math.pi*r/n+.12,width,width),color,'neon',parent,(0,tilt,math.degrees(t)),collide=False)
def beam_between(name,a,b,width,color,mat,parent):
    dx,dy,dz=(b[i]-a[i] for i in range(3));length=math.sqrt(dx*dx+dy*dy+dz*dz)
    # A beam aligned along local Y: supply explicit orthonormal matrix.
    uy=(dx/length,dy/length,dz/length);helper=(0,0,1) if abs(uy[2])<.9 else (1,0,0)
    ux=(uy[1]*helper[2]-uy[2]*helper[1],uy[2]*helper[0]-uy[0]*helper[2],uy[0]*helper[1]-uy[1]*helper[0]);l=math.sqrt(sum(x*x for x in ux));ux=tuple(x/l for x in ux)
    uz=(ux[1]*uy[2]-ux[2]*uy[1],ux[2]*uy[0]-ux[0]*uy[2],ux[0]*uy[1]-ux[1]*uy[0])
    ref=part(name,tuple((a[i]+b[i])/2 for i in range(3)),(width,length,width),color,mat,parent,collide=False)
    p.added[-1]['props']['CFrame']=(16,tuple((a[i]+b[i])/2 for i in range(3))+(ux[0],uy[0],uz[0],ux[1],uy[1],uz[1],ux[2],uy[2],uz[2]))
    return ref
def orbit(center,r,color,parent,tilt=0):
    a=math.radians(tilt);cx,cy,cz=center
    pts=[(cx+r*math.cos(t),cy+r*math.sin(t)*math.sin(a),cz+r*math.sin(t)*math.cos(a)) for t in [2*math.pi*i/48 for i in range(49)]]
    for j in range(48):beam_between('Orbit',pts[j],pts[j+1],.22,color,'neon',parent)
def bench(pos,parent,ry=0):
    x,y,z=pos;m=model('Bench',parent)
    part('Seat',(x,y+1.4,z),(9,.5,2.5),GOLD,'wood',m,rot=(ry,0,0))
    for dx in [-3.3,3.3]:part('Leg',(x+dx,y+.65,z),(.5,1.3,2),INK,'metal',m)
    part('Back',(x,y+2.6,z+1),(9,2,.4),GOLD,'wood',m)
def tree(pos,parent,scale=1):
    x,y,z=pos;m=model('CanopyTree',parent)
    cylinder('Planter',(x,y+.5,z),3.6*scale,1,WHITE,'concrete',m)
    cylinder('Soil',(x,y+1.1,z),3.2*scale,.3,(61,75,54),'grass',m)
    cylinder('Trunk',(x,y+4*scale,z),.7*scale,7*scale,(108,83,61),'wood',m)
    for dx,dy,dz,r in [(-2,8,0,3.5),(2,9,0,3.7),(0,11,1,3.8),(0,8,-2,3)]:
        ball('Foliage',(x+dx*scale,y+dy*scale,z+dz*scale),r*scale,GREEN if dx<0 else (100,163,119),parent=m)
def lamp(pos,parent,color=GOLD):
    x,y,z=pos;cylinder('LampStem',(x,y+3,z),.15,6,INK,'metal',parent)
    head=ball('Lamp',(x,y+6,z),.65,color,'neon',parent)
    p.add('PointLight','WarmLight',head,{'Color':(12,tuple(c/255 for c in color)),'Brightness':(4,.7),'Range':(4,15),'Shadows':(2,False)})
def platform(zone,x,z,w,d,accent):
    par=zones[zone]
    part('FloatingFoundation',(x,.8,z),(w+3,5,d+3),INK,'concrete',par)
    part('Floor',(x,3.5,z),(w,1,d),WHITE,'marble',par)
    for side in [-1,1]:
        part('Inlay',(x+side*(w/2-.8),4.03,z),(.18,.08,d-1),accent,'neon',par,collide=False)
        part('Inlay',(x,4.03,z+side*(d/2-.8)),(w-1,.08,.18),accent,'neon',par,collide=False)
def doorwall(parent,x,z,w,h,door=16,ry=0):
    # North/south walls, center opening.
    for s in [-1,1]:part('Wall',(x+s*(w+door)/4,4+h/2,z),((w-door)/2,h,1),WHITE,'concrete',parent)
    part('Lintel',(x,4+h-2,z),(door,4,1),WHITE,'concrete',parent)
def gallery_stand(i,x,z,project,ry=0):
    m=model(f'Stand{i:02d}',zones['Gallery']);color=tuple(project['color'])
    cylinder('Plinth',(x,4.55,z),4.9,1.1,INK,'smooth',m)
    cylinder('Halo',(x,5.12,z),4.65,.12,color,'neon',m,False)
    part('Monolith',(x,8.7,z-1.8),(10,7,.7),INK,parent=m)
    textpanel('Title',f'{i:02d}  /  {project["title"]}\nARSIP DRIVE RISTEK',(x,10.4,z-1.35),(9.3,2.4),INK,color,parent=m)
    textpanel('Category',project['category'].upper(),(x,7.9,z-1.33),(9.3,1),INK,WHITE,parent=m)
    # Different abstract miniatures; all editable native Roblox geometry.
    if i%4==1:
        for j,h in enumerate([1.5,3,4,2.4]):part('DataTower',(x-2.5+j*1.6,5.4+h/2,z+1),(1.1,h,1.1),color,'glass',m,alpha=.15)
    elif i%4==2:
        for j in range(3):part('Module',(x,6+j*1.25,z+1),(4-j,.6,3-j*.4),color,'metal',m,rot=(j*30,0,0))
    elif i%4==3:
        ball('Planet',(x,7,z+1),1.65,color,parent=m);orbit((x,7,z+1),2.5,GOLD,m,35)
    else:
        for j in range(5):ball('Node',(x+math.cos(j*1.26)*2,6.7,z+1+math.sin(j*1.26)),.55,color,'neon',m)
    target=part('Interact',(x,6.1,z+5.4),(6,3,.4),WHITE,parent=m,collide=False,alpha=1)
    prompt(target,f'Project_{i}','Lihat proyek',project['title'])

catalog=json.loads((ROOT/'src/catalog.json').read_text(encoding='utf-8'))
# Rebuild only the template baseplate's class, keeping its referent and children.
p.replace_classes.add('Part')
part('Baseplate',(0,-12,0),(2048,4,2048),(65,122,143),'smooth',zones['Landscape'],ref=p.service('Part'))
part('Sea',(0,-8.5,0),(1600,.3,1600),(74,147,164),'glass',zones['Landscape'],alpha=.22)
for name,x,z,w,d,col in [('Atrium',0,0,66,66,TEAL),('Gallery',-106,0,100,72,BLUE),('Archive',106,0,76,72,GOLD),('Auditorium',0,-106,80,72,PURPLE),('Garden',0,106,78,72,CORAL)]:platform(name,x,z,w,d,col)
# Short bridges, flush with floor; glass balustrades.
for x,z,w,d,col in [(-44.5,0,23,16,BLUE),(50.5,0,35,16,GOLD),(0,-51.5,16,37,PURPLE),(0,51.5,16,37,CORAL)]:
    par=zones['Bridges'];part('Bridge',(x,3.5,z),(w,1,d),WHITE,'concrete',par)
    for s in [-1,1]:
        if w>d:
            part('GlassRail',(x,6,z+s*7.5),(w,4,.35),BLUE,'glass',par,alpha=.6)
            part('Handrail',(x,8.1,z+s*7.5),(w,.2,.4),col,'neon',par)
        else:
            part('GlassRail',(x+s*7.5,6,z),(.35,4,d),BLUE,'glass',par,alpha=.6)
            part('Handrail',(x+s*7.5,8.1,z),(.4,.2,d),col,'neon',par)
# Atrium / open rotunda with clear cardinal exits.
a=zones['Atrium']
cylinder('CompassBase',(0,4.3,0),10,.6,INK,'marble',a)
cylinder('SculpturePlinth',(0,5.2,0),4.8,1.5,WHITE,'marble',a)
ball('AtlasCore',(0,13,0),3.3,TEAL,'glass',a)
for radius,tilt,col in [(6,25,GOLD),(7,75,TEAL),(8,-45,BLUE)]:orbit((0,13,0),radius,col,a,tilt)
for angle in range(0,360,45):
    t=math.radians(angle+22.5);x,z=29*math.cos(t),29*math.sin(t)
    cylinder('RotundaColumn',(x,16,z),.75,24,WHITE,'concrete',a)
    cylinder('Capital',(x,28,z),1.25,.6,GOLD,'metal',a)
orbit((0,28,0),29,TEAL,a)
orbit((0,29,0),30,WHITE,a)
for x in [-24,24]:
    for z in [-24,24]:tree((x,4,z),a,.65)
textpanel('Welcome','A T L A S\nMUSEUM KEPULAUAN IDE',(0,23,-23),(26,7),INK,WHITE,parent=a)
textpanel('Subtitle','Jelajahi karya. Temukan proses. Simpan inspirasi.',(0,18.4,-23.1),(25,1.5),INK,TEAL,parent=a)
terminal=textpanel('Directory','DIREKTORI\nBuka katalog proyek',(14,8,16),(9,5),INK,TEAL,parent=a)
prompt(terminal,'Catalog','Buka katalog','Direktori ATLAS')
textpanel('GallerySign','01  GALERI\n<  Karya & eksperimen',(-22,13,9),(16,4),INK,BLUE,parent=a)
textpanel('ArchiveSign','02  ARSIP\nDokumentasi & proses  >',(22,13,9),(16,4),INK,GOLD,parent=a)
textpanel('ShowSign','03  FORUM\nPresentasi / arah utara',(0,11,-28),(18,3),INK,PURPLE,parent=a)
bench((-17,4,18),a);lamp((-27,4,18),a)
# Gallery: tall glass pavilion, eight physical slots, generous central aisle.
g=zones['Gallery']
doorwall(g,-106,-35,100,22)
doorwall(g,-106,35,100,22)
part('WestWall',(-155,15,0),(1,22,72),WHITE,'concrete',g)
for z in [-22,22]:part('EastGlass',(-57,14,z),(1,20,28),BLUE,'glass',g,alpha=.65)
for x in [-147,-127,-106,-85,-65]:
    part('RoofBeam',(x,27,0),(.8,1,72),WHITE,'metal',g)
part('GlassRoof',(-106,27.5,0),(100,.4,72),BLUE,'glass',g,alpha=.85)
for z in [-35,35]:part('RoofRim',(-106,26.5,z),(100,1,1),BLUE,'neon',g)
textpanel('GalleryTitle','01  /  GALERI INOVASI',(-106,20,-34.3),(54,4),INK,BLUE,parent=g)
# All exhibit panels face south so titles are readable from both broad aisles.
for i,proj in enumerate(catalog['projects'][:8],1):
    x=-141+((i-1)%4)*23;z=-19 if i<=4 else 16
    gallery_stand(i,x,z,proj)
for x in [-144,-68]:tree((x,4,30),g,.55)
for name,text,x in [('PagePrev','<  HALAMAN',-121),('PageNext','HALAMAN  >',-91)]:
    b=textpanel(name,text,(x,7,32),(14,3),INK,BLUE,ry=180,parent=g);prompt(b,name,'Ganti halaman','Galeri pribadi')
# Archive: bookshelves, reading desks, a physical process timeline.
r=zones['Archive']
part('BackWall',(106,15,-35),(76,22,1),WHITE,'concrete',r)
part('EastWall',(143,15,0),(1,22,72),WHITE,'concrete',r)
for z in [-35,35]:part('TimberBeam',(106,25,z),(76,1,1.5),GOLD,'wood',r)
for x in [73,94,117,139]:part('TimberRafter',(x,25,0),(1,1,70),GOLD,'wood',r)
for x in [80,105,130]:
    m=model('ArchiveShelf',r)
    for y in [5,9,13,17]:part('Shelf',(x,y,-24),(17,.5,4),INK,'wood',m)
    for dx in [-8,8]:part('Frame',(x+dx,11,-24),(.5,12,4),INK,'wood',m)
    for level in range(3):
        for j in range(12):
            h=random.uniform(2,3.2);color=random.choice([TEAL,GOLD,BLUE,CORAL,PURPLE,WHITE])
            part('Book',(x-7+j*1.2,5.25+level*4+h/2,-24),(.85,h,2.7),color,'smooth',m)
for x in [87,122]:
    part('ReadingDesk',(x,7,9),(17,.8,9),GOLD,'wood',r)
    for dx in [-6,6]:part('DeskLeg',(x+dx,5.5,9),(.6,3,7),INK,'metal',r)
    b=textpanel('ArchiveTerminal','ARSIP TERKURASI\nCari proyek & dokumentasi',(x,10,7),(10,4),INK,GOLD,parent=r)
    prompt(b,'Archive','Telusuri','Arsip & proses')
    bench((x,4,15),r)
textpanel('ArchiveTitle','02  /  PERPUSTAKAAN PROSES',(106,21,-34.3),(61,4),INK,GOLD,parent=r)
for i,txt in enumerate(['PROJECT','TECH INSIGHT','MINI SHOWCASE','TECH CORNER','ADMINISTRASI']):
    x=76+i*15;textpanel('Timeline',txt,(x,8,32),(14,5),INK,GOLD,ry=180,parent=r)
tree((137,4,25),r,.7)
# Auditorium / forum with concentric stage light and usable chairs.
s=zones['Auditorium']
part('BackWall',(0,17,-141),(80,26,1),INK,'concrete',s)
for x in [-39,39]:
    part('SideWall',(x,13,-106),(1,18,72),WHITE,'concrete',s)
    for z in [-130,-110,-90]:part('AcousticFin',(x*.97,15,z),(1,20,5),PURPLE,'wood',s)
part('Stage',(0,4.7,-128),(56,1.4,18),INK,'wood',s)
part('StageLight',(0,5.45,-119),(56,.15,.4),PURPLE,'neon',s)
b=textpanel('PresentationScreen','ATLAS / FORUM\nSETIAP KARYA PUNYA CERITA\n\n[E] Buka presentasi contoh',(0,17,-139.8),(48,19),INK,WHITE,ry=180,parent=s)
# Front-facing second panel sits in front of the screen for south-side audience.
b=textpanel('ForumFront','ATLAS / FORUM\nSETIAP KARYA PUNYA CERITA',(0,17,-139),(48,19),INK,WHITE,parent=s)
console=textpanel('Podium','MULAI PRESENTASI',(24,8,-119),(9,3),INK,PURPLE,parent=s);prompt(console,'Showcase','Lihat koleksi','Forum ATLAS')
for row in range(4):
    for col in range(8):
        x=(col-3.5)*7+(2 if col>=4 else -2);z=-108+row*8
        seat=p.add('Seat','AudienceSeat',s,{'CFrame':(16,cf((x,5.5,z))),'size':(14,(4,1,4)),'Anchored':(2,True),'Color3uint8':(26,PURPLE),'Material':(18,256),'CanCollide':(2,True),'TopSurface':(18,0),'BottomSurface':(18,0)})
        part('SeatBack',(x,7.3,z+1.7),(4,3,.6),INK,parent=s)
textpanel('ForumSign','03 / FORUM',(0,14,-72),(24,4),INK,PURPLE,parent=s)
# Garden with lotus pond, pergola, tactile stepping stones and credits.
c=zones['Garden']
part('GardenBed',(0,4.15,106),(72,.3,66),(112,150,117),'grass',c)
part('Path',(0,4.4,106),(12,.3,68),WHITE,'concrete',c)
part('CrossPath',(0,4.4,107),(72,.3,10),WHITE,'concrete',c)
cylinder('PondBasin',(-20,4.6,120),10,1,INK,'concrete',c)
cylinder('ReflectingWater',(-20,5.14,120),9.3,.15,BLUE,'glass',c,False)
for j in range(6):
    t=j*1.047;x=-20+6*math.cos(t);z=120+6*math.sin(t)
    cylinder('LilyPad',(x,5.3,z),1.5,.15,TEAL,parent=c,collide=False)
    for k in range(5):ball('LotusPetal',(x+math.cos(k*1.256)*.55,5.6,z+math.sin(k*1.256)*.55),.5,CORAL,parent=c)
for x,z in [(-28,82),(28,82),(29,129),(-30,132)]:tree((x,4.3,z),c,.9)
for x,z in [(20,112),(20,131),(-20,96)]:bench((x,4.4,z),c)
for x in [11,30]:
    for z in [111,133]:part('PergolaPost',(x,10,z),(.7,12,.7),GOLD,'wood',c)
for z in range(111,134,3):part('PergolaSlat',(20.5,16,z),(23,.6,1),GOLD,'wood',c)
credits=textpanel('Credits','04 / TAMAN IDE\nBangun. Bagikan. Bertumbuh.\n\nBantu Sari merawat taman.\nKumpulkan Token Inspirasi!',(0,10,139),(27,9),INK,CORAL,ry=180,parent=c)
prompt(credits,'Credits','Tentang museum','Taman Ide')
for x,z in [(-32,100),(32,100),(-7,78),(7,134)]:lamp((x,4,z),c)
# Island perimeter safety rails with openings only toward bridges.
for zone,x,z,w,d,col in [('Atrium',0,0,66,66,TEAL),('Gallery',-106,0,100,72,BLUE),('Archive',106,0,76,72,GOLD),('Auditorium',0,-106,80,72,PURPLE),('Garden',0,106,78,72,CORAL)]:
    par=zones[zone]
    for side in [-1,1]:
        for off in [-1,1]:
            part('EdgeRail',(x+side*w/2,5.5,z+off*(d+18)/4),(.5,3,(d-18)/2),INK,'metal',par)
            part('EdgeRail',(x+off*(w+18)/4,5.5,z+side*d/2),((w-18)/2,3,.5),INK,'metal',par)
    openings={'Atrium':{'W','E','N','S'},'Gallery':{'E'},'Archive':{'W'},'Auditorium':{'S'},'Garden':{'N'}}[zone]
    for direction,dx,dz,sx,sz in [('W',-w/2,0,.5,18),('E',w/2,0,.5,18),('N',0,-d/2,18,.5),('S',0,d/2,18,.5)]:
        if direction not in openings:part('EdgeRail',(x+dx,5.5,z+dz),(sx,3,sz),INK,'metal',par)
# Far islands, native rocky silhouettes.
for x,z,radius in [(-180,130,20),(165,145,25),(-155,-165,18),(145,-170,24)]:
    cylinder('DistantIsland',(x,-4,z),radius,10,(77,112,108),'concrete',zones['Landscape'])
    cylinder('DistantGrass',(x,1.2,z),radius-1,.6,GREEN,'grass',zones['Landscape'])
    tree((x,1.5,z),zones['Landscape'],1.4)
# Configure original services, preserving all unrelated serialized data.
p.override('SpawnLocation','CFrame',16,[cf((0,4.3,22))]);p.override('SpawnLocation','size',14,[(7,.5,7)])
p.override('SpawnLocation','Transparency',4,[1]);p.override('SpawnLocation','Duration',3,[0])
p.override('Workspace','StreamingEnabled',2,[False])
p.override('Decal','Transparency',4,[1]);p.override('Texture','Transparency',4,[1])
for name,t,v in [('TimeOfDay',1,'15:15:00'),('Brightness',4,2.4),('Ambient',12,(.38,.43,.48)),('OutdoorAmbient',12,(.55,.6,.65)),('ExposureCompensation',4,.15),('GlobalShadows',2,True)]:p.override('Lighting',name,t,[v])
p.override('Atmosphere','Density',4,[.23]);p.override('Atmosphere','Haze',4,[1.3]);p.override('BloomEffect','Intensity',4,[.25]);p.override('BloomEffect','Size',4,[28]);p.override('BloomEffect','Threshold',4,[1.8]);p.override('DepthOfFieldEffect','Enabled',2,[False])
for name,t,v in [('CharacterWalkSpeed',4,18),('CameraMinZoomDistance',4,.5),('CameraMaxZoomDistance',4,28),('CameraMode',18,0)]:p.override('StarterPlayer',name,t,[v])
# Editor camera, looking from the south-east toward the complete campus.
def lookat(eye,target):
    z=tuple(eye[i]-target[i] for i in range(3));l=math.sqrt(sum(v*v for v in z));z=tuple(v/l for v in z)
    x=(z[2],0,-z[0]);l=math.sqrt(sum(v*v for v in x));x=tuple(v/l for v in x)
    y=(z[1]*x[2]-z[2]*x[1],z[2]*x[0]-z[0]*x[2],z[0]*x[1]-z[1]*x[0])
    return tuple(eye)+(x[0],y[0],z[0],x[1],y[1],z[1],x[2],y[2],z[2])
p.override('Camera','CFrame',16,[lookat((185,170,235),(0,5,0))]);p.override('Camera','Focus',16,[cf((0,5,0))])
shared=folder('Atlas',p.service('ReplicatedStorage'))
for remote in ['Travel','Quest','Presentation']:p.add('RemoteEvent',remote,shared)
for name,file in [('Catalog','Catalog.luau'),('CatalogService','CatalogService.luau'),('DriveArchive','DriveArchive.luau'),('GameConfig','GameConfig.luau'),('FeatureUI','FeatureUI.luau')]:
    p.add('ModuleScript',name,shared,{'Source':(1,(ROOT/'src'/file).read_text(encoding='utf-8'))})
for name in ['QuestEngine','PresentationConfig','PresentationRules','CanvaDecks']:
    p.add('ModuleScript',name,p.service('ServerScriptService'),{'Source':(1,(ROOT/'src'/(name+'.luau')).read_text(encoding='utf-8'))})
p.add('ModuleScript','AtlasValidation',p.service('ServerStorage'),{'Source':(1,(ROOT/'src/StudioValidation.luau').read_text(encoding='utf-8'))})
p.add('Script','AtlasServer',p.service('ServerScriptService'),{'Source':(1,(ROOT/'src/Server.server.luau').read_text(encoding='utf-8')),'Disabled':(2,False)})
for name,file in [('AtlasQuests','Quests.server.luau'),('AtlasPresentation','Presentation.server.luau')]:
    p.add('Script',name,p.service('ServerScriptService'),{'Source':(1,(ROOT/'src'/file).read_text(encoding='utf-8')),'Disabled':(2,False)})
p.add('LocalScript','AtlasClient',p.service('StarterPlayerScripts'),{'Source':(1,(ROOT/'src/Client.client.luau').read_text(encoding='utf-8')),'Disabled':(2,False)})
from build_living import build as build_living
build_living(globals())
p.save(ROOT/'Place1.rbxl')
manifest={k:sum(1 for obj in p.added if obj['class']==k) for k in sorted(set(obj['class'] for obj in p.added))}
(ROOT/'docs/build-manifest.json').write_text(json.dumps({'output':'Place1.rbxl','sha256':hashlib.sha256((ROOT/'Place1.rbxl').read_bytes()).hexdigest(),'generated':manifest,'driveCollections':len(catalog['projects']),'preservedOriginalInstances':len(p.parents),'geometry':[{ 'name':o['props']['Name'][1],'position':o['props']['CFrame'][1][:3],'size':o['props']['size'][1]} for o in p.added if o['class']=='Part']},indent=2),encoding='utf-8')
print('Built Place1.rbxl',manifest)
