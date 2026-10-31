/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{id:number,productoId:number,claseId:number,familiaId:number,codigo:string,producto:string,familia:string,cantidadTexto:string,cantidadVisible:string,estado:string,productoEstado:string,revision:string}} Row */
    /** @type {{canWrite:boolean,contextoId:number,urls:Record<string,string>}} */ const config = window.erpListings.config("cortes-gratis-config");
    /** @param {string} id */ function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} id */ function input(id) { const value = node(id); if (!(value instanceof HTMLInputElement)) throw new Error("Campo inválido"); return value; }
    /** @param {string} id */ function select(id) { const value = node(id); if (!(value instanceof HTMLSelectElement)) throw new Error("Selector inválido"); return value; }
    /** @param {string} id */ function button(id) { const value = node(id); if (!(value instanceof HTMLButtonElement)) throw new Error("Botón inválido"); return value; }
    const formNode = node("cg-form"); if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode, modalNode = node("cg-modal"), modal = new bootstrap.Modal(modalNode), body = node("cg-rows"), head = node("cg-head"), editor = node("cg-editor"), quantity = input("cg-quantity"), family = select("cg-family"), size = select("cg-size"), search = input("cg-search"), save = document.getElementById("cg-save");
    let classId = 0, writable = false, busy = false, loading = false, sessionVersion = 0, detailVersion = 0, page = 1, pages = 1, sort = "producto", dir = "asc";
    /** @type {Row|null} */ let editing = null;
    /** @type {Map<number,Row>} */ let rows = new Map();
    /** @param {string} name */ function hidden(name) { const control = form.elements.namedItem(name); if (!(control instanceof HTMLInputElement)) throw new Error("Falta un dato del registro."); return control; }
    /** @param {unknown} error */ function failure(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} url @param {RequestInit} [init] */ async function request(url, init) {
        const response = await fetch(url, { credentials: "same-origin", ...init, headers: { "X-Requested-With": "XMLHttpRequest", ...init?.headers } });
        const json = !response.redirected && response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "La sesión no está disponible o no se pudo completar la operación."); return json;
    }
    function updateControls() {
        const locked = busy || loading || editing !== null; family.disabled = locked; size.disabled = locked; search.disabled = locked;
        button("cg-find").disabled = locked; button("cg-clear").disabled = locked; button("cg-prev").disabled = locked || page <= 1; button("cg-next").disabled = locked || page >= pages;
        button("cg-discard").disabled = busy; quantity.readOnly = busy || !writable; if (save instanceof HTMLButtonElement) { save.hidden = !writable; save.disabled = busy; }
        body.querySelectorAll("button").forEach(control => { control.disabled = locked; });
    }
    function closeEditor() { editing = null; editor.hidden = true; quantity.disabled = true; quantity.value = ""; updateControls(); }
    async function load() {
        const version = ++detailVersion, session = sessionVersion;
        loading = true; updateControls();
        try {
        const url = new URL(config.urls.productos, location.origin);
        Object.entries({ claseId: classId, familiaId: family.value, search: search.value, page, top: size.value, sort, dir }).forEach(([key, value]) => { if (value !== "") url.searchParams.set(key, String(value)); });
        const result = await request(url.toString()); if (version !== detailVersion || session !== sessionVersion) return;
        /** @type {{items:Row[],page:number,pageCount:number,sort:string,dir:string,firstRecord:number,lastRecord:number,total:number}} */ const data = result.data;
        page = data.page; pages = data.pageCount; sort = data.sort; dir = data.dir; rows = new Map(data.items.map(row => [row.id, row])); render(data.items);
        node("cg-range").textContent = `Viendo ${data.firstRecord}–${data.lastRecord} de ${data.total} registros · Página ${page} de ${pages}`;
        } finally { if (version === detailVersion && session === sessionVersion) { loading = false; updateControls(); } }
    }
    /** @param {Row[]} items */ function render(items) {
        const columns = [["codigo", "Código"], ["producto", "Producto"], ["familia", "Familia"], ["cantidad", "Cortes gratis"], ["estado", "Configuración"], ["productoestado", "Estado producto"]];
        head.replaceChildren(); body.replaceChildren(); const headerRow = document.createElement("tr");
        columns.forEach(([key, title]) => {
            const th = document.createElement("th"), link = document.createElement("a"), icon = document.createElement("i");
            th.setAttribute("aria-sort", key === sort ? dir === "asc" ? "ascending" : "descending" : "none"); link.href = "#"; link.dataset.cgSort = key; link.className = "text-decoration-none text-body d-inline-flex align-items-center gap-1"; link.textContent = title;
            icon.className = "bi " + (key === sort ? dir === "asc" ? "bi-arrow-up" : "bi-arrow-down" : "bi-arrow-down-up"); link.append(icon); th.append(link); if (key === "cantidad") th.className = "text-end"; headerRow.append(th);
        }); const options = document.createElement("th"); options.textContent = "Opciones"; options.className = "listing-options"; headerRow.append(options); head.append(headerRow);
        items.forEach(row => {
            const line = document.createElement("tr"); columns.forEach(([key]) => {
                const cell = document.createElement("td");
                if (key === "estado" || key === "productoestado") { const state = key === "estado" ? row.estado : row.productoEstado, badge = document.createElement("span"); badge.className = state === "A" ? "badge bg-success-subtle text-success" : "badge bg-secondary-subtle text-body-secondary"; badge.textContent = state === "A" ? "Activo" : state === "I" ? "Inactivo" : "Sin estado"; cell.append(badge); }
                else if (key === "cantidad") { cell.textContent = row.cantidadVisible; cell.className = "text-end"; }
                else cell.textContent = String(row[/** @type {keyof Row} */ (key)] ?? ""); line.append(cell);
            });
            const cell = document.createElement("td"), action = document.createElement("button"), icon = document.createElement("i"); cell.className = "listing-options"; action.type = "button"; action.className = "btn btn-sm btn-outline-primary"; action.dataset.cgRow = String(row.id); action.title = writable ? "Editar cantidad" : "Consultar cantidad"; action.setAttribute("aria-label", action.title); icon.className = "bi " + (writable ? "bi-pencil-square" : "bi-eye"); action.append(icon); cell.append(action); line.append(cell); body.append(line);
        }); if (!items.length) { const line = document.createElement("tr"), cell = document.createElement("td"); cell.colSpan = 7; cell.className = "text-center text-muted py-4"; cell.textContent = "No se encontraron configuraciones para estos filtros."; line.append(cell); body.append(line); }
    }
    /** @param {number} id @param {boolean} edit */ async function open(id, edit) {
        if (busy) return; const ticket = ++sessionVersion; detailVersion++;
        const result = await request(config.urls.obtener + "?id=" + encodeURIComponent(String(id))); if (ticket !== sessionVersion) return;
        const data = result.data; form.reset(); classId = data.datos.id; writable = edit && config.canWrite && data.datos.estado === "A";
        hidden("ClaseId").value = String(classId); hidden("ContextoId").value = String(config.contextoId); node("cg-class").textContent = data.datos.codigo + " · " + data.datos.nombre; node("cg-site").textContent = data.sede; node("cg-mode").textContent = writable ? "Edición de cantidades" : "Solo consulta";
        family.replaceChildren(new Option("Todas las familias", "")); data.familias.forEach(/** @param {{id:number,nombre:string}} option */ option => { family.add(new Option(option.nombre, String(option.id))); });
        search.value = ""; size.value = "25"; page = 1; pages = 1; sort = "producto"; dir = "asc"; rows.clear(); body.replaceChildren(); head.replaceChildren(); node("cg-range").textContent = "Cargando productos…"; closeEditor(); modal.show(); await load();
    }
    window.erpListings.onClick(".js-cg-open", control => { void open(Number(control.dataset.id), control.dataset.edit === "true").catch(failure); });
    form.addEventListener("click", event => {
        const target = event.target instanceof Element ? event.target.closest("[data-cg-sort],[data-cg-row]") : null; if (!(target instanceof HTMLElement)) return; event.preventDefault(); if (busy || loading || editing) return;
        if (target.dataset.cgSort) { const key = target.dataset.cgSort; dir = sort === key && dir === "asc" ? "desc" : "asc"; sort = key; page = 1; void load().catch(failure); }
        else { const row = rows.get(Number(target.dataset.cgRow)); if (!row) return; editing = row; editor.hidden = false; quantity.disabled = false; quantity.value = row.cantidadTexto; node("cg-product").textContent = row.codigo + " · " + row.producto; updateControls(); if (writable) quantity.focus(); }
    });
    button("cg-discard").addEventListener("click", closeEditor);
    function refresh() { if (busy || loading || editing) return; page = 1; void load().catch(failure); }
    family.addEventListener("change", refresh); size.addEventListener("change", refresh); button("cg-find").addEventListener("click", refresh);
    button("cg-clear").addEventListener("click", () => { search.value = ""; refresh(); });
    button("cg-prev").addEventListener("click", () => { if (busy || loading || editing || page <= 1) return; page--; void load().catch(failure); });
    button("cg-next").addEventListener("click", () => { if (busy || loading || editing || page >= pages) return; page++; void load().catch(failure); });
    form.addEventListener("keydown", event => { if (event.key === "Enter" && event.target === search) { event.preventDefault(); refresh(); } });
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { sessionVersion++; detailVersion++; loading = false; closeEditor(); } });
    form.addEventListener("submit", async event => {
        event.preventDefault(); if (busy || !editing || !writable || !form.reportValidity()) return;
        const row = editing, ticket = sessionVersion; busy = true; updateControls();
        try {
            const answer = await Swal.fire({ icon: "question", title: "Guardar cantidad", text: row.estado === "A" ? "Se actualizará la configuración de este producto en la sede seleccionada." : "Se guardará la cantidad y se activará la configuración de este producto en la sede seleccionada.", showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar" });
            if (!answer.isConfirmed || ticket !== sessionVersion) return;
            hidden("Id").value = String(row.id); hidden("ProductoId").value = String(row.productoId); hidden("Revision").value = row.revision;
            const result = await request(config.urls.guardar, { method: "POST", body: new FormData(form) }); closeEditor(); await load();
            void Swal.fire({ icon: "success", title: result.message, toast: true, position: "top-end", timer: 1800, showConfirmButton: false });
        } catch (error) { failure(error); } finally { busy = false; updateControls(); }
    });
})();
