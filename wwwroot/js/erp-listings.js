/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    const rootSelector = "main.app-main .app-content, main.app-main > .content-header";
    const modulePath = normalizePath(window.location.pathname);
    let version = 0;
    /** @type {AbortController | undefined} */
    let pending;
    /** @type {Map<string, Record<string, unknown>>} */
    const configurations = new Map();

    /** @param {string} id */
    function config(id) {
        if (!configurations.has(id)) {
            const node = document.getElementById(id);
            if (!node?.textContent) throw new Error("Falta la configuración del módulo.");
            configurations.set(id, JSON.parse(node.textContent));
        }
        return configurations.get(id);
    }

    /** @param {string} path */
    function normalizePath(path) {
        return path.replace(/\/index\/?$/i, "").replace(/\/$/, "").toLowerCase();
    }
    /** @param {URL} url */
    function isModuleUrl(url) {
        return url.origin === window.location.origin && normalizePath(url.pathname) === modulePath;
    }
    /** @param {ParentNode} scope */
    function getRoot(scope) {
        return scope.querySelector(rootSelector);
    }
    /** @param {Element} current @param {Element} incoming */
    function copyAttributes(current, incoming) {
        Array.from(current.attributes).forEach(attribute => current.removeAttribute(attribute.name));
        Array.from(incoming.attributes).forEach(attribute => current.setAttribute(attribute.name, attribute.value));
    }
    /** @param {Element} control */
    function controlKey(control) {
        if (control.id) return "#" + control.id;
        const name = control.getAttribute("name");
        if (!name) return null;
        const type = control.getAttribute("type") || "";
        return control.tagName + ":" + name + ":" + type +
            (type === "radio" || type === "checkbox" ? ":" + control.getAttribute("value") : "");
    }
    /** @param {Element} current @param {Element} incoming */
    function retainForms(current, incoming) {
        const oldForms = Array.from(current.querySelectorAll('form[method="get"]'))
            .filter(form => !form.closest(".modal"));
        const newForms = Array.from(incoming.querySelectorAll('form[method="get"]'))
            .filter(form => !form.closest(".modal"));
        newForms.forEach((newForm, index) => {
            const oldForm = newForm.id ? oldForms.find(form => form.id === newForm.id) : oldForms[index];
            if (!oldForm) return;
            const controls = new Map();
            oldForm.querySelectorAll("input,select,textarea,button").forEach(control => {
                const key = controlKey(control);
                if (key) controls.set(key, control);
            });
            newForm.querySelectorAll("input,select,textarea,button").forEach(newControl => {
                const key = controlKey(newControl);
                const oldControl = key ? controls.get(key) : undefined;
                if (!oldControl || oldControl.tagName !== newControl.tagName) return;
                copyAttributes(oldControl, newControl);
                if (oldControl instanceof HTMLSelectElement && newControl instanceof HTMLSelectElement) {
                    const value = newControl.value;
                    const selected = new Set(Array.from(newControl.selectedOptions).map(option => option.value));
                    oldControl.replaceChildren(...Array.from(newControl.childNodes));
                    if (oldControl.multiple)
                        Array.from(oldControl.options).forEach(option => { option.selected = selected.has(option.value); });
                    else oldControl.value = value;
                } else if (oldControl instanceof HTMLInputElement && newControl instanceof HTMLInputElement) {
                    oldControl.value = newControl.value;
                    oldControl.checked = newControl.checked;
                } else if (oldControl instanceof HTMLTextAreaElement && newControl instanceof HTMLTextAreaElement) {
                    oldControl.value = newControl.value;
                } else {
                    oldControl.replaceChildren(...Array.from(newControl.childNodes));
                }
                newControl.replaceWith(oldControl);
            });
            copyAttributes(oldForm, newForm);
            oldForm.replaceChildren(...Array.from(newForm.childNodes));
            newForm.replaceWith(oldForm);
        });
    }
    /** @param {unknown} error */
    function reportError(error) {
        const message = error instanceof Error ? error.message : "No se pudo actualizar el listado.";
        if (typeof Swal !== "undefined") {
            void Swal.fire({ icon: "error", title: "No se pudo actualizar", text: message });
        } else {
            const root = getRoot(document);
            if (!root) return;
            const alert = document.createElement("div");
            alert.className = "alert alert-danger";
            alert.setAttribute("role", "alert");
            alert.textContent = message;
            root.prepend(alert);
        }
    }
    /** @param {string} href @param {boolean} pushHistory */
    async function load(href, pushHistory = true) {
        const root = getRoot(document);
        const url = new URL(href, window.location.href);
        if (!root || !isModuleUrl(url)) return;
        pending?.abort();
        pending = new AbortController();
        const signal = pending.signal;
        const requestVersion = ++version;
        const active = document.activeElement;
        const focused = active instanceof HTMLElement && root.contains(active) ? active : null;
        const selection = focused instanceof HTMLInputElement && focused.selectionStart !== null
            ? [focused.selectionStart, focused.selectionEnd] : null;
        root.querySelector("[data-erp-loading]")?.remove();
        const progress = document.createElement("div");
        progress.dataset.erpLoading = "";
        progress.className = "small text-body-secondary px-3 pb-2";
        progress.setAttribute("role", "status");
        progress.textContent = "Actualizando listado…";
        root.prepend(progress);
        root.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(url.toString(), {
                signal, credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" }
            });
            if (response.redirected)
                throw new Error("La sesión no está disponible. Inicie sesión nuevamente.");
            if (!response.ok) {
                const contentType = response.headers.get("content-type") || "";
                const failure = contentType.includes("application/json") ? await response.json() : null;
                throw new Error(failure?.message || "No se pudo consultar el listado. Intente nuevamente.");
            }
            const parsed = new DOMParser().parseFromString(await response.text(), "text/html");
            const result = getRoot(parsed) || parsed.querySelector(".app-content");
            if (!result) throw new Error("El servidor no devolvió el listado esperado.");
            if (signal.aborted || requestVersion !== version) return;
            const incoming = document.importNode(result, true);
            incoming.querySelectorAll("script").forEach(script => script.remove());
            const updates = Array.from(configurations.keys()).map(id => {
                const node = parsed.getElementById(id);
                return { id, value: node?.textContent ? JSON.parse(node.textContent) : null };
            });
            // Retain modal instances and form controls with their existing validation/listeners.
            const modals = new Map(Array.from(root.querySelectorAll(".modal[id]")).map(modal => [modal.id, modal]));
            incoming.querySelectorAll(".modal[id]").forEach(modal => {
                const existing = modals.get(modal.id);
                if (existing) modal.replaceWith(existing);
            });
            retainForms(root, incoming);
            copyAttributes(root, incoming);
            root.replaceChildren(...Array.from(incoming.childNodes));
            const currentActions = document.querySelector(".app-content-header [data-erp-list-actions]");
            const nextActions = parsed.querySelector(".app-content-header [data-erp-list-actions]");
            if (currentActions && nextActions) {
                currentActions.replaceChildren(...Array.from(nextActions.childNodes).map(node => document.importNode(node, true)));
            }
            const currentSummary = document.querySelector(".app-content-header [data-erp-list-summary]");
            const nextSummary = parsed.querySelector(".app-content-header [data-erp-list-summary]");
            if (currentSummary && nextSummary) currentSummary.textContent = nextSummary.textContent;
            updates.forEach(update => {
                const current = configurations.get(update.id);
                if (current && update.value) Object.assign(current, update.value);
            });
            if (pushHistory && url.toString() !== window.location.href)
                history.pushState({ ...history.state, erpListing: true }, "", url);
            if (focused?.isConnected) {
                focused.focus({ preventScroll: true });
                if (focused instanceof HTMLInputElement && selection)
                    focused.setSelectionRange(selection[0], selection[1]);
            }
            document.dispatchEvent(new CustomEvent("erp:list-updated", { detail: { root, url: url.toString() } }));
        } catch (error) {
            if (!signal.aborted && requestVersion === version) reportError(error);
        } finally {
            if (requestVersion === version) {
                root.removeAttribute("aria-busy");
                progress.remove();
            }
        }
    }
    /** @param {string} selector @param {(button: HTMLButtonElement, event: MouseEvent) => void | Promise<void>} handler */
    function onClick(selector, handler) {
        document.addEventListener("click", event => {
            const control = event.target instanceof Element ? event.target.closest(selector) : null;
            if (!(control instanceof HTMLButtonElement) || control.disabled) return;
            void Promise.resolve().then(() => handler(control, event)).catch(reportError);
        });
    }
    window.erpListings = { load, onClick, config };
    document.addEventListener("submit", event => {
        const form = event.target;
        const root = getRoot(document);
        if (event.defaultPrevented || !(form instanceof HTMLFormElement) || form.method.toLowerCase() !== "get" ||
            !root?.contains(form) || form.closest(".modal") || form.hasAttribute("data-full-navigation")) return;
        const url = new URL(form.action, window.location.href);
        if (!isModuleUrl(url)) return;
        event.preventDefault();
        url.search = "";
        const submitter = event.submitter;
        const data = submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement
            ? new FormData(form, submitter) : new FormData(form);
        data.forEach((value, key) => { if (typeof value === "string") url.searchParams.append(key, value); });
        url.searchParams.set("page", "1");
        void load(url.toString());
    });
    document.addEventListener("click", event => {
        if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
        const root = getRoot(document);
        if (!(link instanceof HTMLAnchorElement) || !root?.contains(link) || link.closest(".modal") ||
            link.hasAttribute("download") || link.hasAttribute("data-bs-toggle") || link.hasAttribute("data-full-navigation") ||
            (link.target && link.target !== "_self")) return;
        const href = link.getAttribute("href") || "";
        if (!href || href.startsWith("#") || href.toLowerCase().startsWith("javascript:")) return;
        const url = new URL(link.href, window.location.href);
        if (!isModuleUrl(url)) return;
        event.preventDefault();
        void load(url.toString());
    });
    window.addEventListener("popstate", () => {
        if (isModuleUrl(new URL(window.location.href))) void load(window.location.href, false);
    });
})();
