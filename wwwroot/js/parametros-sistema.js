/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @type {{canWrite:boolean,contextoId:number,obtener:string,guardar:string}} */ const config = window.erpListings.config("parametros-config");
    const node = document.getElementById("ps-modal"), formNode = document.getElementById("ps-form"), save = document.getElementById("ps-save");
    if (!node || !(formNode instanceof HTMLFormElement)) return;
    const form = formNode;
    const modal = new bootstrap.Modal(node); let busy = false, editing = false, version = 0;
    /** @param {string} id */ function input(id) { const el = document.getElementById(id); if (!(el instanceof HTMLInputElement)) throw new Error("Falta un control del parámetro."); return el; }
    /** @param {string} name */ function hidden(name) { const el = form.elements.namedItem(name); if (!(el instanceof HTMLInputElement)) throw new Error("Falta el contexto del parámetro."); return el; }
    /** @param {string} id @param {string} text */ function label(id, text) { const el = document.getElementById(id); if (el) el.textContent = text; }
    /** @param {unknown} error */ function failure(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} url @param {RequestInit} [options] */ async function request(url, options) {
        const response = await fetch(url, { credentials: "same-origin", ...options, headers: { "X-Requested-With": "XMLHttpRequest", ...options?.headers } });
        if (response.redirected || !response.headers.get("content-type")?.includes("application/json")) throw new Error("Vuelva a iniciar sesión y seleccione el contexto de trabajo.");
        const result = await response.json(); if (!response.ok || !result.success) throw new Error(result.message || "No se pudo completar la operación."); return result;
    }
    window.erpListings.onClick(".js-ps-open", async button => {
        if (busy) return; const current = ++version;
        try {
            const result = await request(config.obtener + "?id=" + encodeURIComponent(button.dataset.id || "")); if (current !== version) return;
            const item = result.data, editable = config.canWrite && item.editable && button.dataset.edit === "true"; editing = editable;
            form.reset(); hidden("Id").value = String(item.id); hidden("Revision").value = item.revision; hidden("ContextoId").value = String(config.contextoId);
            label("ps-group", item.grupo); label("ps-code", item.codigo); label("ps-name", item.nombre); label("ps-type", item.tipoNombre); label("ps-current", item.valor);
            const readonly = document.getElementById("ps-readonly"); if (readonly) readonly.hidden = editable;
            node.querySelectorAll("[data-ps-kind]").forEach(block => {
                if (!(block instanceof HTMLElement)) return;
                const active = editable && (block.dataset.psKind || "").split(" ").includes(item.tipo); block.hidden = !active;
                block.querySelectorAll("input").forEach(control => { control.disabled = !active; control.required = active && control.type !== "checkbox" && item.tipo !== "01"; });
            });
            input("ps-text").value = item.cadena || ""; input("ps-secret").value = ""; input("ps-secret").disabled = true;
            input("ps-number").value = item.numeroTexto; input("ps-number").step = item.tipo === "02" ? "1" : "0.0001";
            input("ps-number").min = item.tipo === "02" ? "-2147483648" : "-999999999999999.9999"; input("ps-number").max = item.tipo === "02" ? "2147483647" : "999999999999999.9999";
            input("ps-from").value = item.horaDesde; input("ps-until").value = item.horaHasta;
            if (save instanceof HTMLButtonElement) { save.hidden = !editable; save.disabled = item.tipo === "07"; }
            modal.show();
        } catch (error) { if (current === version) failure(error); }
    });
    input("ps-replace").addEventListener("change", () => { const active = input("ps-replace").checked; input("ps-secret").disabled = !active; input("ps-secret").required = active; if (!active) input("ps-secret").value = ""; if (save instanceof HTMLButtonElement) save.disabled = !active; });
    node.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { version++; editing = false; input("ps-secret").value = ""; } });
    form.addEventListener("submit", async event => {
        event.preventDefault(); if (busy || !editing || !config.canWrite || !form.reportValidity()) return; const current = version;
        const answer = await Swal.fire({ icon: "question", title: "Guardar parámetro", text: "El cambio se aplica a la configuración global del sistema.", showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar" });
        if (!answer.isConfirmed || busy || current !== version || !editing) return;
        const data = new FormData(form); busy = true; if (save instanceof HTMLButtonElement) save.disabled = true;
        try {
            const result = await request(config.guardar, { method: "POST", body: data }); busy = false; modal.hide();
            await window.erpListings.load(window.location.href, false); void Swal.fire({ icon: "success", title: result.message, timer: 1400, showConfirmButton: false });
        } catch (error) { failure(error); } finally { busy = false; if (save instanceof HTMLButtonElement) save.disabled = false; }
    });
})();
