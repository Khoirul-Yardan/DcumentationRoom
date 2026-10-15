import pathlib, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
EDITOR=pathlib.Path(r'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data')
OUT=ROOT/'.atlas-validation/ApiCompile'
args=['/nologo','/target:exe','/nostdlib+',f'/out:"{OUT / "DomainChecks.exe"}"',f'/reference:"{EDITOR / "NetStandard/ref/2.1.0/netstandard.dll"}"',f'/reference:"{OUT / "Atlas.Domain.dll"}"',f'"{ROOT / "Tools/DomainChecks.cs"}"']
rsp=OUT/'DomainChecks.rsp'; rsp.write_text('\n'.join(args),encoding='utf-8')
subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],check=True)
result=subprocess.run([str(EDITOR/'MonoBleedingEdge/bin/mono.exe'),str(OUT/'DomainChecks.exe')],capture_output=True,text=True)
print(result.stdout+result.stderr)
(ROOT/'Docs/Evidence/domain-checks.txt').write_text(result.stdout+result.stderr,encoding='utf-8')
sys.exit(result.returncode)
