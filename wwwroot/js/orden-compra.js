// @ts-check
(function () {
    'use strict';

    const modalEl = document.getElementById('oc-modal');
    if (!modalEl) return;

    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    const statusModalEl = document.getElementById('oc-status-modal');
    // @ts-ignore
    const statusModal = statusModalEl ? new bootstrap.Modal(statusModalEl) : null;

    const tokenForm = /** @type {HTMLFormElement} */ (document.getElementById('oc-token-form'));
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

    // Cabecera elements
    const ocForm = /** @type {HTMLFormElement} */ (document.getElementById('oc-form'));
    const hdrId = /** @type {HTMLInputElement} */ (document.getElementById('oc-id'));
    const hdrContextoId = /** @type {HTMLInputElement} */ (document.getElementById('oc-contexto-id'));
    const hdrRev = /** @type {HTMLInputElement} */ (document.getElementById('oc-revision'));
    const hdrPedId = /** @type {HTMLInputElement} */ (document.getElementById('oc-ped-id'));
    const hdrCodigo = /** @type {HTMLInputElement} */ (document.getElementById('oc-codigo'));
    const hdrFecha = /** @type {HTMLInputElement} */ (document.getElementById('oc-fecha'));
    const hdrTipoProd = /** @type {HTMLSelectElement} */ (document.getElementById('oc-tipo-prod'));
    const hdrProvId = /** @type {HTMLInputElement} */ (document.getElementById('oc-prov-id'));
    const hdrProvText = /** @type {HTMLInputElement} */ (document.getElementById('oc-prov-text'));
    const provResults = document.getElementById('oc-prov-results');
    const hdrFechaEntrega = /** @type {HTMLInputElement} */ (document.getElementById('oc-fecha-entrega'));
    const hdrMoneda = /** @type {HTMLSelectElement} */ (document.getElementById('oc-moneda'));
    const hdrTc = /** @type {HTMLInputElement} */ (document.getElementById('oc-tc'));
    const hdrTasa = /** @type {HTMLSelectElement} */ (document.getElementById('oc-tasa'));
    const hdrCondicion = /** @type {HTMLSelectElement} */ (document.getElementById('oc-condicion'));
    const hdrPlazo = /** @type {HTMLInputElement} */ (document.getElementById('oc-plazo'));
    const hdrLugar = /** @type {HTMLInputElement} */ (document.getElementById('oc-lugar'));
    const hdrObserva = /** @type {HTMLTextAreaElement} */ (document.getElementById('oc-observa'));
    const estadoBadge = document.getElementById('oc-estado-badge');
    const aprobBadge = document.getElementById('oc-aprob-badge');
    const btnRequestAuth = document.getElementById('oc-btn-request-auth');

    const modalTitle = document.getElementById('oc-modal-title');
    const modalSubtitle = document.getElementById('oc-modal-subtitle');
    const tabDetail = document.querySelector('.js-oc-tab-detail');

    // Item elements
    const itemForm = /** @type {HTMLFormElement} */ (document.getElementById('oc-item-form'));
    const itemId = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-id'));
    const itemOcId = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-oc-id'));
    const itemProdId = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-prod-id'));
    const itemProdSearch = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-prod-search'));
    const itemProdResults = document.getElementById('oc-item-prod-results');
    const itemDesc = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-desc'));
    const itemCant = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-cant'));
    const itemVal = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-val'));
    const itemPre = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-pre'));
    const itemSubtotalCalc = /** @type {HTMLInputElement} */ (document.getElementById('oc-item-subtotal-calc'));
    const itemSubmitLbl = document.getElementById('oc-item-submit-lbl');
    const btnCancelItem = document.querySelector('.js-oc-cancel-item');
    const itemsTableBody = document.querySelector('#oc-items-table tbody');

    // Totals
    const totBruto = document.getElementById('oc-tot-bruto');
    const totIgv = document.getElementById('oc-tot-igv');
    const totTotal = document.getElementById('oc-tot-total');

    // Status modal elements
    const statusForm = /** @type {HTMLFormElement} */ (document.getElementById('oc-status-form'));
    const statusId = /** @type {HTMLInputElement} */ (document.getElementById('oc-status-id'));
    const statusRev = /** @type {HTMLInputElement} */ (document.getElementById('oc-status-rev'));
    const statusCode = document.getElementById('oc-status-code');
    const statusSel = /** @type {HTMLSelectElement} */ (document.getElementById('oc-status-sel'));

    let searchProvTimer = null;
    let searchProdTimer = null;
    let currentOrdenId = 0;
    let currentCabecera = null;

    // Proveedor search
    if (hdrProvText && provResults) {
        hdrProvText.addEventListener('input', function () {
            clearTimeout(searchProvTimer);
            const val = hdrProvText.value.trim();
            if (val.length < 2) {
                provResults.classList.add('d-none');
                provResults.innerHTML = '';
                return;
            }
            searchProvTimer = setTimeout(async () => {
                try {
                    const resp = await fetch(`/cm/pedidoCompra/buscarProveedores?search=${encodeURIComponent(val)}`);
                    const res = await resp.json();
                    if (res.success && res.data && res.data.length > 0) {
                        provResults.innerHTML = res.data.map(p =>
                            `<button type="button" class="list-group-item list-group-item-action py-1 px-2 small js-sel-prov" data-id="${p.id}" data-text="${p.descripcion} (${p.codigo})">
                                <strong>${p.descripcion}</strong> <span class="text-muted font-monospace ms-1">${p.codigo}</span>
                            </button>`
                        ).join('');
                        provResults.classList.remove('d-none');
                    } else {
                        provResults.innerHTML = '<div class="list-group-item py-1 px-2 small text-muted">No se encontraron proveedores.</div>';
                        provResults.classList.remove('d-none');
                    }
                } catch (e) {
                    console.error('Error buscando proveedores:', e);
                }
            }, 250);
        });

        provResults.addEventListener('click', function (e) {
            const btn = /** @type {HTMLElement} */ (e.target).closest('.js-sel-prov');
            if (btn) {
                hdrProvId.value = btn.dataset.id || '';
                hdrProvText.value = btn.dataset.text || '';
                provResults.classList.add('d-none');
            }
        });

        document.addEventListener('click', function (e) {
            if (!hdrProvText.contains(/** @type {Node} */(e.target)) && !provResults.contains(/** @type {Node} */(e.target))) {
                provResults.classList.add('d-none');
            }
        });
    }

    // Producto search
    if (itemProdSearch && itemProdResults) {
        itemProdSearch.addEventListener('input', function () {
            clearTimeout(searchProdTimer);
            const val = itemProdSearch.value.trim();
            if (val.length < 2) {
                itemProdResults.classList.add('d-none');
                itemProdResults.innerHTML = '';
                return;
            }
            const tipoId = hdrTipoProd ? hdrTipoProd.value : '';
            searchProdTimer = setTimeout(async () => {
                try {
                    const resp = await fetch(`/cm/pedidoCompra/buscarProductos?tipoProductoId=${tipoId}&search=${encodeURIComponent(val)}`);
                    const res = await resp.json();
                    if (res.success && res.data && res.data.length > 0) {
                        itemProdResults.innerHTML = res.data.map(p =>
                            `<button type="button" class="list-group-item list-group-item-action py-1 px-2 small js-sel-prod" data-id="${p.id}" data-desc="${p.descripcion}" data-code="${p.codigo}" data-um="${p.extra || ''}">
                                <strong>${p.codigo}</strong> - ${p.descripcion} <span class="badge bg-secondary-subtle text-muted float-end">${p.extra || 'UND'}</span>
                            </button>`
                        ).join('');
                        itemProdResults.classList.remove('d-none');
                    } else {
                        itemProdResults.innerHTML = '<div class="list-group-item py-1 px-2 small text-muted">No se encontraron productos.</div>';
                        itemProdResults.classList.remove('d-none');
                    }
                } catch (e) {
                    console.error('Error buscando productos:', e);
                }
            }, 250);
        });

        itemProdResults.addEventListener('click', function (e) {
            const btn = /** @type {HTMLElement} */ (e.target).closest('.js-sel-prod');
            if (btn) {
                itemProdId.value = btn.dataset.id || '';
                itemProdSearch.value = `${btn.dataset.code} - ${btn.dataset.desc}`;
                itemDesc.value = btn.dataset.desc || '';
                itemProdResults.classList.add('d-none');
            }
        });

        document.addEventListener('click', function (e) {
            if (!itemProdSearch.contains(/** @type {Node} */(e.target)) && !itemProdResults.contains(/** @type {Node} */(e.target))) {
                itemProdResults.classList.add('d-none');
            }
        });
    }

    // Auto calculate product line item prices
    function recalculateItemRow(source) {
        const cant = Number(itemCant.value) || 0;
        let val = Number(itemVal.value) || 0;
        let pre = Number(itemPre.value) || 0;
        const tasa = 1.18; // default IGV

        if (source === 'pre') {
            val = Number((pre / tasa).toFixed(4));
            itemVal.value = String(val);
        } else if (source === 'val') {
            pre = Number((val * tasa).toFixed(4));
            itemPre.value = String(pre);
        }

        const subtotal = (cant * pre).toFixed(2);
        if (itemSubtotalCalc) itemSubtotalCalc.value = subtotal;
    }

    itemCant?.addEventListener('input', () => recalculateItemRow('cant'));
    itemVal?.addEventListener('input', () => recalculateItemRow('val'));
    itemPre?.addEventListener('input', () => recalculateItemRow('pre'));

    function resetModal() {
        currentOrdenId = 0;
        currentCabecera = null;
        if (ocForm) ocForm.reset();
        hdrId.value = '0';
        hdrRev.value = '';
        hdrPedId.value = '';
        hdrCodigo.value = '[Autogenerado]';
        hdrFecha.value = new Date().toISOString().substring(0, 10);
        hdrProvId.value = '';
        hdrProvText.value = '';
        hdrTc.value = '1.0000';

        if (estadoBadge) {
            estadoBadge.className = 'badge bg-secondary-subtle text-muted border';
            estadoBadge.textContent = 'En Edición';
        }
        if (aprobBadge) {
            aprobBadge.className = 'badge bg-light text-dark border ms-1';
            aprobBadge.textContent = 'Sin Enviar';
        }
        if (btnRequestAuth) btnRequestAuth.classList.add('d-none');
        if (tabDetail) /** @type {HTMLElement} */(tabDetail).style.display = 'none';

        const tabHdr = document.getElementById('tab-oc-hdr');
        if (tabHdr) {
            // @ts-ignore
            new bootstrap.Tab(tabHdr).show();
        }

        resetItemEditor();
        if (itemsTableBody) {
            itemsTableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No hay productos en esta orden.</td></tr>';
        }
        updateTotalsDisplay(0, 0, 0);
    }

    function resetItemEditor() {
        if (!itemForm) return;
        itemForm.reset();
        itemId.value = '0';
        itemProdId.value = '';
        itemCant.value = '1.0000';
        itemVal.value = '0.0000';
        itemPre.value = '0.0000';
        if (itemSubtotalCalc) itemSubtotalCalc.value = '0.00';
        if (itemSubmitLbl) itemSubmitLbl.textContent = 'Agregar Producto';
        if (btnCancelItem) btnCancelItem.classList.add('d-none');
    }

    if (btnCancelItem) {
        btnCancelItem.addEventListener('click', resetItemEditor);
    }

    function updateTotalsDisplay(bruto, igv, total) {
        if (totBruto) totBruto.textContent = Number(bruto || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        if (totIgv) totIgv.textContent = Number(igv || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        if (totTotal) totTotal.textContent = Number(total || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // Load Detalle for modal
    async function abrirOrden(id) {
        resetModal();
        try {
            const resp = await fetch(`/cm/ordenCompra/obtener?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar la orden de compra.');
                return;
            }

            const data = res.data;
            const cab = data.cabecera;
            currentOrdenId = cab.id;
            currentCabecera = cab;

            // Populate selects
            if (hdrTipoProd && data.tiposProducto) {
                hdrTipoProd.innerHTML = '<option value="">Seleccione...</option>' +
                    data.tiposProducto.map(tp => `<option value="${tp.id}" ${tp.id === cab.tipoProductoId ? 'selected' : ''}>${tp.nombre}</option>`).join('');
            }
            if (hdrMoneda && data.monedas) {
                hdrMoneda.innerHTML = '<option value="">Seleccione...</option>' +
                    data.monedas.map(m => `<option value="${m.id}" ${m.id === cab.monedaId ? 'selected' : ''}>${m.nombre} (${m.codigo})</option>`).join('');
            }
            if (hdrCondicion && data.condicionesPago) {
                hdrCondicion.innerHTML = '<option value="">Seleccione...</option>' +
                    data.condicionesPago.map(cp => `<option value="${cp.id}" ${cp.id === cab.condicionPagoId ? 'selected' : ''}>${cp.nombre}</option>`).join('');
            }
            if (hdrTasa && data.tasasImpuesto) {
                hdrTasa.innerHTML = '<option value="">Seleccione...</option>' +
                    data.tasasImpuesto.map(t => `<option value="${t.id}" ${t.id === cab.impuestoTasaId ? 'selected' : ''}>${t.nombre}</option>`).join('');
            }

            if (cab.id > 0) {
                modalTitle.textContent = `Orden de Compra N° ${cab.codigo}`;
                modalSubtitle.textContent = `Registrada el ${cab.fechaTexto} · Estado: ${cab.estadoOrdenCompraNombre}`;
                hdrId.value = String(cab.id);
                hdrRev.value = cab.revision || '';
                hdrPedId.value = cab.pedidoCompraId ? String(cab.pedidoCompraId) : '';
                hdrCodigo.value = cab.codigo;
                hdrFecha.value = cab.fecha.substring(0, 10);
                if (cab.fechaEntrega) hdrFechaEntrega.value = cab.fechaEntrega.substring(0, 10);
                hdrProvId.value = String(cab.proveedorId);
                hdrProvText.value = cab.proveedorNombre + (cab.proveedorDocumento ? ` (${cab.proveedorDocumento})` : '');
                hdrTc.value = String(cab.tipoCambio || 1.0);
                hdrPlazo.value = cab.plazoEntrega || '';
                hdrLugar.value = cab.lugarEntrega || '';
                hdrObserva.value = cab.observacion || '';

                if (estadoBadge) {
                    estadoBadge.textContent = cab.estadoOrdenCompraNombre;
                    estadoBadge.className = 'badge border ' + (
                        cab.estadoOrdenCompraCodigo === '01' ? 'bg-secondary-subtle text-muted border-secondary-subtle' :
                        cab.estadoOrdenCompraCodigo === '02' ? 'bg-info-subtle text-info border-info-subtle' :
                        cab.estadoOrdenCompraCodigo === '03' ? 'bg-success-subtle text-success border-success-subtle' :
                        cab.estadoOrdenCompraCodigo === '04' ? 'bg-danger-subtle text-danger border-danger-subtle' : 'bg-light text-dark'
                    );
                }

                if (aprobBadge) {
                    aprobBadge.textContent = cab.estadoAprobacionNombre || 'SIN ENVIAR';
                    aprobBadge.className = 'badge ms-1 border ' + (
                        cab.estadoAprobacionCodigo === '02' ? 'bg-success-subtle text-success border-success-subtle' :
                        cab.estadoAprobacionCodigo === '03' ? 'bg-danger-subtle text-danger border-danger-subtle' :
                        cab.estadoAprobacionCodigo === '01' ? 'bg-warning-subtle text-warning border-warning-subtle' : 'bg-light text-dark'
                    );
                }

                if (btnRequestAuth) {
                    if (cab.estadoOrdenCompraCodigo === '01' || cab.estadoAprobacionCodigo === '03') {
                        btnRequestAuth.classList.remove('d-none');
                    } else {
                        btnRequestAuth.classList.add('d-none');
                    }
                }

                if (tabDetail) /** @type {HTMLElement} */(tabDetail).style.display = '';
                if (itemOcId) itemOcId.value = String(cab.id);

                renderProductos(data.productos || []);
                updateTotalsDisplay(cab.montoBruto, cab.montoImpuesto, cab.montoTotal);
            } else {
                modalTitle.textContent = 'Nueva Orden de Compra';
                modalSubtitle.textContent = 'Complete la información general y luego agregue los productos o insumos.';
                if (cab.tipoProductoId && hdrTipoProd) hdrTipoProd.value = String(cab.tipoProductoId);
                if (cab.proveedorId) {
                    hdrProvId.value = String(cab.proveedorId);
                    hdrProvText.value = cab.proveedorNombre;
                }
            }

            modal.show();
        } catch (e) {
            console.error('Error al abrir orden de compra:', e);
            notify('error', 'Error inesperado al cargar la orden de compra.');
        }
    }

    function renderProductos(productos) {
        if (!itemsTableBody) return;
        if (!productos || productos.length === 0) {
            itemsTableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No hay productos en esta orden.</td></tr>';
            return;
        }

        let i = 1;
        itemsTableBody.innerHTML = productos.map(item => `
            <tr>
                <td class="text-muted small">${i++}</td>
                <td class="font-monospace small fw-bold">${item.productoCodigo || '-'}</td>
                <td>
                    <div class="fw-semibold">${item.descripcion}</div>
                    ${item.esAdicional === 'S' ? '<span class="badge bg-info-subtle text-info border border-info-subtle small">Adicional</span>' : ''}
                </td>
                <td class="small text-muted">${item.unidadMedida || 'UND'}</td>
                <td class="text-end font-monospace fw-bold">${item.cantidadTexto || Number(item.cantidad).toFixed(2)}</td>
                <td class="text-end font-monospace small">${item.valorUnitarioTexto || Number(item.valorUnitario).toFixed(4)}</td>
                <td class="text-end font-monospace small">${item.precioUnitarioTexto || Number(item.precioUnitario).toFixed(4)}</td>
                <td class="text-end font-monospace fw-bold">${item.montoTotalTexto || Number(item.montoTotal).toFixed(2)}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-sm btn-outline-primary py-0 px-1 js-edit-item" data-item='${JSON.stringify(item)}' title="Editar item">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger py-0 px-1 js-del-item" data-id="${item.id}" title="Eliminar item">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            </tr>
        `).join('');
    }

    async function recargarProductos() {
        if (!currentOrdenId) return;
        try {
            const resp = await fetch(`/cm/ordenCompra/productos?ordenCompraId=${currentOrdenId}`);
            const res = await resp.json();
            if (res.success) {
                renderProductos(res.data);
                // Also fetch updated header totals
                const respHdr = await fetch(`/cm/ordenCompra/obtener?id=${currentOrdenId}`);
                const resHdr = await respHdr.json();
                if (resHdr.success && resHdr.data && resHdr.data.cabecera) {
                    const c = resHdr.data.cabecera;
                    updateTotalsDisplay(c.montoBruto, c.montoImpuesto, c.montoTotal);
                }
            }
        } catch (e) {
            console.error('Error al recargar productos:', e);
        }
    }

    // New button
    document.querySelector('.js-oc-new')?.addEventListener('click', () => abrirOrden(0));

    // Open existing buttons
    document.addEventListener('click', function (e) {
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-oc-open');
        if (btnOpen) {
            const id = Number(btnOpen.dataset.id) || 0;
            abrirOrden(id);
            return;
        }

        // Send to approval from listing
        const btnAuth = /** @type {HTMLElement} */ (e.target).closest('.js-oc-send-auth');
        if (btnAuth) {
            const id = Number(btnAuth.dataset.id) || 0;
            const rev = btnAuth.dataset.revision || '';
            const code = btnAuth.dataset.code || '';
            enviarAprobacion(id, rev, code);
            return;
        }

        // Change status from listing
        const btnStatus = /** @type {HTMLElement} */ (e.target).closest('.js-oc-change-status');
        if (btnStatus && statusModal) {
            statusId.value = btnStatus.dataset.id || '';
            statusRev.value = btnStatus.dataset.revision || '';
            if (statusCode) statusCode.textContent = btnStatus.dataset.code || '';
            if (statusSel && btnStatus.dataset.status) statusSel.value = btnStatus.dataset.status;
            statusModal.show();
            return;
        }

        // Delete from listing
        const btnDel = /** @type {HTMLElement} */ (e.target).closest('.js-oc-delete');
        if (btnDel) {
            const id = Number(btnDel.dataset.id) || 0;
            const rev = btnDel.dataset.revision || '';
            const code = btnDel.dataset.code || '';
            anularOrden(id, rev, code);
            return;
        }

        // Edit item in modal
        const btnEditItem = /** @type {HTMLElement} */ (e.target).closest('.js-edit-item');
        if (btnEditItem) {
            const raw = btnEditItem.dataset.item;
            if (raw) {
                const item = JSON.parse(raw);
                itemId.value = String(item.id);
                itemProdId.value = String(item.productoId);
                itemProdSearch.value = item.productoCodigo ? `${item.productoCodigo} - ${item.descripcion}` : item.descripcion;
                itemDesc.value = item.descripcion;
                itemCant.value = String(item.cantidad);
                itemVal.value = String(item.valorUnitario);
                itemPre.value = String(item.precioUnitario);
                if (itemSubtotalCalc) itemSubtotalCalc.value = Number(item.montoTotal).toFixed(2);
                if (itemSubmitLbl) itemSubmitLbl.textContent = 'Guardar Cambios';
                if (btnCancelItem) btnCancelItem.classList.remove('d-none');
            }
            return;
        }

        // Delete item in modal
        const btnDelItem = /** @type {HTMLElement} */ (e.target).closest('.js-del-item');
        if (btnDelItem) {
            const pId = Number(btnDelItem.dataset.id) || 0;
            eliminarItem(pId);
            return;
        }
    });

    // Save Cabecera
    ocForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        if (!hdrProvId.value || Number(hdrProvId.value) <= 0) {
            notify('warning', 'Debe seleccionar un proveedor válido.');
            return;
        }

        const formData = new FormData(ocForm);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/guardar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Cabecera guardada.');
                abrirOrden(res.id || currentOrdenId);
            } else {
                notify('error', res.message || 'No se pudo guardar la cabecera.');
            }
        } catch (err) {
            console.error('Error al guardar cabecera:', err);
            notify('error', 'Error al procesar la solicitud.');
        }
    });

    // Save Item
    itemForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        if (!currentOrdenId) {
            notify('warning', 'Guarde la cabecera de la orden antes de agregar productos.');
            return;
        }
        itemOcId.value = String(currentOrdenId);

        const formData = new FormData(itemForm);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/guardarProducto', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Producto registrado.');
                resetItemEditor();
                recargarProductos();
            } else {
                notify('error', res.message || 'No se pudo guardar el producto.');
            }
        } catch (err) {
            console.error('Error al guardar item:', err);
            notify('error', 'Error al guardar el producto.');
        }
    });

    // Delete item
    async function eliminarItem(id) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: '¿Eliminar producto?',
                text: 'El item seleccionado se removerá de la orden de compra.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) return;
        }

        const formData = new FormData();
        formData.append('id', String(id));
        formData.append('contextoId', hdrContextoId.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/eliminarProducto', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Item eliminado.');
                recargarProductos();
            } else {
                notify('error', res.message || 'No se pudo eliminar el item.');
            }
        } catch (e) {
            console.error('Error al eliminar item:', e);
            notify('error', 'Error de comunicación.');
        }
    }

    // Solicitar Aprobacion
    btnRequestAuth?.addEventListener('click', function () {
        if (!currentOrdenId || !currentCabecera) return;
        enviarAprobacion(currentOrdenId, currentCabecera.revision, currentCabecera.codigo);
    });

    async function enviarAprobacion(id, revision, codigo) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Solicitar aprobación de la Orden ${codigo}?`,
                text: 'La orden pasará a estado ORDEN y quedará PENDIENTE DE APROBACIÓN.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#0dcaf0',
                confirmButtonText: 'Sí, solicitar',
                cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) return;
        }

        const formData = new FormData();
        formData.append('id', String(id));
        formData.append('revision', revision);
        formData.append('contextoId', hdrContextoId.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/solicitarAprobacion', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Aprobación solicitada.');
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo enviar a aprobación.');
            }
        } catch (e) {
            console.error('Error al solicitar aprobación:', e);
            notify('error', 'Error de comunicación.');
        }
    }

    // Change status form
    statusForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        const formData = new FormData(statusForm);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/cambiarEstado', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Estado actualizado.');
                if (statusModal) statusModal.hide();
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo cambiar el estado.');
            }
        } catch (e) {
            console.error('Error al cambiar estado:', e);
            notify('error', 'Error de comunicación.');
        }
    });

    // Anular orden
    async function anularOrden(id, revision, codigo) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Anular Orden de Compra ${codigo}?`,
                text: 'Esta acción cancelará la orden de compra.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Sí, anular',
                cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) return;
        }

        const formData = new FormData();
        formData.append('id', String(id));
        formData.append('revision', revision);
        formData.append('contextoId', hdrContextoId.value);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/ordenCompra/anular', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Orden anulada.');
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo anular la orden.');
            }
        } catch (e) {
            console.error('Error al anular orden:', e);
            notify('error', 'Error de comunicación.');
        }
    }

})();

