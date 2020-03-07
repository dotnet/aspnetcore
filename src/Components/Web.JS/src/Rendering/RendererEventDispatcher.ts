import { EventDescriptor } from './BrowserRenderer';
import { UIEventArgs } from './EventForDotNet';

type EventDispatcher = (eventDescriptor: EventDescriptor, eventArgs: UIEventArgs) => void;

let eventDispatcherInstance: EventDispatcher;

export function dispatchEvent(eventDescriptor: EventDescriptor, eventArgs: UIEventArgs): void {
  if (!eventDispatcherInstance) {
    throw new Error('eventDispatcher not initialized. Call \'setEventDispatcher\' to configure it.');
  }

  return eventDispatcherInstance(eventDescriptor, eventArgs);
}

export function setEventDispatcher(newDispatcher: (eventDescriptor: EventDescriptor, eventArgs: UIEventArgs) => Promise<void>): void {
  eventDispatcherInstance = newDispatcher;
}
