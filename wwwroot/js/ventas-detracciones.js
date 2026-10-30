/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @type {{canWrite:boolean,contextoId:number,urls:{obtener:string,guardar:string}}} */
    const config = window.erpListings.config("detraccion-config");
    const modalNode = document.getElementById("dt-modal"), formNode = document.getElementById("dt-form"), detailNode = document.getElementById("dt-details"), fieldsNode = document.getElementById("dt-fields"), save = document.getElementById("dt-save");
    if (!modalNode || !(formNode instanceof HTMLFormElement) || !detailNode || !(fieldsNode instanceof HTMLFieldSetElement)) return;
    const form = formNode, detail = detailNode, fields = fieldsNode, modal = new bootstrap.Modal(modalNode);
    let busy = false, readOnly = true, loadVersion = 0;
    /** @param {string} name */
    function input(name) { const field = form.elements.namedItem(name); if (!(field instanceof HTMLInputElement)) throw new Error("Falta el campo " + name); return field; }
    /** @param {unknown} error */
    function failure(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} url @param {RequestInit} [options] */
    async function request(url, options) {
        const response = await fetch(url, { credentials: "same-origin", ...options, headers: { "X-Requested-With": "XMLHttpRequest", ...options?.headers } });
        if (response.redirected) throw new Error("La sesión no está disponible. Vuelva a seleccionar su perfil de acceso.");
        const json = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "No se pudo consultar o guardar el pago.");
        return json;
    }
    /** @param {HTMLElement} button @param {boolean} edit */
    async function open(button, edit) {
        if (busy) return;
        const version = ++loadVersion;
        const url = new URL(config.urls.obtener, window.location.origin); url.searchParams.set("id", button.dataset.id || ""); url.searchParams.set("consultaId", button.dataset.consulta || "");
        const result = await request(url.toString()); if (version !== loadVersion) return;
        /** @type {Record<string,string|number|null>} */ const row = result.data;
        form.reset(); detail.replaceChildren(); readOnly = !edit || !config.canWrite;
        ["Id", "ConsultaId", "Revision", "NumeroOperacionPago", "FechaPago", "MontoPagoTexto", "MontoCargoTexto"].forEach(name => { const key = name[0].toLowerCase() + name.slice(1); input(name).value = String(row[key] ?? ""); });
        input("ContextoId").value = String(config.contextoId);
        const columns = [["Documento", "documento"], ["Fecha", "fecha"], ["Cliente", "cliente"], ["Documento cliente", "documentoCliente"], ["Moneda", "moneda"], ["Total", "total"], ["Tipo de detracción", "tipoDetraccion"], ["% detracción", "tasa"], ["Monto de detracción", "montoDetraccion"]];
        columns.forEach(([label, key]) => {
            const title = document.createElement("dt"), value = document.createElement("dd"); title.className = "col-sm-4 text-muted"; value.className = "col-sm-8 text-break"; title.textContent = label;
            const raw = String(row[key] ?? ""); value.textContent = key === "fecha" ? raw.split("-").reverse().join("/") : raw; detail.append(title, value);
        });
        fields.disabled = readOnly; if (save instanceof HTMLButtonElement) { save.hidden = readOnly; save.disabled = false; }
        modal.show();
    }
    window.erpListings.onClick(".js-dt-view", button => { void open(button, false).catch(failure); });
    window.erpListings.onClick(".js-dt-edit", button => { void open(button, true).catch(failure); });
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else loadVersion++; });
    form.addEventListener("submit", event => {
        event.preventDefault(); if (busy || readOnly || !config.canWrite || !form.reportValidity()) return;
        void (async () => {
            busy = true; if (save instanceof HTMLButtonElement) save.disabled = true;
            try {
                const result = await request(config.urls.guardar, { method: "POST", body: new FormData(form) });
                busy = false; modal.hide(); await window.erpListings.load(window.location.href, false);
                void Swal.fire({ icon: "success", title: result.message, toast: true, position: "top-end", timer: 2000, showConfirmButton: false });
            } catch (error) { failure(error); }
            finally { busy = false; if (save instanceof HTMLButtonElement) save.disabled = false; }
        })();
    });
})();
