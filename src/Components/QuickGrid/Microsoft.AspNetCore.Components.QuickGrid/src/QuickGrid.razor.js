export function init(tableElement) {
    let resizeState = null;

    const applyColumnWidth = (columnIndex, width) => {
        for (const row of tableElement.rows) {
            const cell = row.cells[columnIndex];
            if (cell) {
                cell.style.width = `${width}px`;
                cell.style.minWidth = `${width}px`;
                cell.style.maxWidth = `${width}px`;
            }
        }
    };

    const stopColumnResize = () => {
        if (resizeState) {
            document.removeEventListener('mousemove', resizeState.mouseMoveHandler);
            document.removeEventListener('mouseup', resizeState.mouseUpHandler);
            resizeState = null;
            document.body.classList.remove('quickgrid-column-resizing');
        }
    };

    const startColumnResize = event => {
        if (event.button !== 0) {
            return;
        }

        const handle = event.target.closest('.col-width-draghandle');
        if (!handle || !tableElement.contains(handle)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const columnIndex = Number.parseInt(handle.dataset.columnIndex, 10);
        if (Number.isNaN(columnIndex)) {
            return;
        }

        const headerCell = handle.closest('th');
        if (!headerCell) {
            return;
        }
        const startWidth = headerCell.getBoundingClientRect().width;
        const startX = event.clientX;

        const mouseMoveHandler = moveEvent => {
            const width = Math.max(48, Math.round(startWidth + (moveEvent.clientX - startX)));
            applyColumnWidth(columnIndex, width);
            resizeState.currentWidth = width;
        };

        const mouseUpHandler = async upEvent => {
            const width = Math.max(48, Math.round(startWidth + (upEvent.clientX - startX)));
            applyColumnWidth(columnIndex, width);
            stopColumnResize();
        };

        resizeState = { mouseMoveHandler, mouseUpHandler, currentWidth: startWidth };
        document.body.classList.add('quickgrid-column-resizing');
        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler);

        applyColumnWidth(columnIndex, Math.round(startWidth));
    };

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

    document.body.addEventListener('click', bodyClickHandler);
    document.body.addEventListener('mousedown', bodyClickHandler); // Otherwise it seems strange that it doesn't go away until you release the mouse button
    document.body.addEventListener('keydown', keyDownHandler);
    tableElement.addEventListener('mousedown', startColumnResize);

    return {
        stop: () => {
            stopColumnResize();
            document.body.removeEventListener('click', bodyClickHandler);
            document.body.removeEventListener('mousedown', bodyClickHandler);
            document.body.removeEventListener('keydown', keyDownHandler);
            tableElement.removeEventListener('mousedown', startColumnResize);
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

