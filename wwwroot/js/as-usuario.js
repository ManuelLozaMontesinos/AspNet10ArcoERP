(() => {
    "use strict";

    const modalUserEl = document.getElementById("modal-user");
    const modalUser = modalUserEl ? new bootstrap.Modal(modalUserEl) : null;

    const modalPswEl = document.getElementById("modal-psw");
    const modalPsw = modalPswEl ? new bootstrap.Modal(modalPswEl) : null;

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

    function renderRolesCheckboxes(rolesDisponibles, rolesAsignados) {
        const container = document.getElementById("roles-checkboxes");
        if (!container) return;
        container.innerHTML = "";

        if (!rolesDisponibles || rolesDisponibles.length === 0) {
            container.innerHTML = '<div class="col-12 text-muted small py-2">No hay roles registrados en el sistema.</div>';
            return;
        }

        rolesDisponibles.forEach(r => {
            const col = document.createElement("div");
            col.className = "col-md-6";
            const checked = rolesAsignados && rolesAsignados.includes(r.id) ? "checked" : "";
            col.innerHTML = `
                <div class="form-check">
                    <input class="form-check-input js-role-chk" type="checkbox" value="${r.id}" id="chk-role-${r.id}" ${checked}>
                    <label class="form-check-label small" for="chk-role-${r.id}">
                        <span class="fw-bold font-monospace">${escapeHtml(r.codigo || "")}</span> - ${escapeHtml(r.descripcion || "")}
                    </label>
                </div>
            `;
            container.appendChild(col);
        });
    }

    // Nuevo Usuario
    document.querySelector(".js-user-new")?.addEventListener("click", async () => {
        if (!modalUser) return;

        document.getElementById("modal-user-title").textContent = "Nuevo Usuario";
        document.getElementById("user-id").value = "0";
        document.getElementById("user-logon").value = "";
        document.getElementById("user-logon").disabled = false;
        document.getElementById("user-nombre").value = "";
        document.getElementById("user-email").value = "";
        document.getElementById("div-user-psw").hidden = false;
        document.getElementById("user-password").required = true;
        document.getElementById("user-password").value = "";
        document.getElementById("user-cambiar-psw").checked = false;
        document.getElementById("user-no-expira").checked = true;

        try {
            const res = await (await fetch("/AS/Usuario/Obtener?id=0")).json();
            if (res.success && res.data) {
                renderRolesCheckboxes(res.data.rolesDisponibles, []);
            }
            modalUser.show();
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: "No se pudieron cargar los roles: " + err.message });
        }
    });

    // Editar Usuario
    document.querySelectorAll(".js-user-edit").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            if (!id || !modalUser) return;

            try {
                const res = await (await fetch(`/AS/Usuario/Obtener?id=${id}`)).json();
                if (!res.success || !res.data) {
                    Swal.fire({ icon: "warning", title: "Atención", text: res.message || "No se encontró el usuario." });
                    return;
                }

                const d = res.data.datos;
                document.getElementById("modal-user-title").textContent = `Editar Usuario: ${d.usuarioLogon}`;
                document.getElementById("user-id").value = d.id;
                document.getElementById("user-logon").value = d.usuarioLogon;
                document.getElementById("user-logon").disabled = true;
                document.getElementById("user-nombre").value = d.nombreCompleto;
                document.getElementById("user-email").value = d.email;
                document.getElementById("div-user-psw").hidden = true;
                document.getElementById("user-password").required = false;
                document.getElementById("user-cambiar-psw").checked = d.cambiarPswProximo;
                document.getElementById("user-no-expira").checked = d.noExpira;

                renderRolesCheckboxes(res.data.rolesDisponibles, res.data.rolesAsignados);

                modalUser.show();
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            }
        });
    });

    // Guardar Usuario
    document.getElementById("btn-save-user")?.addEventListener("click", async () => {
        const form = document.getElementById("form-user");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const selectedRoles = [];
        document.querySelectorAll(".js-role-chk:checked").forEach(chk => {
            selectedRoles.push(parseInt(chk.value, 10));
        });

        const payload = {
            id: parseInt(document.getElementById("user-id").value, 10) || 0,
            usuarioLogon: document.getElementById("user-logon").value.trim(),
            nombreCompleto: document.getElementById("user-nombre").value.trim(),
            email: document.getElementById("user-email").value.trim(),
            password: document.getElementById("user-password").value || null,
            cambiarPswProximo: document.getElementById("user-cambiar-psw").checked,
            noExpira: document.getElementById("user-no-expira").checked,
            rolesIds: selectedRoles
        };

        try {
            const res = await postJson("/AS/Usuario/Guardar", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Guardado", text: res.message, timer: 1500, showConfirmButton: false });
                modalUser.hide();
                window.location.reload();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message || "No se pudo guardar el usuario." });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

    // Cambiar Estado Usuario (Habilitar / Deshabilitar)
    document.querySelectorAll(".js-user-toggle").forEach(btn => {
        btn.addEventListener("click", async () => {
            const id = btn.getAttribute("data-id");
            const estado = btn.getAttribute("data-estado");
            const logon = btn.getAttribute("data-logon");
            const actionText = estado === "A" ? "habilitar" : "deshabilitar";

            const confirm = await Swal.fire({
                icon: "question",
                title: `¿${actionText.toUpperCase()} usuario?`,
                text: `¿Confirma ${actionText} la cuenta de '${logon}'?`,
                showCancelButton: true,
                confirmButtonText: `Sí, ${actionText}`,
                cancelButtonText: "Cancelar"
            });
            if (!confirm.isConfirmed) return;

            try {
                const res = await postJson(`/AS/Usuario/CambiarEstado?id=${id}&estado=${estado}`, {});
                if (res.success) {
                    await Swal.fire({ icon: "success", title: "Actualizado", text: res.message, timer: 1500, showConfirmButton: false });
                    window.location.reload();
                } else {
                    Swal.fire({ icon: "error", title: "Error", text: res.message });
                }
            } catch (err) {
                Swal.fire({ icon: "error", title: "Error", text: err.message });
            }
        });
    });

    // Modal Cambiar Contraseña
    document.querySelectorAll(".js-user-psw").forEach(btn => {
        btn.addEventListener("click", () => {
            const id = btn.getAttribute("data-id");
            const logon = btn.getAttribute("data-logon");
            if (!id || !modalPsw) return;

            document.getElementById("psw-user-id").value = id;
            document.getElementById("psw-user-logon").value = logon || id;
            document.getElementById("psw-nueva").value = "";
            document.getElementById("psw-forzar").checked = true;

            modalPsw.show();
        });
    });

    document.getElementById("btn-save-psw")?.addEventListener("click", async () => {
        const id = parseInt(document.getElementById("psw-user-id").value, 10);
        const psw = document.getElementById("psw-nueva").value;
        if (!psw || psw.length < 4) {
            Swal.fire({ icon: "warning", title: "Contraseña inválida", text: "La contraseña debe tener al menos 4 caracteres." });
            return;
        }

        const payload = {
            usuarioId: id,
            nuevaContrasena: psw,
            forzarCambioProximo: document.getElementById("psw-forzar").checked
        };

        try {
            const res = await postJson("/AS/Usuario/CambiarContrasena", payload);
            if (res.success) {
                await Swal.fire({ icon: "success", title: "Actualizado", text: res.message, timer: 1500, showConfirmButton: false });
                modalPsw.hide();
            } else {
                Swal.fire({ icon: "error", title: "Error", text: res.message });
            }
        } catch (err) {
            Swal.fire({ icon: "error", title: "Error", text: err.message });
        }
    });

})();

