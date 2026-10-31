// @ts-check
(function () {
    'use strict';

    /**
     * @typedef {Object} ListaPrecioConfig
     * @property {boolean} canWrite
     * @property {number} contextoId
     * @property {Object} urls
     * @property {string} urls.obtener
     * @property {string} urls.guardar
     * @property {string} urls.anular
     * @property {string} urls.productos
     * @property {string} urls.guardarProducto
     * @property {string} urls.grupos
     * @property {string} urls.gruposDisponibles
     * @property {string} urls.guardarGrupo
     * @property {string} urls.quitarGrupo
     * @property {string} urls.grupoProductos
     * @property {string} urls.guardarGrupoProducto
     * @property {string} urls.quitarGrupoProducto
     */

    const configEl = document.getElementById('lista-precio-config');
    if (!configEl) return;

    /** @type {ListaPrecioConfig} */
    const config = JSON.parse(configEl.textContent || '{}');

    // DOM Elements
    const modalEl = document.getElementById('lp-modal');
    if (!modalEl) return;
    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    const modalTitle = document.getElementById('lp-modal-title');
    const modalSubtitle = document.getElementById('lp-modal-subtitle');
    const detailTabs = document.querySelectorAll('.js-lp-tab-detail');

    // Header Form elements
    const headerForm = /** @type {HTMLFormElement} */ (document.getElementById('lp-header-form'));
    const hdrId = /** @type {HTMLInputElement} */ (document.getElementById('lp-hdr-id'));
    const hdrRev = /** @type {HTMLInputElement} */ (document.getElementById('lp-hdr-rev'));
    const hdrSede = /** @type {HTMLSelectElement} */ (document.getElementById('lp-hdr-sede'));
    const hdrNombre = /** @type {HTMLInputElement} */ (document.getElementById('lp-hdr-nombre'));
    const hdrTasa = /** @type {HTMLSelectElement} */ (document.getElementById('lp-hdr-tasa'));
    const hdrMoneda = /** @type {HTMLSelectElement} */ (document.getElementById('lp-hdr-moneda'));

    // Products Elements
    const prodEditor = document.getElementById('lp-prod-editor');
    const prodForm = /** @type {HTMLFormElement} */ (document.getElementById('lp-prod-form'));
    const prdTitle = document.getElementById('lp-prd-title');
    const prdId = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-id'));
    const prdLpId = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-lpid'));
    const prdPid = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-pid'));
    const prdRev = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-rev'));
    const prdVal = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-val'));
    const prdPre = /** @type {HTMLInputElement} */ (document.getElementById('lp-prd-pre'));
    const prdDcto = /** @type {HTMLSelectElement} */ (document.getElementById('lp-prd-dcto'));
    const prdEst = /** @type {HTMLSelectElement} */ (document.getElementById('lp-prd-est'));

    const prodSearch = /** @type {HTMLInputElement} */ (document.getElementById('lp-prod-search'));
    const prodStatus = /** @type {HTMLSelectElement} */ (document.getElementById('lp-prod-status'));
    const prodSize = /** @type {HTMLSelectElement} */ (document.getElementById('lp-prod-size'));
    const prodRows = document.getElementById('lp-prod-rows');
    const prodRange = document.getElementById('lp-prod-range');
    const prodPrev = /** @type {HTMLButtonElement} */ (document.getElementById('lp-prod-prev'));
    const prodNext = /** @type {HTMLButtonElement} */ (document.getElementById('lp-prod-next'));

    // Groups Elements
    const groupAddForm = /** @type {HTMLFormElement} */ (document.getElementById('lp-group-add-form'));
    const grpLpId = /** @type {HTMLInputElement} */ (document.getElementById('lp-grp-lpid'));
    const grpSelect = /** @type {HTMLSelectElement} */ (document.getElementById('lp-grp-select'));
    const grpRows = document.getElementById('lp-groups-rows');

    // Group Products Elements
    const gpGroupFilter = /** @type {HTMLSelectElement} */ (document.getElementById('lp-gp-group-filter'));
    const gpSearch = /** @type {HTMLInputElement} */ (document.getElementById('lp-gp-search'));
    const gpEditor = document.getElementById('lp-gp-editor');
    const gpForm = /** @type {HTMLFormElement} */ (document.getElementById('lp-gp-form'));
    const gpId = /** @type {HTMLInputElement} */ (document.getElementById('lp-gp-id'));
    const gpLpProveId = /** @type {HTMLInputElement} */ (document.getElementById('lp-gp-lpproveid'));
    const gpLpProdId = /** @type {HTMLInputElement} */ (document.getElementById('lp-gp-lpprodid'));
    const gpPorc = /** @type {HTMLInputElement} */ (document.getElementById('lp-gp-porc'));
    const gpRows = document.getElementById('lp-gp-rows');

    // State
    let currentListaId = 0;
    let currentTasa = 0.0;
    let prodCurrentPage = 1;
    let prodTotalPages = 1;
    let isCalculating = false;

    // --- Helpers ---
    function getAntiForgeryToken() {
        const tokenInput = /** @type {HTMLInputElement} */ (document.querySelector('input[name="__RequestVerificationToken"]'));
        return tokenInput ? tokenInput.value : '';
    }

    function notify(type, message) {
        // @ts-ignore
        if (typeof Swal !== 'undefined') {
            // @ts-ignore
            Swal.fire({
                icon: type,
                title: type === 'success' ? 'Éxito' : 'Aviso',
                text: message,
                timer: type === 'success' ? 2000 : undefined,
                showConfirmButton: type !== 'success'
            });
        } else {
            alert(message);
        }
    }

    function setDetailTabsVisible(visible) {
        detailTabs.forEach(tab => {
            if (visible) {
                tab.removeAttribute('hidden');
            } else {
                tab.setAttribute('hidden', 'true');
            }
        });
    }

    function updateTaxRateFromSelect() {
        const opt = hdrTasa.selectedOptions[0];
        if (opt && opt.dataset.tasa) {
            currentTasa = parseFloat(opt.dataset.tasa) || 0.0;
        } else {
            currentTasa = 0.0;
        }
    }

    hdrTasa?.addEventListener('change', updateTaxRateFromSelect);

    // --- Open Create Modal ---
    document.querySelector('.js-lp-new')?.addEventListener('click', () => {
        currentListaId = 0;
        currentTasa = 0.0;
        headerForm.reset();
        hdrId.value = '0';
        hdrRev.value = '';
        if (modalTitle) modalTitle.textContent = 'Nueva Lista de Precios';
        if (modalSubtitle) modalSubtitle.textContent = 'Ingrese los datos generales para habilitar la configuración de productos';
        setDetailTabsVisible(false);

        // Activate first tab
        const firstTab = document.getElementById('tab-lp-header');
        if (firstTab) {
            // @ts-ignore
            new bootstrap.Tab(firstTab).show();
        }
        modal.show();
    });

    // --- Open Edit/Detail Modal ---
    document.addEventListener('click', async (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-open');
        if (!btn) return;
        const id = parseInt(btn.getAttribute('data-id') || '0', 10);
        if (id <= 0) return;

        try {
            const resp = await fetch(`${config.urls.obtener}?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'Error al obtener la lista de precios.');
                return;
            }

            const lp = res.data.cabecera;
            currentListaId = lp.id;
            currentTasa = lp.tasa || 0.0;

            hdrId.value = lp.id.toString();
            hdrRev.value = lp.revision || '';
            hdrSede.value = lp.empresaSedeId.toString();
            hdrNombre.value = lp.nombre || '';
            hdrTasa.value = lp.impuestoTasaId.toString();
            hdrMoneda.value = lp.monedaId.toString();

            if (modalTitle) modalTitle.textContent = lp.nombre;
            if (modalSubtitle) modalSubtitle.textContent = `Sede: ${lp.sedeNombre} · Moneda: ${lp.monedaNombre} (${lp.simbolo}) · Tasa: ${currentTasa}%`;

            setDetailTabsVisible(true);

            // Set IDs for sub-forms
            if (prdLpId) prdLpId.value = currentListaId.toString();
            if (grpLpId) grpLpId.value = currentListaId.toString();

            // Load details
            loadProducts(1);
            loadGroups();

            // Activate first tab
            const firstTab = document.getElementById('tab-lp-header');
            if (firstTab) {
                // @ts-ignore
                new bootstrap.Tab(firstTab).show();
            }
            modal.show();
        } catch (err) {
            notify('error', 'Error de comunicación al consultar la lista.');
        }
    });

    // --- Save Header ---
    headerForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const formData = new FormData(headerForm);

        try {
            const resp = await fetch(config.urls.guardar, {
                method: 'POST',
                body: formData,
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            const res = await resp.json();
            if (!res.success) {
                notify('error', res.message || 'Error al guardar la lista.');
                return;
            }

            notify('success', res.message || 'Cabecera guardada.');
            currentListaId = res.id;
            hdrId.value = currentListaId.toString();
            if (prdLpId) prdLpId.value = currentListaId.toString();
            if (grpLpId) grpLpId.value = currentListaId.toString();

            updateTaxRateFromSelect();
            setDetailTabsVisible(true);

            loadProducts(1);
            loadGroups();
        } catch (err) {
            notify('error', 'Error de red al guardar cabecera.');
        }
    });

    // --- Inactivate List ---
    document.addEventListener('click', async (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-delete');
        if (!btn) return;
        const id = parseInt(btn.getAttribute('data-id') || '0', 10);
        const rev = btn.getAttribute('data-revision') || '';
        if (id <= 0) return;

        // @ts-ignore
        Swal.fire({
            title: '¿Anular Lista de Precios?',
            text: 'La lista quedará inactiva y no se mostrará en los procesos de venta.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, anular',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545'
        }).then(async (result) => {
            if (!result.isConfirmed) return;

            const fd = new FormData();
            fd.append('id', id.toString());
            fd.append('revision', rev);
            fd.append('contextoId', config.contextoId.toString());

            try {
                const resp = await fetch(config.urls.anular, {
                    method: 'POST',
                    body: fd,
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const res = await resp.json();
                if (!res.success) {
                    notify('error', res.message || 'Error al anular la lista.');
                    return;
                }
                notify('success', res.message || 'Lista anulada.');
                setTimeout(() => window.location.reload(), 800);
            } catch (err) {
                notify('error', 'Error al procesar la anulación.');
            }
        });
    });

    // ==========================================
    // PRODUCTS MANAGEMENT
    // ==========================================

    async function loadProducts(page) {
        if (!currentListaId || !prodRows) return;
        prodCurrentPage = page || 1;
        prodRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3"><div class="spinner-border spinner-border-sm text-primary me-2"></div>Cargando productos...</td></tr>';

        const search = encodeURIComponent(prodSearch?.value || '');
        const estado = encodeURIComponent(prodStatus?.value || '');
        const top = prodSize?.value || '25';

        try {
            const resp = await fetch(`${config.urls.productos}?listaPrecioId=${currentListaId}&search=${search}&estado=${estado}&page=${prodCurrentPage}&top=${top}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                prodRows.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-3">Error al cargar productos.</td></tr>';
                return;
            }

            const data = res.data;
            prodTotalPages = data.pageCount || 1;

            if (prodRange) {
                prodRange.textContent = `Mostrando ${data.firstRecord}–${data.lastRecord} de ${data.total} productos · Página ${data.page} de ${data.pageCount}`;
            }

            if (prodPrev) prodPrev.disabled = data.page <= 1;
            if (prodNext) prodNext.disabled = data.page >= data.pageCount;

            if (!data.items || data.items.length === 0) {
                prodRows.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">No se encontraron productos con los filtros aplicados.</td></tr>';
                return;
            }

            let html = '';
            data.items.forEach(p => {
                const isAct = p.estado === 'A';
                const hasDcto = p.permiteDescuento === 'S';
                html += `<tr>
                    <td class="font-monospace small">${p.codigo}</td>
                    <td>${p.descripcion}</td>
                    <td><small class="text-muted">${p.familia}</small></td>
                    <td class="text-end font-monospace">${p.valorUnitarioTexto}</td>
                    <td class="text-end font-monospace fw-bold text-success">${p.precioUnitarioTexto}</td>
                    <td class="text-center"><span class="badge ${hasDcto ? 'bg-info-subtle text-info' : 'bg-light text-muted border'}">${hasDcto ? 'Sí' : 'No'}</span></td>
                    <td class="text-center"><span class="badge ${isAct ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}">${isAct ? 'Activo' : 'Inactivo'}</span></td>
                    <td class="text-center">
                        ${config.canWrite ? `<button type="button" class="btn btn-sm btn-outline-primary js-lp-prod-edit"
                            data-id="${p.id}" data-pid="${p.productoId}" data-codigo="${p.codigo}" data-desc="${p.descripcion}"
                            data-val="${p.valorUnitario}" data-pre="${p.precioUnitario}" data-dcto="${p.permiteDescuento}"
                            data-est="${p.estado}" data-rev="${p.revision}" title="Editar precio"><i class="bi bi-pencil"></i></button>` : ''}
                    </td>
                </tr>`;
            });
            prodRows.innerHTML = html;
        } catch (err) {
            prodRows.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-3">Error de comunicación al consultar productos.</td></tr>';
        }
    }

    document.getElementById('lp-prod-btn-search')?.addEventListener('click', () => loadProducts(1));
    document.getElementById('lp-prod-btn-clear')?.addEventListener('click', () => {
        if (prodSearch) prodSearch.value = '';
        if (prodStatus) prodStatus.value = 'A';
        loadProducts(1);
    });
    prodSearch?.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            loadProducts(1);
        }
    });
    prodStatus?.addEventListener('change', () => loadProducts(1));
    prodSize?.addEventListener('change', () => loadProducts(1));

    prodPrev?.addEventListener('click', () => {
        if (prodCurrentPage > 1) loadProducts(prodCurrentPage - 1);
    });
    prodNext?.addEventListener('click', () => {
        if (prodCurrentPage < prodTotalPages) loadProducts(prodCurrentPage + 1);
    });

    // Auto-calculate ValorUnitario <-> PrecioUnitario
    prdVal?.addEventListener('input', () => {
        if (isCalculating) return;
        isCalculating = true;
        const val = parseFloat(prdVal.value) || 0.0;
        const pre = val * (1 + currentTasa / 100);
        prdPre.value = pre.toFixed(4);
        isCalculating = false;
    });

    prdPre?.addEventListener('input', () => {
        if (isCalculating) return;
        isCalculating = true;
        const pre = parseFloat(prdPre.value) || 0.0;
        const val = pre / (1 + currentTasa / 100);
        prdVal.value = val.toFixed(4);
        isCalculating = false;
    });

    // Click Edit on a Product
    document.addEventListener('click', (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-prod-edit');
        if (!btn || !prodEditor) return;

        prdId.value = btn.getAttribute('data-id') || '';
        prdLpId.value = currentListaId.toString();
        prdPid.value = btn.getAttribute('data-pid') || '';
        prdRev.value = btn.getAttribute('data-rev') || '';
        prdVal.value = parseFloat(btn.getAttribute('data-val') || '0').toFixed(4);
        prdPre.value = parseFloat(btn.getAttribute('data-pre') || '0').toFixed(4);
        prdDcto.value = btn.getAttribute('data-dcto') || 'N';
        prdEst.value = btn.getAttribute('data-est') || 'A';

        if (prdTitle) {
            prdTitle.textContent = `Editar: ${btn.getAttribute('data-codigo')} · ${btn.getAttribute('data-desc')}`;
        }
        prodEditor.removeAttribute('hidden');
        prdVal.focus();
    });

    document.querySelectorAll('.js-lp-prod-cancel').forEach(b => {
        b.addEventListener('click', () => prodEditor?.setAttribute('hidden', 'true'));
    });

    // Submit Product Update
    prodForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData(prodForm);

        try {
            const resp = await fetch(config.urls.guardarProducto, {
                method: 'POST',
                body: fd,
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            const res = await resp.json();
            if (!res.success) {
                notify('error', res.message || 'Error al guardar precio del producto.');
                return;
            }
            notify('success', res.message || 'Precio actualizado.');
            prodEditor?.setAttribute('hidden', 'true');
            loadProducts(prodCurrentPage);
        } catch (err) {
            notify('error', 'Error al guardar precio del producto.');
        }
    });

    // ==========================================
    // CLIENT GROUPS MANAGEMENT
    // ==========================================

    async function loadGroups() {
        if (!currentListaId || !grpRows) return;
        grpRows.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">Cargando grupos...</td></tr>';

        try {
            // Load assigned
            const resp = await fetch(`${config.urls.grupos}?listaPrecioId=${currentListaId}`);
            const res = await resp.json();

            // Populate group filter on tab 4
            if (gpGroupFilter) {
                gpGroupFilter.innerHTML = '<option value="">Seleccione grupo para ver/configurar precios...</option>';
            }

            if (!res.success || !res.data || res.data.length === 0) {
                grpRows.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-4">No hay grupos asignados a esta lista.</td></tr>';
            } else {
                let html = '';
                res.data.forEach(g => {
                    html += `<tr>
                        <td class="fw-semibold">${g.grupoNombre}</td>
                        <td class="text-center"><span class="badge bg-light text-dark border">${g.cantidadDescuentos}</span></td>
                        <td class="text-center"><span class="badge bg-success-subtle text-success">Activo</span></td>
                        <td class="text-center">
                            <button type="button" class="btn btn-sm btn-outline-primary js-lp-goto-gp me-1" data-id="${g.id}" data-name="${g.grupoNombre}" title="Configurar descuentos">
                                <i class="bi bi-percent"></i> Precios
                            </button>
                            ${config.canWrite ? `<button type="button" class="btn btn-sm btn-outline-danger js-lp-group-remove" data-id="${g.id}" title="Quitar grupo">
                                <i class="bi bi-x-lg"></i>
                            </button>` : ''}
                        </td>
                    </tr>`;

                    if (gpGroupFilter) {
                        const opt = document.createElement('option');
                        opt.value = g.id.toString();
                        opt.textContent = g.grupoNombre;
                        gpGroupFilter.appendChild(opt);
                    }
                });
                grpRows.innerHTML = html;
            }

            // Load available
            if (grpSelect) {
                const respAvail = await fetch(`${config.urls.gruposDisponibles}?listaPrecioId=${currentListaId}`);
                const resAvail = await respAvail.json();
                grpSelect.innerHTML = '<option value="">Seleccione un grupo de clientes disponible...</option>';
                if (resAvail.success && resAvail.data) {
                    resAvail.data.forEach(g => {
                        const opt = document.createElement('option');
                        opt.value = g.id.toString();
                        opt.textContent = g.nombre;
                        grpSelect.appendChild(opt);
                    });
                }
            }
        } catch (err) {
            grpRows.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-3">Error al consultar grupos.</td></tr>';
        }
    }

    // Add group submit
    groupAddForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const fd = new FormData(groupAddForm);

        try {
            const resp = await fetch(config.urls.guardarGrupo, {
                method: 'POST',
                body: fd,
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            const res = await resp.json();
            if (!res.success) {
                notify('error', res.message || 'Error al asignar grupo.');
                return;
            }
            notify('success', res.message || 'Grupo asignado.');
            loadGroups();
        } catch (err) {
            notify('error', 'Error al asignar grupo.');
        }
    });

    // Remove group click
    document.addEventListener('click', async (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-group-remove');
        if (!btn) return;
        const id = parseInt(btn.getAttribute('data-id') || '0', 10);
        if (id <= 0) return;

        // @ts-ignore
        Swal.fire({
            title: '¿Retirar Grupo?',
            text: 'El grupo de clientes se retirará de esta lista de precios.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, retirar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545'
        }).then(async (result) => {
            if (!result.isConfirmed) return;

            const fd = new FormData();
            fd.append('id', id.toString());
            fd.append('contextoId', config.contextoId.toString());

            try {
                const resp = await fetch(config.urls.quitarGrupo, {
                    method: 'POST',
                    body: fd,
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const res = await resp.json();
                if (!res.success) {
                    notify('error', res.message || 'Error al retirar grupo.');
                    return;
                }
                notify('success', res.message || 'Grupo retirado.');
                loadGroups();
            } catch (err) {
                notify('error', 'Error al retirar grupo.');
            }
        });
    });

    // Go to group products tab
    document.addEventListener('click', (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-goto-gp');
        if (!btn) return;
        const id = btn.getAttribute('data-id');
        if (gpGroupFilter && id) {
            gpGroupFilter.value = id;
            loadGroupProducts(parseInt(id, 10));
            const tabBtn = document.getElementById('tab-lp-group-products');
            if (tabBtn) {
                // @ts-ignore
                new bootstrap.Tab(tabBtn).show();
            }
        }
    });

    // ==========================================
    // GROUP PRODUCTS MANAGEMENT
    // ==========================================

    gpGroupFilter?.addEventListener('change', () => {
        const id = parseInt(gpGroupFilter.value || '0', 10);
        loadGroupProducts(id);
    });

    document.getElementById('lp-gp-btn-find')?.addEventListener('click', () => {
        const id = parseInt(gpGroupFilter?.value || '0', 10);
        loadGroupProducts(id);
    });

    async function loadGroupProducts(proveId) {
        if (!gpRows) return;
        if (!proveId) {
            gpRows.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">Seleccione un grupo para ver sus productos.</td></tr>';
            return;
        }

        gpRows.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">Cargando productos con descuento especial...</td></tr>';
        const search = encodeURIComponent(gpSearch?.value || '');

        try {
            const resp = await fetch(`${config.urls.grupoProductos}?listaPProveId=${proveId}&search=${search}`);
            const res = await resp.json();

            if (!res.success || !res.data) {
                gpRows.innerHTML = '<tr><td colspan="5" class="text-center text-danger py-3">Error al consultar productos del grupo.</td></tr>';
                return;
            }

            const items = res.data.items || [];
            if (items.length === 0) {
                gpRows.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">No hay precios configurados para este grupo.</td></tr>';
                return;
            }

            let html = '';
            items.forEach(p => {
                html += `<tr>
                    <td class="font-monospace small">${p.codigo}</td>
                    <td>${p.descripcion}</td>
                    <td class="text-end font-monospace">${p.precioUnitarioTexto}</td>
                    <td class="text-end font-monospace fw-bold text-primary">${p.porcentajePrecioBaseTexto}%</td>
                    <td class="text-center">
                        ${config.canWrite ? `<button type="button" class="btn btn-sm btn-outline-danger js-lp-gp-remove" data-id="${p.id}" title="Quitar descuento">
                            <i class="bi bi-x-lg"></i>
                        </button>` : ''}
                    </td>
                </tr>`;
            });
            gpRows.innerHTML = html;
        } catch (err) {
            gpRows.innerHTML = '<tr><td colspan="5" class="text-center text-danger py-3">Error de comunicación.</td></tr>';
        }
    }

    // Remove group product discount
    document.addEventListener('click', async (e) => {
        const btn = /** @type {HTMLElement} */ (e.target)?.closest('.js-lp-gp-remove');
        if (!btn) return;
        const id = parseInt(btn.getAttribute('data-id') || '0', 10);
        if (id <= 0) return;

        const fd = new FormData();
        fd.append('id', id.toString());
        fd.append('contextoId', config.contextoId.toString());

        try {
            const resp = await fetch(config.urls.quitarGrupoProducto, {
                method: 'POST',
                body: fd,
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            const res = await resp.json();
            if (!res.success) {
                notify('error', res.message || 'Error al quitar descuento.');
                return;
            }
            notify('success', res.message || 'Descuento retirado.');
            const proveId = parseInt(gpGroupFilter?.value || '0', 10);
            loadGroupProducts(proveId);
        } catch (err) {
            notify('error', 'Error al retirar descuento.');
        }
    });

})();

