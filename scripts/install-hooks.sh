#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"

echo "Installing git hooks..."

# Ensure .husky/_/husky.sh shim exists
if [ ! -f "$REPO_ROOT/.husky/_/husky.sh" ]; then
  mkdir -p "$REPO_ROOT/.husky/_"
  cat > "$REPO_ROOT/.husky/_/husky.sh" << 'EOF'
#!/usr/bin/env sh
if [ -z "$husky_skip_init" ]; then
  debug () {
    if [ "$HUSKY_DEBUG" = "1" ]; then
      echo "husky (debug) - $1"
    fi
  }

  readonly hook_name="$(basename -- "$0")"
  debug "starting $hook_name..."

  if [ "$HUSKY" = "0" ]; then
    debug "HUSKY env variable is set to 0, skipping hook"
    exit 0
  fi
fi
EOF
  chmod +x "$REPO_ROOT/.husky/_/husky.sh"
fi

# Configure git to use .husky as the hooks directory
git -C "$REPO_ROOT" config core.hooksPath .husky

chmod +x "$REPO_ROOT/.husky/pre-commit"

# Verify gitleaks
if ! command -v gitleaks >/dev/null 2>&1; then
  echo ""
  echo "⚠️  IMPORTANT: gitleaks is NOT installed."
  echo "   Install it before making any commits:"
  echo "   macOS:   brew install gitleaks"
  echo "   Windows: winget install gitleaks"
  echo ""
  echo "   The pre-commit hook will BLOCK commits until gitleaks is installed."
else
  echo "✓ gitleaks found: $(gitleaks version)"
fi

echo "✓ Git hooks installed (core.hooksPath = .husky)"
