# Authoring and reviewing workflow

One process produces a traceable, line-level set of performance suggestions, for both authoring new code and reviewing a change. The canonical record lives in your session database so it survives and can be retrieved later; the human-readable report and any PR comments are rendered from it. Never delete a finding once recorded; qualify it instead.

## The store (your session database)

Create these two tables once per session with the SQL tool. `perf_findings` holds the suggestions; `perf_finding_sources` holds the sources each suggestion cites, kept separately for retrieval.

```sql
CREATE TABLE IF NOT EXISTS perf_findings (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  run_id TEXT,                 -- groups one authoring or review pass
  mode TEXT,                   -- 'authoring' | 'review'
  file TEXT,
  line_start INTEGER,
  line_end INTEGER,
  signal TEXT,                 -- what was detected (from signals.md)
  area TEXT,                   -- reference area, e.g. 'strings-spans'
  rule TEXT,                   -- rule anchor, e.g. 'bcl-patterns/strings-spans.md#slice-...'
  recommendation TEXT,
  before_code TEXT,
  after_code TEXT,
  complexity TEXT,             -- low | medium | high
  hot_path TEXT,               -- yes | either | cold
  confidence TEXT,             -- high | medium | low
  status TEXT DEFAULT 'candidate', -- candidate|verified|rejected|needs-info|applied
  critique TEXT,               -- why verified or rejected (the challenge result)
  created_at TEXT DEFAULT (datetime('now')),
  updated_at TEXT
);
CREATE TABLE IF NOT EXISTS perf_finding_sources (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  finding_id INTEGER,          -- references perf_findings.id
  source_type TEXT,            -- 'rule' | 'repo_example' | 'article' | 'helper'
  locator TEXT,                -- reference path#anchor, src file#Lstart-Lend, or URL
  note TEXT
);
```

## Phase 1: detect (insert candidates)

