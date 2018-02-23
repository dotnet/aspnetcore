import { System_Array, MethodHandle } from '../Platform/Platform';
import { getRenderTreeEditPtr, renderTreeEdit, RenderTreeEditPointer, EditType } from './RenderTreeEdit';
import { getTreeFramePtr, renderTreeFrame, FrameType, RenderTreeFramePointer } from './RenderTreeFrame';
import { platform } from '../Environment';
let raiseEventMethod: MethodHandle;
let renderComponentMethod: MethodHandle;

export class BrowserRenderer {
  private childComponentLocations: { [componentId: number]: Element } = {};

  constructor(private browserRendererId: number) {
  }

  public attachComponentToElement(componentId: number, element: Element) {
    this.childComponentLocations[componentId] = element;
  }

  public updateComponent(componentId: number, edits: System_Array<RenderTreeEditPointer>, editsOffset: number, editsLength: number, referenceFrames: System_Array<RenderTreeFramePointer>) {
    const element = this.childComponentLocations[componentId];
    if (!element) {
      throw new Error(`No element is currently associated with component ${componentId}`);
    }

    this.applyEdits(componentId, element, 0, edits, editsOffset, editsLength, referenceFrames);
  }

  public disposeComponent(componentId: number) {
    delete this.childComponentLocations[componentId];
  }

