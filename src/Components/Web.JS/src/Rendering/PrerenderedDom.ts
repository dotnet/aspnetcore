// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogicalElement, adoptLogicalChild, getLogicalChildrenArray, getLogicalDomNodeContainer, getLogicalRootDescriptor, isLogicalElement, resetLogicalChildren, toLogicalElement } from './LogicalElements';
import { deferredValuePropname } from './DomSpecialPropertyUtil';
import { synchronizePrerenderedDom } from './DomMerging/DomSync';

export interface PrerenderedDomPreview {
  document: Document;
  root: Element;
  frames: Map<number, Node | LogicalElement>;
}

export type PrerenderedFramesForRendering = Map<number, {
  nodes: Node[];
  preserveFormState?: () => void;
  prepareMarkupForRendering?: () => void;
}>;

export class PrerenderedDom {
  private preview?: PrerenderedDomPreview;

  public constructor(private readonly createPreview: () => PrerenderedDomPreview | undefined) {
  }

  public tryPlanRoot(root: LogicalElement): boolean {
    const descriptor = getLogicalRootDescriptor(root);
    if (!descriptor?.end || !descriptor.start.parentNode || descriptor.end.parentNode !== descriptor.start.parentNode) {
      return false;
    }
    for (let node = descriptor.start.nextSibling; node && node !== descriptor.end; node = node.nextSibling) {
      if (hasUnsupportedContent(node)) {
        return false;
      }
    }
    const preview = this.createPreview();
    if (!preview || hasUnsupportedContent(preview.root)) {
      return false;
    }
    this.preview = preview;
    return true;
  }

  public applyRootPlan(root: LogicalElement, framesForRendering: PrerenderedFramesForRendering): void {
    const preview = this.preview!;
    const descriptor = getLogicalRootDescriptor(root);
    const end = descriptor.end!;
    const anchors = new Set<Node>(Array.from(preview.frames.values()).filter(node => node instanceof Comment));
    const children = new Map<Node, Node[]>();
    const actual = new Map<Node | LogicalElement, Node>([[preview.root, descriptor.start.parentNode!]]);
    const textRuns = new Map<Text, { node: Text; length: number }[]>();

    const prepareChildren = (source: Node) => {
      const originalChildren = Array.from(source.childNodes);
      children.set(source, originalChildren);
      let previousText: Text | undefined;
      for (const child of originalChildren) {
        if (anchors.has(child)) {
          source.removeChild(child);
        } else if (child instanceof Text) {
          const part = { node: child, length: child.length };
          if (previousText) {
            textRuns.get(previousText)!.push(part);
            previousText.appendData(child.data);
            source.removeChild(child);
          } else {
            previousText = child;
            textRuns.set(child, [part]);
          }
        } else {
          previousText = undefined;
          actual.set(child, child);
          prepareChildren(child);
          if (child instanceof HTMLInputElement && deferredValuePropname in child && child.type !== 'file') {
            child.setAttribute('value', child.value);
          }
        }
      }
    };
    prepareChildren(preview.root);
    for (const text of textRuns.keys()) {
      if (!text.data) {
        text.parentNode!.removeChild(text);
        textRuns.delete(text);
      }
    }

    const preservedFormState = new Set<Node>();
    synchronizePrerenderedDom(
      { startExclusive: descriptor.start, endExclusive: end },
      preview.root,
      (destination, source) => {
        actual.set(source, destination);
        if (matchesFormDefaults(destination, source)) {
          preservedFormState.add(destination);
          if (destination instanceof HTMLSelectElement) {
            destination.querySelectorAll('*').forEach(child => preservedFormState.add(child));
          } else if (destination instanceof HTMLTextAreaElement) {
            // Removing SSR text must not reset a pristine textarea's live value.
            const value = destination.value;
            destination.value = value;
          }
        }
        return preservedFormState.has(destination);
      }
    );
    // SSR coalesces adjacent frame text. Restore frame boundaries only after the physical diff.
    for (const [source, parts] of textRuns) {
      let text = (actual.get(source) ?? source) as Text;
      for (let i = 0; i < parts.length; i++) {
        const next = i + 1 < parts.length ? text.splitText(parts[i].length) : undefined;
        actual.set(parts[i].node, text);
        text = next!;
      }
    }
    const restoreAnchors = (source: Node, boundary: Node | null) => {
      const parent = actual.get(source)!;
      let next = boundary;
      for (const child of (children.get(source) ?? []).slice().reverse()) {
        let node = actual.get(child);
        if (!node) {
          node = child;
          parent.insertBefore(node, next);
          actual.set(child, node);
        } else if (node.parentNode !== parent) {
          throw new Error('Prerendered DOM synchronization left an interactive node outside its expected parent.');
        }
        restoreAnchors(child, null);
        next = node;
      }
    };
    restoreAnchors(preview.root, end);

    // Capture frame ownership before clearing logical children on inserted preview nodes.
    for (const [index, source] of preview.frames) {
      const node = actual.get(source)!;
      const nodes = [node];
      if (source instanceof Comment) {
        nodes.push(...getLogicalChildrenArray(toLogicalElement(source)).map(child => actual.get(child)!));
      }
      if (node instanceof HTMLInputElement) {
        normalizeInputDefaults(node);
      }
      framesForRendering.set(index, {
        nodes,
        preserveFormState: preservedFormState.has(node) ? () => {
          // Initialize renderer bookkeeping only after the retained children are bound.
          if (deferredValuePropname in source) {
            node[deferredValuePropname] = source[deferredValuePropname];
          }
        } : undefined,
        prepareMarkupForRendering: () => {
          for (const child of nodes.slice(1)) {
            restoreMarkupChildren(child);
          }
        },
      });
    }
    resetLogicalChildren(root);
    for (const [source, node] of actual) {
      if (source !== preview.root) {
        resetLogicalChildren(toLogicalElement(node, true));
      }
    }
    end.parentNode!.removeChild(end);
  }
}

