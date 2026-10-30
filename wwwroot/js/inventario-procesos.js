/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @param {string} id */
    function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} label @param {string|number|null} value */
    function info(label, value) {
        const block = document.createElement("div"); block.className = "col-md-4";
        const title = document.createElement("small"); title.className = "text-muted d-block"; title.textContent = label;
        const content = document.createElement("div"); content.className = "fw-semibold"; content.textContent = value === null ? "—" : String(value);
        block.append(title, content); return block;
    }
    /** @param {unknown} error */
    function message(error) { return error instanceof Error ? error.message : "No se pudo completar la operación."; }
    const quantity = new Intl.NumberFormat("es-PE", { minimumFractionDigits: 4, maximumFractionDigits: 4 });
    if (document.getElementById("inventory-arrival-config")) {
        /** @typedef {{id:number,proveedor:string,codigo:string,producto:string,unidad:string,fecha:string|null,cantidad:number,orden:string,ordenada:number,ingresada:number,pendiente:number}} Arrival */
        const config = /** @type {{sinProveedor:boolean,rows:Arrival[]}} */ (/** @type {unknown} */ (window.erpListings.config("inventory-arrival-config")));
        const modal = new bootstrap.Modal(node("inventory-arrival-modal"));
        window.erpListings.onClick("[data-inv-arrival]", button => {
            const row = config.rows.find(item => item.id === Number(button.dataset.invArrival)); if (!row) return;
            const content = document.createElement("div"); content.className = "row g-3";
            content.append(info("Orden de compra", row.orden), info("Producto", row.codigo + " · " + row.producto), info("U.M.", row.unidad), info("Fecha programada", row.fecha), info("Cantidad programada", quantity.format(row.cantidad)));
            if (!config.sinProveedor) content.append(info("Proveedor", row.proveedor));
            content.append(info("Cantidad ordenada del producto", quantity.format(row.ordenada)), info("Cantidad ingresada del producto", quantity.format(row.ingresada)), info("Pendiente del producto en la orden", quantity.format(row.pendiente)));
            const note = document.createElement("p"); note.className = "small text-muted mt-4 mb-0"; note.textContent = "El pendiente corresponde al producto de la orden de compra; la cantidad programada corresponde a esta entrega.";
            node("inventory-arrival-body").replaceChildren(content, note); modal.show();
        });
        return;
    }
    if (!document.getElementById("inventory-process-config")) return;
    /** @typedef {{id:number,mes:string,codigo:string,estado:string,fecha:string|null,posterior:string}} Month */
    /** @typedef {{contextoId:number,operacion:string,almacenId:number,almacen:string,sede:string,anio:number,esVenta:boolean,mesContableId:number|null,revision:string,hastaMes:number,meses:Array<{mesNombre:string,estadoNombre:string,movimientoPostCierre:string}>}} Plan */
    const config = /** @type {{almacenId:number|null,almacen:string|null,anio:number,sede:string,rows:Month[],urls:Record<string,string>}} */ (/** @type {unknown} */ (window.erpListings.config("inventory-process-config")));
    const modalNode = node("inventory-process-modal"), form = node("inventory-process-form"), body = node("inventory-process-body"), submitControl = node("inventory-process-submit");
    if (!(form instanceof HTMLFormElement) || !(submitControl instanceof HTMLButtonElement)) throw new Error("El formulario del proceso no está disponible.");
    const submit = submitControl;
    const modal = new bootstrap.Modal(modalNode);
    /** @type {Plan|null} */ let plan = null;
    let version = 0, busy = false;
    /** @type {Record<string,string>} */ const labels = { apertura: "Aperturar mes", cierre: "Cerrar mes", reproceso: "Reprocesar mes", anual: "Reproceso anual" };
    /** @param {string} text */
    function status(text) { const p = document.createElement("p"); p.className = "text-muted py-3"; p.setAttribute("role", "status"); p.textContent = text; body.replaceChildren(p); }
    /** @param {string} title */
    function open(title) {
        ++version; plan = null; submit.classList.add("d-none"); submit.disabled = false;
        node("inventory-process-title").textContent = title; status("Preparando operación…"); modal.show();
    }
    /** @param {Response} response */
    async function result(response) {
        const value = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !value?.success || response.redirected) throw new Error(value?.message || "No se pudo completar la operación. Revise su sesión y permisos.");
        return value;
    }
    window.erpListings.onClick("[data-inv-process-detail]", button => {
        if (busy) return;
        const row = config.rows.find(item => item.id === Number(button.dataset.invProcessDetail)); if (!row) return;
        open("Detalle del mes contable");
        const content = document.createElement("div"); content.className = "row g-3";
        content.append(info("Ejercicio", config.anio), info("Sede", config.sede), info("Almacén", config.almacen), info("Mes contable", row.codigo + " · " + row.mes), info("Estado", row.estado), info("Fecha de cierre", row.fecha), info("Movimientos posteriores al cierre", row.posterior === "S" ? "Sí · revisar saldos" : "No"));
        body.replaceChildren(content);
    });
    window.erpListings.onClick("[data-inv-process]", async button => {
        if (busy || !config.almacenId) return;
        const operation = button.dataset.invProcess || ""; if (!labels[operation]) return;
        open(labels[operation]); const current = version;
        try {
            const url = new URL(config.urls.preparar, window.location.origin);
            url.searchParams.set("operacion", operation); url.searchParams.set("almacenId", String(config.almacenId));
            if (button.dataset.month) url.searchParams.set("mesContableId", button.dataset.month);
            const response = await fetch(url, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } });
            const value = await result(response); if (current !== version) return;
            const prepared = /** @type {Plan} */ (value.data); plan = prepared;
            const content = document.createElement("div"); content.className = "row g-3 mb-4";
            content.append(info("Operación", labels[prepared.operacion]), info("Ejercicio", prepared.anio), info("Sede", prepared.sede), info("Almacén", prepared.almacen), info("Meses incluidos", prepared.meses.length));
            const note = document.createElement("div"); note.className = "alert alert-info";
            note.textContent = operation === "apertura" ? "Abre el mes y limpia la fecha de cierre y el indicador de movimientos posteriores."
                : operation === "cierre" ? "Recalcula los saldos del mes y lo registra como cerrado con la fecha de hoy."
                    : operation === "anual" ? "Recalcula los saldos de enero al mes actual, en orden. Conserva los estados de apertura y cierre."
                        : "Recalcula los saldos del mes." + (prepared.esVenta ? " También recalcula las reservas de la sede, por tratarse de un almacén de ventas." : "");
            const list = document.createElement("ul"); list.className = "list-group";
            prepared.meses.forEach(month => { const li = document.createElement("li"); li.className = "list-group-item bg-body d-flex flex-wrap justify-content-between gap-2"; const name = document.createElement("strong"); name.textContent = month.mesNombre; const state = document.createElement("span"); state.className = "text-muted"; state.textContent = month.estadoNombre + (month.movimientoPostCierre === "S" ? " · con movimientos posteriores al cierre" : ""); li.append(name, state); list.append(li); });
            body.replaceChildren(content, note, list); submit.classList.remove("d-none");
        } catch (error) { if (current !== version) return; status(message(error)); await Swal.fire({ icon: "error", title: "No se pudo preparar", text: message(error) }); }
    });
    form.addEventListener("submit", async event => {
        event.preventDefault(); if (!plan || busy) return;
        const prepared = plan, current = version;
        busy = true; submit.disabled = true;
        try {
            const confirmation = await Swal.fire({ icon: "question", title: labels[prepared.operacion], text: `${prepared.almacen} · Ejercicio ${prepared.anio} · ${prepared.meses.map(m => m.mesNombre).join(", ")}`, showCancelButton: true, confirmButtonText: "Confirmar operación", cancelButtonText: "Cancelar" });
            if (!confirmation.isConfirmed || current !== version) return;
            const data = new FormData(form);
            data.set("ContextoId", String(prepared.contextoId)); data.set("AlmacenId", String(prepared.almacenId)); data.set("Revision", prepared.revision);
            if (prepared.mesContableId) data.set("MesContableId", String(prepared.mesContableId));
            if (prepared.operacion === "anual") data.set("HastaMes", String(prepared.hastaMes));
            const url = config.urls[prepared.operacion]; if (!url) throw new Error("La operación no está habilitada.");
            const response = await fetch(url, { method: "POST", credentials: "same-origin", body: data, headers: { "X-Requested-With": "XMLHttpRequest" } });
            const value = await result(response);
            plan = null; busy = false; modal.hide();
            await window.erpListings.load(window.location.href, false);
            await Swal.fire({ icon: "success", title: "Operación completada", text: value.message });
        } catch (error) {
            plan = null; submit.classList.add("d-none");
            status(message(error) + " Vuelva a preparar la operación para revisar su estado actual.");
            await Swal.fire({ icon: "error", title: "No se pudo completar", text: message(error) });
        } finally { busy = false; submit.disabled = false; }
    });
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); });
    modalNode.addEventListener("hidden.bs.modal", () => { ++version; plan = null; });
})();
