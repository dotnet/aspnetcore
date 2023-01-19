export interface EventTypeOptions {
    browserEventName?: string;
    createEventArgs?: (event: Event) => unknown;
}
export declare const eventNameAliasRegisteredCallbacks: ((aliasEventName: string, browserEventName: any) => void)[];
export declare function registerCustomEventType(eventName: string, options: EventTypeOptions): void;
export declare function getEventTypeOptions(eventName: string): EventTypeOptions | undefined;
export declare function getEventNameAliases(eventName: string): string[] | undefined;
export declare function getBrowserEventName(possibleAliasEventName: string): string;
