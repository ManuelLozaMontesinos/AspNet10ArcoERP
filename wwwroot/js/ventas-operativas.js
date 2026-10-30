/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{id:number,nombre:string}} Lookup */
    /** @typedef {{kind:string,canWrite:boolean,contextoId:number,filters:{sedes:Lookup[],tipos:Lookup[],monedas:Lookup[]},urls:Record<string,string>}} Configuration */
    /** @type {Configuration} */ const config = window.erpListings.config("venta-operativa-config");
    /** @param {string} id */
    function node(id) { const el = document.getElementById(id); if (!el) throw new Error("Falta el control " + id); return el; }
    /** @param {string} id */
    function button(id) { const el = node(id); if (!(el instanceof HTMLButtonElement)) throw new Error("Botón no válido"); return el; }
    const formNode = node("vo-form"); if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode, modalNode = node("vo-modal"), modal = new bootstrap.Modal(modalNode), caja = config.kind === "caja";
    /** @param {string} name */
    function field(name) { const el = form.elements.namedItem(name); if (!(el instanceof HTMLInputElement)) throw new Error("Falta el campo " + name); return el; }
    /** @param {string} id */
    function input(id) { const el = node(id); if (!(el instanceof HTMLInputElement)) throw new Error("Control no válido"); return el; }
    /** @param {string} id */
    function select(id) { const el = node(id); if (!(el instanceof HTMLSelectElement)) throw new Error("Control no válido"); return el; }
    const sizeNode = node("vo-size"); if (!(sizeNode instanceof HTMLSelectElement)) return;
    const size = sizeNode, search = input("vo-search");
    const siteFilter = select("vo-filter-site"), typeFilter = select("vo-filter-type"), currencyFilter = select("vo-filter-currency");
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
    function get(action, values) { const url = new URL(config.urls[action], location.origin); Object.entries(values).forEach(([k, v]) => url.searchParams.set(k, String(v))); return request(url.toString()); }
    /** @param {string} action @param {FormData|Record<string,string|number|boolean>} values */
    function post(action, values) {
        const data = values instanceof FormData ? values : new FormData();
        if (!(values instanceof FormData)) Object.entries(values).forEach(([k, v]) => data.set(k, String(v)));
        data.set("__RequestVerificationToken", field("__RequestVerificationToken").value);
        return request(config.urls[action], { method: "POST", body: data });
    }
    let writable = false, busy = false, loading = false, generation = 0, detailGeneration = 0;
    let tab = caja ? "personal" : "series", mode = "links", page = 1, pages = 1, top = 25, sort = "nombre", dir = "asc", filter = "";
    /** @type {Set<number>} */ const selected = new Set();
    /** @type {any[]} */ let rows = [];
    function identity() { return Number(field("Datos.Id").value); }
    function multiple() { return !caja && mode === "candidates"; }
    function applyMode() {
        const fields = node("vo-fields"); if (fields instanceof HTMLFieldSetElement) fields.disabled = !writable || busy;
        const save = document.getElementById("vo-save"); if (save instanceof HTMLButtonElement) { save.hidden = !writable; save.disabled = busy; }
        if (!caja) button("vo-choose").disabled = !writable || busy || loading || identity() > 0;
        node("vo-tabs").classList.toggle("d-none", mode === "personas");
        typeFilter.hidden = tab !== "series" || mode === "personas";
        currencyFilter.hidden = tab !== "listas" || mode === "personas";
        node("vo-panel").classList.toggle("d-none", !identity() && mode !== "personas");
        node("vo-detail-title").textContent = mode === "personas" ? "Seleccionar personal" : tab === "personal" ? "Personal de la caja" : tab === "series" ? "Series de documentos" : "Listas de precios";
        node("vo-note").textContent = mode === "personas" ? "Seleccione personal activo de la empresa que aún no tenga un vendedor registrado." : !identity() ? "Guarde la cabecera para administrar las asignaciones." : mode === "candidates" ? "Seleccione los registros que desea asignar." : "Asignaciones activas e históricas del registro.";
        button("vo-back").classList.toggle("d-none", mode === "links");
        const add = document.getElementById("vo-add-mode"); if (add instanceof HTMLButtonElement) add.hidden = !writable || !identity() || mode !== "links";
        node("vo-selection").classList.toggle("d-none", !multiple());
        node("vo-selected").textContent = selected.size + " seleccionados (máximo 500)";
        node("vo-details").querySelectorAll("button,input,select").forEach(el => { if (el instanceof HTMLButtonElement || el instanceof HTMLInputElement || el instanceof HTMLSelectElement) el.disabled = busy || loading; });
        button("vo-prev").disabled = busy || loading || page <= 1; button("vo-next").disabled = busy || loading || page >= pages;
        button("vo-add-selected").disabled = busy || loading || !selected.size || !writable;
        button("vo-clear-selected").disabled = busy || loading || !selected.size;
        node("vo-tabs").querySelectorAll("[data-vo-tab]").forEach(el => { const active = el.getAttribute("data-vo-tab") === tab; el.classList.toggle("active", active); el.setAttribute("aria-pressed", String(active)); });
    }
    function resetGrid() { page = 1; pages = 1; top = 25; sort = tab === "series" && mode !== "personas" ? "sede" : "nombre"; dir = "asc"; filter = ""; search.value = ""; size.value = "25"; siteFilter.value = ""; typeFilter.value = ""; currencyFilter.value = ""; selected.clear(); rows = []; }
    function filterOptions() {
        /** @param {HTMLSelectElement} el @param {Lookup[]} items @param {string} label */
        const options = (el, items, label) => el.replaceChildren(new Option(label, ""), ...items.map(item => new Option(item.nombre, String(item.id))));
        options(siteFilter, config.filters.sedes, "Todas las sedes"); options(typeFilter, config.filters.tipos, "Todos los documentos"); options(currencyFilter, config.filters.monedas, "Todas las monedas");
    }
    /** @param {number} id */
    async function open(id) {
        if (busy) return; const version = ++generation; ++detailGeneration;
        form.reset(); field("Datos.Id").value = "0"; field("Revision").value = ""; field("ContextoId").value = String(config.contextoId);
        if (!caja) field("Datos.ProveedorId").value = "0";
        tab = caja ? "personal" : "series"; mode = "links"; filterOptions(); resetGrid(); writable = false; busy = true; loading = false;
        node("vo-title").textContent = id ? "Cargando..." : caja ? "Nueva caja" : "Nuevo vendedor"; applyMode(); modal.show();
        try {
            if (id) {
                const json = await get("obtener", { id }); if (version !== generation) return;
                fill(json.data); writable = config.canWrite && json.data.estado === "A";
                if (caja) input("vo-site").value = json.data.sede;
            } else writable = config.canWrite;
            if (id) await loadDetails(version);
        } finally { if (version === generation) { busy = false; applyMode(); } }
    }
    /** @param {any} data */
    function fill(data) {
        form.querySelectorAll('[name^="Datos."]').forEach(el => { if (!(el instanceof HTMLInputElement)) return; const name = el.name.slice(6), value = data[name[0].toLowerCase() + name.slice(1)]; el.value = value == null ? "" : String(value); });
        field("Revision").value = data.revision; node("vo-title").textContent = data.nombre;
    }
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { ++generation; ++detailGeneration; } });
    window.erpListings.onClick(".js-vo-new", () => run(() => open(0)));
    window.erpListings.onClick(".js-vo-edit", el => run(() => open(Number(el.dataset.id))));
    /** @param {HTMLButtonElement} el @param {boolean} activate */
    async function changeOwner(el, activate) {
        if (busy || !config.canWrite) return;
        const values = { Id: Number(el.dataset.id), Revision: el.dataset.revision || "", ContextoId: config.contextoId, Activar: activate };
        busy = true;
        try {
            const answer = await Swal.fire({ icon: "warning", title: activate ? "¿Activar este vendedor?" : "¿Anular este registro?", text: "Las asignaciones conservan su estado actual.", showCancelButton: true, confirmButtonText: activate ? "Activar" : "Anular", cancelButtonText: "Cancelar" });
            if (!answer.isConfirmed) return;
            const json = await post("estado", values); await window.erpListings.load(location.href, false); success(json.message);
        } finally { busy = false; }
    }
    window.erpListings.onClick(".js-vo-cancel", el => run(() => changeOwner(el, false)));
    window.erpListings.onClick(".js-vo-activate", el => run(() => changeOwner(el, true)));
    form.addEventListener("submit", event => { event.preventDefault(); run(async () => {
        if (busy || !writable || !form.reportValidity()) return;
        if (!caja && !Number(field("Datos.ProveedorId").value)) throw new Error("Seleccione personal para el vendedor.");
        const data = new FormData(form); form.querySelectorAll('[name^="Datos."]').forEach(el => { if (el instanceof HTMLInputElement) data.set(el.name, el.value); });
        busy = true; applyMode();
        try {
            const json = await post("guardar", data); field("Datos.Id").value = String(json.id); field("Revision").value = "";
            const fresh = await get("obtener", { id: json.id }); fill(fresh.data);
            mode = "links"; resetGrid(); await loadDetails(generation); await window.erpListings.load(location.href, false); success(json.message);
        } finally { busy = false; applyMode(); }
    }); });
    /** @returns {Array<[string,string]>} */
    function columns() {
        if (mode === "personas") return [["codigo", "Código"], ["nombre", "Apellidos y nombres"], ["documento", "Documento"], ["estado", "Estado"]];
        if (tab === "personal") return [["codigo", "Código"], ["nombre", "Apellidos y nombres"], ["documento", "Documento"], ["sede", "Sede"], ["estado", "Estado"]];
        if (tab === "series") return [["sede", "Sede"], ["almacen", "Almacén"], ["nombre", "Documento"], ["codigo", "Serie"], ["correlativo", "Correlativo"], ["estado", "Estado"]];
        return [["sede", "Sede"], ["nombre", "Nombre"], ["moneda", "Moneda"], ["estado", "Estado"]];
    }
    /** @param {number[]} ids @param {boolean} checked */
    function choose(ids, checked) {
        const next = new Set(selected); ids.forEach(id => checked ? next.add(id) : next.delete(id));
        if (next.size > 500) { showError(new Error("Seleccione hasta 500 registros por lote.")); render(); return; }
        selected.clear(); next.forEach(id => selected.add(id)); render(); applyMode();
    }
    function render() {
        const head = node("vo-head"), body = node("vo-rows"), header = document.createElement("tr"), cols = columns(); head.replaceChildren(header); body.replaceChildren();
        if (multiple()) {
            const th = document.createElement("th"), check = document.createElement("input"); check.type = "checkbox"; check.className = "form-check-input"; check.setAttribute("aria-label", "Seleccionar esta página");
            check.checked = rows.length > 0 && rows.every(r => selected.has(r.destinoId)); check.disabled = !rows.length;
            check.addEventListener("change", () => choose(rows.map(r => r.destinoId), check.checked)); th.append(check); header.append(th);
        }
        for (const [key, label] of cols) {
            const th = document.createElement("th"), control = document.createElement("button"); th.scope = "col"; th.setAttribute("aria-sort", sort === key ? (dir === "asc" ? "ascending" : "descending") : "none");
            control.type = "button"; control.className = "btn btn-link text-body fw-semibold p-0 text-decoration-none"; control.textContent = label + (sort === key ? dir === "asc" ? " ↑" : " ↓" : " ↕");
            control.addEventListener("click", () => { dir = sort === key && dir === "asc" ? "desc" : "asc"; sort = key; page = 1; run(() => loadDetails(generation)); }); th.append(control); header.append(th);
        }
        const options = document.createElement("th"); options.scope = "col"; options.className = "listing-options"; options.textContent = "Opciones"; header.append(options);
        for (const item of rows) {
            const row = document.createElement("tr");
            if (multiple()) { const cell = document.createElement("td"), check = document.createElement("input"); check.type = "checkbox"; check.className = "form-check-input"; check.checked = selected.has(item.destinoId); check.setAttribute("aria-label", "Seleccionar " + item.nombre + " " + item.codigo); check.addEventListener("change", () => choose([item.destinoId], check.checked)); cell.append(check); row.append(cell); }
            for (const [key] of cols) {
                const cell = document.createElement("td");
                if (key === "estado") { const badge = document.createElement("span"); badge.className = "badge " + (item.estado === "A" ? "bg-success-subtle text-success border border-success-subtle" : "bg-secondary-subtle text-body-secondary border"); badge.textContent = item.estado === "A" ? "Activo" : "Anulado"; cell.append(badge); }
                else { cell.textContent = String(item[key] ?? ""); if (key === "correlativo") cell.className = "text-end"; } row.append(cell);
            }
            const cell = document.createElement("td"); cell.className = "listing-options";
            if (writable && (mode !== "links" || item.estado === "A" || !caja)) {
                const control = document.createElement("button"), owner = identity(), version = generation, currentMode = mode, currentTab = tab;
                control.type = "button"; control.className = "btn btn-sm " + (mode !== "links" ? "btn-outline-primary" : item.estado === "A" ? "btn-outline-danger" : "btn-outline-success");
                control.textContent = mode === "personas" ? "Seleccionar" : mode === "candidates" ? "Agregar" : item.estado === "A" ? "Retirar" : "Activar";
                control.addEventListener("click", () => run(async () => {
                    if (busy || loading || version !== generation || owner !== identity() || currentMode !== mode || currentTab !== tab) return;
                    if (mode === "personas") { field("Datos.ProveedorId").value = String(item.destinoId); field("Datos.Nombre").value = item.nombre; mode = "links"; resetGrid(); await loadDetails(version); }
                    else if (mode === "candidates") await addTargets([item.destinoId]);
                    else await changeAssignment(item);
                })); cell.append(control);
            }
            row.append(cell); body.append(row);
        }
        if (!rows.length) { const row = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = cols.length + 1 + (multiple() ? 1 : 0); cell.className = "text-center text-muted py-4"; cell.textContent = "No se encontraron registros."; row.append(cell); body.append(row); }
    }
    /** @param {number} version */
    async function loadDetails(version) {
        if (!identity() && mode !== "personas") { rows = []; render(); applyMode(); return; }
        const nonce = ++detailGeneration, owner = identity(); loading = true; applyMode();
        try {
            /** @type {Record<string,string|number>} */ const values = { id: owner, tipo: tab, search: filter, page, top, sort, dir };
            if (siteFilter.value) values.filtroSedeId = siteFilter.value;
            if (!typeFilter.hidden && typeFilter.value) values.tipoDocumentoId = typeFilter.value;
            if (!currencyFilter.hidden && currencyFilter.value) values.monedaId = currencyFilter.value;
            const json = await get(mode === "personas" ? "personas" : mode === "candidates" ? "candidatos" : "asignaciones", values);
            if (version !== generation || nonce !== detailGeneration || owner !== identity()) return;
            const data = json.data; rows = data.items; page = data.page; pages = data.pageCount; sort = data.sort; dir = data.dir; render();
            node("vo-range").textContent = `Mostrando ${data.firstRecord} a ${data.lastRecord} de ${data.total} registros · Página ${page} de ${pages}`;
        } catch (error) { if (version === generation && nonce === detailGeneration) { rows = []; render(); node("vo-range").textContent = "No se pudo cargar el detalle. Vuelva a buscar."; throw error; } }
        finally { if (nonce === detailGeneration) { loading = false; applyMode(); } }
    }
    /** @param {number[]} ids */
    async function addTargets(ids) {
        if (busy || loading || !writable || !identity()) return;
        busy = true; applyMode();
        try {
            const values = new FormData(); values.set("OwnerId", String(identity())); values.set("Tipo", tab); values.set("ContextoId", field("ContextoId").value); ids.forEach((id, i) => values.set(`DestinoIds[${i}]`, String(id)));
            const json = await post("agregar", values); ids.forEach(id => selected.delete(id)); await loadDetails(generation); success(json.message);
        } finally { busy = false; applyMode(); }
    }
    /** @param {any} item */
    async function changeAssignment(item) {
        if (busy || loading || !writable) return; busy = true; applyMode();
        try {
            const activate = item.estado === "I", answer = await Swal.fire({ icon: "warning", title: activate ? "¿Activar esta asignación?" : "¿Retirar esta asignación?", text: item.nombre + " " + item.codigo, showCancelButton: true, confirmButtonText: activate ? "Activar" : "Retirar", cancelButtonText: "Cancelar" });
            if (!answer.isConfirmed) return;
            const json = await post("estadoAsignacion", { OwnerId: identity(), Tipo: tab, Id: item.id, Revision: item.revision, Activar: activate, ContextoId: field("ContextoId").value });
            await loadDetails(generation); success(json.message);
        } finally { busy = false; applyMode(); }
    }
    function find() { filter = search.value.trim(); page = 1; run(() => loadDetails(generation)); }
    button("vo-find").addEventListener("click", find); button("vo-clear").title = "Limpiar filtros";
    button("vo-clear").addEventListener("click", () => { search.value = ""; siteFilter.value = ""; typeFilter.value = ""; currencyFilter.value = ""; find(); });
    [siteFilter, typeFilter, currencyFilter].forEach(el => el.addEventListener("change", find));
    search.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); find(); } });
    size.addEventListener("change", () => { top = Number(size.value); page = 1; run(() => loadDetails(generation)); });
    button("vo-prev").addEventListener("click", () => { --page; run(() => loadDetails(generation)); }); button("vo-next").addEventListener("click", () => { ++page; run(() => loadDetails(generation)); });
    /** @param {string} value */
    function switchMode(value) { mode = value; resetGrid(); run(() => loadDetails(generation)); }
    const add = document.getElementById("vo-add-mode"); if (add) add.addEventListener("click", () => switchMode("candidates"));
    button("vo-back").addEventListener("click", () => switchMode("links"));
    if (!caja) button("vo-choose").addEventListener("click", () => switchMode("personas"));
    node("vo-tabs").querySelectorAll("[data-vo-tab]").forEach(el => el.addEventListener("click", () => { tab = el.getAttribute("data-vo-tab") || "personal"; switchMode("links"); }));
    button("vo-add-selected").addEventListener("click", () => run(() => addTargets([...selected])));
    button("vo-clear-selected").addEventListener("click", () => { selected.clear(); render(); applyMode(); });
})();
