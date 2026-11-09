// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebRendererId } from '../Rendering/WebRendererId';
import { discoverServerPersistedState, discoverWebAssemblyPersistedState } from './ComponentDescriptorDiscovery';

export type LocationSnapshot = {
  uri: string;
  state: string | undefined;
  interceptedLink: boolean;
  epoch: number;
};

type EpochTaggedState = {
  epoch: number;
  state: string;
};

export class DocumentStateStore {
  private _currentEpoch = 1;

  private _serverPersistedState?: EpochTaggedState;

  private _webAssemblyPersistedState?: EpochTaggedState;

  private readonly _rendererLocationSnapshots = new Map<WebRendererId, LocationSnapshot>();

  private readonly _pendingInterceptedByRenderer = new Map<WebRendererId, boolean>();

  private _latestInterceptedLink = false;

  private _hasUnattachedNavigation = false;

  public get currentEpoch(): number {
    return this._currentEpoch;
  }

  public advanceEpoch(): number {
    this._currentEpoch++;
    // Any unclaimed persisted component state from a previous document epoch
    // belongs to the page we just navigated away from and must not be restored
    // into components of the new document.
    if (this._serverPersistedState && this._serverPersistedState.epoch !== this._currentEpoch) {
      this._serverPersistedState = undefined;
    }
    if (this._webAssemblyPersistedState && this._webAssemblyPersistedState.epoch !== this._currentEpoch) {
      this._webAssemblyPersistedState = undefined;
    }
    return this._currentEpoch;
  }

  public capturePersistedStateFromDocument(root: Node): void {
    const serverState = discoverServerPersistedState(root);
    if (serverState) {
      this._serverPersistedState = {
        epoch: this._currentEpoch,
        state: serverState,
      };
    }

    const wasmState = discoverWebAssemblyPersistedState(root);
    if (wasmState) {
      this._webAssemblyPersistedState = {
        epoch: this._currentEpoch,
        state: wasmState,
      };
    }
  }

  public consumeServerPersistedState(root?: Node): string {
    if (root) {
      const discovered = discoverServerPersistedState(root);
      if (discovered) {
        this._serverPersistedState = {
          epoch: this._currentEpoch,
          state: discovered,
        };
      }
    }

    if (this._serverPersistedState) {
      const { epoch, state } = this._serverPersistedState;
      this._serverPersistedState = undefined;
      if (epoch === this._currentEpoch) {
        return state;
      }
    }

    return '';
  }

  public consumeWebAssemblyPersistedState(root?: Node): string {
    if (root) {
      const discovered = discoverWebAssemblyPersistedState(root);
      if (discovered) {
        this._webAssemblyPersistedState = {
          epoch: this._currentEpoch,
          state: discovered,
        };
      }
    }

    if (this._webAssemblyPersistedState) {
      const { epoch, state } = this._webAssemblyPersistedState;
      this._webAssemblyPersistedState = undefined;
      if (epoch === this._currentEpoch) {
        return state;
      }
    }

    return '';
  }

  public recordRendererInitialLocation(rendererId: WebRendererId, uri: string, state: string | undefined): void {
    this._rendererLocationSnapshots.set(rendererId, {
      uri,
      state,
      interceptedLink: false,
      epoch: this._currentEpoch,
    });
    this._pendingInterceptedByRenderer.delete(rendererId);
  }

  public recordLocationDelivered(rendererId: WebRendererId, uri: string, state: string | undefined, interceptedLink: boolean): void {
    this._rendererLocationSnapshots.set(rendererId, {
      uri,
      state,
      interceptedLink,
      epoch: this._currentEpoch,
    });
    this._pendingInterceptedByRenderer.delete(rendererId);
  }

  public recordUnattachedNavigation(rendererId: WebRendererId | undefined, interceptedLink: boolean): void {
    this._latestInterceptedLink = interceptedLink;
    this._hasUnattachedNavigation = true;
    if (rendererId !== undefined) {
      this._pendingInterceptedByRenderer.set(rendererId, interceptedLink);
    }
  }

  public consumePendingLocationSync(
    rendererId: WebRendererId,
    currentUri: string,
    currentState: string | undefined,
  ): LocationSnapshot | null {
    const previous = this._rendererLocationSnapshots.get(rendererId);
    const pendingIntercepted = this._pendingInterceptedByRenderer.get(rendererId);
    this._pendingInterceptedByRenderer.delete(rendererId);

    const uriChanged = !previous || previous.uri !== currentUri;
    const stateChanged = !previous ? currentState !== undefined : previous.state !== currentState;
    const hadPendingNavigation = pendingIntercepted !== undefined;

    this._rendererLocationSnapshots.set(rendererId, {
      uri: currentUri,
      state: currentState,
      interceptedLink: pendingIntercepted ?? this._latestInterceptedLink,
      epoch: this._currentEpoch,
    });

    if (uriChanged || stateChanged || hadPendingNavigation) {
      return {
        uri: currentUri,
        state: currentState,
        interceptedLink: pendingIntercepted ?? (this._hasUnattachedNavigation ? this._latestInterceptedLink : false),
        epoch: this._currentEpoch,
      };
    }

    return null;
  }

  public resetForTest(): void {
    this._currentEpoch = 1;
    this._serverPersistedState = undefined;
    this._webAssemblyPersistedState = undefined;
    this._rendererLocationSnapshots.clear();
    this._pendingInterceptedByRenderer.clear();
    this._latestInterceptedLink = false;
    this._hasUnattachedNavigation = false;
  }
}

export const documentStateStore = new DocumentStateStore();
