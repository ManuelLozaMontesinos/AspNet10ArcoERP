(() => {
    "use strict";

    const modalAsigEl = document.getElementById("modal-asignar-cortador");
    const modalAsig = modalAsigEl ? new bootstrap.Modal(modalAsigEl) : null;

    const modalCortadoresEl = document.getElementById("modal-cortadores-dia");
    const modalCortadores = modalCortadoresEl ? new bootstrap.Modal(modalCortadoresEl) : null;

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

    function escapeHtml(str) {
        return (str || "").replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }

    // Designar Cortador / Máquina
    document.querySelectorAll(".js-asignar-op").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const codigo = btn.getAttribute("data-codigo");
            if (!id || !modalAsig) return;

            document.getElementById("asig-op-id").value = id;
            document.getElementById("asig-op-codigo").value = codigo || id;
            document.getElementById("asig-obs").value = "";

            try {
                const res = await (await fetch(`/PD/AsigProd/Detalle?id=${id}`)).json();
                if (!res.success || !res.data) {
                    Swal.fire({ icon: "warning", title: "Atención", text: res.message || "No se pudo obtener el detalle de la OP." });
                    return;
                }

                const d = res.data;

                // Subprocesos combo
                const subpItems = (d.subprocesos || []).map(s => ({
                    id: s.subProcesoProduccionId,
                    descripcion: `${s.orden}. ${s.subProcesoNombre} (${s.estadoSubProcesoNombre || "PENDIENTE"})`
                }));
                populateSelect(document.getElementById("asig-subproceso-id"), subpItems, subpItems[0]?.id, null);

                // Personal combo
                populateSelect(document.getElementById("asig-personal-id"), d.personal, null, "(Seleccione cortador/operador)");

                // Máquinas combo
                populateSelect(document.getElementById("asig-maquina-id"), d.maquinas, null, "(Seleccione máquina)");

                modalAsig.show();
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: "Error al cargar datos: " + err.message });
            }
        });
    });

    document.getElementById("btn-save-asig")?.addEventListener("click", async () => {
        const form = document.getElementById("form-asignar-cortador");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const payload = {
            ordenProduccionId: parseInt(document.getElementById("asig-op-id").value, 10),
            subProcesoProduccionId: parseInt(document.getElementById("asig-subproceso-id").value, 10),
            personalId: parseInt(document.getElementById("asig-personal-id").value, 10) || null,
            maquinaId: parseInt(document.getElementById("asig-maquina-id").value, 10) || 0,
            observacion: document.getElementById("asig-obs").value || null
        };

        try {
            const res = await postJson("/PD/AsigProd/AsignarCortador", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Asignado", text: res.message, timer: 1500, showConfirmButton: false });
                modalAsig.hide();
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo asignar el cortador." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Ver Cortadores del Día
    document.getElementById("btn-ver-cortadores")?.addEventListener("click", async () => {
        if (!modalCortadores) return;

        const tbody = document.getElementById("tbody-cortadores-dia");
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">Cargando cortadores...</td></tr>';
        modalCortadores.show();

        try {
            const res = await (await fetch("/PD/AsigProd/Cortadores")).json();
            if (!res.success || !res.data) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No se pudieron obtener los datos.</td></tr>';
                return;
            }

            tbody.innerHTML = "";
            if (res.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No hay cortadores programados para la fecha.</td></tr>';
                return;
            }

            res.data.forEach(c => {
                const tr = document.createElement("tr");
                tr.innerHTML = `
                    <td class="fw-semibold">${escapeHtml(c.maquinaNombre || "")}</td>
                    <td>${escapeHtml(c.personalNombre || "")}</td>
                    <td><small class="text-muted">${escapeHtml(c.asistenteNombre || "-")}</small></td>
                    <td><small>${c.fecha ? new Date(c.fecha).toLocaleDateString("es-PE") : "-"}</small></td>
                `;
                tbody.appendChild(tr);
            });
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-danger text-center py-3">Error: ${err.message}</td></tr>`;
        }
    });

})();

