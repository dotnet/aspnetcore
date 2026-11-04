// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ArrayValues, EditType, FrameType, RenderBatch, RenderTreeDiff, RenderTreeFrame } from './RenderBatch/RenderBatch';
import { LogicalElement, adoptLogicalChild, getLogicalRootDescriptor, isSvgElement, isMathMLElement, resetLogicalChildren, toLogicalElement } from './LogicalElements';

interface Cursor {
  parent: Node;
  next: Node | null;
  end?: Node;
  text?: TextRun;
}

interface TextPart {
  text: Text;
  offset: number;
  length: number;
}

type MatchedNode = Node | TextPart;

interface FrameMatch {
  nodes: MatchedNode[];
  replace?: boolean;
}

interface Insertion {
  node: Node;
  parent: Node;
  next: Node | null;
  offset: number;
  enabled: boolean;
}

interface TextRun {
  content: string;
  segments: { content: string; nodes: MatchedNode[]; insertion: Insertion }[];
  insertions: Insertion[];
}

export type PrerenderedFrameMatches = Map<number, { nodes: Node[]; replace: boolean }>;

export class PrerenderedDom {
  private readonly matches = new Map<number, FrameMatch>();

  private readonly components = new Set<number>();

  private readonly elements: Element[] = [];

  private readonly formActions: Element[] = [];

  private readonly markupElements = new Set<Element>();

  private readonly insertions: Insertion[] = [];

  private readonly removals = new Set<Node>();

  private readonly frames: ArrayValues<RenderTreeFrame>;

  public constructor(
    private readonly batch: RenderBatch,
    private readonly diffs: Map<number, RenderTreeDiff | undefined>,
    private readonly parseMarkup: (markup: string, isSvg: boolean, isMathML: boolean) => Node,
  ) {
    this.frames = batch.arrayRangeReader.values(batch.referenceFrames());
  }

  public match(componentId: number, root: LogicalElement): boolean {
    const descriptor = getLogicalRootDescriptor(root);
    const end = descriptor?.end;
    if (!end || !descriptor.start.parentNode || end.parentNode !== descriptor.start.parentNode) {
      return false;
    }

    const cursor: Cursor = { parent: descriptor.start.parentNode, next: descriptor.start.nextSibling, end };
    if (!this.matchComponent(componentId, cursor)) {
      return false;
    }
    this.removeRemaining(cursor);
    return cursor.next === end;
  }

  public adopt(root: LogicalElement): PrerenderedFrameMatches {
    const splits = new Map<Text, Set<number>>();
    for (const match of this.matches.values()) {
      for (const node of match.nodes) {
        if (!(node instanceof Node)) {
          addSplit(node.text, node.offset);
          addSplit(node.text, node.offset + node.length);
        }
      }
    }
    for (const { next, offset, enabled } of this.insertions) {
      if (enabled && next instanceof Text) {
        addSplit(next, offset);
      }
    }

    const textParts = new Map<Text, Map<number, Text>>();
    for (const [text, offsets] of splits) {
      const parts = new Map<number, Text>([[0, text]]);
      for (const offset of Array.from(offsets).sort((a, b) => b - a)) {
        if (offset > 0 && offset < text.length) {
          parts.set(offset, text.splitText(offset));
        }
      }
      textParts.set(text, parts);
    }

    for (const { node, parent, next, offset, enabled } of this.insertions) {
      if (enabled) {
        const before = next instanceof Text && offset > 0 ? textParts.get(next)!.get(offset)! : next;
        parent.insertBefore(node, before);
      }
    }
    for (const node of this.removals) {
      node.parentNode!.removeChild(node);
    }

    const result: PrerenderedFrameMatches = new Map();
    for (const [index, match] of this.matches) {
      const nodes = match.nodes.map(node => node instanceof Node ? node : textParts.get(node.text)!.get(node.offset)!);
      result.set(index, { nodes, replace: match.replace ?? false });
    }

    resetLogicalChildren(root);
    for (const element of this.elements) {
      resetLogicalChildren(toLogicalElement(element, true));
    }
    for (const element of this.markupElements) {
      const logical = toLogicalElement(element, true);
      resetLogicalChildren(logical);
      const container = element instanceof HTMLTemplateElement ? element.content : element;
      Array.from(container.childNodes).forEach((child, index) => adoptLogicalChild(child, logical, index));
    }
    for (const element of this.formActions) {
      element.removeAttribute('action');
    }
    const end = getLogicalRootDescriptor(root).end!;
    end.parentNode!.removeChild(end);
    return result;

    function addSplit(text: Text, offset: number) {
      let offsets = splits.get(text);
      if (!offsets) {
        offsets = new Set();
        splits.set(text, offsets);
      }
      offsets.add(offset);
    }
  }

