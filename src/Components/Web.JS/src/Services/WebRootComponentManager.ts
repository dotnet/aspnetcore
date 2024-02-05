// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor, ComponentMarker, descriptorToMarker } from './ComponentDescriptorDiscovery';
import { isRendererAttached, registerRendererAttachedListener } from '../Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../Rendering/WebRendererId';
import { DescriptorHandler } from '../Rendering/DomMerging/DomSync';
import { disposeCircuit, hasStartedServer, isCircuitAvailable, startCircuit, startServer, updateServerRootComponents } from '../Boot.Server.Common';
import { hasLoadedWebAssemblyPlatform, hasStartedLoadingWebAssemblyPlatform, hasStartedWebAssembly, isFirstUpdate, loadWebAssemblyPlatformIfNotStarted, resolveInitialUpdate, setWaitForRootComponents, startWebAssembly, updateWebAssemblyRootComponents, waitForBootConfigLoaded } from '../Boot.WebAssembly.Common';
import { MonoConfig } from 'dotnet-runtime';
import { RootComponentManager } from './RootComponentManager';
import { getRendererer } from '../Rendering/Renderer';
import { isPageLoading } from './NavigationEnhancement';
import { setShouldPreserveContentOnInteractiveComponentDisposal } from '../Rendering/BrowserRenderer';
import { LogicalElement } from '../Rendering/LogicalElements';

type RootComponentOperationBatch = {
  batchId: number;
  operations: RootComponentOperation[];
}

type RootComponentOperation = RootComponentAddOperation | RootComponentUpdateOperation | RootComponentRemoveOperation;

type RootComponentAddOperation = {
  type: 'add';
  ssrComponentId: number;
  marker: ComponentMarker;
};

type RootComponentUpdateOperation = {
  type: 'update';
  ssrComponentId: number;
  marker: ComponentMarker;
};

type RootComponentRemoveOperation = {
  type: 'remove';
  ssrComponentId: number;
};

type RootComponentInfo = {
  descriptor: ComponentDescriptor;
  ssrComponentId: number;
  assignedRendererId?: WebRendererId;
  uniqueIdAtLastUpdate?: number;
  hasPendingRemoveOperation?: boolean;
};

export class WebRootComponentManager implements DescriptorHandler, RootComponentManager</* InitialComponentsDescriptorType */ never> {
  private readonly _rootComponentsBySsrComponentId = new Map<number, RootComponentInfo>();

  private readonly _seenDescriptors = new Set<ComponentDescriptor>();

  private readonly _pendingOperationBatches: { [batchId: number]: RootComponentOperationBatch } = {};

  private _nextOperationBatchId = 1;

  private _nextSsrComponentId = 1;

  private _didWebAssemblyFailToLoadQuickly = false;

  private _isComponentRefreshPending = false;

  private _circuitInactivityTimeoutId: any;

  // Implements RootComponentManager.
  // An empty array becuase all root components managed
  // by WebRootComponentManager are added and removed dynamically.
  public readonly initialComponents: never[] = [];

  public constructor(private readonly _circuitInactivityTimeoutMs: number) {
    // After a renderer attaches, we need to activate any components that were
    // previously skipped for interactivity.
    registerRendererAttachedListener(() => {
      this.rootComponentsMayRequireRefresh();
    });
  }

  // Implements RootComponentManager.
  public onAfterRenderBatch(browserRendererId: number): void {
    if (browserRendererId === WebRendererId.Server) {
      this.circuitMayHaveNoRootComponents();
    }
  }

  public onDocumentUpdated() {
    // Root components may have been added, updated, or removed.
    this.rootComponentsMayRequireRefresh();
  }

  public onEnhancedNavigationCompleted() {
    // Root components may now be ready for activation if they had been previously
    // skipped for activation due to an enhanced navigation being underway.
    this.rootComponentsMayRequireRefresh();
  }

  public registerComponent(descriptor: ComponentDescriptor) {
    if (this._seenDescriptors.has(descriptor)) {
      return;
    }

    // When encountering a component with a WebAssembly or Auto render mode,
    // start loading the WebAssembly runtime, even though we're not
    // activating the component yet. This is becuase WebAssembly resources
    // may take a long time to load, so starting to load them now potentially reduces
    // the time to interactvity.
    if (descriptor.type === 'webassembly') {
      this.startLoadingWebAssemblyIfNotStarted();
    } else if (descriptor.type === 'auto') {
      // If the WebAssembly runtime starts downloading because an Auto component was added to
      // the page, we limit the maximum number of parallel WebAssembly resource downloads to 1
      // so that the performance of any Blazor Server circuit is minimally impacted.
      this.startLoadingWebAssemblyIfNotStarted(/* maxParallelDownloadsOverride */ 1);
    }

    const ssrComponentId = this._nextSsrComponentId++;

    this._seenDescriptors.add(descriptor);
    this._rootComponentsBySsrComponentId.set(ssrComponentId, { descriptor, ssrComponentId });
  }

