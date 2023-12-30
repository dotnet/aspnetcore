// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { RenderBatch, ArrayBuilderSegment, RenderTreeEdit, RenderTreeFrame, EditType, FrameType, ArrayValues } from './RenderBatch/RenderBatch';
import { EventDelegator } from './Events/EventDelegator';
import { LogicalElement, PermutationListEntry, toLogicalElement, insertLogicalChild, removeLogicalChild, getLogicalParent, getLogicalChild, createAndInsertLogicalContainer, isSvgElement, permuteLogicalChildren, getClosestDomElement, emptyLogicalElement, getLogicalChildrenArray } from './LogicalElements';
import { applyCaptureIdToElement } from './ElementReferenceCapture';
import { attachToEventDelegator as attachNavigationManagerToEventDelegator } from '../Services/NavigationManager';
import { applyAnyDeferredValue, tryApplySpecialProperty } from './DomSpecialPropertyUtil';
const sharedTemplateElemForParsing = document.createElement('template');
const sharedSvgElemForParsing = document.createElementNS('http://www.w3.org/2000/svg', 'g');
const elementsToClearOnRootComponentRender = new Set<LogicalElement>();
const internalAttributeNamePrefix = '__internal_';
const eventPreventDefaultAttributeNamePrefix = 'preventDefault_';
const eventStopPropagationAttributeNamePrefix = 'stopPropagation_';
const interactiveRootComponentPropname = Symbol();
const preserveContentOnDisposalPropname = Symbol();

export class BrowserRenderer {
  public eventDelegator: EventDelegator;

  private rootComponentIds = new Set<number>();

  private childComponentLocations: { [componentId: number]: LogicalElement } = {};

  public constructor(browserRendererId: number) {
    this.eventDelegator = new EventDelegator(browserRendererId);

    // We don't yet know whether or not navigation interception will be enabled, but in case it will be,
    // we wire up the navigation manager to the event delegator so it has the option to participate
    // in the synthetic event bubbling process later
    attachNavigationManagerToEventDelegator(this.eventDelegator);
  }

  public getRootComponentCount(): number {
    return this.rootComponentIds.size;
  }

  public attachRootComponentToLogicalElement(componentId: number, element: LogicalElement, appendContent: boolean): void {
    if (isInteractiveRootComponentElement(element)) {
      throw new Error(`Root component '${componentId}' could not be attached because its target element is already associated with a root component`);
    }

    // If we want to append content to the end of the element, we create a new logical child container
    // at the end of the element and treat that as the new parent.
    if (appendContent) {
      const indexAfterLastChild = getLogicalChildrenArray(element).length;
      element = createAndInsertLogicalContainer(element, indexAfterLastChild);
    }

    markAsInteractiveRootComponentElement(element, true);
    this.attachComponentToElement(componentId, element);
    this.rootComponentIds.add(componentId);

    elementsToClearOnRootComponentRender.add(element);
  }

  public updateComponent(batch: RenderBatch, componentId: number, edits: ArrayBuilderSegment<RenderTreeEdit>, referenceFrames: ArrayValues<RenderTreeFrame>): void {
    const element = this.childComponentLocations[componentId];
    if (!element) {
      throw new Error(`No element is currently associated with component ${componentId}`);
    }

    // On the first render for each root component, clear any existing content (e.g., prerendered)
    if (elementsToClearOnRootComponentRender.delete(element)) {
      emptyLogicalElement(element);

      if (element instanceof Comment) {
        // We sanitize start comments by removing all the information from it now that we don't need it anymore
        // as it adds noise to the DOM.
        element.textContent = '!';
      }
    }

    const ownerDocument = getClosestDomElement(element)?.getRootNode() as Document;
    const activeElementBefore = ownerDocument && ownerDocument.activeElement;

    this.applyEdits(batch, componentId, element, 0, edits, referenceFrames);

    // Try to restore focus in case it was lost due to an element move
    if ((activeElementBefore instanceof HTMLElement) && ownerDocument && ownerDocument.activeElement !== activeElementBefore) {
      activeElementBefore.focus();
    }
  }

