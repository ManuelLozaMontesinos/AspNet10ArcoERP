(() => {
    'use strict';

    const modalEl = document.getElementById('mov-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const formHdr = document.getElementById('mov-form');
    const formItem = document.getElementById('mov-item-form');
    const tabHdrBtn = document.getElementById('tab-mov-hdr');
    const tabItemsBtn = document.getElementById('tab-mov-items');
    const tabDetailLi = document.querySelector('.js-mov-tab-detail');

    const idInput = document.getElementById('mov-id');
    const contextoInput = document.getElementById('mov-contexto-id');
    const revisionInput = document.getElementById('mov-revision');
    const codigoInput = document.getElementById('mov-codigo');
    const fechaInput = document.getElementById('mov-fecha');
    const almacenSelect = document.getElementById('mov-almacen-id');
    const tipoMovSelect = document.getElementById('mov-tipo-mov-id');
    const transSelect = document.getElementById('mov-trans-id');
    const almacenDestSelect = document.getElementById('mov-almacen-dest-id');

    const provIdInput = document.getElementById('mov-prov-id');
    const provSearchInput = document.getElementById('mov-prov-search');
    const provResults = document.getElementById('mov-prov-results');
    const provSelected = document.getElementById('mov-prov-selected');
    const provSelectedName = document.getElementById('mov-prov-selected-name');

    const numOcInput = document.getElementById('mov-num-oc');
    const numGrInput = document.getElementById('mov-num-gr');
    const tipoDocSelect = document.getElementById('mov-tipo-doc-id');
    const serieDocInput = document.getElementById('mov-serie-doc');
    const numDocInput = document.getElementById('mov-num-doc');
    const obsInput = document.getElementById('mov-obs');
    const btnSaveHdr = document.getElementById('btn-save-mov-hdr');

    // Item form inputs
    const itemIdInput = document.getElementById('mov-item-id');
    const itemMovIdInput = document.getElementById('mov-item-mov-id');
    const itemPrdIdInput = document.getElementById('mov-item-prd-id');
    const itemPrdSearch = document.getElementById('mov-item-prd-search');
    const itemPrdResults = document.getElementById('mov-item-prd-results');
    const itemPrdSelected = document.getElementById('mov-item-prd-selected');
    const itemPrdBadge = document.getElementById('mov-item-prd-badge');
    const itemPrdName = document.getElementById('mov-item-prd-name');
    const itemPrdUm = document.getElementById('mov-item-prd-um');
    const itemCantInput = document.getElementById('mov-item-cant');
    const itemCantDocInput = document.getElementById('mov-item-cant-doc');
    const itemValUInput = document.getElementById('mov-item-val-u');
    const btnSaveItem = document.getElementById('btn-save-mov-item');

    const itemsTbody = document.getElementById('mov-items-tbody');
    const itemsCount = document.getElementById('mov-items-count');
    const itemsTotal = document.getElementById('mov-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    async function loadTransacciones(almacenId, tipoMovId, selectedVal = null) {
        if (!almacenId || !tipoMovId) {
            transSelect.innerHTML = '<option value="">-- Seleccione Almacén y Tipo primero --</option>';
            return;
        }

        try {
            const res = await fetch(`/IV/MovAlmacen/Transacciones?almacenId=${almacenId}&tipoMovimientoId=${tipoMovId}`);
            const result = await res.json();
            if (result.success && result.data) {
                transSelect.innerHTML = '<option value="">-- Seleccionar Transacción --</option>' +
                    result.data.map(t => `<option value="${t.id}" data-codigo="${t.codigo}" ${selectedVal == t.id ? 'selected' : ''}>[${t.codigo}] ${t.nombre}</option>`).join('');
            }
        } catch (e) {
            console.error('Error cargando transacciones', e);
        }
    }

    almacenSelect?.addEventListener('change', () => loadTransacciones(almacenSelect.value, tipoMovSelect.value));
    tipoMovSelect?.addEventListener('change', () => loadTransacciones(almacenSelect.value, tipoMovSelect.value));

    // Proveedor autocomplete
    function selectProveedor(id, nombre) {
        provIdInput.value = id;
        provSelectedName.textContent = nombre;
        provResults.classList.add('d-none');
        provSearchInput.parentElement.classList.add('d-none');
        provSelected.classList.remove('d-none');
    }

    function clearProveedor() {
        provIdInput.value = '';
        provSearchInput.value = '';
        provSearchInput.parentElement.classList.remove('d-none');
        provSelected.classList.add('d-none');
        provResults.classList.add('d-none');
    }

    document.querySelector('.js-mov-clear-prov')?.addEventListener('click', clearProveedor);

    let provDebounce = null;
    provSearchInput?.addEventListener('input', () => {
        clearTimeout(provDebounce);
        const q = provSearchInput.value.trim();
        if (q.length < 2) {
            provResults.classList.add('d-none');
            return;
        }

        provDebounce = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/MovAlmacen/BuscarProveedores?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (result.success && result.data && result.data.length > 0) {
                    provResults.innerHTML = result.data.map(p => `
                        <button type="button" class="list-group-item list-group-item-action py-2" data-id="${p.id}" data-name="${p.nombre}">
                            <strong>${p.nombre}</strong> <span class="text-muted small">(${p.codigo || ''})</span>
                        </button>
                    `).join('');
                    provResults.classList.remove('d-none');
                } else {
                    provResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron resultados</div>';
                    provResults.classList.remove('d-none');
                }
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    provResults?.addEventListener('click', (e) => {
        const btn = e.target.closest('button.list-group-item');
        if (!btn) return;
        selectProveedor(btn.dataset.id, btn.dataset.name);
    });

    // Producto autocomplete for item form
    function selectProducto(id, codigo, descripcion, um) {
        itemPrdIdInput.value = id;
        itemPrdBadge.textContent = codigo;
        itemPrdName.textContent = descripcion;
        itemPrdUm.textContent = `(${um})`;
        itemPrdResults.classList.add('d-none');
        itemPrdSearch.parentElement.classList.add('d-none');
        itemPrdSelected.classList.remove('d-none');
        itemCantInput.focus();
    }

    function clearProducto() {
        itemPrdIdInput.value = '';
        itemPrdSearch.value = '';
        itemPrdSearch.parentElement.classList.remove('d-none');
        itemPrdSelected.classList.add('d-none');
        itemPrdResults.classList.add('d-none');
    }

    document.querySelector('.js-mov-clear-item-prd')?.addEventListener('click', clearProducto);

    let prdDebounce = null;
    itemPrdSearch?.addEventListener('input', () => {
        clearTimeout(prdDebounce);
        const q = itemPrdSearch.value.trim();
        if (q.length < 2) {
            itemPrdResults.classList.add('d-none');
            return;
        }

        prdDebounce = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/MovAlmacen/BuscarProductos?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (result.success && result.data && result.data.length > 0) {
                    itemPrdResults.innerHTML = result.data.map(p => `
                        <button type="button" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center py-2" data-id="${p.id}" data-codigo="${p.codigo}" data-name="${p.nombre}" data-um="${p.extra || 'UND'}">
                            <div>
                                <span class="badge bg-secondary me-1 font-monospace">${p.codigo}</span>
                                <strong>${p.nombre}</strong>
                            </div>
                            <span class="badge bg-light text-dark border">${p.extra || 'UND'}</span>
                        </button>
                    `).join('');
                    itemPrdResults.classList.remove('d-none');
                } else {
                    itemPrdResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron productos</div>';
                    itemPrdResults.classList.remove('d-none');
                }
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    itemPrdResults?.addEventListener('click', (e) => {
        const btn = e.target.closest('button.list-group-item');
        if (!btn) return;
        selectProducto(btn.dataset.id, btn.dataset.codigo, btn.dataset.name, btn.dataset.um);
    });

    document.addEventListener('click', (e) => {
        if (!provSearchInput?.contains(e.target) && !provResults?.contains(e.target)) provResults?.classList.add('d-none');
        if (!itemPrdSearch?.contains(e.target) && !itemPrdResults?.contains(e.target)) itemPrdResults?.classList.add('d-none');
    });

    // Load Items in table
    async function loadItems(movId) {
        if (!movId || movId <= 0) {
            itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Guarde los datos de operación primero para poder agregar productos.</td></tr>';
            itemsCount.textContent = '0';
            itemsTotal.textContent = 'S/ 0.00';
            return;
        }

        try {
            const res = await fetch(`/IV/MovAlmacen/Productos?movimientoAlmacenId=${movId}`);
            const result = await res.json();
            if (result.success && result.data && result.data.length > 0) {
                let total = 0;
                itemsTbody.innerHTML = result.data.map(item => {
                    total += item.total || 0;
                    return `
                        <tr>
                            <td class="font-monospace fw-semibold">${item.productoCodigo}</td>
                            <td>${item.descripcion}</td>
                            <td class="text-center"><span class="badge bg-light text-dark border">${item.unidadMedida}</span></td>
                            <td class="text-end font-monospace">${formatNumber(item.cantidadDocumento)}</td>
                            <td class="text-end font-monospace fw-bold">${formatNumber(item.cantidad)}</td>
                            <td class="text-end font-monospace">${formatNumber(item.valorUnitario, 4)}</td>
                            <td class="text-end font-monospace fw-semibold">${formatNumber(item.total)}</td>
                            <td class="text-end">
                                <button type="button" class="btn btn-sm btn-link text-danger p-0 js-mov-del-item" data-id="${item.id}" title="Eliminar item">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </td>
                        </tr>
                    `;
                }).join('');
                itemsCount.textContent = result.data.length;
                itemsTotal.textContent = `S/ ${formatNumber(total)}`;
            } else {
                itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos registrados en este movimiento.</td></tr>';
                itemsCount.textContent = '0';
                itemsTotal.textContent = 'S/ 0.00';
            }
        } catch (e) {
            console.error('Error al cargar items', e);
        }
    }

    // New Movement
    document.querySelector('.js-mov-new')?.addEventListener('click', () => {
        formHdr.reset();
        formItem?.reset();
        idInput.value = '0';
        itemMovIdInput.value = '0';
        revisionInput.value = '';
        codigoInput.value = '';
        clearProveedor();
        clearProducto();
        fechaInput.value = new Date().toISOString().split('T')[0];
        document.getElementById('mov-modal-title').innerHTML = '<i class="bi bi-box-arrow-in-down-right me-2"></i>Nuevo Movimiento de Almacén';
        document.getElementById('mov-modal-subtitle').textContent = 'Ingrese los datos de la operación';

        transSelect.innerHTML = '<option value="">-- Seleccione Almacén y Tipo primero --</option>';
        loadItems(0);

        tabHdrBtn.click();
        tabDetailLi.classList.add('d-none');
        modal.show();
    });

    // Edit Movement
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-mov-edit');
        if (!btn) return;
        const id = btn.dataset.id;

        try {
            const res = await fetch(`/IV/MovAlmacen/Obtener?id=${id}`);
            const result = await res.json();
            if (!result.success || !result.data) {
                Swal.fire({ icon: 'warning', title: 'Atención', text: result.message || 'No se pudo cargar el movimiento.' });
                return;
            }

            const d = result.data;
            const c = d.cabecera;

            formHdr.reset();
            formItem?.reset();
            clearProveedor();
            clearProducto();

            idInput.value = c.id;
            itemMovIdInput.value = c.id;
            revisionInput.value = c.revision || '';
            codigoInput.value = c.codigo;
            if (c.fecha) fechaInput.value = c.fecha.split('T')[0];
            almacenSelect.value = c.almacenId;
            tipoMovSelect.value = c.tipoMovimientoAlmacenId;

            await loadTransacciones(c.almacenId, c.tipoMovimientoAlmacenId, c.transaccionAlmacenId);

            if (c.almacenDestinoId) almacenDestSelect.value = c.almacenDestinoId;
            else almacenDestSelect.value = '';

            if (c.proveedorId && c.proveedorNombre) {
                selectProveedor(c.proveedorId, c.proveedorNombre);
            }

            numOcInput.value = c.numeroOrdenCompra || '';
            numGrInput.value = c.numeroGuiaRemision || '';

            // Populate Tipos Documento Comercial in dropdown if returned
            if (d.tiposDocumentoComercial && d.tiposDocumentoComercial.length > 0) {
                tipoDocSelect.innerHTML = '<option value="">-- Opcional --</option>' +
                    d.tiposDocumentoComercial.map(td => `<option value="${td.id}" ${c.tipoDocumentoComercialId == td.id ? 'selected' : ''}>${td.nombre}</option>`).join('');
            }

            serieDocInput.value = c.serieDocumento || '';
            numDocInput.value = c.numeroDocumento || '';
            obsInput.value = c.observacion || '';

            document.getElementById('mov-modal-title').innerHTML = `<i class="bi bi-pencil-square me-2"></i>Movimiento: ${c.codigo}`;
            document.getElementById('mov-modal-subtitle').textContent = `Tipo: ${c.tipoMovimientoAlmacenNombre} · ${c.transaccionAlmacenNombre}`;

            tabDetailLi.classList.remove('d-none');
            await loadItems(c.id);

            tabHdrBtn.click();
            modal.show();
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al consultar el movimiento.' });
        }
    });

    // Save Header Form
    formHdr?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!almacenSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione el almacén de origen.' });
            return;
        }
        if (!tipoMovSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione el tipo de movimiento.' });
            return;
        }
        if (!transSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione la transacción de almacén.' });
            return;
        }

        btnSaveHdr.disabled = true;
        btnSaveHdr.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const formData = new FormData(formHdr);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/MovAlmacen/Guardar', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                const movId = result.id;
                idInput.value = movId;
                itemMovIdInput.value = movId;
                tabDetailLi.classList.remove('d-none');
                await loadItems(movId);
                tabItemsBtn.click();
                Swal.fire({ icon: 'success', title: 'Éxito', text: result.message, timer: 1500, showConfirmButton: false });
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo guardar el movimiento.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
        } finally {
            btnSaveHdr.disabled = false;
            btnSaveHdr.innerHTML = '<i class="bi bi-save me-1"></i>Guardar y Continuar a Items';
        }
    });

    // Save Item Form
    formItem?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const movId = parseInt(itemMovIdInput.value);
        if (!movId || movId <= 0) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Debe guardar la cabecera del movimiento antes de agregar productos.' });
            return;
        }
        if (!itemPrdIdInput.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione un producto o insumo.' });
            return;
        }
        const cant = parseFloat(itemCantInput.value);
        if (!cant || cant <= 0) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'La cantidad debe ser mayor a cero.' });
            return;
        }

        btnSaveItem.disabled = true;

        try {
            const formData = new FormData(formItem);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/MovAlmacen/GuardarProducto', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                clearProducto();
                itemCantInput.value = '';
                if (itemCantDocInput) itemCantDocInput.value = '';
                if (itemValUInput) itemValUInput.value = '';
                await loadItems(movId);
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo agregar el producto.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al agregar producto.' });
        } finally {
            btnSaveItem.disabled = false;
        }
    });

    // Delete Item
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-mov-del-item');
        if (!btn) return;
        const id = btn.dataset.id;
        const movId = parseInt(itemMovIdInput.value);

        const confirm = await Swal.fire({
            title: '¿Eliminar producto?',
            text: '¿Desea quitar este producto del movimiento?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, quitar',
            cancelButtonText: 'Cancelar'
        });

        if (!confirm.isConfirmed) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const formData = new FormData();
            formData.append('id', id);
            formData.append('contextoId', contextoInput.value);

            const res = await fetch('/IV/MovAlmacen/EliminarProducto', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                await loadItems(movId);
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al eliminar producto.' });
        }
    });

    // Anular Movement
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-mov-anular');
        if (!btn) return;
        const id = btn.dataset.id;
        const codigo = btn.dataset.codigo;
        const revision = btn.dataset.revision;
        const contexto = btn.dataset.contexto;

        const confirm = await Swal.fire({
            title: '¿Anular movimiento de almacén?',
            text: `¿Está seguro de anular el movimiento ${codigo}? Esta acción cancelará las cantidades movilizadas.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sí, anular',
            cancelButtonText: 'Cancelar'
        });

        if (!confirm.isConfirmed) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const formData = new FormData();
            formData.append('id', id);
            formData.append('revision', revision || '');
            formData.append('contextoId', contexto);

            const res = await fetch('/IV/MovAlmacen/Anular', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                await Swal.fire({ icon: 'success', title: 'Anulado', text: result.message, timer: 1400, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo anular el movimiento.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
        }
    });

    modalEl?.addEventListener('hidden.bs.modal', () => {
        // If items or header were modified, refresh listing
        if (parseInt(idInput.value) > 0) {
            window.location.reload();
        }
    });
})();

