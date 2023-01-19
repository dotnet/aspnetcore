import { ReconnectDisplay } from './ReconnectDisplay';
import { Logger } from '../Logging/Logger';
export declare class DefaultReconnectDisplay implements ReconnectDisplay {
    private readonly maxRetries;
    private readonly document;
    private readonly logger;
    modal: HTMLDivElement;
    message: HTMLHeadingElement;
    button: HTMLButtonElement;
    addedToDom: boolean;
    reloadParagraph: HTMLParagraphElement;
    loader: HTMLDivElement;
    constructor(dialogId: string, maxRetries: number, document: Document, logger: Logger);
    show(): void;
    update(currentAttempt: number): void;
    hide(): void;
    failed(): void;
    rejected(): void;
    private getLoader;
}
