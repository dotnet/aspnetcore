import '/Sortable.min.js';

export function enableDragging(container, componentInstance) {
    Sortable.create(container, {
        handle: '.draghandle',
        onEnd: evt => {
            // Preserve the original DOM until the component re-renders
            const offset = evt.oldIndex > evt.newIndex ? 1 : 0;
            evt.from.insertBefore(evt.item, container.children[evt.oldIndex + offset]);

            componentInstance.invokeMethodAsync('ChangeIngredientsOrder', evt.oldIndex, evt.newIndex);
        }
    });
}
