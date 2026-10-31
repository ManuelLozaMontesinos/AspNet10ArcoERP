(() => {
    'use strict';

    const modalEl = document.getElementById('ting-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const form = document.getElementById('ting-form');
    const idInput = document.getElementById('ting-id');
    const contextoInput = document.getElementById('ting-contexto-id');
    const revisionInput = document.getElementById('ting-revision');
    const fechaInput = document.getElementById('ting-fecha');
    const obsInput = document.getElementById('ting-obs');
    const btnSave = document.getElementById('btn-save-ting');
    const itemsTbody = document.getElementById('ting-items-tbody');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    async function showModalWithData(id, isViewOnly = false) {
        try {
            const res = await fetch(`/IV/TransfeIngreso/Obtener?id=${id}`);
            const result = await res.json();
            if (!result.success || !result.data) {
                Swal.fire({ icon: 'warning', title: 'Atención', text: result.message || 'No se pudo cargar la transferencia.' });
                return;
            }

            const d = result.data;
            const c = d.cabecera;

            idInput.value = c.id;
            revisionInput.value = c.revision || '';
            fechaInput.value = new Date().toISOString().split('T')[0];
            obsInput.value = '';

            document.getElementById('ting-modal-title').innerHTML = isViewOnly
                ? `<i class="bi bi-eye me-2"></i>Detalle de Transferencia: ${c.codigo}`
                : `<i class="bi bi-box-arrow-in-down me-2"></i>Recepcionar Transferencia: ${c.codigo}`;
            document.getElementById('ting-modal-subtitle').textContent = `Origen: ${c.almacenNombre} ➔ Destino: ${c.almacenDestinoNombre}`;

            if (isViewOnly) {
                btnSave.classList.add('d-none');
                fechaInput.disabled = true;
                obsInput.disabled = true;
            } else {
                btnSave.classList.remove('d-none');
                fechaInput.disabled = false;
                obsInput.disabled = false;
            }

            if (d.productos && d.productos.length > 0) {
                itemsTbody.innerHTML = d.productos.map(p => `
                    <tr>
                        <td class="font-monospace fw-semibold">${p.productoCodigo}</td>
                        <td>${p.descripcion}</td>
                        <td class="text-center"><span class="badge bg-light text-dark border">${p.unidadMedida}</span></td>
                        <td class="text-end font-monospace fw-bold">${formatNumber(p.cantidad)}</td>
                        <td class="text-end font-monospace">${formatNumber(p.valorUnitario, 4)}</td>
                        <td class="text-end font-monospace fw-semibold">${formatNumber(p.total)}</td>
                    </tr>
                `).join('');
            } else {
                itemsTbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">No hay productos registrados en esta transferencia.</td></tr>';
            }

            modal.show();
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al consultar la transferencia.' });
        }
    }

    document.addEventListener('click', (e) => {
        const btnRecep = e.target.closest('.js-ting-recep');
        if (btnRecep) {
            showModalWithData(btnRecep.dataset.id, false);
            return;
        }

        const btnView = e.target.closest('.js-ting-view');
        if (btnView) {
            showModalWithData(btnView.dataset.id, true);
        }
    });

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = parseInt(idInput.value);
        if (!id || id <= 0) return;

        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Recepcionando...';

        try {
            const formData = new FormData(form);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/TransfeIngreso/Recepcionar', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                await Swal.fire({ icon: 'success', title: 'Recepcionada', text: result.message, timer: 1800, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo recepcionar la transferencia.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
        } finally {
            btnSave.disabled = false;
            btnSave.innerHTML = '<i class="bi bi-check2-circle me-1"></i>Confirmar Recepción';
        }
    });
})();

