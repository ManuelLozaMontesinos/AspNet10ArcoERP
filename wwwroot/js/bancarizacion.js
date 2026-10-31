/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{id:number,fecha:string,operacion:string,montoTexto:string,montoVisible:string,cuentaId:number,cuenta:string,estado:string,revision:string}} Row */
    /** @typedef {{id:number,nombre:string}} Account */
    /** @type {{canWrite:boolean,contextoId:number,urls:Record<string,string>}} */ const config = window.erpListings.config("bancarizacion-config");
    /** @param {string} id */ function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} id */ function input(id) { const value = node(id); if (!(value instanceof HTMLInputElement)) throw new Error("Campo inválido"); return value; }
    /** @param {string} id */ function select(id) { const value = node(id); if (!(value instanceof HTMLSelectElement)) throw new Error("Selector inválido"); return value; }
    /** @param {string} id */ function button(id) { const value = node(id); if (!(value instanceof HTMLButtonElement)) throw new Error("Botón inválido"); return value; }
    const formNode = node("bn-form"); if (!(formNode instanceof HTMLFormElement)) return;
    const form = formNode, modalNode = node("bn-modal"), modal = new bootstrap.Modal(modalNode), head = node("bn-head"), body = node("bn-rows"), editor = node("bn-editor"), search = input("bn-search"), size = select("bn-size"), date = input("bn-date"), operation = input("bn-operation"), amount = input("bn-amount"), account = select("bn-account");
    const add = document.getElementById("bn-new"), save = document.getElementById("bn-save");
    let docId = 0, consultaId = 0, writable = false, busy = false, loading = false, editorOpen = false, editorWritable = false, sessionVersion = 0, detailVersion = 0, page = 1, pages = 1, sort = "fecha", dir = "asc", today = "";
    /** @type {Account[]} */ let accounts = [];
    /** @type {Map<number,Row>} */ let rows = new Map();
    /** @type {Row|null} */ let editing = null;
    /** @param {string} name */ function hidden(name) { const value = form.elements.namedItem(name); if (!(value instanceof HTMLInputElement)) throw new Error("Falta el contexto del documento."); return value; }
    /** @param {unknown} error */ function failure(error) { void Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Intente nuevamente." }); }
    /** @param {string} raw */ function decimal(raw) { const m = raw.match(/^(-?)(\d+)(?:\.(\d+))?$/); return m ? m[1] + new Intl.NumberFormat("es-PE").format(BigInt(m[2])) + "." + (m[3] || "").padEnd(4, "0") : raw; }
    /** @param {string} raw */ function dateLabel(raw) { return raw.slice(0, 10).split("-").reverse().join("/"); }
    /** @param {string} url @param {RequestInit} [init] */ async function request(url, init) {
        const response = await fetch(url, { credentials: "same-origin", ...init, headers: { "X-Requested-With": "XMLHttpRequest", ...init?.headers } });
        const json = !response.redirected && response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !json?.success) throw new Error(json?.message || "La sesión no está disponible o no se pudo completar la operación."); return json;
    }
    function canWrite() { return writable && config.canWrite; }
    function updateControls() {
        const locked = busy || loading || editorOpen; search.disabled = locked; size.disabled = locked;
        button("bn-find").disabled = locked; button("bn-clear").disabled = locked; button("bn-prev").disabled = locked || page <= 1; button("bn-next").disabled = locked || page >= pages;
        button("bn-discard").disabled = busy; date.readOnly = busy || !editorWritable || editing !== null; operation.readOnly = busy || !editorWritable; amount.readOnly = busy || !editorWritable; account.disabled = !editorOpen || busy || !editorWritable;
        if (add instanceof HTMLButtonElement) { add.hidden = !canWrite(); add.disabled = locked || !accounts.length; }
        if (save instanceof HTMLButtonElement) { save.hidden = !editorWritable; save.disabled = busy; }
        body.querySelectorAll("button").forEach(control => { control.disabled = locked || control.dataset.bnAction !== "view" && !canWrite(); });
    }
    function closeEditor() { editing = null; editorOpen = false; editorWritable = false; editor.hidden = true; [date, operation, amount, account].forEach(control => { control.disabled = true; }); amount.value = ""; operation.value = ""; updateControls(); }
    /** @param {any} data */ function applyMetadata(data) {
        const doc = data.datos; docId = doc.id; consultaId = doc.consultaId; today = data.fechaNueva.slice(0, 10); accounts = data.cuentas;
        hidden("ComprobanteId").value = String(docId); hidden("ConsultaId").value = String(consultaId); hidden("DocumentoRevision").value = doc.revision;
        node("bn-document").textContent = doc.tipo + " · " + doc.documento; node("bn-site").textContent = data.sede;
        node("bn-mode").textContent = canWrite() ? "Administrar cobranzas" : "Solo consulta";
        const info = node("bn-info"); info.replaceChildren();
        /** @type {[string,string][]} */ const fields = [["Fecha", dateLabel(doc.fecha)], ["Cliente", doc.cliente], ["Identidad", doc.identidad], ["Pedido", doc.pedido], ["Vendedor", doc.vendedor], ["Moneda", doc.moneda], ["T/C", decimal(doc.tipoCambioTexto)], ["Lista de precios", doc.listaPrecio], ["Condición de pago", doc.condicionPago], ["Medio de pago", doc.medioPago], ["Neto", decimal(doc.netoTexto)], ["IGV", decimal(doc.igvTexto)], ["Total", decimal(doc.totalTexto)], ["Bancarizado", doc.bancarizado === "S" ? "Sí" : doc.bancarizado === "N" ? "No" : "Sin marcar"], ["Observación", doc.observacion]];
        fields.forEach(([title, value]) => { const dt = document.createElement("dt"), dd = document.createElement("dd"); dt.className = "col-sm-3 text-muted"; dd.className = "col-sm-9 text-break"; dt.textContent = title; dd.textContent = value; info.append(dt, dd); });
        node("bn-account-note").textContent = accounts.length ? "Las cuentas disponibles son las activas de la empresa y moneda del comprobante. La fecha se fija al crear la cobranza." : "No hay cuentas activas de la empresa en esta moneda. Configure una cuenta antes de agregar o editar cobranzas."; updateControls();
    }
    async function metadata() { const url = new URL(config.urls.obtener, location.origin); url.searchParams.set("id", String(docId)); url.searchParams.set("consultaId", String(consultaId)); applyMetadata((await request(url.toString())).data); }
    async function load() {
        const ticket = ++detailVersion, session = sessionVersion; loading = true; updateControls();
        try {
            const url = new URL(config.urls.cobranzas, location.origin); Object.entries({ comprobanteId: docId, consultaId, search: search.value, page, top: size.value, sort, dir }).forEach(([key, value]) => url.searchParams.set(key, String(value)));
            const result = await request(url.toString()); if (ticket !== detailVersion || session !== sessionVersion) return;
            /** @type {{items:Row[],page:number,pageCount:number,firstRecord:number,lastRecord:number,total:number,sort:string,dir:string}} */ const data = result.data;
            page = data.page; pages = data.pageCount; sort = data.sort; dir = data.dir; rows = new Map(data.items.map(row => [row.id, row])); render(data.items);
            node("bn-range").textContent = `Viendo ${data.firstRecord}–${data.lastRecord} de ${data.total} registros · Página ${page} de ${pages}`;
        } finally { if (ticket === detailVersion && session === sessionVersion) { loading = false; updateControls(); } }
    }
    /** @param {Row[]} items */ function render(items) {
        const columns = [["fecha", "Fecha"], ["operacion", "Operación"], ["monto", "Monto"], ["cuenta", "Cuenta bancaria"], ["estado", "Estado"]]; head.replaceChildren(); body.replaceChildren(); const heading = document.createElement("tr");
        columns.forEach(([key, title]) => { const th = document.createElement("th"), a = document.createElement("a"), icon = document.createElement("i"); th.setAttribute("aria-sort", key === sort ? dir === "asc" ? "ascending" : "descending" : "none"); a.href = "#"; a.dataset.bnSort = key; a.className = "text-decoration-none text-body d-inline-flex align-items-center gap-1"; a.textContent = title; icon.className = "bi " + (key === sort ? dir === "asc" ? "bi-arrow-up" : "bi-arrow-down" : "bi-arrow-down-up"); a.append(icon); th.append(a); if (key === "monto") th.className = "text-end"; heading.append(th); });
        const last = document.createElement("th"); last.textContent = "Opciones"; last.className = "listing-options"; heading.append(last); head.append(heading);
        items.forEach(row => { const tr = document.createElement("tr"); columns.forEach(([key]) => { const td = document.createElement("td"); if (key === "fecha") td.textContent = dateLabel(row.fecha); else if (key === "monto") { td.textContent = row.montoVisible; td.className = "text-end"; } else if (key === "estado") { const badge = document.createElement("span"); badge.className = row.estado === "A" ? "badge bg-success-subtle text-success" : "badge bg-secondary-subtle text-body-secondary"; badge.textContent = row.estado === "A" ? "Activo" : "Anulado"; td.append(badge); } else td.textContent = String(row[/** @type {keyof Row} */ (key)] ?? ""); tr.append(td); });
            const actions = document.createElement("td"); actions.className = "listing-options";
            /** @param {string} kind @param {string} title @param {string} icon */ function action(kind, title, icon) { const btn = document.createElement("button"), glyph = document.createElement("i"); btn.type = "button"; btn.className = "btn btn-sm " + (kind === "cancel" ? "btn-outline-danger" : "btn-outline-primary"); btn.dataset.bnAction = kind; btn.dataset.id = String(row.id); btn.title = title; btn.setAttribute("aria-label", title); glyph.className = "bi " + icon; btn.append(glyph); actions.append(btn); }
            action("view", "Consultar cobranza", "bi-eye"); if (canWrite() && row.estado === "A") { action("edit", "Editar cobranza", "bi-pencil-square"); action("cancel", "Anular cobranza", "bi-x-circle"); } tr.append(actions); body.append(tr);
        }); if (!items.length) { const tr = document.createElement("tr"), td = document.createElement("td"); td.colSpan = 6; td.className = "text-center text-muted py-4"; td.textContent = "No se encontraron cobranzas."; tr.append(td); body.append(tr); }
    }
    /** @param {Row|null} row @param {boolean} write */ function edit(row, write) {
        editing = row; editorOpen = true; editorWritable = write && canWrite() && (!row || row.estado === "A"); editor.hidden = false;
        node("bn-editor-title").textContent = !row ? "Agregar cobranza" : editorWritable ? "Editar cobranza" : "Consultar cobranza";
        [date, operation, amount].forEach(control => { control.disabled = false; }); account.replaceChildren(new Option("Seleccione una cuenta", "")); accounts.forEach(item => account.add(new Option(item.nombre, String(item.id))));
        if (row && !accounts.some(item => item.id === row.cuentaId) && !editorWritable) account.add(new Option(row.cuenta, String(row.cuentaId)));
        date.value = row ? row.fecha.slice(0, 10) : today; operation.value = row?.operacion || ""; amount.value = row?.montoTexto || ""; account.value = row ? String(row.cuentaId) : "";
        hidden("Id").value = row ? String(row.id) : "0"; hidden("Revision").value = row?.revision || ""; updateControls(); if (editorWritable) operation.focus();
    }
    /** @param {number} id @param {number} consulta @param {boolean} write */ async function open(id, consulta, write) {
        if (busy) return; const ticket = ++sessionVersion; detailVersion++; const url = new URL(config.urls.obtener, location.origin); url.searchParams.set("id", String(id)); url.searchParams.set("consultaId", String(consulta));
        const result = await request(url.toString()); if (ticket !== sessionVersion) return; form.reset(); writable = write && config.canWrite; hidden("ContextoId").value = String(config.contextoId); applyMetadata(result.data);
        search.value = ""; size.value = "25"; page = 1; pages = 1; sort = "fecha"; dir = "asc"; closeEditor(); rows.clear(); body.replaceChildren(); head.replaceChildren(); node("bn-range").textContent = "Cargando cobranzas…"; modal.show(); await load();
    }
    window.erpListings.onClick(".js-bn-open", control => { void open(Number(control.dataset.id), Number(control.dataset.consulta), control.dataset.edit === "true").catch(failure); });
    /** @param {string} action @param {Record<string,string>} values */ async function post(action, values) { const data = new FormData(); Object.entries(values).forEach(([key, value]) => data.set(key, value)); ["ContextoId", "ConsultaId", "ComprobanteId", "DocumentoRevision", "__RequestVerificationToken"].forEach(key => data.set(key, hidden(key).value)); return request(config.urls[action], { method: "POST", body: data }); }
    /** @param {Row} row */ async function cancel(row) {
        if (busy || loading || editorOpen || !canWrite()) return; busy = true; updateControls();
        try { const answer = await Swal.fire({ icon: "warning", title: "Anular cobranza", text: "Se anulará este registro y se actualizará el indicador de bancarización del comprobante.", showCancelButton: true, confirmButtonText: "Anular", cancelButtonText: "Cancelar" }); if (!answer.isConfirmed) return; const result = await post("anular", { Id: String(row.id), Revision: row.revision }); await metadata(); await load(); await window.erpListings.load(location.href, false); void Swal.fire({ icon: "success", title: result.message, toast: true, position: "top-end", timer: 1800, showConfirmButton: false }); }
        catch (error) { failure(error); } finally { busy = false; updateControls(); }
    }
    form.addEventListener("click", event => { const target = event.target instanceof Element ? event.target.closest("[data-bn-sort],[data-bn-action]") : null; if (!(target instanceof HTMLElement)) return; event.preventDefault(); if (busy || loading || editorOpen) return;
        if (target.dataset.bnSort) { const key = target.dataset.bnSort; dir = sort === key && dir === "asc" ? "desc" : "asc"; sort = key; page = 1; void load().catch(failure); }
        else { const row = rows.get(Number(target.dataset.id)); if (!row) return; if (target.dataset.bnAction === "cancel") void cancel(row); else edit(row, target.dataset.bnAction === "edit"); }
    });
    add?.addEventListener("click", () => { if (!busy && !loading && !editorOpen && canWrite() && accounts.length) edit(null, true); }); button("bn-discard").addEventListener("click", closeEditor);
    function refresh() { if (busy || loading || editorOpen) return; page = 1; void load().catch(failure); }
    size.addEventListener("change", refresh); button("bn-find").addEventListener("click", refresh); button("bn-clear").addEventListener("click", () => { search.value = ""; refresh(); });
    button("bn-prev").addEventListener("click", () => { if (busy || loading || editorOpen || page <= 1) return; page--; void load().catch(failure); }); button("bn-next").addEventListener("click", () => { if (busy || loading || editorOpen || page >= pages) return; page++; void load().catch(failure); });
    form.addEventListener("keydown", event => { if (event.key === "Enter" && event.target === search) { event.preventDefault(); refresh(); } }); modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); else { sessionVersion++; detailVersion++; loading = false; closeEditor(); } });
    form.addEventListener("submit", async event => { event.preventDefault(); if (busy || loading || !editorOpen || !editorWritable || !canWrite() || !form.reportValidity()) return;
        const data = new FormData(form); busy = true; updateControls();
        try { const answer = await Swal.fire({ icon: "question", title: "Guardar cobranza", text: "Se guardará la cobranza y se actualizará el indicador del comprobante.", showCancelButton: true, confirmButtonText: "Guardar", cancelButtonText: "Cancelar" }); if (!answer.isConfirmed) return; const result = await request(config.urls.guardar, { method: "POST", body: data }); closeEditor(); await metadata(); await load(); await window.erpListings.load(location.href, false); void Swal.fire({ icon: "success", title: result.message, toast: true, position: "top-end", timer: 1800, showConfirmButton: false }); }
        catch (error) { failure(error); } finally { busy = false; updateControls(); }
    });
})();
