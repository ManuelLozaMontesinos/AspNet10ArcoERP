(() => {
    "use strict";

    const modalOpEl = document.getElementById("modal-op");
    if (!modalOpEl) return;
    const modalOp = new bootstrap.Modal(modalOpEl);

    const modalEstadoEl = document.getElementById("modal-op-estado");
    const modalEstado = modalEstadoEl ? new bootstrap.Modal(modalEstadoEl) : null;

    const modalProdEl = document.getElementById("modal-op-item-prod");
    const modalProd = modalProdEl ? new bootstrap.Modal(modalProdEl) : null;

    const modalSubpEl = document.getElementById("modal-op-item-subp");
    const modalSubp = modalSubpEl ? new bootstrap.Modal(modalSubpEl) : null;

    let currentOpData = null;

    function getCsrfToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

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

    function populateSelect(selectEl, items, selectedId, emptyText = "(Seleccione)") {
        if (!selectEl) return;
        selectEl.innerHTML = "";
        if (emptyText) {
            const opt = document.createElement("option");
            opt.value = "";
            opt.textContent = emptyText;
            selectEl.appendChild(opt);
        }
        (items || []).forEach(it => {
            const opt = document.createElement("option");
            opt.value = it.id;
            opt.textContent = it.descripcion;
            if (it.id === selectedId) opt.selected = true;
            selectEl.appendChild(opt);
        });
    }

    function renderProductos(productos) {
        const tbody = document.getElementById("tbody-op-productos");
        const countBadge = document.getElementById("badge-prod-count");
        if (!tbody) return;
        tbody.innerHTML = "";

        countBadge.textContent = productos ? productos.length : 0;

        if (!productos || productos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos registrados.</td></tr>';
            return;
        }

        productos.forEach(p => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td class="font-monospace fw-semibold">${escapeHtml(p.productoCodigo || "")}</td>
                <td>${escapeHtml(p.productoDescripcion || "")}</td>
                <td><small class="text-muted">${escapeHtml(p.descripcion || "-")}</small></td>
                <td class="text-end fw-bold">${Number(p.cantidad || 0).toFixed(2)}</td>
                <td class="text-end text-success">${Number(p.cantidadEntregada || 0).toFixed(2)}</td>
                <td class="text-center"><span class="badge bg-light text-dark border">${escapeHtml(p.unidadMedida || "")}</span></td>
                <td><small>${escapeHtml(p.insumo || "-")}</small></td>
                <td class="text-end text-nowrap">
                    <button type="button" class="btn btn-outline-danger btn-sm py-0 px-1 js-prod-del" data-id="${p.id}" title="Quitar producto">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    }

    function renderSubprocesos(subprocesos) {
        const tbody = document.getElementById("tbody-op-subprocesos");
        const countBadge = document.getElementById("badge-proc-count");
        if (!tbody) return;
        tbody.innerHTML = "";

        countBadge.textContent = subprocesos ? subprocesos.length : 0;

        if (!subprocesos || subprocesos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay subprocesos asignados.</td></tr>';
            return;
        }

        subprocesos.forEach((s, idx) => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td class="text-center fw-bold">${s.orden || idx + 1}</td>
                <td class="fw-semibold">${escapeHtml(s.subProcesoNombre || "")}</td>
                <td><small>${escapeHtml(s.personalNombre || "Sin asignar")}</small></td>
                <td><small class="text-truncate d-inline-block" style="max-width: 140px;">${escapeHtml(s.maquinaRealNombre || s.maquinaNombre || "-")}</small></td>
                <td><small>${formatDate(s.fechaInicio)}</small></td>
                <td><small>${formatDate(s.fechaFin)}</small></td>
                <td><span class="badge bg-info-subtle text-info">${escapeHtml(s.estadoSubProcesoNombre || "PENDIENTE")}</span></td>
                <td class="text-end text-nowrap">
                    <button type="button" class="btn btn-outline-danger btn-sm py-0 px-1 js-subp-del" data-id="${s.id}" title="Quitar subproceso">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    }

    function formatDate(val) {
        if (!val) return "-";
        const d = new Date(val);
        if (isNaN(d.getTime())) return "-";
        return d.toLocaleDateString("es-PE") + " " + d.toLocaleTimeString("es-PE", { hour: "2-digit", minute: "2-digit" });
    }

    function escapeHtml(str) {
        return (str || "").replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }

    // Abrir Modal Nueva Orden
    document.querySelector(".js-op-new")?.addEventListener("click", async () => {
        try {
            const res = await fetch("/PD/OrdenProdu/Obtener?id=0");
            const json = await res.json();
            if (!json.success || !json.data) return;

            currentOpData = json.data;

            document.getElementById("modal-op-title").textContent = "Nueva Orden de Producción";
            document.getElementById("op-id").value = "0";
            document.getElementById("op-revision").value = "";
            document.getElementById("op-codigo").value = "";
            document.getElementById("op-fecha").value = new Date().toISOString().substring(0, 10);

            const deliveryDate = new Date();
            deliveryDate.setDate(deliveryDate.getDate() + 2);
            document.getElementById("op-fecha-entrega").value = deliveryDate.toISOString().substring(0, 16);

            document.getElementById("op-estado-nombre").value = "PENDIENTE";
            document.getElementById("op-observacion").value = "";

            populateSelect(document.getElementById("op-almacen"), currentOpData.almacenes, currentOpData.cabecera.almacenId, null);
            populateSelect(document.getElementById("op-ruta"), currentOpData.rutas, null, "(Sin ruta predeterminada)");
            populateSelect(document.getElementById("op-proveedor"), currentOpData.personal, null, "(Seleccione cliente)");

            renderProductos([]);
            renderSubprocesos([]);

            // En nueva orden, ocultar botones de agregar producto/subproceso hasta que se guarde la cabecera
            document.getElementById("btn-add-prod").disabled = true;
            document.getElementById("btn-add-subp").disabled = true;

            const tabTrigger = new bootstrap.Tab(document.getElementById("op-general-tab"));
            tabTrigger.show();

            modalOp.show();
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: "No se pudieron cargar los datos de la orden: " + err.message });
        }
    });

    // Abrir Modal Editar Orden
    document.querySelectorAll(".js-op-edit").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            if (!id) return;

            try {
                const res = await fetch(`/PD/OrdenProdu/Obtener?id=${id}`);
                const json = await res.json();
                if (!json.success || !json.data) {
                    Swal.fire({ icon: "warning", title: "Atención", text: json.message || "No se pudo obtener la orden." });
                    return;
                }

                currentOpData = json.data;
                const c = currentOpData.cabecera;

                document.getElementById("modal-op-title").textContent = `Orden de Producción N° ${c.codigo}`;
                document.getElementById("op-id").value = c.id;
                document.getElementById("op-revision").value = c.rowVersionHex || "";
                document.getElementById("op-codigo").value = c.codigo;
                document.getElementById("op-fecha").value = c.fecha ? c.fecha.substring(0, 10) : "";

                if (c.fechaEntrega) {
                    document.getElementById("op-fecha-entrega").value = c.fechaEntrega.substring(0, 16);
                } else {
                    document.getElementById("op-fecha-entrega").value = "";
                }

                document.getElementById("op-estado-nombre").value = c.estadoOrdenProduccionNombre || "";
                document.getElementById("op-observacion").value = c.observacion || "";

                populateSelect(document.getElementById("op-almacen"), currentOpData.almacenes, c.almacenId, null);
                populateSelect(document.getElementById("op-ruta"), currentOpData.rutas, c.rutaProduccionId, "(Sin ruta predeterminada)");

                // Cliente
                const provSelect = document.getElementById("op-proveedor");
                provSelect.innerHTML = `<option value="${c.proveedorId}" selected>${escapeHtml(c.clienteRazonSocial || "Cliente")}</option>`;

                renderProductos(currentOpData.productos);
                renderSubprocesos(currentOpData.subprocesos);

                document.getElementById("btn-add-prod").disabled = false;
                document.getElementById("btn-add-subp").disabled = false;

                const tabTrigger = new bootstrap.Tab(document.getElementById("op-general-tab"));
                tabTrigger.show();

                modalOp.show();
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: "No se pudo cargar la orden: " + err.message });
            }
        });
    });

    // Guardar Cabecera OP
    document.getElementById("btn-save-op")?.addEventListener("click", async () => {
        const form = document.getElementById("form-op-header");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const payload = {
            id: parseInt(document.getElementById("op-id").value, 10) || 0,
            contextoId: parseInt(document.getElementById("op-contexto").value, 10) || 0,
            fecha: document.getElementById("op-fecha").value,
            fechaEntrega: document.getElementById("op-fecha-entrega").value || null,
            almacenId: parseInt(document.getElementById("op-almacen").value, 10) || 0,
            proveedorId: parseInt(document.getElementById("op-proveedor").value, 10) || 0,
            rutaProduccionId: parseInt(document.getElementById("op-ruta").value, 10) || null,
            observacion: document.getElementById("op-observacion").value || null,
            revision: document.getElementById("op-revision").value || null
        };

        try {
            const res = await postJson("/PD/OrdenProdu/Guardar", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1500, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo guardar la orden." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Anular OP
    document.querySelectorAll(".js-op-delete").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const codigo = btn.getAttribute("data-codigo");
            if (!id) return;

            const confirm = await Swal.fire({
                icon: "warning",
                title: "¿Anular Orden de Producción?",
                text: `¿Confirma que desea anular la OP N° ${codigo}?`,
                showCancelButton: true,
                confirmButtonColor: "#d33",
                cancelButtonColor: "#6c757d",
                confirmButtonText: "Sí, anular",
                cancelButtonText: "Cancelar"
            });

            if (!confirm.isConfirmed) return;

            try {
                const res = await postJson(`/PD/OrdenProdu/Anular?id=${id}`, {});
                if (res.success) {
                    await Swal.fire({ icon: "success", title: "Anulado", text: res.message, timer: 1500, showConfirmButton: false });
                    window.location.reload();
                } else {
                    Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo anular la orden." });
                }
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            }
        });
    });

    // Cambiar Estado / Planos
    document.querySelectorAll(".js-op-status").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = parseInt(btn.getAttribute("data-id"), 10);
            const mel = parseInt(btn.getAttribute("data-mel"), 10) || null;
            const mdf = parseInt(btn.getAttribute("data-mdf"), 10) || null;
            const com = btn.getAttribute("data-com") || "";
            const mod = parseInt(btn.getAttribute("data-mod"), 10) || 0;

            if (!id || !modalEstado) return;

            try {
                const res = await fetch(`/PD/OrdenProdu/Obtener?id=${id}`);
                const json = await res.json();
                if (!json.success || !json.data) return;

                document.getElementById("estado-op-id").value = id;
                populateSelect(document.getElementById("estado-plano-mel"), json.data.estadosPlanoMel, mel, "(Sin estado)");
                populateSelect(document.getElementById("estado-plano-mdf"), json.data.estadosPlanoMdf, mdf, "(Sin estado)");
                document.getElementById("estado-modificaciones").value = mod;
                document.getElementById("estado-comentarios").value = com;

                modalEstado.show();
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            }
        });
    });

    document.getElementById("btn-save-estado")?.addEventListener("click", async () => {
        const payload = {
            ordenProduccionId: parseInt(document.getElementById("estado-op-id").value, 10),
            estadoPlanoMelId: parseInt(document.getElementById("estado-plano-mel").value, 10) || null,
            estadoPlanoMdfId: parseInt(document.getElementById("estado-plano-mdf").value, 10) || null,
            modificaciones: parseInt(document.getElementById("estado-modificaciones").value, 10) || 0,
            comentarios: document.getElementById("estado-comentarios").value || null
        };

        try {
            const res = await postJson("/PD/OrdenProdu/CambiarEstado", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Actualizado", text: res.message, timer: 1500, showConfirmButton: false });
                modalEstado.hide();
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo actualizar el estado." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Agregar Producto a la OP
    document.getElementById("btn-add-prod")?.addEventListener("click", () => {
        const opId = parseInt(document.getElementById("op-id").value, 10);
        if (!opId || !modalProd) return;

        document.getElementById("prod-item-id").value = "0";
        document.getElementById("prod-item-op-id").value = opId;
        document.getElementById("prod-item-producto-id").value = "";
        document.getElementById("prod-item-desc").value = "";
        document.getElementById("prod-item-cant").value = "1";
        document.getElementById("prod-item-entregado").value = "0";
        document.getElementById("prod-item-entrega-a").value = "";

        modalProd.show();
    });

    document.getElementById("btn-save-item-prod")?.addEventListener("click", async () => {
        const form = document.getElementById("form-op-item-prod");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const payload = {
            id: parseInt(document.getElementById("prod-item-id").value, 10) || 0,
            ordenProduccionId: parseInt(document.getElementById("prod-item-op-id").value, 10) || 0,
            productoId: parseInt(document.getElementById("prod-item-producto-id").value, 10) || 0,
            descripcion: document.getElementById("prod-item-desc").value || null,
            cantidad: parseFloat(document.getElementById("prod-item-cant").value) || 0,
            cantidadEntregada: parseFloat(document.getElementById("prod-item-entregado").value) || 0,
            entregadoA: document.getElementById("prod-item-entrega-a").value || null
        };

        try {
            const res = await postJson("/PD/OrdenProdu/GuardarProducto", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1200, showConfirmButton: false });
                modalProd.hide();
                // Refrescar productos
                const refresh = await (await fetch(`/PD/OrdenProdu/Obtener?id=${payload.ordenProduccionId}`)).json();
                if (refresh.success && refresh.data) {
                    renderProductos(refresh.data.productos);
                }
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Quitar producto de la OP
    document.getElementById("tbody-op-productos")?.addEventListener("click", async (e) => {
        const btn = e.target.closest(".js-prod-del");
        if (!btn) return;
        const id = btn.getAttribute("data-id");
        if (!id) return;

        const confirm = await Swal.fire({
            icon: "warning",
            title: "¿Quitar producto?",
            text: "¿Confirma retirar este producto de la orden?",
            showCancelButton: true,
            confirmButtonText: "Sí, retirar",
            cancelButtonText: "Cancelar"
        });
        if (!confirm.isConfirmed) return;

        try {
            const res = await postJson(`/PD/OrdenProdu/AnularProducto?id=${id}`, {});
            if (res.success) {
                const opId = parseInt(document.getElementById("op-id").value, 10);
                const refresh = await (await fetch(`/PD/OrdenProdu/Obtener?id=${opId}`)).json();
                if (refresh.success && refresh.data) {
                    renderProductos(refresh.data.productos);
                }
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Agregar Subproceso a la OP
    document.getElementById("btn-add-subp")?.addEventListener("click", () => {
        const opId = parseInt(document.getElementById("op-id").value, 10);
        if (!opId || !modalSubp || !currentOpData) return;

        document.getElementById("subp-item-id").value = "0";
        document.getElementById("subp-item-op-id").value = opId;
        document.getElementById("subp-item-subproceso-id").value = "";
        document.getElementById("subp-item-inicio").value = "";
        document.getElementById("subp-item-fin").value = "";
        document.getElementById("subp-item-obs").value = "";

        populateSelect(document.getElementById("subp-item-personal"), currentOpData.personal, null, "(Sin asignar)");
        populateSelect(document.getElementById("subp-item-maquina"), currentOpData.maquinas, null, "(Sin asignar)");
        populateSelect(document.getElementById("subp-item-maquina-real"), currentOpData.maquinas, null, "(Sin asignar)");

        modalSubp.show();
    });

    document.getElementById("btn-save-item-subp")?.addEventListener("click", async () => {
        const form = document.getElementById("form-op-item-subp");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const payload = {
            id: parseInt(document.getElementById("subp-item-id").value, 10) || 0,
            ordenProduccionId: parseInt(document.getElementById("subp-item-op-id").value, 10) || 0,
            subProcesoProduccionId: parseInt(document.getElementById("subp-item-subproceso-id").value, 10) || 0,
            personalId: parseInt(document.getElementById("subp-item-personal").value, 10) || null,
            maquinaId: parseInt(document.getElementById("subp-item-maquina").value, 10) || null,
            maquinaRealId: parseInt(document.getElementById("subp-item-maquina-real").value, 10) || null,
            fechaInicio: document.getElementById("subp-item-inicio").value || null,
            fechaFin: document.getElementById("subp-item-fin").value || null,
            observacion: document.getElementById("subp-item-obs").value || null
        };

        try {
            const res = await postJson("/PD/OrdenProdu/GuardarSubproceso", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1200, showConfirmButton: false });
                modalSubp.hide();
                // Refrescar subprocesos
                const refresh = await (await fetch(`/PD/OrdenProdu/Obtener?id=${payload.ordenProduccionId}`)).json();
                if (refresh.success && refresh.data) {
                    renderSubprocesos(refresh.data.subprocesos);
                }
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Quitar subproceso de la OP
    document.getElementById("tbody-op-subprocesos")?.addEventListener("click", async (e) => {
        const btn = e.target.closest(".js-subp-del");
        if (!btn) return;
        const id = btn.getAttribute("data-id");
        if (!id) return;

        const confirm = await Swal.fire({
            icon: "warning",
            title: "¿Quitar subproceso?",
            text: "¿Confirma retirar este subproceso de la orden?",
            showCancelButton: true,
            confirmButtonText: "Sí, retirar",
            cancelButtonText: "Cancelar"
        });
        if (!confirm.isConfirmed) return;

        try {
            const res = await postJson(`/PD/OrdenProdu/AnularSubproceso?id=${id}`, {});
            if (res.success) {
                const opId = parseInt(document.getElementById("op-id").value, 10);
                const refresh = await (await fetch(`/PD/OrdenProdu/Obtener?id=${opId}`)).json();
                if (refresh.success && refresh.data) {
                    renderSubprocesos(refresh.data.subprocesos);
                }
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

})();

