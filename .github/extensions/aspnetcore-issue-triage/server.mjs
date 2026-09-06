import { randomBytes } from "node:crypto";
import { createServer } from "node:http";

import { HTML } from "./render.mjs";
import { createTriageController, summarizeState } from "./state.mjs";
import { DEFAULT_AREA, normalizeArea } from "./taxonomy.mjs";

const instances = new Map();
const lifecycleTails = new Map();
const INITIALIZATION_STOP_TIMEOUT_MS = 2_000;
let queueLoader = null;
let reportStore = null;
let investigationScheduler = null;

export function configureServer({ load, store, scheduleInvestigation }) {
  queueLoader = load;
  reportStore = store;
  investigationScheduler = scheduleInvestigation;
}

export function startInstance(instanceId, input, log) {
  return enqueueLifecycle(instanceId, async () => {
    const entry = instances.get(instanceId);
    if (entry) {
      return entry;
    }
    if (!queueLoader || !reportStore || !investigationScheduler) {
      throw serverError("server_unconfigured", "Issue triage server is not configured.");
    }
    return createInstance(instanceId, input, log);
  });
}

async function createInstance(instanceId, input, log) {
  let server = null;
  let entry = null;
  try {
    const storedArea = await reportStore.readArea?.();
    const controller = createTriageController({
      initialArea: storedArea ?? normalizeArea(input?.area, DEFAULT_AREA),
      load: queueLoader,
      reportStore,
    });
    const token = randomBytes(32).toString("base64url");
    server = createServer((request, response) => {
      void handleRequest(instanceId, request, response, log);
    });
    await listenLoopback(server);
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    entry = {
      controller,
      server,
      token,
      url: `http://127.0.0.1:${port}/?token=${token}`,
      clients: new Set(),
      unsubscribe: null,
      initialization: null,
      log,
    };
    entry.unsubscribe = controller.subscribe((state) => broadcast(entry, state));
    instances.set(instanceId, entry);
    entry.initialization = Promise.resolve().then(() => controller.initialize()).catch((error) => {
      if (error.code === "controller_disposed") {
        return;
      }
      void log?.(`ASP.NET Core issue triage initial load failed: ${error.message}`);
    });
    return entry;
  } catch (error) {
    entry?.unsubscribe?.();
    if (instances.get(instanceId) === entry) {
      instances.delete(instanceId);
    }
    await closeServer(server);
    throw error;
  }
}

