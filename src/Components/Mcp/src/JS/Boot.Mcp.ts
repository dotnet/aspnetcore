// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { App, applyHostStyleVariables } from '@modelcontextprotocol/ext-apps';
import { synchronizeAttributes } from '../../../Web.JS/src/Rendering/DomMerging/AttributeSync';
import { synchronizeDomContent } from '../../../Web.JS/src/Rendering/DomMerging/DomSync';

interface DocumentResult {
  viewId: string;
  location: string;
  baseUri: string;
  dispatchTool: string;
  document: string;
  redirected: boolean;
  replaceHistory: boolean;
  preserveDom: boolean;
  logLevel: number;
}

interface FormFile {
  name: string;
  fileName: string;
  contentType: string;
  lastModified: number;
  data: string;
}

const runtimeScript = getRuntimeScript();

function getRuntimeScript(): HTMLScriptElement {
  const script = document.currentScript;
  if (!(script instanceof HTMLScriptElement)) {
    throw new Error('The MCP runtime must be loaded by an authored document script.');
  }

  return script;
}

const app = new App({ name: 'Blazor MCP', version: '1.0.0' }, {}, { strict: true, autoResize: true });
let current: DocumentResult | undefined;
let generation = 0;
let closing = false;
let connected = false;
let initialResult: unknown;
const pendingOperations = new Set<Promise<void>>();
const lifetime = new AbortController();
let minimumLogLevel = readInitialLogLevel();

function readInitialLogLevel(): number {
  const configured = document.currentScript?.getAttribute('data-blazor-log-level');
  if (configured === undefined || configured === null) {
    return 2;
  }

  const level = Number(configured);
  if (!Number.isInteger(level) || level < 0 || level > 6) {
    throw new Error('The MCP browser log level is invalid.');
  }

  return level;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object';
}

function readDocument(result: unknown): DocumentResult {
  if (!isObject(result)) {
    throw new Error('The component tool returned an invalid result.');
  }

  if (result.isError) {
    const messages = Array.isArray(result.content)
      ? result.content.filter(isObject).filter(item => item.type === 'text').map(item => item.text)
      : [];
    throw new Error(messages.join('\n') || 'The component tool failed.');
  }

  const document = isObject(result._meta) ? result._meta.blazor : undefined;
  if (!isObject(document)
    || typeof document.viewId !== 'string'
    || typeof document.location !== 'string'
    || typeof document.baseUri !== 'string'
    || typeof document.dispatchTool !== 'string'
    || typeof document.document !== 'string'
    || typeof document.redirected !== 'boolean'
    || typeof document.replaceHistory !== 'boolean') {
    throw new Error('The component tool returned an invalid document.');
  }

  const browserOptions = document.browserOptions;
  if (!isObject(browserOptions)
    || typeof browserOptions.preserveDom !== 'boolean'
    || typeof browserOptions.logLevel !== 'number'
    || !Number.isInteger(browserOptions.logLevel)
    || browserOptions.logLevel < 0 || browserOptions.logLevel > 6) {
    throw new Error('The component tool returned invalid browser options.');
  }

  return {
    viewId: document.viewId,
    location: document.location,
    baseUri: document.baseUri,
    dispatchTool: document.dispatchTool,
    document: document.document,
    redirected: document.redirected,
    replaceHistory: document.replaceHistory,
    preserveDom: browserOptions.preserveDom,
    logLevel: browserOptions.logLevel,
  };
}

function showError(error: unknown): void {
  if (minimumLogLevel <= 4) {
    console.error(error instanceof Error ? error.message : String(error));
  }
  let element = document.getElementById('blazor-mcp-error');
  if (!element) {
    element = document.createElement('div');
    element.id = 'blazor-mcp-error';
    element.setAttribute('role', 'alert');
    document.body.append(element);
  }

  element.textContent = error instanceof Error ? error.message : String(error);
}

function updateBusyState(): void {
  document.documentElement.setAttribute('aria-busy', pendingOperations.size === 0 ? 'false' : 'true');
}

function trackOperation(operation: Promise<void>, requestGeneration = generation): void {
  const observed: Promise<void> = operation.catch(error => {
    if (!closing && requestGeneration === generation) {
      showError(error);
    } else if (minimumLogLevel <= 1) {
      console.debug('Discarding completion from a superseded or closed component operation.', error);
    }
  }).finally(() => {
    pendingOperations.delete(observed);
    updateBusyState();
  });
  pendingOperations.add(observed);
  updateBusyState();
}

