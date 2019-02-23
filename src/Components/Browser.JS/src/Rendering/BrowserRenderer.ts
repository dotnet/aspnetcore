import { RenderBatch, ArraySegment, RenderTreeEdit, RenderTreeFrame, EditType, FrameType, ArrayValues } from './RenderBatch/RenderBatch';
import { EventDelegator } from './EventDelegator';
import { EventForDotNet, UIEventArgs } from './EventForDotNet';
import { LogicalElement, toLogicalElement, insertLogicalChild, removeLogicalChild, getLogicalParent, getLogicalChild, createAndInsertLogicalContainer, isSvgElement } from './LogicalElements';
import { applyCaptureIdToElement } from './ElementReferenceCapture';
const selectValuePropname = '_blazorSelectValue';
const sharedTemplateElemForParsing = document.createElement('template');
const sharedSvgElemForParsing = document.createElementNS('http://www.w3.org/2000/svg', 'g');
const preventDefaultEvents: { [eventType: string]: boolean } = { submit: true };
const rootComponentsPendingFirstRender: { [componentId: number]: Element } = {};

export class BrowserRenderer {
  private eventDelegator: EventDelegator;
  private childComponentLocations: { [componentId: number]: LogicalElement } = {};

  constructor(private browserRendererId: number) {
    this.eventDelegator = new EventDelegator((event, eventHandlerId, eventArgs) => {
      raiseEvent(event, this.browserRendererId, eventHandlerId, eventArgs);
    });
  }

  public attachRootComponentToElement(componentId: number, element: Element) {
    // 'allowExistingContents' to keep any prerendered content until we do the first client-side render
    this.attachComponentToElement(componentId, toLogicalElement(element, /* allowExistingContents */ true));
    rootComponentsPendingFirstRender[componentId] = element;
  }

  public updateComponent(batch: RenderBatch, componentId: number, edits: ArraySegment<RenderTreeEdit>, referenceFrames: ArrayValues<RenderTreeFrame>) {
    const element = this.childComponentLocations[componentId];
    if (!element) {
      throw new Error(`No element is currently associated with component ${componentId}`);
    }

    // On the first render for each root component, clear any existing content (e.g., prerendered)
    const rootElementToClear = rootComponentsPendingFirstRender[componentId];
    if (rootElementToClear) {
      delete rootComponentsPendingFirstRender[componentId];
      clearElement(rootElementToClear);
    }

    this.applyEdits(batch, element, 0, edits, referenceFrames);
  }

  public disposeComponent(componentId: number) {
    delete this.childComponentLocations[componentId];
  }

  public disposeEventHandler(eventHandlerId: number) {
    this.eventDelegator.removeListener(eventHandlerId);
  }

  private attachComponentToElement(componentId: number, element: LogicalElement) {
    this.childComponentLocations[componentId] = element;
  }

