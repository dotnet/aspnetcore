// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ArrayValues, FrameType, RenderBatch, RenderTreeDiff, RenderTreeFrame, countDescendantFrames } from './RenderBatch/RenderBatch';
import { LogicalElement, adoptLogicalChild, getLogicalRootDescriptor, isSvgElement, isMathMLElement, resetLogicalChildren, toLogicalElement, getLogicalDomNodeContainer, getElementNamespace } from './LogicalElements';
import { internalAttributeNamePrefix } from './DomSpecialPropertyUtil';

interface Cursor {
  parent: Node;
  next: Node | null;
  end?: Node;
  pendingText?: TextRun;
}

interface Insertion {
  node: Node | null;
  elementToConnect?: Element;
  parent: Node;
  next: Node | null;
  offset: number;
}

interface TextRun {
  content: string;
  segments: { content: string; nodes: Node[]; insertion: Insertion; nodeIndex?: number }[];
  insertions: Insertion[];
}

export type PrerenderedFramesForRendering = Map<number, {
  nodes: Node[];
  prepareMarkupForRendering?: () => void;
}>;

export class PrerenderedDom {
  private readonly plannedFrames: PrerenderedFramesForRendering = new Map();

  private readonly matchingTextRuns: { text: Text; run: TextRun }[] = [];

  private readonly preservedElements: Element[] = [];

  private readonly formActions: Element[] = [];

  private readonly insertions: Insertion[] = [];

  private readonly removals: Node[] = [];

  private hasDifferences = false;

  private hasPreservedInitializer = false;

  private readonly frames: ArrayValues<RenderTreeFrame>;

  public constructor(
    private readonly batch: RenderBatch,
    private readonly diffs: Map<number, RenderTreeDiff | undefined>,
    private readonly parseMarkup: (markup: string, isSvg: boolean, isMathML: boolean) => Node,
  ) {
    this.frames = batch.arrayRangeReader.values(batch.referenceFrames());
  }

  public tryPlanRoot(componentId: number, root: LogicalElement): boolean {
    const descriptor = getLogicalRootDescriptor(root);
    const end = descriptor?.end;
    if (!end || !descriptor.start.parentNode || end.parentNode !== descriptor.start.parentNode) {
      return false;
    }

    const cursor: Cursor = { parent: descriptor.start.parentNode, next: descriptor.start.nextSibling, end };
    if (!this.planComponent(componentId, cursor)) {
      return false;
    }
    this.queueRemainingNodeRemovals(cursor);
    // A retained initializer cannot be rerun safely when its dependent DOM changes.
    return !(this.hasDifferences && this.hasPreservedInitializer);
  }

  public applyRootPlan(root: LogicalElement, framesForRendering: PrerenderedFramesForRendering): void {
    this.splitTextNodesAtFrameBoundaries();
    this.applyQueuedDomChanges();

    resetLogicalChildren(root);
    for (const element of this.preservedElements) {
      resetLogicalChildren(toLogicalElement(element, true));
      if (element instanceof HTMLInputElement) {
        normalizeInputDefaults(element);
      }
    }
    for (const element of this.formActions) {
      element.removeAttribute('action');
    }
    const end = getLogicalRootDescriptor(root).end!;
    end.parentNode!.removeChild(end);
    for (const [index, framePlan] of this.plannedFrames) {
      framesForRendering.set(index, framePlan);
    }
  }

  private splitTextNodesAtFrameBoundaries(): void {
    for (const { text, run } of this.matchingTextRuns) {
      const parts = new Map<number, Text>([[0, text]]);
      let offset = run.content.length;
      for (let index = run.segments.length - 1; index >= 0; index--) {
        const segment = run.segments[index];
        offset -= segment.content.length;
        if (segment.content) {
          const part = offset > 0 ? text.splitText(offset) : text;
          parts.set(offset, part);
          segment.nodes[segment.nodeIndex!] = part;
        }
      }
      for (const insertion of run.insertions) {
        if (insertion.offset > 0) {
          insertion.next = parts.get(insertion.offset)!;
          insertion.offset = 0;
        }
      }
    }
  }

  private applyQueuedDomChanges(): void {
    for (const { node, parent, next } of this.insertions) {
      if (node) {
        parent.insertBefore(node, next);
      }
    }
    for (const node of this.removals) {
      node.parentNode!.removeChild(node);
    }
  }

