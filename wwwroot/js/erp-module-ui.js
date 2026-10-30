/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    if (!document.body.classList.contains("erp-module")) return;
    const collator = new Intl.Collator("es", { numeric: true, sensitivity: "base" });
    /** @type {Map<string, {column:number, ascending:boolean, numeric:boolean}>} */
    const orders = new Map();
    /** @type {WeakSet<Element>} */
    const observed = new WeakSet();
    let scheduled = false;

    function root() {
        return document.querySelector("main.app-main .app-content");
    }
    /** @param {HTMLTableElement} table */
    function rows(table) {
        return Array.from(table.tBodies[0]?.rows || []).filter(row =>
            row.cells.length > 1 && !Array.from(row.cells).some(cell => cell.colSpan > 1));
    }
    /** @param {Element} node @param {number} value */
    function countText(node, value) {
        const next = String(value);
        if (node.textContent !== next) node.textContent = next;
    }
    function summarize() {
        const table = root()?.querySelector("table[data-erp-grid]");
        if (!(table instanceof HTMLTableElement)) return;
        const summary = table.closest(".card")?.querySelector("[data-erp-local-summary]");
        if (!summary) return;
        const all = rows(table);
        const visible = all.filter(row => !row.hidden && row.style.display !== "none" && !row.classList.contains("d-none")).length;
        const shown = summary.querySelector("[data-erp-visible]");
        const loaded = summary.querySelector("[data-erp-loaded]");
        if (shown) countText(shown, visible);
        if (loaded) countText(loaded, all.length);
    }
    function scheduleSummary() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(() => { scheduled = false; summarize(); });
    }
    function applyFilters() {
        if (typeof window.aplicarFiltros === "function") window.aplicarFiltros();
        else if (typeof window.aplicarFiltrosLocal === "function") window.aplicarFiltrosLocal();
        else if (typeof window.filtrarTablaCliente === "function") window.filtrarTablaCliente();
    }
    /** @param {HTMLTableCellElement} header */
    function numeric(header) {
        const inline = header.getAttribute("onclick") || "";
        return /,\s*true\s*\)/.test(inline) ||
            /^(#|ID|N[º°.]|Secuencia|Días|Horas|Cantidad|Compra|Venta|Importe|Monto|Saldo|Total|T\/C|Procesos)/i.test(header.textContent?.trim() || "");
    }
    /** @param {string} value @param {boolean} isNumeric */
    function sortValue(value, isNumeric) {
        const text = value.trim();
        const date = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})/);
        if (date) return Number(date[3]) * 10000 + Number(date[2]) * 100 + Number(date[1]);
        if (!isNumeric) return text;
        let number = text.replace(/[^\d,.\-]/g, "");
        if (number.includes(",") && number.includes(".")) {
            number = number.lastIndexOf(",") > number.lastIndexOf(".")
                ? number.replace(/\./g, "").replace(",", ".") : number.replace(/,/g, "");
        } else if (/^-?\d{1,3}(,\d{3})+$/.test(number)) number = number.replace(/,/g, "");
        else number = number.replace(",", ".");
        return number !== "" && Number.isFinite(Number(number)) ? Number(number) : text;
    }
    /** @param {HTMLTableElement} table @param {{column:number, ascending:boolean, numeric:boolean}} order */
    function indicators(table, order) {
        const headers = Array.from(table.tHead?.rows[0]?.cells || []);
        headers.forEach((header, index) => {
            if (!header.hasAttribute("onclick") && !header.querySelector("[data-erp-sort-index]")) return;
            const active = order.column === index;
            header.setAttribute("aria-sort", active ? order.ascending ? "ascending" : "descending" : "none");
            const icon = header.querySelector(".sort-icon") || header.querySelector("i.bi");
            if (icon) icon.className = "bi ms-1 sort-icon " + (!active ? "bi-arrow-down-up text-muted opacity-50" :
                order.numeric ? order.ascending ? "bi-sort-numeric-down text-primary" : "bi-sort-numeric-down-alt text-primary" :
                order.ascending ? "bi-sort-alpha-down text-primary" : "bi-sort-alpha-down-alt text-primary");
        });
    }
    /** @param {HTMLTableElement} table @param {{column:number, ascending:boolean, numeric:boolean}} order */
    function sort(table, order) {
        const body = table.tBodies[0];
        if (!body) return;
        rows(table).map((row, index) => ({ row, index })).sort((left, right) => {
            const aCell = left.row.cells[order.column], bCell = right.row.cells[order.column];
            const a = sortValue(aCell?.dataset.sortValue ?? aCell?.textContent ?? "", order.numeric);
            const b = sortValue(bCell?.dataset.sortValue ?? bCell?.textContent ?? "", order.numeric);
            const result = typeof a === "number" && typeof b === "number" ? a - b : collator.compare(String(a), String(b));
            return (order.ascending ? result : -result) || left.index - right.index;
        }).forEach(item => body.append(item.row));
        indicators(table, order);
        applyFilters();
        summarize();
    }
    function selectedSize() {
        const value = Number(new URL(window.location.href).searchParams.get("top"));
        return [25, 50, 100, 500].includes(value) ? value : 25;
    }
    function sizeMarkers() {
        const scope = root();
        const size = selectedSize();
        scope?.querySelectorAll(".item-top[onclick]").forEach(item => {
            const candidate = Number(item.getAttribute("onclick")?.match(/cambiarTop\(\s*(\d+)/)?.[1]);
            item.classList.toggle("active", candidate === size);
        });
    }
    function initialize() {
        const scope = root();
        if (!scope) return;
        const table = scope.querySelector("table[data-erp-grid]");
        if (table instanceof HTMLTableElement) {
            table.tHead?.querySelectorAll("th[onclick]").forEach(header => {
                header.setAttribute("tabindex", "0");
                header.setAttribute("role", "button");
                if (!header.hasAttribute("aria-sort")) header.setAttribute("aria-sort", "none");
            });
            if (typeof window.cambiarTop === "function" && scope.querySelector(".item-top[onclick]"))
                window.cambiarTop(selectedSize());
            sizeMarkers();
            const order = orders.get(table.id || "main");
            if (order) sort(table, order);
        }
        if (!observed.has(scope)) {
            new MutationObserver(scheduleSummary).observe(scope, {
                subtree: true, childList: true, attributes: true, attributeFilter: ["class", "style", "hidden"]
            });
            observed.add(scope);
        }
        summarize();
    }
    document.addEventListener("click", event => {
        if (!(event.target instanceof Element)) return;
        const header = event.target.closest("th");
        const table = header?.closest("table[data-erp-grid]");
        if (header instanceof HTMLTableCellElement && table instanceof HTMLTableElement &&
            (header.hasAttribute("onclick") || event.target.closest("[data-erp-sort-index]"))) {
            const key = table.id || "main";
            const previous = orders.get(key);
            const order = { column: header.cellIndex,
                ascending: !(previous?.column === header.cellIndex && previous.ascending), numeric: numeric(header) };
            orders.set(key, order);
            event.preventDefault();
            event.stopImmediatePropagation();
            sort(table, order);
        }
        const option = event.target.closest(".item-top[onclick]");
        if (!option || !root()?.contains(option) || option.closest(".modal") || event.ctrlKey || event.metaKey) return;
        const size = Number(option.getAttribute("onclick")?.match(/cambiarTop\(\s*(25|50|100|500)\s*\)/)?.[1]);
        if (!size) return;
        const snapshot = Array.from(root()?.querySelectorAll('input[id^="filtro-"],select[id^="filtro-"]') || [])
            .filter(node => !node.closest(".modal") && (node instanceof HTMLInputElement || node instanceof HTMLSelectElement))
            .map(node => ({ id: node.id, value: /** @type {HTMLInputElement | HTMLSelectElement} */ (node).value }));
        const url = new URL(window.location.href);
        url.searchParams.set("top", String(size));
        url.searchParams.set("page", "1");
        void window.erpListings.load(url.toString()).then(() => {
            snapshot.forEach(item => {
                const control = document.getElementById(item.id);
                if (control instanceof HTMLInputElement || control instanceof HTMLSelectElement) control.value = item.value;
            });
            applyFilters();
            summarize();
        });
    }, true);
    document.addEventListener("keydown", event => {
        if (!(event.target instanceof HTMLTableCellElement) || !event.target.matches("table[data-erp-grid] th[onclick]")) return;
        if (event.key === "Enter" || event.key === " ") { event.preventDefault(); event.target.click(); }
    });
    document.addEventListener("erp:list-updated", initialize);
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initialize);
    else initialize();
})();