  private applyEdits(batch: RenderBatch, parent: LogicalElement, childIndex: number, edits: ArraySegment<RenderTreeEdit>, referenceFrames: ArrayValues<RenderTreeFrame>) {
    let currentDepth = 0;
    let childIndexAtCurrentDepth = childIndex;

    const arraySegmentReader = batch.arraySegmentReader;
    const editReader = batch.editReader;
    const frameReader = batch.frameReader;
    const editsValues = arraySegmentReader.values(edits);
    const editsOffset = arraySegmentReader.offset(edits);
    const editsLength = arraySegmentReader.count(edits);
    const maxEditIndexExcl = editsOffset + editsLength;

    for (let editIndex = editsOffset; editIndex < maxEditIndexExcl; editIndex++) {
      const edit = batch.diffReader.editsEntry(editsValues, editIndex);
      const editType = editReader.editType(edit);
      switch (editType) {
        case EditType.prependFrame: {
          const frameIndex = editReader.newTreeIndex(edit);
          const frame = batch.referenceFramesEntry(referenceFrames, frameIndex);
          const siblingIndex = editReader.siblingIndex(edit);
          this.insertFrame(batch, parent, childIndexAtCurrentDepth + siblingIndex, referenceFrames, frame, frameIndex);
          break;
        }
        case EditType.removeFrame: {
          const siblingIndex = editReader.siblingIndex(edit);
          removeLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          break;
        }
        case EditType.setAttribute: {
          const frameIndex = editReader.newTreeIndex(edit);
          const frame = batch.referenceFramesEntry(referenceFrames, frameIndex);
          const siblingIndex = editReader.siblingIndex(edit);
          const element = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          if (element instanceof Element) {
            this.applyAttribute(batch, element, frame);
          } else {
            throw new Error(`Cannot set attribute on non-element child`);
          }
          break;
        }
        case EditType.removeAttribute: {
          // Note that we don't have to dispose the info we track about event handlers here, because the
          // disposed event handler IDs are delivered separately (in the 'disposedEventHandlerIds' array)
          const siblingIndex = editReader.siblingIndex(edit);
          const element = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          if (element instanceof HTMLElement) {
            const attributeName = editReader.removedAttributeName(edit)!;
            // First try to remove any special property we use for this attribute
            if (!this.tryApplySpecialProperty(batch, element, attributeName, null)) {
              // If that's not applicable, it's a regular DOM attribute so remove that
              element.removeAttribute(attributeName);
            }
          } else {
            throw new Error(`Cannot remove attribute from non-element child`);
          }
          break;
        }
        case EditType.updateText: {
          const frameIndex = editReader.newTreeIndex(edit);
          const frame = batch.referenceFramesEntry(referenceFrames, frameIndex);
          const siblingIndex = editReader.siblingIndex(edit);
          const textNode = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          if (textNode instanceof Text) {
            textNode.textContent = frameReader.textContent(frame);
          } else {
            throw new Error(`Cannot set text content on non-text child`);
          }
          break;
        }
        case EditType.updateMarkup: {
          const frameIndex = editReader.newTreeIndex(edit);
          const frame = batch.referenceFramesEntry(referenceFrames, frameIndex);
          const siblingIndex = editReader.siblingIndex(edit);
          removeLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          this.insertMarkup(batch, parent, childIndexAtCurrentDepth + siblingIndex, frame);
          break;
        }
        case EditType.stepIn: {
          const siblingIndex = editReader.siblingIndex(edit);
          parent = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          currentDepth++;
          childIndexAtCurrentDepth = 0;
          break;
        }
        case EditType.stepOut: {
          parent = getLogicalParent(parent)!;
          currentDepth--;
          childIndexAtCurrentDepth = currentDepth === 0 ? childIndex : 0; // The childIndex is only ever nonzero at zero depth
          break;
        }
        default: {
          const unknownType: never = editType; // Compile-time verification that the switch was exhaustive
          throw new Error(`Unknown edit type: ${unknownType}`);
        }
      }
    }
  }

  private insertFrame(batch: RenderBatch, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, frame: RenderTreeFrame, frameIndex: number): number {
    const frameReader = batch.frameReader;
    const frameType = frameReader.frameType(frame);
    switch (frameType) {
      case FrameType.element:
        this.insertElement(batch, parent, childIndex, frames, frame, frameIndex);
        return 1;
      case FrameType.text:
        this.insertText(batch, parent, childIndex, frame);
        return 1;
      case FrameType.attribute:
        throw new Error('Attribute frames should only be present as leading children of element frames.');
      case FrameType.component:
        this.insertComponent(batch, parent, childIndex, frame);
        return 1;
      case FrameType.region:
        return this.insertFrameRange(batch, parent, childIndex, frames, frameIndex + 1, frameIndex + frameReader.subtreeLength(frame));
      case FrameType.elementReferenceCapture:
        if (parent instanceof Element) {
          applyCaptureIdToElement(parent, frameReader.elementReferenceCaptureId(frame)!);
          return 0; // A "capture" is a child in the diff, but has no node in the DOM
        } else {
          throw new Error('Reference capture frames can only be children of element frames.');
        }
      case FrameType.markup:
        this.insertMarkup(batch, parent, childIndex, frame);
        return 1;
      default:
        const unknownType: never = frameType; // Compile-time verification that the switch was exhaustive
        throw new Error(`Unknown frame type: ${unknownType}`);
    }
  }

