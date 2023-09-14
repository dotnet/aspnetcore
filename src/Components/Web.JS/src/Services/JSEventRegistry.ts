// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IBlazor } from '../GlobalExports';

// The base Blazor event type.
// Properties listed here get assigned by the event registry in 'dispatchEvent'.
interface BlazorEvent {
  type: keyof BlazorEventMap;
}

// Maps Blazor event names to the argument type passed to registered listeners.
export interface BlazorEventMap {
  'enhancedload': BlazorEvent;
}

export class JSEventRegistry {
  private readonly _eventListeners = new Map<string, Set<(ev: any) => void>>();

  static create(blazor: IBlazor): JSEventRegistry {
    const result = new JSEventRegistry();
    blazor.addEventListener = result.addEventListener.bind(result);
    blazor.removeEventListener = result.removeEventListener.bind(result);
    return result;
  }

  public addEventListener<K extends keyof BlazorEventMap>(type: K, listener: (ev: BlazorEventMap[K]) => void): void {
    let listenersForEventType = this._eventListeners.get(type);
    if (!listenersForEventType) {
      listenersForEventType = new Set();
      this._eventListeners.set(type, listenersForEventType);
    }

    listenersForEventType.add(listener);
  }

  public removeEventListener<K extends keyof BlazorEventMap>(type: K, listener: (ev: BlazorEventMap[K]) => void): void {
    this._eventListeners.get(type)?.delete(listener);
  }

  public dispatchEvent<K extends keyof BlazorEventMap>(type: K, ev: Omit<BlazorEventMap[K], keyof BlazorEvent>): void {
    const listenersForEventType = this._eventListeners.get(type);
    if (!listenersForEventType) {
      return;
    }

    const event: BlazorEventMap[K] = {
      ...ev,
      type,
    };

    for (const listener of listenersForEventType) {
      listener(event);
    }
  }
}
