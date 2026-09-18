import { randomBytes } from "node:crypto";
import { createServer } from "node:http";

import { HTML } from "./render.mjs";
import { createTriageController } from "./state.mjs";
import { DEFAULT_AREA, normalizeArea } from "./taxonomy.mjs";

const instances = new Map();
const lifecycleTails = new Map();
let queueLoader;
let areaStore;
let handoff;

export function configureServer({ load, store, launch }) {
  queueLoader = load;
  areaStore = store;
  handoff = launch;
}

export function startInstance(instanceId, input, log) {
  return enqueueLifecycle(instanceId, async () => {
    if (instances.has(instanceId)) {
      return instances.get(instanceId);
    }
    if (!queueLoader || !areaStore || !handoff) {
      throw serverError("server_unconfigured", "Issue triage server is not configured.");
    }
    const storedArea = await areaStore.readArea();
    const controller = createTriageController({
      initialArea: storedArea ?? normalizeArea(input?.area, DEFAULT_AREA),
      load: queueLoader,
      areaStore,
    });
    const token = randomBytes(32).toString("base64url");
    const entry = { controller, token, clients: new Set(), log, server: null, url: null };
    const server = createServer((request, response) => {
      void handleRequest(entry, request, response);
    });
    entry.server = server;
    try {
      await new Promise((resolve, reject) => {
        const onError = (error) => reject(error);
        server.once("error", onError);
        server.listen(0, "127.0.0.1", () => {
          server.off("error", onError);
          resolve();
        });
      });
      entry.url = `http://127.0.0.1:${server.address().port}/?token=${token}`;
      entry.unsubscribe = controller.subscribe((state) => {
        for (const client of entry.clients) {
          client.write(`data: ${JSON.stringify(state)}\n\n`);
        }
      });
      instances.set(instanceId, entry);
      entry.initialization = controller.initialize().catch((error) => {
        if (error.code !== "controller_disposed" && error.code !== "artifact_write_cancelled") {
          reportError(log, `Initial queue retrieval failed: ${error.message}`);
        }
      });
      return entry;
    } catch (error) {
      controller.dispose();
      server.close();
      throw error;
    }
  });
}

export function stopInstance(instanceId) {
  return enqueueLifecycle(instanceId, async () => {
    const entry = instances.get(instanceId);
    if (!entry) {
      return;
    }
    instances.delete(instanceId);
    entry.controller.dispose();
    entry.unsubscribe();
    for (const client of entry.clients) {
      client.end();
    }
    await new Promise((resolve) => {
      entry.server.close(resolve);
      entry.server.closeAllConnections();
    });
  });
}

export function getInstanceState(instanceId) {
  return requireInstance(instanceId).controller.getState();
}

export function getInstancePage(instanceId, input) {
  return requireInstance(instanceId).controller.getPage(input);
}

export function refreshInstance(instanceId, input) {
  return requireInstance(instanceId).controller.refresh(parseRefreshRequest(input));
}

export function selectInstanceIssue(instanceId, input) {
  return requireInstance(instanceId).controller.select(parseItemRequest(input));
}

export function investigateInstanceIssue(instanceId, input) {
  return requireInstance(instanceId).controller.investigate(parseItemRequest(input, true), handoff.dispatch);
}

async function handleRequest(entry, request, response) {
  try {
    const url = new URL(request.url ?? "/", "http://127.0.0.1");
    if (!hasInstanceToken(url, entry.token)) {
      throw serverError("request_forbidden", "A valid canvas capability token is required.");
    }
    if (request.method === "POST" && !isAllowedPostRequest(request)) {
      throw serverError("request_forbidden", "Cross-origin requests are not allowed.");
    }
    if (request.method === "GET" && ["/", "/index.html"].includes(url.pathname)) {
      return send(response, 200, HTML, "text/html; charset=utf-8");
    }
    if (request.method === "GET" && url.pathname === "/api/state") {
      return send(response, 200, entry.controller.getState());
    }
    if (request.method === "GET" && url.pathname === "/api/issues") {
      return send(response, 200, entry.controller.getPage(parsePageRequest(url.searchParams)));
    }
    if (request.method === "GET" && url.pathname === "/events") {
      response.writeHead(200, {
        "Cache-Control": "no-store",
        "Content-Type": "text/event-stream",
        "Referrer-Policy": "no-referrer",
      });
      entry.clients.add(response);
      response.on("close", () => entry.clients.delete(response));
      // Subscribe before reading state so a late subscriber cannot miss initialization.
      response.write(`data: ${JSON.stringify(entry.controller.getState())}\n\n`);
      return;
    }
    if (request.method === "POST" && url.pathname === "/api/refresh") {
      const state = await entry.controller.refresh(parseRefreshRequest(await readJsonBody(request)));
      return send(response, 200, state);
    }
    if (request.method === "POST" && url.pathname === "/api/select") {
      const state = entry.controller.select(parseItemRequest(await readJsonBody(request)));
      return send(response, 200, state);
    }
    if (request.method === "POST" && url.pathname === "/api/investigate") {
      const state = await entry.controller.investigate(parseItemRequest(await readJsonBody(request), true), handoff.dispatch);
      return send(response, 202, state);
    }
    return send(response, 404, { code: "not_found", error: "Not found." });
  } catch (error) {
    reportError(entry.log, `Issue triage request failed: ${error.message}`);
    const status = error.code === "request_forbidden" ? 403
      : error.code === "request_too_large" ? 413
        : error.code === "github_rate_limited" ? 429
          : error.code === "queue_not_open" ? 404 : 400;
    return send(response, status, {
      code: error.code ?? "queue_request_failed",
      error: error.message,
      state: entry.controller.getState(),
    });
  }
}