  private insertElement(batch: RenderBatch, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, frame: RenderTreeFrame, frameIndex: number) {
    const frameReader = batch.frameReader;
    const tagName = frameReader.elementName(frame)!;
    const newDomElementRaw = tagName === 'svg' || isSvgElement(parent) ?
      document.createElementNS('http://www.w3.org/2000/svg', tagName) :
      document.createElement(tagName);
    const newElement = toLogicalElement(newDomElementRaw);
    insertLogicalChild(newDomElementRaw, parent, childIndex);

    // Apply attributes
    const descendantsEndIndexExcl = frameIndex + frameReader.subtreeLength(frame);
    for (let descendantIndex = frameIndex + 1; descendantIndex < descendantsEndIndexExcl; descendantIndex++) {
      const descendantFrame = batch.referenceFramesEntry(frames, descendantIndex);
      if (frameReader.frameType(descendantFrame) === FrameType.attribute) {
        this.applyAttribute(batch, newDomElementRaw, descendantFrame);
      } else {
        // As soon as we see a non-attribute child, all the subsequent child frames are
        // not attributes, so bail out and insert the remnants recursively
        this.insertFrameRange(batch, newElement, 0, frames, descendantIndex, descendantsEndIndexExcl);
        break;
      }
    }
  }

  private insertComponent(batch: RenderBatch, parent: LogicalElement, childIndex: number, frame: RenderTreeFrame) {
    const containerElement = createAndInsertLogicalContainer(parent, childIndex);

    // All we have to do is associate the child component ID with its location. We don't actually
    // do any rendering here, because the diff for the child will appear later in the render batch.
    const childComponentId = batch.frameReader.componentId(frame);
    this.attachComponentToElement(childComponentId, containerElement);
  }

  private insertText(batch: RenderBatch, parent: LogicalElement, childIndex: number, textFrame: RenderTreeFrame) {
    const textContent = batch.frameReader.textContent(textFrame)!;
    const newTextNode = document.createTextNode(textContent);
    insertLogicalChild(newTextNode, parent, childIndex);
  }

  private insertMarkup(batch: RenderBatch, parent: LogicalElement, childIndex: number, markupFrame: RenderTreeFrame) {
    const markupContainer = createAndInsertLogicalContainer(parent, childIndex);

    const markupContent = batch.frameReader.markupContent(markupFrame);
    const parsedMarkup = parseMarkup(markupContent, isSvgElement(parent));
    let logicalSiblingIndex = 0;
    while (parsedMarkup.firstChild) {
      insertLogicalChild(parsedMarkup.firstChild, markupContainer, logicalSiblingIndex++);
    }
  }

  private applyAttribute(batch: RenderBatch, toDomElement: Element, attributeFrame: RenderTreeFrame) {
    const frameReader = batch.frameReader;
    const attributeName = frameReader.attributeName(attributeFrame)!;
    const browserRendererId = this.browserRendererId;
    const eventHandlerId = frameReader.attributeEventHandlerId(attributeFrame);

    if (eventHandlerId) {
      const firstTwoChars = attributeName.substring(0, 2);
      const eventName = attributeName.substring(2);
      if (firstTwoChars !== 'on' || !eventName) {
        throw new Error(`Attribute has nonzero event handler ID, but attribute name '${attributeName}' does not start with 'on'.`);
      }
      this.eventDelegator.setListener(toDomElement, eventName, eventHandlerId);
      return;
    }

    // First see if we have special handling for this attribute
    if (!this.tryApplySpecialProperty(batch, toDomElement, attributeName, attributeFrame)) {
      // If not, treat it as a regular string-valued attribute
      toDomElement.setAttribute(
        attributeName,
        frameReader.attributeValue(attributeFrame)!
      );
    }
  }

  private tryApplySpecialProperty(batch: RenderBatch, element: Element, attributeName: string, attributeFrame: RenderTreeFrame | null) {
    switch (attributeName) {
      case 'value':
        return this.tryApplyValueProperty(batch, element, attributeFrame);
      case 'checked':
        return this.tryApplyCheckedProperty(batch, element, attributeFrame);
      default:
        return false;
    }
  }

