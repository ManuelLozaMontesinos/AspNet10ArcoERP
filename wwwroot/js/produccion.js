/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{id:number,procesoId:number,subProcesoId:number,personalId:number,nombre:string,codigo:string,documento:string,sede:string,estado:string,estadoPersonal:string,revision:string}} Row */
    /** @typedef {{id:number,nombre:string,codigo:string}} Lookup */
    /** @type {{canWrite:boolean,contextoId:number,urls:Record<string,string>}} */ const config = window.erpListings.config("produccion-config");
    /** @param {string} id */
    function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} id */
    function input(id) { const value = node(id); if (!(value instanceof HTMLInputElement)) throw new Error("Campo no válido"); return value; }
    /** @param {string} id */
    function select(id) { const value = node(id); if (!(value instanceof HTMLSelectElement)) throw new Error("Selector no válido"); return value; }
    /** @param {string} id */
    function button(id) { const value = node(id); if (!(value instanceof HTMLButtonElement)) throw new Error("Botón no válido"); return value; }
    const formNode = node("pd-form"); if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode, modalNode = node("pd-modal"), modal = new bootstrap.Modal(modalNode), name = input("pd-name"), search = input("pd-search"), size = select("pd-size"), sub = select("pd-sub");
    const panel = node("pd-panel"), note = node("pd-note"), head = node("pd-head"), body = node("pd-rows"), range = node("pd-range"), editor = node("pd-sub-editor"), subName = input("pd-sub-name"), save = document.getElementById("pd-save"), add = document.getElementById("pd-add");
    let processId = 0, editable = false, active = true, busy = false, sessionVersion = 0, detailVersion = 0, tab = "subprocesos", candidates = false, page = 1, pageCount = 1, sort = "nombre", dir = "asc";
    /** @type {Lookup[]} */ let options = [];
    /** @type {Row|null} */ let editingSub = null;
    /** @type {Map<number,Row>} */ let rows = new Map();
    /** @param {string} field */
    function hidden(field) { const control = form.elements.namedItem(field); if (!(control instanceof HTMLInputElement)) throw new Error("Falta el dato " + field); return control; }
    /** @param {unknown} error */
    function failure(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} url @param {RequestInit} [init] */
    async function request(url, init) {
        const response = await fetch(url, { credentials: "same-origin", ...init, headers: { "X-Requested-With": "XMLHttpRequest", ...init?.headers } });
        if (response.redirected) throw new Error("La sesión no está disponible. Seleccione nuevamente su perfil de acceso.");
        const json = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "No se pudo consultar o guardar el registro.");
        return json;
    }
    /** @param {string} action @param {Record<string,string|number>} values */
    async function post(action, values) {
        const data = new FormData(); Object.entries(values).forEach(([key, value]) => data.set(key, String(value)));
        data.set("ContextoId", String(config.contextoId)); data.set("__RequestVerificationToken", hidden("__RequestVerificationToken").value);
        return request(config.urls[action], { method: "POST", body: data });
    }
    /** @param {() => Promise<void>} action */
    function run(action) {
        if (busy) return; busy = true; updateControls();
        void action().catch(failure).finally(() => { busy = false; updateControls(); });
    }
    function canEdit() { return editable && active && config.canWrite; }
    function canAssign() { return canEdit() && options.some(o => o.id === Number(sub.value) && o.codigo === "A"); }
    function updateControls() {
        name.readOnly = busy || !canEdit(); subName.readOnly = busy; size.disabled = busy; sub.disabled = busy; search.disabled = busy;
        if (save instanceof HTMLButtonElement) { save.hidden = !canEdit(); save.disabled = busy; }
        if (add instanceof HTMLButtonElement) { add.hidden = !canEdit() || candidates; add.disabled = busy || !processId || (tab === "operadores" && !canAssign()); }
        button("pd-sub-save").disabled = busy || !canEdit(); button("pd-sub-discard").disabled = busy;
        button("pd-find").disabled = busy; button("pd-clear").disabled = busy; button("pd-back").disabled = busy;
        button("pd-prev").disabled = busy || page <= 1; button("pd-next").disabled = busy || page >= pageCount;
    }
    /** @param {number} id @param {boolean} replaceHeader */
    async function metadata(id, replaceHeader) {
        const url = new URL(config.urls.obtener, location.origin); url.searchParams.set("id", String(id));
        const result = await request(url.toString()); applyMetadata(result.data, replaceHeader);
    }
    /** @param {{datos:Row,subprocesos:Lookup[]}} data @param {boolean} replaceHeader */
    function applyMetadata(data, replaceHeader) {
        processId = data.datos.id; active = data.datos.estado === "A";
        if (replaceHeader) { hidden("Id").value = String(processId); hidden("ProcesoId").value = String(processId); hidden("Revision").value = data.datos.revision; name.value = data.datos.nombre; }
        const previous = Number(sub.value); options = data.subprocesos; sub.replaceChildren();
        options.forEach(row => sub.add(new Option(row.nombre + (row.codigo === "A" ? "" : " (anulado)"), String(row.id))));
        sub.value = String(options.find(o => o.id === previous)?.id || options.find(o => o.codigo === "A")?.id || options[0]?.id || "");
        updateControls();
    }
    /** @param {number} id @param {boolean} edit */
    async function open(id, edit) {
        if (busy) return; const ticket = ++sessionVersion; detailVersion++;
        let data = null;
        if (id) { const url = new URL(config.urls.obtener, location.origin); url.searchParams.set("id", String(id)); data = (await request(url.toString())).data; }
        if (ticket !== sessionVersion) return;
        form.reset(); hidden("ContextoId").value = String(config.contextoId); hidden("Id").value = "0"; hidden("ProcesoId").value = "0"; hidden("Revision").value = "";
        processId = 0; editable = edit && config.canWrite; active = true; tab = "subprocesos"; candidates = false; page = 1; sort = "nombre"; dir = "asc"; search.value = ""; size.value = "25"; options = []; sub.replaceChildren(); editingSub = null; editor.hidden = true;
        if (data) applyMetadata(data, true); updateControls(); updatePanel(); modal.show(); await loadDetails();
    }
    function updatePanel() {
        panel.hidden = !processId; sub.hidden = tab === "subprocesos"; button("pd-back").hidden = !candidates;
        document.querySelectorAll("[data-pd-tab]").forEach(el => el.classList.toggle("active", el instanceof HTMLElement && el.dataset.pdTab === tab));
        note.textContent = !processId ? "Guarde el proceso para administrar sus detalles." : tab === "subprocesos" ? "Subprocesos del catálogo compartido. Sus nombres se guardan de forma individual." : candidates ? "Seleccione personal activo de la empresa para asignarlo al subproceso." : "Operadores de la empresa actual. Seleccione un subproceso para consultar sus asignaciones.";
        if (tab !== "subprocesos") editor.hidden = true; updateControls();
    }
    async function loadDetails() {
        updatePanel(); const version = ++detailVersion;
        if (!processId || (tab === "operadores" && !sub.value)) { rows.clear(); page = 1; pageCount = 1; render([]); range.textContent = "Viendo 0–0 de 0 registros"; updateControls(); return; }
        const url = new URL(config.urls.detalles, location.origin);
        Object.entries({ procesoId: processId, subProcesoId: Number(sub.value) || 0, tipo: candidates ? "personas" : tab, search: search.value, page, top: size.value, sort, dir }).forEach(([key, value]) => url.searchParams.set(key, String(value)));
        const result = await request(url.toString()); if (version !== detailVersion) return;
        /** @type {{items:Row[],page:number,pageCount:number,firstRecord:number,lastRecord:number,total:number,sort:string,dir:string}} */ const data = result.data;
        page = data.page; pageCount = data.pageCount; sort = data.sort; dir = data.dir; rows = new Map(data.items.map(row => [row.id, row]));
        render(data.items); range.textContent = `Viendo ${data.firstRecord}–${data.lastRecord} de ${data.total} registros`;
        button("pd-prev").disabled = page <= 1; button("pd-next").disabled = page >= pageCount;
    }
    /** @param {Row[]} items */
    function render(items) {
        const columns = tab === "subprocesos" ? [["nombre", "Nombre"], ["estado", "Estado"]] : [["codigo", "Código"], ["nombre", "Operador"], ["documento", "Documento"], ["sede", "Sede"]];
        head.replaceChildren(); body.replaceChildren(); const tr = document.createElement("tr");
        columns.forEach(([key, title]) => {
            const th = document.createElement("th"), link = document.createElement("a"), icon = document.createElement("i");
            th.setAttribute("aria-sort", key === sort ? dir === "asc" ? "ascending" : "descending" : "none"); link.href = "#"; link.className = "text-decoration-none text-reset d-inline-flex align-items-center gap-1"; link.dataset.pdSort = key; link.textContent = title;
            icon.className = "bi " + (key === sort ? dir === "asc" ? "bi-arrow-up" : "bi-arrow-down" : "bi-arrow-down-up"); link.append(icon); th.append(link); tr.append(th);
        }); const optionHead = document.createElement("th"); optionHead.className = "listing-options"; optionHead.textContent = "Opciones"; tr.append(optionHead); head.append(tr);
        items.forEach(row => {
            const line = document.createElement("tr"); columns.forEach(([key]) => {
                const cell = document.createElement("td");
                if (key === "estado") { const badge = document.createElement("span"); badge.className = row.estado === "A" ? "badge bg-success-subtle text-success border border-success-subtle" : "badge bg-secondary-subtle text-body-secondary border"; badge.textContent = row.estado === "A" ? "Activo" : "Anulado"; cell.append(badge); }
                else cell.textContent = String(row[/** @type {keyof Row} */ (key)] ?? ""); line.append(cell);
            });
            const actions = document.createElement("td"); actions.className = "listing-options";
            /** @param {string} action @param {string} title @param {string} icon */
            function action(action, title, icon) { const btn = document.createElement("button"), glyph = document.createElement("i"); btn.type = "button"; btn.className = "btn btn-sm btn-outline-primary"; btn.dataset.pdAction = action; btn.dataset.id = String(row.id); btn.title = title; glyph.className = "bi " + icon; btn.append(glyph); actions.append(btn); }
            if (tab === "subprocesos" && canEdit() && row.estado === "A") action("edit", "Editar nombre", "bi-pencil");
            else if (candidates && canAssign()) action("add", "Asignar operador", "bi-plus-lg");
            else if (tab === "operadores" && canAssign()) action("remove", "Retirar operador", "bi-x-circle");
            else actions.textContent = "—";
            line.append(actions); body.append(line);
        });
        if (!items.length) { const line = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = columns.length + 1; cell.className = "text-center text-muted py-4"; cell.textContent = "No se encontraron registros."; line.append(cell); body.append(line); }
    }
    /** @param {string} message */
    function success(message) { void Swal.fire({ icon: "success", title: message, toast: true, position: "top-end", timer: 2000, showConfirmButton: false }); }
    async function saveSub() {
        const value = subName.value.trim(); if (!canEdit() || !processId || !value || value.length > 150) throw new Error("Ingrese un nombre de hasta 150 caracteres y guarde primero el proceso.");
        const result = await post("guardarSub", { ProcesoId: processId, Id: editingSub?.id || 0, Revision: editingSub?.revision || "", Nombre: value });
        editor.hidden = true; editingSub = null; await metadata(processId, false); await loadDetails(); success(result.message);
    }
    document.getElementById("pd-new")?.addEventListener("click", () => { void open(0, true).catch(failure); });
    window.erpListings.onClick(".js-pd-view", btn => { void open(Number(btn.dataset.id), false).catch(failure); });
    window.erpListings.onClick(".js-pd-edit", btn => { void open(Number(btn.dataset.id), true).catch(failure); });
    window.erpListings.onClick(".js-pd-cancel", btn => run(async () => {
        const confirmed = await Swal.fire({ icon: "warning", title: "¿Anular este proceso del catálogo compartido?", text: "Los subprocesos y asignaciones se conservan.", showCancelButton: true, confirmButtonText: "Anular", cancelButtonText: "Cancelar" });
        if (!confirmed.isConfirmed) return;
        const result = await post("anular", { ProcesoId: Number(btn.dataset.id), Id: Number(btn.dataset.id), Revision: btn.dataset.revision || "" }); await window.erpListings.load(location.href, false); success(result.message);
    }));
    form.addEventListener("submit", event => {
        event.preventDefault(); if (busy || !canEdit() || !form.reportValidity()) return;
        run(async () => {
            const result = await post("guardar", { Id: processId, ProcesoId: processId, Nombre: name.value, Revision: hidden("Revision").value });
            await metadata(result.id, true); await loadDetails(); await window.erpListings.load(location.href, false); success(result.message);
        });
    });
    form.addEventListener("keydown", event => {
        if (event.key !== "Enter") return;
        if (event.target === search) { event.preventDefault(); page = 1; void loadDetails().catch(failure); }
        if (event.target === subName) { event.preventDefault(); run(saveSub); }
    });
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { sessionVersion++; detailVersion++; } });
    form.addEventListener("click", event => {
        const target = event.target instanceof Element ? event.target.closest("[data-pd-tab],[data-pd-sort],[data-pd-action]") : null;
        if (!(target instanceof HTMLElement) || busy) return;
        if (target.dataset.pdTab) { tab = target.dataset.pdTab; candidates = false; page = 1; sort = "nombre"; dir = "asc"; search.value = ""; void loadDetails().catch(failure); }
        else if (target.dataset.pdSort) { event.preventDefault(); const key = target.dataset.pdSort; dir = sort === key && dir === "asc" ? "desc" : "asc"; sort = key; page = 1; void loadDetails().catch(failure); }
        else if (target.dataset.pdAction) {
            const row = rows.get(Number(target.dataset.id)); if (!row) return;
            if (target.dataset.pdAction === "edit" && canEdit()) { editingSub = row; subName.value = row.nombre; editor.hidden = false; subName.focus(); }
            else if (target.dataset.pdAction === "add" && canAssign()) run(async () => { const result = await post("agregar", { ProcesoId: processId, SubProcesoId: Number(sub.value), PersonalId: row.personalId }); await loadDetails(); success(result.message); });
            else if (target.dataset.pdAction === "remove" && canAssign()) run(async () => {
                const ask = await Swal.fire({ icon: "question", title: "¿Retirar este operador del subproceso?", showCancelButton: true, confirmButtonText: "Retirar", cancelButtonText: "Cancelar" }); if (!ask.isConfirmed) return;
                const result = await post("quitar", { ProcesoId: processId, SubProcesoId: Number(sub.value), Id: row.id, Revision: row.revision }); await loadDetails(); success(result.message);
            });
        }
    });
    add?.addEventListener("click", () => {
        if (busy || !canEdit()) return;
        if (tab === "subprocesos") { editingSub = null; subName.value = ""; editor.hidden = false; subName.focus(); }
        else if (canAssign()) { candidates = true; page = 1; search.value = ""; void loadDetails().catch(failure); }
    });
    button("pd-sub-save").addEventListener("click", () => run(saveSub));
    button("pd-sub-discard").addEventListener("click", () => { if (!busy) { editor.hidden = true; editingSub = null; } });
    button("pd-back").addEventListener("click", () => { if (!busy) { candidates = false; page = 1; search.value = ""; void loadDetails().catch(failure); } });
    button("pd-find").addEventListener("click", () => { page = 1; void loadDetails().catch(failure); });
    button("pd-clear").addEventListener("click", () => { search.value = ""; page = 1; void loadDetails().catch(failure); });
    [size, sub].forEach(control => control.addEventListener("change", () => { page = 1; void loadDetails().catch(failure); }));
    button("pd-prev").addEventListener("click", () => { if (page > 1) { page--; void loadDetails().catch(failure); } });
    button("pd-next").addEventListener("click", () => { if (page < pageCount) { page++; void loadDetails().catch(failure); } });
})();
