(() => {
    'use strict';

    const modalEl = document.getElementById('mauto-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const idInput = document.getElementById('mauto-id');
    const revisionInput = document.getElementById('mauto-revision');
    const contextoInput = document.getElementById('mauto-contexto-id');

    const tipoSpan = document.getElementById('mauto-tipo');
    const almSpan = document.getElementById('mauto-alm');
    const fechaSpan = document.getElementById('mauto-fecha');
    const obsSpan = document.getElementById('mauto-obs');

    const itemsTbody = document.getElementById('mauto-items-tbody');
    const itemsCount = document.getElementById('mauto-items-count');
    const itemsTotal = document.getElementById('mauto-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    async function loadReview(id) {
        itemsTbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando datos...</td></tr>';

        try {
            const [resHdr, resItems] = await Promise.all([
                fetch(`/IV/ControlMermaAuto/Obtener?id=${id}`).then(r => r.json()),
                fetch(`/IV/ControlMerma/Productos?movimientoAlmacenId=${id}`).then(r => r.json())
            ]);

            if (!resHdr.success || !resHdr.data) {
                Swal.fire('Error', resHdr.message || 'No se pudo obtener el detalle de la merma.', 'error');
                return;
            }

            const d = resHdr.data;
            idInput.value = d.id;
            revisionInput.value = d.revision || '';

            document.getElementById('mauto-modal-title').textContent = `Revisión de Merma: ${d.codigo}`;
            document.getElementById('mauto-modal-subtitle').textContent = `Estado actual: ${d.estadoAprobacionNombre || 'PENDIENTE'} · ${d.estadoMovimientoAlmacenNombre}`;

            tipoSpan.textContent = d.tipoMovimientoAlmacenNombre ? `${d.tipoMovimientoAlmacenNombre} (${d.transaccionAlmacenCodigo || ''})` : '-';
            almSpan.textContent = d.almacenNombre || '-';
            fechaSpan.textContent = d.fechaTexto || '-';
            obsSpan.textContent = d.observacion || '(Sin observaciones registradas)';

            const items = resItems.success ? (resItems.data || []) : [];
            if (items.length === 0) {
                itemsTbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">No hay productos en este registro de merma.</td></tr>';
                itemsCount.textContent = '0';
                itemsTotal.textContent = 'S/ 0.00';
            } else {
                let total = 0;
                itemsTbody.innerHTML = items.map(item => {
                    const rowTotal = (Number(item.cantidad) || 0) * (Number(item.valorUnitario) || 0);
                    total += rowTotal;
                    return `
                        <tr>
                            <td class="font-monospace fw-semibold">${item.productoCodigo}</td>
                            <td>${item.productoNombre}</td>
                            <td class="text-center"><span class="badge bg-light text-dark border">${item.unidadMedida}</span></td>
                            <td class="text-end font-monospace fw-bold">${formatNumber(item.cantidad)}</td>
                            <td class="text-end font-monospace">${formatNumber(item.valorUnitario, 4)}</td>
                            <td class="text-end font-monospace fw-bold text-danger">S/ ${formatNumber(rowTotal)}</td>
                        </tr>
                    `;
                }).join('');

                itemsCount.textContent = items.length.toString();
                itemsTotal.textContent = `S/ ${formatNumber(total)}`;
            }

            modal.show();
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Ocurrió un error al cargar la información para revisión.', 'error');
        }
    }

    // Bind Review buttons
    document.querySelectorAll('.js-mauto-review').forEach(btn => {
        btn.addEventListener('click', () => {
            loadReview(btn.dataset.id);
        });
    });

    // Approve button
    document.querySelector('.js-mauto-approve')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Aprobar Registro de Merma?',
            text: 'Se autorizará el ajuste de inventario por merma.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, Aprobar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#198754'
        });

        if (!confirm.isConfirmed) return;

        try {
            const formData = new FormData();
            formData.append('Id', id);
            formData.append('Revision', revision);
            formData.append('ContextoId', contexto);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/ControlMermaAuto/Aprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Aprobado', result.message || 'Control de merma aprobado satisfactoriamente.', 'success').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo aprobar la merma.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al aprobar merma.', 'error');
        }
    });

    // Reject button
    document.querySelector('.js-mauto-reject')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Desaprobar Registro de Merma?',
            text: 'El registro quedará rechazado y no se ejecutará el ajuste.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, Desaprobar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545'
        });

        if (!confirm.isConfirmed) return;

        try {
            const formData = new FormData();
            formData.append('Id', id);
            formData.append('Revision', revision);
            formData.append('ContextoId', contexto);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/ControlMermaAuto/Desaprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Desaprobado', result.message || 'Registro de merma rechazado.', 'info').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo desaprobar la merma.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al desaprobar.', 'error');
        }
    });
})();

