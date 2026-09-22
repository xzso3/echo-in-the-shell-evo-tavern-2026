"""Independent file/assembly/meta and generated-schema checks; no gameplay claims."""
import os
import hashlib
import json
import re
import subprocess
from pathlib import Path
root = Path(__file__).resolve().parents[3]
evidence = root / os.environ.get('ECHO_EVIDENCE_DIR', 'Docs/Framework/Contracts/Evidence')
base = '94ccceff3d00e3e929161e2024ca0a2728aa3067'
checks = []
def check(name, condition, detail):
    checks.append(dict(case_id=name, status='passed' if condition else 'failed', detail=detail))
framework = root / 'Assets/EchoFramework'
for name, refs in [('Core/Echo.Framework.Core.asmdef', []), ('Gameplay/Echo.Gameplay.asmdef', ['Echo.Framework.Core']), ('UnityHost/Echo.UnityHost.asmdef', ['Echo.Framework.Core', 'Echo.Gameplay'])]:
    data = json.loads((framework/name).read_text())
    check('A01.asmdef.' + data['name'], data['references'] == refs, str(refs))
for folder in ['Core', 'Gameplay']:
    bad = [str(p.relative_to(root)) for p in (framework/folder).rglob('*.cs') if re.search(r'\busing\s+(UnityEngine|UnityEditor|PlagueSurvivor)\b', p.read_text())]
    check('A01.source_dependencies.'+folder, not bad, repr(bad))
paths = [framework] + [p for p in framework.rglob('*') if p.suffix != '.meta']
missing = [str(p.relative_to(root)) for p in paths if not Path(str(p)+'.meta').exists()]
check('A01.meta_pairs', not missing, repr(missing))
guids = []
for p in [root/'Assets/EchoFramework.meta'] + list(framework.rglob('*.meta')):
    match = re.search(r'^guid: ([a-f0-9]{32})$', p.read_text(), re.M)
    if match: guids.append(match.group(1))
check('A01.unique_guids', len(guids) == len(paths) == len(set(guids)), f'{len(guids)} new GUIDs')
changed = subprocess.check_output(['git','diff','--name-only',base], cwd=root, text=True).splitlines()
untracked = subprocess.check_output(['git','ls-files','--others','--exclude-standard'], cwd=root, text=True).splitlines()
allowed = ['Assets/EchoFramework/', 'Assets/EchoFramework.meta', 'Tests/EchoFramework/Contracts/', 'Tests/EchoFramework/Host/', 'Tools/EchoContent/Contracts/', 'Docs/Framework/Contracts/', 'Docs/Framework/PHASE1_IMPLEMENTATION_SPEC.md', 'Docs/Framework/PHASE1_CAPABILITIES.md', 'Docs/Framework/PHASE1_ACCEPTANCE.md', 'global.json', 'Directory.Build.props', 'EchoFramework.sln', '.gitignore']
bad = [p for p in changed+untracked if not any(p.startswith(a) if a.endswith('/') else p == a for a in allowed)]
check('A01.write_boundary', not bad, repr(bad))
for rel in ['Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt']:
    original = subprocess.check_output(['git','show',base+':'+rel], cwd=root)
    check('A01.unchanged.'+rel, original == (root/rel).read_bytes(), hashlib.sha256(original).hexdigest())
report = json.loads((evidence/'dotnet-report.json').read_text())
files = sorted((root/'Docs/Framework/Contracts/Generated').rglob('*.schema.json'))
joined = '\n'.join(str(p.relative_to(root/'Docs/Framework/Contracts/Generated')).removesuffix('.schema.json')+':'+re.sub(r'("(?:\\.|[^"\\])*")|\s+', lambda m: m.group(1) or '', p.read_text()) for p in files)
check('A02.generated_schema_digest', hashlib.sha256(joined.encode()).hexdigest() == report['schema_digest'], f'{len(files)} generated schemas')
prod = json.loads((root/'Docs/Framework/Contracts/Generated/production-catalog.json').read_text())
check('A03.production_catalog_empty', prod['use'] == 'production' and prod['capabilities'] == [], 'No production capability implemented by P1-01')
output = evidence/'static-report.json'
output.write_text(json.dumps(dict(tested_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(), framework_digest=report['framework_digest'], checks=checks), indent=2)+'\n')
for c in checks: print(c['case_id']+': '+c['status'])
raise SystemExit(0 if all(c['status']=='passed' for c in checks) else 1)