export function parseRefreshRequest(body) {
  requireKeys(body, ["area"], "invalid_refresh");
  if (typeof body.area !== "string") {
    throw serverError("invalid_refresh", "area must be a supported area label.");
  }
  return { area: normalizeArea(body.area) };
}

export function parseItemRequest(body, withDestination = false) {
  requireKeys(body, withDestination ? ["destination", "itemId"] : ["itemId"], "invalid_selection");
  if (typeof body.itemId !== "string" || !/^[A-Za-z0-9-]{16,64}$/.test(body.itemId)) {
    throw serverError("invalid_selection", "itemId is invalid.");
  }
  if (withDestination && !["current", "new-child"].includes(body.destination)) {
    throw serverError("invalid_destination", "Investigation destination is invalid.");
  }
  return withDestination ? { itemId: body.itemId, destination: body.destination } : { itemId: body.itemId };
}

export function parsePageRequest(params) {
  for (const key of params.keys()) {
    if (!["offset", "limit", "token"].includes(key) || params.getAll(key).length !== 1) {
      throw serverError("invalid_page", "Page request contains an unexpected or repeated parameter.");
    }
  }
  const offset = parseInteger(params.get("offset"), 0);
  const limit = parseInteger(params.get("limit"), 50);
  if (![25, 50, 100].includes(limit)) {
    throw serverError("invalid_page", "Page limit must be 25, 50, or 100.");
  }
  return { offset, limit };
}

function parseInteger(value, fallback) {
  if (value === null) {
    return fallback;
  }
  if (!/^(0|[1-9][0-9]*)$/.test(value) || !Number.isSafeInteger(Number(value))) {
    throw serverError("invalid_page", "Page parameters must be non-negative integers.");
  }
  return Number(value);
}

export function hasInstanceToken(url, expectedToken) {
  return typeof expectedToken === "string" && expectedToken.length >= 32
    && url.searchParams.getAll("token").length === 1
    && url.searchParams.get("token") === expectedToken;
}

export function isAllowedPostRequest(request) {
  const host = request.headers.host;
  if (!host || !/^127\.0\.0\.1:\d+$/.test(host)) {
    return false;
  }
  const origin = request.headers.origin;
  if (origin && origin !== `http://${host}`) {
    return false;
  }
  const fetchSite = request.headers["sec-fetch-site"];
  return !fetchSite || fetchSite === "same-origin" || fetchSite === "none";
}

export async function readJsonBody(request, maxBytes = 4_096) {
  request.setEncoding("utf8");
  let body = "";
  for await (const chunk of request) {
    body += chunk;
    if (Buffer.byteLength(body, "utf8") > maxBytes) {
      throw serverError("request_too_large", "Request body is too large.");
    }
  }
  try {
    return JSON.parse(body);
  } catch {
    throw serverError("invalid_json", "Request body must be valid JSON.");
  }
}

function requireKeys(value, keys, code) {
  if (!value || typeof value !== "object" || Array.isArray(value)
    || Object.keys(value).length !== keys.length || keys.some((key) => !Object.hasOwn(value, key))) {
    throw serverError(code, "Request has an invalid shape.");
  }
}

function requireInstance(instanceId) {
  const entry = instances.get(instanceId);
  if (!entry) {
    throw serverError("queue_not_open", "Open the issue triage canvas first.");
  }
  return entry;
}

function send(response, status, body, contentType = "application/json; charset=utf-8") {
  if (response.destroyed || response.writableEnded) {
    return;
  }
  response.writeHead(status, {
    "Cache-Control": "no-store",
    "Content-Security-Policy": "default-src 'none'; connect-src 'self'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'self'",
    "Content-Type": contentType,
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  });
  response.end(typeof body === "string" ? body : JSON.stringify(body));
}

function reportError(log, message) {
  void Promise.resolve().then(() => log?.(message)).catch((error) => {
    process.stderr.write(`Issue triage logging failed: ${error.message}\n`);
  });
}

function serverError(code, message) {
  return Object.assign(new Error(message), { code });
}

function enqueueLifecycle(instanceId, operation) {
  const previous = lifecycleTails.get(instanceId) ?? Promise.resolve();
  const result = previous.then(operation);
  const tail = result.then(() => undefined, () => undefined);
  lifecycleTails.set(instanceId, tail);
  void tail.then(() => {
    if (lifecycleTails.get(instanceId) === tail) {
      lifecycleTails.delete(instanceId);
    }
  });
  return result;
}
