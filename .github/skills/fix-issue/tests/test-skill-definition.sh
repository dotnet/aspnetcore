#!/usr/bin/env bash
# Tests for the fix-issue skill definition.
# Validates that SKILL.md enforces mandatory multi-model exploration
# and integrates with the ai-summary-comment skill.
#
# Usage: bash .github/skills/fix-issue/tests/test-skill-definition.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_FILE="$SCRIPT_DIR/../SKILL.md"
AI_SUMMARY_SKILL="$SCRIPT_DIR/../../ai-summary-comment/SKILL.md"

PASS=0
FAIL=0

assert_contains() {
    local description="$1"
    local file="$2"
    local pattern="$3"

    if grep -qi "$pattern" "$file" 2>/dev/null; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description"
        echo "     Pattern not found: $pattern"
        FAIL=$((FAIL + 1))
    fi
}

assert_contains_exact() {
    local description="$1"
    local file="$2"
    local pattern="$3"

    if grep -q "$pattern" "$file" 2>/dev/null; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description"
        echo "     Pattern not found: $pattern"
        FAIL=$((FAIL + 1))
    fi
}

assert_file_exists() {
    local description="$1"
    local file="$2"

    if [[ -f "$file" ]]; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description"
        echo "     File not found: $file"
        FAIL=$((FAIL + 1))
    fi
}

count_occurrences() {
    local file="$1"
    local pattern="$2"
    grep -ci "$pattern" "$file" 2>/dev/null || echo 0
}

# ============================================================================
echo "╔═══════════════════════════════════════════════════════════╗"
echo "║  Fix-Issue Skill Tests                                    ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# ============================================================================
echo "📂 Test 1: Skill files exist"
# ============================================================================
assert_file_exists "fix-issue SKILL.md exists" "$SKILL_FILE"
assert_file_exists "ai-summary-comment SKILL.md exists" "$AI_SUMMARY_SKILL"
assert_file_exists "post-try-fix-comment.sh exists" "$SCRIPT_DIR/../../ai-summary-comment/scripts/post-try-fix-comment.sh"
assert_file_exists "post-ai-summary-comment.sh exists" "$SCRIPT_DIR/../../ai-summary-comment/scripts/post-ai-summary-comment.sh"

echo ""

# ============================================================================
echo "🔒 Test 2: Multi-model exploration is MANDATORY"
# ============================================================================
assert_contains "Contains 'MANDATORY' keyword" "$SKILL_FILE" "mandatory"
assert_contains "Contains 'NEVER SKIP' language" "$SKILL_FILE" "never skip"
assert_contains "Contains 'NO EXCEPTIONS' language" "$SKILL_FILE" "no exceptions"
assert_contains "Contains negative rule about skipping Phase 3" "$SKILL_FILE" "never skip phase 3"

MANDATORY_COUNT=$(count_occurrences "$SKILL_FILE" "mandatory")
if [[ "$MANDATORY_COUNT" -ge 3 ]]; then
    echo "  ✅ 'MANDATORY' appears at least 3 times ($MANDATORY_COUNT occurrences)"
    PASS=$((PASS + 1))
else
    echo "  ❌ 'MANDATORY' appears only $MANDATORY_COUNT times (expected >= 3)"
    FAIL=$((FAIL + 1))
fi

echo ""

# ============================================================================
echo "🤖 Test 3: All 5 models are specified"
# ============================================================================
assert_contains "Lists claude-sonnet-4.6" "$SKILL_FILE" "claude-sonnet-4.6"
assert_contains "Lists claude-opus-4.6" "$SKILL_FILE" "claude-opus-4.6"
assert_contains "Lists gpt-5.2" "$SKILL_FILE" "gpt-5.2"
assert_contains "Lists gpt-5.3-codex" "$SKILL_FILE" "gpt-5.3-codex"
assert_contains "Lists gemini-3-pro-preview" "$SKILL_FILE" "gemini-3-pro-preview"

echo ""

