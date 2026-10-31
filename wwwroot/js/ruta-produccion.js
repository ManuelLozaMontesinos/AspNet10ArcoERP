(() => {
    "use strict";

    const configEl = document.getElementById("ruta-config");
    if (!configEl) return;
    const config = JSON.parse(configEl.textContent || "{}");

    const modalEl = document.getElementById("rp-modal");
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const form = document.getElementById("rp-form");
    const nameInput = document.getElementById("rp-name");
    const idInput = form.querySelector('input[name="Id"]');
    const revInput = form.querySelector('input[name="Revision"]');
    const saveBtn = document.getElementById("rp-save");
    const noteEl = document.getElementById("rp-note");
    const panelEl = document.getElementById("rp-panel");
    const countBadge = document.getElementById("rp-sub-count");
    const subSelect = document.getElementById("rp-sub-select");
    const addSubBtn = document.getElementById("rp-add-sub");
    const subsBody = document.getElementById("rp-subs-body");

    let currentRouteId = 0;
    let isReadOnly = false;
    let currentSubprocesos = [];

    // Helper: AntiForgeryToken
    function getCsrfToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    // Helper: Fetch POST JSON
    async function postJson(url, data) {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify(data)
        });
        return await response.json();
    }

    // Modal: Abrir en modo nuevo
    document.getElementById("rp-new")?.addEventListener("click", () => {
        currentRouteId = 0;
        isReadOnly = false;
        form.reset();
        idInput.value = "0";
        revInput.value = "";
        nameInput.disabled = false;
        if (saveBtn) saveBtn.hidden = false;

        document.getElementById("rp-title").innerHTML = '<i class="bi bi-signpost-split me-2 text-primary"></i>Nueva ruta de producción';
        noteEl.hidden = false;
        panelEl.hidden = true;
        countBadge.textContent = "0 subprocesos";
        subsBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No hay subprocesos en la secuencia.</td></tr>';
        modal.show();
    });

    // Modal: Abrir en modo consulta o edición
    async function openModal(id, readOnly = false) {
        currentRouteId = id;
        isReadOnly = readOnly;
        form.reset();

        try {
            const resp = await fetch(`${config.urls.obtener}?id=${id}`);
            const json = await resp.json();
            if (!json.success || !json.data) {
                Swal.fire("Error", json.message || "No se pudo obtener la ruta.", "error");
                return;
            }

            const data = json.data;
            idInput.value = data.cabecera.id;
            revInput.value = data.cabecera.revision || "";
            nameInput.value = data.cabecera.nombre || "";
            nameInput.disabled = isReadOnly;
            if (saveBtn) saveBtn.hidden = isReadOnly;

            document.getElementById("rp-title").innerHTML = `<i class="bi bi-signpost-split me-2 text-primary"></i>${isReadOnly ? "Consultar" : "Editar"} ruta: ${data.cabecera.nombre}`;
            noteEl.hidden = true;
            panelEl.hidden = false;

            renderSubprocesos(data.subprocesos || []);
            renderDisponibles(data.subprocesosDisponibles || []);

            modal.show();
        } catch (err) {
            Swal.fire("Error", "Error de comunicación con el servidor.", "error");
        }
    }

    // Renderizar subprocesos asignados
    function renderSubprocesos(items) {
        currentSubprocesos = items.sort((a, b) => a.orden - b.orden);
        countBadge.textContent = `${items.length} subproceso(s)`;

        if (!items || items.length === 0) {
            subsBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No hay subprocesos en la secuencia.</td></tr>';
            return;
        }

        subsBody.innerHTML = items.map((sub, idx) => `
            <tr data-sub-id="${sub.id}" data-orden="${sub.orden}">
                <td class="text-center fw-bold text-primary">${sub.orden}</td>
                <td><span class="badge bg-secondary-subtle text-secondary border">${sub.procesoNombre}</span></td>
                <td class="fw-medium">${sub.subProcesoNombre}</td>
                <td class="text-center">
                    ${!isReadOnly ? `
                        <div class="btn-group btn-group-sm">
                            <button type="button" class="btn btn-outline-secondary js-order-up" data-id="${sub.id}" title="Mover arriba" ${idx === 0 ? "disabled" : ""}>
                                <i class="bi bi-arrow-up"></i>
                            </button>
                            <button type="button" class="btn btn-outline-secondary js-order-down" data-id="${sub.id}" title="Mover abajo" ${idx === items.length - 1 ? "disabled" : ""}>
                                <i class="bi bi-arrow-down"></i>
                            </button>
                            <button type="button" class="btn btn-outline-danger js-remove-sub" data-id="${sub.id}" data-name="${sub.subProcesoNombre}" title="Quitar de la ruta">
                                <i class="bi bi-trash3"></i>
                            </button>
                        </div>
                    ` : '<span class="text-muted small">Sólo lectura</span>'}
                </td>
            </tr>
        `).join("");
    }

    // Renderizar dropdown de subprocesos disponibles
    function renderDisponibles(list) {
        if (!subSelect) return;
        subSelect.innerHTML = '<option value="">Seleccione un subproceso...</option>' +
            list.map(s => `<option value="${s.id}">${s.nombre}</option>`).join("");
        subSelect.value = "";
        if (addSubBtn) addSubBtn.disabled = true;
    }

    // Evento change en subproceso disponible
    subSelect?.addEventListener("change", () => {
        if (addSubBtn) addSubBtn.disabled = !subSelect.value;
    });

    // Guardar Cabecera de la Ruta
    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const nombre = nameInput.value.trim().toUpperCase();
        if (!nombre) return;

        const payload = {
            id: parseInt(idInput.value, 10) || 0,
            nombre: nombre,
            revision: revInput.value,
            contextoId: parseInt(config.contextoId, 10) || 0
        };

        saveBtn.disabled = true;
        saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const resp = await postJson(config.urls.guardar, payload);
            if (!resp.success) {
                Swal.fire("Advertencia", resp.message || "No se pudo guardar la ruta.", "warning");
                return;
            }

            Swal.fire({
                icon: "success",
                title: "Éxito",
                text: resp.message,
                timer: 1500,
                showConfirmButton: false
            });

            // Si era nueva, cargarla en modo edición para habilitar la secuencia
            if (payload.id === 0 && resp.data) {
                openModal(resp.data, false);
            } else {
                openModal(payload.id, false);
            }
        } catch (err) {
            Swal.fire("Error", "Error al procesar la solicitud.", "error");
        } finally {
            saveBtn.disabled = false;
            saveBtn.innerHTML = '<i class="bi bi-save me-1"></i>Guardar ruta';
        }
    });

    // Agregar subproceso a la ruta
    addSubBtn?.addEventListener("click", async () => {
        const subId = parseInt(subSelect.value, 10);
        if (!subId || currentRouteId <= 0) return;

        addSubBtn.disabled = true;
        try {
            const resp = await postJson(config.urls.agregarSubproceso, {
                rutaProduccionId: currentRouteId,
                subProcesoProduccionId: subId,
                orden: 0,
                contextoId: parseInt(config.contextoId, 10) || 0
            });

            if (!resp.success) {
                Swal.fire("Advertencia", resp.message || "No se pudo agregar el subproceso.", "warning");
                return;
            }

            // Recargar datos actualizados
            const reload = await fetch(`${config.urls.obtener}?id=${currentRouteId}`);
            const json = await reload.json();
            if (json.success && json.data) {
                renderSubprocesos(json.data.subprocesos || []);
                renderDisponibles(json.data.subprocesosDisponibles || []);
            }
        } catch (err) {
            Swal.fire("Error", "Error al agregar subproceso.", "error");
        } finally {
            addSubBtn.disabled = !subSelect.value;
        }
    });

    // Eventos delegados en la tabla de subprocesos (Mover arriba / Mover abajo / Quitar)
    subsBody?.addEventListener("click", async (e) => {
        const upBtn = e.target.closest(".js-order-up");
        const downBtn = e.target.closest(".js-order-down");
        const removeBtn = e.target.closest(".js-remove-sub");

        if (upBtn) {
            const subId = parseInt(upBtn.dataset.id, 10);
            await moveSubprocess(subId, -1);
        } else if (downBtn) {
            const subId = parseInt(downBtn.dataset.id, 10);
            await moveSubprocess(subId, 1);
        } else if (removeBtn) {
            const subId = parseInt(removeBtn.dataset.id, 10);
            const subName = removeBtn.dataset.name || "el subproceso";

            const result = await Swal.fire({
                title: "¿Quitar subproceso?",
                text: `¿Desea remover '${subName}' de la secuencia de esta ruta?`,
                icon: "question",
                showCancelButton: true,
                confirmButtonColor: "#dc3545",
                confirmButtonText: "Sí, quitar",
                cancelButtonText: "Cancelar"
            });

            if (result.isConfirmed) {
                try {
                    const resp = await postJson(`${config.urls.quitarSubproceso}?id=${subId}`, {});
                    if (!resp.success) {
                        Swal.fire("Error", resp.message || "No se pudo remover.", "error");
                        return;
                    }

                    // Recargar datos
                    const reload = await fetch(`${config.urls.obtener}?id=${currentRouteId}`);
                    const json = await reload.json();
                    if (json.success && json.data) {
                        renderSubprocesos(json.data.subprocesos || []);
                        renderDisponibles(json.data.subprocesosDisponibles || []);
                    }
                } catch (err) {
                    Swal.fire("Error", "Error al procesar la solicitud.", "error");
                }
            }
        }
    });

    // Mover posición del subproceso (intercambiar orden con su adyacente)
    async function moveSubprocess(subId, direction) {
        const idx = currentSubprocesos.findIndex(s => s.id === subId);
        if (idx === -1) return;
        const targetIdx = idx + direction;
        if (targetIdx < 0 || targetIdx >= currentSubprocesos.length) return;

        const currentItem = currentSubprocesos[idx];
        const targetItem = currentSubprocesos[targetIdx];

        const origOrderA = currentItem.orden;
        const origOrderB = targetItem.orden;

        // Actualizar en backend
        try {
            await postJson(config.urls.actualizarOrden, {
                rutaSubProcesoId: currentItem.id,
                orden: origOrderB,
                contextoId: parseInt(config.contextoId, 10) || 0
            });
            await postJson(config.urls.actualizarOrden, {
                rutaSubProcesoId: targetItem.id,
                orden: origOrderA,
                contextoId: parseInt(config.contextoId, 10) || 0
            });

            // Recargar datos
            const reload = await fetch(`${config.urls.obtener}?id=${currentRouteId}`);
            const json = await reload.json();
            if (json.success && json.data) {
                renderSubprocesos(json.data.subprocesos || []);
            }
        } catch (err) {
            Swal.fire("Error", "Error al actualizar el orden.", "error");
        }
    }

    // Eventos en listado principal: Ver, Editar, Anular
    document.querySelectorAll(".js-rp-view").forEach(btn => {
        btn.addEventListener("click", () => openModal(parseInt(btn.dataset.id, 10), true));
    });

    document.querySelectorAll(".js-rp-edit").forEach(btn => {
        btn.addEventListener("click", () => openModal(parseInt(btn.dataset.id, 10), false));
    });

    document.querySelectorAll(".js-rp-cancel").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = parseInt(btn.dataset.id, 10);
            const name = btn.dataset.name || "esta ruta";

            const result = await Swal.fire({
                title: "¿Anular ruta de producción?",
                text: `¿Confirma anular la ruta '${name}'?`,
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#dc3545",
                confirmButtonText: "Sí, anular",
                cancelButtonText: "Cancelar"
            });

            if (result.isConfirmed) {
                try {
                    const resp = await postJson(`${config.urls.anular}?id=${id}`, {});
                    if (resp.success) {
                        Swal.fire({
                            icon: "success",
                            title: "Anulado",
                            text: resp.message,
                            timer: 1500,
                            showConfirmButton: false
                        }).then(() => location.reload());
                    } else {
                        Swal.fire("No se pudo anular", resp.message, "error");
                    }
                } catch (err) {
                    Swal.fire("Error", "Error al procesar la solicitud.", "error");
                }
            }
        });
    });

    // Al cerrar el modal, si se hicieron cambios, refrescar para actualizar la columna de cantidad de subprocesos
    modalEl.addEventListener("hidden.bs.modal", () => {
        if (currentRouteId > 0) {
            location.reload();
        }
    });

})();

