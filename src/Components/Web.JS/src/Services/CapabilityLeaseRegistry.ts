// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebRendererId } from '../Rendering/WebRendererId';

export class CapabilityLeaseRegistry {
  private readonly _routerLeasesByRenderer = new Map<WebRendererId, Set<string>>();

  private readonly _navigationLocksByRenderer = new Map<WebRendererId | 0, Set<string>>();

  private readonly _locationChangingRenderers = new Set<WebRendererId>();

  private readonly _revocationListeners = new Set<(rendererId: WebRendererId) => void>();

  private _beforeUnloadAttached = false;

  private readonly _beforeUnloadHandler = (event: BeforeUnloadEvent) => {
    event.preventDefault();
    event.returnValue = true;
  };

  public acquireRouterLease(rendererId: WebRendererId, leaseId = 'default'): void {
    for (const [existingRendererId, leases] of this._routerLeasesByRenderer) {
      if (existingRendererId !== rendererId && leases.size > 0) {
        throw new Error('Only one interactive runtime may enable navigation interception at a time.');
      }
    }

    let leases = this._routerLeasesByRenderer.get(rendererId);
    if (!leases) {
      leases = new Set<string>();
      this._routerLeasesByRenderer.set(rendererId, leases);
    }
    leases.add(leaseId);
  }

  public releaseRouterLease(rendererId: WebRendererId, leaseId = 'default'): void {
    const leases = this._routerLeasesByRenderer.get(rendererId);
    if (!leases) {
      return;
    }

    if (leaseId === '*') {
      leases.clear();
    } else {
      leases.delete(leaseId);
    }

    if (leases.size === 0) {
      this._routerLeasesByRenderer.delete(rendererId);
    }
  }

  public getInteractiveRouterRendererId(): WebRendererId | undefined {
    for (const [rendererId, leases] of this._routerLeasesByRenderer) {
      if (leases.size > 0) {
        return rendererId;
      }
    }
    return undefined;
  }

  public hasInteractiveRouter(): boolean {
    return this.getInteractiveRouterRendererId() !== undefined;
  }

  public enableNavigationPrompt(id: string, rendererId: WebRendererId | 0 = 0): void {
    let locks = this._navigationLocksByRenderer.get(rendererId);
    if (!locks) {
      locks = new Set<string>();
      this._navigationLocksByRenderer.set(rendererId, locks);
    }
    locks.add(id);
    this.syncBeforeUnloadListener();
  }

  public disableNavigationPrompt(id: string, rendererId?: WebRendererId | 0): void {
    if (rendererId !== undefined) {
      const locks = this._navigationLocksByRenderer.get(rendererId);
      locks?.delete(id);
      if (locks && locks.size === 0) {
        this._navigationLocksByRenderer.delete(rendererId);
      }
    } else {
      for (const [ownerId, locks] of this._navigationLocksByRenderer) {
        locks.delete(id);
        if (locks.size === 0) {
          this._navigationLocksByRenderer.delete(ownerId);
        }
      }
    }
    this.syncBeforeUnloadListener();
  }

  public hasAnyNavigationPrompts(): boolean {
    for (const locks of this._navigationLocksByRenderer.values()) {
      if (locks.size > 0) {
        return true;
      }
    }
    return false;
  }

  public setLocationChangingLease(rendererId: WebRendererId, hasListeners: boolean): void {
    if (hasListeners) {
      this._locationChangingRenderers.add(rendererId);
    } else {
      this._locationChangingRenderers.delete(rendererId);
    }
  }

  public hasLocationChangingLease(rendererId: WebRendererId): boolean {
    return this._locationChangingRenderers.has(rendererId);
  }

  public getLocationChangingRendererIds(): WebRendererId[] {
    return Array.from(this._locationChangingRenderers);
  }

  public onRendererLeasesRevoked(listener: (rendererId: WebRendererId) => void): () => void {
    this._revocationListeners.add(listener);
    return () => this._revocationListeners.delete(listener);
  }

  public revokeAllForRenderer(rendererId: WebRendererId): void {
    this._routerLeasesByRenderer.delete(rendererId);
    this._navigationLocksByRenderer.delete(rendererId);
    if (rendererId === WebRendererId.Server) {
      // Also clear any unattributed locks when the server circuit is torn down
      // and no WebAssembly renderer is attached.
      this._navigationLocksByRenderer.delete(0);
    }
    this._locationChangingRenderers.delete(rendererId);
    this.syncBeforeUnloadListener();

    for (const listener of this._revocationListeners) {
      listener(rendererId);
    }
  }

  public resetForTest(): void {
    this._routerLeasesByRenderer.clear();
    this._navigationLocksByRenderer.clear();
    this._locationChangingRenderers.clear();
    this.syncBeforeUnloadListener();
  }

  private syncBeforeUnloadListener(): void {
    const shouldAttach = this.hasAnyNavigationPrompts();
    if (shouldAttach && !this._beforeUnloadAttached) {
      window.addEventListener('beforeunload', this._beforeUnloadHandler);
      this._beforeUnloadAttached = true;
    } else if (!shouldAttach && this._beforeUnloadAttached) {
      window.removeEventListener('beforeunload', this._beforeUnloadHandler);
      this._beforeUnloadAttached = false;
    }
  }
}

export const capabilityLeaseRegistry = new CapabilityLeaseRegistry();
