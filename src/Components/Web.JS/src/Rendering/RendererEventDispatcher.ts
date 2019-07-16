import { EventDescriptor } from './BrowserRenderer';
import { UIEventArgs } from './EventForDotNet';

type EventDispatcher = (eventDescriptor: EventDescriptor, eventArgs: UIEventArgs) => Promise<void>;

let eventDispatcherInstance: EventDispatcher;

export function eventDispatcher(eventDescriptor: EventDescriptor, eventArgs: UIEventArgs): Promise<void> {
  if (!eventDispatcherInstance) {
    throw new Error('eventDispatcher not initialized. Call \'setEventDispatcher\' to configure it.');
  }

  return eventDispatcherInstance(eventDescriptor, eventArgs);
}

export function setEventDispatcher(newDispatcher: (eventDescriptor: EventDescriptor, eventArgs: UIEventArgs) => Promise<void>): void {
  eventDispatcherInstance = newDispatcher;
}
