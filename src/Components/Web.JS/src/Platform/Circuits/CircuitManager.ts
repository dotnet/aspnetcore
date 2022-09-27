// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, LogicalElement, toLogicalElement } from '../../Rendering/LogicalElements';
import { ServerComponentDescriptor } from '../../Services/ComponentDescriptorDiscovery';
import { HubConnectionState } from '@microsoft/signalr';
import { getAndRemovePendingRootComponentContainer } from '../../Rendering/JSRootComponents';

export class CircuitDescriptor {
  public circuitId?: string;

  public components: ServerComponentDescriptor[];

  public applicationState: string;

  public constructor(components: ServerComponentDescriptor[], appState: string) {
    this.circuitId = undefined;
    this.components = components;
    this.applicationState = appState;
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

  public initialize(circuitId: string): void {
    if (this.circuitId) {
      throw new Error(`Circuit host '${this.circuitId}' already initialized.`);
    }

    this.circuitId = circuitId;
  }

  public async startCircuit(connection: signalR.HubConnection): Promise<boolean> {
    if (connection.state !== HubConnectionState.Connected) {
      return false;
    }
    const result = await connection.invoke<string>(
      'StartCircuit',
      navigationManagerFunctions.getBaseURI(),
      navigationManagerFunctions.getLocationHref(),
      JSON.stringify(this.components.map(c => c.toRecord())),
      this.applicationState || ''
    );

    if (result) {
      this.initialize(result);
      return true;
    } else {
      return false;
    }
  }

  public resolveElement(sequenceOrIdentifier: string): LogicalElement {
    // It may be a root component added by JS
    const jsAddedComponentContainer = getAndRemovePendingRootComponentContainer(sequenceOrIdentifier);
    if (jsAddedComponentContainer) {
      return toLogicalElement(jsAddedComponentContainer, true);
    }

    // ... or it may be a root component added by .NET
    const parsedSequence = Number.parseInt(sequenceOrIdentifier);
    if (!Number.isNaN(parsedSequence)) {
      return toLogicalRootCommentElement(this.components[parsedSequence].start as Comment, this.components[parsedSequence].end as Comment);
    }

    throw new Error(`Invalid sequence number or identifier '${sequenceOrIdentifier}'.`);
  }
}
