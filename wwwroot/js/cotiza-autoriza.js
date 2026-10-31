// @ts-check
(function () {
    'use strict';

    /**
     * @typedef {Object} CotizaAutorizaConfig
     * @property {boolean} canWrite
     * @property {number} contextoId
     * @property {Object} urls
     * @property {string} urls.obtener
     * @property {string} urls.aprobar
     * @property {string} urls.desaprobar
     */

    const configEl = document.getElementById('cotiza-autoriza-config');
    if (!configEl) return;

    /** @type {CotizaAutorizaConfig} */
    const config = JSON.parse(configEl.textContent || '{}');

    // Modal
    const modalEl = document.getElementById('auth-modal');
    if (!modalEl) return;
    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    // Modal Elements
    const modalTitle = document.getElementById('auth-modal-title');
    const modalSubtitle = document.getElementById('auth-modal-subtitle');
    const viewCodigo = document.getElementById('auth-view-codigo');
    const viewFecha = document.getElementById('auth-view-fecha');
    const viewCliente = document.getElementById('auth-view-cliente');
    const viewDoc = document.getElementById('auth-view-doc');
    const viewVendedor = document.getElementById('auth-view-vendedor');
    const viewLista = document.getElementById('auth-view-lista');
    const viewCondicion = document.getElementById('auth-view-condicion');
    const viewMoneda = document.getElementById('auth-view-moneda');
    const viewTc = document.getElementById('auth-view-tc');
    const viewTasa = document.getElementById('auth-view-tasa');
    const viewObsWrapper = document.getElementById('auth-view-obs-wrapper');
    const viewObs = document.getElementById('auth-view-obs');

    // Totals
    const totBruto = document.getElementById('auth-tot-bruto');
    const totDcto = document.getElementById('auth-tot-dcto');
    const totIgv = document.getElementById('auth-tot-igv');
    const totTotal = document.getElementById('auth-tot-total');

    // Items
    const itemsRows = document.getElementById('auth-items-rows');
    const actionButtons = document.getElementById('auth-action-buttons');
    const btnAprobar = document.getElementById('auth-btn-aprobar');
    const btnDesaprobar = document.getElementById('auth-btn-desaprobar');

    // Current State
    let currentAuth = null;

    function getAntiForgeryToken() {
        const tokenInput = /** @type {HTMLInputElement} */ (document.querySelector('input[name="__RequestVerificationToken"]'));
        return tokenInput ? tokenInput.value : '';
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

    function formatNumber(val, decimals = 2) {
        const num = Number(val) || 0;
        return num.toLocaleString('en-US', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    async function cargarDetalle(id) {
        if (!itemsRows) return;
        itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando solicitud...</td></tr>';

        try {
            const resp = await fetch(`${config.urls.obtener}?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar la solicitud.');
                return;
            }

            const data = res.data;
            currentAuth = data;

            if (modalTitle) modalTitle.textContent = `Solicitud de Autorización: ${data.tipoAutorizacionNombre || 'Comercial'}`;
            if (modalSubtitle) modalSubtitle.textContent = `Cotización ${data.codigo} · Estado: ${data.estadoAprobacionNombre}`;

            if (viewCodigo) viewCodigo.textContent = data.codigo || '';
            if (viewFecha) viewFecha.textContent = data.fechaTexto || '';
            if (viewCliente) viewCliente.textContent = data.clienteNombre || '';
            if (viewDoc) viewDoc.textContent = data.clienteDocumento ? `Doc: ${data.clienteDocumento}` : '';
            if (viewVendedor) viewVendedor.textContent = data.vendedorNombre || '';
            if (viewLista) viewLista.textContent = data.listaPrecioNombre || '-';
            if (viewCondicion) viewCondicion.textContent = data.tipoCondicionPagoNombre || '-';
            if (viewMoneda) viewMoneda.textContent = `${data.monedaNombre || ''} (${data.monedaSimbolo || ''})`;
            if (viewTc) viewTc.textContent = formatNumber(data.tipoCambio, 3);
            if (viewTasa) viewTasa.textContent = `${formatNumber(data.impuestoTasa, 2)}%`;

            if (viewObsWrapper && viewObs) {
                if (data.observacion) {
                    viewObs.textContent = data.observacion;
                    viewObsWrapper.style.display = '';
                } else {
                    viewObsWrapper.style.display = 'none';
                }
            }

            if (totBruto) totBruto.textContent = formatNumber(data.montoBruto);
            if (totDcto) totDcto.textContent = formatNumber(data.montoDctoItem);
            if (totIgv) totIgv.textContent = formatNumber(data.montoIgv);
            if (totTotal) totTotal.textContent = formatNumber(data.montoTotal);

            const isPending = data.estadoAprobacionId === 1;
            const canAction = config.canWrite && isPending;

            if (actionButtons) {
                actionButtons.style.display = canAction ? '' : 'none';
            }

            const list = data.productos || [];
            if (list.length === 0) {
                itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">No hay ítems registrados en esta solicitud.</td></tr>';
            } else {
                let html = '';
                list.forEach(p => {
                    const isSolicitado = p.solicitaAutorizacionDescuento === 'S';
                    const rowClass = isSolicitado ? 'table-warning' : '';
                    const badgeSolicitado = isSolicitado ? '<span class="badge bg-warning text-dark me-1">Requiere Autorización</span>' : '';

                    html += `
                        <tr class="${rowClass}">
                            <td class="font-monospace small">${escapeHtml(p.productoCodigo || '')}</td>
                            <td>
                                <div class="fw-semibold">${escapeHtml(p.productoNombre || p.descripcion || '')}</div>
                                ${badgeSolicitado}
                            </td>
                            <td class="small text-muted">${escapeHtml(p.unidadMedidaCodigo || '')}</td>
                            <td class="text-end font-monospace">${formatNumber(p.cantidad, 2)}</td>
                            <td class="text-end font-monospace text-muted">${formatNumber(p.precioUnitarioLista, 4)}</td>
                            <td class="text-end font-monospace text-primary fw-semibold">${formatNumber(p.precioUnitario, 4)}</td>
                            <td class="text-end font-monospace">
                                ${canAction ? `
                                    <input type="number" step="0.0001" min="0.0001" max="999999"
                                           class="form-control form-control-sm text-end font-monospace js-auth-price"
                                           data-id="${p.cotizacionProductoId}" data-cant="${p.cantidad}"
                                           value="${(p.precioAutorizado || p.precioUnitario).toFixed(4)}" />
                                ` : `
                                    <span class="fw-bold">${formatNumber(p.precioAutorizado || p.precioUnitario, 4)}</span>
                                `}
                            </td>
                            <td class="text-end font-monospace fw-bold js-auth-row-total">${formatNumber(p.montoTotal, 2)}</td>
                        </tr>
                    `;
                });
                itemsRows.innerHTML = html;
            }

            modal.show();
        } catch (err) {
            console.error(err);
            notify('error', 'Error al consultar la solicitud de autorización.');
        }
    }

    // Dynamic row total recalculation on price input
    if (itemsRows) {
        itemsRows.addEventListener('input', function (e) {
            const input = /** @type {HTMLInputElement} */ (e.target);
            if (!input.classList.contains('js-auth-price')) return;

            const cant = Number(input.getAttribute('data-cant')) || 0;
            const precio = Number(input.value) || 0;
            const total = cant * precio;

            const tr = input.closest('tr');
            if (tr) {
                const totalCell = tr.querySelector('.js-auth-row-total');
                if (totalCell) totalCell.textContent = formatNumber(total, 2);
            }
        });
    }

    // --- Aprobar ---
    if (btnAprobar) {
        btnAprobar.addEventListener('click', async function () {
            if (!currentAuth) return;

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const confirmRes = await Swal.fire({
                    title: `¿Aprobar solicitud de la cotización ${currentAuth.codigo}?`,
                    text: 'Se aplicarán los precios autorizados y la cotización quedará aprobada para venta.',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, Aprobar',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#198754'
                });
                if (!confirmRes.isConfirmed) return;
            } else if (!confirm(`¿Aprobar la cotización ${currentAuth.codigo}?`)) {
                return;
            }

            // Recolectar precios autorizados
            const items = [];
            const priceInputs = itemsRows?.querySelectorAll('.js-auth-price') || [];
            priceInputs.forEach(el => {
                const inp = /** @type {HTMLInputElement} */ (el);
                const pid = Number(inp.getAttribute('data-id')) || 0;
                const price = Number(inp.value) || 0;
                if (pid > 0 && price > 0) {
                    items.push({ cotizacionProductoId: pid, precioAutorizado: price });
                }
            });

            try {
                btnAprobar.setAttribute('disabled', 'true');
                const payload = {
                    contextoId: config.contextoId,
                    cotizacionAutorizacionId: currentAuth.cotizacionAutorizacionId,
                    cotizacionId: currentAuth.cotizacionId,
                    revision: currentAuth.revision,
                    items: items
                };

                const resp = await fetch(config.urls.aprobar, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(payload)
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo aprobar la solicitud.');
                    return;
                }

                modal.hide();

                // @ts-ignore
                if (window.Swal) {
                    // @ts-ignore
                    await Swal.fire({
                        title: '¡Aprobada!',
                        text: res.message || 'La solicitud ha sido aprobada correctamente.',
                        icon: 'success',
                        confirmButtonText: 'Aceptar'
                    });
                } else {
                    alert(res.message || 'Aprobado correctamente.');
                }

                window.location.reload();
            } catch (err) {
                console.error(err);
                notify('error', 'Error de comunicación al aprobar la solicitud.');
            } finally {
                btnAprobar.removeAttribute('disabled');
            }
        });
    }

    // --- Desaprobar ---
    if (btnDesaprobar) {
        btnDesaprobar.addEventListener('click', async function () {
            if (!currentAuth) return;

            let observacion = '';
            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const promptRes = await Swal.fire({
                    title: `¿Desaprobar solicitud de la cotización ${currentAuth.codigo}?`,
                    text: 'Indique el motivo o indicaciones para el vendedor:',
                    input: 'textarea',
                    inputPlaceholder: 'Escriba las observaciones del rechazo...',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, Desaprobar',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#dc3545'
                });
                if (!promptRes.isConfirmed) return;
                observacion = promptRes.value || '';
            } else if (!confirm(`¿Desaprobar la cotización ${currentAuth.codigo}?`)) {
                return;
            }

            try {
                btnDesaprobar.setAttribute('disabled', 'true');
                const payload = {
                    contextoId: config.contextoId,
                    cotizacionAutorizacionId: currentAuth.cotizacionAutorizacionId,
                    cotizacionId: currentAuth.cotizacionId,
                    revision: currentAuth.revision,
                    observacion: observacion
                };

                const resp = await fetch(config.urls.desaprobar, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(payload)
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo desaprobar la solicitud.');
                    return;
                }

                modal.hide();

                // @ts-ignore
                if (window.Swal) {
                    // @ts-ignore
                    await Swal.fire({
                        title: 'Desaprobada',
                        text: res.message || 'La solicitud ha sido desaprobada.',
                        icon: 'info',
                        confirmButtonText: 'Aceptar'
                    });
                } else {
                    alert(res.message || 'Solicitud desaprobada.');
                }

                window.location.reload();
            } catch (err) {
                console.error(err);
                notify('error', 'Error al desaprobar la solicitud.');
            } finally {
                btnDesaprobar.removeAttribute('disabled');
            }
        });
    }

    // Global click listener to open modal from listing
    document.addEventListener('click', async function (e) {
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-auth-open');
        if (btnOpen) {
            e.preventDefault();
            const id = Number(btnOpen.getAttribute('data-id')) || 0;
            if (id > 0) {
                await cargarDetalle(id);
            }
        }
    });

})();

