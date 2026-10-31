(() => {
    "use strict";

    const modalInsumosEl = document.getElementById("modal-insumos");
    const modalInsumos = modalInsumosEl ? new bootstrap.Modal(modalInsumosEl) : null;

    const modalCerrarEl = document.getElementById("modal-cerrar-insumo");
    const modalCerrar = modalCerrarEl ? new bootstrap.Modal(modalCerrarEl) : null;

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

    function escapeHtml(str) {
        return (str || "").replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }

    // Ver Insumos
    document.querySelectorAll(".js-insumo-view").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            if (!id || !modalInsumos) return;

            const tbody = document.getElementById("tbody-insumos");
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">Cargando insumos...</td></tr>';
            modalInsumos.show();

            try {
                const res = await (await fetch(`/PD/RequeInsumo/Obtener?id=${id}`)).json();
                if (!res.success || !res.data || !res.data.productos) {
                    tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No se encontraron insumos.</td></tr>';
                    return;
                }

                tbody.innerHTML = "";
                if (res.data.productos.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No hay insumos registrados para esta orden.</td></tr>';
                    return;
                }

                res.data.productos.forEach(p => {
                    const tr = document.createElement("tr");
                    tr.innerHTML = `
                        <td class="font-monospace fw-semibold">${escapeHtml(p.productoCodigo || "")}</td>
                        <td>${escapeHtml(p.productoDescripcion || "")}</td>
                        <td><small class="text-muted">${escapeHtml(p.descripcion || "-")}</small></td>
                        <td class="text-end fw-bold">${Number(p.cantidad || 0).toFixed(2)}</td>
                        <td class="text-center"><span class="badge bg-light text-dark border">${escapeHtml(p.unidadMedida || "")}</span></td>
                    `;
                    tbody.appendChild(tr);
                });
            } catch (err) {
                tbody.innerHTML = `<tr><td colspan="5" class="text-danger text-center py-3">Error al cargar: ${err.message}</td></tr>`;
            }
        });
    });

    // Cerrar Requerimiento
    document.querySelectorAll(".js-insumo-close").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.getAttribute("data-id");
            const codigo = btn.getAttribute("data-codigo");
            if (!id || !modalCerrar) return;

            document.getElementById("cerrar-op-id").value = id;
            document.getElementById("cerrar-op-codigo").value = codigo || id;
            document.getElementById("cerrar-obs").value = "";

            modalCerrar.show();
        });
    });

    document.getElementById("btn-save-cerrar")?.addEventListener("click", async () => {
        const payload = {
            ordenProduccionId: parseInt(document.getElementById("cerrar-op-id").value, 10),
            estadoDespachoId: parseInt(document.getElementById("cerrar-estado-despacho").value, 10) || 3,
            observacion: document.getElementById("cerrar-obs").value || null
        };

        try {
            const res = await postJson("/PD/RequeInsumo/CerrarInsumo", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Cerrado", text: res.message, timer: 1500, showConfirmButton: false });
                modalCerrar.hide();
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo cerrar el requerimiento." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

})();

