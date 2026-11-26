// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import * as DotNet from '../JSInterop/Microsoft.JSInterop';
import { depthFirstNodeTreeTraversal, getLogicalParent, LogicalElement } from './LogicalElements';

const pendingRootComponentDisposals = new Map<number, { root: LogicalElement, references: Map<string, Element> }>();
const retainedReferencesByRoot = new WeakMap<LogicalElement, Map<string, Element>>();

export function applyCaptureIdToElement(element: Element & LogicalElement, referenceCaptureId: string): void {
  element.setAttribute(getCaptureIdAttributeName(referenceCaptureId), '');

  if (pendingRootComponentDisposals.size === 0 || element.isConnected) {
    return;
  }

  // A render batch can add captures after navigation has moved the logical root out of the document.
  for (let ancestor: LogicalElement | null = element; ancestor; ancestor = getLogicalParent(ancestor)) {
    const references = retainedReferencesByRoot.get(ancestor);
    if (references) {
      references.set(referenceCaptureId, element);
      return;
    }
  }
}

export function retainRootComponentElementReferences(ssrComponentId: number, root: LogicalElement): void {
  // Rendering can empty the detached subtree before asynchronous .NET disposal completes.
  const references = new Map<string, Element>();
  for (const node of depthFirstNodeTreeTraversal(root)) {
    if (node instanceof Element) {
      for (const name of node.getAttributeNames()) {
        if (name.startsWith('_bl_')) {
          references.set(name.substring(4), node);
        }
      }
    }
  }

  pendingRootComponentDisposals.set(ssrComponentId, { root, references });
  retainedReferencesByRoot.set(root, references);
}

export function releaseRootComponentElementReferences(ssrComponentId: number): void {
  const pendingDisposal = pendingRootComponentDisposals.get(ssrComponentId);
  if (pendingDisposal) {
    retainedReferencesByRoot.delete(pendingDisposal.root);
    pendingRootComponentDisposals.delete(ssrComponentId);
  }
}

function getElementByCaptureId(referenceCaptureId: string) {
  const selector = `[${getCaptureIdAttributeName(referenceCaptureId)}]`;
  const element = document.querySelector(selector);
  if (element) {
    return element;
  }

  for (const { references } of pendingRootComponentDisposals.values()) {
    const retainedElement = references.get(referenceCaptureId);
    if (retainedElement) {
      return retainedElement;
    }
  }

  return null;
}

function getCaptureIdAttributeName(referenceCaptureId: string) {
  return `_bl_${referenceCaptureId}`;
}

// Support receiving ElementRef instances as args in interop calls
const elementRefKey = '__internalId'; // Keep in sync with ElementRef.cs
DotNet.attachReviver((key, value) => {
  if (value && typeof value === 'object' && Object.prototype.hasOwnProperty.call(value, elementRefKey) && typeof value[elementRefKey] === 'string') {
    return getElementByCaptureId(value[elementRefKey]);
  } else {
    return value;
  }
});