  private unregisterComponent(component: RootComponentInfo) {
    this._seenDescriptors.delete(component.descriptor);
    this._rootComponentsBySsrComponentId.delete(component.ssrComponentId);
    this.circuitMayHaveNoRootComponents();
  }

  private async startLoadingWebAssemblyIfNotStarted(maxParallelDownloadsOverride?: number) {
    if (hasStartedLoadingWebAssemblyPlatform()) {
      return;
    }

    setWaitForRootComponents();

    const loadWebAssemblyPromise = loadWebAssemblyPlatformIfNotStarted();
    const bootConfig = await waitForBootConfigLoaded();

    if (maxParallelDownloadsOverride !== undefined) {
      bootConfig.maxParallelDownloads = maxParallelDownloadsOverride;
    }

    if (!areWebAssemblyResourcesLikelyCached(bootConfig)) {
      // Since WebAssembly resources aren't likely cached,
      // they will probably need to be fetched over the network.
      // Therefore, we can guess that Blazor WebAssembly won't
      // load quickly, so we fall back to Blazor Server immediately,
      // allowing "auto" components to become interactive sooner than if
      // we were to wait for the timeout.
      this.onWebAssemblyFailedToLoadQuickly();
    }

    await loadWebAssemblyPromise;

    // Store the boot config resource hash in local storage
    // so that we can detect during the next load that WebAssembly
    // resources are cached.
    cacheWebAssemblyResourceHash(bootConfig);

    this.rootComponentsMayRequireRefresh();
  }

  private onWebAssemblyFailedToLoadQuickly() {
    if (this._didWebAssemblyFailToLoadQuickly) {
      return;
    }

    this._didWebAssemblyFailToLoadQuickly = true;
    this.rootComponentsMayRequireRefresh();
  }

  private startCircutIfNotStarted() {
    if (!hasStartedServer()) {
      return startServer(this);
    }

    if (!isCircuitAvailable()) {
      return startCircuit();
    }
  }

  private async startWebAssemblyIfNotStarted() {
    this.startLoadingWebAssemblyIfNotStarted();

    if (!hasStartedWebAssembly()) {
      await startWebAssembly(this);
    }
  }

  // This function should be called each time we think an SSR update
  // should be reflected in an interactive component renderer.
  // Examples include component descriptors updating, document content changing,
  // or an interactive renderer attaching for the first time.
  private rootComponentsMayRequireRefresh() {
    if (this._isComponentRefreshPending) {
      return;
    }

    this._isComponentRefreshPending = true;

    // The following timeout allows us to liberally call this function without
    // taking the small performance hit from requent repeated calls to
    // refreshRootComponents.
    setTimeout(() => {
      this._isComponentRefreshPending = false;
      this.refreshRootComponents(this._rootComponentsBySsrComponentId.values());
    }, 0);
  }

  private circuitMayHaveNoRootComponents() {
    const isCircuitInUse = this.rendererHasExistingOrPendingComponents(WebRendererId.Server, 'server', 'auto');
    if (isCircuitInUse) {
      // Clear the timeout because we know the circuit is in use.
      clearTimeout(this._circuitInactivityTimeoutId);
      this._circuitInactivityTimeoutId = undefined;
      return;
    }

    if (this._circuitInactivityTimeoutId !== undefined) {
      // A timeout is already present, so we shouldn't reset it.
      return;
    }

    // Start a new timeout to dispose the circuit unless it starts getting used.
    this._circuitInactivityTimeoutId = setTimeout(() => {
      if (!this.rendererHasExistingOrPendingComponents(WebRendererId.Server, 'server', 'auto')) {
        disposeCircuit();
        this._circuitInactivityTimeoutId = undefined;
      }
    }, this._circuitInactivityTimeoutMs) as unknown as number;
  }

  private rendererHasComponents(rendererId: WebRendererId): boolean {
    const renderer = getRendererer(rendererId);
    return renderer !== undefined && renderer.getRootComponentCount() > 0;
  }

