// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const editableInputTypes = new Set<string>([
  'text', 'search', 'email', 'url', 'tel', 'password',
  'number', 'date', 'time', 'datetime-local', 'month', 'week',
]);

const contentEditableSnapshots = new WeakMap<Element, string>();
const trackedDocs = new WeakSet<Document>();
const trackedIframes = new WeakSet<HTMLIFrameElement>();

function trackDocument(doc: Document): void {
  if (trackedDocs.has(doc)) {
    return;
  }
  trackedDocs.add(doc);
  doc.addEventListener('focusin', () => {
    trackSameOriginIframes();
    const el = getDeepActiveElement();
    if (el?.isContentEditable && !contentEditableSnapshots.has(el)) {
      contentEditableSnapshots.set(el, el.textContent ?? '');
    }
  }, true);
}

function tryTrackIframeDocument(iframe: HTMLIFrameElement): void {
  try {
    if (iframe.contentDocument) {
      trackDocument(iframe.contentDocument);
    }
  } catch {
    // Cross-origin iframe; contentDocument access throws SecurityError.
  }
}

function trackSameOriginIframes(): void {
  for (const iframe of Array.from(document.getElementsByTagName('iframe'))) {
    tryTrackIframeDocument(iframe);
    if (trackedIframes.has(iframe)) {
      continue;
    }
    trackedIframes.add(iframe);
    iframe.addEventListener('load', () => tryTrackIframeDocument(iframe));
  }
}

export function initEditedTracking(): void {
  trackDocument(document);
  trackSameOriginIframes();
}

export function getDeepActiveElement(): HTMLElement | null {
  let el: Element | null = document.activeElement;
  while (el) {
    const shadowFocus = (el as HTMLElement).shadowRoot?.activeElement;
    if (shadowFocus) {
      el = shadowFocus;
      continue;
    }
    if (el instanceof HTMLIFrameElement) {
      try {
        const inner = el.contentDocument?.activeElement;
        if (inner && inner !== el.contentDocument!.body) {
          el = inner;
          continue;
        }
      } catch {
        // Cross-origin iframe; contentDocument access throws SecurityError.
      }
    }
    return el as HTMLElement;
  }
  return null;
}

export function isFocusedElementEdited(): boolean {
  const el = getDeepActiveElement();
  if (!el || el === document.body) {
    return false;
  }
  if (el.isContentEditable) {
    return (el.textContent ?? '') !== (contentEditableSnapshots.get(el) ?? '');
  }
  if (el instanceof HTMLTextAreaElement) {
    return el.value !== el.defaultValue;
  }
  if (el instanceof HTMLInputElement) {
    if (!editableInputTypes.has((el.type || 'text').toLowerCase())) {
      return false;
    }
    return el.value !== el.defaultValue;
  }
  return false;
}
