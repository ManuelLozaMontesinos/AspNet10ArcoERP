(() => {
    "use strict";

    /** @param {string} id */
    function element(id) {
        const found = document.getElementById(id);
        if (!found) throw new Error("Falta el control " + id);
        return found;
    }
    /** @param {string} id */
    function input(id) {
        const found = element(id);
        if (!(found instanceof HTMLInputElement)) throw new Error("Control no válido: " + id);
        return found;
    }
    /** @param {string} id */
    function select(id) {
        const found = element(id);
        if (!(found instanceof HTMLSelectElement)) throw new Error("Control no válido: " + id);
        return found;
    }
    /** @param {string} id */
    function button(id) {
        const found = element(id);
        if (!(found instanceof HTMLButtonElement)) throw new Error("Control no válido: " + id);
        return found;
    }

    const config = window.erpListings.config("processConfig");
    const form = element("processForm");
    function getFields() {
        const found = element("processFields");
        if (!(found instanceof HTMLFieldSetElement)) throw new Error("Falta el formulario del proceso.");
        return found;
    }
    const fields = getFields();
    if (!(form instanceof HTMLFormElement)) return;
    const modalElement = element("processModal");
    const modal = new bootstrap.Modal(modalElement);
    const save = button("saveProcess");
    const load = button("loadDetails");
    const previous = button("detailPrevious");
    const next = button("detailNext");
    const currency = select("processCurrency");
    const exchange = input("processExchange");
    const from = input("processFrom");
    const to = input("processTo");
    const numberFormat = new Intl.NumberFormat("es-PE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    /** @typedef {{personalNombre:string, documentoIdentidad:string, personalCodigo:string,
     * concepRemuCodigo:string, concepRemuNombre:string, tipoConceptoNombre:string,
     * tipoConceptoCodigo:string, horas:number, montoSub:number, monto:number}} CalculationRow */
    /** @type {CalculationRow[]} */
    let rows = [];
    let detailPage = 1;
    let detailsLoaded = false;
    let editable = false;
    let saving = false;
    let opening = false;
    let generation = 0;
    let detailController = new AbortController();

    /** @param {string} url @param {RequestInit} options */
    async function request(url, options = {}) {
        const response = await fetch(url, { credentials: "same-origin", ...options });
        const type = response.headers.get("content-type") || "";
        if (response.redirected || !type.includes("application/json"))
            throw new Error("La sesión no está disponible o el servidor no pudo responder. Recargue la página.");
        const result = await response.json();
        if (!response.ok || !result.success)
            throw new Error(result.message || "No se pudo completar la operación.");
        return result;
    }
    /** @param {unknown} error */
    function showError(error) {
        return Swal.fire({ icon: "error", title: "No se pudo completar", text: error instanceof Error ? error.message : "Error inesperado." });
    }
    /** @param {string} url @param {string | undefined} id */
    function queryUrl(url, id) {
        if (!id) throw new Error("El proceso seleccionado no es válido.");
        const target = new URL(url, window.location.href);
        target.searchParams.set("id", String(id));
        return target.toString();
    }
    function setCurrency() {
        const local = String(config.localCurrencyId) === currency.value;
        exchange.readOnly = local || !editable;
        if (local && editable) exchange.value = "1";
    }
    /** @param {boolean} isNew @param {boolean} canEdit */
    function setMode(isNew, canEdit) {
        editable = canEdit;
        fields.disabled = !canEdit;
        select("processPlanilla").disabled = !isNew;
        select("processMonth").disabled = !isNew;
        select("processPeriodicity").disabled = !isNew;
        input("processNumber").readOnly = !isNew;
        save.classList.toggle("d-none", !canEdit);
        setCurrency();
    }
    /** @param {string} message */
    function detailMessage(message) {
        const body = element("detailBody");
        body.replaceChildren();
        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 6;
        cell.className = "text-center text-body-secondary py-4";
        cell.textContent = message;
        row.append(cell);
        body.append(row);
        previous.disabled = true;
        next.disabled = true;
    }
    function resetDetails() {
        detailController.abort();
        detailController = new AbortController();
        rows = [];
        detailsLoaded = false;
        detailPage = 1;
        input("detailSearch").value = "";
        select("detailSize").value = "25";
        element("detailCount").textContent = "";
        element("detailNet").textContent = "";
        load.disabled = false;
        detailMessage("Pulse Cargar resultados para consultar los cálculos existentes.");
    }
    function renderDetails() {
        if (!detailsLoaded) return;
        const term = input("detailSearch").value.trim().toLocaleLowerCase("es");
        const filtered = rows.filter(row => [row.personalNombre, row.documentoIdentidad, row.personalCodigo,
            row.concepRemuCodigo, row.concepRemuNombre].some(value => String(value || "").toLocaleLowerCase("es").includes(term)));
        const size = Number(select("detailSize").value);
        const pages = Math.max(1, Math.ceil(filtered.length / size));
        detailPage = Math.max(1, Math.min(detailPage, pages));
        const body = element("detailBody");
        body.replaceChildren();
        filtered.slice((detailPage - 1) * size, detailPage * size).forEach(row => {
            const tr = document.createElement("tr");
            const values = [row.personalNombre, row.concepRemuCodigo + " · " + row.concepRemuNombre,
                row.tipoConceptoNombre, numberFormat.format(row.horas), numberFormat.format(row.montoSub), numberFormat.format(row.monto)];
            values.forEach((value, index) => {
                const td = document.createElement("td");
                td.textContent = String(value ?? "");
                if (index >= 3) td.className = "text-end font-monospace";
                tr.append(td);
            });
            body.append(tr);
        });
        if (!filtered.length) detailMessage("No se encontraron resultados.");
        element("detailCount").textContent = filtered.length + " concepto(s) · Página " + detailPage + " de " + pages;
        previous.disabled = detailPage <= 1;
        next.disabled = detailPage >= pages;
        const net = rows.reduce((sum, row) => sum + (row.tipoConceptoCodigo === "01" ? Number(row.monto)
            : row.tipoConceptoCodigo === "03" ? -Number(row.monto) : 0), 0);
        element("detailNet").textContent = "Neto del proceso: " + numberFormat.format(net) + " · " +
            (currency.selectedOptions[0]?.textContent || "");
    }

    document.getElementById("createProcess")?.addEventListener("click", () => {
        if (opening || saving) return;
        generation++;
        form.reset();
        to.setCustomValidity("");
        input("processId").value = "0";
        currency.value = config.localCurrencyId ? String(config.localCurrencyId) : "";
        element("processTitle").textContent = "Nuevo proceso de planilla";
        element("processSubtitle").textContent = "Período " + config.year + " · Sede seleccionada";
        element("calculationSection").classList.add("d-none");
        resetDetails();
        setMode(true, config.canWrite === true);
        modal.show();
    });

    window.erpListings.onClick(".js-open-process", async (control) => {
            if (opening || saving) return;
            opening = true;
            control.disabled = true;
            const version = ++generation;
            try {
                const result = await request(queryUrl(config.getUrl, control.dataset.id));
                if (version !== generation) return;
                const item = result.data;
                form.reset();
                to.setCustomValidity("");
                input("processId").value = String(item.id);
                select("processPlanilla").value = String(item.planillaId);
                select("processMonth").value = String(item.mesId);
                select("processPeriodicity").value = String(item.periRemuId);
                input("processNumber").value = String(item.procAnual);
                currency.value = String(item.monedaId);
                exchange.value = String(item.montoTipoCambio);
                from.value = String(item.fechaDesde).slice(0, 10);
                to.value = String(item.fechaHasta).slice(0, 10);
                element("processTitle").textContent = "Proceso #" + item.id + " · " + item.planillaNombre;
                element("processSubtitle").textContent = "Período " + item.periodoAnio + " · " + item.estadoPlanProceNombre +
                    (item.estado !== "A" ? " · Anulado" : "");
                element("calculationSection").classList.remove("d-none");
                resetDetails();
                setMode(false, config.canWrite === true && item.estado === "A" && item.estadoPlanProceCodigo === "01");
                modal.show();
            } catch (error) {
                await showError(error);
            } finally {
                opening = false;
                control.disabled = false;
            }
        });

    load.addEventListener("click", async () => {
        detailController.abort();
        detailController = new AbortController();
        const version = generation;
        const signal = detailController.signal;
        load.disabled = true;
        detailsLoaded = false;
        detailMessage("Cargando resultados...");
        element("detailCount").textContent = "";
        element("detailNet").textContent = "";
        try {
            const result = await request(queryUrl(config.detailUrl, input("processId").value), { signal });
            if (version !== generation || signal.aborted) return;
            rows = result.data;
            detailsLoaded = true;
            detailPage = 1;
            renderDetails();
        } catch (error) {
            if (signal.aborted) return;
            rows = [];
            detailMessage("No se pudieron cargar los resultados. Puede volver a intentarlo.");
            await showError(error);
        } finally {
            if (version === generation) load.disabled = false;
        }
    });

    function validateDates() {
        to.setCustomValidity(from.value && to.value && from.value > to.value ? "La fecha de fin debe ser igual o posterior al inicio." : "");
    }
    from.addEventListener("change", validateDates);
    to.addEventListener("change", validateDates);
    currency.addEventListener("change", setCurrency);
    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (saving || !editable) return;
        validateDates();
        if (!form.reportValidity()) return;
        const data = new FormData(form);
        // Disabled identity selectors must still be validated against persisted values on the server.
        data.set("PlanillaId", select("processPlanilla").value);
        data.set("MesId", select("processMonth").value);
        data.set("PeriRemuId", select("processPeriodicity").value);
        saving = true;
        save.disabled = true;
        fields.disabled = true;
        try {
            const result = await request(config.saveUrl, { method: "POST", body: data });
            await Swal.fire({ icon: "success", title: "Guardado", text: result.message });
            window.location.reload();
        } catch (error) {
            await showError(error);
        } finally {
            saving = false;
            save.disabled = false;
            fields.disabled = !editable;
        }
    });

    window.erpListings.onClick(".js-cancel-process", async (control) => {
            const answer = await Swal.fire({ icon: "warning", title: "Anular proceso",
                text: "El proceso quedará inactivo. Esta pantalla no permite reactivarlo.",
                showCancelButton: true, confirmButtonText: "Anular", cancelButtonText: "Cancelar" });
            if (!answer.isConfirmed) return;
            control.disabled = true;
            try {
                const data = new FormData();
                data.set("id", control.dataset.id || "");
                const token = form.querySelector('input[name="__RequestVerificationToken"]');
                if (!(token instanceof HTMLInputElement)) throw new Error("Recargue la página antes de continuar.");
                data.set("__RequestVerificationToken", token.value);
                const result = await request(config.cancelUrl, { method: "POST", body: data });
                await Swal.fire({ icon: "success", title: "Proceso anulado", text: result.message });
                window.location.reload();
            } catch (error) {
                await showError(error);
                control.disabled = false;
            }
        });

    input("detailSearch").addEventListener("input", () => { detailPage = 1; renderDetails(); });
    select("detailSize").addEventListener("change", () => { detailPage = 1; renderDetails(); });
    previous.addEventListener("click", () => { detailPage--; renderDetails(); });
    next.addEventListener("click", () => { detailPage++; renderDetails(); });
    modalElement.addEventListener("hide.bs.modal", event => { if (saving) event.preventDefault(); });
    modalElement.addEventListener("hidden.bs.modal", () => { generation++; detailController.abort(); });
})();
