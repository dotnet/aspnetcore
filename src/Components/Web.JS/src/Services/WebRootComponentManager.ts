// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor, ComponentMarker, descriptorToMarker } from './ComponentDescriptorDiscovery';
import { isRendererAttached, registerRendererAttachedListener, updateRootComponents } from '../Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../Rendering/WebRendererId';
import { DescriptorHandler } from '../Rendering/DomMerging/DomSync';
import { disposeCircuit, hasStartedServer, isCircuitAvailable, startCircuit, startServer } from '../Boot.Server.Common';
import { hasLoadedWebAssemblyPlatform, hasStartedLoadingWebAssemblyPlatform, hasStartedWebAssembly, loadWebAssemblyPlatformIfNotStarted, startWebAssembly, waitForBootConfigLoaded } from '../Boot.WebAssembly.Common';
import { MonoConfig } from 'dotnet';
import { RootComponentManager } from './RootComponentManager';
import { Blazor } from '../GlobalExports';
import { getRendererer } from '../Rendering/Renderer';
import { isPageLoading } from './NavigationEnhancement';

type RootComponentOperation = RootComponentAddOperation | RootComponentUpdateOperation | RootComponentRemoveOperation;

type RootComponentAddOperation = {
  type: 'add';
  selectorId: number;
  marker: ComponentMarker;
};

type RootComponentUpdateOperation = {
  type: 'update';
  componentId: number;
  marker: ComponentMarker;
};

type RootComponentRemoveOperation = {
  type: 'remove';
  componentId: number;
};

type RootComponentInfo = {
  descriptor: ComponentDescriptor;
  assignedRendererId?: WebRendererId;
  uniqueIdAtLastUpdate?: number;
  interactiveComponentId?: number;
}

export class WebRootComponentManager implements DescriptorHandler, RootComponentManager<never> {
  private readonly _rootComponents = new Set<RootComponentInfo>();

  private readonly _descriptors = new Set<ComponentDescriptor>();

  private readonly _pendingComponentsToResolve = new Map<number, RootComponentInfo>();

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
    this.rootComponentsMayRequireRefresh();
  }

  public registerComponent(descriptor: ComponentDescriptor) {
    if (this._descriptors.has(descriptor)) {
      return;
    }

    if (descriptor.type === 'auto' || descriptor.type === 'webassembly') {
      // Eagerly start loading the WebAssembly runtime, even though we're not
      // activating the component yet. This is becuase WebAssembly resources
      // may take a long time to load, so starting to load them now potentially reduces
      // the time to interactvity.
      this.startLoadingWebAssemblyIfNotStarted();
    }

    this._descriptors.add(descriptor);
    this._rootComponents.add({ descriptor });
  }

  private unregisterComponent(component: RootComponentInfo) {
    this._descriptors.delete(component.descriptor);
    this._rootComponents.delete(component);
  }

  private async startLoadingWebAssemblyIfNotStarted() {
    if (hasStartedLoadingWebAssemblyPlatform()) {
      return;
    }

    const loadWebAssemblyPromise = loadWebAssemblyPlatformIfNotStarted();

    // If WebAssembly resources can't be loaded within some time limit,
    // we take note of this fact so that "auto" components fall back
    // to using Blazor Server.
    setTimeout(() => {
      if (!hasLoadedWebAssemblyPlatform()) {
        this.onWebAssemblyFailedToLoadQuickly();
      }
    }, Blazor._internal.loadWebAssemblyQuicklyTimeout);

    const bootConfig = await waitForBootConfigLoaded();

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
      this.refreshRootComponents(this._rootComponents);
    }, 0);
  }

  private circuitMayHaveNoRootComponents() {
    const isCircuitInUse = this.hasAnyExistingOrPendingServerComponents();
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
      if (!this.hasAnyExistingOrPendingServerComponents()) {
        disposeCircuit();
        this._circuitInactivityTimeoutId = undefined;
      }
    }, this._circuitInactivityTimeoutMs) as unknown as number;
  }

  private hasAnyExistingOrPendingServerComponents(): boolean {
    // If there are active Blazor Server components on the page, we shouldn't dispose the circuit.
    const renderer = getRendererer(WebRendererId.Server);
    if (renderer && renderer.getRootComponentCount() > 0) {
      return true;
    }

    // If we have SSR components that may become Blazor Server components in the future,
    // we shouldn't dispose the circuit.
    for (const { descriptor: { type }, assignedRendererId } of this._rootComponents) {
      if (assignedRendererId === WebRendererId.Server) {
        // The component has been assigned to use Blazor Server.
        return true;
      }

      if (assignedRendererId === undefined && (type === 'auto' || type === 'server')) {
        // The component has not been assigned a renderer yet, so it's possible it might
        // use Blazor Server.
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
      const operationsJson = JSON.stringify(operations);
      updateRootComponents(rendererId, operationsJson);
    }

    this.circuitMayHaveNoRootComponents();
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
    // If the WebAssembly runtime has loaded, we will always use WebAssembly
    // for auto components. Otherwise, we'll wait to activate root components
    // until we determine whether the WebAssembly runtime can be loaded quickly.
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

        component.assignedRendererId = rendererId;
        component.uniqueIdAtLastUpdate = component.descriptor.uniqueId;
        this._pendingComponentsToResolve.set(component.descriptor.uniqueId, component);
        return { type: 'add', selectorId: component.descriptor.uniqueId, marker: descriptorToMarker(component.descriptor) };
      }

      if (component.uniqueIdAtLastUpdate === component.descriptor.uniqueId) {
        // The descriptor has not changed since the last update.
        // Nothing to do.
        return null;
      }

      if (component.interactiveComponentId !== undefined) {
        // The component has become interactive, so we'll update its parameters.
        component.uniqueIdAtLastUpdate = component.descriptor.uniqueId;
        return { type: 'update', componentId: component.interactiveComponentId, marker: descriptorToMarker(component.descriptor) };
      }

      // We have started to add the component, but it has not become interactive yet.
      // We'll wait until we have a component ID to work with before sending parameter
      // updates.
    } else {
      this.unregisterComponent(component);

      if (component.interactiveComponentId !== undefined) {
        // We have an interactive component for this marker, so we'll remove it.
        return { type: 'remove', componentId: component.interactiveComponentId };
      }

      // If we make it here, that means we either:
      // 1. Haven't started to make the component interactive, in which case we have no further action to take.
      // 2. Have started to make the component interactive, but it hasn't become interactive yet. In this case,
      //    we'll wait to remove the component until after we have a component ID to provide.
    }

    return null;
  }

  public resolveRootComponent(selectorId: number, componentId: number): ComponentDescriptor {
    const component = this._pendingComponentsToResolve.get(selectorId);
    if (!component) {
      throw new Error(`Could not resolve a root component for descriptor with ID '${selectorId}'.`);
    }

    this._pendingComponentsToResolve.delete(selectorId);

    if (component.interactiveComponentId !== undefined) {
      throw new Error('Cannot resolve a root component for the same descriptor multiple times.');
    }

    component.interactiveComponentId = componentId;

    // The descriptor may have changed since the last call to handleUpdatedRootComponentsCore().
    // We'll update this single descriptor so that the component receives the most up-to-date parameters
    // or gets removed if it no longer exists on the page.
    this.refreshRootComponents([component]);

    return component.descriptor;
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
