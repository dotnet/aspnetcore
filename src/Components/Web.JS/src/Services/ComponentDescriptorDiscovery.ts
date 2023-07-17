// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function discoverComponents(root: Node, type: 'webassembly' | 'server' | 'auto'): ServerComponentDescriptor[] | WebAssemblyComponentDescriptor[] | AutoComponentDescriptor[] {
  switch (type) {
    case 'webassembly':
      return discoverWebAssemblyComponents(root);
    case 'server':
      return discoverServerComponents(root);
    case 'auto':
      return discoverAutoComponents(root);
  }
}

function discoverServerComponents(root: Node): ServerComponentDescriptor[] {
  const componentComments = resolveComponentComments(root, 'server') as ServerComponentComment[];
  const discoveredComponents: ServerComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const componentComment = componentComments[i];
    const entry = new ServerComponentDescriptor(
      componentComment.type,
      componentComment.start,
      componentComment.end,
      componentComment.sequence,
      componentComment.descriptor,
      componentComment.key
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents.sort((a, b): number => a.sequence - b.sequence);
}

const blazorStateCommentRegularExpression = /^\s*Blazor-Component-State:(?<state>[a-zA-Z0-9+/=]+)$/;

export function discoverPersistedState(node: Node): string | null | undefined {
  if (node.nodeType === Node.COMMENT_NODE) {
    const content = node.textContent || '';
    const parsedState = blazorStateCommentRegularExpression.exec(content);
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
    const result = discoverPersistedState(candidate);
    if (result){
      return result;
    }
  }

  return;
}

function discoverWebAssemblyComponents(node: Node): WebAssemblyComponentDescriptor[] {
  const componentComments = resolveComponentComments(node, 'webassembly') as WebAssemblyComponentComment[];
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
      componentComment.key
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents.sort((a, b): number => a.id - b.id);
}

function discoverAutoComponents(node: Node): AutoComponentDescriptor[] {
  const componentComments = resolveComponentComments(node, 'auto') as AutoComponentComment[];
  const discoveredComponents: AutoComponentDescriptor[] = [];
  for (let i = 0; i < componentComments.length; i++) {
    const componentComment = componentComments[i];
    const s = componentComment.server;
    const w = componentComment.webAssembly;
    const entry = new AutoComponentDescriptor(
      componentComment.type,
      componentComment.start,
      componentComment.end,
      new ServerComponentDescriptor(s.type, s.start, s.end, s.sequence, s.descriptor, s.key),
      new WebAssemblyComponentDescriptor(w.type, w.start, w.end, w.assembly, w.typeName, w.parameterDefinitions, w.parameterValues, w.key)
    );

    discoveredComponents.push(entry);
  }

  return discoveredComponents;
}

interface ComponentComment {
  type: 'server' | 'webassembly' | 'auto';
  prerenderId?: string;
}

interface ServerComponentComment {
  type: 'server';
  sequence: number;
  descriptor: string;
  start: Comment;
  end?: Comment;
  prerenderId?: string;
  key?: string;
}

interface WebAssemblyComponentComment {
  type: 'webassembly';
  typeName: string;
  assembly: string;
  parameterDefinitions?: string;
  parameterValues?: string;
  prerenderId?: string;
  start: Comment;
  end?: Comment;
  key?: string;
}

interface AutoComponentComment {
  type: 'auto'
  server: ServerComponentComment;
  webAssembly: WebAssemblyComponentComment;
  prerenderId?: string;
  start: Comment;
  end?: Comment;
}

function resolveComponentComments(node: Node, type: 'webassembly' | 'server' | 'auto'): ComponentComment[] {
  const result: ComponentComment[] = [];
  const childNodeIterator = new ComponentCommentIterator(node.childNodes);
  while (childNodeIterator.next() && childNodeIterator.currentElement) {
    const componentComment = getComponentComment(childNodeIterator, type);
    if (componentComment) {
      result.push(componentComment);
    } else if (childNodeIterator.currentElement.hasChildNodes()) {
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
      assertNotDirectlyOnDocument(candidateStart);
      try {
        const componentComment = parseCommentPayload(json);
        switch (type) {
          case 'webassembly':
            return createWebAssemblyComponentComment(componentComment as WebAssemblyComponentComment, candidateStart as Comment, commentNodeIterator);
          case 'server':
            return createServerComponentComment(componentComment as ServerComponentComment, candidateStart as Comment, commentNodeIterator);
          case 'auto':
            return createAutoComponentComment(componentComment as AutoComponentComment, candidateStart as Comment, commentNodeIterator);
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

function assertNotDirectlyOnDocument(marker: Node) {
  if (marker.parentNode instanceof Document) {
    throw new Error('Root components cannot be marked as interactive. The <html> element must be rendered statically so that scripts are not evaluated multiple times.');
  }
}

function createServerComponentComment(payload: ServerComponentComment, start: Comment, iterator: ComponentCommentIterator): ServerComponentComment | undefined {
  const { type, descriptor, sequence, prerenderId, key } = payload;

  // Regardless of whether this comment matches the type we're looking for (i.e., 'server'), we still need to move the iterator
  // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
  // by the end marker.
  const end = prerenderId ? getComponentEndComment(prerenderId, iterator) : undefined;
  if (prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'.`);
  }

  if (type !== 'server') {
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
    key,
  };
}

function createWebAssemblyComponentComment(payload: WebAssemblyComponentComment, start: Comment, iterator: ComponentCommentIterator): WebAssemblyComponentComment | undefined {
  const { type, assembly, typeName, parameterDefinitions, parameterValues, prerenderId, key } = payload;

  // Regardless of whether this comment matches the type we're looking for (i.e., 'webassembly'), we still need to move the iterator
  // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
  // by the end marker.
  const end = prerenderId ? getComponentEndComment(prerenderId, iterator) : undefined;
  if (prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'.`);
  }

  if (type !== 'webassembly') {
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
    key,
  };
}

function createAutoComponentComment(payload: AutoComponentComment, start: Comment, iterator: ComponentCommentIterator): AutoComponentComment | undefined {
  const { type, prerenderId, server, webAssembly } = payload;

  const end = prerenderId ? getComponentEndComment(prerenderId, iterator) : undefined;
  if (server?.prerenderId && !end) {
    throw new Error(`Could not find an end component comment for '${start}'.`);
  }

  if (type !== 'auto') {
    return undefined;
  }

  return {
    type,
    server: {
      ...server,
      start,
      end,
    },
    webAssembly: {
      ...webAssembly,
      start,
      end,
      // See comment in createWebAssemblyComponentComment
      parameterDefinitions: webAssembly.parameterDefinitions && atob(webAssembly.parameterDefinitions),
      parameterValues: webAssembly.parameterValues && atob(webAssembly.parameterValues),
    },
    start,
    end,
  };
}

function getComponentEndComment(prerenderedId: string, iterator: ComponentCommentIterator): Comment | undefined {
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

    validateEndComponentPayload(json, prerenderedId);

    return node as Comment;
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

export type ComponentMarker = ServerComponentMarker | WebAssemblyComponentMarker;

type ServerComponentMarker = {
  type: 'server';
  sequence: number;
  descriptor: string;
}

type WebAssemblyComponentMarker = {
  type: 'webassembly';
  typeName: string;
  assembly: string;
  parameterDefinitions?: string;
  parameterValues?: string;
}

export type ComponentDescriptor = ServerComponentDescriptor | WebAssemblyComponentDescriptor | AutoComponentDescriptor;

export class ServerComponentDescriptor {
  private static globalId = 1;

  public type: 'server';

  public start: Comment;

  public end?: Comment;

  public id: number;

  public sequence: number;

  public descriptor: string;

  public key?: string;

  public constructor(type: 'server', start: Comment, end: Comment | undefined, sequence: number, descriptor: string, key: string | undefined) {
    this.id = ServerComponentDescriptor.globalId++;
    this.type = type;
    this.start = start;
    this.end = end;
    this.sequence = sequence;
    this.descriptor = descriptor;
    this.key = key;
  }

  public matches(other: ComponentDescriptor): other is ServerComponentDescriptor {
    return this.type === other.type && this.key === other.key;
  }

  public update(other: ComponentDescriptor) {
    if (!this.matches(other)) {
      throw new Error(`Cannot merge mismatching component descriptors:\n${JSON.stringify(this)}\nand\n${JSON.stringify(other)}`);
    }

    this.sequence = other.sequence;
    this.descriptor = other.descriptor;
    this.id = other.id;
  }

  public toRecord(): ServerComponentMarker {
    return {
      type: this.type,
      sequence: this.sequence,
      descriptor: this.descriptor,
    };
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

  public start: Comment;

  public end?: Comment;

  public key?: string;

  public constructor(type: 'webassembly', start: Comment, end: Comment | undefined, assembly: string, typeName: string, parameterDefinitions?: string, parameterValues?: string, key?: string) {
    this.id = WebAssemblyComponentDescriptor.globalId++;
    this.type = type;
    this.assembly = assembly;
    this.typeName = typeName;
    this.parameterDefinitions = parameterDefinitions;
    this.parameterValues = parameterValues;
    this.start = start;
    this.end = end;
    this.key = key;
  }

  public matches(other: ComponentDescriptor): other is WebAssemblyComponentDescriptor {
    return this.type === other.type && this.key === other.key && this.typeName === other.typeName && this.assembly === other.assembly;
  }

  public update(other: ComponentDescriptor) {
    if (!this.matches(other)) {
      throw new Error(`Cannot merge mismatching component descriptors:\n${JSON.stringify(this)}\nand\n${JSON.stringify(other)}`);
    }

    this.parameterDefinitions = other.parameterDefinitions;
    this.parameterValues = other.parameterValues;
    this.id = other.id;
  }

  public toRecord(): WebAssemblyComponentMarker {
    return {
      type: this.type,
      typeName: this.typeName,
      assembly: this.assembly,
      parameterDefinitions: this.parameterDefinitions,
      parameterValues: this.parameterValues,
    };
  }
}

export class AutoComponentDescriptor {
  private static globalId = 1;

  public type: 'auto';

  public id: number;

  public start: Comment;

  public end?: Comment;

  public serverDescriptor: ServerComponentDescriptor;

  public webAssemblyDescriptor: WebAssemblyComponentDescriptor;

  private resolvedType: 'server' | 'webassembly' | undefined;

  public constructor(type: 'auto', start: Comment, end: Comment | undefined, serverDescriptor: ServerComponentDescriptor, webAssemblyDescriptor: WebAssemblyComponentDescriptor) {
    this.id = AutoComponentDescriptor.globalId++;
    this.type = type;
    this.start = start;
    this.end = end;
    this.serverDescriptor = serverDescriptor;
    this.webAssemblyDescriptor = webAssemblyDescriptor;
  }

  public matches(other: ComponentDescriptor): other is AutoComponentDescriptor {
    return this.type === other.type && this.serverDescriptor.matches(other.serverDescriptor) && this.webAssemblyDescriptor.matches(other.webAssemblyDescriptor);
  }

  public update(other: ComponentDescriptor) {
    if (!this.matches(other)) {
      throw new Error(`Cannot merge mismatching component descriptors:\n${JSON.stringify(this)}\nand\n${JSON.stringify(other)}`);
    }

    this.serverDescriptor.update(other.serverDescriptor);
    this.webAssemblyDescriptor.update(other.webAssemblyDescriptor);
  }

  public hasResolvedType(): boolean {
    return this.resolvedType !== undefined;
  }

  public setResolvedType(type: 'server' | 'webassembly') {
    if (this.resolvedType) {
      throw new Error('Auto components can only resolve to a component type once');
    }
    this.resolvedType = type;
  }

  public getResolvedType(): 'server' | 'webassembly' {
    if (!this.resolvedType) {
      throw new Error('Cannot get the resolved type of an auto component before setResolvedType() is called');
    }
    return this.resolvedType;
  }

  public toRecord(): ServerComponentMarker | WebAssemblyComponentMarker {
    switch (this.resolvedType) {
      case 'server':
        return this.serverDescriptor.toRecord();
      case 'webassembly':
        return this.webAssemblyDescriptor.toRecord();
      default:
        throw new Error(`Auto component record cannot be created from resolved marker type '${this.resolvedType}'`);
    }
  }
}
