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

  test('should retain text and comment nodes while inserting and deleting others, updating textContent in place', () => {
    // Arrange
    const destination = makeExistingContent(`First<!--comment1-->Second<!--comment2--><!--comment3-will-delete-->Third`);
    const newContent = makeNewContent(`<!--inserted-->First edited<!--comment1 edited-->Second<!--comment2-->Third edited`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination);

    // Assert
    expect(newNodes.length).toBe(6);
    expect(newNodes[0].textContent).toBe('inserted');
    expect(newNodes[1].textContent).toBe('First edited');
    expect(newNodes[2].textContent).toBe('comment1 edited');
    expect(newNodes[3].textContent).toBe('Second');
    expect(newNodes[4].textContent).toBe('comment2');
    expect(newNodes[5].textContent).toBe('Third edited');

    expect(newNodes[1]).toBe(oldNodes[0]);
    expect(newNodes[2]).toBe(oldNodes[1]);
    expect(newNodes[3]).toBe(oldNodes[2]);
    expect(newNodes[4]).toBe(oldNodes[3]);
    expect(newNodes[5]).toBe(oldNodes[5]);
  });

  test('should retain elements when nothing has changed', () => {
    // Arrange
    const destination = makeExistingContent(`<a></a><b></b><a></a><b></b>`);
    const newContent = makeNewContent(`<a></a><b></b><a></a><b></b>`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination);

    // Assert
    assertSameContentsByIdentity(newNodes, oldNodes);
  });

  test('should retain elements when inserting new ones', () => {
    // Arrange
    const destination = makeExistingContent(
      `<a></a>` +
      `<b></b>` +
      `<a></a>`);
    const newContent = makeNewContent(
      `<new></new>` +
      `<a></a>` +
      `<new></new>` +
      `<b></b>` +
      `<a></a>` +
      `<new></new>`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination) as Element[];

    // Assert
    expect(newNodes[0].tagName).toBe('NEW');
    expect(newNodes[1]).toBe(oldNodes[0]);
    expect(newNodes[2].tagName).toBe('NEW');
    expect(newNodes[3]).toBe(oldNodes[1]);
    expect(newNodes[4]).toBe(oldNodes[2]);
    expect(newNodes[5].tagName).toBe('NEW');
  });

  test('should retain elements when deleting some', () => {
    // Arrange
    const destination = makeExistingContent(
      `<will-delete></will-delete>` +
      `<a></a>` +
      `<will-delete></will-delete>` +
      `<b></b>` +
      `<a></a>` +
      `<will-delete></will-delete>`);
    const newContent = makeNewContent(
      `<a></a>` +
      `<b></b>` +
      `<a></a>`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination) as Element[];

    // Assert
    expect(newNodes.length).toBe(3);
    expect(newNodes[0]).toBe(oldNodes[1]);
    expect(newNodes[1]).toBe(oldNodes[3]);
    expect(newNodes[2]).toBe(oldNodes[4]);
  });

  test('should update attribute values, respecting namespaces', () => {
    // Arrange
    const destination = makeExistingContent(
      `<elem a='A' b='B' c='C'></elem>`);
    const newContent = makeNewContent(
      `<elem a='A updated' b='B' c='C updated'></elem>`);
    const targetNode = destination.startExclusive.nextSibling as Element;
    const newContentNode = newContent.firstChild as Element;

    targetNode.setAttributeNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix', 'oldval 1');
    targetNode.setAttributeNS('http://example/namespace2', 'exampleprefix:attributeWithNamespaceAndPrefix', 'oldval 2');

    newContentNode.setAttributeNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix', 'updatedval 1');
    newContentNode.setAttributeNS('http://example/namespace2', 'exampleprefix:attributeWithNamespaceAndPrefix', 'updatedval 2');

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(destination.startExclusive.nextSibling).toBe(targetNode); // Preserved the element
    const targetNodeAttribs = targetNode.attributes;
    expect(targetNodeAttribs.length).toBe(5);
    expect(targetNodeAttribs.getNamedItem('a')?.value).toBe('A updated');
    expect(targetNodeAttribs.getNamedItem('b')?.value).toBe('B');
    expect(targetNodeAttribs.getNamedItem('c')?.value).toBe('C updated');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix')?.value).toBe('updatedval 1');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace2', 'attributeWithNamespaceAndPrefix')?.value).toBe('updatedval 2');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace2', 'attributeWithNamespaceAndPrefix')?.name).toBe('exampleprefix:attributeWithNamespaceAndPrefix');
  });

  test('should insert added attributes, including ones with namespace', () => {
    // Arrange
    const destination = makeExistingContent(
      `<elem preserved='preserved value'></elem>`);
    const newContent = makeNewContent(
      `<elem added='added value 1' preserved='preserved value' yetanother='added value 2'></elem>`);
    const targetNode = destination.startExclusive.nextSibling as Element;
    expect(targetNode.attributes.length).toBe(1);

    const newContentNode = newContent.firstChild as Element;
    newContentNode.setAttributeNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix', 'new namespaced value 1');
    newContentNode.setAttributeNS('http://example/namespace2', 'exampleprefix:attributeWithNamespaceAndPrefix', 'new namespaced value 2');

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(destination.startExclusive.nextSibling).toBe(targetNode); // Preserved the element
    expect(newContentNode).not.toBe(targetNode);
    const targetNodeAttribs = targetNode.attributes;
    expect(targetNodeAttribs.length).toBe(5);
    expect(targetNodeAttribs.getNamedItem('preserved')?.value).toBe('preserved value');
    expect(targetNodeAttribs.getNamedItem('added')?.value).toBe('added value 1');
    expect(targetNodeAttribs.getNamedItem('yetanother')?.value).toBe('added value 2');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix')?.value).toBe('new namespaced value 1');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace2', 'attributeWithNamespaceAndPrefix')?.value).toBe('new namespaced value 2');
    expect(targetNodeAttribs.getNamedItemNS('http://example/namespace2', 'attributeWithNamespaceAndPrefix')?.name).toBe('exampleprefix:attributeWithNamespaceAndPrefix');
  });

  test('should delete removed attributes, including ones with namespace', () => {
    // Arrange
    const destination = makeExistingContent(
      `<elem will-delete='val1' preserved='preserved value' another-to-delete='val2'></elem>`);
    const newContent = makeNewContent(
      `<elem preserved='preserved value'></elem>`);
    const targetNode = destination.startExclusive.nextSibling as Element;
    const newContentNode = newContent.firstChild as Element;

    targetNode.setAttributeNS('http://example/namespace1', 'attributeWithNamespaceButNoPrefix', 'new namespaced value 1');
    targetNode.setAttributeNS('http://example/namespace2', 'exampleprefix:attributeWithNamespaceAndPrefix', 'new namespaced value 2');
    expect(targetNode.attributes.length).toBe(5);

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(destination.startExclusive.nextSibling).toBe(targetNode); // Preserved the element
    expect(newContentNode).not.toBe(targetNode);
    expect(targetNode.getAttributeNames()).toEqual(['preserved']);
    expect(targetNode.getAttribute('preserved')).toBe('preserved value');
  });

  test('should recurse into all elements', () => {
    // Arrange
    const destination = makeExistingContent(
      `<root>` +
        `Text that will change` +
        `<child-will-retain>Text that will be removed</child-will-retain>` +
        `<child-will-delete>Any content</child-will-delete>` +
      `</root>` +
      `<root>` +
        `<another-child-will-retain attr='will-remove'></another-child-will-retain>` +
      `</root>`);
    const newContent = makeNewContent(
      `<root>` +
        `<inserted-child></inserted-child>` +
        `Text that was changed` +
        `<child-will-retain><new-thing attr=val></new-thing></child-will-retain>` +
      `</root>` +
      `<!--newcomment-->` +
      `<root>` +
        `<another-child-will-retain attr='added'>` +
            `<inserted-grandchild></inserted-grandchild>` +
        `</another-child-will-retain>` +
      `</root>`);
    const newContentHtml = toHtml(newContent);
    const oldNodes = toNodeArray(destination);
    const origRoot1 = oldNodes[0];
    const origRoot2 = oldNodes[1];
    const textThatWillChange = oldNodes[0].childNodes[0];
    const childWillRetain = oldNodes[0].childNodes[1];
    const anotherChildWillRetain = oldNodes[1].childNodes[0];

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination) as Element[];

    // Assert: we inserted and changed the right elements/textnodes/comments/attributes
    expect(toHtml(newNodes)).toEqual(newContentHtml);

    // Assert: we retained the expected original nodes
    expect(newNodes[0]).toBe(origRoot1);
    expect(newNodes[0].childNodes[1]).toBe(textThatWillChange);
    expect(newNodes[0].childNodes[2]).toBe(childWillRetain);
    expect(newNodes[2]).toBe(origRoot2);
    expect(newNodes[2].childNodes[0]).toBe(anotherChildWillRetain);
  });

  test('should update input/textarea element value property when not modified by user', () => {
    // For input-like elements, what we mostly care about is the value *property*,
    // not the attribute. When this property hasn't explicitly been written, it takes
    // its value from the value attribute. However we do still also want to update
    // the attribute to make the DOM as consistent as possible.
    //
    // This test aims to show that, in this situation prior to user edits,
    // we update both the property and attribute to match the new content.

    // Arrange
    const destination = makeExistingContent(
      `<input value='original'><textarea>original</textarea>`);
    const newContent = makeNewContent(
      `<input value='changed'><textarea>changed</textarea>`);
    const inputElem = toNodeArray(destination)[0] as HTMLInputElement;
    const textareaElem = inputElem.nextElementSibling as HTMLTextAreaElement;

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(inputElem.value).toEqual('changed');
    expect(inputElem.getAttribute('value')).toEqual('changed');
    expect(textareaElem.value).toEqual('changed');
    expect(textareaElem.textContent).toEqual('changed');
  });

  test('should update input/textarea element value when modified by user and changed in new content', () => {
    // After an input-like element is edited (or equivalently, after something
    // is written to its 'value' property), that element's 'value' property
    // no longer stays in sync with the element's 'value' attribute. The property
    // and attribute become independent, and the property is what actually
    // reflects the UI state.
    //
    // This test aims to show that, in this situation after user edits, we still
    // update both the property and attribute to match the new content. This
    // means we are discarding the user's edit, which is desirable because the
    // whole idea of DomSync is to ensure the UI state matches the new content
    // and create an equivalent result to reloading the whole page.

    // Arrange
    const destination = makeExistingContent(
      `<input value='original'><textarea>original</textarea>`);
    const newContent = makeNewContent(
      `<input value='changed'><textarea>changed</textarea>`);
    const inputElem = toNodeArray(destination)[0] as HTMLInputElement;
    const textAreaElem = inputElem.nextElementSibling as HTMLTextAreaElement;
    inputElem.value = 'edited by user';
    textAreaElem.value = 'edited by user';

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(inputElem.value).toEqual('changed');
    expect(inputElem.getAttribute('value')).toEqual('changed');
    expect(textAreaElem.value).toEqual('changed');
    expect(textAreaElem.textContent).toEqual('changed');
  });

  test('should update input/textarea element value when modified by user but unchanged in new content', () => {
    // Equivalent to the test above, except the old and new content is identical
    // (so by looking at the attributes alone it seems nothing has to be updated)
    // and we are showing that it still reverts the user's edit

    // Arrange
    const destination = makeExistingContent(
      `<input value='original'><textarea>original</textarea>`);
    const newContent = makeNewContent(
      `<input value='original'><textarea>original</textarea>`);
    const inputElem = toNodeArray(destination)[0] as HTMLInputElement;
    const textAreaElem = inputElem.nextElementSibling as HTMLTextAreaElement;
    inputElem.value = 'edited by user';
    textAreaElem.value = 'edited by user';

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(inputElem.value).toEqual('original');
    expect(inputElem.getAttribute('value')).toEqual('original');
    expect(textAreaElem.value).toEqual('original');
    expect(textAreaElem.textContent).toEqual('original');
  });

  test('should be able to add select with nonempty option value', () => {
    // Shows that when inserting a completely new <select>, the correct initial
    // value is set and that none of the deferred value assignment logic breaks this.

    // Arrange
    const destination = makeExistingContent(
      ``);
    const newContent = makeNewContent(
      `<select>`
      + `<option value='first'></option>`
      + `<option value='second' selected></option>`
      + `<option value='third'></option>` +
      `</select>`);

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    const selectElem = destination.startExclusive.nextSibling;
    expect(selectElem).toBeInstanceOf(HTMLSelectElement);
    expect((selectElem as HTMLSelectElement).value).toBe('second');
  });

  test('should be able to update select to a newly-added option value', () => {
    // Shows that the introduction of an <option> with 'selected' is sufficient
    // to make the <select>'s 'value' property update, and that none of the
    // deferred value assignment logic breaks this.

    // Arrange
    const destination = makeExistingContent(
      `<select>`
      + `<option value='original1' selected></option>`
      + `<option value='original2'></option>` +
      `</select>`);
    const newContent = makeNewContent(
      `<select>`
      + `<option value='new1'></option>`
      + `<option value='new2'></option>`
      + `<option value='new3' selected></option>` +
      `</select>`);
    const selectElem = destination.startExclusive.nextSibling as HTMLSelectElement;
    selectElem.value = 'original2';

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(selectElem.value).toBe('new3');
  });

  test('should be able to update an input range to a value outside the min/max of the old content', () => {
    // This shows that the deferred value handling works. We can't actually assign the attributes/properties
    // in the given order, because it would cause the value to exceed the max (as we have not yet updated the
    // max). However it works anyway because of the deferred value assignment mechanism.

    // Arrange
    const destination = makeExistingContent(
      `<input type='range' min='100' max='200' value='150'>`);
    const newContent = makeNewContent(
      `<input type='range' value='1000' min='950' max='1050'>`);
    const inputRange = destination.startExclusive.nextSibling as HTMLInputElement;
    expect(inputRange.value).toBe('150');
    expect(inputRange.min).toBe('100');
    expect(inputRange.max).toBe('200');
    inputRange.value = '175';

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    expect(inputRange.value).toBe('1000');
    expect(inputRange.min).toBe('950');
    expect(inputRange.max).toBe('1050');
  });

  test('should not replay old deferred value on subsequent update (input)', () => {
    // This case may seem obscure but represents a bug that existed at one point.
    // The 'deferred' values tracked for some element types need to be cleared
    // after usage otherwise older values can overwrite newer ones.

    const destination = makeExistingContent(`<input value='First'>`);
    const newContent1 = makeNewContent(`<input value='Second'>`);
    const newContent2 = makeNewContent(`<input value='Third'>`);

    const elem = destination.startExclusive.nextSibling as HTMLInputElement;
    expect(elem.value).toBe('First');

    // Act/Assert 1: Initial update
    synchronizeDomContent(destination, newContent1);
    expect(elem.value).toBe('Second');

    // Act/Assert 2: The user performs an edit, then we try to synchronize the DOM
    // with some content that matches the edit exactly. The diff algorithm will see
    // that the DOM already matches the desired output, so it won't track any new
    // deferred value. We need to check the old deferred value doesn't reappear.
    elem.value = 'Third';
    synchronizeDomContent(destination, newContent2);
    expect(elem.value).toBe('Third');
  });

  test('should not replay old deferred value on subsequent update (select)', () => {
    // This case may seem obscure but represents a bug that existed at one point.
    // The 'deferred' values tracked for some element types need to be cleared
    // after usage otherwise older values can overwrite newer ones.

    const destination = makeExistingContent(`<select><option value='v1' selected>1</option><option value='v2'>2</option><option value='v3'>3</option></select>`);
    const newContent1 = makeNewContent(`<select><option value='v1'>1</option><option value='v2' selected>2</option><option value='v3'>3</option></select>`);
    const newContent2 = makeNewContent(`<select><option value='v1'>1</option><option value='v2'>2</option><option value='v3' selected>3</option></select>`);

    const selectElem = destination.startExclusive.nextSibling as HTMLSelectElement;
    expect(selectElem.selectedIndex).toBe(0);

    // Act/Assert 1: Initial update
    synchronizeDomContent(destination, newContent1);
    expect(selectElem.selectedIndex).toBe(1);

    // Act/Assert 2: The user performs an edit, then we try to synchronize the DOM
    // with some content that matches the edit exactly. The diff algorithm will see
    // that the DOM already matches the desired output, so it won't track any new
    // deferred value. We need to check the old deferred value doesn't reappear.
    selectElem.selectedIndex = 2;
    expect(selectElem.selectedIndex).toBe(2);
    synchronizeDomContent(destination, newContent2);
    expect(selectElem.selectedIndex).toBe(2);
  });

  test('should handle checkboxes with value attribute', () => {
    // Checkboxes require even more special-case handling because their 'value' attribute
    // has to be handled as a regular attribute, and 'checked' must be handled similarly
    // to 'value' on other inputs

    const destination = makeExistingContent(`<input type='checkbox' value='first' checked />`);
    const newContent = makeNewContent(`<input type='checkbox' value='second' checked />`);

    const checkboxElem = destination.startExclusive.nextSibling as HTMLInputElement;

    // Act/Assert
    synchronizeDomContent(destination, newContent);
    expect(checkboxElem.checked).toBeTruthy();
    expect(checkboxElem.value).toBe('second');
  });

  test('should handle radio buttons with value attribute', () => {
    // Radio buttons require even more special-case handling because their 'value' attribute
    // has to be handled as a regular attribute, and 'checked' must be handled similarly
    // to 'value' on other inputs

    const destination = makeExistingContent(`<input type='radio' value='first' checked />`);
    const newContent = makeNewContent(`<input type='radio' value='second' checked />`);

    const checkboxElem = destination.startExclusive.nextSibling as HTMLInputElement;

    // Act/Assert
    synchronizeDomContent(destination, newContent);
    expect(checkboxElem.checked).toBeTruthy();
    expect(checkboxElem.value).toBe('second');
  });

  test('should treat doctype nodes as unchanged', () => {
    // Can't update a doctype after the document is created, nor is there a use case for doing so
    // We just have to skip them, as it would be an error to try removing or inserting them

    // Arrange
    const destination = new DOMParser().parseFromString(
      `<!DOCTYPE html>` +
      `<html><body>Hello</body></html>`, 'text/html');
    const newContent = new DOMParser().parseFromString(
      `<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">` +
      `<html><body>Goodbye</body></html>`, 'text/html');
    const origDocTypeNode = destination.firstChild!;
    expect(origDocTypeNode.nodeType).toBe(Node.DOCUMENT_TYPE_NODE);
    expect(destination.body.textContent).toBe('Hello');

    // Act
    synchronizeDomContent(destination, newContent);

    // Assert
    const newDocTypeNode = destination.firstChild;
    expect(newDocTypeNode).toBe(origDocTypeNode);
    expect(destination.body.textContent).toBe('Goodbye');
  });

  test('should preserve content in elements marked as data permanent', () => {
    // Arrange
    const destination = makeExistingContent(`<div>not preserved</div><div data-permanent>preserved</div><div>also not preserved</div>`);
    const newContent = makeNewContent(`<div></div><div data-permanent>other content</div><div></div>`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination);

    // Assert
    expect(oldNodes[0]).toBe(newNodes[0]);
    expect(oldNodes[1]).toBe(newNodes[1]);
    expect(newNodes[0].textContent).toBe('');
    expect(newNodes[1].textContent).toBe('preserved');
  });

  test('should preserve content in elements marked as data permanent by matching attribute value', () => {
    // Arrange
    const destination = makeExistingContent(`<div>not preserved</div><div data-permanent="first">first preserved</div>`);
    const newContent1 = makeNewContent(`<div>not preserved</div><div data-permanent="second">second preserved</div><div data-permanent="first">other content</div>`);
    const newContent2 = makeNewContent(`<div>not preserved</div><div data-permanent="second">other content</div><div id="foo"></div><div data-permanent="first">other content</div>`);
    const nodes1 = toNodeArray(destination);

    // Act/assert 1: The original data permanent content is preserved
    synchronizeDomContent(destination, newContent1);
    const nodes2 = toNodeArray(destination);
    expect(nodes1[1]).toBe(nodes2[2]);
    expect(nodes2[1].textContent).toBe('second preserved');
    expect(nodes2[2].textContent).toBe('first preserved');

    // Act/assert 2: The new data permanent content is preserved
    synchronizeDomContent(destination, newContent2);
    const nodes3 = toNodeArray(destination);
    expect(nodes2[1]).toBe(nodes3[1]);
    expect(nodes2[2]).toBe(nodes3[3]);
    expect(nodes3[1].textContent).toBe('second preserved');
    expect(nodes3[3].textContent).toBe('first preserved');
  });

  test('should not preserve content in elements marked as data permanent if attribute value does not match', () => {
    // Arrange
    const destination = makeExistingContent(`<div>not preserved</div><div data-permanent="first">preserved</div><div>also not preserved</div>`);
    const newContent = makeNewContent(`<div></div><div data-permanent="second">new content</div><div></div>`);
    const oldNodes = toNodeArray(destination);

    // Act
    synchronizeDomContent(destination, newContent);
    const newNodes = toNodeArray(destination);

    // Assert
    expect(oldNodes[0]).toBe(newNodes[0]);
    expect(oldNodes[1]).not.toBe(newNodes[1]);
    expect(newNodes[0].textContent).toBe('');
    expect(newNodes[1].textContent).toBe('new content');
  });
});

