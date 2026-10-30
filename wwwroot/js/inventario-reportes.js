/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    document.querySelector("[data-print-report]")?.addEventListener("click", () => window.print());
    if (!document.getElementById("inventory-report-config")) return;
    /** @typedef {{id:number,sede:string,codigo:string,producto:string,anterior:number,ingreso:number,salida:number,reserva:number,saldo:number,costoAnterior:string,costo:string,importe:number}} ValueRow */
    /** @typedef {{kind:string,mes:number,urls:Record<string,string>,filtros?:Record<string,string>,valores?:ValueRow[]}} Config */
    /** @typedef {{id:number,codigo:string,nombre:string}} Lookup */
    const config = /** @type {Config} */ (/** @type {unknown} */ (window.erpListings.config("inventory-report-config")));
    /** @param {string} id */
    function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    /** @param {string} id */
    function input(id) { const value = node(id); if (!(value instanceof HTMLInputElement)) throw new Error("Control no válido: " + id); return value; }
    const modalNode = node("inventory-report-modal"), body = node("inventory-report-modal-body");
    const modal = new bootstrap.Modal(modalNode);
    let version = 0;
    let picker = { tipo: "producto", search: "", page: 1, sort: "nombre", dir: "asc" };
    const quantity = new Intl.NumberFormat("es-PE", { minimumFractionDigits: 4, maximumFractionDigits: 4 });
    /** @param {string} message */
    function status(message) { const value = document.createElement("p"); value.className = "text-muted py-3"; value.setAttribute("role", "status"); value.textContent = message; body.replaceChildren(value); }
    /** @param {string} title */
    function open(title) { ++version; node("inventory-report-modal-title").textContent = title; status("Cargando consulta…"); modal.show(); }
    /** @param {string} action @param {Record<string,string|number>} args */
    async function get(action, args) {
        const url = new URL(config.urls[action], window.location.origin);
        Object.entries(args).forEach(([key, value]) => url.searchParams.set(key, String(value)));
        const response = await fetch(url, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (response.redirected) throw new Error("Vuelva a seleccionar su contexto de trabajo en Perfil de acceso.");
        const result = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (!response.ok || !result?.success) throw new Error(result?.message || "No se pudo consultar la información.");
        return result.data;
    }
    /** @param {() => Promise<void>} action */
    async function request(action) {
        const pending = action();
        const current = version;
        try { await pending; }
        catch (error) {
            if (current !== version) return;
            const message = error instanceof Error ? error.message : "No se pudo completar la consulta.";
            status(message); await Swal.fire({ icon: "error", title: "No se pudo consultar", text: message });
        }
    }
    /** @param {string} label @param {string} value */
    function info(label, value) {
        const block = document.createElement("div"); block.className = "col-md-4";
        const caption = document.createElement("small"); caption.className = "text-muted d-block"; caption.textContent = label;
        const content = document.createElement("div"); content.className = "fw-semibold"; content.textContent = value;
        block.append(caption, content); return block;
    }
    /** @param {string[]} labels @param {Array<Array<string|number>>} data */
    function table(labels, data) {
        const wrapper = document.createElement("div"); wrapper.className = "table-responsive border rounded";
        const grid = document.createElement("table"); grid.className = "table table-sm table-striped table-hover align-middle mb-0";
        const head = grid.createTHead().insertRow();
        labels.forEach((label, index) => {
            const th = document.createElement("th"), button = document.createElement("button");
            button.type = "button"; button.className = "btn btn-sm text-body p-0"; button.dataset.reportLocalSort = String(index); button.textContent = label + " ";
            const icon = document.createElement("i"); icon.className = "bi bi-arrow-down-up text-muted"; icon.setAttribute("aria-hidden", "true"); button.append(icon); th.append(button); head.append(th);
        });
        const rows = grid.createTBody();
        data.forEach(values => {
            const row = rows.insertRow();
            values.forEach(value => { const cell = row.insertCell(); cell.textContent = typeof value === "number" ? quantity.format(value) : value; cell.dataset.sortValue = String(value); if (typeof value === "number") { cell.className = "text-end"; cell.dataset.numeric = "true"; } });
        });
        if (!data.length) { const cell = rows.insertRow().insertCell(); cell.colSpan = labels.length; cell.className = "text-center text-muted py-3"; cell.textContent = "No se encontraron registros."; }
        wrapper.append(grid); return wrapper;
    }
    async function loadPicker() {
        const current = ++version;
        status("Buscando registros…");
        /** @type {{items:Lookup[],page:number,pageCount:number,firstRecord:number,lastRecord:number,total:number,sort:string,dir:string}} */
        const result = await get("selector", picker);
        if (current !== version) return;
        const form = document.createElement("form"); form.className = "input-group input-group-sm mb-3";
        const search = document.createElement("input"); search.type = "search"; search.className = "form-control"; search.maxLength = 150; search.value = picker.search; search.setAttribute("aria-label", "Buscar en el selector"); search.placeholder = "Código, documento o nombre…";
        const submit = document.createElement("button"); submit.type = "submit"; submit.className = "btn btn-outline-primary"; submit.textContent = "Buscar";
        form.append(search, submit);
        form.addEventListener("submit", event => { event.preventDefault(); picker.search = search.value; picker.page = 1; void request(loadPicker); });
        const wrapper = document.createElement("div"); wrapper.className = "table-responsive border rounded";
        const grid = document.createElement("table"); grid.className = "table table-sm table-striped table-hover align-middle mb-0";
        const heading = grid.createTHead().insertRow();
        for (const [key, label] of [["codigo", "Código"], ["nombre", "Nombre"]]) {
            const th = document.createElement("th"), sort = document.createElement("button"); sort.type = "button"; sort.className = "btn btn-sm text-body p-0"; sort.textContent = label + " ";
            const icon = document.createElement("i"); icon.className = "bi " + (result.sort === key ? result.dir === "asc" ? "bi-sort-alpha-down text-primary" : "bi-sort-alpha-down-alt text-primary" : "bi-arrow-down-up text-muted"); sort.append(icon);
            th.setAttribute("aria-sort", result.sort === key ? result.dir === "asc" ? "ascending" : "descending" : "none");
            sort.addEventListener("click", () => { picker.dir = picker.sort === key && picker.dir === "asc" ? "desc" : "asc"; picker.sort = key; picker.page = 1; void request(loadPicker); }); th.append(sort); heading.append(th);
        }
        const last = document.createElement("th"); last.className = "text-center"; last.textContent = "Opciones"; heading.append(last);
        const rows = grid.createTBody();
        result.items.forEach(item => {
            const row = rows.insertRow(); row.insertCell().textContent = item.codigo; row.insertCell().textContent = item.nombre;
            const cell = row.insertCell(); cell.className = "text-center";
            const choose = document.createElement("button"); choose.type = "button"; choose.className = "btn btn-sm btn-outline-primary"; choose.textContent = "Seleccionar";
            choose.addEventListener("click", () => { input("report-" + picker.tipo + "-id").value = String(item.id); input("report-" + picker.tipo + "-name").value = item.codigo + " · " + item.nombre; modal.hide(); }); cell.append(choose);
        });
        if (!result.items.length) { const cell = rows.insertRow().insertCell(); cell.colSpan = 3; cell.className = "text-center text-muted py-3"; cell.textContent = "No se encontraron registros."; }
        const footer = document.createElement("div"); footer.className = "d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3 small";
        const summary = document.createElement("span"); summary.setAttribute("role", "status"); summary.textContent = `Viendo ${result.firstRecord}–${result.lastRecord} de ${result.total} registros`;
        const paging = document.createElement("div"); paging.className = "d-flex gap-2 align-items-center";
        for (const [label, delta] of [["Anterior", -1], ["Siguiente", 1]]) {
            const button = document.createElement("button"); button.type = "button"; button.className = "btn btn-sm btn-outline-secondary"; button.textContent = String(label);
            const offset = Number(delta); button.disabled = offset < 0 ? result.page <= 1 : result.page >= result.pageCount;
            button.addEventListener("click", () => { picker.page = result.page + offset; void request(loadPicker); }); paging.append(button);
        }
        footer.append(summary, paging); wrapper.append(grid); body.replaceChildren(form, wrapper, footer); search.focus();
    }
    function modes() {
        const mode = document.getElementById("report-mode");
        const variant = document.getElementById("report-variant");
        if (variant instanceof HTMLSelectElement) {
            const detail = variant.value === "detalle", matrix = variant.value === "almacenes";
            if (mode instanceof HTMLSelectElement) { if (!detail) mode.value = "mes"; mode.disabled = !detail; }
            document.querySelector("[data-unit-warehouse]")?.classList.toggle("d-none", !detail);
            document.querySelector("[data-unit-detail]")?.classList.toggle("d-none", !detail);
            document.querySelector("[data-unit-reserve]")?.classList.toggle("d-none", !matrix);
            const warehouse = document.getElementById("report-warehouse"), reserve = document.getElementById("report-reserve");
            if (warehouse instanceof HTMLSelectElement) { warehouse.disabled = !detail; warehouse.required = detail; }
            if (reserve instanceof HTMLSelectElement) reserve.disabled = !matrix;
        }
        const value = mode instanceof HTMLSelectElement ? mode.value : "mes";
        document.querySelector("[data-report-month]")?.classList.toggle("d-none", value !== "mes");
        document.querySelector("[data-report-date]")?.classList.toggle("d-none", value !== "dia");
        const month = document.getElementById("report-month"), date = document.getElementById("report-date");
        if (month instanceof HTMLSelectElement) month.disabled = value !== "mes";
        if (date instanceof HTMLInputElement) { date.disabled = value !== "dia"; date.required = value === "dia"; }
    }
    window.erpListings.onClick("[data-report-picker]", async button => {
        picker = { tipo: button.dataset.reportPicker || "producto", search: "", page: 1, sort: "nombre", dir: "asc" };
        open(picker.tipo === "producto" ? "Seleccionar producto" : "Seleccionar tercero"); await request(loadPicker);
    });
    window.erpListings.onClick("[data-report-clear]", button => { const type = button.dataset.reportClear; input("report-" + type + "-id").value = ""; input("report-" + type + "-name").value = ""; });
    window.erpListings.onClick(".js-report-saldo", async button => {
        open("Composición del saldo · " + (button.dataset.label || "Producto")); const current = version;
        await request(async () => {
            /** @type {Array<{almacen:string,estado:string,anterior:number,ingreso:number,salida:number,fisico:number,reserva:number,disponible:number}>} */
            const rows = await get("detalle", { ...config.filtros, productoId: Number(button.dataset.productId), mes: config.mes });
            if (current !== version) return;
            body.replaceChildren(table(["Almacén", "Anterior", "Ingresos", "Salidas", "Físico", "Reserva", "Disponible"], rows.map(row => [row.almacen + (row.estado === "A" ? "" : " (inactivo)"), row.anterior, row.ingreso, row.salida, row.fisico, row.reserva, row.disponible])));
        });
    });
    window.erpListings.onClick("[data-report-value]", button => {
        const row = config.valores?.find(item => item.id === Number(button.dataset.reportValue)); if (!row) return;
        open("Saldo valorizado · " + row.codigo);
        const content = document.createElement("div"); content.className = "row g-3 mb-4";
        content.append(info("Sede", row.sede), info("Producto", row.producto), info("Valor unitario anterior", row.costoAnterior), info("Valor unitario del saldo", row.costo));
        body.replaceChildren(content, table(["Anterior", "Ingresos", "Salidas", "Reserva", "Saldo disponible", "Importe"], [[row.anterior, row.ingreso, row.salida, row.reserva, row.saldo, row.importe]]));
    });
    window.erpListings.onClick(".js-report-movement", async button => {
        open("Detalle del movimiento"); const current = version;
        await request(async () => {
            const detail = await get("movimiento", { id: Number(button.dataset.id) }); if (current !== version) return;
            const header = document.createElement("div"); header.className = "row g-3 mb-4";
            header.append(info("Operación", detail.operacion), info("Fecha", String(detail.fecha).slice(0, 10)), info("Almacén", detail.almacen), info("Transacción", detail.transaccion), info("Tercero", detail.tercero), info("Guía / Documento", [detail.guia, detail.documento].filter(Boolean).join(" · ")));
            const observation = document.createElement("p"); observation.className = "small text-muted"; observation.textContent = detail.observacion;
            /** @type {Array<{codigo:string,producto:string,unidad:string,descripcion:string,cantidad:number,valorUnitario:number}>} */ const products = detail.productos;
            body.replaceChildren(header, observation, table(["Código", "Producto", "U.M.", "Detalle", "Cantidad", "Valor unit. ingreso"], products.map(item => [item.codigo, item.producto, item.unidad, item.descripcion, item.cantidad, item.valorUnitario])));
        });
    });
    modalNode.addEventListener("hidden.bs.modal", () => { ++version; });
    document.addEventListener("change", event => {
        if (!(event.target instanceof HTMLSelectElement)) return;
        if (event.target.id === "report-variant") { const reserve = document.getElementById("report-reserve"); if (reserve instanceof HTMLSelectElement) reserve.value = event.target.value === "almacenes" ? "true" : "false"; modes(); }
        if (event.target.id === "report-mode") modes();
    });
    document.addEventListener("erp:list-updated", modes);
    document.addEventListener("submit", event => {
        if (!(event.target instanceof HTMLFormElement) || event.target.id !== "inventory-report-filters") return;
        const mode = document.getElementById("report-mode");
        if (config.kind === "kardex" && mode instanceof HTMLSelectElement && mode.value === "anual" && !input("report-producto-id").value) {
            event.preventDefault(); void Swal.fire({ icon: "info", title: "Seleccione un producto para el Kardex anual." });
        }
    }, true);
    body.addEventListener("click", event => {
        const button = event.target instanceof Element ? event.target.closest("[data-report-local-sort]") : null;
        if (!(button instanceof HTMLButtonElement)) return;
        const grid = button.closest("table"), rows = grid?.tBodies[0]; if (!rows) return;
        const column = Number(button.dataset.reportLocalSort), ascending = button.dataset.direction !== "asc";
        grid?.querySelectorAll("[data-report-local-sort]").forEach(other => {
            if (other instanceof HTMLElement && other !== button) delete other.dataset.direction;
            other.closest("th")?.setAttribute("aria-sort", "none"); const icon = other.querySelector("i"); if (icon) icon.className = "bi bi-arrow-down-up text-muted";
        });
        button.dataset.direction = ascending ? "asc" : "desc"; button.closest("th")?.setAttribute("aria-sort", ascending ? "ascending" : "descending");
        const icon = button.querySelector("i"); if (icon) icon.className = "bi text-primary " + (ascending ? "bi-sort-alpha-down" : "bi-sort-alpha-down-alt");
        Array.from(rows.rows).filter(row => row.cells.length > 1).sort((a, b) => {
            const left = a.cells[column], right = b.cells[column];
            const result = left?.dataset.numeric && right?.dataset.numeric ? Number(left.dataset.sortValue) - Number(right.dataset.sortValue) : (left?.textContent || "").localeCompare(right?.textContent || "", "es", { numeric: true, sensitivity: "base" });
            return (ascending ? 1 : -1) * result;
        }).forEach(row => rows.append(row));
    });
    modes();
})();
