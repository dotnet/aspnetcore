export function enableFormEnhancement() {
    document.body.addEventListener('submit', async evt => {
        const form = evt.target as HTMLFormElement;
        if (!form || form.getAttribute('enhance') === null) {
            return;
        }

        evt.preventDefault();

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
    });
}
