(() => {
    'use strict';

    const modalEl = document.getElementById('tsal-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const formHdr = document.getElementById('tsal-form');
    const formItem = document.getElementById('tsal-item-form');
    const tabHdrBtn = document.getElementById('tab-tsal-hdr');
    const tabItemsBtn = document.getElementById('tab-tsal-items');
    const tabDetailLi = document.querySelector('.js-tsal-tab-detail');

    const idInput = document.getElementById('tsal-id');
    const contextoInput = document.getElementById('tsal-contexto-id');
    const revisionInput = document.getElementById('tsal-revision');
    const codigoInput = document.getElementById('tsal-codigo');
    const fechaInput = document.getElementById('tsal-fecha');
    const almacenSelect = document.getElementById('tsal-almacen-id');
    const almacenDestSelect = document.getElementById('tsal-almacen-dest-id');
    const numGrInput = document.getElementById('tsal-num-gr');
    const serieDocInput = document.getElementById('tsal-serie-doc');
    const numDocInput = document.getElementById('tsal-num-doc');
    const obsInput = document.getElementById('tsal-obs');
    const btnSaveHdr = document.getElementById('btn-save-tsal-hdr');

    // Item form inputs
    const itemIdInput = document.getElementById('tsal-item-id');
    const itemMovIdInput = document.getElementById('tsal-item-mov-id');
    const itemPrdIdInput = document.getElementById('tsal-item-prd-id');
    const itemPrdSearch = document.getElementById('tsal-item-prd-search');
    const itemPrdResults = document.getElementById('tsal-item-prd-results');
    const itemPrdSelected = document.getElementById('tsal-item-prd-selected');
    const itemPrdBadge = document.getElementById('tsal-item-prd-badge');
    const itemPrdName = document.getElementById('tsal-item-prd-name');
    const itemPrdUm = document.getElementById('tsal-item-prd-um');
    const itemCantInput = document.getElementById('tsal-item-cant');
    const itemCantDocInput = document.getElementById('tsal-item-cant-doc');
    const itemValUInput = document.getElementById('tsal-item-val-u');
    const btnSaveItem = document.getElementById('btn-save-tsal-item');

    const itemsTbody = document.getElementById('tsal-items-tbody');
    const itemsCount = document.getElementById('tsal-items-count');
    const itemsTotal = document.getElementById('tsal-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    // Filter destination warehouses so origin cannot be chosen as destination
    almacenSelect?.addEventListener('change', () => {
        const originId = almacenSelect.value;
        Array.from(almacenDestSelect.options).forEach(opt => {
            if (opt.value && opt.value === originId) {
                opt.disabled = true;
                if (almacenDestSelect.value === originId) almacenDestSelect.value = '';
            } else {
                opt.disabled = false;
            }
        });
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

    document.querySelector('.js-tsal-clear-item-prd')?.addEventListener('click', clearProducto);

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
                const res = await fetch(`/IV/TransfeSalida/BuscarProductos?q=${encodeURIComponent(q)}`);
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
        if (!itemPrdSearch?.contains(e.target) && !itemPrdResults?.contains(e.target)) itemPrdResults?.classList.add('d-none');
    });

    // Load Items in table
    async function loadItems(movId) {
        if (!movId || movId <= 0) {
            itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Guarde los datos de envío primero para poder agregar productos.</td></tr>';
            itemsCount.textContent = '0';
            itemsTotal.textContent = 'S/ 0.00';
            return;
        }

        try {
            const res = await fetch(`/IV/TransfeSalida/Productos?movimientoAlmacenId=${movId}`);
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
                                <button type="button" class="btn btn-sm btn-link text-danger p-0 js-tsal-del-item" data-id="${item.id}" title="Eliminar item">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </td>
                        </tr>
                    `;
                }).join('');
                itemsCount.textContent = result.data.length;
                itemsTotal.textContent = `S/ ${formatNumber(total)}`;
            } else {
                itemsTbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No hay productos registrados en esta transferencia.</td></tr>';
                itemsCount.textContent = '0';
                itemsTotal.textContent = 'S/ 0.00';
            }
        } catch (e) {
            console.error('Error al cargar items', e);
        }
    }

    // New Transfer
    document.querySelector('.js-tsal-new')?.addEventListener('click', () => {
        formHdr.reset();
        formItem?.reset();
        idInput.value = '0';
        itemMovIdInput.value = '0';
        revisionInput.value = '';
        codigoInput.value = '';
        clearProducto();
        fechaInput.value = new Date().toISOString().split('T')[0];
        document.getElementById('tsal-modal-title').innerHTML = '<i class="bi bi-arrow-left-right me-2"></i>Nueva Transferencia de Salida';
        document.getElementById('tsal-modal-subtitle').textContent = 'Ingrese el almacén de origen y destino';

        loadItems(0);

        tabHdrBtn.click();
        tabDetailLi.classList.add('d-none');
        modal.show();
    });

    // Edit Transfer
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-tsal-edit');
        if (!btn) return;
        const id = btn.dataset.id;

        try {
            const res = await fetch(`/IV/TransfeSalida/Obtener?id=${id}`);
            const result = await res.json();
            if (!result.success || !result.data) {
                Swal.fire({ icon: 'warning', title: 'Atención', text: result.message || 'No se pudo cargar la transferencia.' });
                return;
            }

            const d = result.data;
            const c = d.cabecera;

            formHdr.reset();
            formItem?.reset();
            clearProducto();

            idInput.value = c.id;
            itemMovIdInput.value = c.id;
            revisionInput.value = c.revision || '';
            codigoInput.value = c.codigo;
            if (c.fecha) fechaInput.value = c.fecha.split('T')[0];
            almacenSelect.value = c.almacenId;
            almacenDestSelect.value = c.almacenDestinoId || '';

            numGrInput.value = c.numeroGuiaRemision || '';
            serieDocInput.value = c.serieDocumento || '';
            numDocInput.value = c.numeroDocumento || '';
            obsInput.value = c.observacion || '';

            document.getElementById('tsal-modal-title').innerHTML = `<i class="bi bi-pencil-square me-2"></i>Transferencia: ${c.codigo}`;
            document.getElementById('tsal-modal-subtitle').textContent = `Origen: ${c.almacenNombre} ➔ Destino: ${c.almacenDestinoNombre}`;

            tabDetailLi.classList.remove('d-none');
            await loadItems(c.id);

            tabHdrBtn.click();
            modal.show();
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al consultar la transferencia.' });
        }
    });

    // Save Header Form
    formHdr?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!almacenSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione el almacén de origen.' });
            return;
        }
        if (!almacenDestSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione el almacén de destino.' });
            return;
        }
        if (almacenSelect.value === almacenDestSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'El almacén de destino no puede ser igual al de origen.' });
            return;
        }

        btnSaveHdr.disabled = true;
        btnSaveHdr.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const formData = new FormData(formHdr);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/TransfeSalida/Guardar', {
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
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo guardar la transferencia.' });
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
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Debe guardar la cabecera antes de agregar productos.' });
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
            const res = await fetch('/IV/TransfeSalida/GuardarProducto', {
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
        const btn = e.target.closest('.js-tsal-del-item');
        if (!btn) return;
        const id = btn.dataset.id;
        const movId = parseInt(itemMovIdInput.value);

        const confirm = await Swal.fire({
            title: '¿Eliminar producto?',
            text: '¿Desea quitar este producto de la transferencia?',
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

            const res = await fetch('/IV/TransfeSalida/EliminarProducto', {
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

    // Anular Transfer
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-tsal-anular');
        if (!btn) return;
        const id = btn.dataset.id;
        const codigo = btn.dataset.codigo;
        const revision = btn.dataset.revision;
        const contexto = btn.dataset.contexto;

        const confirm = await Swal.fire({
            title: '¿Anular transferencia?',
            text: `¿Está seguro de anular la transferencia ${codigo}? Esta acción cancelará el envío físico.`,
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

            const res = await fetch('/IV/TransfeSalida/Anular', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                await Swal.fire({ icon: 'success', title: 'Anulada', text: result.message, timer: 1400, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo anular la transferencia.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
        }
    });

    modalEl?.addEventListener('hidden.bs.modal', () => {
        if (parseInt(idInput.value) > 0) {
            window.location.reload();
        }
    });
})();

