#!/usr/bin/env bash
# Set PR description to the AI Summary content.
# Reads pre-flight, test, gate, and fix phase outputs and sets them
# as the PR body using nested expandable <details> sections.
#
# Usage:
#   bash post-ai-summary-comment.sh <IssueNumber> <PRNumber>
#   DRY_RUN=1 bash post-ai-summary-comment.sh <IssueNumber> <PRNumber>

set -euo pipefail

REPO="dotnet/aspnetcore"
MAIN_MARKER="<!-- AI Summary -->"
SECTION_START="<!-- SECTION:PR-REVIEW -->"
SECTION_END="<!-- /SECTION:PR-REVIEW -->"

ISSUE_NUMBER="${1:?Usage: post-ai-summary-comment.sh <IssueNumber> <PRNumber>}"
PR_NUMBER="${2:?Usage: post-ai-summary-comment.sh <IssueNumber> <PRNumber>}"
DRY_RUN="${DRY_RUN:-}"

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
STATE_BASE="$REPO_ROOT/CustomAgentLogsTmp/PRState/ISSUE-${ISSUE_NUMBER}/PRAgent"

# ============================================================================
# Load phase content
# ============================================================================
load_phase() {
    local phase="$1"
    local file="$STATE_BASE/$phase/content.md"
    if [[ -f "$file" ]]; then
        cat "$file"
    else
        echo "_Phase not completed yet._"
    fi
}

PREFLIGHT_CONTENT="$(load_phase "pre-flight")"
TEST_CONTENT="$(load_phase "test")"
GATE_CONTENT="$(load_phase "gate")"

# ============================================================================
# Build fix section with per-attempt expandable details
# ============================================================================
TRY_FIX_BASE="$STATE_BASE/try-fix"
TRYFIX_SUMMARY=""
if [[ -f "$TRY_FIX_BASE/content.md" ]]; then
    TRYFIX_SUMMARY="$(cat "$TRY_FIX_BASE/content.md")"
fi

build_attempt_section() {
    local dir="$1"
    local num="$2"
    local approach="" result_status="" diff_content="" analysis=""

    if [[ -f "$dir/approach.md" ]]; then
        approach="$(cat "$dir/approach.md")"
    elif [[ -f "$dir/approach.txt" ]]; then
        approach="$(cat "$dir/approach.txt")"
    else
        approach="_No approach description available_"
    fi

    if [[ -f "$dir/result.txt" ]]; then
        local raw
        raw="$(head -1 "$dir/result.txt" | tr '[:lower:]' '[:upper:]')"
        case "$raw" in
            *PASS*) result_status="PASS" ;;
            *FAIL*) result_status="FAIL" ;;
            *) result_status="UNKNOWN" ;;
        esac
    else
        result_status="UNKNOWN"
    fi

    local emoji="⚪"
    [[ "$result_status" == "PASS" ]] && emoji="✅"
    [[ "$result_status" == "FAIL" ]] && emoji="❌"

    if [[ -f "$dir/fix.diff" ]]; then
        diff_content="$(cat "$dir/fix.diff")"
    fi
    if [[ -f "$dir/analysis.md" ]]; then
        analysis="$(cat "$dir/analysis.md")"
    fi

    local section=""
    section+="<details>"$'\n'
    section+="<summary>${emoji} <b>Attempt ${num}: ${result_status}</b></summary>"$'\n'
    section+=""$'\n'
    section+="${approach}"$'\n'
    section+=""$'\n'
    if [[ -n "$diff_content" ]]; then
        section+="<details>"$'\n'
        section+="<summary>📄 Diff</summary>"$'\n'
        section+=""$'\n'
        section+="\`\`\`diff"$'\n'
        section+="${diff_content}"$'\n'
        section+="\`\`\`"$'\n'
        section+=""$'\n'
        section+="</details>"$'\n'
        section+=""$'\n'
    fi
    if [[ -n "$analysis" ]]; then
        section+="${analysis}"$'\n'
        section+=""$'\n'
    fi
    section+="</details>"$'\n'
    echo "$section"
}

