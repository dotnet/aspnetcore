import assert from "node:assert/strict";
import { mkdtemp } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";
import { createAreaStore } from "./storage.mjs";

test("area preference is the only durable canvas state", async () => {
  const root = await mkdtemp(join(tmpdir(), "triage-store-"));
  const store = createAreaStore(root, `${root}-repository`);
  assert.equal(await store.readArea(), null);
  await store.writeArea("area-blazor");
  assert.equal(await store.readArea(), "area-blazor");
});
test("artifact storage refuses the repository checkout", () => {
  assert.throws(() => createAreaStore("/repo", "/repo"), /outside the repository/);
});
