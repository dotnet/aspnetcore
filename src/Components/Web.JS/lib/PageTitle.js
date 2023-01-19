// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { getLogicalParent } from './Rendering/LogicalElements';
export const PageTitle = {
    getAndRemoveExistingTitle,
};
function getAndRemoveExistingTitle() {
    var _a;
    // Other <title> elements may exist outside <head> (e.g., inside <svg> elements) but they aren't page titles
    const titleElements = document.head ? document.head.getElementsByTagName('title') : [];
    if (titleElements.length === 0) {
        return null;
    }
    let existingTitle = null;
    for (let index = titleElements.length - 1; index >= 0; index--) {
        const currentTitleElement = titleElements[index];
        const previousSibling = currentTitleElement.previousSibling;
        const isBlazorTitle = previousSibling instanceof Comment && getLogicalParent(previousSibling) !== null;
        if (isBlazorTitle) {
            continue;
        }
        if (existingTitle === null) {
            existingTitle = currentTitleElement.textContent;
        }
        (_a = currentTitleElement.parentNode) === null || _a === void 0 ? void 0 : _a.removeChild(currentTitleElement);
    }
    return existingTitle;
}
//# sourceMappingURL=PageTitle.js.map