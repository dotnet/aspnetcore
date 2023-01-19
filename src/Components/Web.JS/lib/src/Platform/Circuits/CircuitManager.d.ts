import { LogicalElement } from '../../Rendering/LogicalElements';
import { ServerComponentDescriptor } from '../../Services/ComponentDescriptorDiscovery';
export declare class CircuitDescriptor {
    circuitId?: string;
    components: ServerComponentDescriptor[];
    applicationState: string;
    constructor(components: ServerComponentDescriptor[], appState: string);
    reconnect(reconnection: signalR.HubConnection): Promise<boolean>;
    initialize(circuitId: string): void;
    startCircuit(connection: signalR.HubConnection): Promise<boolean>;
    resolveElement(sequenceOrIdentifier: string): LogicalElement;
}
