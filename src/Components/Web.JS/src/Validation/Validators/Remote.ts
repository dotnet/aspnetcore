// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Remote validator: validates a field value against a server endpoint.
//
// Error handling: fail-open. Network errors and aborted requests resolve as
// valid (true). Server-side validation is always the authoritative check, so
// a transient client-side failure should not block form submission.

import { getElementForm } from '../DomUtils';
import { ValidatableElement, ValidationContext, ValidationResult } from '../ValidationTypes';

// Per-element cache (WeakMap → auto GC when element removed from DOM)
const cache = new WeakMap<ValidatableElement, { value: string; result: ValidationResult }>();
const timers = new WeakMap<ValidatableElement, ReturnType<typeof setTimeout>>();

const DEFAULT_DEBOUNCE_MS = 200;

export function remoteValidator(context: ValidationContext): ValidationResult | Promise<ValidationResult> {
  const { value, element, params, signal } = context;
  const url = params.url;
  if (!url) {
    return true;
  }

  const fieldValue = value ?? '';

  // Cache hit → return sync result
  const cached = cache.get(element);
  if (cached && cached.value === fieldValue) {
    return cached.result;
  }

  // Clear previous debounce timer (the previous signal is already aborted by the tracker)
  const prevTimer = timers.get(element);
  if (prevTimer !== undefined) {
    clearTimeout(prevTimer);
  }

  const method = (params.type ?? 'GET').toUpperCase();
  if (method !== 'GET' && method !== 'POST') {
    return true;
  }

  const data = resolveRequestData(element, fieldValue, params);
  const errorMessage = params[''] || 'This field is invalid.';

  // On form submit, skip debounce — the user has finished typing.
  const debounceMs = context.immediate ? 0 : parseDebounce(params.debounce);

  return new Promise<ValidationResult>((resolve) => {
    // If the tracker aborts the signal (new validation supersedes this one),
    // clear the debounce timer and resolve as valid (fail-open).
    signal?.addEventListener('abort', () => {
      clearTimeout(timer);
      resolve(true);
    });

    const timer = setTimeout(async () => {
      timers.delete(element);
      try {
        const result = await doFetch(url, method, data, errorMessage, signal);
        cache.set(element, { value: fieldValue, result });
        resolve(result);
      } catch {
        // Aborted or network error → fail open
        resolve(true);
      }
    }, debounceMs);

    timers.set(element, timer);
  });
}

async function doFetch(
  url: string,
  method: string,
  data: URLSearchParams,
  errorMessage: string,
  signal?: AbortSignal,
): Promise<ValidationResult> {
  const fetchUrl = method === 'GET' ? `${url}?${data}` : url;
  const response = await fetch(fetchUrl, {
    method,
    signal,
    ...(method !== 'GET' && { body: data }),
  });

  const json = await response.json();

  if (json === true || json === 'true') {
    return true;
  }

  // Server can return a custom error message string
  if (typeof json === 'string') {
    return json;
  }

  // Fall back to the error message from data-val-remote attribute
  return errorMessage;
}

function resolveRequestData(element: ValidatableElement, value: string, params: Record<string, string>): URLSearchParams {
  const data = new URLSearchParams();
  const fieldName = element.name;
  data.set(fieldName, value);

  if (params.additionalfields) {
    const form = getElementForm(element);
    if (form) {
      for (const spec of params.additionalfields.split(',')) {
        const name = resolveFieldName(fieldName, spec.trim());
        const el = form.querySelector<HTMLInputElement>(`[name="${CSS.escape(name)}"]`);
        if (el) {
          data.set(name, el.value);
        }
      }
    }
  }

  return data;
}

function resolveFieldName(currentName: string, spec: string): string {
  if (spec.startsWith('*.')) {
    const dot = currentName.lastIndexOf('.');
    return (dot >= 0 ? currentName.substring(0, dot + 1) : '') + spec.substring(2);
  }

  return spec;
}

function parseDebounce(value: string | undefined): number {
  if (!value) {
    return DEFAULT_DEBOUNCE_MS;
  }

  const parsed = parseInt(value, 10);

  return parsed >= 0 ? parsed : DEFAULT_DEBOUNCE_MS;
}
