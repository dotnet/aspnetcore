// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ServerComponentDescriptor, WebAssemblyComponentDescriptor, discoverComponents } from '../../Services/ComponentDescriptorDiscovery';
import { LogicalElement, getLogicalRootDescriptor, moveLogicalRootToDocumentFragment } from '../LogicalElements';
import { CommentBoundedRange, synchronizeDomContent } from './DomSync';

const boundaryDataSymbol = Symbol();
const descriptorSymbol = Symbol();
let descriptorHandler: DescriptorHandler;

type ComponentDescriptor = ServerComponentDescriptor | WebAssemblyComponentDescriptor;

interface BoundaryCommentData {
  descriptor: ComponentDescriptor;
  content: DocumentFragment | null;
  type: 'incoming-ssr' | 'existing-ssr' | 'existing-interactive';
}

export interface DescriptorHandler {
  onDescriptorAdded(descriptor: ComponentDescriptor): void;
}

export function attachComponentDescriptorHandler(handler: DescriptorHandler) {
  descriptorHandler = handler;
}

export function insertBoundaryCommentsIntoDestination(destination: CommentBoundedRange | Node) {
  let nextDestinationNode: Node | null;
  let endAtNodeExclOrNull: Node | null;

  if (destination instanceof Node) {
    nextDestinationNode = destination.firstChild;
    endAtNodeExclOrNull = null;
  } else {
    nextDestinationNode = destination.startExclusive.nextSibling;
    endAtNodeExclOrNull = destination.endExclusive;
  }

  while (nextDestinationNode && nextDestinationNode !== endAtNodeExclOrNull) {
    if (nextDestinationNode.nodeType !== Node.COMMENT_NODE) {
      // Only consider comment nodes.
      nextDestinationNode = nextDestinationNode.nextSibling;
      continue;
    }

    const nextDestinationNodeAsComment = nextDestinationNode as Comment;
    const boundaryComment =
      tryReplaceInteractiveComponentWithBoundaryComment(nextDestinationNodeAsComment) ||
      tryReplaceSsrComponentWithBoundaryComment(nextDestinationNodeAsComment);

    nextDestinationNode = boundaryComment
      ? boundaryComment.nextSibling
      : nextDestinationNode.nextSibling;
  }
}

export function insertBoundaryCommentsIntoNewContent(newContent?: Node) {
  if (!newContent) {
    return;
  }

  const newServerComponents = discoverComponents(newContent, 'server', /* directChildrenOnly */ true) as ServerComponentDescriptor[];
  const newWebAssemblyComponents = discoverComponents(newContent, 'webassembly', /* directChildrenOnly */ true) as WebAssemblyComponentDescriptor[];

  for (const descriptor of [...newServerComponents, ...newWebAssemblyComponents]) {
    // Add the descriptor as a property on the 'start' comment.
    // This lets us find already-discovered component comments easily.
    descriptor.start[descriptorSymbol] = descriptor;

    const boundaryComment = insertBoundaryComment(descriptor.start);
    const content = extractDescriptorContentsToDocumentFragment(descriptor);
    setBoundaryCommentData(boundaryComment, {
      descriptor,
      content,
      type: 'incoming-ssr',
    });
  }
}

function tryReplaceInteractiveComponentWithBoundaryComment(destination: Comment): Comment | null {
  const destinationAsLogicalElement = destination as unknown as LogicalElement;
  const descriptor = getLogicalRootDescriptor(destinationAsLogicalElement);
  if (!descriptor) {
    return null;
  }

  const boundaryComment = insertBoundaryComment(destination);
  const content = moveLogicalRootToDocumentFragment(destinationAsLogicalElement);
  setBoundaryCommentData(boundaryComment, {
    content,
    descriptor,
    type: 'existing-interactive',
  });

  return boundaryComment;
}

function tryReplaceSsrComponentWithBoundaryComment(destination: Comment): Comment | null {
  const descriptor = destination[descriptorSymbol];
  if (!descriptor) {
    return null;
  }

  const boundaryComment = insertBoundaryComment(destination);
  const content = extractDescriptorContentsToDocumentFragment(descriptor);
  setBoundaryCommentData(boundaryComment, {
    content,
    descriptor,
    type: 'existing-ssr',
  });

  return boundaryComment;
}

export function isBoundaryComment(node: Node): boolean {
  return node[boundaryDataSymbol];
}

