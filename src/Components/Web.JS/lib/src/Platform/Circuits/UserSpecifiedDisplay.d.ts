import { ReconnectDisplay } from './ReconnectDisplay';
export declare class UserSpecifiedDisplay implements ReconnectDisplay {
    private dialog;
    private readonly maxRetries;
    private readonly document;
    static readonly ShowClassName = "components-reconnect-show";
    static readonly HideClassName = "components-reconnect-hide";
    static readonly FailedClassName = "components-reconnect-failed";
    static readonly RejectedClassName = "components-reconnect-rejected";
    static readonly MaxRetriesId = "components-reconnect-max-retries";
    static readonly CurrentAttemptId = "components-reconnect-current-attempt";
    constructor(dialog: HTMLElement, maxRetries: number, document: Document);
    show(): void;
    update(currentAttempt: number): void;
    hide(): void;
    failed(): void;
    rejected(): void;
    private removeClasses;
}
