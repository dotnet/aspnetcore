import assert from "node:assert/strict";
import test from "node:test";

import {
  abortIsolatedSession,
  cleanupIsolatedResources,
  withDeadline,
} from "./isolation.mjs";

test("cleanup force-stops when graceful shutdown does not settle", async () => {
  const calls = [];
  const isolatedSession = {
    disconnect: async () => calls.push("disconnect"),
  };
  const client = {
    stop: () => new Promise(() => {}),
    forceStop: async () => calls.push("forceStop"),
  };

  await cleanupIsolatedResources(isolatedSession, client, 5);
  assert.deepEqual(calls, ["disconnect", "forceStop"]);
});

test("abort is bounded when the isolated session does not settle", async () => {
  await abortIsolatedSession({ abort: () => new Promise(() => {}) }, 5);
});

test("deadline reports cleanup timeouts", async () => {
  await assert.rejects(
    withDeadline(() => new Promise(() => {}), 5, "cleanup"),
    (error) => error.code === "investigation_cleanup_timeout",
  );
});
