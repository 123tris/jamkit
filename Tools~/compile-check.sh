#!/bin/bash
# Fast compile verification without launching Unity ("nothing ships unverified").
# Compiles Runtime, Editor, and Tests sources with Roslyn against the reference set
# extracted from the Unity-generated csprojs in the project root. Requires the dotnet SDK
# and that Unity has generated csprojs + built Library/ScriptAssemblies at least once.
#
# Usage: bash Packages/com.metz.jamkit/Tools~/compile-check.sh [project-root]
set -u
PROJ="${1:-$(cd "$(dirname "$0")/../../.." && pwd)}"
PKG="$PROJ/Packages/com.metz.jamkit"
# Windows-style path so the native compiler can write to it (Git Bash /tmp is not C:\tmp).
OUT="$(cygpath -m "${TEMP:-/tmp}" 2>/dev/null || echo /tmp)/jamkit-compile-check"
mkdir -p "$OUT"

CSC=$(ls -d "C:/Program Files/dotnet/sdk"/*/Roslyn/bincore/csc.dll 2>/dev/null | sort -V | tail -1)
if [ -z "$CSC" ]; then echo "dotnet SDK csc.dll not found"; exit 2; fi

build_asm () {
  local csproj="$1" srcdir="$2" out="$3" extra_ref="${4:-}"
  local rsp="$OUT/$(basename "$out" .dll).rsp"
  : > "$rsp"

  grep -o '<HintPath>[^<]*' "$csproj" | sed 's/<HintPath>/-r:"/;s/$/"/' >> "$rsp"

  grep -o 'ProjectReference Include="[^"]*"' "$csproj" \
    | sed 's/ProjectReference Include="//;s/"$//;s/\.csproj$//' \
    | while read -r name; do
        case "$name" in
          Metz.JamKit.*) ;; # replaced by freshly built dlls via extra_ref
          *) echo "-r:\"$PROJ/Library/ScriptAssemblies/$name.dll\"" >> "$rsp" ;;
        esac
      done

  if [ -n "$extra_ref" ]; then echo "-r:\"$extra_ref\"" >> "$rsp"; fi

  find "$srcdir" -name '*.cs' | sed 's/^/"/;s/$/"/' >> "$rsp"

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

build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Runtime" "$OUT/Metz.JamKit.Runtime.dll" || exit 1
build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor" "$OUT/Metz.JamKit.Editor.dll" "$OUT/Metz.JamKit.Runtime.dll" || exit 1
build_asm "$PROJ/Metz.JamKit.Tests.csproj" "$PKG/Tests/Runtime" "$OUT/Metz.JamKit.Tests.dll" "$OUT/Metz.JamKit.Runtime.dll" || exit 1
# Samples~ is invisible to Unity until imported, so this is the ONLY compile coverage sample
# code gets. Same reference set as Runtime + the fresh Runtime dll.
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Samples~" "$OUT/Metz.JamKit.Samples.dll" "$OUT/Metz.JamKit.Runtime.dll" || exit 1
echo "ALL GREEN"