function applyContext(context: ReturnType<App['getHostContext']>): void {
  if (context?.theme) {
    document.documentElement.style.colorScheme = context.theme;
  }

  if (context?.styles?.variables) {
    applyHostStyleVariables(context.styles.variables);
  }
}

function isInternal(url: URL, base: URL): boolean {
  return url.origin === base.origin && url.pathname.startsWith(base.pathname);
}

function applyDocument(result: unknown, replaceHistory = false, fromPost = false): void {
  const next = readDocument(result);
  minimumLogLevel = next.logLevel;
  if (current && next.viewId !== current.viewId) {
    throw new Error('A different component view cannot replace this view.');
  }

  const location = new URL(next.location);
  const base = new URL(next.baseUri);
  if (!base.pathname.endsWith('/') || !isInternal(location, base)) {
    throw new Error('The component location is outside its registered application.');
  }

  const incoming = new DOMParser().parseFromString(next.document, 'text/html');
  let baseElement = incoming.querySelector('base');
  if (!baseElement) {
    baseElement = incoming.createElement('base');
    incoming.head.prepend(baseElement);
  }

  const assetBase = new URL(baseElement.getAttribute('href') ?? next.location, location);
  baseElement.href = assetBase.href;
  for (const script of incoming.querySelectorAll<HTMLScriptElement>('script[src]')) {
    const source = new URL(script.getAttribute('src')!, assetBase);
    if (source.pathname.endsWith('/blazor.mcp.js')) {
      script.replaceWith(runtimeScript.cloneNode(true));
    } else if (/\/_framework\/blazor\.(web|server|webassembly)\.js$/.test(source.pathname)) {
      script.remove();
    }
  }

  const previousLocation = current?.location;
  if (next.preserveDom) {
    synchronizeDomContent(document, incoming);
  } else {
    synchronizeAttributes(document.documentElement, incoming.documentElement);
    synchronizeAttributes(document.head, incoming.head);
    synchronizeDomContent(document.head, incoming.head);
    document.body.replaceWith(document.importNode(incoming.body, true));
  }

  current = next;
  updateBusyState();
  applyContext(app.getHostContext());
  if (replaceHistory || next.replaceHistory || history.state?.blazorMcp === undefined
    || (fromPost && (!next.redirected || previousLocation === next.location))) {
    history.replaceState({ blazorMcp: next.location }, '');
  } else {
    history.pushState({ blazorMcp: next.location }, '');
  }

}

async function dispatch(location: URL, method: 'GET' | 'POST', fields?: Record<string, string[]>, files?: FormFile[], replaceHistory = false, enctype?: string, requestGeneration = ++generation): Promise<void> {
  if (closing || !current) {
    throw new Error('The component view is not available.');
  }

  if (requestGeneration !== generation) {
    return;
  }

  const snapshot = current;
  try {
    const result = await app.callServerTool({
      name: snapshot.dispatchTool,
      arguments: {
        viewId: snapshot.viewId,
        location: location.href,
        method,
        fields,
        files,
        enctype,
      },
    }, { signal: lifetime.signal });
    if (!closing && generation === requestGeneration) {
      applyDocument(result, replaceHistory, method === 'POST');
    }
  } catch (error) {
    if (!closing && generation === requestGeneration) {
      throw error;
    }

    if (minimumLogLevel <= 1) {
      console.debug('Discarding completion from a superseded or closed component operation.', error);
    }
  }
}

function getFormMethod(form: HTMLFormElement, submitter: HTMLElement | null): string {
  const control = submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement ? submitter : undefined;
  return (control?.hasAttribute('formmethod') ? control.formMethod : form.method).toUpperCase();
}

