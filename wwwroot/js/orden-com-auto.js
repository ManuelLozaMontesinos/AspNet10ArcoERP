// @ts-check
(function () {
    'use strict';

    const modalEl = document.getElementById('ocauto-modal');
    if (!modalEl) return;

    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    const tokenForm = /** @type {HTMLFormElement} */ (document.getElementById('ocauto-token-form'));
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
    const modalTitle = document.getElementById('ocauto-modal-title');
    const modalSubtitle = document.getElementById('ocauto-modal-subtitle');
    const viewCodigo = document.getElementById('ocauto-view-codigo');
    const viewFecha = document.getElementById('ocauto-view-fecha');
    const viewTipo = document.getElementById('ocauto-view-tipo');
    const viewAprobBadge = document.getElementById('ocauto-view-aprob-badge');
    const viewEstadoBadge = document.getElementById('ocauto-view-estado-badge');
    const viewProv = document.getElementById('ocauto-view-prov');
    const viewDoc = document.getElementById('ocauto-view-doc');
    const viewTotal = document.getElementById('ocauto-view-total');
    const viewPlazo = document.getElementById('ocauto-view-plazo');
    const viewPlazoWrapper = document.getElementById('ocauto-view-plazo-wrapper');
    const viewLugar = document.getElementById('ocauto-view-lugar');
    const viewLugarWrapper = document.getElementById('ocauto-view-lugar-wrapper');
    const viewObs = document.getElementById('ocauto-view-obs');
    const viewObsWrapper = document.getElementById('ocauto-view-obs-wrapper');
    const itemsTableBody = document.querySelector('#ocauto-items-table tbody');

    const modalId = /** @type {HTMLInputElement} */ (document.getElementById('ocauto-modal-id'));
    const modalRev = /** @type {HTMLInputElement} */ (document.getElementById('ocauto-modal-rev'));
    const modalContexto = /** @type {HTMLInputElement} */ (document.getElementById('ocauto-modal-contexto'));

    const btnApprove = document.querySelector('.js-ocauto-approve');
    const btnReject = document.querySelector('.js-ocauto-reject');

    let currentReq = null;

    // Open modal
    document.addEventListener('click', async function (e) {
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-ocauto-open');
        if (!btnOpen) return;

        const id = Number(btnOpen.dataset.id) || 0;
        if (!id) return;

        try {
            const resp = await fetch(`/cm/ordenComAuto/obtener?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar la orden de compra.');
                return;
            }

            const data = res.data;
            const cab = data.cabecera;
            currentReq = cab;

            modalId.value = String(cab.id);
            modalRev.value = cab.revision || '';

            if (modalTitle) modalTitle.textContent = `Autorización - Orden de Compra N° ${cab.codigo}`;
            if (modalSubtitle) modalSubtitle.textContent = `Emitida el ${cab.fechaTexto} · Proveedor: ${cab.proveedorNombre}`;

            if (viewCodigo) viewCodigo.textContent = cab.codigo;
            if (viewFecha) viewFecha.textContent = cab.fechaTexto;
            if (viewTipo) viewTipo.textContent = cab.tipoProductoNombre;

            if (viewProv) viewProv.textContent = cab.proveedorNombre;
            if (viewDoc) viewDoc.textContent = cab.proveedorDocumento ? `RUC / Documento: ${cab.proveedorDocumento}` : '';

            if (viewTotal) viewTotal.textContent = `${cab.monedaSimbolo} ${cab.montoTotalTexto || Number(cab.montoTotal).toFixed(2)}`;

            if (viewPlazoWrapper && viewPlazo) {
                if (cab.plazoEntrega && cab.plazoEntrega.trim().length > 0) {
                    viewPlazo.textContent = cab.plazoEntrega;
                    viewPlazoWrapper.style.display = '';
                } else {
                    viewPlazoWrapper.style.display = 'none';
                }
            }

            if (viewLugarWrapper && viewLugar) {
                if (cab.lugarEntrega && cab.lugarEntrega.trim().length > 0) {
                    viewLugar.textContent = cab.lugarEntrega;
                    viewLugarWrapper.style.display = '';
                } else {
                    viewLugarWrapper.style.display = 'none';
                }
            }

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
                viewEstadoBadge.textContent = cab.estadoOrdenCompraNombre;
                viewEstadoBadge.className = 'badge border ' + (
                    cab.estadoOrdenCompraCodigo === '02' ? 'bg-info-subtle text-info border-info-subtle' :
                    cab.estadoOrdenCompraCodigo === '03' ? 'bg-success-subtle text-success border-success-subtle' :
                    cab.estadoOrdenCompraCodigo === '04' ? 'bg-danger-subtle text-danger border-danger-subtle' : 'bg-secondary-subtle text-muted'
                );
            }

            // Render items
            if (itemsTableBody) {
                const prods = data.productos || [];
                if (prods.length === 0) {
                    itemsTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos en esta orden de compra.</td></tr>';
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
                            <td class="text-end font-monospace small">${Number(p.valorUnitario || 0).toFixed(4)}</td>
                            <td class="text-end font-monospace small">${Number(p.precioUnitario || 0).toFixed(4)}</td>
                            <td class="text-end font-monospace fw-bold">${Number(p.montoTotal || 0).toFixed(2)}</td>
                        </tr>
                    `).join('');
                }
            }

            modal.show();
        } catch (e) {
            console.error('Error al abrir detalle para autorización:', e);
            notify('error', 'Error inesperado al cargar la orden.');
        }
    });

    // Approve
    btnApprove?.addEventListener('click', async function () {
        if (!currentReq) return;

        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Aprobar Orden de Compra ${currentReq.codigo}?`,
                text: 'La orden quedará APROBADA formalmente para el proveedor.',
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
            const resp = await fetch('/cm/ordenComAuto/aprobar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Orden de compra aprobada.');
                modal.hide();
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo aprobar la orden.');
            }
        } catch (e) {
            console.error('Error al aprobar orden:', e);
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
                title: `¿Desaprobar Orden de Compra ${currentReq.codigo}?`,
                text: 'La orden quedará marcada como DESAPROBADA.',
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
            const resp = await fetch('/cm/ordenComAuto/desaprobar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Orden de compra desaprobada.');
                modal.hide();
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo desaprobar la orden.');
            }
        } catch (e) {
            console.error('Error al desaprobar orden:', e);
            notify('error', 'Error al procesar la desaprobación.');
        }
    });

})();