export function getInstanceState(instanceId) {
  return instances.get(instanceId)?.controller.getState() ?? null;
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

export function queueInstanceInvestigation(instanceId, input) {
  const entry = requireInstance(instanceId);
  const queued = entry.controller.queueInvestigation(parseItemRequest(input));
  investigationScheduler({ instanceId, job: queued.job });
  return queued.state;
}

export async function performInstanceInvestigation(instanceId, job, run) {
  const entry = instances.get(instanceId);
  if (!entry) {
    return null;
  }
  try {
    const item = entry.controller.beginInvestigation(job);
    const report = await run(item);
    return await entry.controller.completeInvestigation(job, report);
  } catch (error) {
    return entry.controller.failInvestigation(job, error);
  }
}

export function stopInstance(instanceId) {
  return enqueueLifecycle(instanceId, () => stopInstanceCore(instanceId));
}

async function stopInstanceCore(instanceId) {
  const entry = instances.get(instanceId);
  if (!entry) {
    return;
  }
  instances.delete(instanceId);
  entry.controller.dispose();
  entry.unsubscribe?.();
  for (const client of entry.clients) {
    client.end();
  }
  entry.clients.clear();
  const initializationSettled = await settleWithin(
    entry.initialization,
    INITIALIZATION_STOP_TIMEOUT_MS,
  );
  if (!initializationSettled) {
    void entry.log?.("ASP.NET Core issue triage initial load did not stop before the cleanup deadline.");
  }
  await closeServer(entry.server);
}

function settleWithin(promise, timeoutMs) {
  if (!promise) {
    return Promise.resolve(true);
  }
  return new Promise((resolve) => {
    const timer = setTimeout(() => resolve(false), timeoutMs);
    Promise.resolve(promise).then(
      () => {
        clearTimeout(timer);
        resolve(true);
      },
      () => {
        clearTimeout(timer);
        resolve(true);
      },
    );
  });
}

function enqueueLifecycle(instanceId, operation) {
  const previous = lifecycleTails.get(instanceId) ?? Promise.resolve();
  const result = previous.catch(() => {}).then(operation);
  const tail = result.then(
    () => undefined,
    () => undefined,
  );
  lifecycleTails.set(instanceId, tail);
  void tail.finally(() => {
    if (lifecycleTails.get(instanceId) === tail) {
      lifecycleTails.delete(instanceId);
    }
  });
  return result;
}

function listenLoopback(server) {
  return new Promise((resolve, reject) => {
    const onError = (error) => {
      server.off("listening", onListening);
      reject(error);
    };
    const onListening = () => {
      server.off("error", onError);
      resolve();
    };
    server.once("error", onError);
    server.once("listening", onListening);
    server.listen(0, "127.0.0.1");
  });
}

function closeServer(server) {
  if (!server?.listening) {
    return Promise.resolve();
  }
  return new Promise((resolve) => server.close(resolve));
}

async function handleRequest(instanceId, request, response, log) {
  const url = new URL(request.url ?? "/", "http://127.0.0.1");
  try {
    const entry = instances.get(instanceId);
    if (!entry || !hasInstanceToken(url, entry.token)) {
      return send(response, 403, {
        code: "request_forbidden",
        error: "A valid canvas capability token is required.",
      });
    }
    if (request.method === "POST" && !isAllowedPostRequest(request)) {
      return send(response, 403, {
        code: "request_forbidden",
        error: "Cross-origin requests are not allowed.",
      });
    }
    if (request.method === "GET" && ["/", "/index.html"].includes(url.pathname)) {
      return send(response, 200, HTML, "text/html; charset=utf-8");
    }
    if (request.method === "GET" && url.pathname === "/api/state") {
      const state = getInstanceState(instanceId);
      return state
        ? send(response, 200, state)
        : send(response, 404, { code: "queue_not_open", error: "Queue instance not found." });
    }
    if (request.method === "GET" && url.pathname === "/api/issues") {
      return send(response, 200, getInstancePage(instanceId, parsePageRequest(url.searchParams)));
    }
    if (request.method === "GET" && url.pathname === "/events") {
      const entry = instances.get(instanceId);
      if (!entry) {
        return send(response, 404, { code: "queue_not_open", error: "Queue instance not found." });
      }
      response.writeHead(200, {
        "Cache-Control": "no-cache",
        Connection: "keep-alive",
        "Content-Type": "text/event-stream",
        "X-Content-Type-Options": "nosniff",
      });
      response.write(": connected\n\n");
      entry.clients.add(response);
      request.on("close", () => entry.clients.delete(response));
      return;
    }
    if (request.method === "POST" && url.pathname === "/api/refresh") {
      return send(response, 200, await refreshInstance(instanceId, await readJsonBody(request)));
    }
    if (request.method === "POST" && url.pathname === "/api/select") {
      return send(response, 200, await selectInstanceIssue(instanceId, await readJsonBody(request)));
    }
    if (request.method === "POST" && url.pathname === "/api/investigate") {
      return send(response, 202, queueInstanceInvestigation(instanceId, await readJsonBody(request)));
    }
    return send(response, 404, { code: "not_found", error: "Not found." });
  } catch (error) {
    void log?.(`ASP.NET Core issue triage request failed: ${error.message}`);
    return send(response, errorStatus(error), {
      code: error.code ?? "queue_request_failed",
      error: error.message,
      state: getInstanceState(instanceId),
    });
  }
}

export function parseRefreshRequest(body) {
  requireObjectWithKeys(body, ["area"], "invalid_refresh");
  return { area: normalizeArea(body.area) };
}

export function parseItemRequest(body) {
  requireObjectWithKeys(body, ["itemId"], "invalid_selection");
  if (typeof body.itemId !== "string" || !/^[A-Za-z0-9-]{16,64}$/.test(body.itemId)) {
    throw serverError("invalid_selection", "itemId is invalid.");
  }
  return { itemId: body.itemId };
}

export function parsePageRequest(searchParams) {
  const allowed = new Set(["offset", "limit"]);
  for (const key of searchParams.keys()) {
    if (!allowed.has(key)) {
      throw serverError("invalid_page", "Page request accepts only offset and limit.");
    }
  }
  const offset = parseIntegerParameter(searchParams.get("offset"), 0, "offset");
  const limit = parseIntegerParameter(searchParams.get("limit"), 50, "limit");
  if (![25, 50, 100].includes(limit)) {
    throw serverError("invalid_page", "limit must be 25, 50, or 100.");
  }
  return { offset, limit };
}

export function isAllowedPostRequest(request) {
  const host = request.headers.host;
  if (!host) {
    return false;
  }
  let expectedOrigin;
  try {
    const parsed = new URL(`http://${host}`);
    if (parsed.hostname !== "127.0.0.1") {
      return false;
    }
    expectedOrigin = parsed.origin;
  } catch {
    return false;
  }

  const origin = request.headers.origin;
  if (origin) {
    try {
      if (new URL(origin).origin !== expectedOrigin) {
        return false;
      }
    } catch {
      return false;
    }
  }
  const fetchSite = request.headers["sec-fetch-site"];
  return !fetchSite || fetchSite === "same-origin" || fetchSite === "none";
}

export function hasInstanceToken(url, expectedToken) {
  const actualToken = url.searchParams.get("token");
  return typeof expectedToken === "string"
    && expectedToken.length >= 32
    && actualToken === expectedToken;
}

export function readJsonBody(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    let settled = false;
    request.setEncoding("utf8");
    request.on("data", (chunk) => {
      if (settled) {
        return;
      }
      body += chunk;
      if (body.length > 4_096) {
        settled = true;
        reject(serverError("request_too_large", "Request body is too large."));
        request.destroy();
      }
    });
    request.on("end", () => {
      if (settled) {
        return;
      }
      try {
        settled = true;
        resolve(body ? JSON.parse(body) : {});
      } catch {
        settled = true;
        reject(serverError("invalid_json", "Request body must be valid JSON."));
      }
    });
    request.on("error", (error) => {
      if (!settled) {
        settled = true;
        reject(error);
      }
    });
  });
}

