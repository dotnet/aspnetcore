// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventFieldInfo } from './EventFieldInfo';
import { eventNameAliasRegisteredCallbacks, getBrowserEventName, getEventNameAliases, getEventTypeOptions } from './EventTypes';
import { dispatchEvent } from '../WebRendererInteropMethods';

const nonBubblingEvents = toLookup([
  'abort',
  'blur',
  'cancel',
  'canplay',
  'canplaythrough',
  'change',
  'close',
  'cuechange',
  'durationchange',
  'emptied',
  'ended',
  'error',
  'focus',
  'load',
  'loadeddata',
  'loadedmetadata',
  'loadend',
  'loadstart',
  'mouseenter',
  'mouseleave',
  'pointerenter',
  'pointerleave',
  'pause',
  'play',
  'playing',
  'progress',
  'ratechange',
  'reset',
  'scroll',
  'seeked',
  'seeking',
  'stalled',
  'submit',
  'suspend',
  'timeupdate',
  'toggle',
  'unload',
  'volumechange',
  'waiting',
  'DOMNodeInsertedIntoDocument',
  'DOMNodeRemovedFromDocument',
]);

const alwaysPreventDefaultEvents: { [eventType: string]: boolean } = { submit: true };

const disableableEventNames = toLookup([
  'click',
  'dblclick',
  'mousedown',
  'mousemove',
  'mouseup',
]);

// Responsible for adding/removing the eventInfo on an expando property on DOM elements, and
// calling an EventInfoStore that deals with registering/unregistering the underlying delegated
// event listeners as required (and also maps actual events back to the given callback).
export class EventDelegator {
  private static nextEventDelegatorId = 0;

  private readonly eventsCollectionKey: string;

  private readonly afterClickCallbacks: ((event: MouseEvent) => void)[] = [];

  private eventInfoStore: EventInfoStore;

  constructor(private browserRendererId: number) {
    const eventDelegatorId = ++EventDelegator.nextEventDelegatorId;
    this.eventsCollectionKey = `_blazorEvents_${eventDelegatorId}`;
    this.eventInfoStore = new EventInfoStore(this.onGlobalEvent.bind(this));
  }

  public setListener(element: Element, eventName: string, eventHandlerId: number, renderingComponentId: number): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    const existingHandler = infoForElement.getHandler(eventName);

