const getTitle = () => {
    return document.title;
};

const setTitle = (title) => {
    document.title = title;
};

const getMetaElementByName = (name) => {
    const elements = Array.from(document.getElementsByTagName('meta'));
    const domMetaElement = elements.find(e => e.name === name);

    if (!domMetaElement) {
        return undefined;
    }

    return {
        name: domMetaElement.name,
        content: domMetaElement.content,
    };
};

const setMetaElementByName = (name, metaElement) => {
    const elements = Array.from(document.getElementsByTagName('meta'));
    let domMetaElement = elements.find(e => e.name === name);

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

    domMetaElement.name = metaElement.name;
    domMetaElement.content = metaElement.content;

    return true;
};

window._blazorHeadManager = {
    getTitle,
    setTitle,
    getMetaElementByName,
    setMetaElementByName,
};