  applyEdits(componentId: number, parent: Element, childIndex: number, edits: System_Array<RenderTreeEditPointer>, editsOffset: number, editsLength: number, referenceFrames: System_Array<RenderTreeFramePointer>) {
    let currentDepth = 0;
    let childIndexAtCurrentDepth = childIndex;
    const maxEditIndexExcl = editsOffset + editsLength;
    for (let editIndex = editsOffset; editIndex < maxEditIndexExcl; editIndex++) {
      const edit = getRenderTreeEditPtr(edits, editIndex);
      const editType = renderTreeEdit.type(edit);
      switch (editType) {
        case EditType.prependFrame: {
          const frameIndex = renderTreeEdit.newTreeIndex(edit);
          const frame = getTreeFramePtr(referenceFrames, frameIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          this.insertFrame(componentId, parent, childIndexAtCurrentDepth + siblingIndex, referenceFrames, frame, frameIndex);
          break;
        }
        case EditType.removeFrame: {
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          removeNodeFromDOM(parent, childIndexAtCurrentDepth + siblingIndex);
          break;
        }
        case EditType.setAttribute: {
          const frameIndex = renderTreeEdit.newTreeIndex(edit);
          const frame = getTreeFramePtr(referenceFrames, frameIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          const element = parent.childNodes[childIndexAtCurrentDepth + siblingIndex] as HTMLElement;
          this.applyAttribute(componentId, element, frame);
          break;
        }
        case EditType.removeAttribute: {
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          removeAttributeFromDOM(parent, childIndexAtCurrentDepth + siblingIndex, renderTreeEdit.removedAttributeName(edit)!);
          break;
        }
        case EditType.updateText: {
          const frameIndex = renderTreeEdit.newTreeIndex(edit);
          const frame = getTreeFramePtr(referenceFrames, frameIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          const domTextNode = parent.childNodes[childIndexAtCurrentDepth + siblingIndex] as Text;
          domTextNode.textContent = renderTreeFrame.textContent(frame);
          break;
        }
        case EditType.stepIn: {
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          parent = parent.childNodes[childIndexAtCurrentDepth + siblingIndex] as HTMLElement;
          currentDepth++;
          childIndexAtCurrentDepth = 0;
          break;
        }
        case EditType.stepOut: {
          parent = parent.parentElement!;
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

  insertFrame(componentId: number, parent: Element, childIndex: number, frames: System_Array<RenderTreeFramePointer>, frame: RenderTreeFramePointer, frameIndex: number): number {
    const frameType = renderTreeFrame.frameType(frame);
    switch (frameType) {
      case FrameType.element:
        this.insertElement(componentId, parent, childIndex, frames, frame, frameIndex);
        return 1;
      case FrameType.text:
        this.insertText(parent, childIndex, frame);
        return 1;
      case FrameType.attribute:
        throw new Error('Attribute frames should only be present as leading children of element frames.');
      case FrameType.component:
        this.insertComponent(parent, childIndex, frame);
        return 1;
      case FrameType.region:
        return this.insertFrameRange(componentId, parent, childIndex, frames, frameIndex + 1, frameIndex + renderTreeFrame.subtreeLength(frame));
      default:
        const unknownType: never = frameType; // Compile-time verification that the switch was exhaustive
        throw new Error(`Unknown frame type: ${unknownType}`);
    }
  }

  insertElement(componentId: number, parent: Element, childIndex: number, frames: System_Array<RenderTreeFramePointer>, frame: RenderTreeFramePointer, frameIndex: number) {
    const tagName = renderTreeFrame.elementName(frame)!;
    const newDomElement = document.createElement(tagName);
    insertNodeIntoDOM(newDomElement, parent, childIndex);

    // Apply attributes
    const descendantsEndIndexExcl = frameIndex + renderTreeFrame.subtreeLength(frame);
    for (let descendantIndex = frameIndex + 1; descendantIndex < descendantsEndIndexExcl; descendantIndex++) {
      const descendantFrame = getTreeFramePtr(frames, descendantIndex);
      if (renderTreeFrame.frameType(descendantFrame) === FrameType.attribute) {
        this.applyAttribute(componentId, newDomElement, descendantFrame);
      } else {
        // As soon as we see a non-attribute child, all the subsequent child frames are
        // not attributes, so bail out and insert the remnants recursively
        this.insertFrameRange(componentId, newDomElement, 0, frames, descendantIndex, descendantsEndIndexExcl);
        break;
      }
    }
  }

  insertComponent(parent: Element, childIndex: number, frame: RenderTreeFramePointer) {
    // Currently, to support O(1) lookups from render tree frames to DOM nodes, we rely on
    // each child component existing as a single top-level element in the DOM. To guarantee
    // that, we wrap child components in these 'blazor-component' wrappers.
    // To improve on this in the future:
    // - If we can statically detect that a given component always produces a single top-level
    //   element anyway, then don't wrap it in a further nonstandard element
    // - If we really want to support child components producing multiple top-level frames and
    //   not being wrapped in a container at all, then every time a component is refreshed in
    //   the DOM, we could update an array on the parent element that specifies how many DOM
    //   nodes correspond to each of its render tree frames. Then when that parent wants to
    //   locate the first DOM node for a render tree frame, it can sum all the frame counts for
    //   all the preceding render trees frames. It's O(N), but where N is the number of siblings
    //   (counting child components as a single item), so N will rarely if ever be large.
    //   We could even keep track of whether all the child components happen to have exactly 1
    //   top level frames, and in that case, there's no need to sum as we can do direct lookups.
    const containerElement = document.createElement('blazor-component');
    insertNodeIntoDOM(containerElement, parent, childIndex);

    // All we have to do is associate the child component ID with its location. We don't actually
    // do any rendering here, because the diff for the child will appear later in the render batch.
    const childComponentId = renderTreeFrame.componentId(frame);
    this.attachComponentToElement(childComponentId, containerElement);
  }

  insertText(parent: Element, childIndex: number, textFrame: RenderTreeFramePointer) {
    const textContent = renderTreeFrame.textContent(textFrame)!;
    const newDomTextNode = document.createTextNode(textContent);
    insertNodeIntoDOM(newDomTextNode, parent, childIndex);
  }

  applyAttribute(componentId: number, toDomElement: Element, attributeFrame: RenderTreeFramePointer) {
    const attributeName = renderTreeFrame.attributeName(attributeFrame)!;
    const browserRendererId = this.browserRendererId;
    const eventHandlerId = renderTreeFrame.attributeEventHandlerId(attributeFrame);

    if (attributeName === 'value') {
      if (this.tryApplyValueProperty(toDomElement, renderTreeFrame.attributeValue(attributeFrame))) {
        return; // If this DOM element type has special 'value' handling, don't also write it as an attribute
      }
    }

    // TODO: Instead of applying separate event listeners to each DOM element, use event delegation
    // and remove all the _blazor*Listener hacks
    switch (attributeName) {
      case 'onclick': {
        toDomElement.removeEventListener('click', toDomElement['_blazorClickListener']);
        const listener = evt => raiseEvent(evt, browserRendererId, componentId, eventHandlerId, 'mouse', { Type: 'click' });
        toDomElement['_blazorClickListener'] = listener;
        toDomElement.addEventListener('click', listener);
        break;
      }
      case 'onchange': {
        toDomElement.removeEventListener('change', toDomElement['_blazorChangeListener']);
        const listener = evt => raiseEvent(evt, browserRendererId, componentId, eventHandlerId, 'change', { Type: 'change', Value: evt.target.value });
        toDomElement['_blazorChangeListener'] = listener;
        toDomElement.addEventListener('change', listener);
        break;
      }
      case 'onkeypress': {
        toDomElement.removeEventListener('keypress', toDomElement['_blazorKeypressListener']);
        const listener = evt => {
          // This does not account for special keys nor cross-browser differences. So far it's
          // just to establish that we can pass parameters when raising events.
          // We use C#-style PascalCase on the eventInfo to simplify deserialization, but this could
          // change if we introduced a richer JSON library on the .NET side.
          raiseEvent(evt, browserRendererId, componentId, eventHandlerId, 'keyboard', { Type: evt.type, Key: (evt as any).key });
        };
        toDomElement['_blazorKeypressListener'] = listener;
        toDomElement.addEventListener('keypress', listener);
        break;
      }
      default:
        // Treat as a regular string-valued attribute
        toDomElement.setAttribute(
          attributeName,
          renderTreeFrame.attributeValue(attributeFrame)!
        );
        break;
    }
  }

  tryApplyValueProperty(element: Element, value: string | null) {
    // Certain elements have built-in behaviour for their 'value' property
    switch (element.tagName) {
      case 'INPUT':
      case 'SELECT': // Note: this doen't handle <select> correctly: https://github.com/aspnet/Blazor/issues/157
        (element as any).value = value;
        return true;
      default:
        return false;
    }
  }

  insertFrameRange(componentId: number, parent: Element, childIndex: number, frames: System_Array<RenderTreeFramePointer>, startIndex: number, endIndexExcl: number): number {
    const origChildIndex = childIndex;
    for (let index = startIndex; index < endIndexExcl; index++) {
      const frame = getTreeFramePtr(frames, index);
      const numChildrenInserted = this.insertFrame(componentId, parent, childIndex, frames, frame, index);
      childIndex += numChildrenInserted;

      // Skip over any descendants, since they are already dealt with recursively
      const subtreeLength = renderTreeFrame.subtreeLength(frame);
      if (subtreeLength > 1) {
        index += subtreeLength - 1;
      }
    }

    return (childIndex - origChildIndex); // Total number of children inserted
  }
}

function insertNodeIntoDOM(node: Node, parent: Element, childIndex: number) {
  if (childIndex >= parent.childNodes.length) {
    parent.appendChild(node);
  } else {
    parent.insertBefore(node, parent.childNodes[childIndex]);
  }
}

function removeNodeFromDOM(parent: Element, childIndex: number) {
  parent.removeChild(parent.childNodes[childIndex]);
}

function removeAttributeFromDOM(parent: Element, childIndex: number, attributeName: string) {
  const element = parent.childNodes[childIndex] as Element;
  element.removeAttribute(attributeName);
}

function raiseEvent(event: Event, browserRendererId: number, componentId: number, eventHandlerId: number, eventInfoType: EventInfoType, eventInfo: any) {
  event.preventDefault();

  if (!raiseEventMethod) {
    raiseEventMethod = platform.findMethod(
      'Microsoft.AspNetCore.Blazor.Browser', 'Microsoft.AspNetCore.Blazor.Browser.Rendering', 'BrowserRendererEventDispatcher', 'DispatchEvent'
    );
  }

  const eventDescriptor = {
    BrowserRendererId: browserRendererId,
    ComponentId: componentId,
    EventHandlerId: eventHandlerId,
    EventArgsType: eventInfoType
  };

  platform.callMethod(raiseEventMethod, null, [
    platform.toDotNetString(JSON.stringify(eventDescriptor)),
    platform.toDotNetString(JSON.stringify(eventInfo))
  ]);
}

type EventInfoType = 'mouse' | 'keyboard' | 'change';