  private matchComponent(componentId: number, cursor: Cursor, selectValue?: string | null): boolean {
    if (this.components.has(componentId)) {
      return false;
    }
    this.components.add(componentId);
    const diff = this.diffs.get(componentId);
    if (diff === undefined) {
      return !this.diffs.has(componentId);
    }

    const { arrayBuilderSegmentReader: segmentReader, diffReader, editReader } = this.batch;
    const edits = diffReader.edits(diff);
    const values = segmentReader.values(edits);
    const offset = segmentReader.offset(edits);
    const count = segmentReader.count(edits);
    let siblingIndex = 0;
    for (let i = offset; i < offset + count; i++) {
      const edit = diffReader.editsEntry(values, i);
      if (editReader.editType(edit) !== EditType.prependFrame || editReader.siblingIndex(edit) !== siblingIndex) {
        return false;
      }
      const inserted = this.matchFrame(editReader.newTreeIndex(edit), cursor, selectValue);
      if (inserted < 0) {
        return false;
      }
      siblingIndex += inserted;
    }
    return true;
  }

  private matchFrame(index: number, cursor: Cursor, selectValue?: string | null): number {
    const { frameReader } = this.batch;
    const frame = this.batch.referenceFramesEntry(this.frames, index);
    switch (frameReader.frameType(frame)) {
      case FrameType.element: {
        this.flushText(cursor);
        const element = cursor.next;
        const tagName = frameReader.elementName(frame)!;
        const parentElement = cursor.parent instanceof Element ? cursor.parent : null;
        const namespace = tagName === 'svg' || (parentElement !== null && isSvgElement(parentElement))
          ? 'http://www.w3.org/2000/svg'
          : tagName === 'math' || (parentElement !== null && isMathMLElement(parentElement))
            ? 'http://www.w3.org/1998/Math/MathML'
            : 'http://www.w3.org/1999/xhtml';
        if (!(element instanceof Element) || element === cursor.end || element.namespaceURI !== namespace ||
            element.localName !== (namespace === 'http://www.w3.org/1999/xhtml' ? tagName.toLowerCase() : tagName)) {
          return this.replaceFrame(index, cursor);
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
          if (name.startsWith('__internal_') && !element.hasAttribute(name)) {
            attributes.delete(name);
          }
        }
        if (!matchesAttributes(element, attributes) ||
            (element instanceof HTMLTextAreaElement &&
              element.defaultValue !== (value ?? this.textContent(childIndex, end)))) {
          return this.replaceFrame(index, cursor);
        }

        this.matches.set(index, { nodes: [element] });
        this.elements.push(element);
        if (normalizeFormAction) {
          this.formActions.push(element);
        }
        const container = element instanceof HTMLTemplateElement ? element.content : element;
        const children: Cursor = { parent: container, next: container.firstChild };
        if (!(element instanceof HTMLTextAreaElement && value !== null)) {
          if (this.matchRange(childIndex, end, children, element instanceof HTMLSelectElement ? value : selectValue) < 0) {
            return -1;
          }
          this.removeRemaining(children);
        }
        cursor.next = element.nextSibling;
        return 1;
      }
      case FrameType.text: {
        const content = frameReader.textContent(frame)!;
        const nodes: MatchedNode[] = [];
        this.matches.set(index, { nodes });
        this.matchText(content, cursor, nodes);
        return 1;
      }
      case FrameType.component: {
        this.matches.set(index, { nodes: [this.insert(document.createComment('!'), cursor).node] });
        return this.matchComponent(frameReader.componentId(frame), cursor, selectValue) ? 1 : -1;
      }
      case FrameType.region:
        return this.matchRange(index + 1, index + frameReader.subtreeLength(frame), cursor, selectValue);
      case FrameType.markup: {
        const markup = frameReader.markupContent(frame);
        const parent = cursor.parent instanceof Element ? cursor.parent : null;
        const parsed = markup ? this.parseMarkup(
          markup,
          parent !== null && isSvgElement(parent),
          parent !== null && isMathMLElement(parent)
        ) : document.createDocumentFragment();
        const match: FrameMatch = { nodes: [this.insert(document.createComment('!'), cursor).node] };
        this.matches.set(index, match);
        for (const expected of Array.from(parsed.childNodes)) {
          this.matchMarkupNode(expected, cursor, match.nodes);
        }
        return 1;
      }
      case FrameType.elementReferenceCapture:
      case FrameType.namedEvent:
        return 0;
      default:
        return -1;
    }
  }