  private planComponent(componentId: number, cursor: Cursor, selectValue?: string | null): boolean {
    const diff = this.diffs.get(componentId);
    if (diff === undefined) {
      return !this.diffs.has(componentId);
    }

    const { arrayBuilderSegmentReader: segmentReader, diffReader, editReader } = this.batch;
    const edits = diffReader.edits(diff);
    const values = segmentReader.values(edits);
    const offset = segmentReader.offset(edits);
    const editCount = segmentReader.count(edits);
    for (let i = offset; i < offset + editCount; i++) {
      const edit = diffReader.editsEntry(values, i);
      if (!this.planFrame(editReader.newTreeIndex(edit), cursor, selectValue)) {
        return false;
      }
    }
    return true;
  }

  private planFrame(index: number, cursor: Cursor, selectValue?: string | null): boolean {
    const { frameReader } = this.batch;
    const frame = this.batch.referenceFramesEntry(this.frames, index);
    switch (frameReader.frameType(frame)) {
      case FrameType.element:
        return this.planElementSubtree(index, frame, cursor, selectValue);
      case FrameType.text: {
        const content = frameReader.textContent(frame)!;
        const nodes: Node[] = [];
        this.plannedFrames.set(index, { nodes });
        this.queueTextSegment(content, cursor, nodes);
        return true;
      }
      case FrameType.component: {
        const container = document.createComment('!');
        this.queueInsertion(container, cursor);
        this.plannedFrames.set(index, { nodes: [container] });
        return this.planComponent(frameReader.componentId(frame), cursor, selectValue);
      }
      case FrameType.region:
        return this.planFrameRange(index + 1, index + frameReader.subtreeLength(frame), cursor, selectValue);
      case FrameType.markup: {
        const markup = frameReader.markupContent(frame);
        const parent = cursor.parent instanceof Element ? cursor.parent : null;
        const parsed = markup ? this.parseMarkup(
          markup,
          parent !== null && isSvgElement(parent),
          parent !== null && isMathMLElement(parent)
        ) : document.createDocumentFragment();
        const container = document.createComment('!');
        const insertionStart = this.insertions.length;
        this.queueInsertion(container, cursor);
        const nodes: Node[] = [container];
        const preservedElements: Element[] = [];
        for (const expected of Array.from(parsed.childNodes)) {
          this.planMarkupNode(expected, cursor, nodes, preservedElements);
        }
        const markupInsertions = this.insertions.slice(insertionStart);
        this.plannedFrames.set(index, {
          nodes,
          prepareMarkupForRendering: () => {
            for (const { node, elementToConnect } of markupInsertions) {
              if (elementToConnect) {
                node!.parentNode!.replaceChild(elementToConnect, node!);
              }
            }
            for (const element of preservedElements) {
              const logical = toLogicalElement(element, true);
              resetLogicalChildren(logical);
              const container = getLogicalDomNodeContainer(element);
              Array.from(container.childNodes).forEach((child, childIndex) => adoptLogicalChild(child, logical, childIndex));
            }
          },
        });
        return true;
      }
      case FrameType.elementReferenceCapture:
      case FrameType.namedEvent:
        return true;
      default:
        return false;
    }
  }

