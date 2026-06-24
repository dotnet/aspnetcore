// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

function collectMediaElements(root: Document | ShadowRoot, into: HTMLMediaElement[]): void {
  for (const el of Array.from(root.querySelectorAll<HTMLElement>('audio, video'))) {
    into.push(el as HTMLMediaElement);
  }
  for (const el of Array.from(root.querySelectorAll<HTMLElement>('*'))) {
    if (el.shadowRoot) {
      collectMediaElements(el.shadowRoot, into);
    }
  }

  for (const iframe of Array.from(root.querySelectorAll('iframe'))) {
    try {
      const doc = (iframe as HTMLIFrameElement).contentDocument;
      if (doc) {
        collectMediaElements(doc, into);
      }
    } catch {
      // Cross-origin iframe; contentDocument access throws SecurityError.
    }
  }
}

export function isMediaPlaying(): boolean {
  const elements: HTMLMediaElement[] = [];
  collectMediaElements(document, elements);
  for (const el of elements) {
    if (!el.paused && !el.muted && el.volume > 0) {
      return true;
    }
  }
  return false;
}

export function isPictureInPictureActive(): boolean {
  return (document as Document & { pictureInPictureElement?: Element | null }).pictureInPictureElement != null;
}

export async function queryWebLockHeld(): Promise<boolean> {
  if (!navigator.locks) {
    return false;
  }
  try {
    const state = await navigator.locks.query();
    return (state.held?.length ?? 0) > 0;
  } catch {
    return false;
  }
}
