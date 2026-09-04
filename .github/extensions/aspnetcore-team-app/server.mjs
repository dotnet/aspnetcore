import { createServer } from "node:http";

import { buildAgentActionLog, buildAgentActionPrompt } from "./agent.mjs";
import { loadQueue } from "./queue.mjs";
import { HTML } from "./render.mjs";
import { createQueueController } from "./state.mjs";

const instances = new Map();
let agentSend = null;
let browserOpen = null;

export function setAgentSend(handler) {
  agentSend = typeof handler === "function" ? handler : null;
}

export function setBrowserOpen(handler) {
  browserOpen = typeof handler === "function" ? handler : null;
}

export async function startInstance(instanceId, input, log) {
  let entry = instances.get(instanceId);
  if (entry) {
    return entry;
  }

  const controller = createQueueController({
    initialOptions: { source: "live", preset: input.preset ?? "blazor" },
    load: loadQueue,
  });

  const server = createServer((request, response) => {
    void handleRequest(instanceId, request, response, log);
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });

  const address = server.address();
  const port = typeof address === "object" && address ? address.port : 0;
  entry = {
    controller,
    server,
    url: `http://127.0.0.1:${port}/`,
    sseClients: new Set(),
    unsubscribe: null,
  };
  entry.unsubscribe = controller.subscribe((state) => broadcastState(entry, state));
  instances.set(instanceId, entry);
  void controller.initialize().catch((error) => {
    try {
      const logged = log?.(`ASP.NET Core Team App initial load failed: ${error.message}`);
      logged?.catch?.(() => {});
    } catch {
      // The retained error state is authoritative; logging is diagnostic only.
    }
  });
  return entry;
}

export function getInstanceState(instanceId) {
  return instances.get(instanceId)?.controller.getState() ?? null;
}

export function refreshInstance(instanceId, input = {}) {
  const entry = instances.get(instanceId);
  if (!entry) {
    const error = new Error("Open the ASP.NET Core Team App before refreshing it.");
    error.code = "queue_not_open";
    throw error;
  }

  return entry.controller.refresh({
    source: "live",
    preset: input.preset,
  });
}

export async function stopInstance(instanceId) {
  const entry = instances.get(instanceId);
  if (!entry) {
    return;
  }

  instances.delete(instanceId);
  entry.unsubscribe?.();
  for (const client of entry.sseClients) {
    client.end();
  }
  entry.sseClients.clear();
  await new Promise((resolve) => entry.server.close(resolve));
}

export async function dispatchResolvedAction({ kind, item }, handlers = {}) {
  const send = handlers.agentSend ?? agentSend;
  const open = handlers.browserOpen ?? browserOpen;

  if (kind === "open") {
    if (!open) {
      throw actionError("browser_unavailable", "The in-app browser is not available.");
    }
    const opened = await open(item);
    return {
      ok: true,
      kind,
      instanceId: opened?.instanceId ?? null,
    };
  }

  if (!send) {
    throw actionError("agent_unavailable", "The Copilot session is not ready.");
  }
  const prompt = buildAgentActionPrompt(kind, item);
  const log = buildAgentActionLog(kind, item);
  const result = await send({ prompt, log });
  return {
    ok: true,
    kind,
    messageId: typeof result === "string" ? result : result?.messageId ?? null,
  };
}