  private planElementSubtree(index: number, frame: RenderTreeFrame, cursor: Cursor, selectValue?: string | null): boolean {
    const { frameReader } = this.batch;
    this.planPendingText(cursor);
    const element = cursor.next;
    const tagName = frameReader.elementName(frame)!;
    const parentElement = cursor.parent instanceof Element ? cursor.parent : null;
    const namespace = getElementNamespace(tagName, parentElement);
    if (!(element instanceof Element) || element === cursor.end || element.namespaceURI !== namespace ||
        element.localName !== (namespace === 'http://www.w3.org/1999/xhtml' ? tagName.toLowerCase() : tagName)) {
      return this.planElementReplacement(index, cursor);
    }

    const end = index + frameReader.subtreeLength(frame);
    const attributes = new Map<string, string>();
    let value: string | null = null;
    let normalizeFormAction = false;
    let childIndex = index + 1;
    for (; childIndex < end; childIndex++) {
      const attribute = this.batch.referenceFramesEntry(this.frames, childIndex);
      if (frameReader.frameType(attribute) !== FrameType.attribute) {
        break;
      }
      const name = frameReader.attributeName(attribute)!;
      const attributeValue = frameReader.attributeValue(attribute);
      if (name === 'value') {
        value = attributeValue;
      }
      if (!frameReader.attributeEventHandlerId(attribute) && attributeValue !== null) {
        attributes.set(namespace === 'http://www.w3.org/1999/xhtml' ? name.toLowerCase() : name, attributeValue);
      }
    }
    if (element instanceof HTMLTextAreaElement) {
      attributes.delete('value');
    }
    if (element instanceof HTMLOptionElement && selectValue !== null && selectValue !== undefined && value === selectValue) {
      attributes.set('selected', '');
    }
    if (element instanceof HTMLFormElement && !attributes.get('action') &&
        element.getAttribute('action') === location.pathname + location.search) {
      attributes.set('action', element.getAttribute('action')!);
      normalizeFormAction = true;
    }
    for (const name of Array.from(attributes.keys())) {
      if (name.startsWith(internalAttributeNamePrefix) && !element.hasAttribute(name)) {
        attributes.delete(name);
      }
    }
    if (!matchesAttributes(element, attributes) ||
        (element instanceof HTMLTextAreaElement &&
          element.defaultValue !== (value ?? this.textContent(childIndex, end))) ||
        (element instanceof HTMLSelectElement && value !== null && !matchesDefaultSelection(element, value))) {
      return this.planElementReplacement(index, cursor);
    }

    this.plannedFrames.set(index, { nodes: [element] });
    this.preservedElements.push(element);
    this.hasPreservedInitializer ||= mayInitializeDom(element);
    if (normalizeFormAction) {
      this.formActions.push(element);
    }
    const container = getLogicalDomNodeContainer(element);
    const children: Cursor = { parent: container, next: container.firstChild };
    if (!(element instanceof HTMLTextAreaElement && value !== null)) {
      if (!this.planFrameRange(childIndex, end, children, element instanceof HTMLSelectElement ? value : selectValue)) {
        return false;
      }
      this.queueRemainingNodeRemovals(children);
    }
    cursor.next = element.nextSibling;
    return true;
  }

  private planFrameRange(start: number, end: number, cursor: Cursor, selectValue?: string | null): boolean {
    for (let index = start; index < end; index++) {
      if (!this.planFrame(index, cursor, selectValue)) {
        return false;
      }
      const frame = this.batch.referenceFramesEntry(this.frames, index);
      index += countDescendantFrames(this.batch, frame);
    }
    return true;
  }

  private queueTextSegment(content: string, cursor: Cursor, nodes: Node[]): void {
    if (!cursor.pendingText) {
      cursor.pendingText = { content: '', segments: [], insertions: [] };
    }
    const run = cursor.pendingText;
    // Reserve the insertion order before intervening component or markup boundaries are queued.
    const insertion = this.queueInsertion(null, cursor);
    run.segments.push({ content, nodes, insertion });
    run.content += content;
  }

  private textContent(start: number, end: number): string | undefined {
    let content = '';
    for (let index = start; index < end; index++) {
      const frame = this.batch.referenceFramesEntry(this.frames, index);
      switch (this.batch.frameReader.frameType(frame)) {
        case FrameType.text:
          content += this.batch.frameReader.textContent(frame);
          break;
        case FrameType.region:
        case FrameType.elementReferenceCapture:
        case FrameType.namedEvent:
          break;
        default:
          return undefined;
      }
    }
    return content;
  }

  private planPendingText(cursor: Cursor): void {
    const run = cursor.pendingText;
    if (!run) {
      return;
    }
    delete cursor.pendingText;
    const text = cursor.next;
    const textMatches = text instanceof Text && text.data === run.content;
    for (const segment of run.segments) {
      if (textMatches && segment.content) {
        segment.nodeIndex = segment.nodes.length;
        segment.nodes.push(text);
      } else {
        const node = document.createTextNode(segment.content);
        segment.insertion.node = node;
        segment.nodes.push(node);
      }
    }
    for (const insertion of run.insertions) {
      if (textMatches && insertion.offset === text.length) {
        insertion.next = text.nextSibling;
        insertion.offset = 0;
      } else if (!textMatches) {
        insertion.offset = 0;
      }
    }
    if (run.content) {
      if (textMatches) {
        this.matchingTextRuns.push({ text, run });
        cursor.next = text.nextSibling;
      } else {
        this.hasDifferences = true;
        this.queueNextNodeRemoval(cursor);
      }
    }
  }

