"""Author the Atlas modular 3D kit as real OBJ/MTL files. No external asset downloads."""
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_Project/Art/Models'
OUT.mkdir(parents=True, exist_ok=True)
PALETTE = {'Paper':'F5F2EA','Ink':'223549','Teal':'2F8F91','Sage':'9BB79B','Orange':'ECA35C',
           'Wood':'B78658','Purple':'8172C2','Leaf':'557B60','White':'FFFFFF','Screen':'152B38','Gold':'D6B66D'}

class Model:
    def __init__(self, name): self.name=name; self.vertices=[]; self.faces=[]
    def face(self, points, material):
        start=len(self.vertices)+1; self.vertices.extend(points)
        self.faces.append((material,list(range(start,start+len(points)))))
    def box(self, center, size, material, bevel=.035, yaw=0):
        h=[v/2 for v in size]; b=min(bevel,*[v*.35 for v in h]); inner=[v-b for v in h]
        def add(points):
            u=[points[1][i]-points[0][i] for i in range(3)]; v=[points[2][i]-points[0][i] for i in range(3)]
            normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
            centroid=[sum(p[i] for p in points)/len(points) for i in range(3)]
            if sum(normal[i]*centroid[i] for i in range(3))<0: points.reverse()
            c,s=math.cos(yaw),math.sin(yaw)
            self.face([(center[0]+p[0]*c+p[2]*s,center[1]+p[1],center[2]-p[0]*s+p[2]*c) for p in points],material)
        for axis in range(3):
            other=[i for i in range(3) if i!=axis]
            for sign in [-1,1]:
                points=[]
                for a,d in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                    p=[0,0,0]; p[axis]=h[axis]*sign; p[other[0]]=inner[other[0]]*a; p[other[1]]=inner[other[1]]*d; points.append(p)
                add(points)
        for free in range(3):
            a,d=[i for i in range(3) if i!=free]
            for sa in [-1,1]:
                for sd in [-1,1]:
                    points=[]
                    for t,swap in [(-1,False),(1,False),(1,True),(-1,True)]:
                        p=[0,0,0]; p[free]=inner[free]*t; p[a]=(inner[a] if swap else h[a])*sa; p[d]=(h[d] if swap else inner[d])*sd; points.append(p)
                    add(points)
        for x in [-1,1]:
            for y in [-1,1]:
                for z in [-1,1]:
                    sign=[x,y,z]; points=[]
                    for axis in range(3): points.append([(h[i] if i==axis else inner[i])*sign[i] for i in range(3)])
                    add(points)
    def cylinder(self,center,radius,height,material,segments=16,top=None):
        top=radius if top is None else top
        rings=[[(center[0]+r*math.cos(i*2*math.pi/segments),center[1]+dy,center[2]+r*math.sin(i*2*math.pi/segments)) for i in range(segments)] for r,dy in [(radius,-height/2),(top,height/2)]]
        self.face(rings[0],material); self.face(list(reversed(rings[1])),material)
        for i in range(segments): j=(i+1)%segments; self.face([rings[0][j],rings[0][i],rings[1][i],rings[1][j]],material)
    def sphere(self,center,scale,material,segments=10,rings=6):
        for j in range(rings):
            a=-math.pi/2+j*math.pi/rings; b=a+math.pi/rings
            for i in range(segments):
                points=[]
                for lat,lon in [(a,i),(b,i),(b,i+1),(a,i+1)]:
                    t=lon*2*math.pi/segments; points.append((center[0]+math.cos(lat)*math.cos(t)*scale[0],center[1]+math.sin(lat)*scale[1],center[2]+math.cos(lat)*math.sin(t)*scale[2]))
                self.face(points,material)
    def ring(self,center,radius,tube,material):
        for i in range(32):
            for j in range(6):
                points=[]
                for a,b in [(i,j),(i+1,j),(i+1,j+1),(i,j+1)]:
                    u=a*2*math.pi/32; v=b*2*math.pi/6
                    points.append((center[0]+(radius+tube*math.cos(v))*math.cos(u),center[1]+(radius+tube*math.cos(v))*math.sin(u),center[2]+tube*math.sin(v)))
                self.face(points,material)
    def save(self):
        lines=['# Project Atlas original procedural model. Units: metres. +Y up.', 'mtllib AtlasPalette.mtl','o '+self.name]
        lines += ['v %.6f %.6f %.6f'%v for v in self.vertices]
        lines.append('s off'); previous=None
        for material,indices in self.faces:
            if material!=previous: lines.append('usemtl '+material); previous=material
            lines.append('f '+' '.join(map(str,indices)))
        (OUT/(self.name+'.obj')).write_text('\n'.join(lines)+'\n',encoding='utf-8')
        return len(self.vertices),sum(len(face)-2 for _,face in self.faces)