  public disposeComponent(componentId: number): void {
    if (this.rootComponentIds.delete(componentId)) {
      // When disposing a root component, the container element won't be removed from the DOM (because there's
      // no parent to remove that child), so we empty it to restore it to the state it was in before the root
      // component was added.
      const logicalElement = this.childComponentLocations[componentId];
      markAsInteractiveRootComponentElement(logicalElement, false);

      if (shouldPreserveContentOnInteractiveComponentDisposal(logicalElement)) {
        elementsToClearOnRootComponentRender.add(logicalElement);
      } else {
        emptyLogicalElement(logicalElement);
      }
    }

    delete this.childComponentLocations[componentId];
  }

  public disposeEventHandler(eventHandlerId: number): void {
    this.eventDelegator.removeListener(eventHandlerId);
  }

  private attachComponentToElement(componentId: number, element: LogicalElement) {
    this.childComponentLocations[componentId] = element;
  }

  private applyEdits(batch: RenderBatch, componentId: number, parent: LogicalElement, childIndex: number, edits: ArrayBuilderSegment<RenderTreeEdit>, referenceFrames: ArrayValues<RenderTreeFrame>) {
    let currentDepth = 0;
    let childIndexAtCurrentDepth = childIndex;
    let permutationList: PermutationListEntry[] | undefined;

    const arrayBuilderSegmentReader = batch.arrayBuilderSegmentReader;
    const editReader = batch.editReader;
    const frameReader = batch.frameReader;
    const editsValues = arrayBuilderSegmentReader.values(edits);
    const editsOffset = arrayBuilderSegmentReader.offset(edits);
    const editsLength = arrayBuilderSegmentReader.count(edits);
    const maxEditIndexExcl = editsOffset + editsLength;

    for (let editIndex = editsOffset; editIndex < maxEditIndexExcl; editIndex++) {
      const edit = batch.diffReader.editsEntry(editsValues, editIndex);
      const editType = editReader.editType(edit);
      switch (editType) {
        case EditType.prependFrame: {
          const frameIndex = editReader.newTreeIndex(edit);
          const frame = batch.referenceFramesEntry(referenceFrames, frameIndex);
          const siblingIndex = editReader.siblingIndex(edit);
          this.insertFrame(batch, componentId, parent, childIndexAtCurrentDepth + siblingIndex, referenceFrames, frame, frameIndex);
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
            this.applyAttribute(batch, componentId, element, frame);
          } else {
            throw new Error('Cannot set attribute on non-element child');
          }
          break;
        }
        case EditType.removeAttribute: {
          // Note that we don't have to dispose the info we track about event handlers here, because the
          // disposed event handler IDs are delivered separately (in the 'disposedEventHandlerIds' array)
          const siblingIndex = editReader.siblingIndex(edit);
          const element = getLogicalChild(parent, childIndexAtCurrentDepth + siblingIndex);
          if (element instanceof Element) {
            const attributeName = editReader.removedAttributeName(edit)!;
            this.setOrRemoveAttributeOrProperty(element, attributeName, null);
          } else {
            throw new Error('Cannot remove attribute from non-element child');
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
            throw new Error('Cannot set text content on non-text child');
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
        case EditType.permutationListEntry: {
          permutationList = permutationList || [];
          permutationList.push({
            fromSiblingIndex: childIndexAtCurrentDepth + editReader.siblingIndex(edit),
            toSiblingIndex: childIndexAtCurrentDepth + editReader.moveToSiblingIndex(edit),
          });
          break;
        }
        case EditType.permutationListEnd: {
          permuteLogicalChildren(parent, permutationList!);
          permutationList = undefined;
          break;
        }
        default: {
          const unknownType: never = editType; // Compile-time verification that the switch was exhaustive
          throw new Error(`Unknown edit type: ${unknownType}`);
        }
      }
    }
  }

  private insertFrame(batch: RenderBatch, componentId: number, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, frame: RenderTreeFrame, frameIndex: number): number {
    const frameReader = batch.frameReader;
    const frameType = frameReader.frameType(frame);
    switch (frameType) {
      case FrameType.element:
        this.insertElement(batch, componentId, parent, childIndex, frames, frame, frameIndex);
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
        return this.insertFrameRange(batch, componentId, parent, childIndex, frames, frameIndex + 1, frameIndex + frameReader.subtreeLength(frame));
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
      case FrameType.namedEvent: // Not used on the JS side
        return 0;
      default: {
        const unknownType: never = frameType; // Compile-time verification that the switch was exhaustive
        throw new Error(`Unknown frame type: ${unknownType}`);
      }
    }
  }

  private insertElement(batch: RenderBatch, componentId: number, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, frame: RenderTreeFrame, frameIndex: number) {
    const frameReader = batch.frameReader;
    const tagName = frameReader.elementName(frame)!;

    const newDomElementRaw = (tagName === 'svg' || isSvgElement(parent)) ?
      document.createElementNS('http://www.w3.org/2000/svg', tagName) :
      document.createElement(tagName);
    const newElement = toLogicalElement(newDomElementRaw);

    let inserted = false;

    // Apply attributes
    const descendantsEndIndexExcl = frameIndex + frameReader.subtreeLength(frame);
    for (let descendantIndex = frameIndex + 1; descendantIndex < descendantsEndIndexExcl; descendantIndex++) {
      const descendantFrame = batch.referenceFramesEntry(frames, descendantIndex);
      if (frameReader.frameType(descendantFrame) === FrameType.attribute) {
        this.applyAttribute(batch, componentId, newDomElementRaw, descendantFrame);
      } else {
        insertLogicalChild(newDomElementRaw, parent, childIndex);
        inserted = true;
        // As soon as we see a non-attribute child, all the subsequent child frames are
        // not attributes, so bail out and insert the remnants recursively
        this.insertFrameRange(batch, componentId, newElement, 0, frames, descendantIndex, descendantsEndIndexExcl);
        break;
      }
    }

    // this element did not have any children, so it's not inserted yet.
    if (!inserted) {
      insertLogicalChild(newDomElementRaw, parent, childIndex);
    }

    applyAnyDeferredValue(newDomElementRaw);
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

  private applyAttribute(batch: RenderBatch, componentId: number, toDomElement: Element, attributeFrame: RenderTreeFrame) {
    const frameReader = batch.frameReader;
    const attributeName = frameReader.attributeName(attributeFrame)!;
    const eventHandlerId = frameReader.attributeEventHandlerId(attributeFrame);

    if (eventHandlerId) {
      const eventName = stripOnPrefix(attributeName);
      this.eventDelegator.setListener(toDomElement, eventName, eventHandlerId, componentId);
      return;
    }

    const value = frameReader.attributeValue(attributeFrame);
    this.setOrRemoveAttributeOrProperty(toDomElement, attributeName, value);
  }

  private insertFrameRange(batch: RenderBatch, componentId: number, parent: LogicalElement, childIndex: number, frames: ArrayValues<RenderTreeFrame>, startIndex: number, endIndexExcl: number): number {
    const origChildIndex = childIndex;
    for (let index = startIndex; index < endIndexExcl; index++) {
      const frame = batch.referenceFramesEntry(frames, index);
      const numChildrenInserted = this.insertFrame(batch, componentId, parent, childIndex, frames, frame, index);
      childIndex += numChildrenInserted;

      // Skip over any descendants, since they are already dealt with recursively
      index += countDescendantFrames(batch, frame);
    }

    return (childIndex - origChildIndex); // Total number of children inserted
  }

  private setOrRemoveAttributeOrProperty(element: Element, name: string, valueOrNullToRemove: string | null) {
    // First see if we have special handling for this attribute
    if (!tryApplySpecialProperty(element, name, valueOrNullToRemove)) {
      // If not, maybe it's one of our internal attributes
      if (name.startsWith(internalAttributeNamePrefix)) {
        this.applyInternalAttribute(element, name.substring(internalAttributeNamePrefix.length), valueOrNullToRemove);
      } else {
        // If not, treat it as a regular DOM attribute
        if (valueOrNullToRemove !== null) {
          element.setAttribute(name, valueOrNullToRemove);
        } else {
          element.removeAttribute(name);
        }
      }
    }
  }

  private applyInternalAttribute(element: Element, internalAttributeName: string, value: string | null) {
    if (internalAttributeName.startsWith(eventStopPropagationAttributeNamePrefix)) {
      // Stop propagation
      const eventName = stripOnPrefix(internalAttributeName.substring(eventStopPropagationAttributeNamePrefix.length));
      this.eventDelegator.setStopPropagation(element, eventName, value !== null);
    } else if (internalAttributeName.startsWith(eventPreventDefaultAttributeNamePrefix)) {
      // Prevent default
      const eventName = stripOnPrefix(internalAttributeName.substring(eventPreventDefaultAttributeNamePrefix.length));
      this.eventDelegator.setPreventDefault(element, eventName, value !== null);
    } else {
      // The prefix makes this attribute name reserved, so any other usage is disallowed
      throw new Error(`Unsupported internal attribute '${internalAttributeName}'`);
    }
  }
}

function markAsInteractiveRootComponentElement(element: LogicalElement, isInteractive: boolean) {
  element[interactiveRootComponentPropname] = isInteractive;
}

export function isInteractiveRootComponentElement(element: LogicalElement): boolean | undefined {
  return element[interactiveRootComponentPropname];
}

export function setShouldPreserveContentOnInteractiveComponentDisposal(element: LogicalElement, shouldPreserve: boolean) {
  element[preserveContentOnDisposalPropname] = shouldPreserve;
}

function shouldPreserveContentOnInteractiveComponentDisposal(element: LogicalElement): boolean {
  return element[preserveContentOnDisposalPropname] === true;
}

export interface ComponentDescriptor {
  start: Node;
  end: Node;
}

function parseMarkup(markup: string, isSvg: boolean) {
  if (isSvg) {
    sharedSvgElemForParsing.innerHTML = markup || ' ';
    return sharedSvgElemForParsing;
  } else {
    sharedTemplateElemForParsing.innerHTML = markup || ' ';

    // Since this is a markup string, we want to honor the developer's intent to
    // evaluate any scripts it may contain. Scripts parsed from an innerHTML assignment
    // won't be executable by default (https://stackoverflow.com/questions/1197575/can-scripts-be-inserted-with-innerhtml)
    // but that's inconsistent with anything constructed from a sequence like:
    // - OpenElement("script")
    // - AddContent(js) or AddMarkupContent(js)
    // - CloseElement()
    // It doesn't make sense to have such an inconsistency in Blazor's interactive
    // renderer, and for back-compat with pre-.NET 8 code (when the Razor compiler always
    // used OpenElement like above), as well as consistency with static SSR, we need to make it work.
    sharedTemplateElemForParsing.content.querySelectorAll('script').forEach(oldScriptElem => {
      const newScriptElem = document.createElement('script');
      newScriptElem.textContent = oldScriptElem.textContent;

      oldScriptElem.getAttributeNames().forEach(attribName => {
        newScriptElem.setAttribute(attribName, oldScriptElem.getAttribute(attribName)!);
      });

      oldScriptElem.parentNode!.replaceChild(newScriptElem, oldScriptElem);
    });

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

function stripOnPrefix(attributeName: string) {
  if (attributeName.startsWith('on')) {
    return attributeName.substring(2);
  }

  throw new Error(`Attribute should be an event name, but doesn't start with 'on'. Value: '${attributeName}'`);
}
