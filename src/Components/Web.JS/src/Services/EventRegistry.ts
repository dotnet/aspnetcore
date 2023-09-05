// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IBlazor } from '../GlobalExports';

// The base Blazor event type.
// Properties listed here get assigned by the event registry in 'dispatchEvent'.
interface BlazorEvent {
  blazor: IBlazor;
  type: keyof BlazorEventMap;
}

// Maps Blazor event names to the argument type passed to registered listeners.
export interface BlazorEventMap {
  'enhancedload': BlazorEvent;
}

export class EventRegistry {
  private readonly _eventListeners = new Map<string, Set<(ev: any) => void>>();

  private _blazor: IBlazor | null = null;

  public attachBlazorInstance(blazor: IBlazor) {
    this._blazor = blazor;
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
    const listenersForEventType = this._eventListeners.get(type);
    if (!listenersForEventType) {
      return;
    }

    listenersForEventType.delete(listener);
  }

  public dispatchEvent<K extends keyof BlazorEventMap>(type: K, ev: Omit<BlazorEventMap[K], keyof BlazorEvent>): void {
    if (this._blazor === null) {
      throw new Error('Blazor events cannot be dispatched until a Blazor instance gets attached');
    }

    const listenersForEventType = this._eventListeners.get(type);
    if (!listenersForEventType) {
      return;
    }

    const event: BlazorEventMap[K] = {
      ...ev,
      blazor: this._blazor,
      type,
    };

    for (const listener of listenersForEventType) {
      listener(event);
    }
  }
}