    if (existingHandler) {
      // We can cheaply update the info on the existing object and don't need any other housekeeping
      // Note that this also takes care of updating the eventHandlerId on the existing handler object
      this.eventInfoStore.update(existingHandler.eventHandlerId, eventHandlerId);
    } else {
      // Go through the whole flow which might involve registering a new global handler
      const newInfo = { element, eventName, eventHandlerId, renderingComponentId };
      this.eventInfoStore.add(newInfo);
      infoForElement.setHandler(eventName, newInfo);
    }
  }

  public getHandler(eventHandlerId: number): EventHandlerInfo {
    return this.eventInfoStore.get(eventHandlerId);
  }

  public removeListener(eventHandlerId: number): void {
    // This method gets called whenever the .NET-side code reports that a certain event handler
    // has been disposed. However we will already have disposed the info about that handler if
    // the eventHandlerId for the (element,eventName) pair was replaced during diff application.
    const info = this.eventInfoStore.remove(eventHandlerId);
    if (info) {
      // Looks like this event handler wasn't already disposed
      // Remove the associated data from the DOM element
      const element = info.element;
      const elementEventInfos = this.getEventHandlerInfosForElement(element, false);
      if (elementEventInfos) {
        elementEventInfos.removeHandler(info.eventName);
      }
    }
  }

  public notifyAfterClick(callback: (event: MouseEvent) => void): void {
    // This is extremely special-case. It's needed so that navigation link click interception
    // can be sure to run *after* our synthetic bubbling process. If a need arises, we can
    // generalise this, but right now it's a purely internal detail.
    this.afterClickCallbacks.push(callback);
    this.eventInfoStore.addGlobalListener('click'); // Ensure we always listen for this
  }

  public setStopPropagation(element: Element, eventName: string, value: boolean): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    infoForElement.stopPropagation(eventName, value);
  }

  public setPreventDefault(element: Element, eventName: string, value: boolean): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    infoForElement.preventDefault(eventName, value);
  }

  private onGlobalEvent(evt: Event) {
    if (!(evt.target instanceof Element)) {
      return;
    }

    // Always dispatch to any listeners for the original underlying browser event name
    this.dispatchGlobalEventToAllElements(evt.type, evt);

    // If this event name has aliases, dispatch for those listeners too
    const eventNameAliases = getEventNameAliases(evt.type);
    eventNameAliases && eventNameAliases.forEach(alias =>
      this.dispatchGlobalEventToAllElements(alias, evt));

    // Special case for navigation interception
    if (evt.type === 'click') {
      this.afterClickCallbacks.forEach(callback => callback(evt as MouseEvent));
    }
  }

  private dispatchGlobalEventToAllElements(eventName: string, browserEvent: Event) {
    // Note that 'eventName' can be an alias. For example, eventName may be 'click.special'
    // while browserEvent.type may be 'click'.

    // Use the event's 'path' rather than the chain of parent nodes, since the path gives
    // visibility into shadow roots.
    const path = browserEvent.composedPath();

    // Scan up the element hierarchy, looking for any matching registered event handlers
    let candidateEventTarget = path.shift();
    let eventArgs: unknown = null; // Populate lazily
    let eventArgsIsPopulated = false;
    const eventIsNonBubbling = Object.prototype.hasOwnProperty.call(nonBubblingEvents, eventName);
    let stopPropagationWasRequested = false;
    while (candidateEventTarget) {
      const candidateElement = candidateEventTarget as Element;
      const handlerInfos = this.getEventHandlerInfosForElement(candidateElement, false);
      if (handlerInfos) {
        const handlerInfo = handlerInfos.getHandler(eventName);
        if (handlerInfo && !eventIsDisabledOnElement(candidateElement, browserEvent.type)) {
          // We are going to raise an event for this element, so prepare info needed by the .NET code
          if (!eventArgsIsPopulated) {
            const eventOptionsIfRegistered = getEventTypeOptions(eventName);
            // For back-compat, if there's no registered createEventArgs, we supply empty event args (not null).
            // But if there is a registered createEventArgs, it can supply anything (including null).
            eventArgs = eventOptionsIfRegistered?.createEventArgs
              ? eventOptionsIfRegistered.createEventArgs(browserEvent)
              : {};
            eventArgsIsPopulated = true;
          }

          // For certain built-in events, having any .NET handler implicitly means we will prevent
          // the browser's default behavior. This has to be based on the original browser event type name,
          // not any alias (e.g., if you create a custom 'submit' variant, it should still preventDefault).
          if (Object.prototype.hasOwnProperty.call(alwaysPreventDefaultEvents, browserEvent.type)) {
            browserEvent.preventDefault();
          }

          dispatchEvent(this.browserRendererId, {
            eventHandlerId: handlerInfo.eventHandlerId,
            eventName: eventName,
            eventFieldInfo: EventFieldInfo.fromEvent(handlerInfo.renderingComponentId, browserEvent),
          }, eventArgs);
        }

        if (handlerInfos.stopPropagation(eventName)) {
          stopPropagationWasRequested = true;
        }

        if (handlerInfos.preventDefault(eventName)) {
          browserEvent.preventDefault();
        }
      }

      candidateEventTarget = (eventIsNonBubbling || stopPropagationWasRequested) ? undefined : path.shift();
    }
  }

  private getEventHandlerInfosForElement(element: Element, createIfNeeded: boolean): EventHandlerInfosForElement | null {
    if (Object.prototype.hasOwnProperty.call(element, this.eventsCollectionKey)) {
      return element[this.eventsCollectionKey];
    } else if (createIfNeeded) {
      return (element[this.eventsCollectionKey] = new EventHandlerInfosForElement());
    } else {
      return null;
    }
  }
}

// Responsible for adding and removing the global listener when the number of listeners
// for a given event name changes between zero and nonzero
class EventInfoStore {
  private infosByEventHandlerId: { [eventHandlerId: number]: EventHandlerInfo } = {};

  private countByEventName: { [eventName: string]: number } = {};

  constructor(private globalListener: EventListener) {
    eventNameAliasRegisteredCallbacks.push(this.handleEventNameAliasAdded.bind(this));
  }

  public add(info: EventHandlerInfo) {
    if (this.infosByEventHandlerId[info.eventHandlerId]) {
      // Should never happen, but we want to know if it does
      throw new Error(`Event ${info.eventHandlerId} is already tracked`);
    }

    this.infosByEventHandlerId[info.eventHandlerId] = info;

    this.addGlobalListener(info.eventName);
  }

  public get(eventHandlerId: number) {
    return this.infosByEventHandlerId[eventHandlerId];
  }

  public addGlobalListener(eventName: string) {
    // If this event name is an alias, update the global listener for the corresponding browser event
    eventName = getBrowserEventName(eventName);

    if (Object.prototype.hasOwnProperty.call(this.countByEventName, eventName)) {
      this.countByEventName[eventName]++;
    } else {
      this.countByEventName[eventName] = 1;

      // To make delegation work with non-bubbling events, register a 'capture' listener.
      // We preserve the non-bubbling behavior by only dispatching such events to the targeted element.
      const useCapture = Object.prototype.hasOwnProperty.call(nonBubblingEvents, eventName);
      document.addEventListener(eventName, this.globalListener, useCapture);
    }
  }

