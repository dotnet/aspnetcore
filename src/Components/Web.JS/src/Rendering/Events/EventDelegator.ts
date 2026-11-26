// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EventFieldInfo } from './EventFieldInfo';
import {
  eventNameAliasRegisteredCallbacks, getBrowserEventName, getEventNameAliases, getEventTypeOptions, usesTrailingThrottle,
} from './EventTypes';
import { isRendererAttached, dispatchEvent } from '../WebRendererInteropMethods';
import {
  beginInFlightUserInputDispatch,
  clearDispatchedUserInputTracking,
  setHasPendingUserInput,
} from '../DomSpecialPropertyUtil';

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

// Keys pressed while an IME composes text (for example, Enter selecting a Japanese candidate) belong to the IME.
// Browsers report keyCode 229 for such keys, including the key that starts a composition.
function isImeCompositionKeyEvent(event: KeyboardEvent): boolean {
  return event.isComposing || event.keyCode === 229;
}

// Leaving an element or pressing Enter in it dispatches its pending events,
// so .NET observes what the user did before they acted on it.
function isFlushTriggerEvent(event: Event): boolean {
  return event.type === 'blur'
    || (event.type === 'keydown' && event instanceof KeyboardEvent && event.key === 'Enter' && !isImeCompositionKeyEvent(event));
}

// Throttling key events limits only auto-repeat. Each fresh key press gets through.
function isFreshKeyPress(event: Event): boolean {
  return (event.type === 'keydown' || event.type === 'keypress') && event instanceof KeyboardEvent && !event.repeat;
}

// While these events are delayed, the element's value is protected from stale renders.
function reportsElementValue(eventName: string): boolean {
  const browserEventName = getBrowserEventName(eventName);
  return browserEventName === 'input' || browserEventName === 'change';
}

// Responsible for adding/removing the eventInfo on an expando property on DOM elements, and
// calling an EventInfoStore that deals with registering/unregistering the underlying delegated
// event listeners as required (and also maps actual events back to the given callback).
export class EventDelegator {
  private static nextEventDelegatorId = 0;

  private readonly eventsCollectionKey: string;

  private readonly afterClickCallbacks: ((event: MouseEvent) => void)[] = [];

  private eventInfoStore: EventInfoStore;

  private renderBatchDepth = 0;

