"""Small loss-preserving RBXL patcher. Original opaque chunks stay byte-for-byte.

Only explicitly rebuilt classes/properties are replaced. Zstd uses Git for Windows'
bundled library; no pip/network dependencies. Format: https://dom.rojo.space/binary.html
"""
import ctypes
import os
import struct
from collections import defaultdict
from pathlib import Path

def u32(n): return struct.pack('<I', n)
def string(s):
    b = s.encode('utf-8') if isinstance(s, str) else s
    return u32(len(b)) + b
def read_string(b, at):
    n = struct.unpack_from('<I', b, at)[0]
    return b[at+4:at+4+n], at+4+n
def interleave(values):
    return bytes(v[i] for i in range(4) for v in values)
def ints(values, signed=False):
    return interleave([struct.pack('>I', ((v << 1) ^ (v >> 31)) & 0xffffffff if signed else v) for v in values])
def read_ints(b, n, signed=False):
    out = [int.from_bytes(bytes(b[i+j*n] for j in range(4)), 'big') for i in range(n)]
    return [(v >> 1) ^ -(v & 1) for v in out] if signed else out
def refs(values):
    return ints([v-(values[i-1] if i else 0) for i,v in enumerate(values)], True)
def read_refs(b, n):
    out=[]; total=0
    for v in read_ints(b,n,True): total+=v; out.append(total)
    return out
def floats(values):
    bits=[struct.unpack('<I',struct.pack('<f',v))[0] for v in values]
    return ints([((b << 1) | (b >> 31)) & 0xffffffff for b in bits])
def read_floats(b,n):
    return [struct.unpack('<f',u32((v>>1)|((v&1)<<31)))[0] for v in read_ints(b,n)]
def encode(t, v):
    if t==1: return b''.join(string(x) for x in v)
    if t==2: return bytes(v)
    if t==3: return ints(v,True)
    if t==4: return floats(v)
    if t==5: return b''.join(struct.pack('<d',x) for x in v)
    if t==7: return floats([x[0] for x in v])+floats([x[2] for x in v])+ints([x[1] for x in v],True)+ints([x[3] for x in v],True)
    if t in (12,13,14): return b''.join(floats([x[i] for x in v]) for i in range(2 if t==13 else 3))
    if t==16: return b''.join(b'\0'+struct.pack('<9f',*x[3:]) for x in v)+encode(14,[x[:3] for x in v])
    if t in (11,18): return ints(v)
    if t==19: return refs(v)
    if t==26: return bytes(x[i] for i in range(3) for x in v)
    raise ValueError(f'Unsupported write type {t}')
def decode(t,b,n):
    if t==1:
        out=[];at=0
        for _ in range(n): x,at=read_string(b,at);out.append(x.decode('utf-8',errors='replace'))
        return out
    if t==2:return list(map(bool,b))
    if t==3:return read_ints(b,n,True)
    if t==4:return read_floats(b,n)
    if t==5:return list(struct.unpack('<'+'d'*n,b))
    if t==7:
        xs=read_floats(b[:4*n],n);ys=read_floats(b[4*n:8*n],n)
        xo=read_ints(b[8*n:12*n],n,True);yo=read_ints(b[12*n:],n,True)
        return list(zip(xs,xo,ys,yo))
    if t in (11,18):return read_ints(b,n)
    if t==19:return read_refs(b,n)
    if t==26:return [tuple(b[i+j*n] for j in range(3)) for i in range(n)]
    if t in (12,13,14):
        axes=2 if t==13 else 3
        return list(zip(*[read_floats(b[j*n*4:(j+1)*n*4],n) for j in range(axes)]))
    if t==16:
        mats=[];at=0
        for _ in range(n):
            k=b[at];at+=1
            if k==0:mats.append(struct.unpack_from('<9f',b,at));at+=36
            elif k==2:mats.append((1,0,0,0,1,0,0,0,1))
            else: mats.append((k,))
        pos=decode(14,b[at:],n)
        return [tuple(p)+m for p,m in zip(pos,mats)]
    raise ValueError(f'Unsupported read type {t}')
def chunk(tag,b):return tag+struct.pack('<III',0,len(b),0)+b

