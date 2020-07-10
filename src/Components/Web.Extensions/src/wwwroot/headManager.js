// Local helpers

const getDomMetaElement = (key) => {
    const elements = Array.from(document.getElementsByTagName('meta'));
    return elements.find(e => e.getAttribute(key.name) === key.value);
};

const getDomLinkElement = (id) => {
    const elements = Array.from(document.getElementsByTagName('link'));
    return elements.find(e => e._blazorLinkId === id);
};

const createHeadElement = (tagName) => {
    const head = document.getElementsByTagName('head')[0];
    const element = document.createElement(tagName);

    head.appendChild(element);

    return element;
};

// Exported functions

const getTitle = () => {
    return document.title;
};

const setTitle = (title) => {
    document.title = title;
};

const getMetaElement = (key) => {
    const domMetaElement = getDomMetaElement(key);

    if (!domMetaElement) {
        return undefined;
    }

    return {
        key,
        content: domMetaElement.getAttribute('content'),
    };
};

const setMetaElement = (key, metaElement) => {
    let domMetaElement = getDomMetaElement(key);

    if (!metaElement) {
        domMetaElement && domMetaElement.remove();
        return true;
    }

    if (!domMetaElement) {
        domMetaElement = createHeadElement('meta');
    }

    domMetaElement.setAttribute(key.name, key.value);

    if (metaElement.content) {
        domMetaElement.setAttribute('content', metaElement.content);
    } else {
        domMetaElement.removeAttribute('content');
    }

    return true;
};

const setLinkElement = (id, attributes) => {
    let domLinkElement = getDomLinkElement(id);

    if (domLinkElement) {
        // Remove existing attributes
        while (domLinkElement.attributes.length > 0) {
            domLinkElement.removeAttribute(domLinkElement.attributes[0].name);
        }
    } else {
        domLinkElement = createHeadElement('link');
        domLinkElement._blazorLinkId = id;
    }

    for (const attributeName in attributes) {
        if (attributes.hasOwnProperty(attributeName)) {
            domLinkElement.setAttribute(attributeName, attributes[attributeName]);
        }
    }
};

const deleteLinkElement = (id) => {
    const domLinkElement = getDomLinkElement(id);
    domLinkElement && domLinkElement.remove();
};

window._blazorHeadManager = {
    getTitle,
    setTitle,
    getMetaElement,
    setMetaElement,
    setLinkElement,
    deleteLinkElement,
};