function requireInstance(instanceId) {
  const entry = instances.get(instanceId);
  if (!entry) {
    throw serverError("queue_not_open", "Open the ASP.NET Core Issue Triage canvas first.");
  }
  return entry;
}

function requireObjectWithKeys(body, keys, code) {
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    throw serverError(code, "Request body must be an object.");
  }
  const actual = Object.keys(body).sort();
  const expected = [...keys].sort();
  if (actual.length !== expected.length || actual.some((key, index) => key !== expected[index])) {
    throw serverError(code, `Request accepts only ${expected.join(", ")}.`);
  }
}

function parseIntegerParameter(value, fallback, name) {
  if (value === null) {
    return fallback;
  }
  if (!/^(0|[1-9][0-9]*)$/.test(value)) {
    throw serverError("invalid_page", `${name} must be a non-negative integer.`);
  }
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed)) {
    throw serverError("invalid_page", `${name} is too large.`);
  }
  return parsed;
}

function send(response, status, body, contentType = "application/json; charset=utf-8") {
  response.writeHead(status, {
    "Cache-Control": "no-store",
    "Content-Security-Policy": "default-src 'none'; connect-src 'self'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'self'",
    "Content-Type": contentType,
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  });
  response.end(typeof body === "string" ? body : JSON.stringify(body));
}

function broadcast(entry, state) {
  const data = `event: state\ndata: ${JSON.stringify(state)}\n\n`;
  for (const client of entry.clients) {
    client.write(data);
  }
}

function errorStatus(error) {
  if (["queue_not_open"].includes(error.code)) {
    return 404;
  }
  if (["github_rate_limited"].includes(error.code)) {
    return 429;
  }
  if (
    typeof error.code === "string"
    && (
      error.code.startsWith("invalid_")
      || error.code.startsWith("stale_")
      || error.code === "request_too_large"
      || error.code === "investigation_in_progress"
    )
  ) {
    return 400;
  }
  return 500;
}

function serverError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
