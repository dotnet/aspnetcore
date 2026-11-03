// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let clickListenerRegisteredCallback: ((isDispatchingClick: boolean) => void) | undefined;
let clickDispatchDepth = 0;

export function setClickListenerRegisteredCallback(callback: typeof clickListenerRegisteredCallback): void {
  clickListenerRegisteredCallback = callback;
}

export function notifyGlobalListenerAdded(eventName: string): void {
  if (eventName === 'click') {
    clickListenerRegisteredCallback?.(clickDispatchDepth > 0);
  }
}

export function dispatchGlobalEvent(event: Event, listener: EventListener): void {
  if (event.type !== 'click') {
    listener(event);
    return;
  }

  clickDispatchDepth++;
  try {
    listener(event);
  } finally {
    clickDispatchDepth--;
  }
}