  private flushTriggerListenersAdded = false;

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
        this.cancelRateLimitState(elementEventInfos, info.eventName);
        elementEventInfos.removeHandler(info.eventName);
      }
    }
  }

  public removeListenersForElement(element: Element): void {
    // This method gets called whenever the .NET-side code reports that a certain element
    // has been disposed. We remove all event handlers for that element.
    const infosForElement = this.getEventHandlerInfosForElement(element, false);
    if (infosForElement) {
      for (const handlerInfo of infosForElement.enumerateHandlers()) {
        this.cancelRateLimitState(infosForElement, handlerInfo.eventName);
        this.eventInfoStore.remove(handlerInfo.eventHandlerId);
      }

      for (const state of infosForElement.enumerateRateLimitStates()) {
        this.clearRateLimitTimer(state);
        state.pendingSnapshot = null;
      }

      for (const eventName of infosForElement.enumerateDebounceEventNames()) {
        this.setDebounce(element, eventName, 0);
      }

      for (const eventName of infosForElement.enumerateThrottleEventNames()) {
        this.setThrottle(element, eventName, 0);
      }

      clearDispatchedUserInputTracking(element);
      delete element[this.eventsCollectionKey];
    }
  }

  // Applying a render batch can blur the focused element, for example when Chrome removes or moves it.
  // Such a blur doesn't dispatch pending events, because the user didn't leave the element and its handler may already be disposed.
  public beginRenderBatch(): void {
    this.renderBatchDepth++;
  }

  public endRenderBatch(): void {
    this.renderBatchDepth--;
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
    const currentValue = infoForElement.stopPropagation(eventName);
    infoForElement.stopPropagation(eventName, value);

    if (!currentValue && value) {
      this.eventInfoStore.addGlobalListener(eventName);
    } else if (currentValue && !value) {
      this.eventInfoStore.decrementCountByEventName(eventName);
    }
  }

  public setPreventDefault(element: Element, eventName: string, value: boolean): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    const currentValue = infoForElement.preventDefault(eventName);
    infoForElement.preventDefault(eventName, value);

    if (!currentValue && value) {
      // To ensure that preventDefault works for wheel and touch events,,
      // we need to register a listener with the passive mode explicitly disabled.
      // Note that this does not change behavior for other events as those
      // use active mode by default.
      this.eventInfoStore.addActiveGlobalListener(eventName);
    } else if (currentValue && !value) {
      this.eventInfoStore.decrementCountByEventName(eventName);
    }
  }

  public setDebounce(element: Element, eventName: string, durationMs: number): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    const previousDuration = infoForElement.debounce(eventName);
    const normalizedDuration = durationMs > 0 ? durationMs : 0;

    if (normalizedDuration > 0 && infoForElement.throttle(eventName) > 0) {
      this.setThrottle(element, eventName, 0);
    }

    infoForElement.debounce(eventName, normalizedDuration);

    if (previousDuration === 0 && normalizedDuration > 0) {
      this.eventInfoStore.addGlobalListener(eventName);
      this.addFlushTriggerListeners();
    } else if (previousDuration > 0 && normalizedDuration === 0) {
      this.flushRateLimitState(infoForElement, eventName);
      this.eventInfoStore.decrementCountByEventName(getBrowserEventName(eventName));
    }
  }

  public setThrottle(element: Element, eventName: string, durationMs: number): void {
    const infoForElement = this.getEventHandlerInfosForElement(element, true)!;
    const previousDuration = infoForElement.throttle(eventName);
    const normalizedDuration = durationMs > 0 ? durationMs : 0;

    if (normalizedDuration > 0 && infoForElement.debounce(eventName) > 0) {
      this.setDebounce(element, eventName, 0);
    }

    infoForElement.throttle(eventName, normalizedDuration);

    if (previousDuration === 0 && normalizedDuration > 0) {
      this.eventInfoStore.addGlobalListener(eventName);
      this.addFlushTriggerListeners();
    } else if (previousDuration > 0 && normalizedDuration === 0) {
      this.flushRateLimitState(infoForElement, eventName);
      this.eventInfoStore.decrementCountByEventName(getBrowserEventName(eventName));
    }
  }

  // Registered once and never removed, so flushing doesn't depend on which handlers exist elsewhere on the page.
  // Navigation interception already listens to click, so in practice this doesn't add a click listener.
  private addFlushTriggerListeners(): void {
    if (!this.flushTriggerListenersAdded) {
      this.flushTriggerListenersAdded = true;
      this.eventInfoStore.addGlobalListener('blur');
      this.eventInfoStore.addGlobalListener('keydown');
      this.eventInfoStore.addGlobalListener('click');
    }
  }

  private onGlobalEvent(evt: Event) {
    if (!(evt.target instanceof Element)) {
      return;
    }

    if (!isRendererAttached(this.browserRendererId)) {
      // when connection closed, it will detachWebRendererInterop, so we need to check if the renderer is still attached
      return;
    }

    if (this.flushTriggerListenersAdded && this.renderBatchDepth === 0) {
      if (isFlushTriggerEvent(evt)) {
        this.flushPendingEventsOnPath(evt.composedPath(), evt.type);
      } else if (evt.type === 'click') {
        this.flushPendingEventsBeforeClickOutsideFocusedElement(evt);
      }
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

          const eventFieldInfo = EventFieldInfo.fromEvent(handlerInfo.renderingComponentId, browserEvent);
          const debounceMs = handlerInfos.debounce(eventName);
          const throttleMs = handlerInfos.throttle(eventName);

          if (debounceMs > 0) {
            this.scheduleDebouncedEvent(candidateElement, handlerInfos, eventName, debounceMs, eventFieldInfo, eventArgs);
          } else if (throttleMs > 0) {
            this.scheduleThrottledEvent(candidateElement, handlerInfos, eventName, throttleMs, eventFieldInfo, eventArgs, browserEvent);
          } else {
            dispatchEvent(this.browserRendererId, {
              eventHandlerId: handlerInfo.eventHandlerId,
              eventName: eventName,
              eventFieldInfo: eventFieldInfo,
            }, eventArgs);
          }
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

  private scheduleDebouncedEvent(
    element: Element,
    handlerInfos: EventHandlerInfosForElement,
    eventName: string,
    debounceMs: number,
    eventFieldInfo: EventFieldInfo | null,
    eventArgs: unknown
  ): void {
    const state = handlerInfos.getOrCreateRateLimitState(element, eventName);
    if (state.timerId !== null) {
      clearTimeout(state.timerId);
    }

    state.pendingSnapshot = { eventFieldInfo, eventArgs };

    if (reportsElementValue(eventName)) {
      setHasPendingUserInput(element, true);
    }

    state.timerId = setTimeout(() => {
      state.timerId = null;
      this.dispatchPendingSnapshot(state);
    }, debounceMs);
  }

  private scheduleThrottledEvent(
    element: Element,
    handlerInfos: EventHandlerInfosForElement,
    eventName: string,
    throttleMs: number,
    eventFieldInfo: EventFieldInfo | null,
    eventArgs: unknown,
    sourceEvent: Event
  ): void {
    const state = handlerInfos.getOrCreateRateLimitState(element, eventName);

    if (state.inThrottleCooldown && isFreshKeyPress(sourceEvent)) {
      this.clearRateLimitTimer(state);
      state.pendingSnapshot = null;
    }

    if (!state.inThrottleCooldown) {
      const handlerInfo = handlerInfos.getHandler(eventName);
      if (handlerInfo && isRendererAttached(this.browserRendererId)) {
        const onCompleted = (reportsElementValue(eventName) && eventFieldInfo)
          ? beginInFlightUserInputDispatch(element, eventFieldInfo.fieldValue, /* advanceSequence */ true)
          : undefined;
        dispatchEvent(this.browserRendererId, {
          eventHandlerId: handlerInfo.eventHandlerId,
          eventName: eventName,
          eventFieldInfo: eventFieldInfo,
        }, eventArgs, onCompleted);
      }

      state.inThrottleCooldown = true;
      state.timerId = setTimeout(() => {
        this.onThrottleCooldownElapsed(state);
      }, throttleMs);
    } else if (usesTrailingThrottle(eventName)) {
      state.pendingSnapshot = { eventFieldInfo, eventArgs };

      if (reportsElementValue(eventName)) {
        setHasPendingUserInput(element, true);
      }
    }
  }

  private onThrottleCooldownElapsed(state: EventRateLimitState): void {
    state.timerId = null;

    if (state.pendingSnapshot) {
      this.dispatchPendingSnapshot(state);
      const currentThrottleMs = state.handlerInfos.throttle(state.eventName);
      if (currentThrottleMs > 0) {
        state.inThrottleCooldown = true;
        state.timerId = setTimeout(() => {
          this.onThrottleCooldownElapsed(state);
        }, currentThrottleMs);
      } else {
        state.inThrottleCooldown = false;
      }
    } else {
      state.inThrottleCooldown = false;
    }
  }

  private dispatchPendingSnapshot(state: EventRateLimitState): void {
    const snapshot = state.pendingSnapshot;
    if (!snapshot) {
      return;
    }

    state.pendingSnapshot = null;

    if (reportsElementValue(state.eventName)) {
      setHasPendingUserInput(state.element, false);
    }

    if (!state.element.isConnected) {
      this.clearRateLimitTimer(state);
      return;
    }

    const handlerInfo = state.handlerInfos.getHandler(state.eventName);
    if (handlerInfo && isRendererAttached(this.browserRendererId)) {
      const onCompleted = (reportsElementValue(state.eventName) && snapshot.eventFieldInfo)
        ? beginInFlightUserInputDispatch(state.element, snapshot.eventFieldInfo.fieldValue)
        : undefined;
      dispatchEvent(this.browserRendererId, {
        eventHandlerId: handlerInfo.eventHandlerId,
        eventName: state.eventName,
        eventFieldInfo: snapshot.eventFieldInfo,
      }, snapshot.eventArgs, onCompleted);
    }
  }

  private flushPendingEventsOnPath(path: Iterable<EventTarget>, triggerEventType: string): void {
    for (const target of path) {
      const infos = target instanceof Element ? this.getEventHandlerInfosForElement(target, false) : null;
      if (infos) {
        for (const state of infos.enumerateRateLimitStates()) {
          // A pending event of the trigger's own type continues its own debounce or throttle window.
          if (state.pendingSnapshot && getBrowserEventName(state.eventName) !== triggerEventType) {
            this.clearRateLimitTimer(state);
            this.dispatchPendingSnapshot(state);
          }
        }
      }
    }
  }

  // This trigger exists for assistive technologies. Screen readers, voice control, and switch access can activate
  // an element that can't take focus, such as a clickable suggestion, without moving focus away from the element
  // the user typed in. No blur is raised then, so a click outside the focused element dispatches the pending events
  // of the focused element and its ancestors, as the blur would have done.
  // Clicks from scripts and on controls that keep focus by cancelling mousedown are covered the same way.
  private flushPendingEventsBeforeClickOutsideFocusedElement(click: Event): void {
    const focusedElement = getFocusedElement();
    if (focusedElement && click.composedPath().indexOf(focusedElement) === -1) {
      this.flushPendingEventsOnPath(composedSelfAndAncestors(focusedElement), click.type);
    }
  }

  // Used when rate limiting is turned off or switched to another mode while the handler stays attached.
  // The user already produced the pending event, so it is dispatched instead of being dropped.
  private flushRateLimitState(infos: EventHandlerInfosForElement, eventName: string): void {
    const state = infos.getRateLimitState(eventName);
    if (state) {
      this.clearRateLimitTimer(state);
      this.dispatchPendingSnapshot(state);
    }
  }

  private cancelRateLimitState(infos: EventHandlerInfosForElement, eventName: string): void {
    const state = infos.getRateLimitState(eventName);
    if (state) {
      this.clearRateLimitTimer(state);
      state.pendingSnapshot = null;
      if (reportsElementValue(eventName)) {
        setHasPendingUserInput(state.element, false);
      }
    }
  }

  private clearRateLimitTimer(state: EventRateLimitState): void {
    if (state.timerId !== null) {
      clearTimeout(state.timerId);
      state.timerId = null;
    }
    state.inThrottleCooldown = false;
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

  public addActiveGlobalListener(eventName: string) {
    // If this event name is an alias, update the global listener for the corresponding browser event
    eventName = getBrowserEventName(eventName);

    // If the listener for this event is already registered, we recreate it to ensure
    // that it is using the active mode.
    if (Object.prototype.hasOwnProperty.call(this.countByEventName, eventName)) {
      this.countByEventName[eventName]++;
      document.removeEventListener(eventName, this.globalListener);
    } else {
      this.countByEventName[eventName] = 1;
    }

    // To make delegation work with non-bubbling events, register a 'capture' listener.
    // We preserve the non-bubbling behavior by only dispatching such events to the targeted element.
    const useCapture = Object.prototype.hasOwnProperty.call(nonBubblingEvents, eventName);
    document.addEventListener(eventName, this.globalListener, { capture: useCapture, passive: false });
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
      this.decrementCountByEventName(eventName);
    }

    return info;
  }

  public decrementCountByEventName(eventName: string) {
    if (--this.countByEventName[eventName] === 0) {
      delete this.countByEventName[eventName];
      document.removeEventListener(eventName, this.globalListener);
    }
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

  private debounceIntervals: { [eventName: string]: number } | null = null;

  private throttleIntervals: { [eventName: string]: number } | null = null;

  private rateLimitStates: { [eventName: string]: EventRateLimitState } | null = null;

  public *enumerateHandlers() : IterableIterator<EventHandlerInfo> {
    for (const eventName in this.handlers) {
      if (Object.prototype.hasOwnProperty.call(this.handlers, eventName)) {
        yield this.handlers[eventName];
      }
    }
  }

  public *enumerateRateLimitStates() : IterableIterator<EventRateLimitState> {
    if (this.rateLimitStates) {
      for (const eventName in this.rateLimitStates) {
        if (Object.prototype.hasOwnProperty.call(this.rateLimitStates, eventName)) {
          yield this.rateLimitStates[eventName];
        }
      }
    }
  }

  public *enumerateDebounceEventNames() : IterableIterator<string> {
    if (this.debounceIntervals) {
      for (const eventName in this.debounceIntervals) {
        if (Object.prototype.hasOwnProperty.call(this.debounceIntervals, eventName) && this.debounceIntervals[eventName] > 0) {
          yield eventName;
        }
      }
    }
  }

  public *enumerateThrottleEventNames() : IterableIterator<string> {
    if (this.throttleIntervals) {
      for (const eventName in this.throttleIntervals) {
        if (Object.prototype.hasOwnProperty.call(this.throttleIntervals, eventName) && this.throttleIntervals[eventName] > 0) {
          yield eventName;
        }
      }
    }
  }

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

  public debounce(eventName: string, setValue?: number): number {
    if (setValue !== undefined) {
      this.debounceIntervals = this.debounceIntervals || {};
      this.debounceIntervals[eventName] = setValue;
    }

    return (this.debounceIntervals && this.debounceIntervals[eventName]) || 0;
  }

  public throttle(eventName: string, setValue?: number): number {
    if (setValue !== undefined) {
      this.throttleIntervals = this.throttleIntervals || {};
      this.throttleIntervals[eventName] = setValue;
    }

    return (this.throttleIntervals && this.throttleIntervals[eventName]) || 0;
  }

  public getRateLimitState(eventName: string): EventRateLimitState | null {
    return (this.rateLimitStates && this.rateLimitStates[eventName]) || null;
  }

  public getOrCreateRateLimitState(element: Element, eventName: string): EventRateLimitState {
    this.rateLimitStates = this.rateLimitStates || {};
    let state = this.rateLimitStates[eventName];
    if (!state) {
      state = {
        element,
        handlerInfos: this,
        eventName,
        timerId: null,
        inThrottleCooldown: false,
        pendingSnapshot: null,
      };
      this.rateLimitStates[eventName] = state;
    }
    return state;
  }
}

interface PendingEventSnapshot {
  eventFieldInfo: EventFieldInfo | null;
  eventArgs: unknown;
}

interface EventRateLimitState {
  element: Element;
  handlerInfos: EventHandlerInfosForElement;
  eventName: string;
  timerId: ReturnType<typeof setTimeout> | null;
  inThrottleCooldown: boolean;
  pendingSnapshot: PendingEventSnapshot | null;
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

// Event handlers also work inside open shadow trees, so focus is followed into them,
// and ancestors are walked through slots and shadow hosts in the same order as an event's composed path.
function getFocusedElement(): Element | null {
  let focusedElement = document.activeElement;
  while (focusedElement?.shadowRoot?.activeElement) {
    focusedElement = focusedElement.shadowRoot.activeElement;
  }
  return focusedElement;
}

function* composedSelfAndAncestors(element: Element): IterableIterator<Node> {
  for (let node: Node | null = element; node;) {
    yield node;
    node = node instanceof ShadowRoot ? node.host : ((node instanceof Element && node.assignedSlot) || node.parentNode);
  }
}

function eventIsDisabledOnElement(element: Element, rawBrowserEventName: string): boolean {
  // We want to replicate the normal DOM event behavior that, for 'interactive' elements
  // with a 'disabled' attribute, certain mouse events are suppressed
  return (element instanceof HTMLButtonElement || element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement)
    && Object.prototype.hasOwnProperty.call(disableableEventNames, rawBrowserEventName)
    && element.disabled;
}
