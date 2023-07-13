// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, LogicalElement, toLogicalElement } from '../../Rendering/LogicalElements';
import { ServerComponentDescriptor } from '../../Services/ComponentDescriptorDiscovery';
import { HubConnectionState } from '@microsoft/signalr';
import { getAndRemovePendingRootComponentContainer } from '../../Rendering/JSRootComponents';
import { removeRootComponentAsync } from '../../Rendering/WebRendererInteropMethods';
import { RendererId } from '../../Rendering/RendererId';

export class CircuitDescriptor {
  public circuitId?: string;

  private _descriptorsByLastSequence: { [sequence: number]: ServerComponentDescriptor } = {};

  private _lastSequencesByDescriptor = new Map<ServerComponentDescriptor, number>();

  private _activeDescriptors = new Set<ServerComponentDescriptor>();

  private _connection?: signalR.HubConnection;

  public constructor() {
    this.circuitId = undefined;
  }

  public reconnect(reconnection: signalR.HubConnection): Promise<boolean> {
    if (!this.circuitId) {
      throw new Error('Circuit host not initialized.');
    }

    if (reconnection.state !== HubConnectionState.Connected) {
      return Promise.resolve(false);
    }
    return reconnection.invoke<boolean>('ConnectCircuit', this.circuitId);
  }

  public initialize(connection: signalR.HubConnection, circuitId: string): void {
    if (this.circuitId) {
      throw new Error(`Circuit host '${this.circuitId}' already initialized.`);
    }

    this.circuitId = circuitId;
    this._connection = connection;
  }

  public registerDescriptor(descriptor: ServerComponentDescriptor) {
    this._activeDescriptors.add(descriptor);
  }

  public handleUpdatedDescriptors() {
    const newDescriptors: ServerComponentDescriptor[] = [];
    const updatedInteractiveDescriptors: ServerComponentDescriptor[] = [];

    for (const descriptor of this._activeDescriptors) {
      if (document.contains(descriptor.start)) {
        this.processUpdatedDescriptor(descriptor, newDescriptors, updatedInteractiveDescriptors);
      }
    }

    if (newDescriptors.length) {
      this.addRootComponents(newDescriptors);
    }

    if (updatedInteractiveDescriptors.length) {
      this.updateRootComponents(updatedInteractiveDescriptors);
    }
  }

  public handleRemovedDescriptors() {
    const removedInteractiveDescriptors: ServerComponentDescriptor[] = [];

    for (const descriptor of this._activeDescriptors) {
      if (!document.contains(descriptor.start)) {
        this.processRemovedDescriptor(descriptor, removedInteractiveDescriptors);
      }
    }

    if (removedInteractiveDescriptors.length) {
      this.removeRootComponents(removedInteractiveDescriptors);
    }
  }

  private processUpdatedDescriptor(
    descriptor: ServerComponentDescriptor,
    newDescriptors: ServerComponentDescriptor[],
    updatedInteractiveDescriptors: ServerComponentDescriptor[]
  ) {
    const lastSequence = this._lastSequencesByDescriptor.get(descriptor);
    if (lastSequence === undefined) {
      // This is the first time we're processing a descriptor.
      newDescriptors.push(descriptor);
      return;
    }

    if (descriptor.interactiveComponentId === undefined) {
      // We've seen the descriptor before, but no associated interactive component
      // has been created yet.
      // We'll update the interactive component after it gets attached.
      return;
    }

    if (lastSequence === descriptor.sequence) {
      // The sequence has not changed since the last update, so there's no reason
      // to update the root component again.
      return;
    }

    updatedInteractiveDescriptors.push(descriptor);
  }

  private processRemovedDescriptor(descriptor: ServerComponentDescriptor, removedInteractiveDescriptors: ServerComponentDescriptor[]) {
    this._activeDescriptors.delete(descriptor);

    const lastSequence = this._lastSequencesByDescriptor.get(descriptor);
    if (lastSequence === undefined) {
      // We haven't yet attempted to create an interactive component from this descriptor, so
      // we can safely avoid further action.
      return;
    }

    if (descriptor.interactiveComponentId === undefined) {
      // No interactive component has been attached, so there's nothing to do at this time.
      return;
    }

    removedInteractiveDescriptors.push(descriptor);
  }

