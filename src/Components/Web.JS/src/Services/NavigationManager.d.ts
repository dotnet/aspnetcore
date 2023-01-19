import '@microsoft/dotnet-js-interop';
import { EventDelegator } from '../Rendering/Events/EventDelegator';
export declare const internalFunctions: {
    listenForNavigationEvents: typeof listenForNavigationEvents;
    enableNavigationInterception: typeof enableNavigationInterception;
    setHasLocationChangingListeners: typeof setHasLocationChangingListeners;
    endLocationChanging: typeof endLocationChanging;
    navigateTo: typeof navigateToFromDotNet;
    getBaseURI: () => string;
    getLocationHref: () => string;
};
declare function listenForNavigationEvents(locationChangedCallback: (uri: string, state: string | undefined, intercepted: boolean) => Promise<void>, locationChangingCallback: (callId: number, uri: string, state: string | undefined, intercepted: boolean) => Promise<void>): void;
declare function enableNavigationInterception(): void;
declare function setHasLocationChangingListeners(hasListeners: boolean): void;
export declare function attachToEventDelegator(eventDelegator: EventDelegator): void;
export declare function navigateTo(uri: string, options: NavigationOptions): void;
export declare function navigateTo(uri: string, forceLoad: boolean): void;
export declare function navigateTo(uri: string, forceLoad: boolean, replace: boolean): void;
declare function navigateToFromDotNet(uri: string, options: NavigationOptions): void;
declare function endLocationChanging(callId: number, shouldContinueNavigation: boolean): void;
export declare function toAbsoluteUri(relativeUri: string): string;
export interface NavigationOptions {
    forceLoad: boolean;
    replaceHistoryEntry: boolean;
    historyEntryState?: string;
}
export {};
