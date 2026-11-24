// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let removed: HTMLLinkElement[] | undefined;
let inserted: HTMLLinkElement[] | undefined;
const pending = new Map<HTMLLinkElement, { previous: HTMLLinkElement; finish: () => void }>();

export function beginHeadStylesheetTransition(): void {
  removed = [];
  inserted = [];
}

export function recordInsertedHeadStylesheet(node: Node): void {
  if (inserted && isHeadStylesheet(node)) {
    inserted.push(node);
  }
}

export function deferHeadStylesheetRemoval(node: Node): boolean {
  if (removed && isHeadStylesheet(node)) {
    removed.push(node);
    return true;
  }

  return false;
}

export function endHeadStylesheetTransition(): void {
  const outgoing = removed!;
  const incoming = inserted!.filter(link => link.parentNode === document.head && !outgoing.includes(link));
  removed = inserted = undefined;

  for (const link of outgoing) {
    const transition = pending.get(link);
    const previous = transition?.previous ?? link;
    const replacement = link.id ? incoming.find(candidate => candidate.id === link.id) : undefined;

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

function isHeadStylesheet(node: Node): node is HTMLLinkElement {
  return node instanceof HTMLLinkElement && node.parentNode === document.head && node.relList.contains('stylesheet');
}
