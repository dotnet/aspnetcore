const rowNavigators = new WeakMap();

export function init(tableElement) {
    const rowNavigator = initRowNavigation(tableElement);
    rowNavigators.set(tableElement, rowNavigator);

    const bodyClickHandler = event => {
        const columnOptionsElement = tableElement.tHead.querySelector('.col-options');
        if (columnOptionsElement && event.composedPath().indexOf(columnOptionsElement) < 0) {
            tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
        }
    };
    const keyDownHandler = event => {
        rowNavigator.prepareEntry(event);
        const columnOptionsElement = tableElement.tHead.querySelector('.col-options');
        if (columnOptionsElement && event.key === "Escape") {
            tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
        }
    };

    document.body.addEventListener('click', bodyClickHandler);
    document.body.addEventListener('mousedown', bodyClickHandler); // Otherwise it seems strange that it doesn't go away until you release the mouse button
    document.body.addEventListener('keydown', keyDownHandler);

    return {
        stop: () => {
            rowNavigator.stop();
            rowNavigators.delete(tableElement);
            document.body.removeEventListener('click', bodyClickHandler);
            document.body.removeEventListener('mousedown', bodyClickHandler);
            document.body.removeEventListener('keydown', keyDownHandler);
        }
    };
}

export function getPendingRowIndex(tableElement) {
    return rowNavigators.get(tableElement)?.pendingRowIndex() ?? null;
}