async function submit(form: HTMLFormElement, submitter: HTMLElement | null): Promise<void> {
  if (!current) {
    throw new Error('The component view has not received its initial document.');
  }

  const requestGeneration = ++generation;
  const target = submitter?.getAttribute('formtarget') ?? form.getAttribute('target');
  if (target && target.toLowerCase() !== '_self') {
    throw new Error(`The component form target '${target}' is not supported by this host.`);
  }

  const method = getFormMethod(form, submitter);
  if (method !== 'GET' && method !== 'POST') {
    throw new Error(`The component form method '${method}' is not supported.`);
  }

  const action = submitter?.getAttribute('formaction') ?? form.getAttribute('action') ?? current.location;
  const location = action ? new URL(action, document.baseURI) : new URL(current.location);
  if (!isInternal(location, new URL(current.baseUri))) {
    throw new Error('The component form action is outside its registered application.');
  }

  const data = new FormData(form, submitter);
  const control = submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement ? submitter : undefined;
  const enctype = control?.hasAttribute('formenctype') ? control.formEnctype : form.enctype;
  if (method === 'POST' && enctype !== 'application/x-www-form-urlencoded' && enctype !== 'multipart/form-data') {
    throw new Error(`The component form encoding '${enctype}' is not supported.`);
  }

  const fields: Record<string, string[]> = Object.create(null);
  const files: FormFile[] = [];
  const query = new URLSearchParams();
  let totalFileBytes = 0;
  for (const [name, value] of data) {
    if (method === 'GET') {
      query.append(name, typeof value === 'string' ? value : value.name);
    } else if (typeof value === 'string' || enctype === 'application/x-www-form-urlencoded') {
      (fields[name] ??= []).push(typeof value === 'string' ? value : value.name);
    } else {
      if (value.name === '' && value.size === 0) {
        (fields[name] ??= []).push('');
        continue;
      }

      totalFileBytes += value.size;
      if (totalFileBytes > 32 * 1024 * 1024) {
        throw new Error('Submitted files exceed the 32 MiB submission limit.');
      }

      const bytes = new Uint8Array(await value.arrayBuffer());
      let binary = '';
      for (let offset = 0; offset < bytes.length; offset += 16384) {
        binary += String.fromCharCode(...bytes.subarray(offset, offset + 16384));
      }

      files.push({ name, fileName: value.name, contentType: value.type, lastModified: value.lastModified, data: btoa(binary) });
    }
  }

  if (method === 'GET') {
    location.search = query.toString();
    await dispatch(location, 'GET', undefined, undefined, false, undefined, requestGeneration);
  } else {
    if (fields._handler?.length !== 1) {
      throw new Error('A component POST must identify exactly one named form handler.');
    }

    await dispatch(location, 'POST', fields, files, false, enctype, requestGeneration);
  }
}

document.addEventListener('submit', event => {
  if (event.defaultPrevented || !(event.target instanceof HTMLFormElement)) {
    return;
  }

  const form = event.target;
  const submitter = event.submitter;
  if (getFormMethod(form, submitter) === 'DIALOG') {
    return;
  }

  event.preventDefault();
  trackOperation(submit(form, submitter));
}, { signal: lifetime.signal });

document.addEventListener('click', event => {
  const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>('a[href]') : null;
  if (!link || !current || event.defaultPrevented || event.button !== 0) {
    return;
  }

  const location = new URL(link.href);
  const modified = event.ctrlKey || event.metaKey || event.altKey || event.shiftKey;
  event.preventDefault();
  if (link.hasAttribute('download')) {
    showError(new Error('The current component host has not enabled host-mediated downloads.'));
  } else if (modified || (link.target && link.target !== '_self') || !isInternal(location, new URL(current.baseUri))) {
    void app.openLink({ url: location.href }).catch(showError);
  } else {
    trackOperation(dispatch(location, 'GET'));
  }
}, { signal: lifetime.signal });

window.addEventListener('popstate', event => {
  if (typeof event.state?.blazorMcp === 'string') {
    trackOperation(dispatch(new URL(event.state.blazorMcp), 'GET', undefined, undefined, true));
  }
}, { signal: lifetime.signal });

app.ontoolresult = result => {
  if (!connected) {
    initialResult = result;
  } else if (!closing && generation === 0) {
    try {
      applyDocument(result, true);
    } catch (error) {
      showError(error);
    }
  }
};
app.ontoolcancelled = parameters => showError(new Error(parameters.reason || 'The component operation was canceled.'));
app.onhostcontextchanged = applyContext;
app.onteardown = async () => {
  closing = true;
  lifetime.abort();
  await Promise.all(pendingOperations);
  if (current) {
    const result = await app.callServerTool({
      name: current.dispatchTool,
      arguments: { viewId: current.viewId, method: 'CLOSE' },
    });
    if (result.isError) {
      throw new Error('The component view could not be closed.');
    }
  }

  return {};
};

async function start(): Promise<void> {
  await app.connect();
  connected = true;
  applyContext(app.getHostContext());
  if (initialResult !== undefined) {
    applyDocument(initialResult, true);
    initialResult = undefined;
  }
}

void start().catch(showError);