  public async startCircuit(connection: signalR.HubConnection, appState: string, initialDescriptors: ServerComponentDescriptor[]): Promise<boolean> {
    if (connection.state !== HubConnectionState.Connected) {
      return false;
    }

    for (const descriptor of initialDescriptors) {
      this.registerDescriptor(descriptor);
    }

    const descriptorsJson = this.serializeDescriptorsForDotNet(initialDescriptors);
    const result = await connection.invoke<string>(
      'StartCircuit',
      navigationManagerFunctions.getBaseURI(),
      navigationManagerFunctions.getLocationHref(),
      descriptorsJson,
      appState
    );

    if (result) {
      this.initialize(connection, result);
      return true;
    } else {
      return false;
    }
  }

  private async addRootComponents(descriptors: ServerComponentDescriptor[]): Promise<void> {
    if (this._connection?.state !== HubConnectionState.Connected) {
      return;
    }

    const descriptorsJson = this.serializeDescriptorsForDotNet(descriptors);
    await this._connection.send('AddRootComponents', descriptorsJson);
  }

  private async updateRootComponents(descriptors: ServerComponentDescriptor[]): Promise<void> {
    if (this._connection?.state !== HubConnectionState.Connected) {
      return;
    }

    const descriptorsJson = this.serializeDescriptorsForDotNet(descriptors);
    await this._connection.send('UpdateRootComponents', descriptorsJson);
  }

  private serializeDescriptorsForDotNet(descriptors: ServerComponentDescriptor[]): string {
    descriptors.sort();

    for (const descriptor of descriptors) {
      this._descriptorsByLastSequence[descriptor.sequence] = descriptor;
      this._lastSequencesByDescriptor.set(descriptor, descriptor.sequence);
    }

    return JSON.stringify(descriptors.map(c => c.toRecord()));
  }

  private removeRootComponents(components: ServerComponentDescriptor[]) {
    if (this._connection?.state !== HubConnectionState.Connected) {
      return;
    }

    // TODO: Consider making the way components are dynamically added, updated, and removed
    // more consistent.
    for (const component of components) {
      if (component.interactiveComponentId !== undefined) {
        removeRootComponentAsync(RendererId.Server, component.interactiveComponentId);
      }
    }
  }

  public resolveElement(sequenceOrIdentifier: string, componentId: number): LogicalElement {
    // It may be a root component added by JS
    const jsAddedComponentContainer = getAndRemovePendingRootComponentContainer(sequenceOrIdentifier);
    if (jsAddedComponentContainer) {
      return toLogicalElement(jsAddedComponentContainer, true);
    }

    // ... or it may be a root component added by .NET
    const parsedSeqeunce = Number.parseInt(sequenceOrIdentifier);
    if (Number.isNaN(parsedSeqeunce)) {
      throw new Error(`Invalid sequence number or identifier '${sequenceOrIdentifier}'.`);
    }

    const descriptor = this._descriptorsByLastSequence[parsedSeqeunce];
    if (!descriptor) {
      throw new Error(`No pending descriptor with sequence '${parsedSeqeunce}' was found.`);
    }

    if (descriptor.interactiveComponentId) {
      throw new Error('Attempted to attach multiple comonents to the same descriptor.');
    }

    descriptor.interactiveComponentId = componentId;

    if (!document.contains(descriptor.start)) {
      // The descriptor has been removed. Remove the interactive component.
      this.removeRootComponents([descriptor]);
      descriptor.interactiveComponentId = undefined;
    } else if (parsedSeqeunce !== descriptor.sequence) {
      // The sequence has updated, indicating the descriptor has updated.
      this.updateRootComponents([descriptor]);
    }

    return toLogicalRootCommentElement(descriptor);
  }
}
