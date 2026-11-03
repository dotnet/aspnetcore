// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ArrayValues, EditType, FrameType, RenderBatch, RenderTreeDiff, RenderTreeFrame } from './RenderBatch/RenderBatch';
import { LogicalElement, getLogicalRootDescriptor, resetLogicalChildren, toLogicalElement } from './LogicalElements';

interface Position {
  parent: Node;
  next: Node | null;
  offset: number;
}

interface TextPart {
  text: Text;
  offset: number;
  length: number;
}

type MatchedNode = Node | TextPart;

interface FrameMatch {
  nodes: MatchedNode[];
  position?: Position;
  emptyText?: boolean;
}

export type PrerenderedFrameMatches = Map<number, Node[]>;

export class PrerenderedDom {
  private readonly matches = new Map<number, FrameMatch>();

  private readonly components = new Set<number>();

  private readonly elements: Element[] = [];

  private readonly formActions: Element[] = [];

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
    if (!end || !descriptor.start.parentNode) {
      return false;
    }

    const cursor: Position = { parent: descriptor.start.parentNode, next: descriptor.start.nextSibling, offset: 0 };
    return this.matchComponent(componentId, cursor) && cursor.next === end && cursor.offset === 0;
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
      if (match.position?.next instanceof Text) {
        addSplit(match.position.next, match.position.offset);
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

    const result: PrerenderedFrameMatches = new Map();
    for (const [index, match] of this.matches) {
      const nodes = match.nodes.map(node => node instanceof Node ? node : textParts.get(node.text)!.get(node.offset)!);
      if (match.position) {
        const { parent, next, offset } = match.position;
        const before = next instanceof Text && offset > 0 ? textParts.get(next)!.get(offset)! : next;
        const marker = match.emptyText ? document.createTextNode('') : document.createComment('!');
        parent.insertBefore(marker, before);
        nodes.unshift(marker);
      }
      result.set(index, nodes);
    }

    resetLogicalChildren(root);
    for (const element of this.elements) {
      resetLogicalChildren(toLogicalElement(element, true));
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

  private matchComponent(componentId: number, cursor: Position, selectValue?: string | null): boolean {
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

  private matchFrame(index: number, cursor: Position, selectValue?: string | null): number {
    const { frameReader } = this.batch;
    const frame = this.batch.referenceFramesEntry(this.frames, index);
    switch (frameReader.frameType(frame)) {
      case FrameType.element: {
        const element = cursor.next;
        const tagName = frameReader.elementName(frame)!;
        const parentElement = cursor.parent instanceof Element ? cursor.parent : null;
        const namespace = tagName === 'svg' || (parentElement?.namespaceURI === 'http://www.w3.org/2000/svg' && parentElement.localName !== 'foreignObject')
          ? 'http://www.w3.org/2000/svg'
          : tagName === 'math' || parentElement?.namespaceURI === 'http://www.w3.org/1998/Math/MathML'
            ? 'http://www.w3.org/1998/Math/MathML'
            : 'http://www.w3.org/1999/xhtml';
        if (!(element instanceof Element) || cursor.offset || element.namespaceURI !== namespace ||
            element.localName !== (namespace === 'http://www.w3.org/1999/xhtml' ? tagName.toLowerCase() : tagName)) {
          return -1;
        }

        const end = index + frameReader.subtreeLength(frame);
        const attributes = new Map<string, string>();
        let value: string | null = null;
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
          this.formActions.push(element);
        }
        for (const name of Array.from(attributes.keys())) {
          if (name.startsWith('__internal_') && !element.hasAttribute(name)) {
            attributes.delete(name);
          }
        }
        if (element.attributes.length !== attributes.size ||
            Array.from(element.attributes).some(attribute => attributes.get(attribute.name) !== attribute.value)) {
          return -1;
        }

        this.matches.set(index, { nodes: [element] });
        this.elements.push(element);
        const container = element instanceof HTMLTemplateElement ? element.content : element;
        const children: Position = { parent: container, next: container.firstChild, offset: 0 };
        if (element instanceof HTMLTextAreaElement && value) {
          if (element.textContent !== value || childIndex !== end) {
            return -1;
          }
        } else if (this.matchRange(childIndex, end, children, element instanceof HTMLSelectElement ? value : selectValue) < 0 || children.next) {
          return -1;
        }
        cursor.next = element.nextSibling;
        return 1;
      }
      case FrameType.text: {
        const content = frameReader.textContent(frame)!;
        if (!content) {
          this.matches.set(index, { nodes: [], position: { ...cursor }, emptyText: true });
          return 1;
        }
        const text = this.matchText(content, cursor);
        if (!text) {
          return -1;
        }
        this.matches.set(index, { nodes: [text] });
        return 1;
      }
      case FrameType.component: {
        this.matches.set(index, { nodes: [], position: { ...cursor } });
        return this.matchComponent(frameReader.componentId(frame), cursor, selectValue) ? 1 : -1;
      }
      case FrameType.region:
        return this.matchRange(index + 1, index + frameReader.subtreeLength(frame), cursor, selectValue);
      case FrameType.markup: {
        const markup = frameReader.markupContent(frame);
        const parent = cursor.parent instanceof Element ? cursor.parent : null;
        const parsed = markup ? this.parseMarkup(
          markup,
          parent?.namespaceURI === 'http://www.w3.org/2000/svg' && parent.localName !== 'foreignObject',
          parent?.namespaceURI === 'http://www.w3.org/1998/Math/MathML'
        ) : document.createDocumentFragment();
        const match: FrameMatch = { nodes: [], position: { ...cursor } };
        this.matches.set(index, match);
        for (const expected of Array.from(parsed.childNodes)) {
          if (expected instanceof Text) {
            const text = this.matchText(expected.data, cursor);
            if (!text) {
              return -1;
            }
            match.nodes.push(text);
          } else if (cursor.offset || !cursor.next?.isEqualNode(expected)) {
            return -1;
          } else {
            match.nodes.push(cursor.next);
            cursor.next = cursor.next.nextSibling;
          }
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

  private matchRange(start: number, end: number, cursor: Position, selectValue?: string | null): number {
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

  private matchText(content: string, cursor: Position): TextPart | Text | undefined {
    if (!content) {
      return document.createTextNode('');
    }
    const text = cursor.next;
    if (!(text instanceof Text) || text.data.substring(cursor.offset, cursor.offset + content.length) !== content) {
      return undefined;
    }
    const result: TextPart = { text, offset: cursor.offset, length: content.length };
    cursor.offset += content.length;
    if (cursor.offset === text.length) {
      cursor.next = text.nextSibling;
      cursor.offset = 0;
    }
    return result;
  }
}
