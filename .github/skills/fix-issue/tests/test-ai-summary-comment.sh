#!/usr/bin/env bash
# Tests for the ai-summary-comment skill scripts.
# Validates that the scripts correctly build expandable comment sections
# and handle the directory structure from fix-issue.
#
# Usage: bash .github/skills/fix-issue/tests/test-ai-summary-comment.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
TRY_FIX_SCRIPT="$REPO_ROOT/.github/skills/ai-summary-comment/scripts/post-try-fix-comment.sh"
SUMMARY_SCRIPT="$REPO_ROOT/.github/skills/ai-summary-comment/scripts/post-ai-summary-comment.sh"

PASS=0
FAIL=0
TEMP_DIR=""

setup_temp_fixtures() {
    TEMP_DIR="$(mktemp -d)"
    local issue_dir="$TEMP_DIR/CustomAgentLogsTmp/PRState/ISSUE-99999/PRAgent"

    # Create try-fix directory structure
    mkdir -p "$issue_dir/try-fix/attempt-1"
    mkdir -p "$issue_dir/try-fix/attempt-2"
    mkdir -p "$issue_dir/pre-flight"
    mkdir -p "$issue_dir/gate"
    mkdir -p "$issue_dir/report"

    echo "Use try/catch to wrap await and throw TimeoutException" > "$issue_dir/try-fix/attempt-1/approach.md"
    echo "PASS" > "$issue_dir/try-fix/attempt-1/result.txt"
    cat > "$issue_dir/try-fix/attempt-1/fix.diff" << 'DIFF'
diff --git a/src/JSRuntime.cs b/src/JSRuntime.cs
--- a/src/JSRuntime.cs
+++ b/src/JSRuntime.cs
@@ -1,3 +1,5 @@
+try {
     return await InvokeAsync(args);
+} catch (OperationCanceledException ex) when (cts.IsCancellationRequested) {
+    throw new TimeoutException("Timed out", ex);
+}
DIFF

    echo "Use Task.WhenAny with delay task" > "$issue_dir/try-fix/attempt-2/approach.md"
    echo "FAIL" > "$issue_dir/try-fix/attempt-2/result.txt"
    echo "Test compilation failed due to missing reference" > "$issue_dir/try-fix/attempt-2/analysis.md"

    # Create summary
    cat > "$issue_dir/try-fix/content.md" << 'SUMMARY'
### Fix Exploration Summary

**Total Attempts:** 2
**Passing Candidates:** 1
**Selected Fix:** Attempt 1

| # | Model | Approach | Result |
|---|-------|----------|--------|
| 1 | claude-sonnet-4.6 | try/catch | ✅ PASS |
| 2 | claude-opus-4.6 | Task.WhenAny | ❌ FAIL |
SUMMARY

    # Create phase files
    echo "**Issue:** #99999 - Test issue" > "$issue_dir/pre-flight/content.md"
    echo "### Gate Result: ✅ PASSED" > "$issue_dir/gate/content.md"
    echo "### Final Report\n**PR:** #12345" > "$issue_dir/report/content.md"
}

cleanup() {
    [[ -n "$TEMP_DIR" ]] && rm -rf "$TEMP_DIR"
}

trap cleanup EXIT

assert_ok() {
    local description="$1"
    local exit_code="$2"
    if [[ "$exit_code" -eq 0 ]]; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description (exit code: $exit_code)"
        FAIL=$((FAIL + 1))
    fi
}

assert_output_contains() {
    local description="$1"
    local output="$2"
    local pattern="$3"

    if echo "$output" | grep -qi "$pattern"; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description"
        echo "     Pattern not found: $pattern"
        FAIL=$((FAIL + 1))
    fi
}

assert_file_contains() {
    local description="$1"
    local file="$2"
    local pattern="$3"

    if grep -qi "$pattern" "$file" 2>/dev/null; then
        echo "  ✅ $description"
        PASS=$((PASS + 1))
    else
        echo "  ❌ $description"
        echo "     Pattern not found in $file: $pattern"
        FAIL=$((FAIL + 1))
    fi
}

# ============================================================================
echo "╔═══════════════════════════════════════════════════════════╗"
echo "║  AI Summary Comment Script Tests                          ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""

# ============================================================================
echo "📂 Test 1: Scripts are executable"
# ============================================================================
if [[ -x "$TRY_FIX_SCRIPT" ]]; then
    echo "  ✅ post-try-fix-comment.sh is executable"
    PASS=$((PASS + 1))
else
    echo "  ❌ post-try-fix-comment.sh is not executable"
    FAIL=$((FAIL + 1))
fi

if [[ -x "$SUMMARY_SCRIPT" ]]; then
    echo "  ✅ post-ai-summary-comment.sh is executable"
    PASS=$((PASS + 1))
