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

})();

