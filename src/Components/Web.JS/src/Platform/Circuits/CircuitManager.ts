import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, LogicalElement } from '../../Rendering/LogicalElements';
import { ServerComponentDescriptor } from '../../Services/ComponentDescriptorDiscovery';

export class CircuitDescriptor {
  public circuitId?: string;

  public components: ServerComponentDescriptor[];

  public constructor(components: ServerComponentDescriptor[]) {
    this.circuitId = undefined;
    this.components = components;
  }

  public reconnect(reconnection: signalR.HubConnection): Promise<boolean> {
    if (!this.circuitId) {
      throw new Error('Circuit host not initialized.');
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

    const result = await connection.invoke<string>(
      'StartCircuit',
      navigationManagerFunctions.getBaseURI(),
      navigationManagerFunctions.getLocationHref(),
      JSON.stringify(this.components.map(c => c.toRecord()))
    );

    if (result) {
      this.initialize(result);
      return true;
    } else {
      return false;
    }
  }

  public resolveElement(sequence: string): LogicalElement {
    const parsedSequence = Number.parseInt(sequence);
    if (!Number.isNaN(parsedSequence)) {
      return toLogicalRootCommentElement(this.components[parsedSequence].start as Comment, this.components[parsedSequence].end as Comment);
    } else {
      throw new Error(`Invalid sequence number '${sequence}'.`);
    }
  }
}

