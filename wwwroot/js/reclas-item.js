(() => {
    'use strict';

    const modalEl = document.getElementById('recla-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);

    const itemModalEl = document.getElementById('recla-item-modal');
    const itemModal = itemModalEl ? new bootstrap.Modal(itemModalEl) : null;

    const formHdr = document.getElementById('recla-form');
    const formItemModal = document.getElementById('recla-item-modal-form');
    const tabHdrBtn = document.getElementById('tab-recla-hdr');
    const tabItemsBtn = document.getElementById('tab-recla-items');
    const tabDetailLi = document.querySelector('.js-recla-tab-detail');

    const idInput = document.getElementById('recla-id');
    const contextoInput = document.getElementById('recla-contexto-id');
    const revisionInput = document.getElementById('recla-revision');
    const codigoInput = document.getElementById('recla-codigo');
    const fechaInput = document.getElementById('recla-fecha');
    const almacenSelect = document.getElementById('recla-almacen-id');
    const movIdInput = document.getElementById('recla-mov-id');
    const movSearch = document.getElementById('recla-mov-search');
    const movResults = document.getElementById('recla-mov-results');
    const movSelected = document.getElementById('recla-mov-selected');
    const movSelectedCode = document.getElementById('recla-mov-selected-code');
    const movSelectedName = document.getElementById('recla-mov-selected-name');
    const obsInput = document.getElementById('recla-obs');
    const btnSaveHdr = document.getElementById('btn-save-recla-hdr');

    const itemsTbody = document.getElementById('recla-items-tbody');

    // Submodal inputs
    const itemMapIdInput = document.getElementById('recla-item-map-id');
    const itemDestIdInput = document.getElementById('recla-item-dest-id');
    const itemOrigName = document.getElementById('recla-item-orig-name');
    const itemOrigCant = document.getElementById('recla-item-orig-cant');
    const itemDestSearch = document.getElementById('recla-item-dest-search');
    const itemDestResults = document.getElementById('recla-item-dest-results');
    const itemDestSelected = document.getElementById('recla-item-dest-selected');
    const itemDestBadge = document.getElementById('recla-item-dest-badge');
    const itemDestNameLabel = document.getElementById('recla-item-dest-name-label');
    const itemDestCant = document.getElementById('recla-item-dest-cant');
    const btnSaveDest = document.getElementById('btn-save-recla-dest');

    function formatNumber(num, dec = 2) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
    }

    // Movimiento Autocomplete
    function selectMovimiento(id, codigo, nombre) {
        if (!movIdInput) return;
        movIdInput.value = id;
        movSelectedCode.textContent = codigo;
        movSelectedName.textContent = nombre;
        movResults.classList.add('d-none');
        movSearch.parentElement.classList.add('d-none');
        movSelected.classList.remove('d-none');
    }

    function clearMovimiento() {
        if (!movIdInput) return;
        movIdInput.value = '';
        movSearch.value = '';
        movSelectedCode.textContent = '';
        movSelectedName.textContent = '';
        movSelected.classList.add('d-none');
        movSearch.parentElement.classList.remove('d-none');
        movResults.classList.add('d-none');
        renderItems([]);
    }

    document.querySelector('.js-recla-clear-mov')?.addEventListener('click', clearMovimiento);

    let movSearchTimer = null;
    movSearch?.addEventListener('input', () => {
        clearTimeout(movSearchTimer);
        const almId = almacenSelect.value;
        if (!almId) {
            Swal.fire('Atención', 'Seleccione primero el almacén.', 'warning');
            movSearch.value = '';
            almacenSelect.focus();
            return;
        }

        const q = movSearch.value.trim();
        movSearchTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/ReclasItem/BuscarMovimientos?almacenId=${almId}&q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (!result.success || !result.data || result.data.length === 0) {
                    movResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron movimientos disponibles.</div>';
                    movResults.classList.remove('d-none');
                    return;
                }

                movResults.innerHTML = result.data.map(m => `
                    <button type="button" class="list-group-item list-group-item-action py-2 px-3 js-recla-pick-mov"
                            data-id="${m.id}" data-codigo="${m.codigo}" data-nombre="${m.nombre}">
                        <div class="fw-semibold small font-monospace text-primary">${m.codigo}</div>
                        <small class="text-muted">${m.nombre}</small>
                    </button>
                `).join('');
                movResults.classList.remove('d-none');
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    movResults?.addEventListener('click', (e) => {
        const item = e.target.closest('.js-recla-pick-mov');
        if (!item) return;
        selectMovimiento(item.dataset.id, item.dataset.codigo, item.dataset.nombre);
    });

    // Submodal Producto Destino Autocomplete
    function selectProductoDestino(id, codigo, descripcion) {
        itemDestIdInput.value = id;
        itemDestBadge.textContent = codigo;
        itemDestNameLabel.textContent = descripcion;
        itemDestResults.classList.add('d-none');
        itemDestSearch.parentElement.classList.add('d-none');
        itemDestSelected.classList.remove('d-none');
        itemDestCant?.focus();
    }

    function clearProductoDestino() {
        itemDestIdInput.value = '';
        itemDestSearch.value = '';
        itemDestBadge.textContent = '';
        itemDestNameLabel.textContent = '';
        itemDestSelected.classList.add('d-none');
        itemDestSearch.parentElement.classList.remove('d-none');
        itemDestResults.classList.add('d-none');
    }

    document.querySelector('.js-recla-clear-dest')?.addEventListener('click', clearProductoDestino);

    let prdSearchTimer = null;
    itemDestSearch?.addEventListener('input', () => {
        clearTimeout(prdSearchTimer);
        const q = itemDestSearch.value.trim();
        if (q.length < 2) {
            itemDestResults.classList.add('d-none');
            itemDestResults.innerHTML = '';
            return;
        }

        prdSearchTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/IV/ReclasItem/BuscarProductos?q=${encodeURIComponent(q)}`);
                const result = await res.json();
                if (!result.success || !result.data || result.data.length === 0) {
                    itemDestResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron productos.</div>';
                    itemDestResults.classList.remove('d-none');
                    return;
                }

                itemDestResults.innerHTML = result.data.map(p => `
                    <button type="button" class="list-group-item list-group-item-action py-2 px-3 js-recla-pick-dest-prd"
                            data-id="${p.id}" data-cod="${p.codigo}" data-nom="${p.descripcion}">
                        <div class="fw-semibold small"><span class="badge bg-secondary font-monospace me-1">${p.codigo}</span> ${p.descripcion}</div>
                        <small class="text-muted">U.M: ${p.unidadMedida}</small>
                    </button>
                `).join('');
                itemDestResults.classList.remove('d-none');
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    itemDestResults?.addEventListener('click', (e) => {
        const item = e.target.closest('.js-recla-pick-dest-prd');
        if (!item) return;
        selectProductoDestino(item.dataset.id, item.dataset.cod, item.dataset.nom);
    });

    // Reset modals
    function resetForm() {
        formHdr.reset();
        idInput.value = '0';
        revisionInput.value = '';
        codigoInput.value = '';
        fechaInput.value = new Date().toISOString().split('T')[0];
        almacenSelect.disabled = false;
        clearMovimiento();

        const bootstrapTab1 = new bootstrap.Tab(tabHdrBtn);
        bootstrapTab1.show();
        tabDetailLi?.classList.add('d-none');

        renderItems([]);
    }

    // Load Items for Movement
    async function loadItems(movAlmId) {
        if (!itemsTbody || !movAlmId || movAlmId <= 0) return;
        itemsTbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando productos...</td></tr>';
        try {
            const res = await fetch(`/IV/ReclasItem/Productos?movimientoAlmacenId=${movAlmId}`);
            const result = await res.json();
            if (result.success) {
                renderItems(result.data || []);
            } else {
                itemsTbody.innerHTML = `<tr><td colspan="9" class="text-danger text-center py-2">${result.message || 'Error al cargar items.'}</td></tr>`;
            }
        } catch (e) {
            console.error(e);
            itemsTbody.innerHTML = '<tr><td colspan="9" class="text-danger text-center py-2">Error de comunicación.</td></tr>';
        }
    }

    function renderItems(items) {
        if (!itemsTbody) return;
        if (!items || items.length === 0) {
            itemsTbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No hay productos en el movimiento seleccionado.</td></tr>';
            return;
        }

        itemsTbody.innerHTML = items.map(item => {
            const tieneRecla = item.productoDestinoId && item.productoDestinoId > 0;
            return `
                <tr>
                    <td class="font-monospace fw-semibold">${item.productoOrigenCodigo}</td>
                    <td>${item.productoOrigenNombre}</td>
                    <td class="text-center"><span class="badge bg-light text-dark border">${item.unidadMedidaOrigen}</span></td>
                    <td class="text-end font-monospace fw-bold">${formatNumber(item.cantidadOrigen)}</td>
                    <td class="font-monospace text-primary fw-semibold">${item.productoDestinoCodigo || '-'}</td>
                    <td>${item.productoDestinoNombre ? `<span class="fw-semibold text-primary">${item.productoDestinoNombre}</span>` : '<span class="text-muted fst-italic">Sin asignar</span>'}</td>
                    <td class="text-center">${item.unidadMedidaDestino ? `<span class="badge bg-light text-dark border">${item.unidadMedidaDestino}</span>` : '-'}</td>
                    <td class="text-end font-monospace fw-bold ${tieneRecla ? 'text-success' : 'text-muted'}">${tieneRecla ? formatNumber(item.cantidadDestino) : '-'}</td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm btn-outline-primary js-recla-set-dest"
                                data-id="${item.movimientoAlmacenProductoId}"
                                data-orig-name="${item.productoOrigenNombre} (${item.productoOrigenCodigo})"
                                data-orig-cant="${item.cantidadOrigen} ${item.unidadMedidaOrigen}"
                                data-dest-id="${item.productoDestinoId || ''}"
                                data-dest-cod="${item.productoDestinoCodigo || ''}"
                                data-dest-nom="${item.productoDestinoNombre || ''}"
                                data-dest-cant="${item.cantidadDestino || item.cantidadOrigen}"
                                title="Asignar reclasificación">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                    </td>
                </tr>
            `;
        }).join('');
    }

    // New Reclasificación
    document.querySelector('.js-recla-new')?.addEventListener('click', () => {
        resetForm();
        document.getElementById('recla-modal-title').textContent = 'Nueva Reclasificación de Items';
        document.getElementById('recla-modal-subtitle').textContent = 'Seleccione el almacén y el movimiento de origen para reclasificar productos';
        modal.show();
    });

    // Edit Reclasificación
    document.querySelectorAll('.js-recla-edit, .js-recla-view').forEach(btn => {
        btn.addEventListener('click', async () => {
            resetForm();
            const id = btn.dataset.id;
            try {
                const res = await fetch(`/IV/ReclasItem/Obtener?id=${id}`);
                const result = await res.json();
                if (!result.success || !result.data) {
                    Swal.fire('Error', result.message || 'No se pudo cargar la reclasificación.', 'error');
                    return;
                }

                const d = result.data;
                idInput.value = d.id;
                revisionInput.value = d.revision || '';
                codigoInput.value = d.codigo || '';
                fechaInput.value = d.fecha ? d.fecha.split('T')[0] : '';
                almacenSelect.value = d.almacenId || '';
                almacenSelect.disabled = true;

                selectMovimiento(d.movimientoAlmacenId, d.movimientoAlmacenCodigo, d.clienteProveedorNombre);
                obsInput.value = d.observacion || '';

                tabDetailLi?.classList.remove('d-none');
                document.getElementById('recla-modal-title').textContent = `Reclasificación: ${d.codigo}`;
                document.getElementById('recla-modal-subtitle').textContent = `Estado: ${d.estadoMovimientoReclasificacionNombre} · Movimiento: ${d.movimientoAlmacenCodigo}`;

                renderItems(d.productos || []);
                modal.show();
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Ocurrió un error al cargar la reclasificación.', 'error');
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

        if (!movIdInput.value || movIdInput.value === '0') {
            Swal.fire('Atención', 'Debe seleccionar el movimiento de almacén a reclasificar.', 'warning');
            movSearch.focus();
            return;
        }

        btnSaveHdr.disabled = true;
        btnSaveHdr.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const formData = new FormData(formHdr);
            if (almacenSelect.disabled) {
                formData.set('AlmacenId', almacenSelect.value);
            }
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const res = await fetch('/IV/ReclasItem/Guardar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                const recId = result.id || idInput.value;
                idInput.value = recId;
                almacenSelect.disabled = true;

                tabDetailLi?.classList.remove('d-none');
                const bootstrapTab2 = new bootstrap.Tab(tabItemsBtn);
                bootstrapTab2.show();

                await loadItems(movIdInput.value);
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: result.message || 'Cabecera guardada. Proceda a configurar los productos destino.',
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
            btnSaveHdr.innerHTML = '<i class="bi bi-save me-1"></i>Guardar y Asignar Productos';
        }
    });

    // Open Assign Destination Submodal
    itemsTbody?.addEventListener('click', (e) => {
        const btn = e.target.closest('.js-recla-set-dest');
        if (!btn || !itemModal) return;

        itemMapIdInput.value = btn.dataset.id;
        itemOrigName.textContent = btn.dataset.origName;
        itemOrigCant.textContent = `Cantidad Original: ${btn.dataset.origCant}`;

        if (btn.dataset.destId) {
            selectProductoDestino(btn.dataset.destId, btn.dataset.destCod, btn.dataset.destNom);
        } else {
            clearProductoDestino();
        }

        itemDestCant.value = btn.dataset.destCant || '';
        itemModal.show();
    });

    // Save Destination Product Submodal
    formItemModal?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!itemDestIdInput.value) {
            Swal.fire('Atención', 'Seleccione el producto de destino.', 'warning');
            itemDestSearch.focus();
            return;
        }

        const cant = parseFloat(itemDestCant.value);
        if (isNaN(cant) || cant <= 0) {
            Swal.fire('Atención', 'Ingrese una cantidad válida mayor a 0.', 'warning');
            itemDestCant.focus();
            return;
        }

        btnSaveDest.disabled = true;
        try {
            const formData = new FormData();
            formData.append('MovimientoAlmacenProductoId', itemMapIdInput.value);
            formData.append('ContextoId', contextoInput.value);
            formData.append('ProductoDestinoId', itemDestIdInput.value);
            formData.append('CantidadDestino', itemDestCant.value);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/IV/ReclasItem/GuardarProducto', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                itemModal.hide();
                await loadItems(movIdInput.value);
            } else {
                Swal.fire('Error', result.message || 'No se pudo guardar la reclasificación del item.', 'error');
            }
        } catch (e) {
            console.error(e);
            Swal.fire('Error', 'Error al guardar.', 'error');
        } finally {
            btnSaveDest.disabled = false;
        }
    });

    // Solicitar Aprobación
    document.querySelectorAll('.js-recla-solicitar').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Enviar a Aprobación?',
                text: `La reclasificación ${codigo} pasará a estado PENDIENTE DE APROBACIÓN.`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar a aprobación',
                cancelButtonText: 'Cancelar'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ReclasItem/SolicitarAprobacion?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Enviado', result.message || 'Reclasificación enviada a aprobación correctamente.', 'success').then(() => {
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

    // Anular Reclasificación
    document.querySelectorAll('.js-recla-anular').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = btn.dataset.id;
            const codigo = btn.dataset.codigo;
            const revision = btn.dataset.revision;
            const contexto = btn.dataset.contexto;

            const confirm = await Swal.fire({
                title: '¿Anular Reclasificación?',
                text: `Se anulará el registro ${codigo}.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, anular',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#dc3545'
            });

            if (!confirm.isConfirmed) return;

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const res = await fetch(`/IV/ReclasItem/Anular?id=${id}&revision=${encodeURIComponent(revision)}&contextoId=${encodeURIComponent(contexto)}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });
                const result = await res.json();
                if (result.success) {
                    Swal.fire('Anulado', result.message || 'Reclasificación anulada correctamente.', 'success').then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire('Error', result.message || 'No se pudo anular la reclasificación.', 'error');
                }
            } catch (e) {
                console.error(e);
                Swal.fire('Error', 'Error de comunicación al anular.', 'error');
            }
        });
    });

    modalEl.addEventListener('hidden.bs.modal', () => {
        if (idInput.value && idInput.value !== '0') {
            window.location.reload();
        }
    });
})();

