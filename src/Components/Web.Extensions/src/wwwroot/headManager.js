const getTitle = () => {
    return document.title;
};

const setTitle = (title) => {
    document.title = title;
};

const getDomMetaElement = (key) => {
    const elements = Array.from(document.getElementsByTagName('meta'));
    return elements.find(e => e.getAttribute(key.name) === key.value);
}

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
        const head = document.getElementsByTagName('head')[0];

        if (!head) {
            return false;
        }

        domMetaElement = document.createElement('meta');
        head.appendChild(domMetaElement);
    }

    domMetaElement.setAttribute(key.name, key.value);

    if (metaElement.content) {
        domMetaElement.setAttribute('content', metaElement.content);
    } else {
        domMetaElement.removeAttribute('content');
    }

    return true;
};

window._blazorHeadManager = {
    getTitle,
    setTitle,
    getMetaElement,
    setMetaElement,
};
