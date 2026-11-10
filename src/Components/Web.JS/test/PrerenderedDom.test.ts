// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, test } from '@jest/globals';
import { Buffer } from 'buffer';
import { BrowserRenderer } from '../src/Rendering/BrowserRenderer';
import { getLogicalChildrenArray, toLogicalElement, toLogicalRootCommentElement } from '../src/Rendering/LogicalElements';
import { EditType, FrameType } from '../src/Rendering/RenderBatch/RenderBatch';
import { OutOfProcessRenderBatch } from '../src/Rendering/RenderBatch/OutOfProcessRenderBatch';
import { discoverComponents } from '../src/Services/ComponentDescriptorDiscovery';

type Frame =
  | { type: FrameType.element; name: string; attributes: Record<string, string>; children: Frame[] }
  | { type: FrameType.text | FrameType.markup; content: string }
  | { type: FrameType.component; componentId: number };

interface Edit {
  type: EditType;
  siblingIndex: number;
  frameIndex?: number;
}

interface Component {
  id: number;
  frames: Frame[];
  edits?: Edit[];
}

describe('prerendered DOM structure', () => {
  let container: HTMLDivElement;
  let renderer: BrowserRenderer;

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
    renderer = new BrowserRenderer(0);
  });

  afterEach(() => {
    renderer.disposeComponent(1);
    container.remove();
  });

  function find(selector: string): Element {
    const element = container.querySelector(selector);
    if (!element) {
      throw new Error(`Missing test element: ${selector}`);
    }
    return element;
  }

  function attach(html: string): Element {
    container.innerHTML = `<!--Blazor:{"type":"server","sequence":0,"descriptor":"test","prerenderId":"root"}-->${html}<!--Blazor:{"prerenderId":"root"}--><footer>Outside</footer>`;
    const [descriptor] = discoverComponents(container, 'server');
    renderer.attachRootComponentToLogicalElement(1, toLogicalRootCommentElement(descriptor), false);
    return find('#root');
  }

  function render(frames: Frame[], children: Component[] = [], edits?: Edit[]): void {
    const batch = createBatch([{ id: 1, frames, edits }, ...children]);
    const beforePlanning = container.innerHTML;
    renderer.beginBatch(batch);
    expect(container.innerHTML).toBe(beforePlanning);
    const range = batch.updatedComponents();
    const values = batch.arrayRangeReader.values(range);
    const referenceFrames = batch.arrayRangeReader.values(batch.referenceFrames());
    for (let index = 0; index < batch.arrayRangeReader.count(range); index++) {
      const diff = batch.updatedComponentsEntry(values, index);
      renderer.updateComponent(batch, batch.diffReader.componentId(diff), batch.diffReader.edits(diff), referenceFrames);
    }
    renderer.endBatch();
  }

  test.each([false, true])('raw-markup text differences require whole-root fallback with a retained initializer: %s', matching => {
    const root = attach('<section id="root"><b>Same markup</b><script></script></section>');
    const footer = container.querySelector('footer');
    render([element('section', [markup(matching ? '<b>Same markup</b>' : '<b>Different markup</b>'), element('script')], { id: 'root' })]);

    expect(container.querySelector('#root') === root).toBe(matching);
    expect(container.querySelector('b')?.textContent).toBe(matching ? 'Same markup' : 'Different markup');
    expect(container.querySelector('footer')).toBe(footer);
  });

  test.each([false, true])('same-name nodes must have the expected namespace: %s', matching => {
    const root = attach('<section id="root"><svg><text>Same</text></svg><script></script></section>');
    const original = find('text');
    if (!matching) {
      const replacement = document.createElementNS('http://www.w3.org/1999/xhtml', 'text');
      replacement.textContent = original.textContent;
      original.replaceWith(replacement);
    }
    const actual = container.querySelector('text');
    render([element('section', [element('svg', [element('text', [text('Same')])]), element('script')], { id: 'root' })]);

    expect(container.querySelector('#root') === root).toBe(matching);
    expect(container.querySelector('text') === actual).toBe(matching);
    expect(container.querySelector('text')?.namespaceURI).toBe('http://www.w3.org/2000/svg');
  });

  test('attribute enumeration order does not affect adoption', () => {
    const root = attach('<section id="root" class="same"><span>Same</span></section>');
    const child = root.firstChild;
    root.removeAttribute('id');
    root.setAttribute('id', 'root');
    render([element('section', [element('span', [text('Same')])], { id: 'root', class: 'same' })]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.firstChild).toBe(child);
  });

  test('a root attribute mismatch replaces only content inside the component markers', () => {
    const root = attach('<section id="root" class="before"><span>Same</span></section>');
    const footer = container.querySelector('footer');
    render([element('section', [element('span', [text('Same')])], { id: 'root', class: 'after' })]);

    expect(container.querySelector('#root')).not.toBe(root);
    expect(container.querySelector('#root')?.getAttribute('class')).toBe('after');
    expect(container.querySelector('footer')).toBe(footer);
    expect(container.children).toHaveLength(2);
  });

  test('a leading insertion does not search ahead or retain a shifted node', () => {
    const root = attach('<section id="root"><em id="note">Note</em></section>');
    const note = root.querySelector('#note');
    render([element('section', [element('span', [text('Before')]), element('em', [text('Note')], { id: 'note' })], { id: 'root' })]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.querySelector('#note')).not.toBe(note);
    expect(Array.from(root.children, child => child.localName)).toEqual(['span', 'em']);
    expect(root.textContent).toBe('BeforeNote');
  });

  test.each([true, false])('a middle removal or insertion retains the prefix but replaces shifted siblings: remove=%s', remove => {
    const before = remove ? [
      1,
      2,
      3,
    ] : [1, 3];
    const after = remove ? [1, 3] : [
      1,
      2,
      3,
    ];
    const root = attach(`<section id="root">${before.map(id => `<span id="item-${id}">${id}</span>`).join('')}</section>`);
    const first = root.querySelector('#item-1');
    const last = root.querySelector('#item-3');
    render([element('section', after.map(id => element('span', [text(`${id}`)], { id: `item-${id}` })), { id: 'root' })]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.querySelector('#item-1')).toBe(first);
    expect(root.querySelector('#item-3')).not.toBe(last);
    expect(Array.from(root.children, child => child.id)).toEqual(after.map(id => `item-${id}`));
  });

  test.each([false, true])('coalesced text is split at frame boundaries and retained only when matching: %s', matching => {
    const root = attach('<section id="root">Hello Alice<em>After</em></section>');
    const original = root.firstChild;
    const last = root.lastChild;
    render([
      element('section', [
        text('Hello '),
        text(matching ? 'Alice' : 'Carol'),
        element('em', [text('After')]),
      ], { id: 'root' }),
    ]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.firstChild === original).toBe(matching);
    expect(Array.from(root.childNodes, node => node.textContent)).toEqual([
      'Hello ',
      matching ? 'Alice' : 'Carol',
      'After',
    ]);
    expect(root.lastChild).toBe(last);
  });

  test.each([false, true])('an empty component does not consume a following sibling: empty=%s', empty => {
    const root = attach('<section id="root"><span id="child">Child</span><em id="note">Note</em></section>');
    const note = root.querySelector('#note');
    render([element('section', [{ type: FrameType.component, componentId: 2 }, element('em', [text('Note')], { id: 'note' })], { id: 'root' })], [{ id: 2, frames: empty ? [] : [element('span', [text('Child')], { id: 'child' })] }]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.querySelector('#note') === note).toBe(!empty);
    expect(root.textContent).toBe(empty ? 'Note' : 'ChildNote');
    const logicalChildren = getLogicalChildrenArray(toLogicalElement(root));
    expect(logicalChildren).toHaveLength(2);
    expect(logicalChildren[0]).toBeInstanceOf(Comment);
    expect(getLogicalChildrenArray(logicalChildren[0])).toHaveLength(empty ? 0 : 1);
    expect(logicalChildren[1]).toBe(root.querySelector('#note'));
    renderer.disposeComponent(2);
  });

  test('multiple markup siblings rebuild logical children and can subsequently be removed', () => {
    const root = attach('<section id="root"><span>State</span><em id="note">Note</em><i>After</i></section>');
    const note = find('#note');
    const last = root.lastChild;
    render([element('section', [markup('<strong>State</strong><em id="note">Note</em><i>After</i>')], { id: 'root' })]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.querySelector('#note')).toBe(note);
    expect(root.lastChild).toBe(last);
    expect(Array.from(root.children, child => child.localName)).toEqual([
      'strong',
      'em',
      'i',
    ]);
    const [markupContainer] = getLogicalChildrenArray(toLogicalElement(root));
    expect(getLogicalChildrenArray(markupContainer)).toEqual(Array.from(root.children));
    expect(getLogicalChildrenArray(toLogicalElement(note))).toEqual(Array.from(note.childNodes));

    render([], [], [
      { type: EditType.stepIn, siblingIndex: 0 },
      { type: EditType.removeFrame, siblingIndex: 0 },
      { type: EditType.stepOut, siblingIndex: 0 },
    ]);

    expect(container.querySelector('#root')).toBe(root);
    expect(root.childNodes).toHaveLength(0);
    expect(note.isConnected).toBe(false);
    expect(getLogicalChildrenArray(toLogicalElement(root))).toHaveLength(0);
  });
});