test('should remove value if neither source nor destination has one', () => {
  // Editing an input assigns a 'value' property but does *not* add a 'value' attribute when one was not already there
  // So if both the source and destination HTML lack a 'value' attribute, it would look to us that nothing has changed.
  // This test shows that we detect this and clear any 'value' so we correctly match the new content

  // Arrange
  const unchangedHtml =
    `<input />` +
    `<input type='checkbox' />` +
    `<select><option value='someval1'></option><option value='someval2' selected></option><option value='someval3' ></option></select>`;
  const destination = makeExistingContent(unchangedHtml);
  const newContent = makeNewContent(unchangedHtml);

  const inputText = destination.startExclusive.nextSibling as HTMLInputElement;
  const inputCheckbox = inputText.nextElementSibling as HTMLInputElement;
  const select = inputCheckbox.nextElementSibling as HTMLSelectElement;

  // Act: User makes some edits, then we synchronize to the blank content
  inputText.value = 'Some edit';
  inputCheckbox.checked = true;
  select.selectedIndex = 2;
  synchronizeDomContent(destination, newContent);

  // Assert: Edits were cleared
  expect(inputText.value).toStrictEqual('');
  expect(inputCheckbox.checked).toStrictEqual(false);
  expect(select.selectedIndex).toStrictEqual(1);
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

function toHtml(content: DocumentFragment | Node[]) {
  let result = '';
  const nodes = content instanceof DocumentFragment ? content.childNodes : content;
  for (let i = 0; i < nodes.length; i++) {
    const node = nodes[i];
    switch (node.nodeType) {
      case Node.ELEMENT_NODE:
        result += (node as Element).outerHTML;
        break;
      case Node.TEXT_NODE:
        result += node.textContent;
        break;
      case Node.COMMENT_NODE:
        result += `<!--${node.textContent}-->`;
        break;
      default:
        throw new Error(`Not implemented toHTML for node type ${node.nodeType}`);
    }
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
