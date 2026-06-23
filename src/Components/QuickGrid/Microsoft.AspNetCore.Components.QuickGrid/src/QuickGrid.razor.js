export function init(tableElement) {

    const applyColumnWidth = (columnIndex, width) => {
        const headerCell = tableElement.tHead.rows[0].cells[columnIndex];
        if (headerCell) {
            headerCell.style.width = `${width}px`;
        }
    };

    const stopColumnResize = () => {
        document.removeEventListener('pointermove', pointerMoveHandler);
        document.removeEventListener('pointerup', stop);
        document.removeEventListener('pointercancel', stop);
    };

    const startColumnResize = event => {
        if (event.button !== undefined && event.button !== 0) return;

        const handle = event.target.closest('.col-width-draghandle');
        if (!handle || !tableElement.contains(handle)) return;

        event.preventDefault();

        const columnIndex = Number.parseInt(handle.dataset.columnIndex, 10);
        if (Number.isNaN(columnIndex)) return;

        const headerCell = handle.closest('th');
        if (!headerCell) return;

        const startWidth = headerCell.offsetWidth;
        const startX = event.clientX;
        const isRtl = getComputedStyle(tableElement).direction === 'rtl';

        let currentWidth = startWidth;

        // ✅ STRONG pointer capture
        if (event.pointerId !== undefined) {
            try {
                handle.setPointerCapture(event.pointerId);
            } catch (e) {
                // ignore (some browsers throw)
            }
        }

        let active = true;

        const pointerMoveHandler = (moveEvent) => {
            if (!active) return;

            const clientX = moveEvent.clientX;

            const delta = isRtl ? (startX - clientX) : (clientX - startX);
            const width = Math.max(48, Math.round(startWidth + delta));
            currentWidth = width;

            // ✅ smooth rendering
            requestAnimationFrame(() => {
                applyColumnWidth(columnIndex, currentWidth);
            });
        };

        const stop = () => {
            active = false;

            document.removeEventListener('pointermove', pointerMoveHandler);
            document.removeEventListener('pointerup', stop);
            document.removeEventListener('pointercancel', stop);

            // ✅ release capture safely
            if (event.pointerId !== undefined && handle.releasePointerCapture) {
                try {
                    handle.releasePointerCapture(event.pointerId);
                } catch {}
            }

            applyColumnWidth(columnIndex, currentWidth);
        };

        document.addEventListener('pointermove', pointerMoveHandler);
        document.addEventListener('pointerup', stop);
        document.addEventListener('pointercancel', stop);
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
    const keyDownResizeHandler = (event) => {
        const handle = event.target.closest('.col-width-draghandle');
        if (!handle) return;

        const columnIndex = Number.parseInt(handle.dataset.columnIndex, 10);
        if (Number.isNaN(columnIndex)) return;

        const headerCell = tableElement.tHead.rows[0].cells[columnIndex];
        if (!headerCell) return;

        const MIN_WIDTH = 48;
        const STEP = event.shiftKey ? 30 : 10;

        const isRtl = getComputedStyle(tableElement).direction === 'rtl';
        let currentWidth = headerCell.getBoundingClientRect().width;
        let newWidth = currentWidth;

        if (event.key === 'ArrowRight') {
            newWidth = isRtl ? currentWidth - STEP : currentWidth + STEP;
        } else if (event.key === 'ArrowLeft') {
            newWidth = isRtl ? currentWidth + STEP : currentWidth - STEP;
        } else {
            return; // ignore other keys
        }

        event.preventDefault();

        newWidth = Math.max(MIN_WIDTH, Math.round(newWidth));
        applyColumnWidth(columnIndex, newWidth);
    };

    document.body.addEventListener('click', bodyClickHandler);
    document.body.addEventListener('mousedown', bodyClickHandler); // Otherwise it seems strange that it doesn't go away until you release the mouse button
    document.body.addEventListener('keydown', keyDownHandler);
    if (tableElement.querySelectorAll('.col-width-draghandle').length > 0) {
        tableElement.addEventListener('pointerdown', startColumnResize);
        tableElement.addEventListener('keydown', keyDownResizeHandler);
    }

    return {
        stop: () => {
            stopColumnResize();
            document.body.removeEventListener('click', bodyClickHandler);
            document.body.removeEventListener('mousedown', bodyClickHandler);
            document.body.removeEventListener('keydown', keyDownHandler);
            if (tableElement.querySelectorAll('.col-width-draghandle').length > 0) {
                tableElement.removeEventListener('pointerdown', startColumnResize);
                tableElement.removeEventListener('keydown', keyDownResizeHandler);
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

