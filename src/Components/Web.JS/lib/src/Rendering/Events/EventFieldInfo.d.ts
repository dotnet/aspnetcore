export declare class EventFieldInfo {
    componentId: number;
    fieldValue: string | boolean;
    constructor(componentId: number, fieldValue: string | boolean);
    static fromEvent(componentId: number, event: Event): EventFieldInfo | null;
}