function element(name: string, children: Frame[] = [], attributes: Record<string, string> = {}): Frame {
  return { type: FrameType.element, name, attributes, children };
}

function text(content: string): Frame {
  return { type: FrameType.text, content };
}

function markup(content: string): Frame {
  return { type: FrameType.markup, content };
}

// Serialize test frames so the renderer uses the production batch readers and opaque handles.
function createBatch(components: Component[]): OutOfProcessRenderBatch {
  const strings: string[] = [];
  const frames: number[][] = [];
  function stringIndex(value: string): number {
    return strings.push(value) - 1;
  }
  function appendFrame(frame: Frame): number {
    const index = frames.length;
    switch (frame.type) {
      case FrameType.element: {
        const entry = [
          FrameType.element,
          0,
          stringIndex(frame.name),
          0,
          0,
        ];
        frames.push(entry);
        for (const [name, value] of Object.entries(frame.attributes)) {
          frames.push([
            FrameType.attribute,
            stringIndex(name),
            stringIndex(value),
            0,
            0,
          ]);
        }
        frame.children.forEach(appendFrame);
        entry[1] = frames.length - index;
        break;
      }
      case FrameType.component:
        frames.push([
          FrameType.component,
          1,
          frame.componentId,
          0,
          0,
        ]);
        break;
      default:
        frames.push([
          frame.type,
          stringIndex(frame.content),
          0,
          0,
          0,
        ]);
        break;
    }
    return index;
  }
  const diffs = components.map(component => {
    const frameIndices = component.frames.map(appendFrame);
    return {
      id: component.id,
      edits: component.edits ?? frameIndices.map((frameIndex, siblingIndex) => ({
        type: EditType.prependFrame,
        siblingIndex,
        frameIndex,
      })),
    };
  });
  const bytes: number[] = [];
  function writeInt(value: number): void {
    bytes.push(value & 255, (value >>> 8) & 255, (value >>> 16) & 255, (value >>> 24) & 255);
  }
  writeInt(frames.length);
  frames.forEach(frame => frame.forEach(writeInt));
  const diffOffsets = diffs.map(diff => {
    const offset = bytes.length;
    writeInt(diff.id);
    writeInt(diff.edits.length);
    for (const edit of diff.edits) {
      writeInt(edit.type);
      writeInt(edit.siblingIndex);
      writeInt(edit.frameIndex ?? 0);
      writeInt(-1);
    }
    return offset;
  });
  const updatedComponentsOffset = bytes.length;
  writeInt(diffs.length);
  diffOffsets.forEach(writeInt);
  const emptyRangeOffset = bytes.length;
  writeInt(0);
  const stringOffsets = strings.map(value => {
    const offset = bytes.length;
    const encoded = Buffer.from(value, 'utf8');
    let length = encoded.length;
    do {
      bytes.push((length & 127) | (length > 127 ? 128 : 0));
      length >>>= 7;
    } while (length);
    bytes.push(...encoded);
    return offset;
  });
  const stringTableOffset = bytes.length;
  stringOffsets.forEach(writeInt);
  [
    updatedComponentsOffset,
    0,
    emptyRangeOffset,
    emptyRangeOffset,
    stringTableOffset,
  ].forEach(writeInt);
  return new OutOfProcessRenderBatch(Uint8Array.from(bytes));
}
