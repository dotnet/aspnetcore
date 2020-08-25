import { LogicalElement, toLogicalRootCommentElement } from "../Rendering/LogicalElements";

export class WebAssemblyComponentAttacher {
  public preregisteredComponents: ComponentDescriptor[];

  private componentsById: { [index: number]: ComponentDescriptor };

  public constructor(components: ComponentDescriptor[]) {
    this.preregisteredComponents = components;
    let componentsById = {};
    for (let index = 0; index < components.length; index++) {
      const component = components[index];
      componentsById[component.id] = component;
    }
    this.componentsById = componentsById;
  }

  public resolveRegisteredElement(id: string): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      return toLogicalRootCommentElement(this.componentsById[parsedId].start as Comment, this.componentsById[parsedId].end as Comment);
    } else {
      return undefined;
    }
  }

  public getParameterValues(id: number): string | undefined {
    return this.componentsById[id].parameterValues;
  }

  public getParameterDefinitions(id: number): string | undefined {
    return this.componentsById[id].parameterDefinitions;
  }

  public getTypeName(id: number): string {
    return this.componentsById[id].typeName;
  }

  public getAssembly(id: number): string {
    return this.componentsById[id].assembly;
  }

  public getId(index: number): number {
    return this.preregisteredComponents[index].id;
  }

  public getCount(): number {
    return this.preregisteredComponents.length;
  }
}

interface ComponentMarker {
  type: string;
  id: number;
  typeName: string;
  assembly: string;
  parameterDefinitions?: string;
  parameterValues?: string;
}

export class ComponentDescriptor {
  private static globalId = 1;

  public type: string;

  public typeName: string;

  public assembly: string;

  public parameterDefinitions?: string;

  public parameterValues?: string;

  public id: number;

  public start: Node;

  public end?: Node;

  public constructor(type: string, start: Node, end: Node | undefined, assembly: string, typeName: string, parameterDefinitions?: string, parameterValues?: string) {
    this.id = ComponentDescriptor.globalId++;
    this.type = type;
    this.assembly = assembly;
    this.typeName = typeName;
    this.parameterDefinitions = parameterDefinitions;
    this.parameterValues = parameterValues;
    this.start = start;
    this.end = end;
  }

  public toRecord(): ComponentMarker {
    const result = { type: this.type, id: this.id, assembly: this.assembly, typeName: this.typeName, parameterDefinitions: this.parameterDefinitions, parameterValues: this.parameterValues };
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
      componentComment.assembly,
      componentComment.typeName,
      componentComment.parameterDefinitions,
      componentComment.parameterValues,
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents.sort((a, b): number => a.id - b.id);
}

interface ComponentComment {
  type: 'server' | 'client';
  assembly: string;
  typeName: string;
  parameterDefinitions?: string;
  parameterValues?: string;
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

const blazorCommentRegularExpression = /\W*Blazor:[^{]*(?<descriptor>.*)$/;

function getComponentComment(commentNodeIterator: ComponentCommentIterator): ComponentComment | undefined {
  const candidateStart = commentNodeIterator.currentElement;

  if (!candidateStart || candidateStart.nodeType !== Node.COMMENT_NODE) {
    return;
  }
  if (candidateStart.textContent) {
    const componentStartComment = new RegExp(blazorCommentRegularExpression);
    const definition = componentStartComment.exec(candidateStart.textContent);
    const json = definition && definition.groups && definition.groups['descriptor'];

    if (json) {
      try {
        return createClientComponentComment(json, candidateStart, commentNodeIterator);
      } catch (error) {
        throw new Error(`Found malformed component comment at ${candidateStart.textContent}`);
      }
    } else {
      return;
    }
  }
}

function createClientComponentComment(json: string, start: Node, iterator: ComponentCommentIterator): ComponentComment | undefined {
  const payload = JSON.parse(json) as ComponentComment;
  const { type, assembly, typeName, parameterDefinitions, parameterValues, prerenderId } = payload;
  if (type !== 'server' && type !== 'client') {
    throw new Error(`Invalid component type '${type}'.`);
  }

  if (type === 'server') {
    return undefined;
  }

  if (!assembly) {
    throw new Error('assembly must be defined when using a descriptor.');
  }

  if (!typeName) {
    throw new Error('typeName must be defined when using a descriptor.');
  }

  if (!prerenderId) {
    return {
      type,
      assembly,
      typeName,
      // Parameter definitions and values come Base64 encoded from the server, since they contain random data and can make the
      // comment invalid. We could unencode them in .NET Code, but that would be slower to do and we can leverage the fact that
      // JS provides a native function that will be much faster and that we are doing this work while we are fetching
      // blazor.boot.json
      parameterDefinitions: parameterDefinitions && atob(parameterDefinitions),
      parameterValues: parameterValues && atob(parameterValues),
      start,
    };
  } else {
    const end = getComponentEndComment(prerenderId, iterator);
    if (!end) {
      throw new Error(`Could not find an end component comment for '${start}'`);
    }

    return {
      type,
      assembly,
      typeName,
      // Same comment as above.
      parameterDefinitions: parameterDefinitions && atob(parameterDefinitions),
      parameterValues: parameterValues && atob(parameterValues),
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