export function synchronizeBoundary(destination: Comment, source: Comment) {
  const destinationBoundaryData = destination[boundaryDataSymbol] as BoundaryCommentData;
  const newBoundaryData = source[boundaryDataSymbol] as BoundaryCommentData;

  if (!destinationBoundaryData !== !newBoundaryData) {
    throw new Error('Attempted to merge a boundary comment with a non-boundary comment.');
  }

  if (!destinationBoundaryData) {
    // Not a boundary comment.
    return;
  }

  if (destinationBoundaryData.descriptor.type !== newBoundaryData.descriptor.type) {
    throw new Error(`Attempted to merge component descriptors with different types: '${destinationBoundaryData.type}' and '${newBoundaryData.type}'.`);
  }

  // Merge the descriptors together, preserving the start and end comments.
  Object.assign(destinationBoundaryData.descriptor, {
    ...newBoundaryData.descriptor,
    start: destinationBoundaryData.descriptor.start,
    end: destinationBoundaryData.descriptor.end,
  });

  const destinationParent = destination.parentNode!;
  switch (destinationBoundaryData.type) {
    case 'existing-interactive':
      // Ignore incoming content for exising interactive components.
      destinationParent.replaceChild(destinationBoundaryData.content!, destination);
      break;
    case 'existing-ssr':
      // FIXME: What happens when merging prerendered content into a non-prerendered comment?
      // I think this should never be allowed because a component can't dynamically switch between
      // being prerendered and not.

      if (destinationBoundaryData.content) {
        const { start, end } = destinationBoundaryData.descriptor;
        destinationParent.insertBefore(start, destination);
        if (end) {
          synchronizeDomContent(destinationBoundaryData.content, newBoundaryData.content!);
          destinationParent.insertBefore(end, destination.nextSibling);
          destinationParent.replaceChild(destinationBoundaryData.content, destination);
        }
      } else {
        destinationParent.insertBefore(destinationBoundaryData.descriptor.start, destination);
      }
      break;
    default:
      throw new Error(`Destination boundary comment had invalid type '${destinationBoundaryData.type}'.`);
  }
}

export function insertBoundary(comment: Comment, nextNode: Node | null, parentNode: Node) {
  const boundary = comment[boundaryDataSymbol] as BoundaryCommentData;
  if (!boundary) {
    throw new Error('A non-boundary comment was inserted as a boundary comment.');
  }

  if (boundary.type !== 'incoming-ssr') {
    throw new Error(`An incoming boundary comment had invalid type ${boundary.type}`);
  }

  const { start, end } = boundary.descriptor;
  parentNode.insertBefore(start, nextNode);

  if (end) {
    if (!boundary.content) {
      throw new Error('Descriptor had an end marker but no prerendered content.');
    }
    parentNode.insertBefore(end, start.nextSibling);
    parentNode.insertBefore(boundary.content, start.nextSibling);
  }

  descriptorHandler.onDescriptorAdded(boundary.descriptor);
}

export function processComponentDescriptors(rootNode: Node) {
  const newServerComponents = discoverComponents(rootNode, 'server') as ServerComponentDescriptor[];
  const newWebAssemblyComponents = discoverComponents(rootNode, 'webassembly') as WebAssemblyComponentDescriptor[];

  for (const descriptor of [...newServerComponents, ...newWebAssemblyComponents]) {
    // Add the descriptor as a property on the 'start' comment.
    // This lets us find already-discovered component comments easily.
    descriptor.start[descriptorSymbol] = descriptor;
    descriptorHandler.onDescriptorAdded(descriptor);
  }
}

function extractDescriptorContentsToDocumentFragment(descriptor: ServerComponentDescriptor | WebAssemblyComponentDescriptor): DocumentFragment | null {
  let docFrag: DocumentFragment | null = null;

  const { start, end } = descriptor;

  // Extact content between start and end commnets.
  if (end) {
    const range = new Range();
    range.setStartAfter(start);
    range.setEndBefore(end);
    docFrag = range.extractContents();
  }

  // Remove comment nodes.
  const parentNode = start.parentNode!;
  parentNode.removeChild(start);
  if (end) {
    parentNode.removeChild(end);
  }

  return docFrag;
}

function insertBoundaryComment(before: Node): Comment {
  const comment = document.createComment('bl-boundary');
  before.parentNode!.insertBefore(comment, before);
  return comment;
}

function setBoundaryCommentData(comment: Comment, data: BoundaryCommentData) {
  comment[boundaryDataSymbol] = data;
}
