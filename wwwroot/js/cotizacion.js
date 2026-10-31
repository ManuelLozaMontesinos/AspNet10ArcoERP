// @ts-check
(function () {
    'use strict';

    /**
     * @typedef {Object} CotizacionConfig
     * @property {boolean} canWrite
     * @property {number} contextoId
     * @property {Object} urls
     * @property {string} urls.obtener
     * @property {string} urls.guardar
     * @property {string} urls.anular
     * @property {string} urls.productos
     * @property {string} urls.guardarProducto
     * @property {string} urls.eliminarProducto
     * @property {string} urls.generarPedido
     * @property {string} urls.buscarClientes
     * @property {string} urls.buscarProductos
     */

    const configEl = document.getElementById('cotizacion-config');
    if (!configEl) return;

    /** @type {CotizacionConfig} */
    const config = JSON.parse(configEl.textContent || '{}');

    // Modals
    const cotModalEl = document.getElementById('cot-modal');
    if (!cotModalEl) return;
    // @ts-ignore
    const cotModal = new bootstrap.Modal(cotModalEl);

    const pedModalEl = document.getElementById('cot-pedido-modal');
    // @ts-ignore
    const pedModal = pedModalEl ? new bootstrap.Modal(pedModalEl) : null;

    // Header Form Elements
    const headerForm = /** @type {HTMLFormElement} */ (document.getElementById('cot-header-form'));
    const hdrId = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-id'));
    const hdrRev = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-rev'));
    const hdrCodigo = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-codigo'));
    const hdrFecha = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-fecha'));
    const hdrVendedor = /** @type {HTMLSelectElement} */ (document.getElementById('cot-hdr-vendedor'));
    const hdrClienteId = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-cliente-id'));
    const hdrClienteSearch = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-cliente-search'));
    const clientResults = document.getElementById('cot-client-results');
    const clientSelectedLabel = document.getElementById('cot-client-selected-label');
    const hdrMoneda = /** @type {HTMLSelectElement} */ (document.getElementById('cot-hdr-moneda'));
    const hdrCondicion = /** @type {HTMLSelectElement} */ (document.getElementById('cot-hdr-condicion'));
    const hdrLista = /** @type {HTMLSelectElement} */ (document.getElementById('cot-hdr-lista'));
    const hdrObs = /** @type {HTMLInputElement} */ (document.getElementById('cot-hdr-obs'));
    const btnSaveHdr = /** @type {HTMLButtonElement} */ (document.getElementById('cot-btn-save-hdr'));

    // Totals Elements
    const totBruto = document.getElementById('cot-tot-bruto');
    const totDcto = document.getElementById('cot-tot-dcto');
    const totIgv = document.getElementById('cot-tot-igv');
    const totTotal = document.getElementById('cot-tot-total');

    // Detail Elements
    const detailTabs = document.querySelectorAll('.js-cot-tab-detail');
    const btnTabItems = document.getElementById('tab-cot-items');
    const btnTabHdr = document.getElementById('tab-cot-hdr');
    const btnModalPedido = /** @type {HTMLButtonElement} */ (document.getElementById('cot-btn-modal-pedido'));

    // Item Form Elements
    const addItemCard = document.getElementById('cot-add-item-card');
    const itemForm = /** @type {HTMLFormElement} */ (document.getElementById('cot-item-form'));
    const itemCotId = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-cotid'));
    const itemPid = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-pid'));
    const itemDesc = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-desc'));
    const itemPermDcto = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-permdcto'));
    const itemSearch = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-search'));
    const prodResults = document.getElementById('cot-prod-results');
    const prodSelectedLabel = document.getElementById('cot-prod-selected-label');
    const itemCant = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-cant'));
    const itemPre = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-pre'));
    const itemVal = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-val'));
    const itemDcto = /** @type {HTMLInputElement} */ (document.getElementById('cot-item-dcto'));
    const btnAddItem = /** @type {HTMLButtonElement} */ (document.getElementById('cot-btn-add-item'));
    const itemsRows = document.getElementById('cot-items-rows');

    // Pedido Form Elements
    const pedForm = /** @type {HTMLFormElement} */ (document.getElementById('cot-pedido-form'));
    const pedId = /** @type {HTMLInputElement} */ (document.getElementById('cot-ped-id'));
    const pedCode = document.getElementById('cot-ped-code');

    // Current Quotation State
    let currentCotizacionId = 0;
    let currentTasaImpuesto = 0.18;
    let clientSearchTimeout = null;
    let prodSearchTimeout = null;

    // Helpers
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

        if (totBruto) totBruto.textContent = '0.00';
        if (totDcto) totDcto.textContent = '0.00';
        if (totIgv) totIgv.textContent = '0.00';
        if (totTotal) totTotal.textContent = '0.00';

        currentCotizacionId = 0;
        detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = 'none');
        if (btnModalPedido) btnModalPedido.style.display = 'none';

        // Set inputs editable
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

    function updateTotalsDisplay(bruto, dcto, igv, total) {
        if (totBruto) totBruto.textContent = formatNumber(bruto);
        if (totDcto) totDcto.textContent = formatNumber(dcto);
        if (totIgv) totIgv.textContent = formatNumber(igv);
        if (totTotal) totTotal.textContent = formatNumber(total);
    }

    // Load Quotation
    async function cargarCotizacion(id) {
        try {
            const resp = await fetch(`${config.urls.obtener}?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar la cotización.');
                return;
            }

            const data = res.data;
            currentCotizacionId = data.id;

            hdrId.value = String(data.id);
            hdrRev.value = data.revision || '';
            hdrCodigo.value = data.codigo || '';
            if (hdrFecha) hdrFecha.value = data.fecha ? data.fecha.substring(0, 10) : '';
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

            updateTotalsDisplay(data.montoBruto, data.montoDctoItem, data.montoIgv, data.montoTotal);

            const isEditable = config.canWrite && data.estadoCotizacionCodigo === '01' && data.estado === 'A';
            setHeaderInputsDisabled(!isEditable);

            if (addItemCard) addItemCard.style.display = isEditable ? '' : 'none';
            if (btnModalPedido) {
                btnModalPedido.style.display = (config.canWrite && data.estadoCotizacionCodigo === '01' && data.estado === 'A') ? '' : 'none';
            }

            // Show items tab and load items
            detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = '');
            if (itemCotId) itemCotId.value = String(data.id);

            resetItemForm();
            await cargarProductos(data.id, isEditable);

            // Switch to header tab
            if (btnTabHdr) {
                // @ts-ignore
                const tab = new bootstrap.Tab(btnTabHdr);
                tab.show();
            }

            const modalTitle = document.getElementById('cot-modal-title');
            const modalSubtitle = document.getElementById('cot-modal-subtitle');
            if (modalTitle) modalTitle.textContent = `Cotización ${data.codigo}`;
            if (modalSubtitle) modalSubtitle.textContent = `Estado: ${data.estadoCotizacionNombre} · Cliente: ${data.clienteNombre}`;

            cotModal.show();
        } catch (err) {
            console.error(err);
            notify('error', 'Error al consultar la cotización.');
        }
    }

    // Load Products Grid
    async function cargarProductos(cotizacionId, isEditable = true) {
        if (!itemsRows) return;
        itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando ítems...</td></tr>';

        try {
            const resp = await fetch(`${config.urls.productos}?cotizacionId=${cotizacionId}`);
            const res = await resp.json();
            if (!res.success) {
                itemsRows.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-3">${res.message || 'Error al cargar ítems.'}</td></tr>`;
                return;
            }

            const list = res.data || [];
            if (list.length === 0) {
                itemsRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">No hay productos agregados a esta cotización.</td></tr>';
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
                                <button type="button" class="btn btn-sm btn-outline-danger js-cot-del-item" data-id="${item.id}" title="Eliminar ítem">
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

        const btnFindClient = document.getElementById('cot-btn-find-client');
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

        // Close Client Search if clicking outside
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

        // Close Product Search if clicking outside
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

        const btnFindProd = document.getElementById('cot-btn-find-prod');
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

    // Dynamic price recalculation when user changes PrecioUnitario in Item form
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
                    notify('error', res.message || 'No se pudo guardar la cotización.');
                    return;
                }

                notify('success', res.message || 'Cotización guardada exitosamente.');

                const isNew = Number(hdrId.value) === 0;
                if (res.id) {
                    hdrId.value = String(res.id);
                    currentCotizacionId = res.id;
                    if (itemCotId) itemCotId.value = String(res.id);
                }
                if (res.codigo) hdrCodigo.value = res.codigo;
                if (res.revision) hdrRev.value = res.revision;

                detailTabs.forEach(el => /** @type {HTMLElement} */(el).style.display = '');

                const modalTitle = document.getElementById('cot-modal-title');
                if (modalTitle) modalTitle.textContent = `Cotización ${hdrCodigo.value}`;

                if (isNew && btnTabItems) {
                    // Auto switch to products tab so user can immediately add products!
                    // @ts-ignore
                    const tab = new bootstrap.Tab(btnTabItems);
                    tab.show();
                    resetItemForm();
                    await cargarProductos(currentCotizacionId, true);
                    if (itemSearch) itemSearch.focus();
                }
            } catch (err) {
                console.error(err);
                notify('error', 'Error de comunicación al guardar la cabecera.');
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
                await cargarProductos(currentCotizacionId, true);

                // Update totals in header display
                const headerResp = await fetch(`${config.urls.obtener}?id=${currentCotizacionId}`);
                const headerRes = await headerResp.json();
                if (headerRes.success && headerRes.data) {
                    updateTotalsDisplay(headerRes.data.montoBruto, headerRes.data.montoDctoItem, headerRes.data.montoIgv, headerRes.data.montoTotal);
                    if (hdrRev) hdrRev.value = headerRes.data.revision || '';
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
            const delBtn = /** @type {HTMLElement} */ (e.target).closest('.js-cot-del-item');
            if (!delBtn) return;

            const itemId = Number(delBtn.getAttribute('data-id')) || 0;
            if (!itemId) return;

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const swalRes = await Swal.fire({
                    title: '¿Eliminar ítem?',
                    text: 'Se quitará este producto de la cotización.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, eliminar',
                    cancelButtonText: 'Cancelar'
                });
                if (!swalRes.isConfirmed) return;
            } else if (!confirm('¿Eliminar este ítem de la cotización?')) {
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
                await cargarProductos(currentCotizacionId, true);

                // Refresh header totals
                const headerResp = await fetch(`${config.urls.obtener}?id=${currentCotizacionId}`);
                const headerRes = await headerResp.json();
                if (headerRes.success && headerRes.data) {
                    updateTotalsDisplay(headerRes.data.montoBruto, headerRes.data.montoDctoItem, headerRes.data.montoIgv, headerRes.data.montoTotal);
                    if (hdrRev) hdrRev.value = headerRes.data.revision || '';
                }
            } catch (err) {
                console.error(err);
                notify('error', 'Error al eliminar el ítem.');
            }
        });
    }

    // --- Generar Pedido desde Cotización ---
    if (btnModalPedido && pedModal) {
        btnModalPedido.addEventListener('click', function () {
            if (!currentCotizacionId) return;
            if (pedId) pedId.value = String(currentCotizacionId);
            if (pedCode) pedCode.textContent = hdrCodigo.value;
            pedModal.show();
        });
    }

    if (pedForm) {
        pedForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (!pedForm.checkValidity()) {
                pedForm.reportValidity();
                return;
            }

            try {
                const formData = new FormData(pedForm);
                const resp = await fetch(config.urls.generarPedido, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                    body: formData
                });
                const res = await resp.json();

                if (!res.success) {
                    notify('error', res.message || 'No se pudo generar el pedido.');
                    return;
                }

                if (pedModal) pedModal.hide();
                cotModal.hide();

                // @ts-ignore
                if (window.Swal) {
                    // @ts-ignore
                    await Swal.fire({
                        title: '¡Pedido Generado!',
                        text: res.message || `Se ha generado el Pedido de Venta N° ${res.codigo}.`,
                        icon: 'success',
                        confirmButtonText: 'Aceptar'
                    });
                } else {
                    alert(res.message || 'Pedido generado con éxito.');
                }

                window.location.reload();
            } catch (err) {
                console.error(err);
                notify('error', 'Error al generar el pedido.');
            }
        });
    }

    // --- Global Click Listeners for Listing Actions ---
    document.addEventListener('click', async function (e) {
        // Open New Quotation
        const btnNew = /** @type {HTMLElement} */ (e.target).closest('.js-cot-new');
        if (btnNew) {
            e.preventDefault();
            resetHeaderForm();
            const modalTitle = document.getElementById('cot-modal-title');
            const modalSubtitle = document.getElementById('cot-modal-subtitle');
            if (modalTitle) modalTitle.textContent = 'Nueva Cotización';
            if (modalSubtitle) modalSubtitle.textContent = 'Ingrese los datos generales para crear la propuesta comercial.';

            if (btnTabHdr) {
                // @ts-ignore
                const tab = new bootstrap.Tab(btnTabHdr);
                tab.show();
            }
            cotModal.show();
            return;
        }

        // Open Existing Quotation
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-cot-open');
        if (btnOpen) {
            e.preventDefault();
            const id = Number(btnOpen.getAttribute('data-id')) || 0;
            if (id > 0) {
                await cargarCotizacion(id);
            }
            return;
        }

        // Annul Quotation
        const btnDel = /** @type {HTMLElement} */ (e.target).closest('.js-cot-delete');
        if (btnDel) {
            e.preventDefault();
            const id = Number(btnDel.getAttribute('data-id')) || 0;
            const rev = btnDel.getAttribute('data-revision') || '';
            const code = btnDel.getAttribute('data-code') || '';

            // @ts-ignore
            if (window.Swal) {
                // @ts-ignore
                const swalRes = await Swal.fire({
                    title: `¿Anular cotización ${code}?`,
                    text: 'Esta acción no se puede deshacer y la cotización pasará a estado ANULADO.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, anular cotización',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#dc3545'
                });
                if (!swalRes.isConfirmed) return;
            } else if (!confirm(`¿Anular la cotización ${code}?`)) {
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
                    notify('error', res.message || 'No se pudo anular la cotización.');
                    return;
                }

                notify('success', res.message || 'Cotización anulada con éxito.');
                setTimeout(() => window.location.reload(), 800);
            } catch (err) {
                console.error(err);
                notify('error', 'Error al anular la cotización.');
            }
            return;
        }

        // Generate Pedido from Listing
        const btnToPed = /** @type {HTMLElement} */ (e.target).closest('.js-cot-to-pedido');
        if (btnToPed && pedModal) {
            e.preventDefault();
            const id = Number(btnToPed.getAttribute('data-id')) || 0;
            const code = btnToPed.getAttribute('data-code') || '';
            if (pedId) pedId.value = String(id);
            if (pedCode) pedCode.textContent = code;
            pedModal.show();
        }
    });

})();