  private tryApplyValueProperty(batch: RenderBatch, element: Element, attributeFrame: RenderTreeFrame | null) {
    // Certain elements have built-in behaviour for their 'value' property
    const frameReader = batch.frameReader;
    switch (element.tagName) {
      case 'INPUT':
      case 'SELECT':
      case 'TEXTAREA': {
        const value = attributeFrame ? frameReader.attributeValue(attributeFrame) : null;
        (element as any).value = value;

        if (element.tagName === 'SELECT') {
          // <select> is special, in that anything we write to .value will be lost if there
          // isn't yet a matching <option>. To maintain the expected behavior no matter the
          // element insertion/update order, preserve the desired value separately so
          // we can recover it when inserting any matching <option>.
          element[selectValuePropname] = value;
        }
        return true;
      }
      case 'OPTION': {
        const value = attributeFrame ? frameReader.attributeValue(attributeFrame) : null;
        if (value) {
          element.setAttribute('value', value);
        } else {
          element.removeAttribute('value');
        }
        // See above for why we have this special handling for <select>/<option>
        const parentElement = element.parentElement;
        if (parentElement && (selectValuePropname in parentElement) && parentElement[selectValuePropname] === value) {
          this.tryApplyValueProperty(batch, parentElement, attributeFrame);
          delete parentElement[selectValuePropname];
        }
        return true;
      }
      default:
        return false;
    }
  }

  private tryApplyCheckedProperty(batch: RenderBatch, element: Element, attributeFrame: RenderTreeFrame | null) {
    // Certain elements have built-in behaviour for their 'checked' property
    if (element.tagName === 'INPUT') {
      const value = attributeFrame ? batch.frameReader.attributeValue(attributeFrame) : null;
      (element as any).checked = value !== null;
      return true;
    } else {
      return false;
    }
  }

  private insertFrameRange(batch: RenderBatch, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, startIndex: number, endIndexExcl: number): number {
    const origChildIndex = childIndex;
    for (let index = startIndex; index < endIndexExcl; index++) {
      const frame = batch.referenceFramesEntry(frames, index);
      const numChildrenInserted = this.insertFrame(batch, parent, childIndex, frames, frame, index);
      childIndex += numChildrenInserted;

      // Skip over any descendants, since they are already dealt with recursively
      index += countDescendantFrames(batch, frame);
    }

    return (childIndex - origChildIndex); // Total number of children inserted
  }
}

function parseMarkup(markup: string, isSvg: boolean) {
  if (isSvg) {
    sharedSvgElemForParsing.innerHTML = markup || ' ';
    return sharedSvgElemForParsing;
  } else {
    sharedTemplateElemForParsing.innerHTML = markup || ' ';
    return sharedTemplateElemForParsing.content;
  }
}

function countDescendantFrames(batch: RenderBatch, frame: RenderTreeFrame): number {
  const frameReader = batch.frameReader;
  switch (frameReader.frameType(frame)) {
    // The following frame types have a subtree length. Other frames may use that memory slot
    // to mean something else, so we must not read it. We should consider having nominal subtypes
    // of RenderTreeFramePointer that prevent access to non-applicable fields.
    case FrameType.component:
    case FrameType.element:
    case FrameType.region:
      return frameReader.subtreeLength(frame) - 1;
    default:
      return 0;
  }
}

function raiseEvent(event: Event, browserRendererId: number, eventHandlerId: number, eventArgs: EventForDotNet<UIEventArgs>) {
  if (preventDefaultEvents[event.type]) {
    event.preventDefault();
  }

  const eventDescriptor = {
    browserRendererId,
    eventHandlerId,
    eventArgsType: eventArgs.type
  };

  return DotNet.invokeMethodAsync(
    'Microsoft.AspNetCore.Components.Browser',
    'DispatchEvent',
    eventDescriptor,
    JSON.stringify(eventArgs.data));
}

function clearElement(element: Element) {
  let childNode: Node | null;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}
