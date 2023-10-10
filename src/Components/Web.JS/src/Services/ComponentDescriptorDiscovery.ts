// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function discoverComponents(root: Node, type: 'webassembly' | 'server' | 'auto'): ComponentDescriptor[] {
  switch (type) {
    case 'webassembly':
      return discoverWebAssemblyComponents(root);
    case 'server':
      return discoverServerComponents(root);
    case 'auto':
      return discoverAutoComponents(root);
  }
}

const blazorServerStateCommentRegularExpression = /^\s*Blazor-Server-Component-State:(?<state>[a-zA-Z0-9+/=]+)$/;
const blazorWebAssemblyStateCommentRegularExpression = /^\s*Blazor-WebAssembly-Component-State:(?<state>[a-zA-Z0-9+/=]+)$/;
const blazorWebInitializerCommentRegularExpression = /^\s*Blazor-Web-Initializers:(?<initializers>[a-zA-Z0-9+/=]+)$/;

export function discoverServerPersistedState(node: Node): string | null | undefined {
  return discoverBlazorComment(node, blazorServerStateCommentRegularExpression);
}

export function discoverWebAssemblyPersistedState(node: Node): string | null | undefined {
  return discoverBlazorComment(node, blazorWebAssemblyStateCommentRegularExpression);
}

export function discoverWebInitializers(node: Node): string | null | undefined {
  return discoverBlazorComment(node, blazorWebInitializerCommentRegularExpression, 'initializers');
}

function discoverBlazorComment(node: Node, comment: RegExp, captureName = 'state'): string | null | undefined {
  if (node.nodeType === Node.COMMENT_NODE) {
    const content = node.textContent || '';
    const parsedState = comment.exec(content);
    const value = parsedState && parsedState.groups && parsedState.groups[captureName];
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
    const result = discoverBlazorComment(candidate, comment, captureName);
    if (result){
      return result;
    }
  }

  return;
}

function discoverServerComponents(root: Node): ServerComponentDescriptor[] {
  const componentComments = resolveComponentComments(root, 'server') as ServerComponentDescriptor[];
  return componentComments.sort((a, b): number => a.sequence - b.sequence);
}

function discoverWebAssemblyComponents(node: Node): WebAssemblyComponentDescriptor[] {
  const componentComments = resolveComponentComments(node, 'webassembly') as WebAssemblyComponentDescriptor[];
  return componentComments;
}

function discoverAutoComponents(node: Node): AutoComponentDescriptor[] {
  const componentComments = resolveComponentComments(node, 'auto') as AutoComponentDescriptor[];
  return componentComments;
}

function resolveComponentComments(node: Node, type: 'webassembly' | 'server' | 'auto'): ComponentDescriptor[] {
  const result: ComponentDescriptor[] = [];
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

function getComponentComment(commentNodeIterator: ComponentCommentIterator, type: 'webassembly' | 'server' | 'auto'): ComponentDescriptor | undefined {
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

        // Regardless of whether this comment matches the type we're looking for, we still need to move the iterator
        // on to its end position since we don't want to recurse into unrelated prerendered components, nor do we want to get confused
        // by the end marker.
        const candidateEnd = getComponentEndComment(componentComment, candidateStart as Comment, commentNodeIterator);

        if (type !== componentComment.type) {
          return undefined;
        }

        switch (componentComment.type) {
          case 'webassembly':
            return createWebAssemblyComponentComment(componentComment, candidateStart as Comment, candidateEnd);
          case 'server':
            return createServerComponentComment(componentComment, candidateStart as Comment, candidateEnd);
          case 'auto':
            return createAutoComponentComment(componentComment, candidateStart as Comment, candidateEnd);
        }
      } catch (error) {
        throw new Error(`Found malformed component comment at ${candidateStart.textContent}`);
      }
    } else {
      return;
    }
  }
}

function parseCommentPayload(json: string): ServerComponentMarker | WebAssemblyComponentMarker | AutoComponentMarker {
  const payload = JSON.parse(json);
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

function getComponentEndComment(payload: ComponentMarker, start: Comment, iterator: ComponentCommentIterator): Comment | undefined {
  const { prerenderId } = payload;
  if (!prerenderId) {
    return undefined;
  }

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

    validateEndComponentPayload(json, prerenderId);

    return node as Comment;
  }

  throw new Error(`Could not find an end component comment for '${start}'.`);
}

let nextUniqueDescriptorId = 0;

function createServerComponentComment(payload: ServerComponentMarker, start: Comment, end: Comment | undefined): ServerComponentDescriptor {
  validateServerComponentPayload(payload);

  return {
    ...payload,
    uniqueId: nextUniqueDescriptorId++,
    start,
    end,
  };
}

function createWebAssemblyComponentComment(payload: WebAssemblyComponentMarker, start: Comment, end: Comment | undefined): WebAssemblyComponentDescriptor {
  validateWebAssemblyComponentPayload(payload);

  return {
    ...payload,
    uniqueId: nextUniqueDescriptorId++,
    start,
    end,
  };
}

