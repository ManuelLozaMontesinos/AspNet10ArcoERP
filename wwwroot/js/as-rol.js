(() => {
    "use strict";

    const modalRolEl = document.getElementById("modal-rol");
    const modalRol = modalRolEl ? new bootstrap.Modal(modalRolEl) : null;

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

    // Nuevo Rol
    document.querySelector(".js-rol-new")?.addEventListener("click", () => {
        if (!modalRol) return;

        document.getElementById("modal-rol-title").textContent = "Nuevo Rol";
        document.getElementById("rol-id").value = "0";
        document.getElementById("rol-codigo").value = "";
        document.getElementById("rol-codigo").disabled = false;
        document.getElementById("rol-nombre").value = "";

        modalRol.show();
    });

    // Editar Rol
    document.querySelectorAll(".js-rol-edit").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.getAttribute("data-id");
            const codigo = btn.getAttribute("data-codigo");
            const nombre = btn.getAttribute("data-nombre");
            if (!id || !modalRol) return;

            document.getElementById("modal-rol-title").textContent = `Editar Rol: ${nombre}`;
            document.getElementById("rol-id").value = id;
            document.getElementById("rol-codigo").value = codigo || "";
            document.getElementById("rol-codigo").disabled = true;
            document.getElementById("rol-nombre").value = nombre || "";

            modalRol.show();
        });
    });

    // Guardar Rol
    document.getElementById("btn-save-rol")?.addEventListener("click", async () => {
        const form = document.getElementById("form-rol");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const payload = {
            id: parseInt(document.getElementById("rol-id").value, 10) || 0,
            codigo: document.getElementById("rol-codigo").value.trim().toUpperCase(),
            nombre: document.getElementById("rol-nombre").value.trim()
        };

        try {
            const res = await postJson("/AS/Rol/Guardar", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1500, showConfirmButton: false });
                modalRol.hide();
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo guardar el rol." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Anular Rol
    document.querySelectorAll(".js-rol-del").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const nombre = btn.getAttribute("data-nombre");
            if (!id) return;

            const confirm = await Swal.fire({
                icon: "warning",
                title: "¿Anular Rol?",
                text: `¿Confirma anular el rol '${nombre}'?`,
                showCancelButton: true,
                confirmButtonColor: "#d33",
                cancelButtonColor: "#6c757d",
                confirmButtonText: "Sí, anular",
                cancelButtonText: "Cancelar"
            });
            if (!confirm.isConfirmed) return;

            try {
                const res = await postJson(`/AS/Rol/Anular?id=${id}`, {});
                if (res.success) {
                    await Swal.fire({ icon: "success", title: "Anulado", text: res.message, timer: 1500, showConfirmButton: false });
                    window.location.reload();
                } else {
                    Swal.fire({ icon: "error", title: "Error", text: res.message });
                }
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            }
        });
    });

    // ========================================================
    // GESTIÓN DE PERMISOS DE MENÚ POR ROL
    // ========================================================
    const modalPermisosEl = document.getElementById("modal-rol-permisos");
    const modalPermisos = modalPermisosEl ? new bootstrap.Modal(modalPermisosEl) : null;

    document.querySelectorAll(".js-rol-permisos").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const nombre = btn.getAttribute("data-nombre");
            if (!id || !modalPermisos) return;

            document.getElementById("modalRolPermisosLabel").textContent = `Permisos: ${nombre}`;
            document.getElementById("permisos-rol-id").value = id;
            document.getElementById("permisos-loading").classList.remove("d-none");
            document.getElementById("permisos-container").classList.add("d-none");

            modalPermisos.show();

            try {
                const res = await (await fetch(`/AS/Rol/ObtenerPermisos?rolId=${id}`)).json();
                if (res.success && res.data) {
                    renderPermisosTable(res.data);
                } else {
                    Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudieron obtener los permisos." });
                }
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            } finally {
                document.getElementById("permisos-loading").classList.add("d-none");
                document.getElementById("permisos-container").classList.remove("d-none");
            }
        });
    });

    function renderPermisosTable(items) {
        const tbody = document.getElementById("permisos-tbody");
        if (!tbody) return;
        tbody.innerHTML = "";

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-center text-muted py-3">No hay opciones de menú disponibles.</td></tr>';
            return;
        }

        // Crear mapa para anidar menús padres e hijos
        const padres = items.filter(x => !x.menuPadreId);
        const mapHijos = {};
        items.forEach(x => {
            if (x.menuPadreId) {
                if (!mapHijos[x.menuPadreId]) mapHijos[x.menuPadreId] = [];
                mapHijos[x.menuPadreId].push(x);
            }
        });

        function appendRow(m, nivel) {
            const tr = document.createElement("tr");
            const indent = nivel > 0 ? `style="padding-left: ${nivel * 24}px !important;"` : "";
            const isPadre = !m.url;
            const rowClass = isPadre ? "table-light fw-bold" : "";
            if (rowClass) tr.className = rowClass;

            const iconHtml = m.icono ? `<i class="${m.icono} me-2 text-primary"></i>` : (isPadre ? '<i class="bi bi-folder me-2 text-secondary"></i>' : '<i class="bi bi-file-earmark me-2 text-muted"></i>');

            tr.innerHTML = `
                <td ${indent}>
                    <div class="d-flex align-items-center">
                        ${iconHtml}
                        <span>${m.nombre}</span>
                        ${m.area ? `<span class="badge bg-body-secondary text-muted ms-2 text-uppercase text-xs">${m.area}</span>` : ""}
                    </div>
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input chk-permiso-lectura" data-menu-id="${m.menuId}" ${m.lectura ? "checked" : ""} />
                </td>
                <td class="text-center">
                    <input type="checkbox" class="form-check-input chk-permiso-escritura" data-menu-id="${m.menuId}" ${m.escritura ? "checked" : ""} />
                </td>
            `;

            // Auto-check lectura si se marca escritura
            const chkLec = tr.querySelector(".chk-permiso-lectura");
            const chkEsc = tr.querySelector(".chk-permiso-escritura");
            chkEsc?.addEventListener("change", function() {
                if (this.checked && chkLec) chkLec.checked = true;
            });
            chkLec?.addEventListener("change", function() {
                if (!this.checked && chkEsc) chkEsc.checked = false;
            });

            tbody.appendChild(tr);

            if (mapHijos[m.menuId]) {
                mapHijos[m.menuId].forEach(h => appendRow(h, nivel + 1));
            }
        }

        padres.forEach(p => appendRow(p, 0));

        // Menús que no tienen padre en la lista
        items.forEach(m => {
            if (m.menuPadreId && !items.some(x => x.menuId === m.menuPadreId)) {
                appendRow(m, 0);
            }
        });
    }

    // Botones rápidos de selección
    document.getElementById("btn-permisos-todos-lectura")?.addEventListener("click", () => {
        document.querySelectorAll(".chk-permiso-lectura").forEach(c => c.checked = true);
    });

    document.getElementById("btn-permisos-todos-escritura")?.addEventListener("click", () => {
        document.querySelectorAll(".chk-permiso-lectura, .chk-permiso-escritura").forEach(c => c.checked = true);
    });

    document.getElementById("btn-permisos-desmarcar-todos")?.addEventListener("click", () => {
        document.querySelectorAll(".chk-permiso-lectura, .chk-permiso-escritura").forEach(c => c.checked = false);
    });

    // Guardar Permisos
    document.getElementById("btn-save-permisos")?.addEventListener("click", async () => {
        const rolId = parseInt(document.getElementById("permisos-rol-id").value, 10);
        if (!rolId) return;

        const permisos = [];
        const chkLecturas = document.querySelectorAll(".chk-permiso-lectura");
        chkLecturas.forEach(chkL => {
            const menuId = parseInt(chkL.getAttribute("data-menu-id"), 10);
            const row = chkL.closest("tr");
            const chkE = row?.querySelector(".chk-permiso-escritura");
            const lectura = chkL.checked;
            const escritura = chkE ? chkE.checked : false;

            permisos.push({
                menuId: menuId,
                lectura: lectura,
                escritura: escritura
            });
        });

        try {
            const res = await postJson("/AS/Rol/GuardarPermisos", {
                rolId: rolId,
                permisos: permisos
            });
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1500, showConfirmButton: false });
                modalPermisos?.hide();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudieron guardar los permisos." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

})();