async function handleRequest(instanceId, request, response, log) {
  const url = new URL(request.url ?? "/", "http://127.0.0.1");

  try {
    if (request.method === "POST" && !isAllowedPostRequest(request)) {
      return send(response, 403, {
        code: "request_forbidden",
        error: "Cross-origin requests are not allowed.",
      });
    }

    if (request.method === "GET" && (url.pathname === "/" || url.pathname === "/index.html")) {
      return send(response, 200, HTML, "text/html; charset=utf-8");
    }

    if (request.method === "GET" && url.pathname === "/api/state") {
      const state = getInstanceState(instanceId);
      return state
        ? send(response, 200, state)
        : send(response, 404, { error: "queue instance not found" });
    }

    if (request.method === "GET" && url.pathname === "/events") {
      const entry = instances.get(instanceId);
      if (!entry) {
        return send(response, 404, { error: "queue instance not found" });
      }

      response.writeHead(200, {
        "Cache-Control": "no-cache",
        Connection: "keep-alive",
        "Content-Type": "text/event-stream",
      });
      response.write(": connected\n\n");
      entry.sseClients.add(response);
      request.on("close", () => entry.sseClients.delete(response));
      return;
    }

    if (request.method === "POST" && url.pathname === "/api/refresh") {
      const entry = instances.get(instanceId);
      if (!entry) {
        return send(response, 404, { error: "queue instance not found" });
      }

      try {
        const input = parseRefreshRequest(await readJsonBody(request));
        return send(response, 200, await entry.controller.refresh(input));
      } catch (error) {
        return send(response, error?.code === "invalid_refresh" ? 400 : 500, {
          code: error.code ?? "queue_refresh_failed",
          error: error.message,
          state: entry.controller.getState(),
        });
      }
    }

    if (request.method === "POST" && url.pathname === "/api/action") {
      const entry = instances.get(instanceId);
      if (!entry) {
        return send(response, 404, { error: "queue instance not found" });
      }

      const resolved = entry.controller.resolveAction(await readJsonBody(request));
      return send(response, 200, await dispatchResolvedAction(resolved));
    }

    return send(response, 404, { error: "not found" });
  } catch (error) {
    try {
      const logged = log?.(`ASP.NET Core Team App request failed: ${error.message}`);
      logged?.catch?.(() => {});
    } catch {
      // The session logger is diagnostic only; the HTTP error remains authoritative.
    }
    return send(response, 400, {
      code: error.code ?? "queue_request_failed",
      error: error.message,
    });
  }
}

export function parseRefreshRequest(body) {
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    throw actionError("invalid_refresh", "Refresh request must be an object.");
  }
  const keys = Object.keys(body);
  if (keys.some((key) => key !== "preset")) {
    throw actionError("invalid_refresh", "Refresh request accepts only preset.");
  }
  if (body.preset !== undefined
      && (typeof body.preset !== "string"
        || !/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(body.preset))) {
    throw actionError("invalid_refresh", "preset is invalid.");
  }

  return {
    source: "live",
    preset: body.preset,
  };
}

export function isAllowedPostRequest(request) {
  const host = request.headers.host;
  if (!host) {
    return false;
  }
  try {
    if (new URL(`http://${host}`).hostname !== "127.0.0.1") {
      return false;
    }
  } catch {
    return false;
  }

  const expectedOrigin = `http://${host}`;
  const origin = request.headers.origin;
  if (origin && !isSameOrigin(origin, expectedOrigin)) {
    return false;
  }

  const fetchSite = request.headers["sec-fetch-site"];
  return !fetchSite || fetchSite === "same-origin" || fetchSite === "none";
}

function isSameOrigin(origin, expectedOrigin) {
  try {
    return new URL(origin).origin === new URL(expectedOrigin).origin;
  } catch {
    return false;
  }
}

function readJsonBody(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.setEncoding("utf8");
    request.on("data", (chunk) => {
      body += chunk;
      if (body.length > 4_096) {
        reject(actionError("invalid_action", "Action request body is too large."));
        request.destroy();
      }
    });
    request.on("end", () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch {
        reject(actionError("invalid_action", "Action request body must be valid JSON."));
      }
    });
    request.on("error", reject);
  });
}

function send(response, status, body, contentType = "application/json; charset=utf-8") {
  response.writeHead(status, {
    "Cache-Control": "no-store",
    "Content-Type": contentType,
    "X-Content-Type-Options": "nosniff",
  });
  response.end(typeof body === "string" ? body : JSON.stringify(body));
}

function broadcastState(entry, state) {
  const data = `event: state\ndata: ${JSON.stringify(state)}\n\n`;
  for (const client of entry.sseClients) {
    client.write(data);
  }
}

function actionError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