  private rendererHasExistingOrPendingComponents(rendererId: WebRendererId, ...descriptorTypesToConsider: ComponentMarker['type'][]): boolean {
    if (this.rendererHasComponents(rendererId)) {
      return true;
    }

    // We consider SSR'd components on the page that may get activated using the specified renderer.
    for (const { descriptor: { type }, assignedRendererId } of this._rootComponentsBySsrComponentId.values()) {
      if (assignedRendererId === rendererId) {
        // The component has been assigned to use the specified renderer.
        return true;
      }

      if (assignedRendererId !== undefined) {
        // The component has been assigned to use another renderer.
        continue;
      }

      if (descriptorTypesToConsider.indexOf(type) !== -1) {
        // The component has not been assigned a renderer yet, but it might get activated with the specified renderer
        // if it doesn't get removed from the page.
        return true;
      }
    }

    return false;
  }

  private refreshRootComponents(components: Iterable<RootComponentInfo>) {
    const operationsByRendererId = new Map<WebRendererId, RootComponentOperation[]>();

    for (const component of components) {
      const operation = this.determinePendingOperation(component);
      if (!operation) {
        continue;
      }

      const rendererId = component.assignedRendererId;
      if (!rendererId) {
        throw new Error('Descriptors must be assigned a renderer ID before getting used as root components');
      }

      let operations = operationsByRendererId.get(rendererId);
      if (!operations) {
        operations = [];
        operationsByRendererId.set(rendererId, operations);
      }

      operations.push(operation);
    }

    for (const [rendererId, operations] of operationsByRendererId) {
      const batch: RootComponentOperationBatch = {
        batchId: this._nextOperationBatchId++,
        operations,
      };
      this._pendingOperationBatches[batch.batchId] = batch;
      const batchJson = JSON.stringify(batch);

      if (rendererId === WebRendererId.Server) {
        updateServerRootComponents(batchJson);
      } else {
        this.updateWebAssemblyRootComponents(batchJson);
      }
    }

    this.circuitMayHaveNoRootComponents();
  }

  private updateWebAssemblyRootComponents(operationsJson: string) {
    if (isFirstUpdate()) {
      resolveInitialUpdate(operationsJson);
    } else {
      updateWebAssemblyRootComponents(operationsJson);
    }
  }

  private resolveRendererIdForDescriptor(descriptor: ComponentDescriptor): WebRendererId | null {
    const resolvedType = descriptor.type === 'auto' ? this.getAutoRenderMode() : descriptor.type;
    switch (resolvedType) {
      case 'server':
        this.startCircutIfNotStarted();
        return WebRendererId.Server;
      case 'webassembly':
        this.startWebAssemblyIfNotStarted();
        return WebRendererId.WebAssembly;
      case null:
        return null;
    }
  }

  private getAutoRenderMode(): 'webassembly' | 'server' | null {
    // If WebAssembly components exist or may exist soon, use WebAssembly.
    if (this.rendererHasExistingOrPendingComponents(WebRendererId.WebAssembly, 'webassembly')) {
      return 'webassembly';
    }

    // If Server components exist or may exist soon, use WebAssembly.
    if (this.rendererHasExistingOrPendingComponents(WebRendererId.Server, 'server')) {
      return 'server';
    }

    // If no interactive components are on the page, we use WebAssembly
    // if the WebAssembly runtime has loaded. Otherwise, we'll wait to activate
    // root components until we determine whether the WebAssembly runtime can be
    // loaded quickly.
    if (hasLoadedWebAssemblyPlatform()) {
      return 'webassembly';
    }

    if (this._didWebAssemblyFailToLoadQuickly) {
      return 'server';
    }

    return null;
  }

