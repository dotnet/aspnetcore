import { EventFieldInfo } from './EventFieldInfo';
export declare class EventDelegator {
    private browserRendererId;
    private static nextEventDelegatorId;
    private readonly eventsCollectionKey;
    private readonly afterClickCallbacks;
    private eventInfoStore;
    constructor(browserRendererId: number);
    setListener(element: Element, eventName: string, eventHandlerId: number, renderingComponentId: number): void;
    getHandler(eventHandlerId: number): EventHandlerInfo;
    removeListener(eventHandlerId: number): void;
    notifyAfterClick(callback: (event: MouseEvent) => void): void;
    setStopPropagation(element: Element, eventName: string, value: boolean): void;
    setPreventDefault(element: Element, eventName: string, value: boolean): void;
    private onGlobalEvent;
    private dispatchGlobalEventToAllElements;
    private getEventHandlerInfosForElement;
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
    renderingComponentId: number;
}
export {};
