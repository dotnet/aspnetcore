import { internalFunctions as uriHelperFunctions } from '../../Services/UriHelper';

export class CircuitDescriptor {
  public circuitId: string;

  public constructor(circuitId: string) {
    this.circuitId = circuitId;
  }

  public reconnect(reconnection: signalR.HubConnection): Promise<boolean> {
    return reconnection.invoke<boolean>('ConnectCircuit', this.circuitId);
  }
}

export async function startCircuit(connection: signalR.HubConnection): Promise<CircuitDescriptor> {
  const result = await connection.invoke<string>('StartCircuit', uriHelperFunctions.getLocationHref(), uriHelperFunctions.getBaseURI());
  if (result) {
    return new CircuitDescriptor(result);
  } else {
    throw new Error('Circuit failed to start');
  }
}
