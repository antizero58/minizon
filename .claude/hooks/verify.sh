#!/usr/bin/env bash
# Stop hook: when Claude finishes, build and test the .NET solution.
# Exit 2 returns stderr to Claude so it keeps working; exit 0 lets it stop.

input=$(cat)

# Already continuing because of this hook: do not loop.
echo "$input" | grep -q '"stop_hook_active"[[:space:]]*:[[:space:]]*true' && exit 0

cd "$CLAUDE_PROJECT_DIR" || exit 0

# No .NET projects yet: nothing to check.
find . -maxdepth 5 \( -name '*.sln' -o -name '*.slnx' -o -name '*.csproj' \) -not -path '*/node_modules/*' | grep -q . || exit 0

# Run only if .NET files differ from main (committed or not) or are untracked.
changed=$( { git diff --name-only main 2>/dev/null; git ls-files --others --exclude-standard; } )
echo "$changed" | grep -qE '\.(cs|csproj|props|targets|proto)$' || exit 0

out=$(dotnet build --nologo -v q 2>&1) || {
  echo "dotnet build failed:" >&2
  echo "$out" | tail -n 60 >&2
  exit 2
}

# Integration tests use Testcontainers and need Docker: without it, skip tests and tell the user.
if ! docker info >/dev/null 2>&1; then
  echo '{"systemMessage": "Docker is not running: dotnet build passed, dotnet test was skipped."}'
  exit 0
fi

out=$(dotnet test --no-build --nologo 2>&1) || {
  echo "dotnet test failed:" >&2
  echo "$out" | tail -n 80 >&2
  exit 2
}

exit 0
