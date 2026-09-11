import assert from "node:assert/strict";
import test from "node:test";
import { buildIssueSessionPrompt, createForegroundHandoff, validateLiveQueueIssue } from "./handoff.mjs";

const item = { id: "01234567-0123-4012-8012-0123456789ab", repository: "dotnet/aspnetcore", number: 123, url: "https://github.com/dotnet/aspnetcore/issues/123", area: "area-blazor" };
const live = { repository: item.repository, number: item.number, url: item.url, state: "open", milestone: null, labels: [item.area], isPullRequest: false };

test("child prompt contains only canonical identity and fresh nested session fields", () => {
  const prompt = buildIssueSessionPrompt(123, "new-child");
  assert.match(prompt, /create_session/);
  assert.match(prompt, /workspace_type: "worktree"/);
  assert.match(prompt, /detached: false/);
  assert.match(prompt, /coordinate_with_creator: false/);
  assert.match(prompt, /kickoff: \{ mode: "interactive"/);
  assert.match(prompt, /investigate-issue/);
  assert.doesNotMatch(prompt, /open_issue_session|model:|base_branch:|issue_title:/);
});
test("current prompt researches here without requesting another session", () => {
  const prompt = buildIssueSessionPrompt(123, "current");
  assert.match(prompt, /here in this foreground session/);
  assert.match(prompt, /investigate-issue/);
  assert.match(prompt, /https:\/\/github.com\/dotnet\/aspnetcore\/issues\/123/);
  assert.doesNotMatch(prompt, /create_session|open_issue_session|kickoff:|model:/);
});
test("missing and invalid destinations reject before reading or sending", async () => {
  let reads = 0;
  let sends = 0;
  const handoff = createForegroundHandoff({
    readIssue: async () => { reads++; return live; },
    send: async () => { sends++; return "receipt"; },
  });
  for (const destination of [undefined, null, "", "reuse", {}, ["current"]]) {
    assert.throws(() => buildIssueSessionPrompt(123, destination), { code: "invalid_destination" });
    await assert.rejects(handoff.dispatch(item, destination), { code: "invalid_destination" });
  }
  assert.equal(reads, 0);
  assert.equal(sends, 0);
});
test("canonical validation rejects stale public issues before dispatch", async () => {
  await assert.rejects(validateLiveQueueIssue(item, { readIssue: async () => ({ ...live, state: "closed" }) }), { code: "stale_issue" });
});
test("foreground dispatch sends once and reports an ambiguous receipt honestly", async () => {
  let calls = 0;
  const handoff = createForegroundHandoff({ send: async () => { calls++; return ""; }, readIssue: async () => live });
  assert.deepEqual(await handoff.dispatch(item, "current"), { status: "unknown", queued: null, messageId: null });
  assert.equal(calls, 1);
});
test("dispatch rechecks cancellation after canonical validation", async () => {
  let send = false;
  const handoff = createForegroundHandoff({ send: async () => { send = true; }, readIssue: async () => live });
  assert.equal((await handoff.dispatch(item, "current", () => false)).status, "cancelled");
  assert.equal(send, false);
});

test("concurrent panels cannot submit duplicate foreground requests", async () => {
  const entered = Promise.withResolvers();
  const release = Promise.withResolvers();
  let sends = 0;
  const handoff = createForegroundHandoff({
    readIssue: async () => { entered.resolve(); await release.promise; return live; },
    send: async () => { sends++; return "receipt"; },
  });
  const first = handoff.dispatch(item, "current");
  await entered.promise;
  await assert.rejects(handoff.dispatch(item, "new-child"), { code: "handoff_in_progress" });
  release.resolve();
  assert.equal((await first).status, "sent");
  assert.equal(sends, 1);
});

test("live canonical failures never send a foreground request", async () => {
  for (const change of [
    { repository: "other/repo" },
    { number: 456 },
    { url: "https://example.invalid/issues/123" },
    { isPullRequest: true },
    { state: "closed" },
    { milestone: { number: 1 } },
    { labels: ["area-mvc"] },
    { labels: [item.area, "Needs: Author Feedback"] },
  ]) {
    let sends = 0;
    const handoff = createForegroundHandoff({
      readIssue: async () => ({ ...live, ...change }),
      send: async () => { sends++; return "receipt"; },
    });
    for (const destination of ["current", "new-child"]) {
      await assert.rejects(handoff.dispatch(item, destination), undefined, JSON.stringify({ change, destination }));
    }
    assert.equal(sends, 0, JSON.stringify(change));
  }
});

test("cancellation during cosmetic logging prevents dispatch", async () => {
  let current = true;
  let sends = 0;
  const handoff = createForegroundHandoff({
    readIssue: async () => live,
    log: async () => { current = false; },
    send: async () => { sends++; return "receipt"; },
  });
  assert.equal((await handoff.dispatch(item, "new-child", () => current)).status, "cancelled");
  assert.equal(sends, 0);
});

test("cosmetic log failures do not suppress a successful dispatch", async () => {
  const handoff = createForegroundHandoff({
    readIssue: async () => live,
    log: async () => { throw new Error("timeline unavailable"); },
    send: async () => "receipt",
    isBusy: () => true,
  });
  assert.deepEqual(await handoff.dispatch(item, "current"), { status: "sent", queued: true, messageId: "receipt" });
});
