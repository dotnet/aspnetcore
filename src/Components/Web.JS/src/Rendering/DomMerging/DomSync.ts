import { ComparisonResult, ItemList, Operation, compareArrays } from './EditDistance';

export function synchronizeDomContent(destination: CommentBoundedRange, source: DocumentFragment) {
  const destinationParent = destination.startExclusive.parentNode!;

  const editScript = compareArrays(
    new SiblingSubsetNodeList(destination),
    source.childNodes,
    domNodeComparer);

  let nextDestinationNode = destination.startExclusive.nextSibling!; // Never null because it walks a range that ends with the end comment
  let nextSourceNode = source.firstChild; // Could be null

  // Handle any common leading items
  for (let i = 0; i < editScript.skipCount; i++) {
    treatAsMatch(nextDestinationNode, nextSourceNode!);
    nextDestinationNode = nextDestinationNode.nextSibling!;
    nextSourceNode = nextSourceNode!.nextSibling;
  }

  // Handle any edited region
  if (editScript.edits) {
    const edits = editScript.edits;
    const editsLength = edits.length;

    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const operation = edits[editIndex];
      switch (operation) {
        case Operation.Keep:
          treatAsMatch(nextDestinationNode, nextSourceNode!);
          nextDestinationNode = nextDestinationNode.nextSibling!;
          nextSourceNode = nextSourceNode!.nextSibling;
          break;
        case Operation.Delete:
          const nodeToRemove = nextDestinationNode;
          nextDestinationNode = nodeToRemove.nextSibling!;
          destinationParent.removeChild(nodeToRemove);
          break;
        case Operation.Insert:
          const nodeToInsert = nextSourceNode!;
          nextSourceNode = nodeToInsert.nextSibling;
          destinationParent.insertBefore(nodeToInsert, nextDestinationNode);
          break;
        default:
          throw new Error(`Unexpected operation: '${operation}'`);
      }
    }

    // Handle any common trailing items
    // These can only exist if there were some edits, otherwise everything would be in the set of common leading items
    while (nextDestinationNode !== destination.endExclusive) {
      treatAsMatch(nextDestinationNode, nextSourceNode!);
      nextDestinationNode = nextDestinationNode.nextSibling!;
      nextSourceNode = nextSourceNode!.nextSibling;
    }
    if (nextSourceNode) {
      // Should never be possible, as it would imply a bug in the edit script calculation, or possibly an unsupported
      // scenario like a DOM mutation observer modifying the destination nodes while we are working on them
      throw new Error('Updating the DOM failed because the sets of trailing nodes had inconsistent lengths.');
    }
  }
}

function treatAsMatch(destination: Node, source: Node) {
  throw new Error('Function not implemented.');
}

function domNodeComparer(a: Node, b: Node): ComparisonResult {
  // TODO
  return ComparisonResult.CannotSubstitute;
}

export interface CommentBoundedRange {
  startExclusive: Comment,
  endExclusive: Comment,
}

class SiblingSubsetNodeList implements ItemList<Node> {
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

  constructor(range: CommentBoundedRange) {
    if (!range.startExclusive.parentNode || range.startExclusive.parentNode !== range.endExclusive.parentNode) {
      throw new Error('Invalid CommentBoundedRange. The start and end markers have no common parent.');
    }

    this.siblings = range.startExclusive.parentNode!.childNodes;
    this.startIndex = Array.prototype.indexOf.call(this.siblings, range.startExclusive) + 1;
    this.endIndexExcl = Array.prototype.indexOf.call(this.siblings, range.endExclusive);
    this.length = this.endIndexExcl - this.startIndex;
  }
}
