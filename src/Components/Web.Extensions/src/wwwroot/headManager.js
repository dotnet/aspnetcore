const getTitle = () => {
    return document.title;
};

const setTitle = (title) => {
    document.title = title;
};

const getMetaElement = (key) => {
    const keyName = ['name', 'http-equiv', 'charset'][key.name];
    const elements = Array.from(document.getElementsByTagName('meta'));
    let domMetaElement = elements.find(e => e.getAttribute(keyName) === key.id);

    if (!domMetaElement) {
        return undefined;
    }

    return {
        name: domMetaElement.getAttribute(keyName),
        content: domMetaElement.getAttribute('content'),
    };
};

const setMetaElement = (key, metaElement) => {
    const keyName = ['name', 'http-equiv', 'charset'][key.name];
    const elements = Array.from(document.getElementsByTagName('meta'));
    let domMetaElement = elements.find(e => e.getAttribute(keyName) === key.id);

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

    domMetaElement.setAttribute(keyName, key.id);

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
