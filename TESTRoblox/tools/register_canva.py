"""Register an exported Canva deck; no credentials, network, purchase or uploads."""
import argparse,json,re,sys,urllib.parse
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'bridge'))
from canva_bridge import normalize_images
def design_id(url):
    parsed=urllib.parse.urlparse(url.strip())
    if parsed.scheme!='https' or parsed.netloc not in ['www.canva.com','canva.com']:raise ValueError('Expected a Canva HTTPS design URL')
    match=re.fullmatch(r'/design/([A-Za-z0-9_-]{5,100})(?:/[^\s]*)?',parsed.path)
    if not match:raise ValueError('Invalid Canva design path')
    return match[1]
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--url',required=True);parser.add_argument('--images',nargs='+',required=True)
    args=parser.parse_args();key=design_id(args.url);images=normalize_images(args.images)
    target=ROOT/'bridge/decks.json';registry=json.loads(target.read_text(encoding='utf-8'));registry[key]={'images':images}
    target.write_text(json.dumps(registry,indent=2),encoding='utf-8')
    # Static registry works without a hosted bridge; embed on the next build.
    content='-- Generated registered decks. These are image asset references, not API credentials.\nreturn {\n'
    for design,record in sorted(registry.items()):
        content+='    ["'+design+'"] = {'+', '.join('"'+v+'"' for v in normalize_images(record['images']))+'},\n'
    content+='}\n';(ROOT/'src/CanvaDecks.luau').write_text(content,encoding='utf-8')
    print(f'Registered {key}: {len(images)} images. Rebuild the place, or serve bridge/decks.json through the bridge.')
if __name__=='__main__':main()
