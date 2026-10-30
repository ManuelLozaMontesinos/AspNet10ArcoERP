/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{kind:string, canWrite:boolean, urls:Record<string,string>}} Configuration */
    /** @typedef {{id:number, nombre:string, codigo?:string}} Lookup */
    /** @typedef {{id:number, transaccionId:number, codigo:string, nombre:string, tipoMovimientoNombre:string}} Relation */
    /** @typedef {{id:number, atributoId:number, nombre:string, visible:boolean, requerido:boolean}} Setting */
    /** @typedef {{id:number, documentoNombre:string, serie:string, correlativo:number}} Series */
    /** @type {Configuration} */
    const config = /** @type {Configuration} */ (/** @type {unknown} */ (window.erpListings.config("inventario-config")));
    /** @param {string} id */
    function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} id */
    function input(id) { const value = node(id); if (!(value instanceof HTMLInputElement)) throw new Error("Control no válido: " + id); return value; }
    /** @param {string} id */
    function select(id) { const value = node(id); if (!(value instanceof HTMLSelectElement)) throw new Error("Control no válido: " + id); return value; }
    const formNode = node("inv-form");
    if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode;
    const modal = new bootstrap.Modal(node("inv-modal"));
    const warehouse = config.kind === "almacen";
    let writable = false, saving = false, revision = 0, configRevision = 0, provinceRevision = 0, districtRevision = 0;
    /** @type {Relation[]} */ let relations = [];
    /** @param {unknown} error */
    function errorMessage(error) { return error instanceof Error ? error.message : "No se pudo completar la operación."; }
    /** @param {unknown} error */
    function showError(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: errorMessage(error) }); }
    /** @param {() => Promise<void>} action */
    function run(action) { void action().catch(showError); }
    /** @param {string} action @param {Record<string,string|number>} values */
    async function get(action, values) {
        const url = new URL(config.urls[action], window.location.origin);
        Object.entries(values).forEach(([key, value]) => url.searchParams.set(key, String(value)));
        return request(url.toString());
    }
    /** @param {string} url @param {RequestInit} [options] */
    async function request(url, options) {
        const response = await fetch(url, { credentials: "same-origin", ...options, headers: { "X-Requested-With": "XMLHttpRequest", ...options?.headers } });
        if (response.redirected) throw new Error("La sesión no está disponible. Inicie sesión nuevamente.");
        const json = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "El servidor no pudo completar la operación.");
        return json;
    }
    /** @param {string} action @param {FormData | Record<string,string|number|boolean>} values */
    async function post(action, values) {
        const data = values instanceof FormData ? values : new FormData();
        if (!(values instanceof FormData)) Object.entries(values).forEach(([key, value]) => data.set(key, String(value)));
        const token = form.querySelector('input[name="__RequestVerificationToken"]');
        if (!(token instanceof HTMLInputElement)) throw new Error("Falta el token de seguridad. Recargue la página.");
        data.set("__RequestVerificationToken", token.value);
        return request(config.urls[action], { method: "POST", body: data });
    }
    /** @param {string} id @param {Lookup[]} items @param {string} [prompt] */
    function options(id, items, prompt = "Seleccione") {
        const control = select(id);
        control.replaceChildren(new Option(prompt, ""), ...items.map(item => new Option(item.nombre, String(item.id))));
    }
    /** @param {string} id @param {unknown} value @param {string} [label] */
    function setSelect(id, value, label) {
        const control = select(id), text = value == null ? "" : String(value);
        if (text && !Array.from(control.options).some(option => option.value === text)) {
            const option = new Option(label || text, text); option.dataset.invRetained = ""; control.add(option);
        }
        control.value = text;
    }
    function identity() { return Number(input("inv-id").value) || 0; }
    function mode() {
        form.querySelectorAll("input:not([type=hidden]),select,textarea").forEach(control => {
            if (control instanceof HTMLInputElement || control instanceof HTMLSelectElement || control instanceof HTMLTextAreaElement) control.disabled = !writable || saving;
        });
        form.querySelectorAll("[data-inv-write]").forEach(control => { if (control instanceof HTMLButtonElement) control.disabled = !writable || saving; });
        const save = document.getElementById("inv-save");
        if (save instanceof HTMLButtonElement) { save.hidden = !writable; save.disabled = saving; }
        if (warehouse) {
            select("inv-sede").disabled = !writable || saving || identity() > 0;
            select("inv-config-transaction").disabled = saving;
            node("inv-new-notice").hidden = identity() > 0;
            node("inv-details").hidden = identity() === 0;
        } else select("inv-movement").disabled = !writable || saving || identity() > 0;
    }
    /** @param {string} bodyId @param {number} columns @param {string} message */
    function empty(bodyId, columns, message) {
        const body = node(bodyId), row = document.createElement("tr"), cell = document.createElement("td");
        cell.colSpan = columns; cell.className = "text-center text-muted py-3"; cell.textContent = message;
        row.append(cell); body.replaceChildren(row);
    }
    /** @param {unknown[]} values */
    function row(values) {
        const tr = document.createElement("tr");
        values.forEach(value => { const cell = document.createElement("td"); cell.textContent = String(value ?? ""); tr.append(cell); });
        return tr;
    }
    /** @param {string} title @param {string} icon @param {string} action @param {number} id */
    function button(title, icon, action, id) {
        const value = document.createElement("button"); value.type = "button";
        value.className = "btn btn-sm " + (action.includes("cancel") ? "btn-outline-danger" : "btn-outline-primary") + " me-1";
        value.title = title; value.setAttribute("aria-label", title); value.dataset.invAction = action; value.dataset.id = String(id);
        value.disabled = !writable || saving;
        const glyph = document.createElement("i"); glyph.className = "bi " + icon; glyph.setAttribute("aria-hidden", "true"); value.append(glyph);
        return value;
    }
    /** @param {HTMLTableRowElement} tr @param {HTMLButtonElement[]} buttons */
    function actions(tr, buttons) { const cell = document.createElement("td"); cell.className = "text-center text-nowrap"; cell.append(...buttons); tr.append(cell); }
    async function loadSettings() {
        const stamp = ++configRevision, current = revision;
        const id = Number(select("inv-config-transaction").value);
        if (!id) { empty("inv-config-body", 4, "Seleccione una transacción vinculada."); return; }
        empty("inv-config-body", 4, "Cargando configuración…");
        const result = await get("configuracion", { almacenId: identity(), id });
        if (stamp !== configRevision || current !== revision) return;
        /** @type {Setting[]} */ const settings = result.data;
        node("inv-config-body").replaceChildren(...settings.map(setting => {
            const tr = row([setting.nombre]); tr.dataset.attribute = String(setting.atributoId);
            for (const key of /** @type {const} */ (["visible", "requerido"])) {
                const cell = document.createElement("td"), box = document.createElement("input");
                cell.className = "text-center"; box.type = "checkbox"; box.className = "form-check-input"; box.dataset.field = key;
                box.checked = setting[key]; box.disabled = !writable || saving; box.setAttribute("aria-label", (key === "visible" ? "Visible: " : "Requerido: ") + setting.nombre);
                cell.append(box); tr.append(cell);
            }
            actions(tr, config.canWrite ? [button("Guardar configuración", "bi-save", "setting-save", setting.atributoId)] : []); return tr;
        }));
        if (!settings.length) empty("inv-config-body", 4, "No hay campos de configuración habilitados.");
    }
    async function loadDetails() {
        const current = revision, previous = select("inv-config-transaction").value;
        empty("inv-transactions-body", 4, "Cargando transacciones…"); empty("inv-series-body", 4, "Cargando series…");
        const result = await get("detalle", { id: identity() });
        if (current !== revision) return;
        /** @type {{transacciones:Relation[], transaccionesDisponibles:Lookup[], series:Series[], seriesDisponibles:Lookup[]}} */ const detail = result.data;
        relations = detail.transacciones;
        options("inv-transaction", detail.transaccionesDisponibles);
        input("inv-relation-id").value = "0";
        options("inv-config-transaction", relations);
        select("inv-config-transaction").value = relations.some(item => String(item.id) === previous) ? previous : String(relations[0]?.id ?? "");
        node("inv-transactions-body").replaceChildren(...relations.map(item => {
            const tr = row([item.codigo, item.nombre, item.tipoMovimientoNombre]);
            actions(tr, config.canWrite ? [button("Editar vínculo", "bi-pencil", "relation-edit", item.id), button("Retirar transacción", "bi-trash", "relation-cancel", item.id)] : []); return tr;
        }));
        if (!relations.length) empty("inv-transactions-body", 4, "No hay transacciones vinculadas.");
        node("inv-transactions-count").textContent = relations.length + " transacciones vinculadas";
        options("inv-series", detail.seriesDisponibles);
        node("inv-series-body").replaceChildren(...detail.series.map(item => {
            const tr = row([item.documentoNombre, item.serie, item.correlativo]);
            actions(tr, config.canWrite ? [button("Retirar serie", "bi-trash", "series-cancel", item.id)] : []); return tr;
        }));
        if (!detail.series.length) empty("inv-series-body", 4, "No hay series vinculadas.");
        node("inv-series-count").textContent = detail.series.length + " series vinculadas";
        await loadSettings();
        mode();
    }
    /** @param {number} department */
    async function provinces(department) {
        const stamp = ++provinceRevision, current = revision;
        ++districtRevision;
        options("inv-province", []); options("inv-district", []);
        if (!department) return;
        const result = await get("provincias", { departamentoId: department });
        if (stamp === provinceRevision && current === revision) options("inv-province", result.data);
    }
    /** @param {number} province */
    async function districts(province) {
        const stamp = ++districtRevision, current = revision;
        options("inv-district", []);
        if (!province) return;
        const result = await get("distritos", { provinciaId: province });
        if (stamp === districtRevision && current === revision) options("inv-district", result.data);
    }
    /** @param {number} id */
    async function open(id) {
        const current = ++revision;
        ++configRevision;
        form.reset(); input("inv-id").value = "0"; writable = config.canWrite;
        form.querySelectorAll("option[data-inv-retained]").forEach(option => option.remove());
        if (id) {
            const result = await get("obtener", { id });
            if (current !== revision) return;
            /** @type {Record<string, any>} */ const item = result.data;
            input("inv-id").value = String(item.id); writable = config.canWrite && item.estado === "A";
            input("inv-name").value = item.nombre;
            if (warehouse) {
                const fields = { "inv-short":"abreviatura", "inv-via-name":"nombreVia", "inv-number":"numeroVia", "inv-interior":"interior", "inv-zone-name":"nombreZona", "inv-sunat":"codigoSunat", "inv-printer":"servidorImpresion" };
                Object.entries(fields).forEach(([control, key]) => { input(control).value = item[key] ?? ""; });
                ["DepartamentoInterior","Manzana","Lote","Kilometro","Block","Etapa"].forEach(key => { input("inv-" + key).value = item[key[0].toLowerCase() + key.slice(1)] ?? ""; });
                input("inv-sale").checked = item.esVenta;
                setSelect("inv-sede", item.empresaSedeId, item.sedeNombre); setSelect("inv-via", item.tipoViaId); setSelect("inv-zone", item.tipoZonaId);
                setSelect("inv-department", item.departamentoId);
                await provinces(item.departamentoId);
                if (current !== revision) return;
                setSelect("inv-province", item.provinciaId);
                await districts(item.provinciaId);
                if (current !== revision) return;
                setSelect("inv-district", item.distritoId);
            } else {
                input("inv-code").value = item.codigo; input("inv-purchase").checked = item.esCompra;
                setSelect("inv-movement", item.tipoMovimientoId, item.tipoMovimientoNombre); setSelect("inv-operation", item.tipoOperacionId, item.tipoOperacionNombre);
            }
        } else if (warehouse) { options("inv-province", []); options("inv-district", []); }
        node("inv-modal-title").textContent = (id ? writable ? "Editar " : "Consultar " : "Nuevo ") + (warehouse ? "almacén" : "tipo de transacción");
        mode(); modal.show();
        if (warehouse && id) await loadDetails();
    }
    /** @param {() => Promise<any>} action @param {boolean} [refreshDetails] */
    async function mutate(action, refreshDetails = true) {
        if (!writable || saving) return;
        saving = true; mode();
        form.querySelectorAll("[data-inv-action]").forEach(control => { if (control instanceof HTMLButtonElement) control.disabled = true; });
        try {
            const result = await action();
            if (refreshDetails) await loadDetails();
            void Swal.fire({ icon: "success", title: result.message, timer: 1300, showConfirmButton: false });
        } finally {
            saving = false; mode();
            form.querySelectorAll("[data-inv-action]").forEach(control => { if (control instanceof HTMLButtonElement) control.disabled = !writable; });
        }
    }
    /** @param {string} text */
    async function confirm(text) { return (await Swal.fire({ icon: "question", title: text, text: "Se realizará una baja lógica del registro.", showCancelButton: true, confirmButtonText: "Sí, anular", cancelButtonText: "Cancelar" })).isConfirmed; }

    document.getElementById("inv-new")?.addEventListener("click", () => run(() => open(0)));
    window.erpListings.onClick(".js-inv-edit", async button => { await open(Number(button.dataset.id)); });
    window.erpListings.onClick(".js-inv-cancel", async button => {
        if (saving || !config.canWrite || !await confirm(warehouse ? "¿Anular este almacén?" : "¿Anular esta transacción?")) return;
        button.disabled = true;
        try { const result = await post("anular", { id: Number(button.dataset.id) }); await window.erpListings.load(window.location.href, false); void Swal.fire({ icon: "success", title: result.message, timer: 1300, showConfirmButton: false }); }
        finally { if (button.isConnected) button.disabled = false; }
    });
    form.addEventListener("submit", event => {
        event.preventDefault();
        run(async () => {
            if (!writable || saving || !form.reportValidity()) return;
            const data = new FormData(form);
            data.set(warehouse ? "EmpresaSedeId" : "TipoMovimientoId", select(warehouse ? "inv-sede" : "inv-movement").value);
            await mutate(async () => {
                const result = await post("guardar", data);
                input("inv-id").value = String(result.id);
                node("inv-modal-title").textContent = "Editar " + (warehouse ? "almacén" : "tipo de transacción");
                if (!warehouse) input("inv-code").value = (await get("obtener", { id: result.id })).data.codigo;
                await window.erpListings.load(window.location.href, false);
                return result;
            }, warehouse);
        });
    });
    if (warehouse) {
        select("inv-department").addEventListener("change", () => run(() => provinces(Number(select("inv-department").value))));
        select("inv-province").addEventListener("change", () => run(() => districts(Number(select("inv-province").value))));
        select("inv-config-transaction").addEventListener("change", () => run(loadSettings));
        document.getElementById("inv-link-reset")?.addEventListener("click", () => { input("inv-relation-id").value = "0"; select("inv-transaction").value = ""; });
        document.getElementById("inv-link")?.addEventListener("click", () => run(async () => {
            if (!select("inv-transaction").value) throw new Error("Seleccione una transacción para vincular.");
            await mutate(() => post("guardarTransaccion", { id: Number(input("inv-relation-id").value), almacenId: identity(), transaccionId: Number(select("inv-transaction").value) }));
        }));
        document.getElementById("inv-series-add")?.addEventListener("click", () => run(async () => {
            if (!select("inv-series").value) throw new Error("Seleccione una serie para vincular.");
            await mutate(() => post("agregarSerie", { almacenId: identity(), documentoSerieId: Number(select("inv-series").value) }));
        }));
        form.addEventListener("click", event => {
            const target = event.target instanceof Element ? event.target.closest("button[data-inv-action]") : null;
            if (!(target instanceof HTMLButtonElement) || target.disabled || !writable || saving) return;
            run(async () => {
                const action = target.dataset.invAction, id = Number(target.dataset.id);
                if (action === "relation-edit") {
                    const item = relations.find(value => value.id === id); if (!item) return;
                    input("inv-relation-id").value = String(id); setSelect("inv-transaction", item.transaccionId, item.nombre); select("inv-transaction").focus();
                } else if (action === "setting-save") {
                    const tr = target.closest("tr");
                    const visible = tr?.querySelector('input[data-field="visible"]'), required = tr?.querySelector('input[data-field="requerido"]');
                    if (!(visible instanceof HTMLInputElement) || !(required instanceof HTMLInputElement)) return;
                    await mutate(() => post("guardarConfiguracion", { almacenId: identity(), almacenTransaccionId: Number(select("inv-config-transaction").value), atributoId: id, visible: visible.checked, requerido: required.checked }));
                } else if (await confirm(action === "relation-cancel" ? "¿Retirar esta transacción del almacén?" : "¿Retirar esta serie del almacén?")) {
                    await mutate(() => post(action === "relation-cancel" ? "anularTransaccion" : "anularSerie", { almacenId: identity(), id }));
                }
            });
        });
        form.addEventListener("click", event => {
            const control = event.target instanceof Element ? event.target.closest("[data-inv-sort]") : null;
            if (!(control instanceof HTMLButtonElement)) return;
            const table = control.closest("table"), body = table?.tBodies[0]; if (!body) return;
            const column = Number(control.dataset.invSort), ascending = control.dataset.direction !== "asc";
            table?.querySelectorAll("th").forEach(header => header.removeAttribute("aria-sort"));
            table?.querySelectorAll("[data-inv-sort]").forEach(button => {
                if (button instanceof HTMLElement && button !== control) delete button.dataset.direction;
                const icon = button.querySelector("i"); if (icon) icon.className = "bi bi-arrow-down-up text-muted";
            });
            control.dataset.direction = ascending ? "asc" : "desc";
            control.closest("th")?.setAttribute("aria-sort", ascending ? "ascending" : "descending");
            const icon = control.querySelector("i"); if (icon) icon.className = "bi text-primary " + (ascending ? "bi-sort-alpha-down" : "bi-sort-alpha-down-alt");
            Array.from(body.rows).filter(tr => tr.cells.length > 1).sort((a, b) => (ascending ? 1 : -1) * (a.cells[column]?.textContent || "").localeCompare(b.cells[column]?.textContent || "", "es", { numeric: true, sensitivity: "base" })).forEach(tr => body.append(tr));
        });
    }
})();
