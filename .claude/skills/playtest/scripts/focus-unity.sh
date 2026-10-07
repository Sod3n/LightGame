#!/bin/zsh
# The editor only imports file changes while focused: bring it forward, wait for a refresh, restore focus.
ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
pid=$(pgrep -if "Unity.app/Contents/MacOS/Unity -projectpath $ROOT( |$)" | head -1)
[ -n "$pid" ] || { echo "no Unity editor running on $ROOT"; exit 1; }
log="$ROOT/Logs/agent-editor.log"
ps -o args= -p $pid | grep -q -- "-logFile" || log=~/Library/Logs/Unity/Editor.log
prev=$(osascript -e 'tell application "System Events" to get name of first application process whose frontmost is true')
before=$(grep -c "Asset Pipeline Refresh" $log)
osascript -e "tell application \"System Events\" to set frontmost of (first process whose unix id is $pid) to true"
for i in $(seq 1 60); do [ $(grep -c "Asset Pipeline Refresh" $log) -gt $before ] && break; sleep 1; done
sleep 3
osascript -e "tell application \"$prev\" to activate"
grep "error CS" <(tail -300 $log) | tail -5