else
    echo "  ❌ post-ai-summary-comment.sh is not executable"
    FAIL=$((FAIL + 1))
fi

echo ""

# ============================================================================
echo "📝 Test 2: Try-fix script dry run with test fixtures"
# ============================================================================
setup_temp_fixtures

# Override REPO_ROOT in the script by cd-ing into the temp dir
PREV_DIR="$(pwd)"
cd "$TEMP_DIR"
git init -q . 2>/dev/null || true

OUTPUT="$(DRY_RUN=1 bash "$TRY_FIX_SCRIPT" 99999 2>&1)" || true
EXIT_CODE=$?
cd "$PREV_DIR"

assert_ok "Dry run exits successfully" "$EXIT_CODE"
assert_output_contains "Output contains AI Summary marker" "$OUTPUT" "AI Summary"
assert_output_contains "Output contains TRY-FIX section marker" "$OUTPUT" "SECTION:TRY-FIX"
assert_output_contains "Output contains expandable details tag" "$OUTPUT" "<details>"
assert_output_contains "Output contains attempt 1" "$OUTPUT" "Attempt 1"
assert_output_contains "Output contains attempt 2" "$OUTPUT" "Attempt 2"
assert_output_contains "Output shows PASS for attempt 1" "$OUTPUT" "✅"
assert_output_contains "Output shows FAIL for attempt 2" "$OUTPUT" "❌"
assert_output_contains "Output contains summary table" "$OUTPUT" "Fix Exploration Summary"
assert_output_contains "Output contains diff content" "$OUTPUT" "TimeoutException"

echo ""

# ============================================================================
echo "📝 Test 3: Try-fix script single attempt mode"
# ============================================================================
cd "$TEMP_DIR"
OUTPUT="$(DRY_RUN=1 bash "$TRY_FIX_SCRIPT" 99999 1 2>&1)" || true
EXIT_CODE=$?
cd "$PREV_DIR"

assert_ok "Single attempt dry run exits successfully" "$EXIT_CODE"
assert_output_contains "Single attempt shows attempt 1" "$OUTPUT" "Attempt 1"

echo ""

# ============================================================================
echo "📝 Test 4: Preview file is created"
# ============================================================================
PREVIEW_FILE="$TEMP_DIR/CustomAgentLogsTmp/PRState/ISSUE-99999/ai-summary-preview.md"
if [[ -f "$PREVIEW_FILE" ]]; then
    echo "  ✅ Preview file created at expected path"
    PASS=$((PASS + 1))
    assert_file_contains "Preview has AI Summary marker" "$PREVIEW_FILE" "AI Summary"
    assert_file_contains "Preview has section markers" "$PREVIEW_FILE" "SECTION:TRY-FIX"
    assert_file_contains "Preview has expandable sections" "$PREVIEW_FILE" "<details>"
else
    echo "  ❌ Preview file not created"
    FAIL=$((FAIL + 1))
fi

echo ""

# ============================================================================
echo "📝 Test 5: Script validates required arguments"
# ============================================================================
OUTPUT="$(bash "$TRY_FIX_SCRIPT" 2>&1)" && EXIT_CODE=$? || EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then
    echo "  ✅ Script fails without issue number"
    PASS=$((PASS + 1))
else
    echo "  ❌ Script should fail without issue number"
    FAIL=$((FAIL + 1))
fi

echo ""

# ============================================================================
echo "📝 Test 6: Comment structure has proper nesting"
# ============================================================================
if [[ -f "$PREVIEW_FILE" ]]; then
    # Count opening/closing details tags
    OPEN_DETAILS=$(grep -c "<details>" "$PREVIEW_FILE" 2>/dev/null || echo 0)
    CLOSE_DETAILS=$(grep -c "</details>" "$PREVIEW_FILE" 2>/dev/null || echo 0)

    if [[ "$OPEN_DETAILS" -eq "$CLOSE_DETAILS" ]]; then
        echo "  ✅ Balanced <details> tags ($OPEN_DETAILS open, $CLOSE_DETAILS close)"
        PASS=$((PASS + 1))
    else
        echo "  ❌ Unbalanced <details> tags ($OPEN_DETAILS open, $CLOSE_DETAILS close)"
        FAIL=$((FAIL + 1))
    fi

    if [[ "$OPEN_DETAILS" -ge 3 ]]; then
        echo "  ✅ Has multiple expandable sections ($OPEN_DETAILS total)"
        PASS=$((PASS + 1))
    else
        echo "  ❌ Expected at least 3 expandable sections (got $OPEN_DETAILS)"
        FAIL=$((FAIL + 1))
    fi
fi

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
