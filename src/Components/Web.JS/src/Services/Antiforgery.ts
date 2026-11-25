// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

interface AntiforgeryRequestToken {
  value: string;
  formFieldName: string;
}

let endpoint: string | null = null;
let tokenRequest: Promise<AntiforgeryRequestToken> | undefined;
let tokenRequestController: AbortController | undefined;
let configurationVersion = 0;
const elements = new Set<AntiforgeryTokenElement>();
const pendingSubmissions = new WeakSet<HTMLFormElement>();

export function cancelAntiforgeryTokenAcquisition(): void {
  tokenRequestController?.abort();
  tokenRequestController = undefined;
  tokenRequest = undefined;
  configurationVersion++;
}

export function configureAntiforgery(value: string | null): void {
  cancelAntiforgeryTokenAcquisition();
  endpoint = value;
}

async function getAntiforgeryToken(): Promise<AntiforgeryRequestToken | null> {
  if (!endpoint) {
    return null;
  }

  const version = configurationVersion;
  if (!tokenRequest) {
    const url = new URL(endpoint, document.baseURI);
    if (url.origin !== location.origin) {
      throw new Error('The antiforgery endpoint must be on the same origin as the document.');
    }
    const controller = new AbortController();
    tokenRequestController = controller;
    const acquire = async (): Promise<AntiforgeryRequestToken> => {
      const response = await fetch(url, {
        method: 'POST',
        signal: controller.signal,
        credentials: 'same-origin',
        cache: 'no-store',
        redirect: 'error',
        headers: { 'Blazor-Antiforgery': 'true' },
      });
      if (!response.ok) {
        throw new Error(`Failed to acquire an antiforgery token: HTTP ${response.status}.`);
      }
      const token: AntiforgeryRequestToken = await response.json();
      if (typeof token.value !== 'string' || !token.value ||
          typeof token.formFieldName !== 'string' || !token.formFieldName) {
        throw new Error('The antiforgery endpoint returned an invalid token.');
      }
      return token;
    };
    // Other tabs can also be acquiring their first cookie.
    const request = (async () => navigator.locks
      ? await navigator.locks.request('blazor-antiforgery', { signal: controller.signal }, acquire)
      : await acquire())();
    tokenRequest = request;
    request.catch(() => {
      if (tokenRequest === request) {
        tokenRequest = undefined;
      }
    });
  }

  const token = await tokenRequest;
  if (version !== configurationVersion) {
    throw new Error('Antiforgery token acquisition was superseded by navigation.');
  }
  return token;
}

export function attachAntiforgeryListener(): void {
  customElements.define('blazor-antiforgery', AntiforgeryTokenElement);
}

export function refreshAntiforgeryTokens(): void {
  elements.forEach(element => element.refresh());
}

class AntiforgeryTokenElement extends HTMLElement {
  ready = false;

  private pending: Promise<void> | undefined;

  private version = -1;

  private form: HTMLFormElement | null = null;

  connectedCallback(): void {
    queueMicrotask(() => {
      if (this.isConnected) {
        this.form?.removeEventListener('submit', this.onSubmit, true);
        this.form = this.closest('form');
        this.form?.addEventListener('submit', this.onSubmit, true);
        elements.add(this);
        this.refresh();
      }
    });
  }

  disconnectedCallback(): void {
    queueMicrotask(() => {
      if (!this.isConnected) {
        this.form?.removeEventListener('submit', this.onSubmit, true);
        this.form = null;
        elements.delete(this);
        this.pending = undefined;
        this.ready = false;
      }
    });
  }

  refresh(): void {
    if (!this.isConnected) {
      return;
    }
    const version = configurationVersion;
    this.ensureToken().catch(error => {
      if (this.version === version) {
        this.showError(error);
      }
    });
  }

  ensureToken(): Promise<void> {
    if (this.version !== configurationVersion || this.ready && !this.querySelector('input')) {
      this.version = configurationVersion;
      this.ready = false;
      this.pending = undefined;
    }
    if (this.ready) {
      return Promise.resolve();
    }
    if (!this.pending) {
      const pending = this.acquireToken(this.version).finally(() => {
        if (this.pending === pending) {
          this.pending = undefined;
        }
      });
      this.pending = pending;
    }
    return this.pending;
  }

  showError(error: unknown): void {
    if (this.isConnected && this.version === configurationVersion) {
      console.error(error);
    }
  }

  private async acquireToken(version: number): Promise<void> {
    const token = await getAntiforgeryToken();
    if (this.isConnected && version === configurationVersion && token) {
      const field = document.createElement('input');
      field.type = 'hidden';
      field.name = token.formFieldName;
      field.value = token.value;
      this.replaceChildren(field);
      this.ready = true;
    }
  }

  private onSubmit = (event: SubmitEvent): void => {
    const form = this.form;
    if (!endpoint || !form || event.defaultPrevented) {
      return;
    }
    // Named form controls can shadow instance members such as requestSubmit.
    const pending = Array.from(Element.prototype.querySelectorAll.call(form, 'blazor-antiforgery'))
      .filter((element): element is AntiforgeryTokenElement => element instanceof AntiforgeryTokenElement && !element.ready);
    if (!pending.length) {
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    if (pendingSubmissions.has(form)) {
      return;
    }
    pendingSubmissions.add(form);
    const submitter = event.submitter;
    const version = configurationVersion;
    Promise.all(pending.map(element => element.ensureToken())).then(() => {
      if (form.isConnected && version === configurationVersion && pending.every(element => element.ready)) {
        if (!submitter) {
          HTMLFormElement.prototype.requestSubmit.call(form);
        } else if ((submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement) &&
            submitter.isConnected && submitter.form === form) {
          HTMLFormElement.prototype.requestSubmit.call(form, submitter);
        }
      }
    }).catch(error => {
      pending.forEach(element => element.showError(error));
    }).finally(() => {
      pendingSubmissions.delete(form);
    });
  };
}
