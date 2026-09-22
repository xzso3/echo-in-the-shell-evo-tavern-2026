#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/../../.." && pwd)
cd "$repo"
json_dll=${ECHO_JSON_DLL:-"$repo/Library/PackageCache/com.unity.nuget.newtonsoft-json@3.2.1/Runtime/Newtonsoft.Json.dll"}
if [ ! -f "$json_dll" ]; then
  json_dll="$HOME/Library/Unity/cache/packages/packages.unity.cn/com.unity.nuget.newtonsoft-json@3.2.1/Runtime/Newtonsoft.Json.dll"
fi
if [ ! -f "$json_dll" ]; then
  echo 'Newtonsoft.Json 13.0.2 not found. Import this worktree in Unity or set ECHO_JSON_DLL to its Runtime/Newtonsoft.Json.dll.' >&2
  exit 2
fi
mkdir -p Docs/Framework/Contracts/Evidence
dotnet build Tests/EchoFramework/Host/Echo.Contracts.Host.csproj --disable-build-servers "-p:EchoJsonAssembly=$json_dll" "-p:RestoreSources=$repo/Tests/EchoFramework/Contracts/Fixtures" -p:NuGetAudit=false --nologo
dotnet Tests/EchoFramework/Host/bin/Debug/net8.0/Echo.Contracts.Host.dll "$repo" "$(git rev-parse HEAD)" --export
python3 Tests/EchoFramework/Contracts/verify_static.py
