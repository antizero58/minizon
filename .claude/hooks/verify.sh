#!/usr/bin/env bash
# Stop hook: when Claude finishes, check the .NET solution and the frontend.
#   .NET:     dotnet build, then fast tests only (Category!=Integration).
#   Frontend: npm run build and npm test in src/Web (only if npm and node_modules exist).
# Exit 2 returns stderr to Claude so it keeps working; exit 0 lets it stop.
# State: .claude/.last-verify holds the hash of the last fully successful check.

input=$(cat)

# When Claude is already continuing because of this hook, we still verify,
# but never block again: a failure only becomes a warning for the user.
continuing=0
echo "$input" | grep -q '"stop_hook_active"[[:space:]]*:[[:space:]]*true' && continuing=1

cd "$CLAUDE_PROJECT_DIR" || exit 0

state_file=".claude/.last-verify"
log_file=".claude/.last-verify.log"

# Files that differ from main (committed or not) plus untracked ones.
changed=$( { git diff --name-only main 2>/dev/null; git ls-files --others --exclude-standard; } | grep -v '/node_modules/' | sort -u )

want_dotnet=0
if echo "$changed" | grep -qE '\.(cs|csproj|props|targets|proto)$' &&
   find . -maxdepth 5 \( -name '*.sln' -o -name '*.slnx' -o -name '*.csproj' \) -not -path '*/node_modules/*' | grep -q .; then
  want_dotnet=1
fi

want_web=0
echo "$changed" | grep -qE '^src/Web/.*\.(ts|tsx|json|css)$' && want_web=1

[ "$want_dotnet" = 0 ] && [ "$want_web" = 0 ] && exit 0

# Hash of the working state: the diff against main plus the CONTENT of untracked files
# (names alone would miss edits to a file that is not tracked yet).
compute_hash() {
  {
    git diff main 2>/dev/null
    git ls-files --others --exclude-standard -z | while IFS= read -r -d '' f; do
      printf '%s ' "$f"
      git hash-object -- "$f" 2>/dev/null
    done
  } | git hash-object --stdin
}

current_hash=$(compute_hash)
if [ -f "$state_file" ] && [ "$(cat "$state_file")" = "$current_hash" ]; then
  exit 0
fi

failed=()
skipped=()
report=""

# fail <step name> <output> <lines to keep>
fail() {
  failed+=("$1")
  report+="$1 failed:"$'\n'"$(echo "$2" | tail -n "$3")"$'\n\n'
}

if [ "$want_dotnet" = 1 ]; then
  if out=$(dotnet build --nologo -v q 2>&1); then
    out=$(dotnet test --no-build --nologo --filter "Category!=Integration" 2>&1) || fail "dotnet test" "$out" 80
  else
    fail "dotnet build" "$out" 60
  fi
fi

if [ "$want_web" = 1 ]; then
  if ! command -v npm >/dev/null 2>&1; then
    skipped+=("npm не найден, фронтенд не проверен")
  elif [ ! -d src/Web/node_modules ]; then
    skipped+=("нет src/Web/node_modules (не выполнен npm install), фронтенд не проверен")
  else
    out=$(cd src/Web && npm run build 2>&1) || fail "npm run build" "$out" 60
    # CI=true makes Vitest run once instead of staying in watch mode.
    out=$(cd src/Web && CI=true npm test 2>&1) || fail "npm test" "$out" 80
  fi
fi

if [ "${#failed[@]}" -gt 0 ]; then
  printf '%s' "$report" > "$log_file"
  if [ "$continuing" = 1 ]; then
    # Second attempt after a block: do not block again, warn the user instead.
    names=$(IFS=,; echo "${failed[*]}")
    echo "{\"systemMessage\": \"Проверка не прошла и не исправлена: $names. Полный вывод: .claude/.last-verify.log\"}"
    exit 0
  fi
  printf '%s' "$report" >&2
  exit 2
fi

if [ "${#skipped[@]}" -gt 0 ]; then
  # Something was not checked: do not remember this state as verified.
  msg=$(IFS=';'; echo "${skipped[*]}")
  echo "{\"systemMessage\": \"Проверка пропущена: $msg.\"}"
  exit 0
fi

echo "$current_hash" > "$state_file"
exit 0