function matchesFormDefaults(destination: Node, source: Node): boolean {
  if (!(destination instanceof Element) || !(source instanceof Element)) {
    return false;
  }
  if (destination instanceof HTMLInputElement && source instanceof HTMLInputElement) {
    const defaults = parseFormDefaults(destination, source.ownerDocument);
    return defaults instanceof HTMLInputElement && matchesFormAttributes(destination, source) &&
      defaults.value === source.value && defaults.checked === source.checked;
  }
  if (destination instanceof HTMLTextAreaElement && source instanceof HTMLTextAreaElement) {
    return matchesFormAttributes(destination, source) && destination.defaultValue === source.value;
  }
  if (destination instanceof HTMLSelectElement && source instanceof HTMLSelectElement) {
    const defaults = parseFormDefaults(destination, source.ownerDocument);
    if (!(defaults instanceof HTMLSelectElement)) {
      return false;
    }
    const expected = source.cloneNode(true) as HTMLSelectElement;
    defaults.removeAttribute('value');
    expected.removeAttribute('value');
    const options = Array.from(defaults.options);
    const selectionMatches = options.length === source.options.length &&
      options.every((option, index) => option.selected === source.options[index].selected);
    for (const option of [...options, ...Array.from(expected.options)]) {
      option.removeAttribute('selected');
    }
    return selectionMatches && defaults.isEqualNode(expected);
  }
  return false;
}

function parseFormDefaults(element: Element, ownerDocument: Document): Element | null {
  const template = ownerDocument.createElement('template');
  template.innerHTML = element.outerHTML;
  return template.content.firstElementChild;
}

function matchesFormAttributes(destination: Element, source: Element): boolean {
  const attributes = (element: Element) => element.getAttributeNames().filter(name => name !== 'value' && name !== 'checked');
  const expected = attributes(source);
  const actual = attributes(destination);
  return actual.length === expected.length &&
    actual.every(name => destination.getAttribute(name) === source.getAttribute(name));
}

function hasUnsupportedContent(node: Node): boolean {
  if (node instanceof HTMLTemplateElement ||
      node instanceof Element && node.hasAttribute('data-permanent') ||
      isLogicalElement(node) && getLogicalRootDescriptor(toLogicalElement(node))) {
    return true;
  }
  return Array.from(node.childNodes).some(hasUnsupportedContent);
}

function restoreMarkupChildren(node: Node): void {
  const logical = toLogicalElement(node, true);
  const children = Array.from(getLogicalDomNodeContainer(node).childNodes);
  resetLogicalChildren(logical);
  children.forEach((child, index) => {
    restoreMarkupChildren(child);
    adoptLogicalChild(child, logical, index);
  });
}

function normalizeInputDefaults(element: HTMLInputElement): void {
  // Keep the synchronized live state when removing the SSR-only reset defaults.
  if (element.hasAttribute('checked')) {
    const checked = element.checked;
    element.checked = checked;
    element.removeAttribute('checked');
  }
  if (element.hasAttribute('value')) {
    switch (element.type) {
      case 'hidden':
      case 'button':
      case 'submit':
      case 'reset':
      case 'image':
      case 'checkbox':
      case 'radio':
        return;
      case 'file':
        break;
      default: {
        if (!element.validity.badInput) {
          const value = element.value;
          element.value = value;
        }
        break;
      }
    }
    element.removeAttribute('value');
  }
}
