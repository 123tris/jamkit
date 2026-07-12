#!/bin/bash
# Fast compile verification without launching Unity ("nothing ships unverified").
# Compiles Runtime, Editor, and Tests sources with Roslyn against the reference set
# extracted from the Unity-generated csprojs in the project root. Requires the dotnet SDK
# and that Unity has generated csprojs + built Library/ScriptAssemblies at least once.
#
# Usage: bash Packages/com.metz.jamkit/Tools~/compile-check.sh [project-root]
set -u
PROJ="${1:-$(cd "$(dirname "$0")/../../.." && pwd)}"
# csc.dll is a Windows process: /c/... paths from Git Bash's pwd/find don't resolve there.
PROJ="$(cygpath -m "$PROJ" 2>/dev/null || echo "$PROJ")"
PKG="$PROJ/Packages/com.metz.jamkit"
# Windows-style path so the native compiler can write to it (Git Bash /tmp is not C:\tmp).
OUT="$(cygpath -m "${TEMP:-/tmp}" 2>/dev/null || echo /tmp)/jamkit-compile-check"
mkdir -p "$OUT"

CSC=$(ls -d "C:/Program Files/dotnet/sdk"/*/Roslyn/bincore/csc.dll 2>/dev/null | sort -V | tail -1)
if [ -z "$CSC" ]; then echo "dotnet SDK csc.dll not found"; exit 2; fi

build_asm () {
  local csproj="$1" srcdir="$2" out="$3" extra_ref="${4:-}" exclude="${5:-}" extra_defines="${6:-}"
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

  # extra_ref may hold several space-separated dll paths ($OUT never contains spaces).
  for ref in $extra_ref; do echo "-r:\"$ref\"" >> "$rsp"; done

  local srcs
  if [ -n "$exclude" ]; then
    srcs=$(find "$srcdir" -name '*.cs' -not -path "$exclude" 2>/dev/null)
  else
    srcs=$(find "$srcdir" -name '*.cs' 2>/dev/null)
  fi
  if [ -z "$srcs" ]; then
    echo "=== $(basename "$out") skipped (no sources in $srcdir)"
    return 0
  fi
  echo "$srcs" | sed 's/^/"/;s/$/"/' >> "$rsp"

  local defines
  defines=$(grep -o 'DefineConstants>[^<]*' "$csproj" | sed 's/DefineConstants>//' | head -1)
  if [ -n "$extra_defines" ]; then defines="$defines;$extra_defines"; fi
  {
    echo "-nologo"; echo "-nostdlib"; echo "-target:library"
    echo "-langversion:9.0"; echo "-warn:0"
    echo "-define:$defines"; echo "-out:\"$out\""
  } >> "$rsp"

  echo "=== $(basename "$out")"
  dotnet "$CSC" -noconfig "@$rsp" || return 1
}

# Runtime/Fmod and Editor/Fmod are their own JAMKIT_FMOD-gated assemblies (built further down
# when FMOD is available), so the base builds exclude them.
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Runtime" "$OUT/Metz.JamKit.Runtime.dll" "" "*/Fmod/*" || exit 1
build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor" "$OUT/Metz.JamKit.Editor.dll" "$OUT/Metz.JamKit.Runtime.dll" "*/Fmod/*" || exit 1
# The tests asmdefs reference Ripple + UltEvents (0.9); csprojs generated before that change
# don't list them, so stage the dlls into $OUT (extra_ref paths must be space-free).
cp -f "$PROJ/Library/ScriptAssemblies/Metz.Ripple.Runtime.dll" "$PROJ/Library/ScriptAssemblies/Kybernetik.UltEvents.dll" "$OUT/" 2>/dev/null
build_asm "$PROJ/Metz.JamKit.Tests.csproj" "$PKG/Tests/Runtime" "$OUT/Metz.JamKit.Tests.dll" "$OUT/Metz.JamKit.Runtime.dll $OUT/Metz.Ripple.Runtime.dll $OUT/Kybernetik.UltEvents.dll" || exit 1
# Editor tests borrow the runtime-tests reference set (nunit + full Unity incl. UnityEditor):
# Metz.JamKit.EditorTests.csproj stays a referenceless stub until Unity regenerates it with
# scripts present, so it can't be trusted as the reference source.
build_asm "$PROJ/Metz.JamKit.Tests.csproj" "$PKG/Tests/Editor" "$OUT/Metz.JamKit.EditorTests.dll" "$OUT/Metz.JamKit.Runtime.dll $OUT/Metz.JamKit.Editor.dll $OUT/Metz.Ripple.Runtime.dll $OUT/Kybernetik.UltEvents.dll" || exit 1
# Samples~ is invisible to Unity until imported, so this is the ONLY compile coverage sample
# code gets. Same reference set as Runtime + the fresh Runtime dll.
build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Samples~" "$OUT/Metz.JamKit.Samples.dll" "$OUT/Metz.JamKit.Runtime.dll" || exit 1

# --- Optional FMOD integration (JAMKIT_FMOD) --------------------------------------------
# Compiled only when the FMOD for Unity sources are reachable: the project has them imported
# at Assets/Plugins/FMOD, or FMOD_SRC points at an integration's root. FMODUnity.dll is built
# from those sources first — the whole FMOD folder minus Editor code, since the platform
# classes under platforms/*/src belong to the same assembly (runtime reference set; the
# UNITY_*_EXIST defines mirror what its asmdef versionDefines would activate in these projects).
FMOD_SRC="${FMOD_SRC:-$PROJ/Assets/Plugins/FMOD}"
if [ -d "$FMOD_SRC" ]; then
  build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$FMOD_SRC" "$OUT/FMODUnity.dll" "" "*/Editor/*" \
    "UNITY_PHYSICS_EXIST;UNITY_PHYSICS2D_EXIST" || exit 1
  build_asm "$PROJ/Metz.JamKit.Runtime.csproj" "$PKG/Runtime/Fmod" "$OUT/Metz.JamKit.Fmod.dll" \
    "$OUT/Metz.JamKit.Runtime.dll $OUT/FMODUnity.dll" "" "JAMKIT_FMOD" || exit 1
  build_asm "$PROJ/Metz.JamKit.Editor.csproj" "$PKG/Editor/Fmod" "$OUT/Metz.JamKit.Fmod.Editor.dll" \
    "$OUT/Metz.JamKit.Runtime.dll $OUT/Metz.JamKit.Editor.dll $OUT/Metz.JamKit.Fmod.dll $OUT/FMODUnity.dll" "" "JAMKIT_FMOD" || exit 1
else
  echo "=== FMOD assemblies skipped (no FMOD sources at $FMOD_SRC)"
fi
echo "ALL GREEN"
