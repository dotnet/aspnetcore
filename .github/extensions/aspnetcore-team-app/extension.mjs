import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";

import {
  getInstanceState,
  refreshInstance,
  setAgentSend,
  setBrowserOpen,
  startInstance,
  stopInstance,
} from "./server.mjs";
import { summarizeState } from "./state.mjs";

const session = await joinSession({
  canvases: [
    createCanvas({
      id: "aspnetcore-team-app",
      displayName: "ASP.NET Core Team App",
      description:
        "Read-only PR attention pilot with deterministic next actors and evidence-based reasons.",
      inputSchema: {
        type: "object",
        properties: {
          preset: {
            type: "string",
            description: "Named pr-attention-queue preset. Defaults to blazor.",
            enum: ["blazor", "all-repo"],
          },
        },
        additionalProperties: false,
      },
      actions: [
        {
          name: "refresh",
          description: "Refresh the live PR Attention snapshot while preserving the last complete view.",
          inputSchema: {
            type: "object",
            properties: {
              preset: {
                type: "string",
                enum: ["blazor", "all-repo"],
              },
            },
            additionalProperties: false,
          },
          handler: async (ctx) => {
            try {
              return summarizeState(await refreshInstance(ctx.instanceId, ctx.input ?? {}));
            } catch (error) {
              throw new CanvasError(error.code ?? "queue_refresh_failed", error.message);
            }
          },
        },
        {
          name: "summary",
          description: "Return counts and primary PR Attention items without scraping the canvas UI.",
          handler: (ctx) => {
            const state = getInstanceState(ctx.instanceId);
            if (!state) {
              throw new CanvasError("queue_not_open", "Open the ASP.NET Core Team App first.");
            }
            return summarizeState(state);
          },
        },
      ],
      open: async (ctx) => {
        try {
          const entry = await startInstance(
            ctx.instanceId,
            ctx.input ?? {},
            (message) => session.log(message, { level: "warning" }),
          );
          return {
            title: "ASP.NET Core Team App",
            status: "PR Attention - live GitHub data",
            url: entry.url,
          };
        } catch (error) {
          throw new CanvasError(error.code ?? "queue_open_failed", error.message);
        }
      },
      onClose: async (ctx) => {
        await stopInstance(ctx.instanceId);
      },
    }),
  ],
});

let agentBusy = false;
let pendingAgentSends = 0;
session.on("assistant.turn_start", () => {
  agentBusy = true;
});
session.on("session.idle", () => {
  agentBusy = false;
});

setAgentSend(async ({ prompt, log }) => {
  const queued = agentBusy || pendingAgentSends > 0;
  pendingAgentSends += 1;
  if (log) {
    try {
      await session.log(
        `ASP.NET Core Team App - ${log} (${queued ? "queued; starts after the current task" : "starting now"})`,
      );
    } catch {
      // The prompt is the requested action; the timeline breadcrumb is best-effort.
    }
  }
  try {
    return {
      messageId: await session.send({ prompt }),
    };
  } finally {
    pendingAgentSends -= 1;
  }
});

setBrowserOpen(async (item) => {
  const instanceId = `aspnetcore-team-app-pr-${item.repository}-${item.number}`
    .replace(/[^A-Za-z0-9._-]/g, "-")
    .slice(0, 128);
  return session.rpc.canvas.open({
    canvasId: "browser",
    instanceId,
    input: {
      url: item.url,
      title: `${item.repository}#${item.number}`,
      placement: { surface: "panel", focus: true },
    },
  });
});
