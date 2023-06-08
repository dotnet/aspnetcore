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
