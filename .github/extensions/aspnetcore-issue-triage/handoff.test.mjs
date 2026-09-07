import assert from "node:assert/strict";
import test from "node:test";
import { buildIssueSessionPrompt, createForegroundHandoff, validateLiveQueueIssue } from "./handoff.mjs";

const item = { id: "01234567-0123-4012-8012-0123456789ab", repository: "dotnet/aspnetcore", number: 123, url: "https://github.com/dotnet/aspnetcore/issues/123", area: "area-blazor" };
const live = { repository: item.repository, number: item.number, url: item.url, state: "open", milestone: null, labels: [item.area], isPullRequest: false };

test("handoff prompt contains only canonical identity and fixed issue-session fields", () => {
  const prompt = buildIssueSessionPrompt(123);
  assert.match(prompt, /open_issue_session/);
  assert.match(prompt, /coordinate_with_creator: false/);
  assert.match(prompt, /kickoff: \{ mode: "interactive"/);
  assert.match(prompt, /investigate-issue/);
  assert.doesNotMatch(prompt, /model:|base_branch:|issue_title:/);
});
test("canonical validation rejects stale public issues before dispatch", async () => {
  await assert.rejects(validateLiveQueueIssue(item, { readIssue: async () => ({ ...live, state: "closed" }) }), { code: "stale_issue" });
});
test("foreground dispatch sends once and reports an ambiguous receipt honestly", async () => {
  let calls = 0;
  const handoff = createForegroundHandoff({ send: async () => { calls++; return ""; }, readIssue: async () => live });
  assert.deepEqual(await handoff.dispatch(item), { status: "unknown", queued: null, messageId: null });
  assert.equal(calls, 1);
});
test("dispatch rechecks cancellation after canonical validation", async () => {
  let send = false;
  const handoff = createForegroundHandoff({ send: async () => { send = true; }, readIssue: async () => live });
  assert.equal((await handoff.dispatch(item, () => false)).status, "cancelled");
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
  const first = handoff.dispatch(item);
  await entered.promise;
  await assert.rejects(handoff.dispatch(item), { code: "handoff_in_progress" });
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
    await assert.rejects(handoff.dispatch(item), undefined, JSON.stringify(change));
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
  assert.equal((await handoff.dispatch(item, () => current)).status, "cancelled");
  assert.equal(sends, 0);
});

test("cosmetic log failures do not suppress a successful dispatch", async () => {
  const handoff = createForegroundHandoff({
    readIssue: async () => live,
    log: async () => { throw new Error("timeline unavailable"); },
    send: async () => "receipt",
    isBusy: () => true,
  });
  assert.deepEqual(await handoff.dispatch(item), { status: "sent", queued: true, messageId: "receipt" });
});
