// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function discoverComponents(document: Document, type: 'webassembly' | 'server' | 'auto'): ServerComponentDescriptor[] | WebAssemblyComponentDescriptor[] | AutoComponentDescriptor[] {
  switch (type){
    case 'webassembly':
      return discoverWebAssemblyComponents(document);
    case 'server':
      return discoverServerComponents(document);
    case 'auto':
      return discoverAutoComponents(document);
  }
}

function discoverAutoComponents(document: Document): AutoComponentDescriptor[] {
  const componentComments = resolveComponentComments(document, 'auto') as AutoComponentComment[];
  const discoveredComponents: AutoComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const s = componentComments[i].server;
    const w = componentComments[i].webAssembly;
    discoveredComponents.push({
      type: 'auto',
      serverDescriptor: new ServerComponentDescriptor(s.type, s.start, s.end, s.sequence, s.descriptor),
      webAssemblyDescriptor: new WebAssemblyComponentDescriptor(w.type, w.start, w.end, w.assembly, w.typeName, w.parameterDefinitions, w.parameterValues),
    });
  }

  return discoveredComponents.sort((a, b): number => a.serverDescriptor.sequence - b.serverDescriptor.sequence);
}

function discoverServerComponents(document: Document): ServerComponentDescriptor[] {
  const componentComments = resolveComponentComments(document, 'server') as ServerComponentComment[];
  const discoveredComponents: ServerComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const componentComment = componentComments[i];
    const entry = new ServerComponentDescriptor(
      componentComment.type,
      componentComment.start,
      componentComment.end,
      componentComment.sequence,
      componentComment.descriptor,
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents.sort((a, b): number => a.sequence - b.sequence);
}

const serverStateCommentRegularExpression = /^\s*Blazor-Component-State-Server:(?<state>[a-zA-Z0-9+/=]+)$/;
const webassemblyStateCommentRegularExpression = /^\s*Blazor-Component-State-WebAssembly:(?<state>[a-zA-Z0-9+/=]+)$/;

export function discoverPersistedState(node: Node, stateType: 'server' | 'webassembly'): string | null | undefined {
  if (node.nodeType === Node.COMMENT_NODE) {
    const content = node.textContent || '';

    const matchingRegex = stateType === 'server'
      ? serverStateCommentRegularExpression
      : webassemblyStateCommentRegularExpression;

    const parsedState = matchingRegex.exec(content);
    const value = parsedState && parsedState.groups && parsedState.groups['state'];
    if (value){
      node.parentNode?.removeChild(node);
    }
    return value;
  }

  if (!node.hasChildNodes()) {
    return;
  }

  const nodes = node.childNodes;
  for (let index = 0; index < nodes.length; index++) {
    const candidate = nodes[index];
    const result = discoverPersistedState(candidate, stateType);
    if (result){
      return result;
    }
  }

  return;
}

function discoverWebAssemblyComponents(document: Document): WebAssemblyComponentDescriptor[] {
  const componentComments = resolveComponentComments(document, 'webassembly') as WebAssemblyComponentDescriptor[];
  const discoveredComponents: WebAssemblyComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const componentComment = componentComments[i];
    const entry = new WebAssemblyComponentDescriptor(
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

type ComponentComment = ServerComponentComment | WebAssemblyComponentComment | AutoComponentComment;

interface ServerComponentComment {
  type: 'server';
  sequence: number;
  descriptor: string;
  start: Node;
  end?: Node;
  prerenderId?: string;
}

interface WebAssemblyComponentComment {
  type: 'webassembly';
  typeName: string;
  assembly: string;
  parameterDefinitions?: string;
  parameterValues?: string;
  prerenderId?: string;
  start: Node;
  end?: Node;
}

interface AutoComponentComment {
  type: 'auto';
  server: ServerComponentComment;
  webAssembly: WebAssemblyComponentComment;
}

function resolveComponentComments(node: Node, type: 'webassembly' | 'server' | 'auto'): ComponentComment[] {
  if (!node.hasChildNodes()) {
    return [];
  }

  const result: ComponentComment[] = [];
  const childNodeIterator = new ComponentCommentIterator(node.childNodes);
  while (childNodeIterator.next() && childNodeIterator.currentElement) {
    const componentComment = getComponentComment(childNodeIterator, type);
    if (componentComment) {
      result.push(componentComment);
    } else {
      const childResults = resolveComponentComments(childNodeIterator.currentElement, type);
      for (let j = 0; j < childResults.length; j++) {
        const childResult = childResults[j];
        result.push(childResult);
      }
    }
  }

  return result;
}

const blazorCommentRegularExpression = new RegExp(/^\s*Blazor:[^{]*(?<descriptor>.*)$/);

function getComponentComment(commentNodeIterator: ComponentCommentIterator, type: 'webassembly' | 'server' | 'auto'): ComponentComment | undefined {
  const candidateStart = commentNodeIterator.currentElement;

  if (!candidateStart || candidateStart.nodeType !== Node.COMMENT_NODE) {
    return;
  }
  if (candidateStart.textContent) {
    const definition = blazorCommentRegularExpression.exec(candidateStart.textContent);
    const json = definition && definition.groups && definition.groups['descriptor'];

    if (json) {
      try {
        const componentComment = parseCommentPayload(json);
        switch (type) {
          case 'webassembly':
            return createWebAssemblyComponentComment(componentComment, candidateStart, commentNodeIterator);
          case 'server':
            return createServerComponentComment(componentComment, candidateStart, commentNodeIterator);
          case 'auto':
            return createAutoComponentComment(componentComment, candidateStart, commentNodeIterator);
        }
      } catch (error) {
        throw new Error(`Found malformed component comment at ${candidateStart.textContent}`);
      }
    } else {
      return;
    }
  }
}

function parseCommentPayload(json: string): ComponentComment {
  const payload = JSON.parse(json) as ComponentComment;
  const { type } = payload;
  if (type !== 'server' && type !== 'webassembly' && type !== 'auto') {
    throw new Error(`Invalid component type '${type}'.`);
  }

  return payload;
}

function createAutoComponentComment(payload: ComponentComment, start: Node, iterator: ComponentCommentIterator): AutoComponentComment | undefined {
  const prerenderId = payload.type === 'auto' ? payload.server.prerenderId : payload.prerenderId;

  // Regardless of whether this comment matches the type we're looking for (i.e., 'auto'), we still need to move the iterator
  // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
  // by the end marker.
  const end = prerenderId ? getComponentEndComment('server', prerenderId, iterator) : undefined;
  if (prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'`);
  }

  if (payload.type !== 'auto') {
    return undefined;
  }

  return {
    type: payload.type,
    server: { ...payload.server, start, end },
    webAssembly: {
      ...payload.webAssembly,
      start,
      end,
      // See comments in createWebAssemblyComponentComment
      parameterDefinitions: payload.webAssembly.parameterDefinitions && atob(payload.webAssembly.parameterDefinitions),
      parameterValues: payload.webAssembly.parameterValues && atob(payload.webAssembly.parameterValues),
    },
  };
}

function createServerComponentComment(payload: ComponentComment, start: Node, iterator: ComponentCommentIterator): ServerComponentComment | undefined {
  const { type, descriptor, sequence, prerenderId } = payload.type === 'auto'
    ? payload.server
    : payload as ServerComponentComment;

  // Regardless of whether this comment matches the type we're looking for (i.e., 'server'), we still need to move the iterator
  // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
  // by the end marker.
  const end = prerenderId ? getComponentEndComment('server', prerenderId, iterator) : undefined;
  if (prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'`);
  }

  if (payload.type !== 'server') {
    return undefined;
  }

  if (!descriptor) {
    throw new Error('descriptor must be defined when using a descriptor.');
  }

  if (sequence === undefined) {
    throw new Error('sequence must be defined when using a descriptor.');
  }

  if (!Number.isInteger(sequence)) {
    throw new Error(`Error parsing the sequence '${sequence}' for component '${JSON.stringify(payload)}'`);
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

function createWebAssemblyComponentComment(payload: ComponentComment, start: Node, iterator: ComponentCommentIterator): WebAssemblyComponentComment | undefined {
  const { type, assembly, typeName, parameterDefinitions, parameterValues, prerenderId } = payload.type === 'auto'
    ? payload.webAssembly
    : payload as WebAssemblyComponentComment;

  // Regardless of whether this comment matches the type we're looking for (i.e., 'webassembly'), we still need to move the iterator
  // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
  // by the end marker.
  const end = prerenderId ? getComponentEndComment('webassembly', prerenderId, iterator) : undefined;
  if (prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'`);
  }

  if (payload.type !== 'webassembly') {
    return undefined;
  }

  if (!assembly) {
    throw new Error('assembly must be defined when using a descriptor.');
  }

  if (!typeName) {
    throw new Error('typeName must be defined when using a descriptor.');
  }

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
    prerenderId,
    end,
  };
}

function getComponentEndComment(type: 'server' | 'webassembly', prerenderedId: string, iterator: ComponentCommentIterator): ChildNode | undefined {
  while (iterator.next() && iterator.currentElement) {
    const node = iterator.currentElement;
    if (node.nodeType !== Node.COMMENT_NODE) {
      continue;
    }
    if (!node.textContent) {
      continue;
    }

    const definition = blazorCommentRegularExpression.exec(node.textContent);
    const json = definition && definition[1];
    if (!json) {
      continue;
    }

    validateEndComponentPayload(type, json, prerenderedId);

    return node;
  }

  return undefined;
}

function validateEndComponentPayload(type: 'server' | 'webassembly', json: string, prerenderedId: string): void {
  const payload = JSON.parse(json) as ComponentComment;
  const effectivePayload = payload.type === 'auto'
    ? (type === 'server' ? payload.server : payload.webAssembly)
    : payload;
  if (Object.keys(effectivePayload).length !== 1) {
    throw new Error(`Invalid end of component comment: '${json}'`);
  }
  const prerenderedEndId = effectivePayload.prerenderId;
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

interface ServerComponentMarker {
  type: string;
  sequence: number;
  descriptor: string;
}

export interface AutoComponentDescriptor {
  type: 'auto';
  serverDescriptor: ServerComponentDescriptor;
  webAssemblyDescriptor: WebAssemblyComponentDescriptor;
}

export class ServerComponentDescriptor {
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

  public toRecord(): ServerComponentMarker {
    const result = { type: this.type, sequence: this.sequence, descriptor: this.descriptor };
    return result;
  }
}

export class WebAssemblyComponentDescriptor {
  private static globalId = 1;

  public type: 'webassembly';

  public typeName: string;

  public assembly: string;

  public parameterDefinitions?: string;

  public parameterValues?: string;

  public id: number;

  public start: Node;

  public end?: Node;

  public constructor(type: 'webassembly', start: Node, end: Node | undefined, assembly: string, typeName: string, parameterDefinitions?: string, parameterValues?: string) {
    this.id = WebAssemblyComponentDescriptor.globalId++;
    this.type = type;
    this.assembly = assembly;
    this.typeName = typeName;
    this.parameterDefinitions = parameterDefinitions;
    this.parameterValues = parameterValues;
    this.start = start;
    this.end = end;
  }
}
