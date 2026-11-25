// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let removed: { link: HTMLLinkElement; id: string }[] | undefined;
let inserted: HTMLLinkElement[] | undefined;
let batchActive = false;
const pending = new Map<HTMLLinkElement, { previous: HTMLLinkElement; finish: () => void }>();

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
      const transition = pending.get(link);
      const previous = transition?.previous ?? link;
      const replacement = id ? incoming?.find(candidate => candidate.id === id) : undefined;

      if (transition) {
        pending.delete(link);
        transition.finish();
        link.remove();
      }

      if (!replacement || replacement.disabled || !previous.sheet || replacement.sheet || pending.has(replacement)) {
        previous.remove();
        continue;
      }

      const finish = () => {
        clearTimeout(timeout);
        replacement.removeEventListener('load', finish);
        replacement.removeEventListener('error', finish);
        if (pending.delete(replacement)) {
          previous.remove();
        }
      };
      const timeout = setTimeout(finish, 30_000);
      pending.set(replacement, { previous, finish });
      replacement.addEventListener('load', finish);
      replacement.addEventListener('error', finish);
    }
  }

  for (const [replacement, transition] of pending) {
    if (replacement.disabled || !isHeadStylesheet(replacement)) {
      transition.finish();
    }
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