# ============================================================================
echo "🔄 Test 4: Cross-pollination is required"
# ============================================================================
assert_contains "Mentions cross-pollination" "$SKILL_FILE" "cross-pollination"
assert_contains "Requires at least 2 models for cross-pollination" "$SKILL_FILE" "at least 2 models"
assert_contains "Mentions 'NO NEW IDEAS' termination" "$SKILL_FILE" "NO NEW IDEAS"

echo ""

# ============================================================================
echo "💬 Test 5: AI Summary comment integration"
# ============================================================================
assert_contains "References ai-summary-comment skill" "$SKILL_FILE" "ai-summary-comment"
assert_contains "Includes post-try-fix-comment.sh command" "$SKILL_FILE" "post-try-fix-comment"
assert_contains "Includes post-ai-summary-comment.sh command" "$SKILL_FILE" "post-ai-summary-comment"
assert_contains "Comment posting is mandatory" "$SKILL_FILE" "MUST post"

echo ""

# ============================================================================
echo "📋 Test 6: All 4 phases are documented"
# ============================================================================
assert_contains "Phase 1: Pre-Flight documented" "$SKILL_FILE" "phase 1.*pre-flight"
assert_contains "Phase 2: Gate documented" "$SKILL_FILE" "phase 2.*gate"
assert_contains "Phase 3: Fix documented" "$SKILL_FILE" "phase 3.*fix"
assert_contains "Phase 4: Report documented" "$SKILL_FILE" "phase 4.*report"

echo ""

# ============================================================================
echo "📊 Test 7: Output directory structure is specified"
# ============================================================================
assert_contains "Specifies pre-flight directory" "$SKILL_FILE" "pre-flight/"
assert_contains "Specifies gate directory" "$SKILL_FILE" "gate/"
assert_contains "Specifies try-fix directory" "$SKILL_FILE" "try-fix/"
assert_contains "Specifies report directory" "$SKILL_FILE" "report/"
assert_contains "Specifies attempt subdirectories" "$SKILL_FILE" "attempt-{N}"
assert_contains "Specifies approach.md" "$SKILL_FILE" "approach.md"
assert_contains "Specifies result.txt" "$SKILL_FILE" "result.txt"
assert_contains "Specifies fix.diff" "$SKILL_FILE" "fix.diff"

echo ""

# ============================================================================
echo "🛡️ Test 8: Critical rules are present"
# ============================================================================
assert_contains "No git checkout in Phase 1-2" "$SKILL_FILE" "never run.*git checkout"
assert_contains "No asking user" "$SKILL_FILE" "never stop and ask"
assert_contains "Source activate.sh" "$SKILL_FILE" "source activate.sh"
assert_contains "Co-authored-by trailer required" "$SKILL_FILE" "Co-authored-by"
assert_contains "gh repo set-default" "$SKILL_FILE" "gh repo set-default"

echo ""

# ============================================================================
echo "⚠️ Test 9: Enforcement section exists"
# ============================================================================
assert_contains "Has enforcement section" "$SKILL_FILE" "Multi-Model Try-Fix Enforcement"
assert_contains "Even if fix seems trivial" "$SKILL_FILE" "trivial"
assert_contains "Even if fix seems obvious" "$SKILL_FILE" "obvious"
assert_contains "Minimum: all 5 models attempted" "$SKILL_FILE" "all 5 models were attempted"

echo ""

# ============================================================================
echo "📝 Test 10: Comparison criteria documented"
# ============================================================================
assert_contains "Correctness criterion" "$SKILL_FILE" "correctness"
assert_contains "Simplicity criterion" "$SKILL_FILE" "simplicity"
assert_contains "Robustness criterion" "$SKILL_FILE" "robustness"
assert_contains "Backward compatibility criterion" "$SKILL_FILE" "backward compatibility"

echo ""

# ============================================================================
# Summary
# ============================================================================
TOTAL=$((PASS + FAIL))
echo "═══════════════════════════════════════════════════════════"
echo "Results: $PASS/$TOTAL passed, $FAIL failed"
echo "═══════════════════════════════════════════════════════════"

if [[ $FAIL -gt 0 ]]; then
    echo "❌ TESTS FAILED"
    exit 1
else
    echo "✅ ALL TESTS PASSED"
    exit 0
fi
