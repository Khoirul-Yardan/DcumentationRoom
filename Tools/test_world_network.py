"""Compile/run the real C# relay client against a real local Python relay process."""
import pathlib, socket, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
EDITOR=pathlib.Path(r'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data')
OUT=ROOT/'.atlas-validation/ApiCompile'
with socket.socket() as sock:
    sock.bind(('127.0.0.1',0)); port=sock.getsockname()[1]
args=['/nologo','/target:exe','/nostdlib+',f'/out:"{OUT / "WorldNetworkChecks.exe"}"',f'/reference:"{EDITOR / "NetStandard/ref/2.1.0/netstandard.dll"}"']
args += [f'/reference:"{OUT / ("Atlas."+name+".dll")}"' for name in ['Domain','Application','Infrastructure']]
args += [f'"{ROOT / "Tools/WorldNetworkChecks.cs"}"']
rsp=OUT/'WorldNetworkChecks.rsp'; rsp.write_text('\n'.join(args),encoding='utf-8')
subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],check=True)
server=subprocess.Popen([sys.executable,str(ROOT/'Backend/AtlasRelay/server.py'),'--host','127.0.0.1','--port',str(port),'--insecure-loopback'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
try:
    startup=server.stdout.readline().strip()
    if 'listening' not in startup: raise RuntimeError(startup+' '+server.stderr.read())
    result=subprocess.run([str(EDITOR/'MonoBleedingEdge/bin/mono.exe'),str(OUT/'WorldNetworkChecks.exe'),str(port)],capture_output=True,text=True,timeout=100)
    report=result.stdout+result.stderr
    print(report)
    (ROOT/'Docs/Evidence/WorldMultiplayer/network-checks.txt').write_text(report,encoding='utf-8')
finally:
    server.terminate()
    try: server.wait(timeout=5)
    except subprocess.TimeoutExpired: server.kill(); server.wait()
sys.exit(result.returncode)
