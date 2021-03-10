import { EventFieldInfo } from './EventFieldInfo';

export interface EventDescriptor {
  browserRendererId: number;
  eventHandlerId: number;
  eventName: string;
  eventFieldInfo: EventFieldInfo | null;
}

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
