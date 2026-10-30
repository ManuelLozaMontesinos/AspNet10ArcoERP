/// <reference path="./erp-listings.d.ts" />
(() => {
    "use strict";
    /** @typedef {{campo:string,titulo:string,numero:boolean,fecha:boolean}} Column */
    /** @type {{columns:Column[]}} */ const config = window.erpListings.config("ventas-reporte-config");
    const modalNode = document.getElementById("vr-modal"), detail = document.getElementById("vr-detail");
    if (!modalNode || !detail) return;
    const modal = new bootstrap.Modal(modalNode);
    /** @param {string} value */
    function decimal(value) {
        const match = value.match(/^(-?)(\d+)(?:\.(\d+))?$/);
        if (!match) return value;
        return match[1] + new Intl.NumberFormat("es-PE").format(BigInt(match[2])) + "." + (match[3] || "").padEnd(4, "0");
    }
    window.erpListings.onClick(".js-vr-row", button => {
        try {
            /** @type {Record<string,string>} */ const row = JSON.parse(button.dataset.row || "{}");
            detail.replaceChildren();
            config.columns.forEach(column => {
                const label = document.createElement("dt"), value = document.createElement("dd");
                label.className = "col-sm-4 text-muted"; value.className = "col-sm-8 text-break"; label.textContent = column.titulo;
                const raw = row[column.campo] || "";
                value.textContent = column.numero ? decimal(raw) : column.fecha && /^\d{4}-\d{2}-\d{2}$/.test(raw) ? raw.split("-").reverse().join("/") : raw;
                detail.append(label, value);
            }); modal.show();
        } catch { void Swal.fire({ icon: "error", title: "No se pudo consultar el registro", text: "Actualice el listado e intente nuevamente." }); }
    });
    document.addEventListener("change", event => {
        const target = event.target;
        if (!(target instanceof HTMLSelectElement) || !["vr-mode", "vr-group"].includes(target.id)) return;
        const form = target.closest("form"); if (!(form instanceof HTMLFormElement)) return;
        if (target.id === "vr-mode") {
            form.querySelectorAll("[data-vr-month],[data-vr-dates]").forEach(el => {
                const active = el.hasAttribute("data-vr-month") ? target.value === "mes" : target.value === "rango";
                if (el instanceof HTMLElement) el.hidden = !active;
                el.querySelectorAll("input,select").forEach(control => { if (control instanceof HTMLInputElement || control instanceof HTMLSelectElement) control.disabled = !active; });
            });
        } else {
            const sort = form.elements.namedItem("sort"), dir = form.elements.namedItem("dir");
            if (sort instanceof HTMLInputElement) sort.value = target.value === "documento" ? "documento" : "codigo";
            if (dir instanceof HTMLInputElement) dir.value = "asc";
        }
        form.requestSubmit();
    });
})();
