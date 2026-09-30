# Shared helpers for the bin/unity-* scripts. Source this file, do not run it.
# Override the editor binary with UNITY_EDITOR=/path/to/Unity.

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

unity_version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
UNITY_EDITOR="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$unity_version/Editor/Unity}"

if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity $unity_version not found at $UNITY_EDITOR (set UNITY_EDITOR)" >&2
  exit 127
fi

# Batch mode cannot share a project with an open editor.
if [[ -e Temp/UnityLockfile ]] && pgrep -fi -- "-projectpath ${PROJECT_ROOT}( |\$)" >/dev/null; then
  echo "The Unity editor has this project open. Close it, or use the editor bridge instead." >&2
  exit 3
fi

mkdir -p Logs

# run_unity <log-name> <unity args...>; returns Unity's exit code.
run_unity() {
  local log="Logs/cli-$1.log"; shift
  local status=0
  "$UNITY_EDITOR" -batchmode -projectPath "$PROJECT_ROOT" -logFile "$log" "$@" >/dev/null || status=$?
  UNITY_LOG="$log"
  return $status
}

# Print de-duplicated C# compiler errors from the last run's log.
print_compile_errors() {
  grep -E '\): error CS[0-9]+:|^error CS[0-9]+:' "$UNITY_LOG" | sort -u || true
}