def kit():
    models=[]
    def new(name): m=Model(name); models.append(m); return m
    m=new('SM_Bench')
    for x in [-.95,.95]: m.box((x,.22,0),(.15,.44,.65),'Ink')
    for z in [-.25,0,.25]: m.box((0,.48,z),(2.4,.16,.20),'Wood')
    for x in [-1.05,1.05]: m.box((x,.7,.3),(.12,.65,.12),'Ink')
    m.box((0,.95,.34),(2.3,.3,.12),'Wood')
    m=new('SM_Planter'); m.cylinder((0,.3,0),.35,.6,'Paper',16,.46); m.cylinder((0,.60,0),.4,.04,'Wood')
    for x,z,s in [(-.18,0,.65),(.16,.12,.85),(0,-.15,.70)]: m.sphere((x,.75+s*.4,z),(.30,s,.28),'Sage' if x<0 else 'Leaf')
    m=new('SM_Tree'); m.cylinder((0,.2,0),1.2,.4,'Paper',16); m.cylinder((0,1.35,0),.17,2.4,'Wood',8,.1)
    for x,y,z,s in [(0,3,0,1.3),(-.7,2.7,.2,.9),(.6,3.2,-.2,1),(0,3.8,.1,.8)]: m.sphere((x,y,z),(s,s*.8,s),'Sage' if x<.1 else 'Leaf')
    m=new('SM_Reception'); m.box((0,.53,0),(4.2,1.06,1.1),'Teal',.12); m.box((0,1.1,0),(4.45,.15,1.25),'Paper',.06)
    for x in [-1.6,-1.4,-1.2,-1,-.8,-.6,-.4,-.2,0,.2,.4,.6,.8,1,1.2,1.4,1.6]: m.box((x,.55,-.57),(.045,.7,.03),'Gold',.005)
    m=new('SM_Terminal'); m.box((0,.12,0),(1.2,.24,.85),'Ink'); m.box((0,.72,.12),(.28,1.2,.30),'Teal'); m.box((0,1.45,0),(1.4,.85,.15),'Ink',.06); m.box((0,1.45,-.09),(1.23,.66,.035),'Screen',.02)
    for y in [1.25,1.42,1.59]: m.box((0,y,-.113),(.92,.025,.01),'Sage',.002)
    m=new('SM_Portal');
    for x in [-1.8,1.8]: m.box((x,1.7,0),(.35,3.4,.55),'Teal',.1); m.box((x,1.7,-.29),(.08,3.1,.04),'Gold',.01)
    m.box((0,3.45,0),(3.95,.5,.55),'Teal',.1); m.box((0,.035,0),(3.8,.07,1.4),'Wood'); m.box((0,1.55,.16),(3.2,3.1,.12),'Screen')
    m=new('SM_ProjectStand'); m.box((0,.08,0),(5,.16,3.7),'Paper',.075); m.box((0,1.5,1.15),(4.7,2.8,.25),'Wood',.07); m.box((0,1.65,.985),(4.25,2.1,.13),'Ink',.05); m.box((0,1.7,.9),(3.95,1.82,.025),'Paper',.02); m.box((0,.48,.95),(4.1,.12,.08),'Teal')
    m=new('SM_Pedestal'); m.cylinder((0,.1,0),.9,.2,'Ink'); m.cylinder((0,.62,0),.7,1.05,'Paper'); m.cylinder((0,1.17,0),.8,.1,'Wood')
    m=new('SM_DocumentStand'); m.box((0,.07,0),(1.5,.14,1),'Ink'); m.box((0,.62,.15),(.5,1.1,.4),'Wood'); m.box((0,1.18,0),(1.7,.12,1.2),'Teal'); m.box((0,1.26,-.03),(1.30,.045,.86),'Paper',.01)
    m=new('SM_Bookshelf'); m.box((0,1.6,.34),(3.2,3.2,.12),'Wood')
    for x in [-1.55,1.55]: m.box((x,1.6,0),(.15,3.2,.8),'Wood')
    for y in [.12,.88,1.64,2.40,3.15]: m.box((0,y,0),(3.2,.12,.8),'Wood')
    for row in range(4):
        for col in range(12):
            h=.40+(col%3)*.06; m.box((-1.34+col*.24,.20+row*.76+h/2,.02),(.16,h,.45),['Teal','Paper','Purple','Orange','Sage'][((row+col)%5)],.012)
    m=new('SM_ReadingTable'); m.box((0,.82,0),(3,.18,1.3),'Wood',.07)
    for x in [-1.25,1.25]:
        for z in [-.45,.45]: m.box((x,.38,z),(.13,.76,.13),'Ink')
    m=new('SM_Chair'); m.box((0,.48,0),(.6,.16,.63),'Teal',.06); m.box((0,.86,.27),(.62,.7,.12),'Teal',.06)
    for x in [-.23,.23]:
        for z in [-.23,.23]: m.box((x,.22,z),(.06,.44,.06),'Ink',.012)
    m=new('SM_AuditoriumSeat'); m.box((0,.47,0),(.75,.20,.68),'Purple',.08); m.box((0,.97,.29),(.77,.95,.18),'Purple',.08); m.box((0,.22,0),(.4,.44,.45),'Ink');
    for x in [-.45,.45]: m.box((x,.65,.02),(.12,.13,.75),'Wood')
    m=new('SM_Stage'); m.box((0,.15,0),(12,.3,4),'Wood',.06); m.box((0,.31,-1.85),(11.8,.025,.08),'Gold',.01)
    m=new('SM_PresentationScreen'); m.box((0,3.5,0),(10.4,6,.30),'Ink',.12); m.box((0,3.5,-.19),(10,5.625,.05),'Paper',.02)
    for x in [-4.5,4.5]: m.box((x,.8,.1),(.3,1.6,.5),'Ink')
    m=new('SM_Podium'); m.box((0,.12,0),(.95,.24,.8),'Ink'); m.box((0,.8,0),(.75,1.4,.65),'Teal'); m.box((0,1.52,-.05),(1.2,.16,.9),'Wood')
    m=new('SM_Sculpture'); m.cylinder((0,.15,0),1,.3,'Paper'); m.box((0,.8,0),(1.1,1.3,1.1),'Ink'); m.ring((0,2.3,0),1.15,.11,'Gold'); m.sphere((0,2.3,0),(.62,.62,.62),'Teal',12,8)
    m=new('SM_Wayfinding'); m.box((0,.08,0),(.95,.16,.7),'Ink'); m.box((0,1.25,.08),(.10,2.5,.10),'Wood'); m.box((0,2,0),(2,1.4,.13),'Paper',.07)
    m=new('SM_Pendant'); m.cylinder((0,.9,0),.025,1.5,'Ink',8); m.cylinder((0,.12,0),.6,.24,'Teal',20,.35); m.cylinder((0,.015,0),.54,.015,'White',20)
    m=new('SM_WallModule'); m.box((0,2,0),(4,4,.22),'Paper',.015); m.box((0,.13,-.13),(4,.25,.04),'Wood',.01)
    m=new('SM_FloorTile'); m.box((0,-.075,0),(4,.15,4),'Paper',.025)
    m=new('SM_Pillar'); m.box((0,2.5,0),(.5,5,.5),'Paper',.07); m.box((0,.15,0),(.68,.3,.68),'Wood',.03)
    m=new('SM_GardenBed'); m.box((0,.20,0),(4,.4,2),'Paper',.1); m.box((0,.42,0),(3.7,.07,1.7),'Leaf',.02)
    for i in range(9): m.sphere((-1.5+(i%5)*.7,.65,(i//5)*.7-.35),(.35,.38,.33),'Sage' if i%2 else 'Leaf')
    m=new('SM_Bollard'); m.box((0,.45,0),(.20,.9,.20),'Ink'); m.box((0,.72,-.11),(.13,.24,.025),'White',.01)
    mtl=[]
    for name,color in PALETTE.items():
        rgb=[int(color[i:i+2],16)/255 for i in (0,2,4)]
        mtl += ['newmtl '+name,'Kd '+' '.join(f'{v:.6f}' for v in rgb),'Ka 0.15 0.15 0.15','Ks 0.05 0.05 0.05','Ns 12','']
    (OUT/'AtlasPalette.mtl').write_text('\n'.join(mtl),encoding='utf-8')
    stats=[]
    for m in models:
        verts,tris=m.save(); stats.append(f'{m.name}: {verts} vertices, {tris} triangles')
    (ROOT/'Docs/Evidence/model-inventory.txt').write_text('\n'.join(stats),encoding='utf-8')
    print(f'Created {len(models)} OBJ models and a shared MTL palette. All geometry authored locally.')

if __name__=='__main__': kit()
