let handler = null;

export function register(target) {
    unregister();

    handler = event => {
        if (event.ctrlKey || event.altKey || event.metaKey || event.repeat) {
            return;
        }

        const tag = event.target?.tagName;
        if (tag === "INPUT" || tag === "TEXTAREA" || event.target?.isContentEditable) {
            return;
        }

        target.invokeMethodAsync("HandleKey", event.key);
    };

    window.addEventListener("keydown", handler);
}

export function unregister() {
    if (handler) {
        window.removeEventListener("keydown", handler);
        handler = null;
    }
}
