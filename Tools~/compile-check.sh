#!/bin/bash
# Roslyn compile check without launching Unity. Builds Ripple from source, then JamKit's
# Runtime, Editor, Tests and Samples against it, using the reference set from the
# Unity-generated csprojs in the host project. Needs the dotnet SDK, and Unity must have
# generated the csprojs and Library/ScriptAssemblies at least once.
#
# Usage: bash Packages/com.metz.jamkit/Tools~/compile-check.sh [project-root] [ripple-root]
#
# Editor/Toolbar and Editor/Flow use Unity 6.3+ / 6.4+ editor APIs. They are only compiled
# when the host project's csprojs point at an editor that has them.
set -u
PROJ="${1:-$(cd "$(dirname "$0")/../../.." && pwd)}"
PROJ="$(cygpath -m "$PROJ" 2>/dev/null || echo "$PROJ")"
PKG="$(cd "$(dirname "$0")/.." && pwd)"
PKG="$(cygpath -m "$PKG" 2>/dev/null || echo "$PKG")"
RIPPLE="${2:-$(grep -o '"com.metz.ripple": *"file:[^"]*' "$PROJ/Packages/manifest.json" | sed 's/.*file://')}"
OUT="$(cygpath -m "${TEMP:-/tmp}" 2>/dev/null || echo /tmp)/jamkit-compile-check"
mkdir -p "$OUT"

CSC=$(ls -d "C:/Program Files/dotnet/sdk"/*/Roslyn/bincore/csc.dll 2>/dev/null | sort -V | tail -1)
if [ -z "$CSC" ]; then echo "dotnet SDK csc.dll not found"; exit 2; fi
if [ ! -d "$RIPPLE/Runtime" ]; then echo "Ripple sources not found (pass the path as the second argument)"; exit 2; fi

# build_asm <csproj> <srcdir> <out.dll> [extra refs] [exclude glob]
build_asm () {
  local csproj="$1" srcdir="$2" out="$3" extra_ref="${4:-}" exclude="${5:-}"
  local rsp="$OUT/$(basename "$out" .dll).rsp"
  : > "$rsp"
  grep -o '<HintPath>[^<]*' "$csproj" | sed 's/<HintPath>/-r:"/;s/$/"/' >> "$rsp"
  grep -o 'ProjectReference Include="[^"]*"' "$csproj" \
    | sed 's/ProjectReference Include="//;s/"$//;s/\.csproj$//' \
    | while read -r name; do
        case "$name" in
          Metz.JamKit.*|Metz.Ripple.*|Kybernetik.UltEvents*) ;;
          *) echo "-r:\"$PROJ/Library/ScriptAssemblies/$name.dll\"" >> "$rsp" ;;
        esac
      done
  for ref in $extra_ref; do echo "-r:\"$ref\"" >> "$rsp"; done
  local srcs
  if [ -n "$exclude" ]; then
    srcs=$(find "$srcdir" -name '*.cs' -not -path "$exclude" 2>/dev/null)
  else
    srcs=$(find "$srcdir" -name '*.cs' 2>/dev/null)
  fi
  if [ -z "$srcs" ]; then echo "=== $(basename "$out") skipped (no sources)"; return 0; fi
  echo "$srcs" | sed 's/^/"/;s/$/"/' >> "$rsp"
  local defines
  defines=$(grep -o 'DefineConstants>[^<]*' "$csproj" | sed 's/DefineConstants>//' | head -1)
  {
    echo "-nologo"; echo "-nostdlib"; echo "-target:library"
    echo "-langversion:9.0"; echo "-warn:0"
    echo "-define:$defines"; echo "-out:\"$out\""
  } >> "$rsp"
  echo "=== $(basename "$out")"
  dotnet "$CSC" -noconfig "@$rsp" || return 1
}

RR="$OUT/Metz.Ripple.Runtime.dll"
JR="$OUT/Metz.JamKit.Runtime.dll"
JE="$OUT/Metz.JamKit.Editor.dll"
build_asm "$PROJ/Metz.Ripple.Runtime.csproj" "$RIPPLE/Runtime" "$RR" || exit 1
build_asm "$PROJ/Metz.Ripple.Editor.csproj" "$RIPPLE/Editor" "$OUT/Metz.Ripple.Editor.dll" "$RR" "*/Dependency Resolver/*" || exit 1
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Runtime" "$JR" "$RR" "*/Fmod/*" || exit 1
build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor" "$JE" "$JR $RR" "*/Editor/[FT][lo]*/*" || exit 1

find "$PROJ/Library/PackageCache" -name "nunit.framework.dll" -path "*unity-custom*" 2>/dev/null | head -1 | while read -r f; do cp -f "$f" "$OUT/"; done
cp -f "$PROJ/Library/ScriptAssemblies/UnityEngine.TestRunner.dll" "$PROJ/Library/ScriptAssemblies/UnityEditor.TestRunner.dll" "$OUT/" 2>/dev/null
TESTREFS="$RR $OUT/nunit.framework.dll $OUT/UnityEngine.TestRunner.dll $OUT/UnityEditor.TestRunner.dll"
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Tests/Runtime" "$OUT/Metz.JamKit.Tests.dll" "$JR $TESTREFS" || exit 1
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Tests/Editor" "$OUT/Metz.JamKit.EditorTests.dll" "$JR $JE $TESTREFS" || exit 1
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Samples~" "$OUT/Metz.JamKit.Samples.dll" "$JR $RR" "*/03 Feel Showcase/*" || exit 1

if grep -q "UnityEditor.GraphToolkitModule.dll" "$PROJ/Metz.JamKit.Editor.csproj"; then
  build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor/Toolbar" "$OUT/Metz.JamKit.Toolbar.Editor.dll" "$JR $JE" || exit 1
  build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor/Flow" "$OUT/Metz.JamKit.Flow.Editor.dll" "$JR $RR" || exit 1
else
  echo "=== Toolbar and Flow editor assemblies skipped (host editor is older than 6.4)"
fi
echo "ALL GREEN"
