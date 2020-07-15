(function () {
    // Local helpers

    const blazorIdAttributeName = '_blazor_id';
    const headCommentRegularExpression = /\W*Head:[^{]*(.*)$/;
    const prerenderedTags = [];

    function createHeadTag({ tagName, attributes }, id) {
        const tagElement = document.createElement(tagName);

        if (id) {
            tagElement.setAttribute(blazorIdAttributeName, id);
        }

        if (attributes) {
            for (const key in attributes) {
                if (attributes.hasOwnProperty(key)) {
                    tagElement.setAttribute(key, attributes[key]);
                }
            }
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

        const headStartComment = new RegExp(headCommentRegularExpression);
        const definition = headStartComment.exec(commentText);
        const json = definition && definition[1];

        if (json) {
            try {
                return JSON.parse(json);
            } catch (error) {
                throw new Error(`Found malformed head comment '${commentText}'.`);
            }
        } else {
            return;
        }
    }

    // Exported functions

    function setTitle(title) {
        document.title = title;
    }

    function applyHeadTag(tag, id) {
        removeHeadTag(id);
        createHeadTag(tag, id);
    }

    function removeHeadTag(id) {
        let tag = document.head.querySelector(`[${blazorIdAttributeName}='${id}']`);
        tag && tag.remove();
    }

    function removePrerenderedHeadTags() {
        prerenderedTags.forEach((tag) => {
            tag.remove();
        });

        prerenderedTags.length = 0;
    }

    window._blazorHeadManager = {
        setTitle,
        applyHeadTag,
        removeHeadTag,
        removePrerenderedHeadTags,
    };

    resolvePrerenderedHeadComponents(document);
})();
