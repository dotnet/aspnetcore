// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AppBridge, McpUiResourceMetaSchema, PostMessageTransport } from '@modelcontextprotocol/ext-apps/app-bridge';
import { CallToolResultSchema, ListResourcesResultSchema, ListToolsResultSchema, ReadResourceResultSchema, TextResourceContentsSchema, ToolSchema } from '@modelcontextprotocol/core';
import { z } from 'zod';

const OpenedAppSchema = z.object({
  id: z.string(),
  tool: ToolSchema,
  resource: TextResourceContentsSchema,
  result: CallToolResultSchema,
  arguments: z.record(z.string(), z.unknown()),
  sandboxUri: z.string(),
});

const status = document.getElementById('host-status')!;
const view = document.getElementById('view')!;
let bridge: AppBridge | undefined;
let connectionId: string | undefined;

function showError(error: unknown): void {
  console.error(error);
  status.textContent = error instanceof Error ? error.message : String(error);
  status.setAttribute('data-error', 'true');
}

async function waitFor<T>(promise: Promise<T>, stage: string): Promise<T> {
  let timer: ReturnType<typeof setTimeout>;
  try {
    return await Promise.race([
      promise,
      new Promise<never>((_, reject) => {
        timer = setTimeout(() => reject(new Error(`${stage} timed out.`)), 20000);
      }),
    ]);
  } finally {
    clearTimeout(timer!);
  }
}

async function broker(method: string, parameters: unknown, signal: AbortSignal): Promise<unknown> {
  const response = await fetch(`/consumer/${connectionId}/rpc`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ method, parameters }),
    signal,
  });
  if (!response.ok) {
    throw new Error(await response.text());
  }

  return await response.json();
}

async function open(): Promise<void> {
  if (bridge) {
    throw new Error('Close the existing application before opening another one.');
  }

  status.textContent = 'Discovering tools and reading the app resource...';
  const parameters = new URLSearchParams(location.search);
  const response = await fetch(`/consumer/open?${parameters}`, { method: 'POST' });
  if (!response.ok) {
    throw new Error(await response.text());
  }

  const opened = OpenedAppSchema.parse(await response.json());
  connectionId = opened.id;
  const frame = document.createElement('iframe');
  frame.id = 'sandbox-frame';
  frame.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-forms');
  frame.style.cssText = 'border:0;width:100%;height:1000px';
  view.append(frame);
  const sandboxReady = Promise.withResolvers<void>();
  const initialized = Promise.withResolvers<void>();
  bridge = new AppBridge(null, { name: 'C# standard Apps test consumer', version: '1.0.0' },
    { serverTools: {}, serverResources: {} },
    { hostContext: { toolInfo: { tool: opened.tool }, platform: 'web', theme: 'light', displayMode: 'inline' } });
  bridge.onsandboxready = () => sandboxReady.resolve();
  bridge.oninitialized = () => initialized.resolve();
  bridge.onerror = showError;
  bridge.oncalltool = async (parameters, extra) => CallToolResultSchema.parse(await broker('tools/call', parameters, extra.mcpReq.signal));
  bridge.setRequestHandler('tools/list', async (request, extra) =>
    ListToolsResultSchema.parse(await broker('tools/list', request.params, extra.mcpReq.signal)));
  bridge.onreadresource = async (parameters, extra) => {
    const result = await broker('resources/read', parameters, extra.mcpReq.signal);
    return ReadResourceResultSchema.parse(result);
  };
  bridge.onlistresources = async (parameters, extra) => ListResourcesResultSchema.parse(await broker('resources/list', parameters, extra.mcpReq.signal));
  await bridge.connect(new PostMessageTransport(frame.contentWindow!, frame.contentWindow!));
  frame.src = `${opened.sandboxUri}/sandbox/${opened.id}`;
  await waitFor(sandboxReady.promise, 'Sandbox readiness');
  const metadata = opened.resource._meta?.ui === undefined
    ? undefined
    : McpUiResourceMetaSchema.parse(opened.resource._meta.ui);
  await bridge.sendSandboxResourceReady({ html: opened.resource.text, csp: metadata?.csp, permissions: metadata?.permissions });
  await waitFor(initialized.promise, 'App initialization');
  await bridge.sendToolInput({ arguments: opened.arguments });
  await bridge.sendToolResult(opened.result);
  status.textContent = 'App ready';
}

async function close(): Promise<void> {
  if (bridge) {
    await bridge.teardownResource({}, { timeout: 10000 });
    await bridge.close();
    bridge = undefined;
  }

  if (connectionId) {
    const response = await fetch(`/consumer/${connectionId}`, { method: 'DELETE' });
    if (!response.ok) {
      throw new Error(await response.text());
    }

    connectionId = undefined;
  }

  view.replaceChildren();
  status.textContent = 'App closed';
}

document.getElementById('open')!.addEventListener('click', () => void open().catch(showError));
document.getElementById('close')!.addEventListener('click', () => void close().catch(showError));