  public update(oldEventHandlerId: number, newEventHandlerId: number) {
    if (Object.prototype.hasOwnProperty.call(this.infosByEventHandlerId, newEventHandlerId)) {
      // Should never happen, but we want to know if it does
      throw new Error(`Event ${newEventHandlerId} is already tracked`);
    }

    // Since we're just updating the event handler ID, there's no need to update the global counts
    const info = this.infosByEventHandlerId[oldEventHandlerId];
    delete this.infosByEventHandlerId[oldEventHandlerId];
    info.eventHandlerId = newEventHandlerId;
    this.infosByEventHandlerId[newEventHandlerId] = info;
  }

  public remove(eventHandlerId: number): EventHandlerInfo {
    const info = this.infosByEventHandlerId[eventHandlerId];
    if (info) {
      delete this.infosByEventHandlerId[eventHandlerId];

      // If this event name is an alias, update the global listener for the corresponding browser event
      const eventName = getBrowserEventName(info.eventName);

      if (--this.countByEventName[eventName] === 0) {
        delete this.countByEventName[eventName];
        document.removeEventListener(eventName, this.globalListener);
      }
    }

    return info;
  }

  private handleEventNameAliasAdded(aliasEventName, browserEventName) {
    // If an event name alias gets registered later, we need to update the global listener
    // registrations to match. This makes it equivalent to the alias having been registered
    // before the elements with event handlers got rendered.
    if (Object.prototype.hasOwnProperty.call(this.countByEventName, aliasEventName)) {
      // Delete old
      const countByAliasEventName = this.countByEventName[aliasEventName];
      delete this.countByEventName[aliasEventName];
      document.removeEventListener(aliasEventName, this.globalListener);

      // Ensure corresponding count is added to new
      this.addGlobalListener(browserEventName);
      this.countByEventName[browserEventName] += countByAliasEventName - 1;
    }
  }
}

class EventHandlerInfosForElement {
  // Although we *could* track multiple event handlers per (element, eventName) pair
  // (since they have distinct eventHandlerId values), there's no point doing so because
  // our programming model is that you declare event handlers as attributes. An element
  // can only have one attribute with a given name, hence only one event handler with
  // that name at any one time.
  // So to keep things simple, only track one EventHandlerInfo per (element, eventName)
  private handlers: { [eventName: string]: EventHandlerInfo } = {};

  private preventDefaultFlags: { [eventName: string]: boolean } | null = null;

  private stopPropagationFlags: { [eventName: string]: boolean } | null = null;

  public getHandler(eventName: string): EventHandlerInfo | null {
    return Object.prototype.hasOwnProperty.call(this.handlers, eventName) ? this.handlers[eventName] : null;
  }

  public setHandler(eventName: string, handler: EventHandlerInfo) {
    this.handlers[eventName] = handler;
  }

  public removeHandler(eventName: string) {
    delete this.handlers[eventName];
  }

  public preventDefault(eventName: string, setValue?: boolean): boolean {
    if (setValue !== undefined) {
      this.preventDefaultFlags = this.preventDefaultFlags || {};
      this.preventDefaultFlags[eventName] = setValue;
    }

    return this.preventDefaultFlags ? this.preventDefaultFlags[eventName] : false;
  }

  public stopPropagation(eventName: string, setValue?: boolean): boolean {
    if (setValue !== undefined) {
      this.stopPropagationFlags = this.stopPropagationFlags || {};
      this.stopPropagationFlags[eventName] = setValue;
    }

    return this.stopPropagationFlags ? this.stopPropagationFlags[eventName] : false;
  }
}

export interface EventDescriptor {
  eventHandlerId: number;
  eventName: string;
  eventFieldInfo: EventFieldInfo | null;
}

interface EventHandlerInfo {
  element: Element;
  eventName: string;
  eventHandlerId: number;

  // The component whose tree includes the event handler attribute frame, *not* necessarily the
  // same component that will be re-rendered after the event is handled (since we re-render the
  // component that supplied the delegate, not the one that rendered the event handler frame)
  renderingComponentId: number;
}

function toLookup(items: string[]): { [key: string]: boolean } {
  const result = {};
  items.forEach(value => {
    result[value] = true;
  });
  return result;
}

function eventIsDisabledOnElement(element: Element, rawBrowserEventName: string): boolean {
  // We want to replicate the normal DOM event behavior that, for 'interactive' elements
  // with a 'disabled' attribute, certain mouse events are suppressed
  return (element instanceof HTMLButtonElement || element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement)
    && Object.prototype.hasOwnProperty.call(disableableEventNames, rawBrowserEventName)
    && element.disabled;
}
