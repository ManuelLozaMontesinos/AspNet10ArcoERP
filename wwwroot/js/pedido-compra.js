// @ts-check
(function () {
    'use strict';

    const modalEl = document.getElementById('pedco-modal');
    if (!modalEl) return;

    // @ts-ignore
    const modal = new bootstrap.Modal(modalEl);

    const tokenForm = /** @type {HTMLFormElement} */ (document.getElementById('pedco-token-form'));
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
    const pedcoForm = /** @type {HTMLFormElement} */ (document.getElementById('pedco-form'));
    const hdrId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-id'));
    const hdrContextoId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-contexto-id'));
    const hdrRev = /** @type {HTMLInputElement} */ (document.getElementById('pedco-revision'));
    const hdrCodigo = /** @type {HTMLInputElement} */ (document.getElementById('pedco-codigo'));
    const hdrFecha = /** @type {HTMLInputElement} */ (document.getElementById('pedco-fecha'));
    const hdrTipoProd = /** @type {HTMLSelectElement} */ (document.getElementById('pedco-tipo-prod'));
    const hdrProvId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-prov-id'));
    const hdrProvText = /** @type {HTMLInputElement} */ (document.getElementById('pedco-prov-text'));
    const provResults = document.getElementById('pedco-prov-results');
    const hdrObserva = /** @type {HTMLTextAreaElement} */ (document.getElementById('pedco-observa'));
    const estadoBadge = document.getElementById('pedco-estado-badge');
    const aprobBadge = document.getElementById('pedco-aprob-badge');
    const btnRequestAuth = document.getElementById('pedco-btn-request-auth');

    const modalTitle = document.getElementById('pedco-modal-title');
    const modalSubtitle = document.getElementById('pedco-modal-subtitle');
    const tabDetail = document.querySelector('.js-pedco-tab-detail');

    // Item elements
    const itemForm = /** @type {HTMLFormElement} */ (document.getElementById('pedco-item-form'));
    const itemId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-id'));
    const itemPedId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-ped-id'));
    const itemProdId = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-prod-id'));
    const itemProdSearch = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-prod-search'));
    const itemProdResults = document.getElementById('pedco-item-prod-results');
    const itemDesc = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-desc'));
    const itemCant = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-cant'));
    const itemPNeto = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-pneto'));
    const itemPBruto = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-pbruto'));
    const itemStockProv = /** @type {HTMLInputElement} */ (document.getElementById('pedco-item-stock-prov'));
    const itemSubmitLbl = document.getElementById('pedco-item-submit-lbl');
    const btnCancelItem = document.querySelector('.js-pedco-cancel-item');
    const itemsTableBody = document.querySelector('#pedco-items-table tbody');

    let searchProvTimer = null;
    let searchProdTimer = null;
    let currentPedidoId = 0;
    let currentCabecera = null;

    // Proveedor search autocomplete
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

    // Producto search autocomplete
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

    // Reset modals
    function resetModal() {
        currentPedidoId = 0;
        currentCabecera = null;
        if (pedcoForm) pedcoForm.reset();
        hdrId.value = '0';
        hdrRev.value = '';
        hdrCodigo.value = '[Autogenerado]';
        hdrFecha.value = new Date().toISOString().substring(0, 10);
        hdrProvId.value = '';
        hdrProvText.value = '';
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

        // Activate Tab 1
        const tabHdr = document.getElementById('tab-pedco-hdr');
        if (tabHdr) {
            // @ts-ignore
            new bootstrap.Tab(tabHdr).show();
        }

        resetItemEditor();
        if (itemsTableBody) {
            itemsTableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No hay productos agregados.</td></tr>';
        }
    }

    function resetItemEditor() {
        if (!itemForm) return;
        itemForm.reset();
        itemId.value = '0';
        itemProdId.value = '';
        itemCant.value = '1.0000';
        itemPNeto.value = '0.0000';
        itemPBruto.value = '0.0000';
        if (itemStockProv) itemStockProv.checked = false;
        if (itemSubmitLbl) itemSubmitLbl.textContent = 'Agregar Producto';
        if (btnCancelItem) btnCancelItem.classList.add('d-none');
    }

    if (btnCancelItem) {
        btnCancelItem.addEventListener('click', resetItemEditor);
    }

    // Load Detalle for modal
    async function abrirPedido(id) {
        resetModal();
        try {
            const resp = await fetch(`/cm/pedidoCompra/obtener?id=${id}`);
            const res = await resp.json();
            if (!res.success || !res.data) {
                notify('error', res.message || 'No se pudo cargar el requerimiento.');
                return;
            }

            const data = res.data;
            const cab = data.cabecera;
            currentPedidoId = cab.id;
            currentCabecera = cab;

            // Populate TiposProducto select
            if (hdrTipoProd && data.tiposProducto) {
                hdrTipoProd.innerHTML = '<option value="">Seleccione...</option>' +
                    data.tiposProducto.map(tp => `<option value="${tp.id}" ${tp.id === cab.tipoProductoId ? 'selected' : ''}>${tp.descripcion}</option>`).join('');
            }

            if (cab.id > 0) {
                modalTitle.textContent = `Requerimiento N° ${cab.codigo}`;
                modalSubtitle.textContent = `Registrado el ${cab.fechaTexto} · Estado: ${cab.estadoPedidoCompraNombre}`;
                hdrId.value = String(cab.id);
                hdrRev.value = cab.revision || '';
                hdrCodigo.value = cab.codigo;
                hdrFecha.value = cab.fecha.substring(0, 10);
                hdrProvId.value = String(cab.proveedorId);
                hdrProvText.value = cab.proveedorNombre + (cab.proveedorDocumento ? ` (${cab.proveedorDocumento})` : '');
                hdrObserva.value = cab.observacion || '';

                if (estadoBadge) {
                    estadoBadge.textContent = cab.estadoPedidoCompraNombre;
                    estadoBadge.className = 'badge border ' + (
                        cab.estadoPedidoCompraCodigo === '01' ? 'bg-secondary-subtle text-muted border-secondary-subtle' :
                        cab.estadoPedidoCompraCodigo === '02' ? 'bg-info-subtle text-info border-info-subtle' :
                        cab.estadoPedidoCompraCodigo === '03' ? 'bg-success-subtle text-success border-success-subtle' :
                        cab.estadoPedidoCompraCodigo === '04' ? 'bg-danger-subtle text-danger border-danger-subtle' : 'bg-light text-dark'
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
                    if (cab.estadoPedidoCompraCodigo === '01' || cab.estadoAprobacionCodigo === '03') {
                        btnRequestAuth.classList.remove('d-none');
                    } else {
                        btnRequestAuth.classList.add('d-none');
                    }
                }

                if (tabDetail) /** @type {HTMLElement} */(tabDetail).style.display = '';
                if (itemPedId) itemPedId.value = String(cab.id);

                renderProductos(data.productos || []);
            } else {
                modalTitle.textContent = 'Nuevo Requerimiento de Compra';
                modalSubtitle.textContent = 'Complete la información general y luego agregue los insumos o productos.';
                if (cab.tipoProductoId && hdrTipoProd) hdrTipoProd.value = String(cab.tipoProductoId);
                if (cab.proveedorId) {
                    hdrProvId.value = String(cab.proveedorId);
                    hdrProvText.value = cab.proveedorNombre;
                }
            }

            modal.show();
        } catch (e) {
            console.error('Error al abrir requerimiento:', e);
            notify('error', 'Error inesperado al cargar el requerimiento.');
        }
    }

    function renderProductos(productos) {
        if (!itemsTableBody) return;
        if (!productos || productos.length === 0) {
            itemsTableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">No hay productos agregados.</td></tr>';
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
                <td class="text-end font-monospace small">${Number(item.pesoNeto || 0).toFixed(2)}</td>
                <td class="text-end font-monospace small">${Number(item.pesoBruto || 0).toFixed(2)}</td>
                <td class="text-center">
                    ${item.stockEnProveedor === 'S' ? '<span class="badge bg-success-subtle text-success">SÍ</span>' : '<span class="text-muted">NO</span>'}
                </td>
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
        if (!currentPedidoId) return;
        try {
            const resp = await fetch(`/cm/pedidoCompra/productos?pedidoCompraId=${currentPedidoId}`);
            const res = await resp.json();
            if (res.success) {
                renderProductos(res.data);
            }
        } catch (e) {
            console.error('Error al recargar productos:', e);
        }
    }

    // New button
    document.querySelector('.js-pedco-new')?.addEventListener('click', () => abrirPedido(0));

    // Open existing buttons
    document.addEventListener('click', function (e) {
        const btnOpen = /** @type {HTMLElement} */ (e.target).closest('.js-pedco-open');
        if (btnOpen) {
            const id = Number(btnOpen.dataset.id) || 0;
            abrirPedido(id);
            return;
        }

        // Send to approval from listing
        const btnAuth = /** @type {HTMLElement} */ (e.target).closest('.js-pedco-send-auth');
        if (btnAuth) {
            const id = Number(btnAuth.dataset.id) || 0;
            const rev = btnAuth.dataset.revision || '';
            const code = btnAuth.dataset.code || '';
            enviarAprobacion(id, rev, code);
            return;
        }

        // Delete from listing
        const btnDel = /** @type {HTMLElement} */ (e.target).closest('.js-pedco-delete');
        if (btnDel) {
            const id = Number(btnDel.dataset.id) || 0;
            const rev = btnDel.dataset.revision || '';
            const code = btnDel.dataset.code || '';
            anularPedido(id, rev, code);
            return;
        }

        // Edit item in modal
        const btnEditItem = /** @type {HTMLElement} */ (e.target).closest('.js-edit-item');
        if (btnEditItem) {
            const raw = btnEditItem.dataset.item;
            if (raw) {
                const item = JSON.parse(raw);
                itemId.value = String(item.id);
                itemProdId.value = item.productoId ? String(item.productoId) : '';
                itemProdSearch.value = item.productoCodigo ? `${item.productoCodigo} - ${item.descripcion}` : item.descripcion;
                itemDesc.value = item.descripcion;
                itemCant.value = String(item.cantidad);
                itemPNeto.value = String(item.pesoNeto || 0);
                itemPBruto.value = String(item.pesoBruto || 0);
                if (itemStockProv) itemStockProv.checked = item.stockEnProveedor === 'S';
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
    pedcoForm?.addEventListener('submit', async function (e) {
        e.preventDefault();
        if (!hdrProvId.value || Number(hdrProvId.value) <= 0) {
            notify('warning', 'Debe seleccionar un proveedor válido.');
            return;
        }

        const formData = new FormData(pedcoForm);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/pedidoCompra/guardar', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Cabecera guardada.');
                abrirPedido(res.id || currentPedidoId);
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
        if (!currentPedidoId) {
            notify('warning', 'Guarde la cabecera del requerimiento antes de agregar productos.');
            return;
        }
        itemPedId.value = String(currentPedidoId);

        const formData = new FormData(itemForm);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        try {
            const resp = await fetch('/cm/pedidoCompra/guardarProducto', {
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
                text: 'El item seleccionado se removerá del requerimiento.',
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
            const resp = await fetch('/cm/pedidoCompra/eliminarProducto', {
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
        if (!currentPedidoId || !currentCabecera) return;
        enviarAprobacion(currentPedidoId, currentCabecera.revision, currentCabecera.codigo);
    });

    async function enviarAprobacion(id, revision, codigo) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Solicitar aprobación del Requerimiento ${codigo}?`,
                text: 'El requerimiento pasará a estado PEDIDO y quedará PENDIENTE DE APROBACIÓN.',
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
            const resp = await fetch('/cm/pedidoCompra/solicitarAprobacion', {
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

    // Anular requerimiento
    async function anularPedido(id, revision, codigo) {
        // @ts-ignore
        if (window.Swal) {
            // @ts-ignore
            const confirm = await Swal.fire({
                title: `¿Anular Requerimiento ${codigo}?`,
                text: 'Esta acción cancelará el requerimiento de compra.',
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
            const resp = await fetch('/cm/pedidoCompra/anular', {
                method: 'POST',
                body: formData
            });
            const res = await resp.json();
            if (res.success) {
                notify('success', res.message || 'Requerimiento anulado.');
                setTimeout(() => window.location.reload(), 800);
            } else {
                notify('error', res.message || 'No se pudo anular el requerimiento.');
            }
        } catch (e) {
            console.error('Error al anular requerimiento:', e);
            notify('error', 'Error de comunicación.');
        }
    }

})();

