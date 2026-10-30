(() => {
    "use strict";
    /** @param {string} id */
    function el(id) {
        const node = document.getElementById(id);
        if (!node) throw new Error("Falta el control " + id);
        return node;
    }
    /** @param {string} id */
    function input(id) {
        const node = el(id);
        if (!(node instanceof HTMLInputElement)) throw new Error("Control no válido: " + id);
        return node;
    }
    /** @param {string} id */
    function select(id) {
        const node = el(id);
        if (!(node instanceof HTMLSelectElement)) throw new Error("Control no válido: " + id);
        return node;
    }
    /** @param {string} id */
    function button(id) {
        const node = el(id);
        if (!(node instanceof HTMLButtonElement)) throw new Error("Control no válido: " + id);
        return node;
    }
    /** @param {string} id */
    function fieldset(id) {
        const node = el(id);
        if (!(node instanceof HTMLFieldSetElement)) throw new Error("Control no válido: " + id);
        return node;
    }
    function getForm() {
        const node = el("payrollForm");
        if (!(node instanceof HTMLFormElement)) throw new Error("Falta el formulario.");
        return node;
    }
    const form = getForm();
    const config = window.erpListings.config("payrollConfig");
    const modalElement = el("payrollModal");
    const modal = new bootstrap.Modal(modalElement);
    /** @typedef {{id:number, conceptoId:number, codigo:string, nombre:string, tipoNombre:string, orden:number}} Concept */
    /** @type {Concept[]} */
    let concepts = [];
    /** @type {"orden"|"codigo"|"nombre"|"tipoNombre"} */
    let sortKey = "orden";
    let ascending = true;
    let page = 1;
    let loaded = false;
    let editable = false;
    let busy = false;
    let changed = false;
    /** @param {string} url @param {RequestInit} options */
    async function request(url, options = {}) {
        const response = await fetch(url, { credentials: "same-origin", ...options });
        if (response.redirected || !(response.headers.get("content-type") || "").includes("application/json"))
            throw new Error("La sesión no está disponible. Recargue la página.");
        const result = await response.json();
        if (!response.ok || !result.success) throw new Error(result.message || "No se pudo completar la operación.");
        return result;
    }
    /** @param {unknown} error */
    function showError(error) {
        return Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Error inesperado." });
    }
    /** @param {string} url @param {FormData} data */
    function post(url, data) {
        const token = form.querySelector('input[name="__RequestVerificationToken"]');
        if (!(token instanceof HTMLInputElement)) throw new Error("Recargue la página antes de guardar.");
        data.set("__RequestVerificationToken", token.value);
        return request(url, { method: "POST", body: data });
    }
    function updateControls() {
        fieldset("payrollFields").disabled = busy || !editable;
        fieldset("conceptFields").disabled = busy || !editable || input("payrollId").value === "0";
        button("savePayroll").disabled = busy;
        button("savePayroll").classList.toggle("d-none", !editable);
        document.querySelectorAll(".js-edit-concept,.js-remove-concept").forEach(node => {
            if (node instanceof HTMLButtonElement) node.disabled = busy;
        });
    }
    function resetConcept() {
        input("conceptId").value = "0";
        select("payrollConcept").value = "";
        select("payrollConcept").disabled = false;
        input("conceptOrder").value = "0";
        button("saveConcept").textContent = "Agregar";
    }
    /** @param {string} message */
    function messageRow(message) {
        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 5;
        cell.className = "text-center text-muted py-4";
        cell.textContent = message;
        row.append(cell);
        el("conceptBody").replaceChildren(row);
        button("conceptPrevious").disabled = true;
        button("conceptNext").disabled = true;
        el("conceptCount").textContent = "";
    }
    function render() {
        if (!loaded) return;
        const term = input("conceptSearch").value.trim().toLocaleLowerCase("es");
        const rows = concepts.filter(item => [item.codigo, item.nombre, item.tipoNombre]
            .some(value => value.toLocaleLowerCase("es").includes(term)));
        rows.sort((a, b) => {
            const order = sortKey === "orden" ? a.orden - b.orden
                : String(a[sortKey]).localeCompare(String(b[sortKey]), "es", { numeric: true, sensitivity: "base" });
            return (ascending ? order : -order) || a.id - b.id;
        });
        const size = Number(select("conceptTop").value);
        const pages = Math.max(1, Math.ceil(rows.length / size));
        page = Math.max(1, Math.min(page, pages));
        const shown = rows.slice((page - 1) * size, page * size);
        const body = el("conceptBody");
        body.replaceChildren();
        shown.forEach(item => {
            const row = document.createElement("tr");
            [item.orden, item.codigo, item.nombre, item.tipoNombre].forEach(value => {
                const cell = document.createElement("td");
                cell.textContent = String(value);
                row.append(cell);
            });
            const options = document.createElement("td");
            options.className = "listing-options";
            if (editable) {
                const edit = document.createElement("button");
                edit.type = "button";
                edit.className = "btn btn-sm btn-outline-primary js-edit-concept me-1";
                edit.title = "Cambiar secuencia";
                edit.innerHTML = '<i class="bi bi-pencil-square"></i>';
                edit.addEventListener("click", () => {
                    input("conceptId").value = String(item.id);
                    const selector = select("payrollConcept");
                    if (!Array.from(selector.options).some(option => option.value === String(item.conceptoId)))
                        selector.add(new Option(item.codigo + " · " + item.nombre, String(item.conceptoId)));
                    selector.value = String(item.conceptoId);
                    selector.disabled = true;
                    input("conceptOrder").value = String(item.orden);
                    button("saveConcept").textContent = "Guardar secuencia";
                    input("conceptOrder").focus();
                });
                const remove = document.createElement("button");
                remove.type = "button";
                remove.className = "btn btn-sm btn-outline-danger js-remove-concept";
                remove.title = "Retirar concepto";
                remove.innerHTML = '<i class="bi bi-x-circle"></i>';
                remove.addEventListener("click", () => removeConcept(item));
                options.append(edit, remove);
            }
            row.append(options);
            body.append(row);
        });
        if (!shown.length) messageRow("No se encontraron conceptos.");
        const first = shown.length ? (page - 1) * size + 1 : 0;
        el("conceptCount").textContent = "Viendo " + first + "–" + (shown.length ? first + shown.length - 1 : 0) +
            " de " + rows.length + " conceptos · Página " + page + " de " + pages;
        button("conceptPrevious").disabled = page <= 1;
        button("conceptNext").disabled = page >= pages;
        updateControls();
    }
    async function loadConcepts() {
        loaded = false;
        messageRow("Cargando conceptos...");
        const url = new URL(config.conceptsUrl, window.location.href);
        url.searchParams.set("id", input("payrollId").value);
        try {
            const result = await request(url.toString());
            concepts = result.data;
            loaded = true;
            render();
        } catch (error) {
            concepts = [];
            messageRow("No se pudieron consultar los conceptos. Cierre y vuelva a abrir la planilla para intentarlo nuevamente.");
            throw error;
        }
    }
    function resetModal() {
        form.reset();
        input("payrollId").value = "0";
        concepts = [];
        loaded = false;
        page = 1;
        input("conceptSearch").value = "";
        select("conceptTop").value = "25";
        resetConcept();
        messageRow("Guarde la cabecera para habilitar los conceptos.");
    }
    document.getElementById("newPayroll")?.addEventListener("click", () => {
        if (busy) return;
        resetModal();
        editable = config.canWrite === true;
        el("payrollTitle").textContent = "Nueva planilla";
        el("payrollConceptHint").textContent = "Guarde la cabecera para habilitar los conceptos.";
        updateControls();
        modal.show();
    });
    window.erpListings.onClick(".js-open-payroll", async (control) => {
            if (busy) return;
            busy = true;
            control.disabled = true;
            try {
                const url = new URL(config.getUrl, window.location.href);
                url.searchParams.set("id", control.dataset.id || "");
                const result = await request(url.toString());
                resetModal();
                input("payrollId").value = String(result.data.id);
                input("payrollName").value = result.data.nombre;
                select("payrollWorker").value = String(result.data.tipoTrabajadorId);
                input("payrollProcesses").value = String(result.data.procesoAnual);
                editable = config.canWrite === true && result.data.estado === "A";
                el("payrollTitle").textContent = "Planilla #" + result.data.id;
                el("payrollConceptHint").textContent = editable
                    ? "Agregue conceptos y ajuste su secuencia de cálculo. Cada cambio se guarda con su botón."
                    : "Consulta de conceptos; esta planilla no admite modificaciones.";
                updateControls();
                modal.show();
                await loadConcepts();
            } catch (error) { await showError(error); }
            finally { busy = false; control.disabled = false; updateControls(); }
        });
    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (busy || !editable) return;
        if (![input("payrollName"), select("payrollWorker"), input("payrollProcesses")].every(node => node.reportValidity())) return;
        const data = new FormData(form);
        busy = true;
        updateControls();
        try {
            const result = await post(config.saveUrl, data);
            input("payrollId").value = String(result.id);
            changed = true;
            el("payrollTitle").textContent = "Planilla #" + result.id;
            el("payrollConceptHint").textContent = "Agregue conceptos y ajuste su secuencia de cálculo. Cada cambio se guarda con su botón.";
            await Swal.fire({ icon: "success", title: "Cabecera guardada", text: result.message });
            await loadConcepts();
        } catch (error) { await showError(error); }
        finally { busy = false; updateControls(); }
    });
    button("saveConcept").addEventListener("click", async () => {
        if (busy || !editable || input("payrollId").value === "0") return;
        if (!select("payrollConcept").value || !input("conceptOrder").value || !input("conceptOrder").reportValidity()) {
            await showError(new Error("Seleccione un concepto e ingrese una secuencia entera válida."));
            return;
        }
        const data = new FormData();
        data.set("Id", input("conceptId").value);
        data.set("PlanillaId", input("payrollId").value);
        data.set("ConceptoId", select("payrollConcept").value);
        data.set("Orden", input("conceptOrder").value);
        busy = true;
        updateControls();
        try {
            const result = await post(config.saveConceptUrl, data);
            changed = true;
            resetConcept();
            await Swal.fire({ icon: "success", title: "Concepto guardado", text: result.message });
            await loadConcepts();
        } catch (error) { await showError(error); }
        finally { busy = false; updateControls(); }
    });
    /** @param {Concept} item */
    async function removeConcept(item) {
        if (busy || !editable) return;
        const confirmation = await Swal.fire({ icon: "warning", title: "Retirar concepto", text: item.nombre,
            showCancelButton: true, confirmButtonText: "Retirar", cancelButtonText: "Cancelar" });
        if (!confirmation.isConfirmed) return;
        const data = new FormData();
        data.set("id", String(item.id));
        data.set("planillaId", input("payrollId").value);
        busy = true;
        updateControls();
        try {
            await post(config.cancelConceptUrl, data);
            changed = true;
            resetConcept();
            await loadConcepts();
        } catch (error) { await showError(error); }
        finally { busy = false; updateControls(); }
    }
    window.erpListings.onClick(".js-cancel-payroll", async (control) => {
            const confirmation = await Swal.fire({ icon: "warning", title: "Anular planilla", text: control.dataset.name,
                showCancelButton: true, confirmButtonText: "Anular", cancelButtonText: "Cancelar" });
            if (!confirmation.isConfirmed) return;
            control.disabled = true;
            try {
                const data = new FormData();
                data.set("id", control.dataset.id || "");
                const result = await post(config.cancelUrl, data);
                await Swal.fire({ icon: "success", title: "Planilla anulada", text: result.message });
                window.location.reload();
            } catch (error) { await showError(error); control.disabled = false; }
        });
    button("resetConcept").addEventListener("click", resetConcept);
    input("conceptSearch").addEventListener("input", () => { page = 1; render(); });
    select("conceptTop").addEventListener("change", () => { page = 1; render(); });
    button("conceptPrevious").addEventListener("click", () => { page--; render(); });
    button("conceptNext").addEventListener("click", () => { page++; render(); });
    document.querySelectorAll(".js-concept-sort").forEach(control => {
        if (!(control instanceof HTMLButtonElement)) return;
        control.addEventListener("click", () => {
            const key = control.dataset.sort;
            if (key !== "orden" && key !== "codigo" && key !== "nombre" && key !== "tipoNombre") return;
            ascending = sortKey === key ? !ascending : true;
            sortKey = key;
            document.querySelectorAll(".js-concept-sort").forEach(node => {
                if (!(node instanceof HTMLButtonElement)) return;
                const active = node.dataset.sort === key;
                node.closest("th")?.setAttribute("aria-sort", active ? ascending ? "ascending" : "descending" : "none");
                const icon = node.querySelector("i");
                if (icon) icon.className = "bi " + (active ? ascending ? "bi-sort-down text-primary" : "bi-sort-up text-primary" : "bi-arrow-down-up");
            });
            page = 1;
            render();
        });
    });
    modalElement.addEventListener("hide.bs.modal", event => { if (busy) event.preventDefault(); });
    modalElement.addEventListener("hidden.bs.modal", () => { if (changed) window.location.reload(); });
})();
