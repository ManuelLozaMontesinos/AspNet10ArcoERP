(() => {
    'use strict';

    const modalEl = document.getElementById('merma-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const formHdr = document.getElementById('merma-form');
    const formItem = document.getElementById('merma-item-form');
    const tabHdrBtn = document.getElementById('tab-merma-hdr');
    const tabItemsBtn = document.getElementById('tab-merma-items');
    const tabDetailLi = document.querySelector('.js-merma-tab-detail');

    const idInput = document.getElementById('merma-id');
    const contextoInput = document.getElementById('merma-contexto-id');
    const revisionInput = document.getElementById('merma-revision');
    const codigoInput = document.getElementById('merma-codigo');
    const tipoSelect = document.getElementById('merma-tipo-id');
    const fechaInput = document.getElementById('merma-fecha');
    const almacenSelect = document.getElementById('merma-almacen-id');
    const obsInput = document.getElementById('merma-obs');
    const btnSaveHdr = document.getElementById('btn-save-merma-hdr');

    // Item form inputs
    const itemIdInput = document.getElementById('merma-item-id');
    const itemMovIdInput = document.getElementById('merma-item-mov-id');
    const itemPrdIdInput = document.getElementById('merma-item-prd-id');
    const itemPrdSearch = document.getElementById('merma-item-prd-search');
    const itemPrdResults = document.getElementById('merma-item-prd-results');
    const itemPrdSelected = document.getElementById('merma-item-prd-selected');
    const itemPrdBadge = document.getElementById('merma-item-prd-badge');
    const itemPrdName = document.getElementById('merma-item-prd-name');
    const itemPrdUm = document.getElementById('merma-item-prd-um');
    const itemCantInput = document.getElementById('merma-item-cant');
    const itemValUInput = document.getElementById('merma-item-val-u');
    const btnSaveItem = document.getElementById('btn-save-merma-item');

    const itemsTbody = document.getElementById('merma-items-tbody');
    const itemsCount = document.getElementById('merma-items-count');
    const itemsTotal = document.getElementById('merma-items-total');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

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

    document.querySelector('.js-merma-clear-item-prd')?.addEventListener('click', clearProducto);

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
                const res = await fetch(`/IV/ControlMerma/BuscarProductos?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (!result.success || !result.data || result.data.length === 0) {
                    itemPrdResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron productos.</div>';
                    itemPrdResults.classList.remove('d-none');
                    return;
                }

                itemPrdResults.innerHTML = result.data.map(p => `
                    <button type="button" class="list-group-item list-group-item-action py-2 px-3 js-merma-pick-prd"
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
        const item = e.target.closest('.js-merma-pick-prd');
        if (!item) return;
        selectProducto(item.dataset.id, item.dataset.cod, item.dataset.nom, item.dataset.um);
    });

    // Reset forms
    function resetForm() {
        formHdr.reset();
        idInput.value = '0';
        revisionInput.value = '';
        codigoInput.value = '';
        tipoSelect.disabled = false;
        tipoSelect.value = '2'; // Default salida por merma
        fechaInput.value = new Date().toISOString().split('T')[0];

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

    // Load Items for Merma
    async function loadItems(movId) {
        if (!itemsTbody || !movId || movId <= 0) return;
        itemsTbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando materiales...</td></tr>';
        try {
            const res = await fetch(`/IV/ControlMerma/Productos?movimientoAlmacenId=${movId}`);
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
            itemsTbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">No hay productos registrados en este registro de merma.</td></tr>';
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
                    <td class="text-end font-monospace fw-bold text-danger">S/ ${formatNumber(rowTotal)}</td>
                    <td class="text-end">
                        <button type="button" class="btn btn-sm btn-link text-danger p-0 js-merma-del-item" data-id="${item.id}" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        }).join('');

        itemsCount.textContent = count.toString();
        itemsTotal.textContent = `S/ ${formatNumber(total)}`;
    }

    // New Merma button
    document.querySelector('.js-merma-new')?.addEventListener('click', () => {
        resetForm();
        document.getElementById('merma-modal-title').textContent = 'Nuevo Registro de Merma';
        document.getElementById('merma-modal-subtitle').textContent = 'Seleccione si es Ingreso (S07) o Salida (S08) por deterioro o calibración';
        modal.show();
    });

    // Edit Merma button
    document.querySelectorAll('.js-merma-edit').forEach(btn => {
        btn.addEventListener('click', async () => {
            resetForm();
            const id = btn.dataset.id;
            try {
                const res = await fetch(`/IV/ControlMerma/Obtener?id=${id}`);
                const result = await res.json();
                if (!result.success || !result.data) {
                    Swal.fire('Error', result.message || 'No se pudo cargar la información.', 'error');
                    return;
                }

                const d = result.data;
                idInput.value = d.id;
                revisionInput.value = d.revision || '';
                codigoInput.value = d.codigo || '';
                tipoSelect.value = d.tipoMovimientoAlmacenId || '2';
                tipoSelect.disabled = true; // Tipo locked on edit
                fechaInput.value = d.fecha ? d.fecha.split('T')[0] : '';
                almacenSelect.value = d.almacenId || '';
                obsInput.value = d.observacion || '';

                itemMovIdInput.value = d.id;
                tabDetailLi?.classList.remove('d-none');

                document.getElementById('merma-modal-title').textContent = `Control de Merma: ${d.codigo}`;
                document.getElementById('merma-modal-subtitle').textContent = `Tipo: ${d.tipoMovimientoAlmacenNombre} | Estado: ${d.estadoMovimientoAlmacenNombre} | Aprobación: ${d.estadoAprobacionNombre || 'PENDIENTE'}`;

                await loadItems(d.id);
                modal.show();
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Ocurrió un error al cargar la merma.', 'error');
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

        btnSaveHdr.disabled = true;
        btnSaveHdr.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const formData = new FormData(formHdr);
            if (tipoSelect.disabled) {
                formData.set('TipoMovimientoAlmacenId', tipoSelect.value);
            }
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const res = await fetch('/IV/ControlMerma/Guardar', {
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
                tipoSelect.disabled = true;

                tabDetailLi?.classList.remove('d-none');
                const bootstrapTab2 = new bootstrap.Tab(tabItemsBtn);
                bootstrapTab2.show();

                await loadItems(movId);
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: result.message || 'Cabecera guardada. Proceda a ingresar los productos de la merma.',
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

            const res = await fetch('/IV/ControlMerma/GuardarProducto', {
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
            Swal.fire('Error', 'Error al guardar el producto.', 'error');
        } finally {
            btnSaveItem.disabled = false;
        }
    });

    // Delete Item
    itemsTbody?.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-merma-del-item');
        if (!btn) return;
        const itemId = btn.dataset.id;

        const confirm = await Swal.fire({
            title: '¿Eliminar material?',
            text: 'Se retirará este producto del registro de merma.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        });

        if (!confirm.isConfirmed) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch(`/IV/ControlMerma/EliminarProducto?id=${itemId}&contextoId=${encodeURIComponent(contextoInput.value)}`, {
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
    document.querySelectorAll('.js-merma-solicitar').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Enviar a Aprobación?',
                text: `El registro de merma ${codigo} pasará a estado PENDIENTE DE APROBACIÓN.`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar a aprobación',
                cancelButtonText: 'Cancelar'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ControlMerma/SolicitarAprobacion?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Enviado', result.message || 'Merma enviada a aprobación exitosamente.', 'success').then(() => {
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

    // Anular Merma
    document.querySelectorAll('.js-merma-anular').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Anular registro de merma?',
                text: `Se anulará el registro ${codigo} y no afectará el kardex.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, anular',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#dc3545'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ControlMerma/Anular?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Anulado', result.message || 'Registro de merma anulado exitosamente.', 'success').then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire('Error', result.message || 'No se pudo anular la merma.', 'error');
                }
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Error de comunicación al anular merma.', 'error');
            }
        });
    });

    modalEl.addEventListener('hidden.bs.modal', () => {
        if (idInput.value && idInput.value !== '0') {
            window.location.reload();
        }
    });
})();

