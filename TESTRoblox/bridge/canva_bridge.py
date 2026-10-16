"""Minimal authenticated registry bridge for exported Canva slides.

This is NOT a Canva website scraper. Export images using Canva, upload them to
the experience owner's Roblox assets, then register the design ID and image IDs.
Run behind HTTPS for a published experience. Tokens are environment-only.
"""
import hmac,json,os,re
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path

REGISTRY=Path(__file__).with_name('decks.json')
def normalize_images(values):
    if not isinstance(values,list) or not 1<=len(values)<=30:raise ValueError('1-30 images required')
    out=[]
    for value in values:
        if not isinstance(value,str):raise ValueError('Asset IDs must be strings')
        match=re.fullmatch(r'(?:rbxassetid://)?([0-9]{1,20})',value)
        if not match or int(match[1])==0:raise ValueError('Invalid image ID')
        out.append('rbxassetid://'+match[1])
    return out
def resolve(design_id,registry):
    if not isinstance(design_id,str) or not re.fullmatch(r'[A-Za-z0-9_-]{5,100}',design_id):raise ValueError('Invalid design ID')
    record=registry.get(design_id)
    if record is None:return None
    return {'images':normalize_images(record['images']),'source':'canva-export-registry'}
class Handler(BaseHTTPRequestHandler):
    def reply(self,status,data):
        raw=json.dumps(data).encode();self.send_response(status);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
    def do_POST(self):
        if self.path!='/v1/canva/resolve':self.reply(404,{'error':'not_found'});return
        token=os.environ.get('ATLAS_BRIDGE_TOKEN','')
        supplied=self.headers.get('x-atlas-token','')
        if not token or not hmac.compare_digest(supplied,token):self.reply(401,{'error':'unauthorized'});return
        try:
            size=int(self.headers.get('Content-Length','0'))
            if size<1 or size>4096:raise ValueError('Invalid body size')
            data=json.loads(self.rfile.read(size))
            if not isinstance(data,dict):raise ValueError('JSON object required')
            result=resolve(data.get('designId'),json.loads(REGISTRY.read_text(encoding='utf-8')))
            self.reply(200 if result else 404,result or {'error':'deck_not_registered'})
        except (ValueError,KeyError,TypeError,json.JSONDecodeError):self.reply(400,{'error':'invalid_request'})
        except OSError:self.reply(503,{'error':'registry_unavailable'})
    def log_message(self,*args):pass # Do not print request bodies or credentials.
if __name__=='__main__':
    if not os.environ.get('ATLAS_BRIDGE_TOKEN'):raise SystemExit('Set ATLAS_BRIDGE_TOKEN; no default token is supplied.')
    host=os.environ.get('ATLAS_BIND','127.0.0.1');port=int(os.environ.get('ATLAS_PORT','8787'))
    print(f'Canva registry bridge on {host}:{port}; put HTTPS in front for Roblox servers.')
    ThreadingHTTPServer((host,port),Handler).serve_forever()
