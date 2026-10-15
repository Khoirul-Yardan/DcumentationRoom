"""Compile against installed Unity/package DLLs without starting an Editor.
This checks C# API compatibility, NOT asset import, scene validity, or player build.
"""
import pathlib, subprocess, sys
ROOT = pathlib.Path(__file__).resolve().parents[1]
EDITOR = pathlib.Path(r'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data')
OUT = ROOT / '.atlas-validation/ApiCompile'
OUT.mkdir(parents=True, exist_ok=True)
deps = {'Domain': [], 'Application':['Domain'], 'Infrastructure':['Domain','Application'], 'Presentation':['Domain','Application'], 'Runtime':['Domain','Application','Infrastructure','Presentation'], 'Editor':['Domain','Application','Infrastructure','Presentation','Runtime']}
engine = list((EDITOR/'Managed/UnityEngine').glob('UnityEngine*.dll'))
packages = [ROOT/'Library/ScriptAssemblies'/name for name in ['Unity.TextMeshPro.dll','UnityEngine.UI.dll','Unity.InputSystem.dll']]
report=[]
for name, refs in deps.items():
    references = [EDITOR/'NetStandard/ref/2.1.0/netstandard.dll']
    if name not in ['Domain','Application']: references += engine + packages
    if name=='Editor': references += [EDITOR/'Managed/UnityEditor.dll']
    references += [OUT/('Atlas.'+ref+'.dll') for ref in refs]
    args = ['/nologo','/target:library','/langversion:9.0','/nostdlib+','/out:"'+str(OUT/('Atlas.'+name+'.dll'))+'"']
    args += ['/reference:"'+str(p)+'"' for p in references]
    args += ['"'+str(p)+'"' for p in (ROOT/'Assets/_Project/Scripts'/name).glob('*.cs')]
    rsp=OUT/(name+'.rsp'); rsp.write_text('\n'.join(args),encoding='utf-8')
    result=subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],capture_output=True,text=True)
    print(name+': '+('PASS' if result.returncode==0 else 'FAIL')); print(result.stdout+result.stderr)
    report.append(name+': '+('PASS' if result.returncode==0 else 'FAIL')+'\n'+result.stdout+result.stderr)
    if result.returncode: break
(ROOT/'Docs/Evidence').mkdir(exist_ok=True)
(ROOT/'Docs/Evidence/compiler-check.txt').write_text('\n'.join(report),encoding='utf-8')
sys.exit(result.returncode)