Scan the target. For review, scan the diff or the named files; for authoring, scan the code as you write it. For each line that matches a signal in [signals.md](signals.md), open the routed section, pick the highest-leverage lowest-complexity fix, and insert a `candidate` finding with its file and line range, the rule anchor, a one-line recommendation, and a concrete `before`/`after` (take the `after` shape from the rule's snippet). Record every source in `perf_finding_sources`: the rule, the repo example it cites, and the article or helper behind it. A finding with no source is not allowed.

## Phase 2: critique and verify (qualify, never delete)

Now challenge your own candidates. For each one, verify against the actual code and the rule, and ask:

- Is it really on a hot path, or did a signal fire on a cold path? (See the repo hot-path list in [decision-framework.md](decision-framework.md).)
- Does the rule actually apply to this code, or is it a superficial token match (false positive)?
- Is the `after` correct: does it compile, preserve behavior, and respect any invariant the rule names (lifetime, write-before-read, endianness)?
- Is the complexity tag right per the rubric, and is the win real here rather than theoretical?

Then qualify the finding by updating its `status` and writing the reasoning into `critique` (set `updated_at`). Do not delete rejected findings; keep them with the reason, so the same false positive is not re-raised later.

- `verified`: confirmed, correct, worth proposing.
- `rejected`: false positive, cold path, or not worth it. Keep it, with why.
- `needs-info`: cannot confirm without more context (record what is missing).

```sql
UPDATE perf_findings SET status='verified', critique='confirmed on per-request header parse; AsSpan compiles and preserves ordinal compare', updated_at=datetime('now') WHERE id=:id;
UPDATE perf_findings SET status='rejected', critique='cold path: runs once at startup', updated_at=datetime('now') WHERE id=:id;
```

## Phase 3: render the report (from curated findings only)

Generate the human-readable markdown from `status IN ('verified','applied')`, ordered by file then line. Group by file; each entry shows the line range, the recommendation, the rule citation (so it is traceable back to the reference and its evidence), and the before/after. Rejected and needs-info findings stay in the database for retrieval but are not in the report.

### Report format

Render the report with this structure. When it is posted to a PR (as a comment or inline), the heading must mark it as agent-generated and advisory, so reviewers know it was not written by a person.

```markdown
## Performance review (agent-generated)

_Generated automatically by the optimize-aspnetcore-performance skill. These are advisory suggestions, not approvals; verify before applying._

**Summary:** 3 suggestions across 2 files. Complexity: 2 low, 1 medium. All on hot paths.

### `src/Http/Http/src/HeaderDictionary.cs`

- **Lines 120-122** | low | hot path: slice instead of allocating a substring.
  Rule: [`strings-spans#slice-with-readonlyspanchar-instead-of-substring`](bcl-patterns/strings-spans.md#slice-with-readonlyspanchar-instead-of-substring)
  ```diff
  - var name = header.Substring(0, idx);
  + var name = header.AsSpan(0, idx);
  ```

### `src/Http/Routing/src/Matching/DfaMatcher.cs`

- **Lines 88-95** | medium | hot path: cache the SearchValues in a static field.
  Rule: [`searching#prefer-searchvalues-for-repeated-byte-and-char-set-search`](bcl-patterns/searching.md#prefer-searchvalues-for-repeated-byte-and-char-set-search)
  ```diff
  - if (value.IndexOfAny(s_delims) >= 0)
  + // s_delims is now a static readonly SearchValues<char>
  + if (value.IndexOfAny(s_delims) >= 0)
  ```
```

Rules:
- Keep the exact agent-generated heading and the advisory disclaimer line as the first two lines; do not drop them when posting to a PR.
- One section per file (the file path as an inline-code H3). One bullet per finding, leading with the bold line range, then complexity and hot/cold, then the recommendation.
- Always include the rule link (traceable back to the reference and its evidence) and a `diff` fenced before/after, with `-` for removed lines and `+` for added lines.
- Order files alphabetically and findings by line number.
- For inline PR comments where the fix is a small, exact replacement of the commented lines, use a GitHub `suggestion` block instead of a `diff` block, so the author can commit it with one click:
  ````markdown
  ```suggestion
  var name = header.AsSpan(0, idx);
  ```
  ````
  The suggestion body must be the complete replacement for the exact lines the comment is anchored to. Use a `suggestion` block only when the change is confined to the commented line range; otherwise use a `diff` block and describe the change.
- Put the agent-generated heading and summary in the review's top-level body when posting inline comments.

## Phase 4: deliver (author chooses the destination)

The database is the source of truth. The author decides where the report goes; offer these and do whichever is asked:

- Write to disk: save the rendered markdown to a file the author names.
- One PR comment: post the whole report as a single summary comment with `gh pr comment <pr> --body-file <report.md>`.
- Inline PR comments: post each verified finding as a review comment keyed to its file and line range with `gh pr review` or the `gh api` review-comments endpoint, then submit the review. Use a `suggestion` block for small exact changes so the author can commit them directly. Put the agent-generated heading and summary in the review body.

Do not pick a destination silently; ask which one the author wants. The findings remain in the session database regardless, for later retrieval.

## Authoring mode

While authoring, run the same detect and critique phases, then act by complexity on hot paths:

- Low complexity, hot path: apply the fix inline as you write, and record the finding with `status='applied'` and the line range you changed. These are the cheap wins; do not wait to be asked.
- Medium or high complexity: do not silently change the code. Record a `verified` finding and report it so the author can choose, including high-complexity options with their tradeoff (see the reporting rule in [decision-framework.md](decision-framework.md)).
- Cold path: record briefly as `rejected` with reason `cold path`, or skip.

## Completion criteria

You are done when every in-scope hot-path signal is either applied (authoring) or a `verified` finding with file, line range, rule, and before/after (review); every finding has at least one row in `perf_finding_sources`; rejected findings are retained with a reason; and the report renders only from curated findings. The session database, not the markdown file, is the durable record.
