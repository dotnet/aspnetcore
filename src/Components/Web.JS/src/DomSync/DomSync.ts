import { synchronizeAttributes } from './AttributeSync';
import { Operation, getEditScript, ComparisonResult } from './EditDistance';
import { ItemList, SiblingSubsetNodeList } from './SiblingSubsetNodeList';

interface NodeRange {
  parent: Node;
  
  // If set, we assume that startMarker and endMarker are both immediate children of parentNode,
  // and we are describing the range of siblings between the two
  subset?: SiblingSubset;
}

interface SiblingSubset {
  startMarker: Comment;
  endMarker: Comment;
}

export function synchronizeDOMContent(destinationWithOldContent: NodeRange, newContent: string | ParentNode) {  
  const treatAsWholeDocument = destinationWithOldContent.parent instanceof Document;
  const newContentFragment = parseContent(newContent, treatAsWholeDocument);
  synchronizeSubtree(destinationWithOldContent, newContentFragment);
}

function parseContent(content: string | Node, treatAsWholeDocument: boolean): Node {
  if (content instanceof Node) {
    return content;
  }

  // Annoyingly, neither HTML parsing approach handles all cases
  if (treatAsWholeDocument) {
    // This approach always returns a Document and will automatically surround your content with other elements
    // like <html> and <body>
    const parser = new DOMParser();
    return parser.parseFromString(content, 'text/html');
  } else {
    // This approach always strips off certain top-level elements like <body>
    const parserTemplate = document.createElement('template');
    parserTemplate.innerHTML = content;
    return parserTemplate.content;
  }
}

function synchronizeSubtree(destination: NodeRange, desiredEndState: Node) {
  const existingNodes = nodeRangeToItemList(destination);
  const edits = getEditScript(existingNodes, desiredEndState.childNodes, compareNodes);
  if (!edits) {
    return;
  }

  let destinationIndex = 0;
  let sourceIndex = 0;

  for (let editIndex = 0; editIndex < edits.length; editIndex++) {
    switch (edits[editIndex]) {
      case Operation.RetainAsIdentical:
        //console.log('Skip', existingNodes.item(destinationIndex));
        destinationIndex++;
        sourceIndex++;
        break;
      case Operation.RetainAsCompatible:
        //console.log('RetainWithEdit', existingNodes.item(destinationIndex), desiredEndState.childNodes[sourceIndex]);
        synchronizeNodeViaRetain(existingNodes.item(destinationIndex)!, desiredEndState.childNodes[sourceIndex]);
        destinationIndex++;
        sourceIndex++;
        break;
      case Operation.Insert:
        const nodeToInsert = desiredEndState.childNodes[sourceIndex];
        //console.log('Insert', nodeToInsert);
        destination.parent.insertBefore(nodeToInsert, existingNodes.item(destinationIndex));
        destinationIndex++;
        break;
      case Operation.Delete:
        const nodeToDelete = existingNodes.item(destinationIndex)!;
        //console.log('Delete', nodeToDelete);
        destination.parent.removeChild(nodeToDelete);
        break;
    }
  }
}

function synchronizeNodeViaRetain(destination: Node, endState: Node) {
  if (destination.nodeType !== endState.nodeType) {
    throw new Error('Can only retain nodes of the same type');
  }

  switch (destination.nodeType) {
    case NodeType.ELEMENT:
      synchronizeAttributes(destination as Element, endState as Element);
      synchronizeSubtree({ parent: destination }, endState);
      break;
    case NodeType.TEXT:
      //console.log(`Change text node content from '${(destination as Text).textContent}' to '${(endState as Text).textContent}'`);
      (destination as Text).textContent = (endState as Text).textContent;
      break;
    case NodeType.COMMENT:
      (destination as Text).textContent = (endState as Text).textContent;
      break;
    default:
      throw new Error(`Unsupported node type for synchronizeNodeViaRetain: ${destination.nodeType}`);
  }
}

function compareNodes(a: Node, b: Node): ComparisonResult {
  if (a.nodeType !== b.nodeType) {
    return ComparisonResult.Incompatible;
  }

  switch (a.nodeType) {
    case NodeType.ELEMENT:
      return compareElements(a as Element, b as Element);
    case NodeType.TEXT:
      return (a as Text).textContent === (b as Text).textContent ? ComparisonResult.Identical : ComparisonResult.Compatible;
    case NodeType.COMMENT:
      return (a as Text).textContent === (b as Text).textContent ? ComparisonResult.Identical : ComparisonResult.Compatible;
    case NodeType.DOCTYPE:
      // TODO: Would these ever have to be updated?
      return ComparisonResult.Identical;
    default:
      throw new Error(`Unsupported node type for diffing: ${a.nodeType}`);
  }
}

function compareElements(a: Element, b: Element): ComparisonResult {
  if (a.tagName !== b.tagName) {
    return ComparisonResult.Incompatible;
  }

  // If there are mismatching keys, prevent retention. Note that the converse isn't handled here and would have to be
  // covered as a post-processing step (i.e., even if elements *do* having matching keys, they may initially show as
  // being inserted and deleted, so we'd need to scan the list of inserts/deletes and find pairs we can match up to
  // treat as 'move' operations).
  const aKey = a.getAttribute('key');
  const bKey = b.getAttribute('key');
  return aKey === bKey ? ComparisonResult.Compatible : ComparisonResult.Incompatible;
}

function nodeRangeToItemList(range: NodeRange): ItemList<Node> {
  if (range.subset) {
    return new SiblingSubsetNodeList(range.subset.startMarker, range.subset.endMarker);
  } else {
    return range.parent.childNodes;
  }
}

const enum NodeType {
  ELEMENT = 1,
  TEXT = 3,
  COMMENT = 8,
  DOCTYPE = 10,
}
