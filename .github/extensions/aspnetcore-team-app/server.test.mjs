import assert from "node:assert/strict";
import test from "node:test";

import {
  isAllowedPostRequest,
  parseRefreshRequest,
} from "./server.mjs";
import { parseActionRequest } from "./state.mjs";

test("action requests accept only opaque IDs and declared kinds", () => {
  assert.deepEqual(
    parseActionRequest({ itemId: "opaque-item-id-123", kind: "review" }),
    { itemId: "opaque-item-id-123", kind: "review" },
  );
  assert.throws(
    () => parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "review",
      number: 69040,
    }),
    (error) => error.code === "invalid_action",
  );
});

test("refresh requests accept only an optional preset", () => {
  assert.deepEqual(parseRefreshRequest({ preset: "blazor" }), {
    source: "live",
    preset: "blazor",
  });
  assert.deepEqual(parseRefreshRequest({}), {
    source: "live",
    preset: undefined,
  });
  assert.throws(
    () => parseRefreshRequest({ source: "fixture" }),
    (error) => error.code === "invalid_refresh",
  );
});

test("POST protection permits same-origin iframe requests and rejects cross-origin requests", () => {
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "http://127.0.0.1:43123",
      "sec-fetch-site": "same-origin",
    },
  }), true);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "https://attacker.example",
      "sec-fetch-site": "cross-site",
    },
  }), false);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "attacker.example:43123",
      origin: "http://attacker.example:43123",
      "sec-fetch-site": "same-origin",
    },
  }), false);
  assert.equal(isAllowedPostRequest({ headers: {} }), false);
});
