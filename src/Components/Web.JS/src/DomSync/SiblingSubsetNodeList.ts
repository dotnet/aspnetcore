export class SiblingSubsetNodeList implements ItemList<Node> {
  private readonly siblings: NodeList;
  private readonly startIndex: number;
  private readonly endIndexExcl: number;

  readonly length: number;

  item(index: number): Node | null {
    return this.siblings.item(this.startIndex + index);
  }

  forEach(callbackfn: (value: Node, key: number, parent: ItemList<Node>) => void, thisArg?: any): void {
    for (let i = 0; i < this.length; i++) {
      callbackfn.call(thisArg, this.item(i)!, i, this);
    }
  }

  constructor(startExcl: Comment, endExcl: Comment) {
    this.siblings = startExcl.parentNode!.childNodes;
    this.startIndex = Array.prototype.indexOf.call(this.siblings, startExcl) + 1;
    this.endIndexExcl = Array.prototype.indexOf.call(this.siblings, endExcl);
    this.length = this.endIndexExcl - this.startIndex;
  }
}

export interface ItemList<T> { // Designed to be compatible with NodeList
  readonly length: number;
  item(index: number): T | null;
  forEach(callbackfn: (value: T, key: number, parent: ItemList<T>) => void, thisArg?: any): void;
}
