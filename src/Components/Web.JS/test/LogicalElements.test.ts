import { expect, test, describe } from '@jest/globals';
import {
  toLogicalElement,
  insertLogicalChild,
  insertLogicalChildBefore,
  removeLogicalChild,
  emptyLogicalElement,
  getLogicalChildrenArray,
  getLogicalChild,
  LogicalElement,
} from '../src/Rendering/LogicalElements';

describe('insertLogicalChild', () => {
  test('should insert child at the correct position when no orphans exist', () => {
    const parent = document.createElement('div');
    const existingChild = document.createElement('span');
    parent.appendChild(existingChild);
    const logicalParent = toLogicalElement(parent, true);

    const newChild = document.createElement('p');
    insertLogicalChild(newChild, logicalParent, 0);

    // New child should be inserted before the existing span in the DOM
    expect(parent.childNodes[0]).toBe(newChild);
    expect(parent.childNodes[1]).toBe(existingChild);

    // Logical children should reflect the correct order
    const children = getLogicalChildrenArray(logicalParent);
    expect(children.length).toBe(2);
    expect(children[0] as any as Node).toBe(newChild);
    expect(children[1] as any as Node).toBe(existingChild);
  });

  test('should remove orphaned sibling at the reference position and insert in correct DOM order', () => {
    // Set up: parent with [orphanComment, connectedSpan] in logical children
    const parent = document.createElement('div');
    const orphanComment = document.createComment('orphan');
    const connectedSpan = document.createElement('span');

    // Add both to the DOM initially so toLogicalElement picks them up
    parent.appendChild(orphanComment);
    parent.appendChild(connectedSpan);
    const logicalParent = toLogicalElement(parent, true);

    // Now orphan the comment by removing it from the DOM (simulates discoverWebAssemblyOptions)
    parent.removeChild(orphanComment);

    // Insert a new element at index 0 — the orphan is at position 0
    const newChild = document.createElement('p');
    insertLogicalChild(newChild, logicalParent, 0);

    // The orphan should have been cleaned up from logical children
    const children = getLogicalChildrenArray(logicalParent);
    expect(children).not.toContain(orphanComment as any);

    // New child should be before the connected span in the DOM
    expect(parent.childNodes[0]).toBe(newChild);
    expect(parent.childNodes[1]).toBe(connectedSpan);

    // Logical children: [newChild, connectedSpan]
    expect(children.length).toBe(2);
    expect(children[0] as any as Node).toBe(newChild);
    expect(children[1] as any as Node).toBe(connectedSpan);
  });

  test('should adjust insertion index when orphans precede the target position', () => {
    // Set up: parent with [orphan1, orphan2, spanA, spanB] in logical children
    const parent = document.createElement('div');
    const orphan1 = document.createComment('orphan1');
    const orphan2 = document.createComment('orphan2');
    const spanA = document.createElement('span');
    const spanB = document.createElement('em');

    parent.appendChild(orphan1);
    parent.appendChild(orphan2);
    parent.appendChild(spanA);
    parent.appendChild(spanB);
    const logicalParent = toLogicalElement(parent, true);

    // Orphan the first two comments
    parent.removeChild(orphan1);
    parent.removeChild(orphan2);

    // Insert at logical index 2 (which, after cleanup of 2 orphans, becomes index 0)
    // This should insert BEFORE spanA
    const newChild = document.createElement('p');
    insertLogicalChild(newChild, logicalParent, 2);

    const children = getLogicalChildrenArray(logicalParent);

    // Orphans should be removed
    expect(children).not.toContain(orphan1 as any);
    expect(children).not.toContain(orphan2 as any);

    // Logical children: [newChild, spanA, spanB]
    expect(children.length).toBe(3);
    expect(children[0] as any as Node).toBe(newChild);
    expect(children[1] as any as Node).toBe(spanA);
    expect(children[2] as any as Node).toBe(spanB);

    // DOM order should match
    expect(parent.childNodes[0]).toBe(newChild);
    expect(parent.childNodes[1]).toBe(spanA);
    expect(parent.childNodes[2]).toBe(spanB);
  });

  test('should append when all siblings after orphan cleanup are before insertion point', () => {
    // Set up: parent with [connectedSpan, orphanComment] in logical children
    const parent = document.createElement('div');
    const connectedSpan = document.createElement('span');
    const orphanComment = document.createComment('orphan');

    parent.appendChild(connectedSpan);
    parent.appendChild(orphanComment);
    const logicalParent = toLogicalElement(parent, true);

    // Orphan the comment
    parent.removeChild(orphanComment);

    // Insert at index 1 — after cleanup, there's only 1 sibling so index 1 means append
    const newChild = document.createElement('p');
    insertLogicalChild(newChild, logicalParent, 1);

    const children = getLogicalChildrenArray(logicalParent);
    expect(children).not.toContain(orphanComment as any);

    // Logical children: [connectedSpan, newChild]
    expect(children.length).toBe(2);
    expect(children[0] as any as Node).toBe(connectedSpan);
    expect(children[1] as any as Node).toBe(newChild);

    // DOM order
    expect(parent.childNodes[0]).toBe(connectedSpan);
    expect(parent.childNodes[1]).toBe(newChild);
  });

  test('should handle multiple orphans interspersed with connected nodes', () => {
    const parent = document.createElement('div');
    const orphan1 = document.createComment('orphan1');
    const spanA = document.createElement('span');
    const orphan2 = document.createComment('orphan2');
    const spanB = document.createElement('em');

    parent.appendChild(orphan1);
    parent.appendChild(spanA);
    parent.appendChild(orphan2);
    parent.appendChild(spanB);
    const logicalParent = toLogicalElement(parent, true);

    // Orphan both comments
    parent.removeChild(orphan1);
    parent.removeChild(orphan2);

    // Insert at index 1 (between orphan1 and spanA originally).
    // After cleanup of orphan1 (index 0, before target) → index becomes 0.
    // orphan2 at original index 2 is also removed but doesn't affect adjusted index.
    // So we insert at index 0 before spanA.
    const newChild = document.createElement('p');
    insertLogicalChild(newChild, logicalParent, 1);

    const children = getLogicalChildrenArray(logicalParent);
    expect(children.length).toBe(3);
    expect(children[0] as any as Node).toBe(newChild);
    expect(children[1] as any as Node).toBe(spanA);
    expect(children[2] as any as Node).toBe(spanB);

    expect(parent.childNodes[0]).toBe(newChild);
    expect(parent.childNodes[1]).toBe(spanA);
    expect(parent.childNodes[2]).toBe(spanB);
  });
});

