export function init(tableElement) {

    const bodyClickHandler = event => {
        const columnOptionsElement = tableElement.tHead.querySelector('.col-options');
        if (columnOptionsElement && event.composedPath().indexOf(columnOptionsElement) < 0) {
            tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
        }
    };
    const keyDownHandler = event => {
        const columnOptionsElement = tableElement.tHead.querySelector('.col-options');
        if (columnOptionsElement && event.key === "Escape") {
            tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
        }
    };
    let focusedRowIndex;
    let focusedRowText;
    const isClickableRow = target => target.parentElement === tableElement.tBodies[0] && target.classList.contains('row-clickable');
    const tableKeyDownHandler = event => {
        if ((event.key === 'Enter' || event.key === ' ') && isClickableRow(event.target)) {
            event.preventDefault();
            if (!event.repeat) {
                event.target.click();
            }
        }
    };
    const focusInHandler = event => {
        if (isClickableRow(event.target)) {
            focusedRowIndex = event.target.getAttribute('aria-rowindex');
            focusedRowText = event.target.textContent;
        } else {
            focusedRowIndex = null;
            focusedRowText = null;
        }
    };
    const focusOutHandler = event => {
        queueMicrotask(() => {
            if (event.target.isConnected && !tableElement.contains(document.activeElement)) {
                focusedRowIndex = null;
                focusedRowText = null;
            }
        });
    };
    const observer = new MutationObserver(() => {
        if (isClickableRow(document.activeElement)) {
            focusedRowIndex = document.activeElement.getAttribute('aria-rowindex');
            focusedRowText = document.activeElement.textContent;
            return;
        }
        if (focusedRowIndex && (document.activeElement === document.body || document.activeElement === document.documentElement)) {
            const row = tableElement.tBodies[0]?.querySelector(`:scope > tr.row-clickable[aria-rowindex="${focusedRowIndex}"]`);
            if (row?.textContent === focusedRowText) {
                row.focus({ preventScroll: true });
                return;
            }
        }
        if (!tableElement.contains(document.activeElement)) {
            focusedRowIndex = null;
            focusedRowText = null;
        }
    });
    observer.observe(tableElement, { childList: true, subtree: true, characterData: true });

    document.body.addEventListener('click', bodyClickHandler);
    document.body.addEventListener('mousedown', bodyClickHandler); // Otherwise it seems strange that it doesn't go away until you release the mouse button
    document.body.addEventListener('keydown', keyDownHandler);
    tableElement.addEventListener('keydown', tableKeyDownHandler);
    tableElement.addEventListener('focusin', focusInHandler);
    tableElement.addEventListener('focusout', focusOutHandler);

    return {
        stop: () => {
            observer.disconnect();
            document.body.removeEventListener('click', bodyClickHandler);
            document.body.removeEventListener('mousedown', bodyClickHandler);
            document.body.removeEventListener('keydown', keyDownHandler);
            tableElement.removeEventListener('keydown', tableKeyDownHandler);
            tableElement.removeEventListener('focusin', focusInHandler);
            tableElement.removeEventListener('focusout', focusOutHandler);
        }
    };
}

export function checkColumnOptionsPosition(tableElement) {
    const colOptions = tableElement.tHead && tableElement.tHead.querySelector('.col-options'); // Only match within *our* thead, not nested tables
    if (colOptions) {
        // We want the options popup to be positioned over the grid, not overflowing on either side, because it's possible that
        // beyond either side is off-screen or outside the scroll range of an ancestor
        const gridRect = tableElement.getBoundingClientRect();
        const optionsRect = colOptions.getBoundingClientRect();
        const leftOverhang = Math.max(0, gridRect.left - optionsRect.left);
        const rightOverhang = Math.max(0, optionsRect.right - gridRect.right);
        if (leftOverhang || rightOverhang) {
            // In the unlikely event that it overhangs both sides, we'll center it
            const applyOffset = leftOverhang && rightOverhang ? (leftOverhang - rightOverhang) / 2 : (leftOverhang - rightOverhang);
            colOptions.style.transform = `translateX(${applyOffset}px)`;
        }

        colOptions.scrollIntoViewIfNeeded();

        const autoFocusElem = colOptions.querySelector('[autofocus]');
        if (autoFocusElem) {
            autoFocusElem.focus();
        }
    }
}
