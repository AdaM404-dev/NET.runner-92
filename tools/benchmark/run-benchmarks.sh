#!/usr/bin/env bash
# NET.runner-92 benchmark for the Linux build: the same runs as
# run-benchmarks.bat, saved with the same file names.
# Usage: tools/benchmark/run-benchmarks.sh [game folder] [output folder]
#   game folder    default: Builds/Linux (made by bin/unity-build linux)
#   output folder  default: <game folder>/benchmark
# Takes about 12 minutes on a fast graphics card. Do not touch the mouse or
# keyboard while it runs; each run opens the game and quits it by itself.
set -uo pipefail

game_dir="${1:-Builds/Linux}"
out="${2:-$game_dir/benchmark}"
game="$game_dir/NET.runner-92.x86_64"
if [[ ! -x "$game" ]]; then
  echo "$game was not found. Build it first: bin/unity-build linux" >&2
  exit 1
fi
mkdir -p "$out/logs"
out="$(cd "$out" && pwd)"

# Runs 4 to 11 switch one feature off each (-net92-without), so their frame
# times show what that feature costs on this computer.
features=(occlusion-culling volumetric-fog fog-lights shadows ambient-occlusion distant-lights transparent-objects decals)
total=$((3 + ${#features[@]} + 1))
step=0

run() {
  local label="$1" name="$2"
  shift 2
  step=$((step + 1))
  echo "[$step/$total] $label"
  "$game" "$@" -logFile "$out/logs/$name.log" >/dev/null 2>&1
  local status=$?
  if [[ $status -ne 0 ]]; then
    echo "  exit code $status; see $out/logs/$name.log"
  fi
}

for level in Low Medium High; do
  name="views-${level,,}"
  run "Five fixed views on $level" "$name" \
    -net92-benchmark -net92-quality "$level" -net92-output "$out/$name.csv"
done
for feature in "${features[@]}"; do
  name="views-low-without-$feature"
  run "Five fixed views on Low without $feature" "$name" \
    -net92-benchmark -net92-quality Low -net92-without "$feature" -net92-output "$out/$name.csv"
done
run "Walking the whole route on Low" route-low \
  -net92-route-benchmark -net92-quality Low -net92-output "$out/route-low"

# Only the hardware names, nothing that identifies the computer or its user.
{
  echo "CPU: $(grep -m1 'model name' /proc/cpuinfo | cut -d: -f2- | sed 's/^ *//')"
  if command -v nvidia-smi >/dev/null 2>&1; then
    echo "GPU: $(nvidia-smi --query-gpu=name,driver_version --format=csv,noheader | sed 's/, / (driver /; s/$/)/' | paste -sd ';' -)"
  fi
  echo "RAM: $(awk '/MemTotal/ { printf "%.0f GB", $2 / 1048576 }' /proc/meminfo)"
} > "$out/system.txt"

echo "Done: $out"
