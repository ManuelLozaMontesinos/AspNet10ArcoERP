/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{id:number,nombre:string,electronico?:boolean,comprobantePago?:boolean}} Lookup */
    /** @typedef {{kind:string,canWrite:boolean,contextoId:number,tipos?:Lookup[],urls:Record<string,string>}} Configuration */
    /** @type {Configuration} */ const config = window.erpListings.config("empresa-maestro-config");
    /** @param {string} id */
    function node(id) { const el = document.getElementById(id); if (!el) throw new Error("Falta el control " + id); return el; }
    const formNode = node("me-form");
    if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode, modalNode = node("me-modal"), modal = new bootstrap.Modal(modalNode);
    /** @param {string} name */
    function field(name) {
        const el = form.elements.namedItem(name);
        if (!(el instanceof HTMLInputElement || el instanceof HTMLSelectElement)) throw new Error("Falta el campo " + name);
        return el;
    }
    /** @param {string} name */
    function input(name) { const el = field(name); if (!(el instanceof HTMLInputElement)) throw new Error("Campo no válido"); return el; }
    /** @param {string} name */
    function select(name) { const el = field(name); if (!(el instanceof HTMLSelectElement)) throw new Error("Campo no válido"); return el; }
    /** @param {string} id */
    function button(id) { const el = node(id); if (!(el instanceof HTMLButtonElement)) throw new Error("Botón no válido"); return el; }
    /** @param {unknown} error */
    function showError(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} message */
    function success(message) { void Swal.fire({ icon: "success", title: message, toast: true, position: "top-end", timer: 2000, showConfirmButton: false }); }
    /** @param {() => Promise<void>} action */
    function run(action) { void action().catch(showError); }
    /** @param {string} url @param {RequestInit} [options] */
    async function request(url, options) {
        const response = await fetch(url, { credentials: "same-origin", ...options, headers: { "X-Requested-With": "XMLHttpRequest", ...options?.headers } });
        if (response.redirected) throw new Error("La sesión no está disponible. Inicie sesión nuevamente.");
        const json = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "El servidor no pudo completar la operación.");
        return json;
    }
    /** @param {string} action @param {Record<string,string|number>} values */
    function get(action, values) {
        const url = new URL(config.urls[action], location.origin);
        Object.entries(values).forEach(([key, value]) => url.searchParams.set(key, String(value)));
        return request(url.toString());
    }
    /** @param {string} action @param {FormData|Record<string,string|number>} values */
    function post(action, values) {
        const data = values instanceof FormData ? values : new FormData();
        if (!(values instanceof FormData)) Object.entries(values).forEach(([key, value]) => data.set(key, String(value)));
        data.set("__RequestVerificationToken", field("__RequestVerificationToken").value);
        return request(config.urls[action], { method: "POST", body: data });
    }
    let writable = false, busy = false, generation = 0, warehouseGeneration = 0, peopleGeneration = 0;
    /** @type {any} */ let current = null;
    let candidates = false, peoplePage = 1, peoplePageCount = 1, peopleSize = 25, peopleSort = "nombre", peopleDir = "asc", peopleSearch = "", peopleLoading = false;
    function identity() { return Number(field("Datos.Id").value); }
    function applyMode() {
        const fields = node("me-fields");
        if (fields instanceof HTMLFieldSetElement) fields.disabled = !writable || busy;
        const save = document.getElementById("me-save");
        if (save instanceof HTMLButtonElement) { save.hidden = !writable; save.disabled = busy; }
        if (config.kind === "serie") {
            select("Datos.EmpresaSedeId").disabled = !writable || busy || identity() > 0;
            select("Datos.TipoDocumentoId").disabled = !writable || busy || identity() > 0;
            input("Datos.Serie").readOnly = identity() > 0;
            const type = config.tipos?.find(t => t.id === Number(field("Datos.TipoDocumentoId").value));
            const electronic = identity() > 0 ? current?.electronico : type?.electronico;
            input("Datos.Correlativo").readOnly = identity() > 0 || !!electronic;
            if (!identity() && electronic) input("Datos.Correlativo").value = "0";
            select("Datos.AlmacenId").required = !!(identity() ? current?.comprobantePago : type?.comprobantePago);
        }
        if (config.kind === "grupo") {
            node("me-people-panel").classList.toggle("d-none", identity() === 0);
            node("me-members-note").textContent = identity() ? (candidates ? "Seleccione un tercero para agregarlo al grupo." : "Integrantes activos y retirados del grupo.") : "Guarde los datos del grupo para administrar sus integrantes.";
            const add = document.getElementById("me-add-mode");
            if (add instanceof HTMLButtonElement) { add.hidden = !writable || !identity() || candidates; add.disabled = busy || peopleLoading; }
            button("me-links-mode").classList.toggle("d-none", !candidates);
            node("me-members").querySelectorAll("button,input,select").forEach(el => {
                if (el instanceof HTMLButtonElement || el instanceof HTMLInputElement || el instanceof HTMLSelectElement) el.disabled = busy || peopleLoading;
            });
            button("me-people-prev").disabled = busy || peopleLoading || peoplePage <= 1;
            button("me-people-next").disabled = busy || peopleLoading || peoplePage >= peoplePageCount;
        }
    }
    /** @param {HTMLSelectElement} el @param {unknown} value @param {string} label */
    function retain(el, value, label) {
        const key = value == null ? "" : String(value);
        if (key && !Array.from(el.options).some(o => o.value === key)) { const o = new Option(label || key, key); o.dataset.meRetained = ""; el.add(o); }
        el.value = key;
    }
    /** @param {number} version */
    async function warehouses(version) {
        const nonce = ++warehouseGeneration, el = select("Datos.AlmacenId"), site = Number(field("Datos.EmpresaSedeId").value);
        el.replaceChildren(new Option(site ? "Cargando..." : "Seleccione una sede", ""));
        if (!site) return;
        const json = await get("almacenes", { sedeId: site });
        if (version !== generation || nonce !== warehouseGeneration) return;
        el.replaceChildren(new Option("Sin especificar", ""), ...json.data.map((/** @type {Lookup} */ x) => new Option(x.nombre, String(x.id))));
        if (current && current.empresaSedeId === site) retain(el, current.almacenId, current.almacen);
    }
    /** @param {number} id */
    async function open(id) {
        if (busy) return;
        const version = ++generation;
        ++peopleGeneration; ++warehouseGeneration;
        form.reset(); form.querySelectorAll("[data-me-retained]").forEach(el => el.remove());
        current = null; writable = false; busy = true; peopleLoading = false;
        candidates = false; peoplePage = 1; peopleSize = 25; peopleSort = "nombre"; peopleDir = "asc"; peopleSearch = "";
        field("Datos.Id").value = "0"; field("Revision").value = ""; field("ContextoId").value = String(config.contextoId);
        node("me-title").textContent = id ? "Cargando..." : ({ serie: "Nueva serie", medio: "Nuevo medio de pago", grupo: "Nuevo grupo" }[config.kind] || "Nuevo registro");
        applyMode(); modal.show();
        try {
            if (id) {
                const json = await get("obtener", { id });
                if (version !== generation) return;
                current = json.data;
                form.querySelectorAll('[name^="Datos."]').forEach(el => {
                    if (!(el instanceof HTMLInputElement || el instanceof HTMLSelectElement)) return;
                    const property = el.name.slice(6), value = current[property.charAt(0).toLowerCase() + property.slice(1)];
                    if (el instanceof HTMLInputElement && el.type === "checkbox") el.checked = !!value;
                    else if (el instanceof HTMLSelectElement) retain(el, value, property === "EmpresaSedeId" ? current.sede : property === "TipoDocumentoId" ? current.tipoDocumento : property === "TipoRelacionadoId" ? current.tipoRelacionado : current.almacen);
                    else el.value = value == null ? "" : String(value);
                });
                field("Revision").value = current.revision;
                node("me-title").textContent = config.kind === "serie" ? "Serie " + current.serie : current.nombre;
            }
            writable = config.canWrite && (!current || current.estado === "A");
            if (config.kind === "serie") await warehouses(version);
            if (config.kind === "grupo" && id) await loadPeople(version);
        } finally { if (version === generation) { busy = false; applyMode(); } }
    }
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { ++generation; ++peopleGeneration; ++warehouseGeneration; } });
    window.erpListings.onClick(".js-me-new", () => run(() => open(0)));
    window.erpListings.onClick(".js-me-edit", el => run(() => open(Number(el.dataset.id))));
    window.erpListings.onClick(".js-me-cancel", el => run(async () => {
        if (busy || !config.canWrite) return;
        const id = Number(el.dataset.id), revision = el.dataset.revision || "", contextoId = config.contextoId;
        busy = true;
        try {
            const result = await Swal.fire({ icon: "warning", title: "¿Anular este registro?", text: "Se conservará para consulta.", showCancelButton: true, confirmButtonText: "Sí, anular", cancelButtonText: "Cancelar" });
            if (!result.isConfirmed) return;
            const json = await post("anular", { id, revision, contextoId });
            await window.erpListings.load(location.href, false);
            success(json.message);
        } finally { busy = false; }
    }));
    form.addEventListener("submit", event => { event.preventDefault(); run(async () => {
        if (busy || !writable || !form.reportValidity()) return;
        busy = true;
        const data = new FormData(form);
        form.querySelectorAll('[name^="Datos."]').forEach(el => {
            if (el instanceof HTMLInputElement || el instanceof HTMLSelectElement) data.set(el.name, el instanceof HTMLInputElement && el.type === "checkbox" ? String(el.checked) : el.value);
        });
        applyMode();
        try {
            const json = await post("guardar", data);
            if (config.kind === "grupo") {
                field("Datos.Id").value = String(json.id);
                field("Revision").value = "";
                const fresh = await get("obtener", { id: json.id });
                current = fresh.data; field("Datos.Id").value = String(current.id); field("Revision").value = current.revision;
                node("me-title").textContent = current.nombre;
                candidates = false; peoplePage = 1; await loadPeople(generation);
            }
            await window.erpListings.load(location.href, false);
            if (config.kind !== "grupo") { busy = false; modal.hide(); }
            success(json.message);
        } finally { busy = false; applyMode(); }
    }); });
    if (config.kind === "serie") {
        select("Datos.TipoDocumentoId").addEventListener("change", applyMode);
        select("Datos.EmpresaSedeId").addEventListener("change", () => run(async () => {
            busy = true; applyMode();
            try { await warehouses(generation); } finally { busy = false; applyMode(); }
        }));
    }
    /** @param {number} version */
    async function loadPeople(version) {
        if (!identity()) return;
        const nonce = ++peopleGeneration, group = identity(); peopleLoading = true; applyMode();
        try {
            const json = await get(candidates ? "candidatos" : "clientes", { id: group, search: peopleSearch, page: peoplePage, top: peopleSize, sort: peopleSort, dir: peopleDir });
            if (version !== generation || nonce !== peopleGeneration || group !== identity()) return;
            const data = json.data; peoplePage = data.page; peoplePageCount = data.pageCount; peopleSort = data.sort; peopleDir = data.dir;
            const body = node("me-people-rows"); body.replaceChildren();
            for (const item of data.items) {
                const row = document.createElement("tr");
                for (const text of [item.codigo, item.nombre, item.documento]) {
                    const cell = document.createElement("td"); cell.textContent = text; row.append(cell);
                }
                const stateCell = document.createElement("td"), badge = document.createElement("span");
                badge.className = "badge " + (item.estado === "A" ? "bg-success-subtle text-success border border-success-subtle" : "bg-secondary-subtle text-body-secondary border");
                badge.textContent = item.estado === "A" ? "Activo" : "Retirado"; stateCell.append(badge); row.append(stateCell);
                const cell = document.createElement("td"); cell.className = "listing-options";
                if (writable && (candidates || item.estado === "A")) {
                    const action = document.createElement("button"); action.type = "button"; action.className = "btn btn-sm " + (candidates ? "btn-outline-primary" : "btn-outline-danger");
                    action.textContent = candidates ? "Agregar" : "Retirar";
                    action.addEventListener("click", () => run(() => changeMember(item, group, version))); cell.append(action);
                }
                row.append(cell); body.append(row);
            }
            if (!data.items.length) { const row = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = 5; cell.className = "text-center text-muted py-4"; cell.textContent = "No se encontraron registros."; row.append(cell); body.append(row); }
            node("me-people-range").textContent = `Mostrando ${data.firstRecord} a ${data.lastRecord} de ${data.total} registros · Página ${data.page} de ${data.pageCount}`;
            node("me-members").querySelectorAll("th[data-me-column]").forEach(th => {
                const key = th.getAttribute("data-me-column"), active = key === peopleSort;
                th.setAttribute("aria-sort", active ? (peopleDir === "asc" ? "ascending" : "descending") : "none");
                const marker = th.querySelector("span"); if (marker) marker.textContent = active ? (peopleDir === "asc" ? "↑" : "↓") : "↕";
                const control = th.querySelector("button"); if (control instanceof HTMLButtonElement) control.hidden = candidates && key === "estado";
            });
        } catch (error) {
            if (version === generation && nonce === peopleGeneration) { node("me-people-rows").replaceChildren(); node("me-people-range").textContent = "No se pudo cargar el detalle. Vuelva a buscar."; throw error; }
        } finally { if (nonce === peopleGeneration) { peopleLoading = false; applyMode(); } }
    }
    /** @param {any} item @param {number} group @param {number} version */
    async function changeMember(item, group, version) {
        if (busy || peopleLoading || !writable || group !== identity() || version !== generation) return;
        busy = true; applyMode();
        try {
            if (!candidates) {
                const answer = await Swal.fire({ icon: "warning", title: "¿Retirar este integrante?", text: item.nombre, showCancelButton: true, confirmButtonText: "Retirar", cancelButtonText: "Cancelar" });
                if (!answer.isConfirmed) return;
                await post("quitar", { grupoId: group, id: item.id, revision: item.revision, contextoId: field("ContextoId").value });
            } else await post("agregar", { grupoId: group, proveedorId: item.proveedorId, contextoId: field("ContextoId").value });
            await loadPeople(version);
            success(candidates ? "Tercero agregado al grupo." : "Integrante retirado del grupo.");
        } finally { busy = false; applyMode(); }
    }
    if (config.kind === "grupo") {
        const search = node("me-people-search"), size = node("me-people-size");
        if (!(search instanceof HTMLInputElement) || !(size instanceof HTMLSelectElement)) return;
        function find() { peopleSearch = /** @type {HTMLInputElement} */ (search).value.trim(); peoplePage = 1; run(() => loadPeople(generation)); }
        button("me-people-find").addEventListener("click", find);
        button("me-people-clear").addEventListener("click", () => { search.value = ""; find(); });
        search.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); find(); } });
        size.addEventListener("change", () => { peopleSize = Number(size.value); peoplePage = 1; run(() => loadPeople(generation)); });
        button("me-people-prev").addEventListener("click", () => { --peoplePage; run(() => loadPeople(generation)); });
        button("me-people-next").addEventListener("click", () => { ++peoplePage; run(() => loadPeople(generation)); });
        node("me-members").querySelectorAll(".me-people-sort").forEach(el => el.addEventListener("click", () => {
            const sort = el.getAttribute("data-sort") || "nombre"; peopleDir = peopleSort === sort && peopleDir === "asc" ? "desc" : "asc"; peopleSort = sort; peoplePage = 1; run(() => loadPeople(generation));
        }));
        /** @param {boolean} value */
        function picker(value) { candidates = value; peoplePage = 1; peopleSort = "nombre"; peopleDir = "asc"; peopleSearch = ""; /** @type {HTMLInputElement} */ (search).value = ""; run(() => loadPeople(generation)); }
        const add = document.getElementById("me-add-mode"); if (add) add.addEventListener("click", () => picker(true));
        button("me-links-mode").addEventListener("click", () => picker(false));
    }
})();