  private matchRange(start: number, end: number, cursor: Cursor, selectValue?: string | null): number {
    let count = 0;
    for (let index = start; index < end; index++) {
      const inserted = this.matchFrame(index, cursor, selectValue);
      if (inserted < 0) {
        return -1;
      }
      count += inserted;
      const frame = this.batch.referenceFramesEntry(this.frames, index);
      switch (this.batch.frameReader.frameType(frame)) {
        case FrameType.element:
        case FrameType.component:
        case FrameType.region:
          index += this.batch.frameReader.subtreeLength(frame) - 1;
      }
    }
    return count;
  }

  private matchText(content: string, cursor: Cursor, nodes: MatchedNode[]): void {
    if (!cursor.text) {
      cursor.text = { content: '', segments: [], insertions: [] };
    }
    const run = cursor.text;
    const insertion = this.insert(document.createTextNode(content), cursor);
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

  private flushText(cursor: Cursor): void {
    const run = cursor.text;
    if (!run) {
      return;
    }
    delete cursor.text;
    const text = cursor.next;
    const matches = text instanceof Text && text.data === run.content;
    let offset = 0;
    for (const segment of run.segments) {
      if (matches && segment.content) {
        segment.nodes.push({ text, offset, length: segment.content.length });
        segment.insertion.enabled = false;
      } else {
        segment.nodes.push(segment.insertion.node);
      }
      offset += segment.content.length;
    }
    for (const insertion of run.insertions) {
      if (matches && insertion.offset === text.length) {
        insertion.next = text.nextSibling;
        insertion.offset = 0;
      } else if (!matches) {
        insertion.offset = 0;
      }
    }
    if (run.content) {
      if (matches) {
        cursor.next = text.nextSibling;
      } else {
        this.removeNext(cursor);
      }
    }
  }

  private insert(node: Node, cursor: Cursor): Insertion {
    const insertion = { node, parent: cursor.parent, next: cursor.next, offset: cursor.text?.content.length ?? 0, enabled: true };
    cursor.text?.insertions.push(insertion);
    this.insertions.push(insertion);
    return insertion;
  }

  private replaceFrame(index: number, cursor: Cursor): number {
    const placeholder = document.createComment('!');
    this.insert(placeholder, cursor);
    this.matches.set(index, { nodes: [placeholder], replace: true });
    this.removeNext(cursor);
    return 1;
  }

  private removeNext(cursor: Cursor): void {
    if (cursor.next && cursor.next !== cursor.end) {
      this.removals.add(cursor.next);
      cursor.next = cursor.next.nextSibling;
    }
  }

  private removeRemaining(cursor: Cursor): void {
    this.flushText(cursor);
    while (cursor.next && cursor.next !== cursor.end) {
      this.removeNext(cursor);
    }
  }

  private matchMarkupNode(expected: Node, cursor: Cursor, nodes: MatchedNode[]): void {
    if (expected instanceof Text) {
      this.matchText(expected.data, cursor, nodes);
      return;
    }
    this.flushText(cursor);
    const actual = cursor.next;
    if (actual && actual !== cursor.end && actual.nodeType === expected.nodeType &&
        (expected instanceof Element
          ? actual instanceof Element && actual.localName === expected.localName && actual.namespaceURI === expected.namespaceURI &&
            matchesAttributes(actual, new Map(Array.from(expected.attributes, attribute => [attribute.name, attribute.value]))) &&
            (!(actual instanceof HTMLTextAreaElement) || expected instanceof HTMLTextAreaElement && actual.defaultValue === expected.defaultValue)
          : actual.nodeValue === expected.nodeValue)) {
      nodes.push(actual);
      if (actual instanceof Element && expected instanceof Element) {
        this.markupElements.add(actual);
        const container = actual instanceof HTMLTemplateElement ? actual.content : actual;
        const expectedContainer = expected instanceof HTMLTemplateElement ? expected.content : expected;
        const children: Cursor = { parent: container, next: container.firstChild };
        for (const child of Array.from(expectedContainer.childNodes)) {
          this.matchMarkupNode(child, children, []);
        }
        this.removeRemaining(children);
      }
      cursor.next = actual.nextSibling;
    } else {
      this.insert(expected, cursor);
      nodes.push(expected);
      this.removeNext(cursor);
    }
  }
}

function matchesAttributes(element: Element, attributes: Map<string, string>): boolean {
  return element.attributes.length === attributes.size &&
    Array.from(element.attributes).every(attribute => attributes.get(attribute.name) === attribute.value);
}
