export declare function toLogicalRootCommentElement(start: Comment, end: Comment): LogicalElement;
export declare function toLogicalElement(element: Node, allowExistingContents?: boolean): LogicalElement;
export declare function emptyLogicalElement(element: LogicalElement): void;
export declare function createAndInsertLogicalContainer(parent: LogicalElement, childIndex: number): LogicalElement;
export declare function insertLogicalChild(child: Node, parent: LogicalElement, childIndex: number): void;
export declare function removeLogicalChild(parent: LogicalElement, childIndex: number): void;
export declare function getLogicalParent(element: LogicalElement): LogicalElement | null;
export declare function getLogicalSiblingEnd(element: LogicalElement): LogicalElement | null;
export declare function getLogicalChild(parent: LogicalElement, childIndex: number): LogicalElement;
export declare function isSvgElement(element: LogicalElement): boolean;
export declare function getLogicalChildrenArray(element: LogicalElement): LogicalElement[];
export declare function permuteLogicalChildren(parent: LogicalElement, permutationList: PermutationListEntry[]): void;
export declare function getClosestDomElement(logicalElement: LogicalElement): Element | (LogicalElement & DocumentFragment);
export interface PermutationListEntry {
    fromSiblingIndex: number;
    toSiblingIndex: number;
}
export interface LogicalElement {
    LogicalElement__DO_NOT_IMPLEMENT: any;
}