function createAutoComponentComment(payload: AutoComponentMarker, start: Comment, end: Comment | undefined): AutoComponentDescriptor {
  validateServerComponentPayload(payload);
  validateWebAssemblyComponentPayload(payload);

  return {
    ...payload,
    uniqueId: nextUniqueDescriptorId++,
    start,
    end,
  };
}

function validateServerComponentPayload(payload: ServerMarkerData) {
  const { descriptor, sequence } = payload;

  if (!descriptor) {
    throw new Error('descriptor must be defined when using a descriptor.');
  }

  if (sequence === undefined) {
    throw new Error('sequence must be defined when using a descriptor.');
  }

  if (!Number.isInteger(sequence)) {
    throw new Error(`Error parsing the sequence '${sequence}' for component '${JSON.stringify(payload)}'`);
  }
}

function validateWebAssemblyComponentPayload(payload: WebAssemblyMarkerData) {
  const { assembly, typeName } = payload;

  if (!assembly) {
    throw new Error('assembly must be defined when using a descriptor.');
  }

  if (!typeName) {
    throw new Error('typeName must be defined when using a descriptor.');
  }

  // Parameter definitions and values come Base64 encoded from the server, since they contain random data and can make the
  // comment invalid. We could unencode them in .NET Code, but that would be slower to do and we can leverage the fact that
  // JS provides a native function that will be much faster and that we are doing this work while we are fetching
  // blazor.boot.json
  payload.parameterDefinitions = payload.parameterDefinitions && atob(payload.parameterDefinitions);
  payload.parameterValues = payload.parameterValues && atob(payload.parameterValues);
}

function validateEndComponentPayload(json: string, prerenderId: string): void {
  const payload = JSON.parse(json) as ComponentEndMarker;
  if (Object.keys(payload).length !== 1) {
    throw new Error(`Invalid end of component comment: '${json}'`);
  }
  const prerenderEndId = payload.prerenderId;
  if (!prerenderEndId) {
    throw new Error(`End of component comment must have a value for the prerendered property: '${json}'`);
  }
  if (prerenderEndId !== prerenderId) {
    throw new Error(`End of component comment prerendered property must match the start comment prerender id: '${prerenderId}', '${prerenderEndId}'`);
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

export function descriptorToMarker(descriptor: ComponentDescriptor): ComponentMarker {
  return {
    ...descriptor,

    // We remove descriptor-specific information to produce a JSON-serializable marker
    start: undefined,
    end: undefined,
  } as unknown as ComponentMarker;
}

function doKeysMatch(a: MarkerKey | undefined, b: MarkerKey | undefined) {
  if (!a || !b) {
    // Unspecified keys are never considered to be matching
    return false;
  }

  return a.locationHash === b.locationHash && a.formattedComponentKey === b.formattedComponentKey;
}

export function canMergeDescriptors(target: ComponentDescriptor, source: ComponentDescriptor): boolean {
  return target.type === source.type && doKeysMatch(target.key, source.key);
}

export function mergeDescriptors(target: ComponentDescriptor, source: ComponentDescriptor) {
  if (!canMergeDescriptors(target, source)) {
    throw new Error(`Cannot merge mismatching component descriptors:\n${JSON.stringify(target)}\nand\n${JSON.stringify(source)}`);
  }

  target.uniqueId = source.uniqueId;

  if (target.type === 'webassembly' || target.type === 'auto') {
    const sourceWebAssemblyData = source as WebAssemblyMarkerData;
    target.parameterDefinitions = sourceWebAssemblyData.parameterDefinitions;
    target.parameterValues = sourceWebAssemblyData.parameterValues;
  }

  if (target.type === 'server' || target.type === 'auto') {
    const sourceServerData = source as ServerMarkerData;
    target.sequence = sourceServerData.sequence;
    target.descriptor = sourceServerData.descriptor;
  }
}

export type ComponentDescriptor = ServerComponentDescriptor | WebAssemblyComponentDescriptor | AutoComponentDescriptor;
export type ComponentMarker = ServerComponentMarker | WebAssemblyComponentMarker | AutoComponentMarker;

export type ServerComponentDescriptor = ServerComponentMarker & DescriptorData;
export type WebAssemblyComponentDescriptor = WebAssemblyComponentMarker & DescriptorData;
export type AutoComponentDescriptor = AutoComponentMarker & DescriptorData;

type DescriptorData = {
  uniqueId: number;
  start: Comment;
  end?: Comment;
};

type ComponentEndMarker = {
  prerenderId: string;
}

type ServerComponentMarker = {
  type: 'server';
} & ServerMarkerData;

type WebAssemblyComponentMarker = {
  type: 'webassembly';
} & WebAssemblyMarkerData;

type AutoComponentMarker = {
  type: 'auto';
} & ServerMarkerData & WebAssemblyMarkerData;

type CommonMarkerData = {
  type: string;
  prerenderId?: string;
  key?: MarkerKey;
}

type MarkerKey = {
  locationHash: string;
  formattedComponentKey?: string;
}

type ServerMarkerData = {
  sequence: number;
  descriptor: string;
} & CommonMarkerData;

type WebAssemblyMarkerData = {
  typeName: string;
  assembly: string;
  parameterDefinitions: string;
  parameterValues: string;
} & CommonMarkerData;
