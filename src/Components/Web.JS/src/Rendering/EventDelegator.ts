import { EventForDotNet, UIEventArgs } from './EventForDotNet';
import { EventFieldInfo } from './EventFieldInfo';

const nonBubblingEvents = toLookup([
  'abort',
  'blur',
  'change',
  'error',
  'focus',
  'load',
  'loadend',
  'loadstart',
  'mouseenter',
  'mouseleave',
  'progress',
  'reset',
  'scroll',
  'submit',
  'unload',
  'DOMNodeInsertedIntoDocument',
  'DOMNodeRemovedFromDocument',
]);

export interface OnEventCallback {
  (event: Event, eventHandlerId: number, eventArgs: EventForDotNet<UIEventArgs>, eventFieldInfo: EventFieldInfo | null): void;
}

// Responsible for adding/removing the eventInfo on an expando property on DOM elements, and
// calling an EventInfoStore that deals with registering/unregistering the underlying delegated
// event listeners as required (and also maps actual events back to the given callback).
export class EventDelegator {
  private static nextEventDelegatorId = 0;

  private eventsCollectionKey: string;

  private eventInfoStore: EventInfoStore;

  constructor(private onEvent: OnEventCallback) {
    const eventDelegatorId = ++EventDelegator.nextEventDelegatorId;
    this.eventsCollectionKey = `_blazorEvents_${eventDelegatorId}`;
    this.eventInfoStore = new EventInfoStore(this.onGlobalEvent.bind(this));
  }

  public setListener(element: Element, eventName: string, eventHandlerId: number, renderingComponentId: number) {
    // Ensure we have a place to store event info for this element
    let infoForElement: EventHandlerInfosForElement = element[this.eventsCollectionKey];
    if (!infoForElement) {
      infoForElement = element[this.eventsCollectionKey] = {};
    }

    if (infoForElement.hasOwnProperty(eventName)) {
      // We can cheaply update the info on the existing object and don't need any other housekeeping
      const oldInfo = infoForElement[eventName];
      this.eventInfoStore.update(oldInfo.eventHandlerId, eventHandlerId);
    } else {
      // Go through the whole flow which might involve registering a new global handler
      const newInfo = { element, eventName, eventHandlerId, renderingComponentId };
      this.eventInfoStore.add(newInfo);
      infoForElement[eventName] = newInfo;
    }
  }

  public removeListener(eventHandlerId: number) {
    // This method gets called whenever the .NET-side code reports that a certain event handler
    // has been disposed. However we will already have disposed the info about that handler if
    // the eventHandlerId for the (element,eventName) pair was replaced during diff application.
    const info = this.eventInfoStore.remove(eventHandlerId);
    if (info) {
      // Looks like this event handler wasn't already disposed
      // Remove the associated data from the DOM element
      const element = info.element;
      if (element.hasOwnProperty(this.eventsCollectionKey)) {
        const elementEventInfos: EventHandlerInfosForElement = element[this.eventsCollectionKey];
        delete elementEventInfos[info.eventName];
        if (Object.getOwnPropertyNames(elementEventInfos).length === 0) {
          delete element[this.eventsCollectionKey];
        }
      }
    }
  }

  private onGlobalEvent(evt: Event) {
    if (!(evt.target instanceof Element)) {
      return;
    }

    // Scan up the element hierarchy, looking for any matching registered event handlers
    let candidateElement = evt.target as Element | null;
    let eventArgs: EventForDotNet<UIEventArgs> | null = null; // Populate lazily
    const eventIsNonBubbling = nonBubblingEvents.hasOwnProperty(evt.type);
    while (candidateElement) {
      if (candidateElement.hasOwnProperty(this.eventsCollectionKey)) {
        const handlerInfos: EventHandlerInfosForElement = candidateElement[this.eventsCollectionKey];
        if (handlerInfos.hasOwnProperty(evt.type)) {
          // We are going to raise an event for this element, so prepare info needed by the .NET code
          if (!eventArgs) {
            eventArgs = EventForDotNet.fromDOMEvent(evt);
          }

          const handlerInfo = handlerInfos[evt.type];
          const eventFieldInfo = EventFieldInfo.fromEvent(handlerInfo.renderingComponentId, evt);
          this.onEvent(evt, handlerInfo.eventHandlerId, eventArgs, eventFieldInfo);
        }
      }

      candidateElement = eventIsNonBubbling ? null : candidateElement.parentElement;
    }
  }
}

// Responsible for adding and removing the global listener when the number of listeners
// for a given event name changes between zero and nonzero
class EventInfoStore {
  private infosByEventHandlerId: { [eventHandlerId: number]: EventHandlerInfo } = {};

  private countByEventName: { [eventName: string]: number } = {};

  constructor(private globalListener: EventListener) {
  }

  public add(info: EventHandlerInfo) {
    if (this.infosByEventHandlerId[info.eventHandlerId]) {
      // Should never happen, but we want to know if it does
      throw new Error(`Event ${info.eventHandlerId} is already tracked`);
    }

    this.infosByEventHandlerId[info.eventHandlerId] = info;

    const eventName = info.eventName;
    if (this.countByEventName.hasOwnProperty(eventName)) {
      this.countByEventName[eventName]++;
    } else {
      this.countByEventName[eventName] = 1;

      // To make delegation work with non-bubbling events, register a 'capture' listener.
      // We preserve the non-bubbling behavior by only dispatching such events to the targeted element.
      const useCapture = nonBubblingEvents.hasOwnProperty(eventName);
      document.addEventListener(eventName, this.globalListener, useCapture);
    }
  }

  public update(oldEventHandlerId: number, newEventHandlerId: number) {
    if (this.infosByEventHandlerId.hasOwnProperty(newEventHandlerId)) {
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

      const eventName = info.eventName;
      if (--this.countByEventName[eventName] === 0) {
        delete this.countByEventName[eventName];
        document.removeEventListener(eventName, this.globalListener);
      }
    }

    return info;
  }
}

interface EventHandlerInfosForElement {
  // Although we *could* track multiple event handlers per (element, eventName) pair
  // (since they have distinct eventHandlerId values), there's no point doing so because
  // our programming model is that you declare event handlers as attributes. An element
  // can only have one attribute with a given name, hence only one event handler with
  // that name at any one time.
  // So to keep things simple, only track one EventHandlerInfo per (element, eventName)
  [eventName: string]: EventHandlerInfo;
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
