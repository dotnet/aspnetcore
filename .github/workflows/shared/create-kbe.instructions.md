# Shared ASP.NET Core KBE Analysis Instructions

Use these rules when one concrete ASP.NET Core test failure might become a
Build Insights `Known Build Error`.

The workflow's deterministic collector decides whether the test satisfies the
minimum Case A quarantine gates. The agent may choose only an exact test listed
in the injected eligible-test array. These instructions cover duplicate
detection and matcher selection; they do not permit overriding a missing,
ineligible, or unproven collector receipt.

## Search for an existing issue

Search open `dotnet/aspnetcore` issues in this order:

1. The exact fully qualified test name with `label:"Known Build Error"`.
2. The test class name with `label:"Known Build Error"`.
3. A specific assertion or exception phrase with
   `label:"Known Build Error"`.
4. The exact test name without a label filter, to find an existing human
   tracker.

Scan the first ten best matches for each query. Read candidate bodies and verify
that the test, failure signature, platform, and configuration describe the same
failure. A same-class issue with a different assertion is not a duplicate.

If the open search misses, repeat the exact test-name and specific-message
searches for `Known Build Error` issues closed within the last 30 days.

Classify the duplicate search as exactly one of:

- `none` - no matching issue found;
- `existing-open` - an open matching KBE or quarantine tracker exists;
- `recently-closed` - a matching issue closed within 30 days;
- `ambiguous` - multiple plausible matches cannot be distinguished;
- `filtered` - integrity filtering hid a plausible result;
- `search-failed` - the search could not be completed reliably.

Only `none` permits a newly created issue to activate Build Insights matching.
All other outcomes must create, at most, an ordinary `test-failure` quarantine
issue. Do not comment on or rewrite an existing KBE as part of this analysis.

## Select a matcher

Use the following preference order.

### Literal message

Prefer one exact, case-sensitive substring copied from the failure message or
stack trace. It must identify the failure mode rather than merely the test.

### Ordered literal array

Use an ordered array when no single line is specific enough. Every element:

- represents one line;
- is copied verbatim;
- contributes meaningful specificity;
- appears in order in the supplied evidence.

Do not pad arrays with generic text.

### Bounded regex

Use a regex only when volatile text prevents a safe literal. The quarantine
handler accepts a conservative syntax shared by JavaScript and .NET
non-backtracking regex:

- anchor the pattern with `^`;
- keep it on one line and under 300 characters;
- use `[^\n]*` instead of `.*`;
- do not use groups, alternation, shorthand classes/boundaries such as `\w`,
  `\d`, `\s`, or `\b`, lookarounds, backreferences, inline options, Unicode
  categories, or nested quantifiers;
- keep every explicit quantifier bound at or below 10,000;
- include a literal token of at least eight characters.

Build Insights evaluates patterns case-insensitively with single-line,
non-backtracking behavior. The handler independently compiles and verifies the
accepted subset before activating the KBE.

## Reject broad matchers

Never emit a matcher consisting only of:

- a fully qualified test name or test-name prefix;
- a stack-frame line or source method;
- a bare exception type;
- `Assert.True() Failure`, `Assert.Equal() Failure`, or another generic
  assertion stem;
- a generic timeout, connection-reset, disk-space, crash, signal, or exit-code
  message;
- a path, GUID, timestamp, duration, port, address, or run-specific identifier.

If the evidence cannot produce a matcher meeting this bar, use
`matcher_kind: incomplete` and explain why.

## Verify before recording

Before calling the quarantine issue tool:

1. Confirm the exact test identity is present in the workflow's injected
   deterministic Case A eligible-test array. Source C-only tests are not
   eligible for automatic KBE activation.
2. Confirm every literal or array element occurs verbatim in that test's
   failure message or stack trace.
3. Confirm the regex matches that evidence when regex is necessary.
4. Confirm the matcher does not also describe a different failure record in the
   injected evidence snapshot.
5. Record the duplicate-search classification and a concise summary.

The deterministic handler verifies the collector-authored receipt, repeats the
evidence and specificity checks, renders the final four-key JSON block, fixes
`BuildRetry` to `false`, fixes `ExcludeConsoleLog` to `true`, and decides whether
the `Known Build Error` label is permitted. Never construct or paste that JSON
yourself.
