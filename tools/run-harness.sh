#!/usr/bin/env bash
# Lance le build Windows avec un harnais (-autoshot / -inputtest) en
# protégeant la sauvegarde solo du PC (copiée avant, restaurée après).
# Usage : run-harness.sh <autoshot|inputtest|tutorialtest|passtest|replaytest|challengetest> <sous-dossier> [args supplémentaires]
set -u
SCR="${TEMP:-/tmp}/levelup-harness"; mkdir -p "$SCR"
SAVE="/c/Users/mathe/AppData/LocalLow/MrDonut Corporate/LevelUp/partie-solo.sav"
MODE="$1"; OUT="$SCR/$2"; shift 2
mkdir -p "$OUT"
[ -f "$SAVE" ] && cp -p "$SAVE" "$SCR/partie-solo.sav.bak"
cd "$(dirname "$0")/../LevelUp/Build/Windows" || exit 2
./LevelUp.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 720 "-$MODE" "$(cygpath -w "$OUT")" "$@" > /dev/null 2>&1
echo "exit=$?"
if [ -f "$SCR/partie-solo.sav.bak" ]; then cp -p "$SCR/partie-solo.sav.bak" "$SAVE"; else rm -f "$SAVE"; fi
ls "$OUT" | tr '\n' ' '; echo
[ -f "$OUT/input-report.txt" ] && cat "$OUT/input-report.txt"
exit 0
