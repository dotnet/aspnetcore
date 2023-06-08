import { expect, test, describe } from '@jest/globals';
import { CommentBoundedRange, synchronizeDomContent } from '../src/Rendering/DomMerging/DomSync';

describe('DomSync', () => {
  test('should remove everything if new content is empty', () => {
    // Arrange
    const destination = makeExistingContent(`
      <elem a=1><child>Hello</child></elem>
      Text node
      <!-- comment node -->`);
    const newContent = makeNewContent(``);

    expect(destination.startExclusive.nextSibling).not.toBe(destination.endExclusive);

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(destination.startExclusive.nextSibling).toBe(destination.endExclusive);
  });

  test('should insert everything if old content is empty', () => {
    // Arrange
    const destination = makeExistingContent(``);
    const newContent = makeNewContent(`
      <elem a=1><child>Hello</child></elem>
      Text node
      <!-- comment node -->`);

    expect(destination.startExclusive.nextSibling).toBe(destination.endExclusive);

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(destination.startExclusive.nextSibling).not.toBe(destination.endExclusive);
  });

  test('should retain text and comment nodes, whether or not the text must be updated', () => {
    // Arrange
    const destination = makeExistingContent(`First<!--comment1-->Second<!--comment2-->Third`);
    const newContent = makeNewContent(`First edited<!--comment1 edited-->Second<!--comment2-->Third edited`);
    const originalDestinationNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newDestinationNodes = toNodeArray(destination);

    // Assert
    assertSameContentsByIdentity(newDestinationNodes, originalDestinationNodes);
    expect(newDestinationNodes[0].textContent).toBe('First edited');
    expect(newDestinationNodes[1].textContent).toBe('comment1 edited');
    expect(newDestinationNodes[2].textContent).toBe('Second');
    expect(newDestinationNodes[3].textContent).toBe('comment2');
    expect(newDestinationNodes[4].textContent).toBe('Third edited');
  });
});

function makeExistingContent(html: string): CommentBoundedRange {
  // Returns a structure like:
  //   Unrelated leading content
  //   <!-- start -->
  //   Your HTML
  //   <!-- end -->
  //   Unrelated trailing content
  // (but without all the spacing, and no text in the comment nodes)
  const parent = document.createElement('div');
  parent.innerHTML = html.trim();

  const startComment = document.createComment('');
  const endComment = document.createComment('');

  parent.appendChild(endComment);
  parent.appendChild(document.createTextNode('Unrelated trailing content'));

  parent.insertBefore(startComment, parent.firstChild);
  parent.insertBefore(document.createTextNode('Unrelated leading content'), parent.firstChild);

  return { startExclusive: startComment, endExclusive: endComment };
}

function makeNewContent(html: string): DocumentFragment {
  const template = document.createElement('template');
  template.innerHTML = html;
  return template.content;
}

function toNodeArray(range: CommentBoundedRange): Node[] {
  const result: Node[] = [];
  let next = range.startExclusive.nextSibling!;
  while (next !== range.endExclusive) {
    result.push(next);
    next = next.nextSibling!;
  }

  return result;
}

function assertSameContentsByIdentity<T>(actual: T[], expected: T[]) {
  if (actual.length !== expected.length) {
    throw new Error(`Expected ${actual} to have length ${expected.length}, but found length ${actual.length}`);
  }

  for (let i = 0; i < actual.length; i++) {
    expect(actual[i]).toBe(expected[i]);
  }
}
