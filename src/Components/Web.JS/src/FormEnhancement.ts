import { isWithinOrigin, performEnhancedPageLoad } from "./NavigationEnhancement";
import { toAbsoluteUri } from "./Services/NavigationManager";

export function enableFormEnhancement() {
    document.body.addEventListener('submit', async evt => {
        const form = evt.target as HTMLFormElement;
        if (!form || form.getAttribute('enhance') === null) {
            return;
        }

        const url = new URL(form.action);
        const fetchOptions: RequestInit = { method: form.method };
        const formData = new FormData(form);
        const submitter = evt.submitter as HTMLButtonElement;
        if (submitter && submitter.name) {
            formData.append(submitter.name, submitter.value);
        }
        if (fetchOptions.method === 'get') {
            (url as any).search = new URLSearchParams(formData as any);
        } else {
            fetchOptions.body = formData;
        }

        const absoluteHref = toAbsoluteUri(url.toString());

        if (isWithinOrigin(absoluteHref)) {
            evt.preventDefault();
            if (absoluteHref !== toAbsoluteUri(location.href)) {
                history.pushState(null, '', url);
            }

            await performEnhancedPageLoad(url.toString(), fetchOptions);
        }
    });
}
