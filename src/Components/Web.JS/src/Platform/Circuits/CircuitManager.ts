import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, LogicalElement } from '../../Rendering/LogicalElements';

export class CircuitDescriptor {
  public circuitId?: string;

  public components: ComponentDescriptor[];

  public constructor(components: ComponentDescriptor[]) {
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

interface ComponentMarker {
  type: string;
  sequence: number;
  descriptor: string;
}

export class ComponentDescriptor {
  public type: string;

  public start: Node;

  public end?: Node;

  public sequence: number;

  public descriptor: string;

  public constructor(type: string, start: Node, end: Node | undefined, sequence: number, descriptor: string) {
    this.type = type;
    this.start = start;
    this.end = end;
    this.sequence = sequence;
    this.descriptor = descriptor;
  }

  public toRecord(): ComponentMarker {
    const result = { type: this.type, sequence: this.sequence, descriptor: this.descriptor };
    return result;
  }
}

export function discoverComponents(document: Document): ComponentDescriptor[] {
  const componentComments = resolveComponentComments(document);
  const discoveredComponents: ComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const componentComment = componentComments[i];
    const entry = new ComponentDescriptor(
      componentComment.type,
      componentComment.start,
      componentComment.end,
      componentComment.sequence,
      componentComment.descriptor,
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents.sort((a, b) => a.sequence - b.sequence);
}


interface ComponentComment {
  type: 'server';
  sequence: number;
  descriptor: string;
  start: Node;
  end?: Node;
  prerenderId?: string;
}

function resolveComponentComments(node: Node): ComponentComment[] {
  if (!node.hasChildNodes()) {
    return [];
  }

  const result: ComponentComment[] = [];
  const childNodeIterator = new ComponentCommentIterator(node.childNodes);
  while (childNodeIterator.next() && childNodeIterator.currentElement) {
    const componentComment = getComponentComment(childNodeIterator);
    if (componentComment) {
      result.push(componentComment);
    } else {
      const childResults = resolveComponentComments(childNodeIterator.currentElement);
      for (let j = 0; j < childResults.length; j++) {
        const childResult = childResults[j];
        result.push(childResult);
      }
    }
  }

  return result;
}

const blazorCommentRegularExpression = /\W*Blazor:[^{]*(.*)$/;

function getComponentComment(commentNodeIterator: ComponentCommentIterator): ComponentComment | undefined {
  const candidateStart = commentNodeIterator.currentElement;

  if (!candidateStart || candidateStart.nodeType !== Node.COMMENT_NODE) {
    return;
  }
  if (candidateStart.textContent) {
    const componentStartComment = new RegExp(blazorCommentRegularExpression);
    const definition = componentStartComment.exec(candidateStart.textContent);
    const json = definition && definition[1];

    if (json) {
      try {
        return createComponentComment(json, candidateStart, commentNodeIterator);
      } catch (error) {
        throw new Error(`Found malformed component comment at ${candidateStart.textContent}`);
      }
    } else {
      return;
    }
  }
}

function createComponentComment(json: string, start: Node, iterator: ComponentCommentIterator): ComponentComment {
  const payload = JSON.parse(json) as ComponentComment;
  const { type, sequence, descriptor, prerenderId } = payload;
  if (type !== 'server') {
    throw new Error(`Invalid component type '${type}'.`);
  }

  if (!descriptor) {
    throw new Error('descriptor must be defined when using a descriptor.');
  }

  if (sequence === undefined) {
    throw new Error('sequence must be defined when using a descriptor.');
  }

  if (!Number.isInteger(sequence)) {
    throw new Error(`Error parsing the sequence '${sequence}' for component '${json}'`);
  }

  if (!prerenderId) {
    return {
      type,
      sequence: sequence,
      descriptor,
      start,
    };
  } else {
    const end = getComponentEndComment(prerenderId, iterator);
    if (!end) {
      throw new Error(`Could not find an end component comment for '${start}'`);
    }

    return {
      type,
      sequence,
      descriptor,
      start,
      prerenderId,
      end,
    };
  }
}

function getComponentEndComment(prerenderedId: string, iterator: ComponentCommentIterator): ChildNode | undefined {
  while (iterator.next() && iterator.currentElement) {
    const node = iterator.currentElement;
    if (node.nodeType !== Node.COMMENT_NODE) {
      continue;
    }
    if (!node.textContent) {
      continue;
    }

    const definition = new RegExp(blazorCommentRegularExpression).exec(node.textContent);
    const json = definition && definition[1];
    if (!json) {
      continue;
    }

    validateEndComponentPayload(json, prerenderedId);

    return node;
  }

  return undefined;
}

function validateEndComponentPayload(json: string, prerenderedId: string): void {
  const payload = JSON.parse(json) as ComponentComment;
  if (Object.keys(payload).length !== 1) {
    throw new Error(`Invalid end of component comment: '${json}'`);
  }
  const prerenderedEndId = payload.prerenderId;
  if (!prerenderedEndId) {
    throw new Error(`End of component comment must have a value for the prerendered property: '${json}'`);
  }
  if (prerenderedEndId !== prerenderedId) {
    throw new Error(`End of component comment prerendered property must match the start comment prerender id: '${prerenderedId}', '${prerenderedEndId}'`);
  }
}

class ComponentCommentIterator {

  private childNodes: NodeListOf<ChildNode>;

  private currentIndex: number;

  private length: number;

  public currentElement: ChildNode | undefined;

  public constructor(childNodes: NodeListOf<ChildNode>) {
    this.childNodes = childNodes;
    this.currentIndex = -1;
    this.length = childNodes.length;
  }

  public next(): boolean {
    this.currentIndex++;
    if (this.currentIndex < this.length) {
      this.currentElement = this.childNodes[this.currentIndex];
      return true;
    } else {
      this.currentElement = undefined;
      return false;
    }
  }
}


