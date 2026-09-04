import assert from "node:assert/strict";
import test from "node:test";

import { loadQueue } from "./queue.mjs";
import { createQueueController } from "./state.mjs";

const fixture = await loadQueue({ source: "fixture", preset: "blazor" });

test("controller publishes an opaque, action-safe snapshot", async () => {
  let nextId = 0;
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => fixture,
    createId: () => `opaque-item-id-${++nextId}`,
    now: () => "2026-09-03T18:00:00.000Z",
  });

  await controller.initialize();
  const state = controller.getState();
  const items = [
    ...state.snapshot.primary.reviewNow,
    ...state.snapshot.primary.needsRescue,
    ...state.snapshot.readyToMerge,
    ...Object.values(state.snapshot.secondary).flat(),
  ];

  assert.equal(state.refresh.phase, "ready");
  assert.ok(items.length > 0);
  assert.equal(new Set(items.map((item) => item.id)).size, items.length);
  assert.ok(items.every((item) => !Object.hasOwn(item, "url")));

  const reviewItem = state.snapshot.primary.reviewNow[0];
  const action = controller.resolveAction({ itemId: reviewItem.id, kind: "review" });
  assert.equal(action.item.number, reviewItem.number);
  assert.match(action.item.url, /^https:\/\/github\.com\/dotnet\/aspnetcore\/pull\/\d+$/);
});

test("refresh coalesces callers and atomically replaces the snapshot", async () => {
  let calls = 0;
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 1) {
        return fixture;
      }
      await pending;
      return fixture;
    },
  });

  await controller.initialize();
  const previousId = controller.getState().snapshot.primary.reviewNow[0].id;
  const first = controller.refresh();
  const second = controller.refresh();

  await Promise.resolve();
  assert.equal(calls, 2);
  assert.equal(controller.getState().refresh.phase, "refreshing");
  assert.equal(controller.getState().refresh.stale, true);
  assert.equal(controller.getState().snapshot.primary.reviewNow[0].id, previousId);

  release();
  await Promise.all([first, second]);
  assert.equal(calls, 2);
  assert.notEqual(controller.getState().snapshot.primary.reviewNow[0].id, previousId);
});

test("refresh rejects a different scope instead of returning the in-flight scope", async () => {
  let calls = 0;
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 1) {
        return fixture;
      }
      await pending;
      return fixture;
    },
  });

  await controller.initialize();
  const refresh = controller.refresh({ preset: "blazor" });
  assert.throws(
    () => controller.refresh({ preset: "all-repo" }),
    (error) => error.code === "refresh_in_progress",
  );
  release();
  await refresh;
});

test("failed refresh retains the previous snapshot and invalidates stale action IDs after success", async () => {
  let calls = 0;
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 2) {
        throw new Error("simulated failure");
      }
      return fixture;
    },
  });

  await controller.initialize();
  const firstItem = controller.getState().snapshot.primary.reviewNow[0];
  await assert.rejects(() => controller.refresh(), /simulated failure/);
  assert.equal(controller.getState().refresh.phase, "error");
  assert.equal(controller.getState().refresh.stale, true);
  assert.equal(controller.getState().snapshot.primary.reviewNow[0].id, firstItem.id);

  await controller.refresh();
  assert.throws(
    () => controller.resolveAction({ itemId: firstItem.id, kind: "open" }),
    (error) => error.code === "stale_item",
  );
});

test("controller rejects actions that do not match the current bucket", async () => {
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => fixture,
  });
  await controller.initialize();

  const rescueItem = controller.getState().snapshot.primary.needsRescue[0];
  assert.throws(
    () => controller.resolveAction({ itemId: rescueItem.id, kind: "review" }),
    (error) => error.code === "action_not_allowed",
  );

  const reviewItem = controller.getState().snapshot.primary.reviewNow[0];
  assert.throws(
    () => controller.resolveAction({ itemId: reviewItem.id, kind: "investigate-rescue" }),
    (error) => error.code === "action_not_allowed",
  );
});
