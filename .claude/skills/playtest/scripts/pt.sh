#!/bin/zsh
# Send commands to PlaytestDriver and print its reply: pt.sh "cmd1" "cmd2" ...
D="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)/Temp/playtest"
[ -d "$D" ] || { echo "no $D - is the editor open and PlaytestDriver compiled?"; exit 1; }
: > "$D/out.txt"
printf '%s\n' "$@" > "$D/cmd.txt"
for i in $(seq 1 120); do [ -s "$D/out.txt" ] && break; sleep 0.5; done
[ -s "$D/out.txt" ] || { echo "no reply in 60s - editor busy, compiling, or not refreshed"; exit 1; }
cat "$D/out.txt"
