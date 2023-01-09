import { toAbsoluteUri } from "./Services/NavigationManager";

let _navigationCompletedCallback: Function;

export function enableNavigationEnhancement(navigationCompletedCallback: Function) {
    _navigationCompletedCallback = navigationCompletedCallback;
    document.body.addEventListener('click', onClick);
    window.addEventListener('popstate', onPopState);
}

export function disableNavigationEnhancement() {
    document.body.removeEventListener('click', onClick);
    window.removeEventListener('popstate', onPopState);
}

function onClick(event: MouseEvent) {
    const anchorTarget = findAnchorTarget(event);
    if (anchorTarget && canProcessAnchor(anchorTarget)) {
        const href = anchorTarget.getAttribute('href')!;
        const absoluteHref = toAbsoluteUri(href);

        if (isWithinOrigin(absoluteHref)) {
          event.preventDefault();

          history.pushState(null, '', href);

          // TODO: Needs something like a cancellation token because if you start a second navigation while one
          // is still in flight, we need to abort the first
          performEnhancedPageLoad(href);
        }
    }
}

async function onPopState(state: PopStateEvent) {
    performEnhancedPageLoad(location.href);
}

export async function performEnhancedPageLoad(url: string, fetchOptions?: RequestInit) {
    const response = await fetch(url, fetchOptions);
    const responseReader = response.body!.getReader();
    const decoder = new TextDecoder();
    let responseHtml = '';
    let finished = false;

    while (!finished) {
        const chunk = await responseReader.read();
        if (chunk.done) {
            finished = true;
        }

        if (chunk.value) {
            const chunkText = decoder.decode(chunk.value);
            responseHtml += chunkText;

            // This is obviously not robust. Maybe we can rely on the initial HTML always being in the first chunk.
            if (chunkText.indexOf('</html>') > 0) {
                break;
            }
        }
    }

    const parsedHtml = new DOMParser().parseFromString(responseHtml, 'text/html');
    document.body.innerHTML = parsedHtml.body.innerHTML;
    responseHtml = '';

    while (!finished) {
        const chunk = await responseReader.read();
        if (chunk.done) {
            finished = true;
        }

        if (chunk.value) {
            const chunkText = decoder.decode(chunk.value);
            responseHtml += chunkText;

            // Not making any attempt to cope if the chunk boundaries don't line up with the script blocks
            if (chunkText.indexOf('</script>') > 0) {
                const parsedHtml = new DOMParser().parseFromString(responseHtml, 'text/html');
                for (let i = 0; i < parsedHtml.scripts.length; i++) {
                    const script = parsedHtml.scripts[i];
                    if (script.textContent) {
                        eval(script.textContent);
                    }
                }
                responseHtml = '';
            }
        }
    }

    _navigationCompletedCallback();
}

function findAnchorTarget(event: MouseEvent): HTMLAnchorElement | null {
    // _blazorDisableComposedPath is a temporary escape hatch in case any problems are discovered
    // in this logic. It can be removed in a later release, and should not be considered supported API.
    const path = (event.composedPath && event.composedPath()) || [];
      // This logic works with events that target elements within a shadow root,
      // as long as the shadow mode is 'open'. For closed shadows, we can't possibly
      // know what internal element was clicked.
      for (let i = 0; i < path.length; i++) {
        const candidate = path[i];
        if (candidate instanceof Element && candidate.tagName === 'A') {
          return candidate as HTMLAnchorElement;
        }
      }
      return null;
}

function canProcessAnchor(anchorTarget: HTMLAnchorElement) {
    const targetAttributeValue = anchorTarget.getAttribute('target');
    const opensInSameFrame = !targetAttributeValue || targetAttributeValue === '_self';
    return opensInSameFrame && anchorTarget.hasAttribute('href') && !anchorTarget.hasAttribute('download');
}

export function isWithinOrigin(href: string) {
    const origin = document.location.origin;
    const nextChar = href.charAt(origin.length);

    return href.startsWith(origin)
      && (nextChar === '' || nextChar === '/' || nextChar === '?' || nextChar === '#');
}