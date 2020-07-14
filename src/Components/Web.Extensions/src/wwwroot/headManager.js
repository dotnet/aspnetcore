// Local helpers

const getHeadElement = (tagName, id) => {
    const elements = Array.from(document.getElementsByTagName(tagName));
    return elements.find(e => e._blazorId === id);
}

const getOrCreateHeadElement = (tagName, id) => {
    let element = getHeadElement(tagName, id);

    if (element) {
        return element;
    }

    element = document.createElement(tagName);
    element._blazorId = id;

    const head = document.getElementsByTagName('head')[0];
    head.appendChild(element);

    return element;
};

// Exported functions

const setTitle = (title) => {
    document.title = title;
};

const setTag = (tagName, id, attributes) => {
    let tag = getOrCreateHeadElement(tagName, id);

    if (attributes) {
        for (let key in attributes) {
            if (attributes.hasOwnProperty(key)) {
                tag.setAttribute(key, attributes[key]);
            }
        }
    }
};

const removeTag = (tagName, id) => {
    let tag = getHeadElement(tagName, id);
    tag && tag.remove();
};

window._blazorHeadManager = {
    setTitle,
    setTag,
    removeTag,
};
