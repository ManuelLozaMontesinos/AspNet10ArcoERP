/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    if (!document.getElementById("minimum-config")) return;
    /** @typedef {{contextoId:number,sedeId:number|null,sede:string,anio:number,write:boolean,urls:Record<string,string>}} Config */
    /** @typedef {{productoId:number,codigo:string,producto:string,stockMinimoTexto:string,anteriorTexto:string,revision:string,fila:number}} ImportRow */
    /** @typedef {{contextoId:number,empresaSedeId:number,sede:string,anio:number,filas:ImportRow[],errores:string[]}} Preview */
    const config = /** @type {Config} */ (/** @type {unknown} */ (window.erpListings.config("minimum-config")));
    /** @param {string} id */
    function node(id) { const value = document.getElementById(id); if (!value) throw new Error("Falta el control " + id); return value; }
    const modalNode = node("minimum-modal"), body = node("minimum-body"), form = node("minimum-form");
    if (!(form instanceof HTMLFormElement)) throw new Error("Falta el formulario de mínimos.");
    const saveNode = document.getElementById("minimum-save"), save = saveNode instanceof HTMLButtonElement ? saveNode : null;
    const modal = new bootstrap.Modal(modalNode);
    /** @type {Preview|null} */ let preview = null;
    /** @type {{productoId:number,revision:string}|null} */ let edited = null;
    let version = 0, busy = false;
    /** @param {string} text */
    function status(text) { const p = document.createElement("p"); p.className = "text-muted py-3"; p.setAttribute("role", "status"); p.textContent = text; body.replaceChildren(p); }
    /** @param {string} title */
    function open(title) { ++version; preview = null; edited = null; save?.classList.add("d-none"); node("minimum-title").textContent = title; status("Cargando…"); modal.show(); }
    /** @param {unknown} error */
    function message(error) { return error instanceof Error ? error.message : "No se pudo completar la operación."; }
    /** @param {Response} response @param {boolean} [withErrors] */
    async function result(response, withErrors = false) {
        const value = response.headers.get("content-type")?.includes("application/json") ? await response.json() : null;
        if (response.redirected || !response.ok || (!value?.success && !(withErrors && value?.data))) throw new Error(value?.message || "No se pudo completar la consulta. Revise su sesión y permisos.");
        return value;
    }
    /** @param {string} label @param {string|number} value */
    function info(label, value) { const div = document.createElement("div"); div.className = "col-md-4"; const small = document.createElement("small"); small.className = "text-muted d-block"; small.textContent = label; const strong = document.createElement("strong"); strong.textContent = String(value); div.append(small, strong); return div; }
    /** @param {string} raw */
    function decimalText(raw) { const [integer, fraction = ""] = raw.split("."); return integer.replace(/\B(?=(\d{3})+(?!\d))/g, ",") + "." + fraction.padEnd(4, "0"); }
    /** @param {string} left @param {string} right */
    function decimalOrder(left, right) {
        const negative = left.startsWith("-"), otherNegative = right.startsWith("-");
        if (negative !== otherNegative) return negative ? -1 : 1;
        const [a, af = ""] = left.replace(/^-/, "").split("."), [b, bf = ""] = right.replace(/^-/, "").split(".");
        const integer = a.replace(/^0+/, "") || "0", other = b.replace(/^0+/, "") || "0";
        const comparison = integer.length - other.length || integer.localeCompare(other) || af.padEnd(4, "0").localeCompare(bf.padEnd(4, "0"));
        return negative ? -comparison : comparison;
    }
    window.erpListings.onClick("[data-minimum-product]", async button => {
        if (busy || !config.sedeId) return;
        open(config.write ? "Editar stock mínimo" : "Consultar stock mínimo"); const current = version;
        try {
            const url = new URL(config.urls.obtener, window.location.origin); url.searchParams.set("sedeId", String(config.sedeId)); url.searchParams.set("productoId", button.dataset.minimumProduct || "");
            const value = await result(await fetch(url, { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } })); if (current !== version) return;
            const row = value.data; if (row.contextoId !== config.contextoId) throw new Error("Cambió el contexto de trabajo. Actualice el listado antes de editar.");
            const header = document.createElement("div"); header.className = "row g-3 mb-4"; header.append(info("Sede", config.sede), info("Ejercicio del catálogo", config.anio), info("Producto", row.codigo + " · " + row.producto));
            const label = document.createElement("label"); label.htmlFor = "minimum-value"; label.className = "form-label"; label.textContent = "Stock mínimo";
            const input = document.createElement("input"); input.type = "number"; input.id = "minimum-value"; input.className = "form-control"; input.min = "0"; input.max = "999999999999999.9999"; input.step = "0.0001"; input.required = true; input.value = row.stockMinimoTexto; input.readOnly = !config.write;
            body.replaceChildren(header, label, input); edited = { productoId: row.productoId, revision: row.revision };
            if (config.write) { save?.classList.remove("d-none"); input.focus(); }
        } catch (error) { if (current !== version) return; status(message(error)); await Swal.fire({ icon: "error", title: "No se pudo consultar", text: message(error) }); }
    });
    /** @param {HTMLElement} container @param {Preview} data */
    function previewGrid(container, data) {
        let page = 1, size = 25, column = "fila", direction = 1;
        function render() {
            const bar = document.createElement("div"); bar.className = "d-flex align-items-center justify-content-between gap-2 my-3 small";
            const summary = document.createElement("span"); summary.setAttribute("role", "status");
            const count = document.createElement("select"); count.className = "form-select form-select-sm w-auto"; count.setAttribute("aria-label", "Filas por página de la vista previa");
            [25, 50, 100, 500].forEach(value => { const option = document.createElement("option"); option.value = String(value); option.textContent = value + " registros"; option.selected = value === size; count.append(option); });
            count.addEventListener("change", () => { size = Number(count.value); page = 1; render(); }); bar.append(summary, count);
            const wrapper = document.createElement("div"); wrapper.className = "table-responsive border rounded";
            const grid = document.createElement("table"); grid.className = "table table-sm table-striped table-hover align-middle mb-0";
            const head = grid.createTHead().insertRow();
            for (const [key, text] of [["fila", "Fila"], ["codigo", "Código"], ["producto", "Producto"], ["anteriorTexto", "Mínimo actual"], ["stockMinimoTexto", "Mínimo del archivo"]]) {
                const th = document.createElement("th"), button = document.createElement("button"); button.type = "button"; button.className = "btn btn-sm text-body p-0"; button.textContent = text + " ";
                const icon = document.createElement("i"); icon.className = "bi " + (key === column ? direction === 1 ? "bi-sort-alpha-down text-primary" : "bi-sort-alpha-down-alt text-primary" : "bi-arrow-down-up text-muted"); button.append(icon); th.append(button); th.setAttribute("aria-sort", key === column ? direction === 1 ? "ascending" : "descending" : "none"); head.append(th);
                button.addEventListener("click", () => { direction = column === key ? -direction : 1; column = key; page = 1; render(); });
            }
            const rows = [...data.filas].sort((a, b) => {
                if (column === "fila") return direction * (a.fila - b.fila);
                const left = column === "codigo" ? a.codigo : column === "producto" ? a.producto : column === "anteriorTexto" ? a.anteriorTexto : a.stockMinimoTexto;
                const right = column === "codigo" ? b.codigo : column === "producto" ? b.producto : column === "anteriorTexto" ? b.anteriorTexto : b.stockMinimoTexto;
                return direction * (column.endsWith("Texto") ? decimalOrder(left, right) : left.localeCompare(right, "es", { numeric: true, sensitivity: "base" })) || a.fila - b.fila;
            });
            const tbody = grid.createTBody(), start = (page - 1) * size;
            rows.slice(start, start + size).forEach(item => { const row = tbody.insertRow(); [String(item.fila), item.codigo, item.producto, decimalText(item.anteriorTexto), decimalText(item.stockMinimoTexto)].forEach((text, index) => { const cell = row.insertCell(); cell.textContent = text; if (index > 2) cell.className = "text-end"; }); });
            summary.textContent = `Viendo ${rows.length ? start + 1 : 0}–${Math.min(start + size, rows.length)} de ${rows.length} registros`;
            const paging = document.createElement("div"); paging.className = "d-flex justify-content-end gap-2 mt-3";
            for (const [label, delta] of [["Anterior", -1], ["Siguiente", 1]]) { const button = document.createElement("button"); button.type = "button"; button.textContent = String(label); button.className = "btn btn-sm btn-outline-secondary"; const offset = Number(delta); button.disabled = offset < 0 ? page <= 1 : start + size >= rows.length; button.addEventListener("click", () => { page += offset; render(); }); paging.append(button); }
            wrapper.append(grid); container.replaceChildren(bar, wrapper, paging);
        }
        render();
    }
    window.erpListings.onClick("#minimum-import", () => {
        if (busy || !config.write || !config.sedeId) return;
        open("Importar stocks mínimos");
        const header = document.createElement("div"); header.className = "row g-3 mb-3"; header.append(info("Sede", config.sede), info("Ejercicio del catálogo", config.anio));
        const note = document.createElement("p"); note.className = "small text-muted"; note.textContent = "Use la plantilla o el Excel exportado. Primera hoja con las seis columnas originales; hasta 5.000 filas y 10 MB. Sin fórmulas. Stock no negativo con hasta cuatro decimales. Corrija todos los errores antes de confirmar.";
        const label = document.createElement("label"); label.className = "form-label"; label.htmlFor = "minimum-file"; label.textContent = "Archivo XLSX";
        const file = document.createElement("input"); file.type = "file"; file.id = "minimum-file"; file.accept = ".xlsx"; file.className = "form-control mb-3";
        const prepare = document.createElement("button"); prepare.type = "button"; prepare.className = "btn btn-sm btn-outline-primary mb-3"; prepare.textContent = "Validar y mostrar vista previa";
        const panel = document.createElement("div"); panel.setAttribute("role", "status");
        file.addEventListener("change", () => { preview = null; save?.classList.add("d-none"); panel.replaceChildren(); });
        prepare.addEventListener("click", async () => {
            const upload = file.files?.[0]; if (busy) return;
            if (!upload || !upload.name.toLowerCase().endsWith(".xlsx") || upload.size > 10 * 1024 * 1024) { await Swal.fire({ icon: "info", title: "Seleccione un archivo XLSX de hasta 10 MB." }); return; }
            const current = version; busy = true; prepare.disabled = true; file.disabled = true; preview = null; save?.classList.add("d-none");
            try {
                const data = new FormData(form); data.set("sedeId", String(config.sedeId)); data.set("archivo", upload);
                const value = await result(await fetch(config.urls.preparar, { method: "POST", body: data, credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } }), true); if (current !== version) return;
                const prepared = /** @type {Preview} */ (value.data);
                if (prepared.contextoId !== config.contextoId) throw new Error("Cambió el contexto de trabajo. Actualice el listado y prepare el archivo nuevamente.");
                panel.replaceChildren();
                if (prepared.errores.length) { const alert = document.createElement("div"); alert.className = "alert alert-warning"; const title = document.createElement("strong"); title.textContent = prepared.errores.length + " errores. El archivo no se aplicará."; const list = document.createElement("ul"); list.className = "mb-0 mt-2"; prepared.errores.forEach(text => { const li = document.createElement("li"); li.textContent = text; list.append(li); }); alert.append(title, list); panel.append(alert); }
                if (prepared.filas.length) { const grid = document.createElement("div"); panel.append(grid); previewGrid(grid, prepared); }
                if (!prepared.errores.length && prepared.filas.length) { preview = prepared; save?.classList.remove("d-none"); }
            } catch (error) { if (current === version) await Swal.fire({ icon: "error", title: "No se pudo validar", text: message(error) }); }
            finally { busy = false; prepare.disabled = false; file.disabled = false; }
        });
        body.replaceChildren(header, note, label, file, prepare, panel);
    });
    form.addEventListener("submit", async event => {
        event.preventDefault(); if (busy || !config.write || !config.sedeId || !save) return;
        const data = new FormData(form); let url = config.urls.guardar;
        if (preview) {
            data.set("contextoId", String(preview.contextoId)); data.set("sedeId", String(preview.empresaSedeId));
            data.set("cambiosJson", JSON.stringify(preview.filas.map(row => ({ productoId: row.productoId, stockMinimo: row.stockMinimoTexto, revision: row.revision })))); url = config.urls.confirmar;
        } else if (edited) {
            const input = document.getElementById("minimum-value"); if (!(input instanceof HTMLInputElement) || !input.reportValidity()) return;
            data.set("ContextoId", String(config.contextoId)); data.set("EmpresaSedeId", String(config.sedeId)); data.set("Cambios[0].ProductoId", String(edited.productoId)); data.set("Cambios[0].StockMinimo", input.value); data.set("Cambios[0].Revision", edited.revision);
        } else return;
        busy = true; save.disabled = true;
        try {
            const confirmation = await Swal.fire({ icon: "question", title: preview ? "Aplicar importación de mínimos" : "Guardar stock mínimo", text: `${config.sede} · ${preview ? preview.filas.length + " productos" : "Producto seleccionado"}`, showCancelButton: true, confirmButtonText: "Confirmar", cancelButtonText: "Cancelar" });
            if (!confirmation.isConfirmed) return;
            const value = await result(await fetch(url, { method: "POST", body: data, credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } }));
            busy = false; modal.hide(); await window.erpListings.load(window.location.href, false); await Swal.fire({ icon: "success", title: "Mínimos actualizados", text: value.message });
        } catch (error) { preview = null; edited = null; save.classList.add("d-none"); status(message(error) + " Vuelva a consultar o preparar el archivo antes de confirmar."); await Swal.fire({ icon: "error", title: "No se pudo guardar", text: message(error) }); }
        finally { busy = false; save.disabled = false; }
    });
    modalNode.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); });
    modalNode.addEventListener("hidden.bs.modal", () => { ++version; preview = null; edited = null; });
})();
