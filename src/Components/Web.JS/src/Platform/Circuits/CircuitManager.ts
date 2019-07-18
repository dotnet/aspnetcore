import { internalFunctions as uriHelperFunctions } from '../../Services/UriHelper';
import { ComponentDescriptor, MarkupRegistrationTags, StartComponentComment, EndComponentComment } from './ComponentDescriptor';

export class CircuitDescriptor {
  public circuitId: string;

  public components: ComponentDescriptor[];

  public constructor(circuitId: string, components: ComponentDescriptor[]) {
    this.circuitId = circuitId;
    this.components = components;
  }

  public reconnect(reconnection: signalR.HubConnection): Promise<boolean> {
    return reconnection.invoke<boolean>('ConnectCircuit', this.circuitId);
  }
}


export function discoverPrerenderedCircuits(document: Document): CircuitDescriptor[] {
  const commentPairs = resolveCommentPairs(document);
  const discoveredCircuits = new Map<string, ComponentDescriptor[]>();
  for (let i = 0; i < commentPairs.length; i++) {
    const pair = commentPairs[i];
    // We replace '--' on the server with '..' when we prerender due to the fact that this
    // is not allowed in HTML comments and doesn't get encoded by default.
    const circuitId = pair.start.circuitId.replace('..', '--');
    let circuit = discoveredCircuits.get(circuitId);
    if (!circuit) {
      circuit = [];
      discoveredCircuits.set(circuitId, circuit);
    }
    const entry = new ComponentDescriptor(pair.start.componentId, circuitId, pair.start.rendererId, pair);
    circuit.push(entry);
  }
  const circuits: CircuitDescriptor[] = [];
  for (const [key, values] of discoveredCircuits) {
    circuits.push(new CircuitDescriptor(key, values));
  }
  return circuits;
}

export async function startCircuit(connection: signalR.HubConnection): Promise<CircuitDescriptor | undefined> {
  const result = await connection.invoke<string>('StartCircuit', uriHelperFunctions.getLocationHref(), uriHelperFunctions.getBaseURI());
  if (result) {
    return new CircuitDescriptor(result, []);
  } else {
    return undefined;
  }
}

function resolveCommentPairs(node: Node): MarkupRegistrationTags[] {
  if (!node.hasChildNodes()) {
    return [];
  }
  const result: MarkupRegistrationTags[] = [];
  const children = node.childNodes;
  let i = 0;
  const childrenLength = children.length;
  while (i < childrenLength) {
    const currentChildNode = children[i];
    const startComponent = getComponentStartComment(currentChildNode);
    if (!startComponent) {
      i++;
      const childResults = resolveCommentPairs(currentChildNode);
      for (let j = 0; j < childResults.length; j++) {
        const childResult = childResults[j];
        result.push(childResult);
      }
      continue;
    }
    const endComponent = getComponentEndComment(startComponent, children, i + 1, childrenLength);
    result.push({ start: startComponent, end: endComponent });
    i = endComponent.index + 1;
  }
  return result;
}
function getComponentStartComment(node: Node): StartComponentComment | undefined {
  if (node.nodeType !== Node.COMMENT_NODE) {
    return;
  }
  if (node.textContent) {
    const componentStartComment = /\W+M.A.C.Component:[^{]*(.*)$/;
    const definition = componentStartComment.exec(node.textContent);
    const json = definition && definition[1];
    if (json) {
      try {
        const { componentId, rendererId, circuitId } = JSON.parse(json);
        const allComponents = componentId !== undefined && rendererId !== undefined && !!circuitId;
        if (allComponents) {
          return {
            node: node as Comment,
            circuitId,
            rendererId: rendererId,
            componentId: componentId,
          };
        }
      } catch (error) {
      }
      throw new Error(`Found malformed start component comment at ${node.textContent}`);
    }
  }
}
function getComponentEndComment(component: StartComponentComment, children: NodeList, index: number, end: number): EndComponentComment {
  for (let i = index; i < end; i++) {
    const node = children[i];
    if (node.nodeType !== Node.COMMENT_NODE) {
      continue;
    }
    if (!node.textContent) {
      continue;
    }
    const componentEndComment = /\W+M.A.C.Component:\W+(\d+)\W+$/;
    const definition = componentEndComment.exec(node.textContent);
    const json = definition && definition[1];
    if (!json) {
      continue;
    }
    try {
      // The value is expected to be a JSON encoded number
      const componentId = JSON.parse(json);
      if (componentId === component.componentId) {
        return { componentId, node: node as Comment, index: i };
      }
    } catch (error) {
    }
    throw new Error(`Found malformed end component comment at ${node.textContent}`);
  }
  throw new Error(`End component comment not found for ${component.node}`);
}