describe('HTMLTemplateElement handling (issue #50831)', () => {
  test('appendChild via logical element should put children into template.content, not template.childNodes', () => {
    // Repro of issue #50831: rendering a <template> with a RenderFragment child appended the
    // child directly to the <template> element instead of the standard HTMLTemplateElement
    // 'content' DocumentFragment.
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const child = document.createElement('div');
    child.textContent = 'hello';
    insertLogicalChild(child, logicalTemplate, 0);

    // The standard <template> contract: children live in template.content, not in the
    // template element's own childNodes. The template's own childNodes must always be empty.
    expect(template.childNodes.length).toBe(0);
    expect(template.content.childNodes.length).toBe(1);
    expect(template.content.firstChild).toBe(child);
    expect(template.content.firstChild!.textContent).toBe('hello');

    // The logical children tracking should still reflect the inserted child.
    const children = getLogicalChildrenArray(logicalTemplate);
    expect(children.length).toBe(1);
    expect(children[0] as any as Node).toBe(child);
  });

  test('inserting multiple children appends them all into template.content', () => {
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const first = document.createElement('div');
    first.textContent = 'one';
    const second = document.createElement('div');
    second.textContent = 'two';
    const third = document.createElement('div');
    third.textContent = 'three';

    insertLogicalChild(first, logicalTemplate, 0);
    insertLogicalChild(second, logicalTemplate, 1);
    insertLogicalChild(third, logicalTemplate, 2);

    expect(template.childNodes.length).toBe(0);
    expect(template.content.childNodes.length).toBe(3);
    expect(template.content.childNodes[0]).toBe(first);
    expect(template.content.childNodes[1]).toBe(second);
    expect(template.content.childNodes[2]).toBe(third);
  });

  test('insertLogicalChildBefore places the new child correctly inside template.content', () => {
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const first = document.createElement('div');
    first.textContent = 'first';
    const third = document.createElement('div');
    third.textContent = 'third';
    insertLogicalChild(first, logicalTemplate, 0);
    insertLogicalChild(third, logicalTemplate, 1);

    const second = document.createElement('div');
    second.textContent = 'second';
    insertLogicalChildBefore(second, logicalTemplate, third as unknown as LogicalElement);

    expect(template.childNodes.length).toBe(0);
    expect(template.content.childNodes.length).toBe(3);
    expect(template.content.childNodes[0]).toBe(first);
    expect(template.content.childNodes[1]).toBe(second);
    expect(template.content.childNodes[2]).toBe(third);
  });

  test('removeLogicalChild should remove from template.content', () => {
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const a = document.createElement('div'); a.textContent = 'a';
    const b = document.createElement('div'); b.textContent = 'b';
    insertLogicalChild(a, logicalTemplate, 0);
    insertLogicalChild(b, logicalTemplate, 1);

    removeLogicalChild(logicalTemplate, 0);

    expect(template.childNodes.length).toBe(0);
    expect(template.content.childNodes.length).toBe(1);
    expect(template.content.firstChild).toBe(b);

    const children = getLogicalChildrenArray(logicalTemplate);
    expect(children.length).toBe(1);
    expect(children[0] as any as Node).toBe(b);
  });

  test('emptyLogicalElement should clear template.content', () => {
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const a = document.createElement('div'); a.textContent = 'a';
    const b = document.createElement('div'); b.textContent = 'b';
    insertLogicalChild(a, logicalTemplate, 0);
    insertLogicalChild(b, logicalTemplate, 1);

    emptyLogicalElement(logicalTemplate);

    expect(template.childNodes.length).toBe(0);
    expect(template.content.childNodes.length).toBe(0);
    expect(getLogicalChildrenArray(logicalTemplate).length).toBe(0);
  });

  test('content inserted into a <template> should be readable from template.content using standard semantics', () => {
    const template = document.createElement('template');
    const logicalTemplate = toLogicalElement(template);

    const child = document.createElement('div');
    child.textContent = 'test';
    insertLogicalChild(child, logicalTemplate, 0);
    const cloned = template.content.cloneNode(true);
    expect(cloned.childNodes.length).toBe(1);
    expect((cloned.firstChild as HTMLElement).textContent).toBe('test');
  });
});
