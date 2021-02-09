import { EventDescriptor } from './BrowserRenderer';
type EventDispatcher = (eventDescriptor: EventDescriptor, eventArgs: any) => void;

let eventDispatcherInstance: EventDispatcher;

export function dispatchEvent(eventDescriptor: EventDescriptor, eventArgs: any): void {
  if (!eventDispatcherInstance) {
    throw new Error('eventDispatcher not initialized. Call \'setEventDispatcher\' to configure it.');
  }

  eventDispatcherInstance(eventDescriptor, eventArgs);
}

export function setEventDispatcher(newDispatcher: (eventDescriptor: EventDescriptor, eventArgs: any) => void): void {
  eventDispatcherInstance = newDispatcher;
}
