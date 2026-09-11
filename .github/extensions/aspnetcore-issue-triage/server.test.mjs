import assert from "node:assert/strict";
import test from "node:test";
import { hasInstanceToken, isAllowedPostRequest, parseItemRequest, parsePageRequest } from "./server.mjs";
test("page requests retain authentication but reject unknown data", () => {
  assert.deepEqual(parsePageRequest(new URLSearchParams("offset=0&limit=25&token=capability")), { offset: 0, limit: 25 });
  assert.throws(() => parsePageRequest(new URLSearchParams("offset=0&limit=25&url=x")), { code: "invalid_page" });
});
test("selection accepts only opaque item ids", () => {
  assert.throws(() => parseItemRequest({ itemId: "../123" }), { code: "invalid_selection" });
});
test("token and same-origin protections reject forged requests", () => {
  assert.equal(hasInstanceToken(new URL("http://127.0.0.1/?token=x".replace("x", "a".repeat(32))), "a".repeat(32)), true);
  assert.equal(isAllowedPostRequest({ headers: { host: "127.0.0.1:1", origin: "https://attacker.example" } }), false);
});
