import { createServer } from "node:http";

import { HTML } from "./render.mjs";
import { loadQueue, normalizeOptions } from "./queue.mjs";

const instances = new Map();

export async function startInstance(instanceId, input, log) {
  let entry = instances.get(instanceId);
  if (entry) {
    return entry;
  }

  const loaded = await loadQueue(input);
  const state = {
    options: loaded.options,
    queue: loaded.queue,
  };

  const server = createServer((request, response) => {
    handleRequest(instanceId, request, response, log);
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });

  const address = server.address();
  const port = typeof address === "object" && address ? address.port : 0;
  entry = {
    server,
    state,
    url: `http://127.0.0.1:${port}/`,
    refreshPromise: null,
    sseClients: new Set(),
  };
  instances.set(instanceId, entry);
  return entry;
}

export function getInstanceState(instanceId) {
  return instances.get(instanceId)?.state ?? null;
}

export async function refreshInstance(instanceId, input = {}) {
  const entry = instances.get(instanceId);
  if (!entry) {
    const error = new Error("Open the PR Attention Queue canvas before refreshing it.");
    error.code = "queue_not_open";
    throw error;
  }

  if (entry.refreshPromise) {
    return entry.refreshPromise;
  }

  const options = normalizeOptions(input, entry.state.options);
  entry.refreshPromise = loadQueue(options)
    .then((loaded) => {
      entry.state = {
        options: loaded.options,
        queue: loaded.queue,
      };
      broadcastState(entry);
      return entry.state;
    })
    .finally(() => {
      entry.refreshPromise = null;
    });

  return entry.refreshPromise;
}

export async function stopInstance(instanceId) {
  const entry = instances.get(instanceId);
  if (!entry) {
    return;
  }

  instances.delete(instanceId);
  for (const client of entry.sseClients) {
    client.end();
  }
  entry.sseClients.clear();
  await new Promise((resolve) => entry.server.close(resolve));
}

async function handleRequest(instanceId, request, response, log) {
  const url = new URL(request.url ?? "/", "http://127.0.0.1");

  try {
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
      const input = await readJsonBody(request);
      const state = await refreshInstance(instanceId, input);
      return send(response, 200, state);
    }

    return send(response, 404, { error: "not found" });
  } catch (error) {
    try {
      const logged = log?.(`PR Attention Queue request failed: ${error.message}`);
      logged?.catch?.(() => {});
    } catch {
      // The session logger is diagnostic only; the HTTP error remains authoritative.
    }
    return send(response, 500, {
      code: error.code ?? "queue_request_failed",
      error: error.message,
    });
  }
}

function readJsonBody(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.setEncoding("utf8");
    request.on("data", (chunk) => {
      body += chunk;
      if (body.length > 16_384) {
        reject(new Error("request body is too large"));
        request.destroy();
      }
    });
    request.on("end", () => {
      if (!body) {
        resolve({});
        return;
      }

      try {
        resolve(JSON.parse(body));
      } catch {
        reject(new Error("request body must be valid JSON"));
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

function broadcastState(entry) {
  const data = `event: state\ndata: ${JSON.stringify(entry.state)}\n\n`;
  for (const client of entry.sseClients) {
    client.write(data);
  }
}
