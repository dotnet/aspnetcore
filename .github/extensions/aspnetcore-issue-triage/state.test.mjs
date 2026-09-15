import assert from "node:assert/strict";
import test from "node:test";
import { createTriageController } from "./state.mjs";

const queue = { schemaVersion: "1.0.0", repository: "dotnet/aspnetcore", area: "area-blazor", generatedAt: "2026-01-01", predicate: "test", coverage: { complete: true }, issues: [{ number: 123, title: "untrusted title", url: "https://github.com/dotnet/aspnetcore/issues/123", author: "a", createdAt: "2026-01-01", updatedAt: "2026-01-01", labels: ["area-blazor"], whyIncluded: ["predicate"] }] };
test("selection and handoff use server-owned queue items", async () => {
  const controller = createTriageController({ load: async () => queue, areaStore: { writeArea: async () => {} }, createId: () => "01234567-0123-4012-8012-0123456789ab" });
  await controller.initialize();
  const id = controller.getPage({ offset: 0, limit: 25 }).items[0].id;
  await controller.investigate({ itemId: id, destination: "current" }, async (item, destination) => {
    assert.equal(item.number, 123);
    assert.equal(destination, "current");
    return { status: "sent", queued: false, messageId: "message" };
  });
  assert.equal(controller.getState().snapshot.handoff.phase, "sent");
  assert.throws(() => controller.select({ itemId: "attacker-controlled" }), { code: "stale_selection" });
});
test("refresh invalidates the selected issue and handoff state", async () => {
  const controller = createTriageController({
    load: async ({ area }) => ({ ...queue, area, issues: queue.issues.map((issue) => ({ ...issue, labels: [area] })) }),
    areaStore: { writeArea: async () => {} },
  });
  await controller.initialize();
  controller.select({ itemId: controller.getPage({ offset: 0, limit: 25 }).items[0].id });
  await controller.refresh({ area: "area-commandlinetools" });
  assert.equal(controller.getState().snapshot?.selectedIssue, null);
});