ATTEMPT_SECTIONS=""
PASS_COUNT=0
FAIL_COUNT=0
TOTAL_COUNT=0

if [[ -d "$TRY_FIX_BASE" ]]; then
    for attempt_dir in "$TRY_FIX_BASE"/attempt-*/; do
        [[ -d "$attempt_dir" ]] || continue
        num="$(basename "$attempt_dir" | sed 's/attempt-//')"
        section="$(build_attempt_section "$attempt_dir" "$num")"
        ATTEMPT_SECTIONS+="${section}"$'\n'
        TOTAL_COUNT=$((TOTAL_COUNT + 1))
        if grep -qi "pass" "$attempt_dir/result.txt" 2>/dev/null; then
            PASS_COUNT=$((PASS_COUNT + 1))
        else
            FAIL_COUNT=$((FAIL_COUNT + 1))
        fi
    done
fi

# Build fix status line
FIX_STATUS=""
[[ $PASS_COUNT -gt 0 ]] && FIX_STATUS+="✅ ${PASS_COUNT} passed"
if [[ $FAIL_COUNT -gt 0 ]]; then
    [[ -n "$FIX_STATUS" ]] && FIX_STATUS+=", "
    FIX_STATUS+="❌ ${FAIL_COUNT} failed"
fi
FIX_LABEL="🔧 Fix — Analysis & Comparison"
[[ -n "$FIX_STATUS" ]] && FIX_LABEL+=" (${FIX_STATUS})"

# Compose the full fix content
TRYFIX_CONTENT="${TRYFIX_SUMMARY}"
if [[ -n "$ATTEMPT_SECTIONS" ]]; then
    TRYFIX_CONTENT+=$'\n\n'"${ATTEMPT_SECTIONS}"
fi
if [[ -z "$TRYFIX_CONTENT" || "$TRYFIX_CONTENT" == $'\n\n' ]]; then
    TRYFIX_CONTENT="_Phase not completed yet._"
fi

# ============================================================================
# Build nested expandable sections
# ============================================================================
PR_REVIEW_CONTENT="<details>
<summary><b>🔍 Automated Fix Report</b></summary>

---

<details>
<summary><b>🔍 Pre-Flight — Context & Validation</b></summary>

${PREFLIGHT_CONTENT}

</details>

---

<details>
<summary><b>🧪 Test — Bug Reproduction</b></summary>

${TEST_CONTENT}

</details>

---

<details>
<summary><b>🚦 Gate — Test Verification & Regression</b></summary>

${GATE_CONTENT}

</details>

---

<details>
<summary><b>${FIX_LABEL}</b></summary>

${TRYFIX_CONTENT}

</details>

</details>"

PR_REVIEW_SECTION="${SECTION_START}
${PR_REVIEW_CONTENT}
${SECTION_END}"

# ============================================================================
# Build PR description body
# ============================================================================
PR_BODY="${MAIN_MARKER}

## 🤖 AI Summary

${PR_REVIEW_SECTION}"

# ============================================================================
# Set PR description or preview
# ============================================================================
if [[ -n "$DRY_RUN" ]]; then
    PREVIEW_FILE="$REPO_ROOT/CustomAgentLogsTmp/PRState/ISSUE-${ISSUE_NUMBER}/ai-summary-preview.md"
    mkdir -p "$(dirname "$PREVIEW_FILE")"
    echo "$PR_BODY" > "$PREVIEW_FILE"
    echo ""
    echo "=== DRY RUN PREVIEW ==="
    echo "$PR_BODY"
    echo "=== END PREVIEW ==="
    echo ""
    echo "✅ Preview saved to: $PREVIEW_FILE"
    exit 0
fi

TEMP_FILE="$(mktemp)"
jq -n --arg body "$PR_BODY" '{"body": $body}' > "$TEMP_FILE"

echo "📝 Setting PR #${PR_NUMBER} description..."
RESULT="$(gh api --method PATCH "repos/${REPO}/pulls/${PR_NUMBER}" --input "$TEMP_FILE" --jq '.html_url')"
echo "✅ PR description updated: $RESULT"

rm -f "$TEMP_FILE"
