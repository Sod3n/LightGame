#!/bin/zsh
# Own a Unity editor for this checkout: editor.sh start|stop|restart|status|log
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
LOG="$ROOT/Logs/agent-editor.log"
VERSION=$(awk '/m_EditorVersion:/{print $2}' "$ROOT/ProjectSettings/ProjectVersion.txt")
HUB=$(tr -d '"' < ~/Library/Application\ Support/UnityHub/secondaryInstallPath.json 2>/dev/null)
APP="${HUB:-/Applications/Unity/Hub/Editor}/$VERSION/Unity.app"
pid() { pgrep -if "Unity.app/Contents/MacOS/Unity -projectpath $ROOT( |$)" | head -1; }

case "$1" in
  start)
    [ -n "$(pid)" ] && { echo "already running pid=$(pid)"; exit 0; }
    [ -d "$APP" ] || { echo "Unity $VERSION not found at $APP"; exit 1; }
    mkdir -p "$ROOT/Logs"
    open -g -n -a "$APP" --args -projectPath "$ROOT" -logFile "$LOG"
    for i in $(seq 1 30); do [ -n "$(pid)" ] && break; sleep 1; done
    echo "started pid=$(pid), log $LOG"
    echo "wait for the driver: until [ -f $ROOT/Temp/playtest/status.txt ]; do sleep 2; done" ;;
  stop)
    p=$(pid); [ -z "$p" ] && { echo "not running"; exit 0; }
    kill $p
    for i in $(seq 1 20); do kill -0 $p 2>/dev/null || break; sleep 1; done
    kill -0 $p 2>/dev/null && { kill -9 $p; echo "force-killed $p"; } || echo "stopped $p"
    rm -f "$ROOT/Temp/playtest/status.txt" ;;
  restart) "$0" stop; "$0" start ;;
  status) p=$(pid); [ -n "$p" ] && echo "running pid=$p" || echo "not running"; cat "$ROOT/Temp/playtest/status.txt" 2>/dev/null ;;
  log) tail -${2:-60} "$LOG" ;;
  *) echo "usage: editor.sh start|stop|restart|status|log [lines]"; exit 1 ;;
esac