  private determinePendingOperation(component: RootComponentInfo): RootComponentOperation | null {
    if (isDescriptorInDocument(component.descriptor)) {
      if (component.assignedRendererId === undefined) {
        // We haven't added this component for interactivity yet.
        if (isPageLoading()) {
          // We don't add new components while the page is loading or while
          // enhanced navigation is underway.
          return null;
        }

        const rendererId = this.resolveRendererIdForDescriptor(component.descriptor);
        if (rendererId === null) {
          // The renderer ID for the component has not been decided yet,
          // probably because the component has an "auto" render mode.
          return null;
        }

        if (!isRendererAttached(rendererId)) {
          // The renderer for this descriptor is not attached, so we'll no-op.
          // After the renderer attaches, we'll handle this descriptor again if
          // it's still in the document.
          return null;
        }

        // .NET may dispose and re-initialize the interactive component as a result of a future 'update' operation.
        // This call prevents the component's content from being deleted from the DOM between the disposal
        // and subsequent re-initialization.
        setShouldPreserveContentOnInteractiveComponentDisposal(component.descriptor.start as unknown as LogicalElement, true);

        component.assignedRendererId = rendererId;
        component.uniqueIdAtLastUpdate = component.descriptor.uniqueId;
        return { type: 'add', ssrComponentId: component.ssrComponentId, marker: descriptorToMarker(component.descriptor) };
      } else {
        if (!isRendererAttached(component.assignedRendererId)) {
          // The renderer for this descriptor is not attached, so we'll no-op.
          // After the renderer attaches, we'll handle this descriptor again if
          // it's still in the document.
          return null;
        }

        // The component has already been added for interactivity.
        if (component.uniqueIdAtLastUpdate === component.descriptor.uniqueId) {
          // The descriptor has not changed since the last update.
          // Nothing to do.
          return null;
        }

        // The descriptor has changed since it was last updated, so we'll update the component's parameters.
        component.uniqueIdAtLastUpdate = component.descriptor.uniqueId;
        return { type: 'update', ssrComponentId: component.ssrComponentId, marker: descriptorToMarker(component.descriptor) };
      }
    } else {
      if (component.hasPendingRemoveOperation) {
        // The component is already being disposed, so there's nothing left to do.
        return null;
      }

      if (component.assignedRendererId === undefined) {
        // The component was removed from the document before it was assigned to a renderer,
        // so we don't have to notify .NET that anything has changed.
        this.unregisterComponent(component);
        return null;
      }

      if (!isRendererAttached(component.assignedRendererId)) {
        // The component was already assigned a renderer, but that renderer is no longer attached.
        // After the renderer attaches, we'll handle the removal of this descriptor again.
        return null;
      }

      // Since the component will be getting completedly diposed from .NET (rather than replaced by another component, which can
      // happen as a result of an 'update' operation), we indicate that its content should no longer be preserved on disposal.
      setShouldPreserveContentOnInteractiveComponentDisposal(component.descriptor.start as unknown as LogicalElement, false);

      // This component was removed from the document and we've assigned a renderer ID,
      // so we'll dispose it in .NET.
      component.hasPendingRemoveOperation = true;
      return { type: 'remove', ssrComponentId: component.ssrComponentId };
    }
  }

  public resolveRootComponent(ssrComponentId: number): ComponentDescriptor {
    const component = this._rootComponentsBySsrComponentId.get(ssrComponentId);
    if (!component) {
      throw new Error(`Could not resolve a root component with SSR component ID '${ssrComponentId}'.`);
    }

    return component.descriptor;
  }

  public onAfterUpdateRootComponents(batchId: number): void {
    const batch = this._pendingOperationBatches[batchId];
    delete this._pendingOperationBatches[batchId];

    for (const operation of batch.operations) {
      switch (operation.type) {
        case 'remove': {
          // We can stop tracking this component now that .NET has acknowedged its removal.
          const component = this._rootComponentsBySsrComponentId.get(operation.ssrComponentId);
          if (component) {
            this.unregisterComponent(component);
          }
          break;
        }
      }
    }
  }
}

function isDescriptorInDocument(descriptor: ComponentDescriptor): boolean {
  return document.contains(descriptor.start);
}

function areWebAssemblyResourcesLikelyCached(config: MonoConfig): boolean {
  if (!config.cacheBootResources) {
    return false;
  }

  const hash = getWebAssemblyResourceHash(config);
  if (!hash) {
    return false;
  }

  const existingHash = window.localStorage.getItem(hash.key);
  return hash.value === existingHash;
}

function cacheWebAssemblyResourceHash(config: MonoConfig) {
  const hash = getWebAssemblyResourceHash(config);
  if (hash) {
    window.localStorage.setItem(hash.key, hash.value);
  }
}

function getWebAssemblyResourceHash(config: MonoConfig): { key: string, value: string } | null {
  const hash = config.resources?.hash;
  const mainAssemblyName = config.mainAssemblyName;
  if (!hash || !mainAssemblyName) {
    return null;
  }

  return {
    key: `blazor-resource-hash:${mainAssemblyName}`,
    value: hash,
  };
}
