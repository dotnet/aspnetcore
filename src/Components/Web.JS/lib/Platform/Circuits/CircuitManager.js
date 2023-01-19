// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, toLogicalElement } from '../../Rendering/LogicalElements';
import { HubConnectionState } from '@microsoft/signalr';
import { getAndRemovePendingRootComponentContainer } from '../../Rendering/JSRootComponents';
export class CircuitDescriptor {
    constructor(components, appState) {
        this.circuitId = undefined;
        this.components = components;
        this.applicationState = appState;
    }
    reconnect(reconnection) {
        if (!this.circuitId) {
            throw new Error('Circuit host not initialized.');
        }
        if (reconnection.state !== HubConnectionState.Connected) {
            return Promise.resolve(false);
        }
        return reconnection.invoke('ConnectCircuit', this.circuitId);
    }
    initialize(circuitId) {
        if (this.circuitId) {
            throw new Error(`Circuit host '${this.circuitId}' already initialized.`);
        }
        this.circuitId = circuitId;
    }
    async startCircuit(connection) {
        if (connection.state !== HubConnectionState.Connected) {
            return false;
        }
        const result = await connection.invoke('StartCircuit', navigationManagerFunctions.getBaseURI(), navigationManagerFunctions.getLocationHref(), JSON.stringify(this.components.map(c => c.toRecord())), this.applicationState || '');
        if (result) {
            this.initialize(result);
            return true;
        }
        else {
            return false;
        }
    }
    resolveElement(sequenceOrIdentifier) {
        // It may be a root component added by JS
        const jsAddedComponentContainer = getAndRemovePendingRootComponentContainer(sequenceOrIdentifier);
        if (jsAddedComponentContainer) {
            return toLogicalElement(jsAddedComponentContainer, true);
        }
        // ... or it may be a root component added by .NET
        const parsedSequence = Number.parseInt(sequenceOrIdentifier);
        if (!Number.isNaN(parsedSequence)) {
            return toLogicalRootCommentElement(this.components[parsedSequence].start, this.components[parsedSequence].end);
        }
        throw new Error(`Invalid sequence number or identifier '${sequenceOrIdentifier}'.`);
    }
}
//# sourceMappingURL=CircuitManager.js.map