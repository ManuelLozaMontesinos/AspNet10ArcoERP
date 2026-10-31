(() => {
    'use strict';

    const modalEl = document.getElementById('cauto-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const idInput = document.getElementById('cauto-id');
    const revisionInput = document.getElementById('cauto-revision');
    const contextoInput = document.getElementById('cauto-contexto-id');

    const almSpan = document.getElementById('cauto-alm');
    const ccSpan = document.getElementById('cauto-cc');
    const solicSpan = document.getElementById('cauto-solic');
    const fechaSpan = document.getElementById('cauto-fecha');
    const obsSpan = document.getElementById('cauto-obs');

    const itemsTbody = document.getElementById('cauto-items-tbody');
    const itemsCount = document.getElementById('cauto-items-count');
    const itemsTotal = document.getElementById('cauto-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    async function loadReview(id) {
        itemsTbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando datos...</td></tr>';

        try {
            const [resHdr, resItems] = await Promise.all([
                fetch(`/IV/ConsumoIntAuto/Obtener?id=${id}`).then(r => r.json()),
                fetch(`/IV/ConsumoInt/Productos?movimientoAlmacenId=${id}`).then(r => r.json())
            ]);

            if (!resHdr.success || !resHdr.data) {
                Swal.fire('Error', resHdr.message || 'No se pudo obtener el detalle del consumo.', 'error');
                return;
            }

            const d = resHdr.data;
            idInput.value = d.id;
            revisionInput.value = d.revision || '';

            document.getElementById('cauto-modal-title').textContent = `Revisión de Consumo: ${d.codigo}`;
            document.getElementById('cauto-modal-subtitle').textContent = `Estado actual: ${d.estadoAprobacionNombre || 'PENDIENTE'} · ${d.estadoMovimientoAlmacenNombre}`;

            almSpan.textContent = d.almacenNombre || '-';
            ccSpan.textContent = d.centroCostoNombre || '-';
            solicSpan.textContent = d.personalNombre ? `${d.personalNombre} (DNI: ${d.personalDocumento || 'S/N'})` : '-';
            fechaSpan.textContent = d.fechaTexto || '-';
            obsSpan.textContent = d.observacion || '(Sin observaciones adicionales)';

            const items = resItems.success ? (resItems.data || []) : [];
            if (items.length === 0) {
                itemsTbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">No hay productos en esta solicitud.</td></tr>';
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
                            <td class="text-end font-monospace fw-bold text-primary">S/ ${formatNumber(rowTotal)}</td>
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
    document.querySelectorAll('.js-cauto-review').forEach(btn => {
        btn.addEventListener('click', () => {
            loadReview(btn.dataset.id);
        });
    });

    // Approve button
    document.querySelector('.js-cauto-approve')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Aprobar Consumo Interno?',
            text: 'Se autorizará el despacho definitivo de los insumos solicitados.',
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
            const res = await fetch('/IV/ConsumoIntAuto/Aprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Aprobado', result.message || 'Consumo interno aprobado satisfactoriamente.', 'success').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo aprobar el consumo.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al aprobar consumo.', 'error');
        }
    });

    // Reject button
    document.querySelector('.js-cauto-reject')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Desaprobar Consumo Interno?',
            text: 'El consumo quedará rechazado y no se despacharán los insumos.',
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
            const res = await fetch('/IV/ConsumoIntAuto/Desaprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Desaprobado', result.message || 'Consumo interno rechazado.', 'info').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo desaprobar el consumo.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al desaprobar.', 'error');
        }
    });
})();

