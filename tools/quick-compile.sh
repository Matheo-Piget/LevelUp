#!/usr/bin/env bash
# Compilation rapide des assemblies du projet avec le Roslyn livré par Unity.
# Sert uniquement à détecter les erreurs C# en quelques secondes ; la
# vérification de référence reste le batchmode Unity (tests EditMode + build).
set -u
PROJ="$(cygpath -m "$(cd "$(dirname "$0")/../LevelUp" && pwd)")"
UNITY="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
OUT="$(cygpath -m "${TEMP:-/tmp}")/levelup-quick-compile"
mkdir -p "$OUT"
cd "$PROJ" || exit 2

DOTNET="$UNITY/NetCoreRuntime/dotnet.exe"
CSC="$UNITY/DotNetSdkRoslyn/csc.dll"

# nom_assembly | csproj | dossiers sources (séparés par ;) | assemblies du projet référencées
PROJECTS=(
  "LevelUp.Utils|LevelUp.Utils.csproj|Assets/Scripts/Utils|"
  "LevelUp.Core|LevelUp.Core.csproj|Assets/Scripts/Core|LevelUp.Utils"
  "LevelUp.AI|LevelUp.AI.csproj|Assets/Scripts/AI|LevelUp.Utils;LevelUp.Core"
  "LevelUp.UI|LevelUp.UI.csproj|Assets/Scripts/UI|LevelUp.Utils;LevelUp.Core"
  "LevelUp.Network|LevelUp.Network.csproj|Assets/Scripts/Network|LevelUp.Utils;LevelUp.Core"
  "Assembly-CSharp|Assembly-CSharp.csproj|ROOT|LevelUp.Utils;LevelUp.Core;LevelUp.AI;LevelUp.UI;LevelUp.Network"
  "Assembly-CSharp-Editor|Assembly-CSharp-Editor.csproj|Assets/Editor|LevelUp.Utils;LevelUp.Core;LevelUp.AI;LevelUp.UI;LevelUp.Network;Assembly-CSharp"
  "LevelUp.Tests.EditMode|LevelUp.Tests.EditMode.csproj|Assets/Tests/EditMode|LevelUp.Utils;LevelUp.Core;LevelUp.AI;LevelUp.UI;LevelUp.Network"
)

status=0
for entry in "${PROJECTS[@]}"; do
  IFS='|' read -r name csproj srcs deps <<< "$entry"
  rsp="$OUT/$name.rsp"
  {
    echo "-nologo -target:library -langversion:9.0 -nostdlib+ -noconfig -debug- -optimize-"
    echo "-nowarn:0169,CS8632,CS0414,CS0649,CS0067,CS0618"
    defs=$(grep -oP '(?<=<DefineConstants>).*?(?=</DefineConstants>)' "$csproj" | head -1)
    echo "-define:\"$defs\""
    echo "-out:\"$OUT/$name.dll\""
    grep -oP '(?<=<HintPath>).*?(?=</HintPath>)' "$csproj" | while read -r ref; do echo "-r:\"$ref\""; done
    IFS=';' read -ra dlist <<< "$deps"
    for d in "${dlist[@]}"; do [ -n "$d" ] && echo "-r:\"$OUT/$d.dll\""; done
    if [ "$srcs" = "ROOT" ]; then
      # Assembly-CSharp : les scripts qui ne sont sous aucun asmdef (liste du csproj + nouveaux à la racine de Scripts)
      ls Assets/Scripts/*.cs 2>/dev/null | while read -r f; do echo "\"$f\""; done
    else
      IFS=';' read -ra slist <<< "$srcs"
      for s in "${slist[@]}"; do find "$s" -name "*.cs" | while read -r f; do echo "\"$f\""; done; done
    fi
  } > "$rsp"
  result=$("$DOTNET" "$CSC" "@$rsp" 2>&1 | grep -E "error|warning CS(0162|0168|0219|0105)" | sed "s#$PROJ/##g" | sort -u)
  errors=$(echo "$result" | grep -c "error")
  if [ "$errors" -gt 0 ]; then
    echo "✗ $name : $errors erreur(s)"
    echo "$result" | grep "error" | head -40
    status=1
    break   # les assemblies suivantes dépendent de celle-ci
  else
    echo "✓ $name"
    [ -n "$result" ] && echo "$result" | head -10
  fi
done
exit $status
