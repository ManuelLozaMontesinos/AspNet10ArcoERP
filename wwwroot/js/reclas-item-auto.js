(() => {
    'use strict';

    const modalEl = document.getElementById('rauto-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const idInput = document.getElementById('rauto-id');
    const revisionInput = document.getElementById('rauto-revision');
    const contextoInput = document.getElementById('rauto-contexto-id');

    const almSpan = document.getElementById('rauto-alm');
    const movSpan = document.getElementById('rauto-mov');
    const clienteSpan = document.getElementById('rauto-cliente');
    const fechaSpan = document.getElementById('rauto-fecha');
    const obsSpan = document.getElementById('rauto-obs');

    const itemsTbody = document.getElementById('rauto-items-tbody');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    async function loadReview(id) {
        itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando datos...</td></tr>';

        try {
            const res = await fetch(`/IV/ReclasItemAuto/Obtener?id=${id}`);
            const result = await res.json();

            if (!result.success || !result.data) {
                Swal.fire('Error', result.message || 'No se pudo obtener el detalle de la reclasificación.', 'error');
                return;
            }

            const d = result.data;
            idInput.value = d.id;
            revisionInput.value = d.revision || '';

            document.getElementById('rauto-modal-title').textContent = `Revisión de Reclasificación: ${d.codigo}`;
            document.getElementById('rauto-modal-subtitle').textContent = `Estado actual: ${d.estadoMovimientoReclasificacionNombre || 'PENDIENTE'} · Movimiento Original: ${d.movimientoAlmacenCodigo}`;

            almSpan.textContent = d.almacenNombre || '-';
            movSpan.textContent = d.movimientoAlmacenCodigo || '-';
            clienteSpan.textContent = d.clienteProveedorNombre || '-';
            fechaSpan.textContent = d.fechaTexto || '-';
            obsSpan.textContent = d.observacion || '(Sin observaciones registradas)';

            const items = d.productos || [];
            if (items.length === 0) {
                itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos en este registro.</td></tr>';
            } else {
                itemsTbody.innerHTML = items.map(item => {
                    const tieneRecla = item.productoDestinoId && item.productoDestinoId > 0;
                    return `
                        <tr>
                            <td class="font-monospace fw-semibold">${item.productoOrigenCodigo}</td>
                            <td>${item.productoOrigenNombre}</td>
                            <td class="text-center"><span class="badge bg-light text-dark border">${item.unidadMedidaOrigen}</span></td>
                            <td class="text-end font-monospace fw-bold">${formatNumber(item.cantidadOrigen)}</td>
                            <td class="font-monospace text-primary fw-semibold">${item.productoDestinoCodigo || '-'}</td>
                            <td>${item.productoDestinoNombre ? `<span class="fw-semibold text-primary">${item.productoDestinoNombre}</span>` : '<span class="text-muted fst-italic">Sin asignar</span>'}</td>
                            <td class="text-center">${item.unidadMedidaDestino ? `<span class="badge bg-light text-dark border">${item.unidadMedidaDestino}</span>` : '-'}</td>
                            <td class="text-end font-monospace fw-bold ${tieneRecla ? 'text-success' : 'text-muted'}">${tieneRecla ? formatNumber(item.cantidadDestino) : '-'}</td>
                        </tr>
                    `;
                }).join('');
            }

            modal.show();
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Ocurrió un error al cargar la información para revisión.', 'error');
        }
    }

    // Bind Review buttons
    document.querySelectorAll('.js-rauto-review').forEach(btn => {
        btn.addEventListener('click', () => {
            loadReview(btn.dataset.id);
        });
    });

    // Approve button
    document.querySelector('.js-rauto-approve')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Aprobar Reclasificación?',
            text: 'Se confirmará la reclasificación de items en el sistema de inventario.',
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
            const res = await fetch('/IV/ReclasItemAuto/Aprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Aprobada', result.message || 'Reclasificación aprobada satisfactoriamente.', 'success').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo aprobar la reclasificación.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al aprobar reclasificación.', 'error');
        }
    });

    // Reject button
    document.querySelector('.js-rauto-reject')?.addEventListener('click', async () => {
        const id = idInput.value;
        const revision = revisionInput.value;
        const contexto = contextoInput.value;

        const confirm = await Swal.fire({
            title: '¿Desaprobar Reclasificación?',
            text: 'El registro quedará rechazado y no se ejecutará la conversión.',
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
            const res = await fetch('/IV/ReclasItemAuto/Desaprobar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                Swal.fire('Desaprobada', result.message || 'Reclasificación rechazada.', 'info').then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo desaprobar la reclasificación.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al desaprobar.', 'error');
        }
    });
})();

