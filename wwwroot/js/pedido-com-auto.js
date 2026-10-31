// @ts-check
(function () {
    'use strict';

    const modalEl = document.getElementById('comauto-modal');
    if (!modalEl) return;

    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    const tokenForm = /** @type {HTMLFormElement} */ (document.getElementById('comauto-token-form'));
    function getAntiForgeryToken() {
        const input = tokenForm ? tokenForm.querySelector('input[name="__RequestVerificationToken"]') : null;
        return input ? /** @type {HTMLInputElement} */ (input).value : '';
    }

    function notify(type, message) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: type,
                title: message,
                showConfirmButton: false,
                timer: 3000,
                timerProgressBar: true
            });
        } else {
            alert(message);
        }
    }

    // Modal elements
    const modalTitle = document.getElementById('comauto-modal-title');
    const modalSubtitle = document.getElementById('comauto-modal-subtitle');
    const viewCodigo = document.getElementById('comauto-view-codigo');
    const viewFecha = document.getElementById('comauto-view-fecha');
    const viewTipo = document.getElementById('comauto-view-tipo');
    const viewAprobBadge = document.getElementById('comauto-view-aprob-badge');
    const viewEstadoBadge = document.getElementById('comauto-view-estado-badge');
    const viewProv = document.getElementById('comauto-view-prov');
    const viewDoc = document.getElementById('comauto-view-doc');
    const viewObs = document.getElementById('comauto-view-obs');
    const viewObsWrapper = document.getElementById('comauto-view-obs-wrapper');
    const itemsTableBody = document.querySelector('#comauto-items-table tbody');

    const modalId = /** @type {HTMLInputElement} */ (document.getElementById('comauto-modal-id'));
    const modalRev = /** @type {HTMLInputElement} */ (document.getElementById('comauto-modal-rev'));
    const modalContexto = /** @type {HTMLInputElement} */ (document.getElementById('comauto-modal-contexto'));

    const btnApprove = document.querySelector('.js-comauto-approve');
    const btnReject = document.querySelector('.js-comauto-reject');

    let currentReq = null;

    // Open modal
    document.addEventListener('click', async function (e) {
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-comauto-open');
        if (!btnOpen) return;

        const id = Number(btnOpen.dataset.id) || 0;
        if (!id) return;

        try {
            const resp = await fetch(`/cm/pedidoComAuto/obtener?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar el requerimiento.');
                return;
            }

            const data = res.data;
            const cab = data.cabecera;
            currentReq = cab;

            modalId.value = String(cab.id);
            modalRev.value = cab.revision || '';

            if (modalTitle) modalTitle.textContent = `Autorización - Requerimiento N° ${cab.codigo}`;
            if (modalSubtitle) modalSubtitle.textContent = `Emitido el ${cab.fechaTexto} · Proveedor: ${cab.proveedorNombre}`;

            if (viewCodigo) viewCodigo.textContent = cab.codigo;
            if (viewFecha) viewFecha.textContent = cab.fechaTexto;
            if (viewTipo) viewTipo.textContent = cab.tipoProductoNombre;

            if (viewProv) viewProv.textContent = cab.proveedorNombre;
            if (viewDoc) viewDoc.textContent = cab.proveedorDocumento ? `RUC / Documento: ${cab.proveedorDocumento}` : '';

            if (viewObsWrapper && viewObs) {
                if (cab.observacion && cab.observacion.trim().length > 0) {
                    viewObs.textContent = cab.observacion;
                    viewObsWrapper.style.display = '';
                } else {
                    viewObsWrapper.style.display = 'none';
                }
            }

            if (viewAprobBadge) {
                viewAprobBadge.textContent = cab.estadoAprobacionNombre || 'PENDIENTE';
                viewAprobBadge.className = 'badge border ' + (
                    cab.estadoAprobacionId === 2 ? 'bg-success-subtle text-success border-success-subtle' :
                    cab.estadoAprobacionId === 3 ? 'bg-danger-subtle text-danger border-danger-subtle' :
                    cab.estadoAprobacionId === 1 ? 'bg-warning-subtle text-warning border-warning-subtle' : 'bg-secondary-subtle text-muted'
                );
            }

            if (viewEstadoBadge) {
                viewEstadoBadge.textContent = cab.estadoPedidoCompraNombre;
                viewEstadoBadge.className = 'badge border ' + (
                    cab.estadoPedidoCompraCodigo === '02' ? 'bg-info-subtle text-info border-info-subtle' :
                    cab.estadoPedidoCompraCodigo === '03' ? 'bg-success-subtle text-success border-success-subtle' :
                    cab.estadoPedidoCompraCodigo === '04' ? 'bg-danger-subtle text-danger border-danger-subtle' : 'bg-secondary-subtle text-muted'
                );
            }

            // Render items
            if (itemsTableBody) {
                const prods = data.productos || [];
                if (prods.length === 0) {
                    itemsTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos en este requerimiento.</td></tr>';
                } else {
                    let i = 1;
                    itemsTableBody.innerHTML = prods.map(p => `
                        <tr>
                            <td class="text-muted small">${i++}</td>
                            <td class="font-monospace fw-bold small text-primary">${p.productoCodigo || '-'}</td>
                            <td>
                                <div class="fw-semibold">${p.descripcion}</div>
                                ${p.esAdicional === 'S' ? '<span class="badge bg-info-subtle text-info border border-info-subtle small">Adicional</span>' : ''}
                            </td>
                            <td class="small text-muted">${p.unidadMedida || 'UND'}</td>
                            <td class="text-end font-monospace fw-bold">${Number(p.cantidad).toFixed(2)}</td>
                            <td class="text-end font-monospace small">${Number(p.pesoNeto || 0).toFixed(2)}</td>
                            <td class="text-end font-monospace small">${Number(p.pesoBruto || 0).toFixed(2)}</td>
                            <td class="text-center">
                                ${p.stockEnProveedor === 'S' ? '<span class="badge bg-success-subtle text-success">SÍ</span>' : '<span class="text-muted">NO</span>'}
                            </td>
                        </tr>
                    `).join('');
                }
            }

            modal.show();
        } catch (e) {
            console.error('Error al abrir detalle para autorización:', e);
            notify('error', 'Error inesperado al cargar la solicitud.');
        }
    });

    // Approve
    btnApprove?.addEventListener('click', async function () {
        if (!currentReq) return;

        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Aprobar Requerimiento ${currentReq.codigo}?`,
                text: 'El requerimiento quedará APROBADO para la generación de la Orden de Compra.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#198754',
                confirmButtonText: 'Sí, aprobar',
                cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) return;
        }

        const formData = new FormData();
        formData.append('id', modalId.value);
        formData.append('revision', modalRev.value);
        formData.append('contextoId', modalContexto.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/pedidoComAuto/aprobar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Requerimiento aprobado.');
                modal.hide();
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo aprobar el requerimiento.');
            }
        } catch (e) {
            console.error('Error al aprobar:', e);
            notify('error', 'Error al procesar la aprobación.');
        }
    });

    // Reject / Desaprobar
    btnReject?.addEventListener('click', async function () {
        if (!currentReq) return;

        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Desaprobar Requerimiento ${currentReq.codigo}?`,
                text: 'El requerimiento quedará marcado como DESAPROBADO.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                confirmButtonText: 'Sí, desaprobar',
                cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) return;
        }

        const formData = new FormData();
        formData.append('id', modalId.value);
        formData.append('revision', modalRev.value);
        formData.append('contextoId', modalContexto.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/pedidoComAuto/desaprobar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Requerimiento desaprobado.');
                modal.hide();
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo desaprobar el requerimiento.');
            }
        } catch (e) {
            console.error('Error al desaprobar:', e);
            notify('error', 'Error al procesar la desaprobación.');
        }
    });

})();

