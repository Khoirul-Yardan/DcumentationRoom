"""Technical geometry preview, not a Roblox Studio screenshot."""
import math
from pathlib import Path
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from rbxl import Place

ROOT=Path(__file__).resolve().parent.parent
p=Place(ROOT/'Place1.rbxl')
faces=[];colors=[]
def polygons(size,shape):
    sx,sy,sz=[v/2 for v in size]
    if shape==0:
        def pt(a,b):return (sx*math.cos(a)*math.cos(b),sy*math.sin(a),sz*math.cos(a)*math.sin(b))
        out=[]
        for i in range(6):
            a=-math.pi/2+i*math.pi/6;a2=a+math.pi/6
            for j in range(10):
                b=j*math.pi/5;b2=b+math.pi/5;out.append([pt(a,b),pt(a,b2),pt(a2,b2),pt(a2,b)])
        return out
    if shape==2:
        a=[(-sx,sy*math.cos(t*math.pi/8),sz*math.sin(t*math.pi/8)) for t in range(16)]
        b=[(sx,v[1],v[2]) for v in a]
        return [a,b]+[[a[i],a[(i+1)%16],b[(i+1)%16],b[i]] for i in range(16)]
    v=[(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),(-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]
    return [[v[i] for i in f] for f in [(0,1,2,3),(4,5,6,7),(0,4,7,3),(1,5,6,2),(0,1,5,4),(3,2,6,7)]]

for cls in ['Part','Seat']:
    names=p.property(cls,'Name');cframes=p.property(cls,'CFrame');sizes=p.property(cls,'size');rgb=p.property(cls,'Color3uint8')
    shapes=p.property(cls,'shape') if cls=='Part' else [1]*len(names)
    alphas=p.property(cls,'Transparency') if cls=='Part' else [0]*len(names)
    for name,c,size,color,shape,alpha in zip(names,cframes,sizes,rgb,shapes,alphas):
        if name in ['Baseplate','Sea','Interact','GlassRoof'] or alpha>=.9:continue
        rotation=np.array(c[3:]).reshape(3,3);position=np.array(c[:3])
        for poly in polygons(size,shape):
            world=np.array(poly)@rotation.T+position
            # Matplotlib Z is vertical; Roblox Y is vertical.
            face=world[:,[0,2,1]]
            normal=np.cross(face[1]-face[0],face[2]-face[0]);norm=np.linalg.norm(normal)
            shade=.8+.2*abs(normal[2]/norm) if norm>0 else 1
            faces.append(face);colors.append(tuple(v/255*shade for v in color)+(1-alpha*.7,))
fig=plt.figure(figsize=(17,12),facecolor='#d6e6e5')
ax=fig.add_subplot(111,projection='3d',computed_zorder=False);ax.set_facecolor('#d6e6e5')
ax.add_collection3d(Poly3DCollection(faces,facecolors=colors,edgecolors='none',zsort='average'))
ax.set_xlim(-170,170);ax.set_ylim(-150,150);ax.set_zlim(-8,80);ax.set_box_aspect((340,300,130));ax.view_init(elev=42,azim=58);ax.set_proj_type('ortho');ax.set_axis_off()
fig.text(.06,.94,'ATLAS / MUSEUM KEPULAUAN IDE',fontsize=23,fontweight='bold',color='#1b2b3a')
fig.text(.06,.905,'Pratinjau geometri dari Place1.rbxl  |  Bukan screenshot atau hasil render Roblox Studio',fontsize=11,color='#526a75')
fig.text(.06,.06,'01 GALERI       02 ARSIP       03 FORUM       04 TAMAN\nLima pulau · delapan stan · aset native Roblox · seluruh konten berlabel contoh',fontsize=12,color='#1b2b3a',linespacing=1.8)
plt.subplots_adjust(left=0,right=1,bottom=.07,top=.9)
fig.savefig(ROOT/'docs/museum-geometry-preview.png',dpi=130,facecolor=fig.get_facecolor())
print('Saved technical preview (not a Studio screenshot)')
