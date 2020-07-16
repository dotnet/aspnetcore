(function () {
    // Local helpers

    const blazorIdAttributeName = '_blazor_id';
    const headCommentRegularExpression = /\W*Head:[^{]*(.*)$/;
    const prerenderedTags = [];

    function createHeadTag({ tagName, attributes }, id) {
        const tagElement = document.createElement(tagName);

        // The id is undefined during prerendering
        if (id) {
            tagElement.setAttribute(blazorIdAttributeName, id);
        }

        if (attributes) {
            Object.keys(attributes).forEach(key => {
                tagElement.setAttribute(key, attributes[key]);
            });
        }

        document.head.appendChild(tagElement);

        return tagElement;
    }

    function resolvePrerenderedHeadComponents(node) {
        node.childNodes.forEach((childNode) => {
            const headElement = parseHeadComment(childNode);

            if (headElement) {
                applyPrerenderedHeadComponent(headElement);
            } else {
                resolvePrerenderedHeadComponents(childNode);
            }
        });
    }

    function applyPrerenderedHeadComponent(headElement) {
        switch (headElement.type) {
            case 'title':
                setTitle(headElement.title);
                break;
            case 'tag':
                const tag = createHeadTag(headElement);
                prerenderedTags.push(tag);
                break;
            default:
                throw new Error(`Invalid head element type '${headElement.type}'.`);
        }
    }

    function parseHeadComment(node) {
        if (!node || node.nodeType != Node.COMMENT_NODE) {
            return;
        }

        const commentText = node.textContent;

        if (!commentText) {
            return;
        }

        const definition = headCommentRegularExpression.exec(commentText);
        const json = definition && definition[1];

        return json && JSON.parse(json);
    }

    function removePrerenderedHeadTags() {
        prerenderedTags.forEach((tag) => {
            tag.remove();
        });

        prerenderedTags.length = 0;
    }

    // Exported functions

    function setTitle(title) {
        document.title = title;
    }

    function addOrUpdateHeadTag(tag, id) {
        removePrerenderedHeadTags();
        removeHeadTag(id);
        createHeadTag(tag, id);
    }

    function removeHeadTag(id) {
        let tag = document.head.querySelector(`[${blazorIdAttributeName}='${id}']`);
        tag && tag.remove();
    }

    window._blazorHeadManager = {
        setTitle,
        addOrUpdateHeadTag,
        removeHeadTag,
    };

    resolvePrerenderedHeadComponents(document);
})();