class Place:
    def __init__(self,path):
        p=Path(path).read_bytes()
        assert p[:14]==b'<roblox!\x89\xff\r\n\x1a\n', 'Not binary RBXL'
        self.header=p[:32];self.chunks=[];self.classes={};self.props={};self.parents={};at=32
        z=None
        while at<len(p):
            start=at;tag,c,u,_=struct.unpack_from('<4sIII',p,at);at+=16
            b=p[at:at+(c or u)];at+=c or u
            if c:
                assert b[:4]==b'\x28\xb5\x2f\xfd', 'This input requires a Zstd-compressed Studio place'
                if z is None:
                    dll=os.environ.get('ATLAS_ZSTD_DLL',r'C:\Program Files\Git\mingw64\bin\libzstd.dll')
                    z=ctypes.CDLL(dll)
                    z.ZSTD_decompress.argtypes=[ctypes.c_void_p,ctypes.c_size_t,ctypes.c_void_p,ctypes.c_size_t]
                    z.ZSTD_decompress.restype=ctypes.c_size_t
                buf=ctypes.create_string_buffer(u)
                assert z.ZSTD_decompress(buf,u,b,c)==u,'Decompression failed'
                b=buf.raw
            self.chunks.append((tag,b,p[start:at]))
            if tag==b'INST':
                cid=struct.unpack_from('<I',b)[0];name,pos=read_string(b,4)
                service=b[pos];n=struct.unpack_from('<I',b,pos+1)[0]
                self.classes[cid]=(name.decode(),service,read_refs(b[pos+5:pos+5+4*n],n))
            elif tag==b'PROP':
                cid=struct.unpack_from('<I',b)[0];name,pos=read_string(b,4)
                self.props[cid,name.decode()]=(b[pos],b[pos+1:])
            elif tag==b'PRNT':
                n=struct.unpack_from('<I',b,1)[0]
                self.parents=dict(zip(read_refs(b[5:5+4*n],n),read_refs(b[5+4*n:],n)))
        assert at==len(p)
        self.byname={v[0]:k for k,v in self.classes.items()}
        self.added=[];self.replace_classes=set();self.overrides={}
        self.nextref=max(self.parents)+1
    def service(self,name):return self.classes[self.byname[name]][2][0]
    def property(self,cls,name):
        cid=self.byname[cls];t,b=self.props[cid,name]
        return decode(t,b,len(self.classes[cid][2]))
    def override(self,cls,name,t,values):self.overrides[self.byname[cls],name]=(t,values)
    def add(self,cls,name,parent,props=None,ref=None):
        if ref is None:ref=self.nextref;self.nextref+=1
        values={'Name':(1,name)};values.update(props or {})
        self.added.append({'class':cls,'ref':ref,'parent':parent,'props':values})
        return ref
    def save(self,path):
        groups=defaultdict(list)
        for obj in self.added:groups[obj['class']].append(obj)
        ids=dict(self.byname);nextcid=max(self.classes)+1
        for cls in groups:
            if cls not in ids:ids[cls]=nextcid;nextcid+=1
            else: assert cls in self.replace_classes, f'Existing class must explicitly be replaced: {cls}'
        replaced={ids[x] for x in self.replace_classes}
        output=[raw for tag,b,raw in self.chunks if tag in (b'META',b'SSTR')]
        for tag,b,raw in self.chunks:
            if tag==b'INST' and struct.unpack_from('<I',b)[0] not in replaced:output.append(raw)
        for cls,objects in groups.items():
            output.append(chunk(b'INST',u32(ids[cls])+string(cls)+b'\0'+u32(len(objects))+refs([o['ref'] for o in objects])))
        written=set()
        for tag,b,raw in self.chunks:
            if tag!=b'PROP':continue
            cid=struct.unpack_from('<I',b)[0];name,_=read_string(b,4);key=(cid,name.decode())
            if cid in replaced:continue
            if key in self.overrides:
                t,values=self.overrides[key];output.append(chunk(b'PROP',u32(cid)+string(name)+bytes([t])+encode(t,values)));written.add(key)
            else:output.append(raw)
        for (cid,name),(t,values) in self.overrides.items():
            if (cid,name) not in written:output.append(chunk(b'PROP',u32(cid)+string(name)+bytes([t])+encode(t,values)))
        for cls,objects in groups.items():
            keys=set(objects[0]['props'])
            assert all(set(o['props'])==keys for o in objects),f'{cls} properties differ'
            for key in sorted(keys):
                t=objects[0]['props'][key][0]
                output.append(chunk(b'PROP',u32(ids[cls])+string(key)+bytes([t])+encode(t,[o['props'][key][1] for o in objects])))
        parents=dict(self.parents)
        for o in self.added:parents[o['ref']]=o['parent']
        children=defaultdict(list)
        for child,parent in parents.items():children[parent].append(child)
        ordered=[]
        def visit(ref):
            for child in children[ref]:visit(child)
            if ref!=-1:ordered.append(ref)
        visit(-1)
        assert len(ordered)==len(parents),'Orphan or cyclic parent'
        output.append(chunk(b'PRNT',b'\0'+u32(len(parents))+refs(ordered)+refs([parents[r] for r in ordered])))
        output.append(chunk(b'END\0',b'</roblox>'))
        header=self.header[:16]+u32(len(ids))+u32(len(parents))+self.header[24:]
        Path(path).write_bytes(header+b''.join(output))

if __name__=='__main__':
    import sys
    p=Place(sys.argv[1])
    print('Instances:',len(p.parents),'Classes:',len(p.classes))
    print({name:len(refs) for name,_,refs in p.classes.values() if len(refs)>1})
