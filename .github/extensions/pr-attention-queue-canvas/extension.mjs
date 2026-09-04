import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";

import { summarizeQueue } from "./queue.mjs";
import {
  getInstanceState,
  refreshInstance,
  startInstance,
  stopInstance,
} from "./server.mjs";

const session = await joinSession({
  canvases: [
    createCanvas({
      id: "pr-attention-queue",
      displayName: "PR Attention Queue",
      description:
        "Deterministic ASP.NET Core pull-request queue with explicit next actors and evidence-based reason codes.",
      inputSchema: {
        type: "object",
        properties: {
          source: {
            type: "string",
            enum: ["fixture", "live"],
            description: "Use the offline fixture or query GitHub live.",
          },
          preset: {
            type: "string",
            description: "Named pr-attention-queue preset. Defaults to blazor.",
          },
        },
        additionalProperties: false,
      },
      actions: [
        {
          name: "refresh",
          description: "Re-run the deterministic queue script and refresh the open canvas.",
          inputSchema: {
            type: "object",
            properties: {
              source: { type: "string", enum: ["fixture", "live"] },
              preset: { type: "string" },
            },
            additionalProperties: false,
          },
          handler: async (ctx) => {
            try {
              const state = await refreshInstance(ctx.instanceId, ctx.input ?? {});
              return summarizeQueue(state.queue);
            } catch (error) {
              throw new CanvasError(error.code ?? "queue_refresh_failed", error.message);
            }
          },
        },
        {
          name: "summary",
          description: "Return counts and visible queue items without scraping the canvas UI.",
          handler: (ctx) => {
            const state = getInstanceState(ctx.instanceId);
            if (!state) {
              throw new CanvasError("queue_not_open", "Open the PR Attention Queue canvas first.");
            }

            return summarizeQueue(state.queue);
          },
        },
      ],
      open: async (ctx) => {
        try {
          const entry = await startInstance(
            ctx.instanceId,
            ctx.input ?? {},
            (message) => session.log(message, { level: "debug" }),
          );
          return {
            title: "PR Attention Queue",
            status: entry.state.options.source === "live" ? "Live GitHub data" : "Offline fixture",
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
