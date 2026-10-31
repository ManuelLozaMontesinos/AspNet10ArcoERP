(() => {
    'use strict';

    const modalEl = document.getElementById('cint-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const formHdr = document.getElementById('cint-form');
    const formItem = document.getElementById('cint-item-form');
    const tabHdrBtn = document.getElementById('tab-cint-hdr');
    const tabItemsBtn = document.getElementById('tab-cint-items');
    const tabDetailLi = document.querySelector('.js-cint-tab-detail');

    const idInput = document.getElementById('cint-id');
    const contextoInput = document.getElementById('cint-contexto-id');
    const revisionInput = document.getElementById('cint-revision');
    const codigoInput = document.getElementById('cint-codigo');
    const fechaInput = document.getElementById('cint-fecha');
    const almacenSelect = document.getElementById('cint-almacen-id');
    const centroCostoSelect = document.getElementById('cint-centro-costo-id');
    const personalIdInput = document.getElementById('cint-personal-id');
    const personalSearch = document.getElementById('cint-personal-search');
    const personalResults = document.getElementById('cint-personal-results');
    const personalSelected = document.getElementById('cint-personal-selected');
    const personalSelectedName = document.getElementById('cint-personal-selected-name');
    const obsInput = document.getElementById('cint-obs');
    const btnSaveHdr = document.getElementById('btn-save-cint-hdr');

    // Item form inputs
    const itemIdInput = document.getElementById('cint-item-id');
    const itemMovIdInput = document.getElementById('cint-item-mov-id');
    const itemPrdIdInput = document.getElementById('cint-item-prd-id');
    const itemPrdSearch = document.getElementById('cint-item-prd-search');
    const itemPrdResults = document.getElementById('cint-item-prd-results');
    const itemPrdSelected = document.getElementById('cint-item-prd-selected');
    const itemPrdBadge = document.getElementById('cint-item-prd-badge');
    const itemPrdName = document.getElementById('cint-item-prd-name');
    const itemPrdUm = document.getElementById('cint-item-prd-um');
    const itemCantInput = document.getElementById('cint-item-cant');
    const itemValUInput = document.getElementById('cint-item-val-u');
    const btnSaveItem = document.getElementById('btn-save-cint-item');

    const itemsTbody = document.getElementById('cint-items-tbody');
    const itemsCount = document.getElementById('cint-items-count');
    const itemsTotal = document.getElementById('cint-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    // Personal Solicitante Autocomplete
    function selectPersonal(id, razonSocial, dni) {
        if (!personalIdInput) return;
        personalIdInput.value = id;
        personalSelectedName.textContent = `${razonSocial} (DNI: ${dni || 'S/N'})`;
        personalResults.classList.add('d-none');
        personalSearch.parentElement.classList.add('d-none');
        personalSelected.classList.remove('d-none');
    }

    function clearPersonal() {
        if (!personalIdInput) return;
        personalIdInput.value = '';
        personalSearch.value = '';
        personalSelectedName.textContent = '';
        personalSelected.classList.add('d-none');
        personalSearch.parentElement.classList.remove('d-none');
        personalResults.classList.add('d-none');
    }

    document.querySelector('.js-cint-clear-personal')?.addEventListener('click', clearPersonal);

    let personalSearchTimer = null;
    personalSearch?.addEventListener('input', () => {
        clearTimeout(personalSearchTimer);
        const q = personalSearch.value.trim();
        if (q.length < 2) {
            personalResults.classList.add('d-none');
            personalResults.innerHTML = '';
            return;
        }

        personalSearchTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/ConsumoInt/BuscarPersonal?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (!result.success || !result.data || result.data.length === 0) {
                    personalResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontró personal coincidente.</div>';
                    personalResults.classList.remove('d-none');
                    return;
                }

                personalResults.innerHTML = result.data.map(p => `
                    <button type="button" class="list-group-item list-group-item-action py-2 px-3 js-cint-pick-personal"
                            data-id="${p.id}" data-nombre="${p.razonSocial}" data-doc="${p.numDocIden || ''}">
                        <div class="fw-semibold small">${p.razonSocial}</div>
                        <small class="text-muted font-monospace">DNI: ${p.numDocIden || '-'}</small>
                    </button>
                `).join('');
                personalResults.classList.remove('d-none');
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    personalResults?.addEventListener('click', (e) => {
        const item = e.target.closest('.js-cint-pick-personal');
        if (!item) return;
        selectPersonal(item.dataset.id, item.dataset.nombre, item.dataset.doc);
    });

    // Producto autocomplete for item form
    function selectProducto(id, codigo, descripcion, um) {
        if (!itemPrdIdInput) return;
        itemPrdIdInput.value = id;
        itemPrdBadge.textContent = codigo;
        itemPrdName.textContent = descripcion;
        itemPrdUm.textContent = `(${um})`;
        itemPrdResults.classList.add('d-none');
        itemPrdSearch.parentElement.classList.add('d-none');
        itemPrdSelected.classList.remove('d-none');
        itemCantInput?.focus();
    }

    function clearProducto() {
        if (!itemPrdIdInput) return;
        itemPrdIdInput.value = '';
        itemPrdSearch.value = '';
        itemPrdBadge.textContent = '';
        itemPrdName.textContent = '';
        itemPrdUm.textContent = '';
        itemPrdSelected.classList.add('d-none');
        itemPrdSearch.parentElement.classList.remove('d-none');
        itemPrdResults.classList.add('d-none');
    }

    document.querySelector('.js-cint-clear-item-prd')?.addEventListener('click', clearProducto);

    let prdSearchTimer = null;
    itemPrdSearch?.addEventListener('input', () => {
        clearTimeout(prdSearchTimer);
        const q = itemPrdSearch.value.trim();
        if (q.length < 2) {
            itemPrdResults.classList.add('d-none');
            itemPrdResults.innerHTML = '';
            return;
        }

        prdSearchTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/ConsumoInt/BuscarProductos?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (!result.success || !result.data || result.data.length === 0) {
                    itemPrdResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron productos.</div>';
                    itemPrdResults.classList.remove('d-none');
                    return;
                }

                itemPrdResults.innerHTML = result.data.map(p => `
                    <button type="button" class="list-group-item list-group-item-action py-2 px-3 js-cint-pick-prd"
                            data-id="${p.id}" data-cod="${p.codigo}" data-nom="${p.descripcion}" data-um="${p.unidadMedida}">
                        <div class="fw-semibold small"><span class="badge bg-secondary font-monospace me-1">${p.codigo}</span> ${p.descripcion}</div>
                        <small class="text-muted">U.M: ${p.unidadMedida}</small>
                    </button>
                `).join('');
                itemPrdResults.classList.remove('d-none');
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    itemPrdResults?.addEventListener('click', (e) => {
        const item = e.target.closest('.js-cint-pick-prd');
        if (!item) return;
        selectProducto(item.dataset.id, item.dataset.cod, item.dataset.nom, item.dataset.um);
    });

    // Reset modals
    function resetForm() {
        formHdr.reset();
        idInput.value = '0';
        revisionInput.value = '';
        codigoInput.value = '';
        fechaInput.value = new Date().toISOString().split('T')[0];
        clearPersonal();

        // Switch to tab 1 and disable tab 2 until saved
        const bootstrapTab1 = new bootstrap.Tab(tabHdrBtn);
        bootstrapTab1.show();
        tabDetailLi?.classList.add('d-none');

        resetItemForm();
        renderItems([]);
    }

    function resetItemForm() {
        if (!formItem) return;
        formItem.reset();
        itemIdInput.value = '0';
        clearProducto();
    }

    // Load Items for Consumption
    async function loadItems(movId) {
        if (!itemsTbody || !movId || movId <= 0) return;
        itemsTbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando materiales...</td></tr>';
        try {
            const res = await fetch(`/IV/ConsumoInt/Productos?movimientoAlmacenId=${movId}`);
            const result = await res.json();
            if (result.success) {
                renderItems(result.data || []);
            } else {
                itemsTbody.innerHTML = `<tr><td colspan="7" class="text-danger text-center py-2">${result.message || 'Error al cargar items.'}</td></tr>`;
            }
        } catch (e) {
            console.error(e);
            itemsTbody.innerHTML = '<tr><td colspan="7" class="text-danger text-center py-2">Error de comunicación.</td></tr>';
        }
    }

    function renderItems(items) {
        if (!itemsTbody) return;
        if (!items || items.length === 0) {
            itemsTbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">No hay productos registrados en este consumo interno.</td></tr>';
            itemsCount.textContent = '0';
            itemsTotal.textContent = 'S/ 0.00';
            return;
        }

        let total = 0;
        let count = items.length;

        itemsTbody.innerHTML = items.map(item => {
            const rowTotal = (Number(item.cantidad) || 0) * (Number(item.valorUnitario) || 0);
            total += rowTotal;
            return `
                <tr>
                    <td class="font-monospace fw-semibold">${item.productoCodigo}</td>
                    <td>${item.productoNombre}</td>
                    <td class="text-center"><span class="badge bg-light text-dark border">${item.unidadMedida}</span></td>
                    <td class="text-end font-monospace fw-bold">${formatNumber(item.cantidad)}</td>
                    <td class="text-end font-monospace">${formatNumber(item.valorUnitario, 4)}</td>
                    <td class="text-end font-monospace fw-bold text-primary">S/ ${formatNumber(rowTotal)}</td>
                    <td class="text-end">
                        <button type="button" class="btn btn-sm btn-link text-danger p-0 js-cint-del-item" data-id="${item.id}" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        }).join('');

        itemsCount.textContent = count.toString();
        itemsTotal.textContent = `S/ ${formatNumber(total)}`;
    }

    // New Consumption button
    document.querySelector('.js-cint-new')?.addEventListener('click', () => {
        resetForm();
        document.getElementById('cint-modal-title').textContent = 'Nuevo Consumo Interno';
        document.getElementById('cint-modal-subtitle').textContent = 'Registre la solicitud de materiales por centro de costo';
        modal.show();
    });

    // Edit Consumption button
    document.querySelectorAll('.js-cint-edit').forEach(btn => {
        btn.addEventListener('click', async () => {
            resetForm();
            const id = btn.dataset.id;
            try {
                const res = await fetch(`/IV/ConsumoInt/Obtener?id=${id}`);
                const result = await res.json();
                if (!result.success || !result.data) {
                    Swal.fire('Error', result.message || 'No se pudo cargar la información.', 'error');
                    return;
                }

                const d = result.data;
                idInput.value = d.id;
                revisionInput.value = d.revision || '';
                codigoInput.value = d.codigo || '';
                fechaInput.value = d.fecha ? d.fecha.split('T')[0] : '';
                almacenSelect.value = d.almacenId || '';
                centroCostoSelect.value = d.centroCostoId || '';
                if (d.personalId) {
                    selectPersonal(d.personalId, d.personalNombre, d.personalDocumento);
                }
                obsInput.value = d.observacion || '';

                itemMovIdInput.value = d.id;
                tabDetailLi?.classList.remove('d-none');

                document.getElementById('cint-modal-title').textContent = `Consumo Interno: ${d.codigo}`;
                document.getElementById('cint-modal-subtitle').textContent = `Estado: ${d.estadoMovimientoAlmacenNombre} | Aprobación: ${d.estadoAprobacionNombre || 'PENDIENTE'}`;

                await loadItems(d.id);
                modal.show();
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Ocurrió un error al cargar el consumo interno.', 'error');
            }
        });
    });

    // Save Header Form
    formHdr?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!formHdr.checkValidity()) {
            formHdr.reportValidity();
            return;
        }

        if (!personalIdInput.value || personalIdInput.value === '0') {
            Swal.fire('Atención', 'Debe seleccionar al personal solicitante.', 'warning');
            personalSearch.focus();
            return;
        }

        btnSaveHdr.disabled = true;
        btnSaveHdr.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const formData = new FormData(formHdr);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const res = await fetch('/IV/ConsumoInt/Guardar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                const movId = result.id || idInput.value;
                idInput.value = movId;
                itemMovIdInput.value = movId;

                tabDetailLi?.classList.remove('d-none');
                const bootstrapTab2 = new bootstrap.Tab(tabItemsBtn);
                bootstrapTab2.show();

                await loadItems(movId);
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: result.message || 'Cabecera guardada correctamente. Proceda a ingresar los insumos.',
                    showConfirmButton: false,
                    timer: 3000
                });
            } else {
                Swal.fire('Error', result.message || 'No se pudo guardar la cabecera.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al guardar.', 'error');
        } finally {
            btnSaveHdr.disabled = false;
            btnSaveHdr.innerHTML = '<i class="bi bi-save me-1"></i>Guardar y Continuar a Items';
        }
    });

    // Save Item Form
    formItem?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!itemPrdIdInput.value) {
            Swal.fire('Atención', 'Debe seleccionar un producto o insumo.', 'warning');
            itemPrdSearch.focus();
            return;
        }

        const cant = parseFloat(itemCantInput.value);
        if (isNaN(cant) || cant <= 0) {
            Swal.fire('Atención', 'Ingrese una cantidad válida mayor a 0.', 'warning');
            itemCantInput.focus();
            return;
        }

        btnSaveItem.disabled = true;
        try {
            const formData = new FormData(formItem);
            formData.set('MovimientoAlmacenId', idInput.value);
            formData.set('ContextoId', contextoInput.value);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const res = await fetch('/IV/ConsumoInt/GuardarProducto', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                resetItemForm();
                await loadItems(idInput.value);
            } else {
                Swal.fire('Error', result.message || 'No se pudo agregar el producto.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error al guardar el detalle del producto.', 'error');
        } finally {
            btnSaveItem.disabled = false;
        }
    });

    // Delete Item
    itemsTbody?.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-cint-del-item');
        if (!btn) return;
        const itemId = btn.dataset.id;

        const confirm = await Swal.fire({
            title: '¿Eliminar material?',
            text: 'Se retirará este insumo del consumo interno.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        });

        if (!confirm.isConfirmed) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch(`/IV/ConsumoInt/EliminarProducto?id=${itemId}&contextoId=${encodeURIComponent(contextoInput.value)}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                }
            });
            const result = await res.json();
            if (result.success) {
                await loadItems(idInput.value);
            } else {
                Swal.fire('Error', result.message || 'No se pudo eliminar el item.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error de comunicación al eliminar item.', 'error');
        }
    });

    // Solicitar Aprobación
    document.querySelectorAll('.js-cint-solicitar').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Enviar a Aprobación?',
                text: `El consumo interno ${codigo} pasará a estado PENDIENTE DE APROBACIÓN para el área de control.`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar a aprobación',
                cancelButtonText: 'Cancelar'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ConsumoInt/SolicitarAprobacion?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Enviado', result.message || 'Consumo enviado a aprobación exitosamente.', 'success').then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire('Error', result.message || 'No se pudo enviar a aprobación.', 'error');
                }
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Error de comunicación al solicitar aprobación.', 'error');
            }
        });
    });

    // Anular Consumo Interno
    document.querySelectorAll('.js-cint-anular').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Anular consumo interno?',
                text: `Se anulará el consumo ${codigo} y se revertirán los movimientos correspondientes.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, anular',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#dc3545'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ConsumoInt/Anular?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Anulado', result.message || 'Consumo interno anulado exitosamente.', 'success').then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire('Error', result.message || 'No se pudo anular el consumo interno.', 'error');
                }
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Error de comunicación al anular consumo.', 'error');
            }
        });
    });

    // Reload page on modal dismiss if saved changes were made
    modalEl.addEventListener('hidden.bs.modal', () => {
        if (idInput.value && idInput.value !== '0') {
            window.location.reload();
        }
    });
})();

