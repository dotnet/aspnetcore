// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let removed: { link: HTMLLinkElement; id: string }[] | undefined;
let inserted: HTMLLinkElement[] | undefined;
let batchActive = false;
type PendingTransition = {
  previous: HTMLLinkElement;
  finish: () => void;
  media?: string;
  mediaQueryList?: MediaQueryList;
  mediaQueryListener?: () => void;
};
const pending = new Map<HTMLLinkElement, PendingTransition>();

export function beginHeadStylesheetTransition(): void {
  batchActive = true;
  removed = inserted = undefined;
}

export function recordInsertedHeadStylesheet(node: Node): void {
  if (batchActive && isHeadStylesheet(node)) {
    inserted ??= [];
    inserted.push(node);
  }
}

export function deferHeadStylesheetRemoval(node: Node): boolean {
  if (batchActive && isHeadStylesheet(node)) {
    removed ??= [];
    removed.push({ link: node, id: node.id });
    node.removeAttribute('id');
    return true;
  }

  return false;
}

export function endHeadStylesheetTransition(): void {
  const outgoing = removed;
  const insertedStylesheets = inserted;
  batchActive = false;
  removed = inserted = undefined;

  if (outgoing?.length) {
    const outgoingLinks = outgoing.map(item => item.link);
    const incoming = insertedStylesheets?.filter(link => link.parentNode === document.head && !outgoingLinks.includes(link));

    for (const item of outgoing) {
      const { link, id } = item;
      const previousTransition = pending.get(link);
      const previous = previousTransition?.previous ?? link;
      const replacement = id ? incoming?.find(candidate => candidate.id === id) : undefined;

      if (previousTransition) {
        pending.delete(link);
        previousTransition.finish();
        link.remove();
      }

      if (!replacement || replacement.disabled || !hasStylesheetHref(replacement) || !matchesCurrentMedia(replacement) || !previous.sheet || replacement.sheet || pending.has(replacement)) {
        previous.remove();
        continue;
      }

      let timeout: ReturnType<typeof setTimeout>;
      const transition: PendingTransition = {
        previous,
        finish: () => {
          clearTimeout(timeout);
          replacement.removeEventListener('load', transition.finish);
          replacement.removeEventListener('error', transition.finish);
          if (transition.mediaQueryList && transition.mediaQueryListener) {
            transition.mediaQueryList.removeEventListener('change', transition.mediaQueryListener);
          }
          if (pending.delete(replacement)) {
            previous.remove();
          }
        },
      };
      timeout = setTimeout(transition.finish, 30_000);
      pending.set(replacement, transition);
      replacement.addEventListener('load', transition.finish);
      replacement.addEventListener('error', transition.finish);
      updateMediaQueryListener(replacement, transition);
    }
  }

  for (const [replacement, transition] of pending) {
    if (!hasStylesheetHref(replacement) || replacement.disabled || !isHeadStylesheet(replacement) || !matchesCurrentMedia(replacement)) {
      transition.finish();
    } else {
      updateMediaQueryListener(replacement, transition);
    }
  }
}

function hasStylesheetHref(link: HTMLLinkElement): boolean {
  const href = link.getAttribute('href');
  return href !== null && href.trim().length > 0;
}

function matchesCurrentMedia(link: HTMLLinkElement): boolean {
  const media = link.media.trim();
  return media.length === 0 || window.matchMedia(media).matches;
}

function updateMediaQueryListener(link: HTMLLinkElement, transition: PendingTransition): void {
  const media = link.media.trim();
  if (transition.media === media) {
    return;
  }

  if (transition.mediaQueryList && transition.mediaQueryListener) {
    transition.mediaQueryList.removeEventListener('change', transition.mediaQueryListener);
    transition.mediaQueryList = undefined;
    transition.mediaQueryListener = undefined;
  }

  transition.media = media;
  if (media.length > 0) {
    const mediaQueryList = window.matchMedia(media);
    const listener = () => {
      if (!mediaQueryList.matches) {
        transition.finish();
      }
    };
    transition.mediaQueryList = mediaQueryList;
    transition.mediaQueryListener = listener;
    mediaQueryList.addEventListener('change', listener);
  }
}

function isHeadStylesheet(node: Node): node is HTMLLinkElement {
  if (!(node instanceof HTMLLinkElement) || node.parentNode !== document.head) {
    return false;
  }

  return node.relList.value
    .split(/[ \t\n\f\r]+/)
    .some(rel => rel.toLowerCase() === 'stylesheet');
}