function initRowNavigation(table) {
    const originalTabIndexes = new Map();
    const controlSelector = 'a[href], button, input, select, textarea, [tabindex], [contenteditable], summary, audio[controls], video[controls]';
    let rows = [];
    let headers = [];
    let currentTarget;
    let rowIndex = 2;
    let columnIndex = -1;
    let pendingIndex = null;
    let editingCell;
    let focusedInside = false;

    const enabled = () => table.hasAttribute('data-row-navigation') && table.getAttribute('role') === 'grid';
    const virtualized = () => table.getAttribute('data-row-navigation') === 'virtualize';
    const restoreTabIndex = (element, value) => {
        if (value === null) {
            element.removeAttribute('tabindex');
        } else {
            element.setAttribute('tabindex', value);
        }
    };
    const remember = element => {
        if (!originalTabIndexes.has(element)) {
            originalTabIndexes.set(element, element.getAttribute('tabindex'));
        }
    };
    const targetInRow = row => columnIndex < 0 ? row : row.cells[Math.min(columnIndex, row.cells.length - 1)];
    const firstVisibleRow = () => {
        let top = 0;
        let bottom = window.innerHeight;
        let left = 0;
        let right = window.innerWidth;
        for (let ancestor = table.parentElement; ancestor; ancestor = ancestor.parentElement) {
            const style = getComputedStyle(ancestor);
            const rect = ancestor.getBoundingClientRect();
            if (style.overflowY !== 'visible') {
                top = Math.max(top, rect.top + ancestor.clientTop);
                bottom = Math.min(bottom, rect.top + ancestor.clientTop + ancestor.clientHeight);
            }
            if (style.overflowX !== 'visible') {
                left = Math.max(left, rect.left + ancestor.clientLeft);
                right = Math.min(right, rect.left + ancestor.clientLeft + ancestor.clientWidth);
            }
        }
        return rows.find(row => {
            const rect = row.getBoundingClientRect();
            return Math.max(rect.top, top) < Math.min(rect.bottom, bottom)
                && Math.max(rect.left, left) < Math.min(rect.right, right);
        }) ?? rows[0];
    };
    const update = () => {
        if (!enabled()) {
            for (const [element, value] of originalTabIndexes) {
                restoreTabIndex(element, value);
            }
            originalTabIndexes.clear();
            currentTarget = editingCell = undefined;
            pendingIndex = null;
            focusedInside = false;
            rowIndex = 2;
            columnIndex = -1;
            return;
        }

        rows = Array.from(table.tBodies[0]?.rows ?? []).filter(row => row.classList.contains('row-clickable'));
        headers = Array.from(table.tHead?.rows[0]?.cells ?? []);
        const targets = [...headers, ...rows.flatMap(row => [row, ...row.cells])];
        const active = document.activeElement;
        const restoreFocus = focusedInside && !table.contains(active);
        let pendingReady = false;
        if (editingCell && !table.contains(editingCell)) {
            editingCell = undefined;
        }
        if (pendingIndex !== null) {
            const pendingRow = rows.find(row => Number(row.getAttribute('aria-rowindex')) === pendingIndex + 2);
            if (pendingRow) {
                currentTarget = targetInRow(pendingRow);
                pendingIndex = null;
                pendingReady = true;
            }
        }
        if (!focusedInside) {
            currentTarget = firstVisibleRow() ?? headers[0];
        }
        if (!targets.includes(currentTarget)) {
            const nearestRow = rows.find(row => Number(row.getAttribute('aria-rowindex')) >= rowIndex) ?? rows.at(-1);
            currentTarget = nearestRow ? targetInRow(nearestRow) : headers[0];
        }

        for (const [element, value] of originalTabIndexes) {
            if (!table.contains(element)) {
                restoreTabIndex(element, value);
                originalTabIndexes.delete(element);
            }
        }
        for (const element of targets) {
            remember(element);
            element.tabIndex = element === currentTarget && !editingCell ? 0 : -1;
        }
        for (const element of table.querySelectorAll(controlSelector)) {
            if (targets.includes(element) || element.closest('table') !== table || element.closest('.col-options')) {
                continue;
            }
            remember(element);
            if (editingCell?.contains(element)) {
                restoreTabIndex(element, originalTabIndexes.get(element));
            } else {
                element.tabIndex = -1;
            }
        }
        if ((restoreFocus || (pendingReady && focusedInside)) && pendingIndex === null) {
            currentTarget?.focus();
        }
    };
    const focusTarget = target => {
        editingCell = undefined;
        pendingIndex = null;
        currentTarget = target;
        update();
        target?.focus();
    };
    const focusRow = index => {
        const lastIndex = Number(table.getAttribute('data-row-count'));
        const lastRowIndex = virtualized() ? lastIndex + 1
            : Number(rows.at(-1)?.getAttribute('aria-rowindex') ?? 2);
        index = Math.max(2, Math.min(index, lastRowIndex));
        const row = rows.find(row => Number(row.getAttribute('aria-rowindex')) === index);
        if (row) {
            focusTarget(targetInRow(row));
        } else if (virtualized() && lastIndex > 0) {
            pendingIndex = index - 2;
            table.dispatchEvent(new CustomEvent('quickgridnavigate', { bubbles: true }));
        }
    };
    const focusIn = event => {
        if (!enabled() || event.target.closest('table') !== table || event.target.closest('.col-options')) {
            return;
        }
        const row = event.target.closest('tr');
        const cell = event.target.closest('td, th');
        if (!rows.includes(row) && !headers.includes(cell)) {
            return;
        }
        focusedInside = true;
        currentTarget = event.target === row ? row : cell;
        rowIndex = Number(row.getAttribute('aria-rowindex') ?? 1);
        columnIndex = event.target === row ? -1 : cell.cellIndex;
        editingCell = event.target === currentTarget ? undefined : cell;
        update();
    };
    const focusOut = event => {
        queueMicrotask(() => {
            if (event.target.isConnected && !table.contains(document.activeElement)) {
                focusedInside = false;
                editingCell = undefined;
                pendingIndex = null;
                columnIndex = -1;
                update();
            }
        });
    };
    const keyDown = event => {
        if (!enabled() || event.defaultPrevented || event.target.closest('table') !== table || event.target.closest('.col-options')
            || event.altKey || event.metaKey || event.shiftKey) {
            return;
        }
        const row = event.target.closest('tr');
        const cell = event.target.closest('td, th');
        const isRow = rows.includes(row) && event.target === row;
        const isCell = event.target === cell && (rows.includes(row) || headers.includes(cell));
        if (!isRow && !isCell) {
            if (event.key === 'Escape' && editingCell) {
                event.preventDefault();
                event.stopPropagation();
                focusTarget(editingCell);
            }
            return;
        }
        if (event.ctrlKey) {
            return;
        }
        rowIndex = Number(row.getAttribute('aria-rowindex') ?? 1);
        columnIndex = isRow ? -1 : cell.cellIndex;
        switch (event.key) {
            case 'Enter':
            case 'F2': {
                event.preventDefault();
                if (event.repeat) {
                    return;
                }
                const controls = isCell ? Array.from(cell.querySelectorAll(controlSelector)).filter(element =>
                    element.closest('table') === table && !element.matches(':disabled') && element.getClientRects().length) : [];
                if (controls.length) {
                    if (event.key === 'Enter' && headers.includes(cell)) {
                        (cell.querySelector('a.col-title, button.col-title') ?? controls[0]).click();
                    } else {
                        editingCell = cell;
                        update();
                        controls[0].focus();
                    }
                } else if (event.key === 'Enter' && rows.includes(row)) {
                    row.click();
                }
                break;
            }
            case 'ArrowUp': {
                event.preventDefault();
                if (virtualized()) {
                    // This key navigates rows, not a user scroll that should cancel Virtualize's alignment.
                    event.stopPropagation();
                }
                const index = pendingIndex === null ? rowIndex : pendingIndex + 2;
                if (index === 2) {
                    focusTarget(headers[Math.max(0, columnIndex)]);
                } else if (index > 2) {
                    focusRow(index - 1);
                }
                break;
            }
            case 'ArrowDown':
                event.preventDefault();
                if (virtualized()) {
                    event.stopPropagation();
                }
                focusRow((pendingIndex === null ? rowIndex : pendingIndex + 2) + 1);
                break;
            case 'ArrowLeft':
            case 'ArrowRight': {
                event.preventDefault();
                const direction = (event.key === 'ArrowRight' ? 1 : -1) * (getComputedStyle(table).direction === 'rtl' ? -1 : 1);
                const nextColumn = columnIndex + direction;
                if (isRow) {
                    focusTarget(row.cells[direction > 0 ? 0 : row.cells.length - 1]);
                } else if (nextColumn >= 0 && nextColumn < row.cells.length) {
                    focusTarget(row.cells[nextColumn]);
                } else if (rows.includes(row)) {
                    focusTarget(row);
                }
                break;
            }
        }
    };
    const observer = new MutationObserver(update);
    observer.observe(table, { childList: true, subtree: true, attributes: true, attributeFilter: ['role', 'data-row-navigation', 'data-row-count', 'class', 'aria-rowindex', 'aria-rowcount'] });
    table.addEventListener('focusin', focusIn);
    table.addEventListener('focusout', focusOut);
    table.addEventListener('keydown', keyDown);
    update();

    return {
        prepareEntry: event => {
            if (event.key === 'Tab' && enabled() && !table.contains(event.target)) {
                focusedInside = false;
                update();
            }
        },
        pendingRowIndex: () => pendingIndex,
        stop: () => {
            observer.disconnect();
            table.removeEventListener('focusin', focusIn);
            table.removeEventListener('focusout', focusOut);
            table.removeEventListener('keydown', keyDown);
            for (const [element, value] of originalTabIndexes) {
                restoreTabIndex(element, value);
            }
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
