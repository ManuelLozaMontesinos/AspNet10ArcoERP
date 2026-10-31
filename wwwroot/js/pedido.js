// @ts-check
(function () {
    'use strict';

    /**
     * @typedef {Object} PedidoConfig
     * @property {boolean} canWrite
     * @property {number} contextoId
     * @property {Object} urls
     * @property {string} urls.obtener
     * @property {string} urls.guardar
     * @property {string} urls.anular
     * @property {string} urls.cambiarACotizacion
     * @property {string} urls.cambiarEstado
     * @property {string} urls.productos
     * @property {string} urls.guardarProducto
     * @property {string} urls.eliminarProducto
     * @property {string} urls.buscarClientes
     * @property {string} urls.buscarProductos
     */

    const configEl = document.getElementById('pedido-config');
    if (!configEl) return;

    /** @type {PedidoConfig} */
    const config = JSON.parse(configEl.textContent || '{}');

    // Modals
    const pedModalEl = document.getElementById('ped-modal');
    if (!pedModalEl) return;
    // @ts-ignore
    const pedModal = new bootstrap.Modal(pedModalEl);

    const estModalEl = document.getElementById('ped-estado-modal');
    // @ts-ignore
    const estModal = estModalEl ? new bootstrap.Modal(estModalEl) : null;

    // Header Form Elements
    const headerForm = /** @type {HTMLFormElement} */ (document.getElementById('ped-header-form'));
    const hdrId = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-id'));
    const hdrRev = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-rev'));
    const hdrCodigo = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-codigo'));
    const hdrFecha = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-fecha'));
    const hdrFechaEnt = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-fecha-ent'));
    const hdrVendedor = /** @type {HTMLSelectElement} */ (document.getElementById('ped-hdr-vendedor'));
    const hdrClienteId = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-cliente-id'));
    const hdrClienteSearch = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-cliente-search'));
    const clientResults = document.getElementById('ped-client-results');
    const clientSelectedLabel = document.getElementById('ped-client-selected-label');
    const hdrMoneda = /** @type {HTMLSelectElement} */ (document.getElementById('ped-hdr-moneda'));
    const hdrCondicion = /** @type {HTMLSelectElement} */ (document.getElementById('ped-hdr-condicion'));
    const hdrLista = /** @type {HTMLSelectElement} */ (document.getElementById('ped-hdr-lista'));
    const hdrObs = /** @type {HTMLInputElement} */ (document.getElementById('ped-hdr-obs'));
    const btnSaveHdr = /** @type {HTMLButtonElement} */ (document.getElementById('ped-btn-save-hdr'));

    // Totals Elements
    const totBruto = document.getElementById('ped-tot-bruto');
    const totDcto = document.getElementById('ped-tot-dcto');
    const totIgv = document.getElementById('ped-tot-igv');
    const totTotal = document.getElementById('ped-tot-total');
    const totAnticipo = document.getElementById('ped-tot-anticipo');
    const totSaldo = document.getElementById('ped-tot-saldo');

    // Detail Elements
    const detailTabs = document.querySelectorAll('.js-ped-tab-detail');
    const btnTabItems = document.getElementById('tab-ped-items');
    const btnTabHdr = document.getElementById('tab-ped-hdr');

    // Item Form Elements
    const addItemCard = document.getElementById('ped-add-item-card');
    const itemForm = /** @type {HTMLFormElement} */ (document.getElementById('ped-item-form'));
    const itemPedId = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-pedid'));
    const itemPid = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-pid'));
    const itemDesc = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-desc'));
    const itemPermDcto = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-permdcto'));
    const itemSearch = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-search'));
    const prodResults = document.getElementById('ped-prod-results');
    const prodSelectedLabel = document.getElementById('ped-prod-selected-label');
    const itemCant = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-cant'));
    const itemPre = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-pre'));
    const itemVal = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-val'));
    const itemDcto = /** @type {HTMLInputElement} */ (document.getElementById('ped-item-dcto'));
    const btnAddItem = /** @type {HTMLButtonElement} */ (document.getElementById('ped-btn-add-item'));
    const itemsRows = document.getElementById('ped-items-rows');

    // Estado Form Elements
    const estForm = /** @type {HTMLFormElement} */ (document.getElementById('ped-estado-form'));
    const estId = /** @type {HTMLInputElement} */ (document.getElementById('ped-est-id'));
    const estRev = /** @type {HTMLInputElement} */ (document.getElementById('ped-est-rev'));
    const estCode = document.getElementById('ped-est-code');
    const estSel = /** @type {HTMLSelectElement} */ (document.getElementById('ped-est-sel'));

    // State
    let currentPedidoId = 0;
    let currentTasaImpuesto = 0.18;
    let clientSearchTimeout = null;
    let prodSearchTimeout = null;

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

    function resetHeaderForm() {
        if (!headerForm) return;
        headerForm.reset();
        hdrId.value = '0';
        hdrRev.value = '';
        hdrCodigo.value = '';
        hdrClienteId.value = '';
        if (hdrClienteSearch) hdrClienteSearch.value = '';
        if (clientSelectedLabel) clientSelectedLabel.textContent = '';
        if (hdrFecha) hdrFecha.value = new Date().toISOString().substring(0, 10);
        if (hdrFechaEnt) hdrFechaEnt.value = '';

        updateTotalsDisplay(0, 0, 0, 0, 0);

        currentPedidoId = 0;
        detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = 'none');
        setHeaderInputsDisabled(false);
    }

    function setHeaderInputsDisabled(disabled) {
        if (!headerForm) return;
        const inputs = headerForm.querySelectorAll('input:not([type="hidden"]), select, textarea');
        inputs.forEach(el => {
            const input = /** @type {HTMLInputElement|HTMLSelectElement} */ (el);
            if (input !== hdrCodigo) {
                input.disabled = disabled;
            }
        });
        if (btnSaveHdr) btnSaveHdr.disabled = disabled;
    }

    function updateTotalsDisplay(bruto, dcto, igv, total, anticipo) {
        if (totBruto) totBruto.textContent = formatNumber(bruto);
        if (totDcto) totDcto.textContent = formatNumber(dcto);
        if (totIgv) totIgv.textContent = formatNumber(igv);
        if (totTotal) totTotal.textContent = formatNumber(total);
        if (totAnticipo) totAnticipo.textContent = formatNumber(anticipo);
        if (totSaldo) {
            const saldo = Math.max(0, (Number(total) || 0) - (Number(anticipo) || 0));
            totSaldo.textContent = formatNumber(saldo);
        }
    }

    async function cargarPedido(id) {
        try {
            const resp = await fetch(`${config.urls.obtener}?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar el pedido.');
                return;
            }

            const data = res.data.cabecera;
            currentPedidoId = data.id;

            hdrId.value = String(data.id);
            hdrRev.value = data.revision || '';
            hdrCodigo.value = data.codigo || '';
            if (hdrFecha) hdrFecha.value = data.fecha ? data.fecha.substring(0, 10) : '';
            if (hdrFechaEnt) hdrFechaEnt.value = data.fechaEntrega ? data.fechaEntrega.substring(0, 10) : '';
            if (hdrVendedor) hdrVendedor.value = String(data.vendedorId || '');
            if (hdrClienteId) hdrClienteId.value = String(data.clienteId || '');
            if (hdrClienteSearch) hdrClienteSearch.value = data.clienteNombre || '';
            if (clientSelectedLabel) {
                clientSelectedLabel.textContent = data.clienteDocumento
                    ? `[${data.clienteDocumento}] ${data.clienteNombre}`
                    : (data.clienteNombre || '');
            }
            if (hdrMoneda) hdrMoneda.value = String(data.monedaId || '');
            if (hdrCondicion) hdrCondicion.value = String(data.tipoCondicionPagoId || '');
            if (hdrLista) hdrLista.value = String(data.listaPrecioId || '');
            if (hdrObs) hdrObs.value = data.observacion || '';

            updateTotalsDisplay(data.montoBruto, data.montoDescuento, data.montoIgv, data.montoTotal, data.montoAnticipo);

            const isEditable = config.canWrite && data.estadoCotizacionCodigo === '02' && data.estado === 'A';
            setHeaderInputsDisabled(!isEditable);

            if (addItemCard) addItemCard.style.display = isEditable ? '' : 'none';

            detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = '');
            if (itemPedId) itemPedId.value = String(data.id);

            resetItemForm();
            await cargarProductos(data.id, isEditable);

            if (btnTabHdr) {
                // @ts-ignore
                const tab = new bootstrap.Tab(btnTabHdr);
                tab.show();
            }

            const modalTitle = document.getElementById('ped-modal-title');
            const modalSubtitle = document.getElementById('ped-modal-subtitle');
            if (modalTitle) modalTitle.textContent = `Pedido ${data.codigo}`;
            if (modalSubtitle) modalSubtitle.textContent = `Estado: ${data.estadoCotizacionNombre} · Cliente: ${data.clienteNombre}`;

            pedModal.show();
        } catch (err) {
            console.error(err);
            notify('error', 'Error al consultar el pedido.');
        }
    }

    async function cargarProductos(pedidoId, isEditable = true) {
        if (!itemsRows) return;
        itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando ítems...</td></tr>';

        try {
            const resp = await fetch(`${config.urls.productos}?pedidoId=${pedidoId}`);
            const res = await resp.json();
            if (!res.success) {
                itemsRows.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-3">${res.message || 'Error al cargar ítems.'}</td></tr>`;
                return;
            }

            const list = res.data || [];
            if (list.length === 0) {
                itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">No hay productos agregados a este pedido.</td></tr>';
                return;
            }

            let html = '';
            list.forEach(item => {
                html += `
                    <tr>
                        <td class="font-monospace small">${escapeHtml(item.codigo || item.productoCodigo || '')}</td>
                        <td>
                            <div class="fw-semibold">${escapeHtml(item.descripcion || item.productoNombre || '')}</div>
                            ${item.observacion ? `<small class="text-muted">${escapeHtml(item.observacion)}</small>` : ''}
                        </td>
                        <td class="small text-muted">${escapeHtml(item.unidadMedida || item.unidadMedidaCodigo || '')}</td>
                        <td class="text-end font-monospace">${formatNumber(item.cantidad, 2)}</td>
                        <td class="text-end font-monospace">${formatNumber(item.precioUnitario, 4)}</td>
                        <td class="text-end font-monospace text-muted">${formatNumber(item.porcentajeDescuento, 2)}%</td>
                        <td class="text-end font-monospace fw-bold text-primary">${formatNumber(item.montoNeto, 2)}</td>
                        <td class="text-center">
                            ${isEditable ? `
                                <button type="button" class="btn btn-sm btn-outline-danger js-ped-del-item" data-id="${item.id}" title="Eliminar ítem">
                                    <i class="bi bi-trash"></i>
                                </button>
                            ` : '-'}
                        </td>
                    </tr>
                `;
            });
            itemsRows.innerHTML = html;
        } catch (err) {
            console.error(err);
            itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-3">Error al consultar los productos.</td></tr>';
        }
    }

    function resetItemForm() {
        if (!itemForm) return;
        itemForm.reset();
        if (itemPid) itemPid.value = '';
        if (itemDesc) itemDesc.value = '';
        if (itemPermDcto) itemPermDcto.value = 'N';
        if (itemVal) itemVal.value = '';
        if (itemPre) itemPre.value = '';
        if (itemCant) itemCant.value = '1.00';
        if (itemDcto) itemDcto.value = '0.00';
        if (itemSearch) itemSearch.value = '';
        if (prodSelectedLabel) prodSelectedLabel.textContent = '';
        if (btnAddItem) btnAddItem.disabled = true;
        if (prodResults) prodResults.style.display = 'none';
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

    // --- Search Autocomplete: Clientes ---
    if (hdrClienteSearch) {
        hdrClienteSearch.addEventListener('input', function () {
            const term = this.value.trim();
            if (clientSearchTimeout) clearTimeout(clientSearchTimeout);
            if (term.length < 2) {
                if (clientResults) clientResults.style.display = 'none';
                return;
            }
            clientSearchTimeout = setTimeout(() => buscarClientes(term), 300);
        });

        const btnFindClient = document.getElementById('ped-btn-find-client');
        if (btnFindClient) {
            btnFindClient.addEventListener('click', () => {
                const term = hdrClienteSearch.value.trim();
                buscarClientes(term);
            });
        }
    }

    async function buscarClientes(term) {
        if (!clientResults) return;
        try {
            const resp = await fetch(`${config.urls.buscarClientes}?search=${encodeURIComponent(term)}`);
            const res = await resp.json();
            if (!res.success || !res.data || res.data.length === 0) {
                clientResults.innerHTML = '<div class="list-group-item small text-muted">No se encontraron clientes.</div>';
                clientResults.style.display = 'block';
                return;
            }

            let html = '';
            res.data.forEach(c => {
                const docBadge = c.documento ? `<span class="badge bg-secondary me-1">${escapeHtml(c.documento)}</span>` : '';
                html += `
                    <a href="#" class="list-group-item list-group-item-action py-1 px-2 small js-pick-client"
                       data-id="${c.id}" data-doc="${escapeHtml(c.documento || '')}" data-nombre="${escapeHtml(c.nombre || '')}"
                       data-condicion="${c.tipoCondicionPagoId || ''}">
                        ${docBadge}<strong>${escapeHtml(c.nombre)}</strong>
                    </a>
                `;
            });
            clientResults.innerHTML = html;
            clientResults.style.display = 'block';
        } catch (e) {
            console.error(e);
        }
    }

    document.addEventListener('click', function (e) {
        // Pick Client
        const pickClientEl = /** @type {HTMLElement} */ (e.target).closest('.js-pick-client');
        if (pickClientEl) {
            e.preventDefault();
            const id = pickClientEl.getAttribute('data-id') || '';
            const doc = pickClientEl.getAttribute('data-doc') || '';
            const nombre = pickClientEl.getAttribute('data-nombre') || '';
            const cond = pickClientEl.getAttribute('data-condicion') || '';

            hdrClienteId.value = id;
            if (hdrClienteSearch) hdrClienteSearch.value = nombre;
            if (clientSelectedLabel) clientSelectedLabel.textContent = doc ? `[${doc}] ${nombre}` : nombre;
            if (cond && hdrCondicion && !hdrCondicion.value) {
                hdrCondicion.value = cond;
            }
            if (clientResults) clientResults.style.display = 'none';
            return;
        }

        if (clientResults && clientResults.style.display !== 'none') {
            if (!clientResults.contains(/** @type {Node} */(e.target)) && e.target !== hdrClienteSearch) {
                clientResults.style.display = 'none';
            }
        }

        // Pick Product
        const pickProdEl = /** @type {HTMLElement} */ (e.target).closest('.js-pick-prod');
        if (pickProdEl) {
            e.preventDefault();
            const id = pickProdEl.getAttribute('data-id') || '';
            const desc = pickProdEl.getAttribute('data-desc') || '';
            const val = pickProdEl.getAttribute('data-val') || '0';
            const pre = pickProdEl.getAttribute('data-pre') || '0';
            const permdcto = pickProdEl.getAttribute('data-permdcto') || 'N';
            const tasa = Number(pickProdEl.getAttribute('data-tasa')) || 18;

            currentTasaImpuesto = tasa / 100.0;
            if (itemPid) itemPid.value = id;
            if (itemDesc) itemDesc.value = desc;
            if (itemVal) itemVal.value = val;
            if (itemPre) itemPre.value = Number(pre).toFixed(4);
            if (itemPermDcto) itemPermDcto.value = permdcto;
            if (prodSelectedLabel) prodSelectedLabel.textContent = desc;
            if (itemSearch) itemSearch.value = desc;
            if (btnAddItem) btnAddItem.disabled = false;
            if (prodResults) prodResults.style.display = 'none';

            if (itemCant) {
                itemCant.focus();
                itemCant.select();
            }
            return;
        }

        if (prodResults && prodResults.style.display !== 'none') {
            if (!prodResults.contains(/** @type {Node} */(e.target)) && e.target !== itemSearch) {
                prodResults.style.display = 'none';
            }
        }
    });

    // --- Search Autocomplete: Productos ---
    if (itemSearch) {
        itemSearch.addEventListener('input', function () {
            const term = this.value.trim();
            if (prodSearchTimeout) clearTimeout(prodSearchTimeout);
            if (term.length < 2) {
                if (prodResults) prodResults.style.display = 'none';
                return;
            }
            prodSearchTimeout = setTimeout(() => buscarProductos(term), 300);
        });

        const btnFindProd = document.getElementById('ped-btn-find-prod');
        if (btnFindProd) {
            btnFindProd.addEventListener('click', () => {
                const term = itemSearch.value.trim();
                buscarProductos(term);
            });
        }
    }

    async function buscarProductos(term) {
        if (!prodResults) return;
        const listaId = hdrLista ? Number(hdrLista.value) : 0;
        if (!listaId) {
            notify('warning', 'Debe seleccionar una lista de precios en los datos generales.');
            return;
        }

        try {
            const resp = await fetch(`${config.urls.buscarProductos}?listaPrecioId=${listaId}&search=${encodeURIComponent(term)}`);
            const res = await resp.json();
            if (!res.success || !res.data || res.data.length === 0) {
                prodResults.innerHTML = '<div class="list-group-item small text-muted">No se encontraron productos en la lista.</div>';
                prodResults.style.display = 'block';
                return;
            }

            let html = '';
            res.data.forEach(p => {
                const codeBadge = p.codigo ? `<span class="badge bg-secondary font-monospace me-1">${escapeHtml(p.codigo)}</span>` : '';
                const stockBadge = p.stockActual > 0 ? `<span class="badge bg-success-subtle text-success border border-success-subtle ms-1">Stock: ${p.stockActual}</span>` : '';
                html += `
                    <a href="#" class="list-group-item list-group-item-action py-1 px-2 small js-pick-prod"
                       data-id="${p.productoId}" data-desc="${escapeHtml(p.nombre)}" data-val="${p.valorUnitario}"
                       data-pre="${p.precioUnitario}" data-permdcto="${p.permiteDescuento ? 'S' : 'N'}" data-tasa="${p.impuestoTasa}">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>${codeBadge}<strong>${escapeHtml(p.nombre)}</strong>${stockBadge}</div>
                            <span class="font-monospace text-primary fw-bold ms-2">S/ ${formatNumber(p.precioUnitario, 2)}</span>
                        </div>
                    </a>
                `;
            });
            prodResults.innerHTML = html;
            prodResults.style.display = 'block';
        } catch (e) {
            console.error(e);
        }
    }

    if (itemPre) {
        itemPre.addEventListener('input', function () {
            const pre = Number(this.value) || 0;
            if (currentTasaImpuesto >= 0 && itemVal) {
                const val = pre / (1.0 + currentTasaImpuesto);
                itemVal.value = val.toFixed(6);
            }
        });
    }

    // --- Header Form Save ---
    if (headerForm) {
        headerForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (!headerForm.checkValidity()) {
                headerForm.reportValidity();
                return;
            }
            if (!hdrClienteId.value || hdrClienteId.value === '0') {
                notify('warning', 'Debe seleccionar un cliente.');
                if (hdrClienteSearch) hdrClienteSearch.focus();
                return;
            }

            if (btnSaveHdr) btnSaveHdr.disabled = true;
            try {
                const formData = new FormData(headerForm);
                const resp = await fetch(config.urls.guardar, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo guardar el pedido.');
                    return;
                }

                notify('success', res.message || 'Pedido guardado exitosamente.');

                const isNew = Number(hdrId.value) === 0;
                if (res.id) {
                    hdrId.value = String(res.id);
                    currentPedidoId = res.id;
                    if (itemPedId) itemPedId.value = String(res.id);
                }
                if (res.codigo) hdrCodigo.value = res.codigo;
                if (res.revision) hdrRev.value = res.revision;

                detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = '');

                const modalTitle = document.getElementById('ped-modal-title');
                if (modalTitle) modalTitle.textContent = `Pedido ${hdrCodigo.value}`;

                if (isNew && btnTabItems) {
                    // @ts-ignore
                    const tab = new bootstrap.Tab(btnTabItems);
                    tab.show();
                    resetItemForm();
                    await cargarProductos(currentPedidoId, true);
                    if (itemSearch) itemSearch.focus();
                }
            } catch (err) {
                console.error(err);
                notify('error', 'Error de comunicación al guardar cabecera del pedido.');
            } finally {
                if (btnSaveHdr) btnSaveHdr.disabled = false;
            }
        });
    }

    // --- Product Form Add ---
    if (itemForm) {
        itemForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (!itemForm.checkValidity()) {
                itemForm.reportValidity();
                return;
            }
            if (!itemPid.value || itemPid.value === '0') {
                notify('warning', 'Debe buscar y seleccionar un producto.');
                if (itemSearch) itemSearch.focus();
                return;
            }

            if (btnAddItem) btnAddItem.disabled = true;
            try {
                const formData = new FormData(itemForm);
                const resp = await fetch(config.urls.guardarProducto, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo agregar el producto.');
                    return;
                }

                notify('success', 'Producto agregado con éxito.');
                resetItemForm();
                await cargarProductos(currentPedidoId, true);

                // Update totals
                const headerResp = await fetch(`${config.urls.obtener}?id=${currentPedidoId}`);
                const headerRes = await headerResp.json();
                if (headerRes.success && headerRes.data && headerRes.data.cabecera) {
                    const c = headerRes.data.cabecera;
                    updateTotalsDisplay(c.montoBruto, c.montoDescuento, c.montoIgv, c.montoTotal, c.montoAnticipo);
                    if (hdrRev) hdrRev.value = c.revision || '';
                }

                if (itemSearch) itemSearch.focus();
            } catch (err) {
                console.error(err);
                notify('error', 'Error de comunicación al agregar producto.');
            } finally {
                if (btnAddItem) btnAddItem.disabled = false;
            }
        });
    }

    // --- Delete Product from Items Grid ---
    if (itemsRows) {
        itemsRows.addEventListener('click', async function (e) {
            const delBtn = /** @type {HTMLElement} */ (e.target).closest('.js-ped-del-item');
            if (!delBtn) return;

            const itemId = Number(delBtn.getAttribute('data-id')) || 0;
            if (!itemId) return;

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const swalRes = await Swal.fire({
                    title: '¿Eliminar ítem?',
                    text: 'Se quitará este producto del pedido.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, eliminar',
                    cancelButtonText: 'Cancelar'
                });
                if (!swalRes.isConfirmed) return;
            } else if (!confirm('¿Eliminar este ítem del pedido?')) {
                return;
            }

            try {
                const formData = new FormData();
                formData.append('id', String(itemId));
                formData.append('contextoId', String(config.contextoId));

                const resp = await fetch(config.urls.eliminarProducto, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo eliminar el ítem.');
                    return;
                }

                notify('success', 'Ítem eliminado con éxito.');
                await cargarProductos(currentPedidoId, true);

                const headerResp = await fetch(`${config.urls.obtener}?id=${currentPedidoId}`);
                const headerRes = await headerResp.json();
                if (headerRes.success && headerRes.data && headerRes.data.cabecera) {
                    const c = headerRes.data.cabecera;
                    updateTotalsDisplay(c.montoBruto, c.montoDescuento, c.montoIgv, c.montoTotal, c.montoAnticipo);
                    if (hdrRev) hdrRev.value = c.revision || '';
                }
            } catch (err) {
                console.error(err);
                notify('error', 'Error al eliminar el ítem.');
            }
        });
    }

    // --- Estado Form Submit ---
    if (estForm) {
        estForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            try {
                const formData = new FormData(estForm);
                const resp = await fetch(config.urls.cambiarEstado, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo cambiar el estado del pedido.');
                    return;
                }

                if (estModal) estModal.hide();
                notify('success', res.message || 'Estado actualizado.');
                setTimeout(() => window.location.reload(), 800);
            } catch (err) {
                console.error(err);
                notify('error', 'Error al cambiar estado.');
            }
        });
    }

    // --- Listing Actions ---
    document.addEventListener('click', async function (e) {
        // Open New Order
        const btnNew = /** @type {HTMLElement} */ (e.target).closest('.js-ped-new');
        if (btnNew) {
            e.preventDefault();
            resetHeaderForm();
            const modalTitle = document.getElementById('ped-modal-title');
            const modalSubtitle = document.getElementById('ped-modal-subtitle');
            if (modalTitle) modalTitle.textContent = 'Nuevo Pedido de Venta';
            if (modalSubtitle) modalSubtitle.textContent = 'Ingrese los datos generales para crear la orden.';

            if (btnTabHdr) {
                // @ts-ignore
                const tab = new bootstrap.Tab(btnTabHdr);
                tab.show();
            }
            pedModal.show();
            return;
        }

        // Open Existing Order
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-ped-open');
        if (btnOpen) {
            e.preventDefault();
            const id = Number(btnOpen.getAttribute('data-id')) || 0;
            if (id > 0) {
                await cargarPedido(id);
            }
            return;
        }

        // Revert to Quotation
        const btnToCot = /** @type {HTMLElement} */ (e.target).closest('.js-ped-to-cot');
        if (btnToCot) {
            e.preventDefault();
            const id = Number(btnToCot.getAttribute('data-id')) || 0;
            const rev = btnToCot.getAttribute('data-revision') || '';
            const code = btnToCot.getAttribute('data-code') || '';

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const swalRes = await Swal.fire({
                    title: `¿Revertir pedido ${code} a Cotización?`,
                    text: 'El documento dejará de ser una orden de venta activa y volverá al estado de cotización preliminar.',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, revertir a cotización',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#ffc107'
                });
                if (!swalRes.isConfirmed) return;
            } else if (!confirm(`¿Revertir el pedido ${code} a cotización?`)) {
                return;
            }

            try {
                const formData = new FormData();
                formData.append('id', String(id));
                formData.append('revision', rev);
                formData.append('contextoId', String(config.contextoId));

                const resp = await fetch(config.urls.cambiarACotizacion, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo revertir el pedido.');
                    return;
                }

                notify('success', res.message || 'Pedido revertido a cotización con éxito.');
                setTimeout(() => window.location.reload(), 800);
            } catch (err) {
                console.error(err);
                notify('error', 'Error al revertir el pedido.');
            }
            return;
        }

        // Change Status Modal Open
        const btnChangeStatus = /** @type {HTMLElement} */ (e.target).closest('.js-ped-change-status');
        if (btnChangeStatus && estModal) {
            e.preventDefault();
            const id = Number(btnChangeStatus.getAttribute('data-id')) || 0;
            const rev = btnChangeStatus.getAttribute('data-revision') || '';
            const code = btnChangeStatus.getAttribute('data-code') || '';
            const status = btnChangeStatus.getAttribute('data-status') || '';

            if (estId) estId.value = String(id);
            if (estRev) estRev.value = rev;
            if (estCode) estCode.textContent = code;
            if (estSel && status) estSel.value = status;

            estModal.show();
            return;
        }

        // Annul Order
        const btnDel = /** @type {HTMLElement} */ (e.target).closest('.js-ped-delete');
        if (btnDel) {
            e.preventDefault();
            const id = Number(btnDel.getAttribute('data-id')) || 0;
            const rev = btnDel.getAttribute('data-revision') || '';
            const code = btnDel.getAttribute('data-code') || '';

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const swalRes = await Swal.fire({
                    title: `¿Anular pedido ${code}?`,
                    text: 'Esta acción no se puede deshacer y el pedido pasará a estado ANULADO.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, anular pedido',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#dc3545'
                });
                if (!swalRes.isConfirmed) return;
            } else if (!confirm(`¿Anular el pedido ${code}?`)) {
                return;
            }

            try {
                const formData = new FormData();
                formData.append('id', String(id));
                formData.append('revision', rev);
                formData.append('contextoId', String(config.contextoId));

                const resp = await fetch(config.urls.anular, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo anular el pedido.');
                    return;
                }

                notify('success', res.message || 'Pedido anulado con éxito.');
                setTimeout(() => window.location.reload(), 800);
            } catch (err) {
                console.error(err);
                notify('error', 'Error al anular el pedido.');
            }
            return;
        }
    });

})();