  private queueInsertion(node: Node | null, cursor: Cursor): Insertion {
    // Keep new elements disconnected until normal rendering reaches them so initialization cannot bind nodes queued for removal.
    const elementToConnect = node instanceof Element ? node : undefined;
    const insertion = {
      node: elementToConnect ? document.createComment('!') : node,
      elementToConnect,
      parent: cursor.parent,
      next: cursor.next,
      offset: cursor.pendingText?.content.length ?? 0,
    };
    cursor.pendingText?.insertions.push(insertion);
    this.insertions.push(insertion);
    return insertion;
  }

  private planElementReplacement(index: number, cursor: Cursor): boolean {
    this.hasDifferences = true;
    const placeholder = document.createComment('!');
    this.queueInsertion(placeholder, cursor);
    this.plannedFrames.set(index, { nodes: [placeholder] });
    this.queueNextNodeRemoval(cursor);
    return true;
  }

  private queueNextNodeRemoval(cursor: Cursor): void {
    if (cursor.next && cursor.next !== cursor.end) {
      this.hasDifferences = true;
      this.removals.push(cursor.next);
      cursor.next = cursor.next.nextSibling;
    }
  }

  private queueRemainingNodeRemovals(cursor: Cursor): void {
    this.planPendingText(cursor);
    while (cursor.next && cursor.next !== cursor.end) {
      this.queueNextNodeRemoval(cursor);
    }
  }

  private planMarkupNode(expected: Node, cursor: Cursor, nodes: Node[], preservedElements: Element[]): void {
    if (expected instanceof Text) {
      this.queueTextSegment(expected.data, cursor, nodes);
      return;
    }
    this.planPendingText(cursor);
    const actual = cursor.next;
    if (actual && actual !== cursor.end && actual.nodeType === expected.nodeType &&
        (expected instanceof Element
          ? actual instanceof Element && actual.localName === expected.localName && actual.namespaceURI === expected.namespaceURI &&
            matchesAttributes(actual, new Map(Array.from(expected.attributes, attribute => [attribute.name, attribute.value]))) &&
            (!(actual instanceof HTMLTextAreaElement) || expected instanceof HTMLTextAreaElement && actual.defaultValue === expected.defaultValue)
          : actual.nodeValue === expected.nodeValue)) {
      nodes.push(actual);
      if (actual instanceof Element && expected instanceof Element) {
        preservedElements.push(actual);
        this.hasPreservedInitializer ||= mayInitializeDom(actual);
        const container = getLogicalDomNodeContainer(actual);
        const expectedContainer = getLogicalDomNodeContainer(expected);
        const children: Cursor = { parent: container, next: container.firstChild };
        for (const child of Array.from(expectedContainer.childNodes)) {
          this.planMarkupNode(child, children, [], preservedElements);
        }
        this.queueRemainingNodeRemovals(children);
      }
      cursor.next = actual.nextSibling;
    } else {
      this.hasDifferences = true;
      this.queueInsertion(expected, cursor);
      nodes.push(expected);
      this.queueNextNodeRemoval(cursor);
    }
  }
}

function normalizeInputDefaults(element: HTMLInputElement): void {
  // Mark the live state dirty before removing SSR defaults so attribute changes cannot reset it.
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
        // These input types also store property-assigned values in the attribute.
        return;
      case 'file':
        break;
      default: {
        // Incomplete native edits already mark the value dirty, and assigning it would discard them.
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

function matchesDefaultSelection(element: HTMLSelectElement, value: string): boolean {
  const options = Array.from(element.options);
  if (element.multiple) {
    const values: string[] = (value ? JSON.parse(value) : null) || [];
    return options.every(option => option.defaultSelected === values.includes(option.value));
  }
  let defaultIndex = options.findLastIndex(option => option.defaultSelected);
  if (defaultIndex < 0 && element.size <= 1) {
    defaultIndex = options.findIndex(option => !option.disabled && !option.closest('optgroup')?.disabled);
  }
  return defaultIndex === options.findIndex(option => option.value === value);
}

function mayInitializeDom(element: Element): boolean {
  return element.localName === 'script' || element.localName.includes('-') || element.hasAttribute('is');
}

function matchesAttributes(element: Element, attributes: Map<string, string>): boolean {
  return element.attributes.length === attributes.size &&
    Array.from(element.attributes).every(attribute => attributes.get(attribute.name) === attribute.value);
}
