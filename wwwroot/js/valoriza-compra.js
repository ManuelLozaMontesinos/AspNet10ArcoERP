(() => {
    'use strict';

    const modalEl = document.getElementById('valco-modal');
    if (!modalEl) return;
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById('valco-form');

    const idInput = document.getElementById('valco-id');
    const contextoInput = document.getElementById('valco-contexto-id');
    const revisionInput = document.getElementById('valco-revision');
    const ocIdInput = document.getElementById('valco-oc-id');

    const ocSearchInput = document.getElementById('valco-oc-search');
    const ocResults = document.getElementById('valco-oc-results');
    const ocSelectedBox = document.getElementById('valco-oc-selected');
    const ocCodigoBadge = document.getElementById('valco-oc-codigo-badge');
    const ocProveedorText = document.getElementById('valco-oc-proveedor-text');
    const ocMontoText = document.getElementById('valco-oc-monto-text');

    const tipoDocSelect = document.getElementById('valco-tipo-doc');
    const serieInput = document.getElementById('valco-serie');
    const numeroInput = document.getElementById('valco-numero');
    const fechaInput = document.getElementById('valco-fecha');
    const fechaVctoInput = document.getElementById('valco-fecha-vcto');
    const condPagoSelect = document.getElementById('valco-cond-pago');
    const montoTotalInput = document.getElementById('valco-monto-total');
    const monedaSimbolo = document.getElementById('valco-moneda-simbolo');
    const subtotalCalc = document.getElementById('valco-subtotal-calc');
    const igvCalc = document.getElementById('valco-igv-calc');
    const btnSave = document.getElementById('valco-btn-save');

    function formatNumber(num) {
        return (Number(num) || 0).toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function recalculateTaxes() {
        const total = parseFloat(montoTotalInput.value) || 0;
        const subtotal = total > 0 ? (total / 1.18) : 0;
        const igv = total - subtotal;
        const simbolo = monedaSimbolo.textContent || 'S/';
        subtotalCalc.textContent = `${simbolo} ${formatNumber(subtotal)}`;
        igvCalc.textContent = `${simbolo} ${formatNumber(igv)}`;
    }

    montoTotalInput.addEventListener('input', recalculateTaxes);

    function selectOrdenCompra(id, codigo, proveedor, extra) {
        ocIdInput.value = id;
        ocCodigoBadge.textContent = `O/C: ${codigo}`;
        ocProveedorText.textContent = proveedor;
        ocMontoText.textContent = extra ? `(${extra.split('|')[0]})` : '';
        ocResults.classList.add('d-none');
        ocSearchInput.parentElement.classList.add('d-none');
        ocSelectedBox.classList.remove('d-none');

        if (extra && extra.includes('|')) {
            const condId = extra.split('|')[1];
            if (condId && condId !== '0' && condPagoSelect) {
                condPagoSelect.value = condId;
            }
        }
    }

    function clearOrdenCompra() {
        ocIdInput.value = '';
        ocSearchInput.value = '';
        ocSearchInput.parentElement.classList.remove('d-none');
        ocSelectedBox.classList.add('d-none');
        ocResults.classList.add('d-none');
    }

    document.querySelector('.js-valco-clear-oc')?.addEventListener('click', clearOrdenCompra);

    let debounceTimer = null;
    ocSearchInput?.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        const query = ocSearchInput.value.trim();
        if (query.length < 2) {
            ocResults.classList.add('d-none');
            ocResults.innerHTML = '';
            return;
        }

        debounceTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/CM/ValorizaCompra/BuscarOrdenes?q=${encodeURIComponent(query)}`);
                const result = await res.json();
                if (result.success && result.data && result.data.length > 0) {
                    ocResults.innerHTML = result.data.map(item => `
                        <button type="button" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center py-2" data-id="${item.id}" data-codigo="${item.codigo}" data-desc="${item.descripcion}" data-extra="${item.extra || ''}">
                            <div>
                                <span class="badge bg-secondary me-2">${item.codigo}</span>
                                <strong>${item.descripcion}</strong>
                            </div>
                            <span class="text-muted small">${item.extra ? item.extra.split('|')[0] : ''}</span>
                        </button>
                    `).join('');
                    ocResults.classList.remove('d-none');
                } else {
                    ocResults.innerHTML = '<div class="list-group-item text-muted small py-2">No se encontraron órdenes de compra coincidentes</div>';
                    ocResults.classList.remove('d-none');
                }
            } catch (e) {
                console.error(e);
            }
        }, 300);
    });

    ocResults?.addEventListener('click', (e) => {
        const btn = e.target.closest('button.list-group-item');
        if (!btn) return;
        selectOrdenCompra(btn.dataset.id, btn.dataset.codigo, btn.dataset.desc, btn.dataset.extra);
    });

    document.addEventListener('click', (e) => {
        if (!ocSearchInput?.contains(e.target) && !ocResults?.contains(e.target)) {
            ocResults?.classList.add('d-none');
        }
    });

    document.querySelector('.js-valco-new')?.addEventListener('click', () => {
        form.reset();
        idInput.value = '0';
        revisionInput.value = '';
        clearOrdenCompra();
        document.getElementById('valco-oc-section')?.classList.remove('d-none');
        document.querySelector('.js-valco-clear-oc')?.classList.remove('d-none');
        const today = new Date().toISOString().split('T')[0];
        fechaInput.value = today;
        document.getElementById('valcoModalLabel').innerHTML = '<i class="bi bi-receipt me-2"></i>Registrar Comprobante de Compra';
        recalculateTaxes();
        modal.show();
    });

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-valco-edit');
        if (!btn) return;
        const id = btn.dataset.id;
        try {
            const res = await fetch(`/CM/ValorizaCompra/Obtener?id=${id}`);
            const result = await res.json();
            if (!result.success || !result.data) {
                Swal.fire({ icon: 'warning', title: 'Atención', text: result.message || 'No se pudo cargar el comprobante.' });
                return;
            }
            const data = result.data;
            idInput.value = data.id;
            revisionInput.value = data.revision || '';
            selectOrdenCompra(data.ordenCompraId, data.ordenCompraCodigo, data.proveedorNombre, `${data.monedaSimbolo} ${data.montoTotal}`);
            document.querySelector('.js-valco-clear-oc')?.classList.add('d-none');

            tipoDocSelect.value = data.tipoDocumentoComercialId;
            serieInput.value = data.serie;
            numeroInput.value = data.numero;
            if (data.fecha) fechaInput.value = data.fecha.split('T')[0];
            if (data.fechaVencimiento) fechaVctoInput.value = data.fechaVencimiento.split('T')[0];
            else fechaVctoInput.value = '';
            if (data.condicionPagoId) condPagoSelect.value = data.condicionPagoId;
            else condPagoSelect.value = '';
            montoTotalInput.value = data.montoTotal;
            monedaSimbolo.textContent = data.monedaSimbolo || 'S/';
            recalculateTaxes();

            document.getElementById('valcoModalLabel').innerHTML = `<i class="bi bi-pencil-square me-2"></i>Editar Comprobante: ${data.documentoCompleto}`;
            modal.show();
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al consultar datos del comprobante.' });
        }
    });

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!ocIdInput.value || ocIdInput.value === '0') {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Debe seleccionar una orden de compra vinculada.' });
            return;
        }
        if (!tipoDocSelect.value) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Seleccione el tipo de comprobante.' });
            return;
        }
        if (!serieInput.value.trim() || !numeroInput.value.trim()) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Ingrese la serie y el número del comprobante.' });
            return;
        }
        const total = parseFloat(montoTotalInput.value);
        if (!total || total <= 0) {
            Swal.fire({ icon: 'warning', title: 'Validación', text: 'Ingrese un monto total válido superior a cero.' });
            return;
        }

        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Guardando...';

        try {
            const formData = new FormData(form);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const res = await fetch('/CM/ValorizaCompra/Guardar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                modal.hide();
                await Swal.fire({ icon: 'success', title: 'Éxito', text: result.message, timer: 1500, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: result.message || 'Ocurrió un error al guardar el comprobante.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
        } finally {
            btnSave.disabled = false;
            btnSave.innerHTML = '<i class="bi bi-check-lg me-1"></i>Guardar Comprobante';
        }
    });

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-valco-del');
        if (!btn) return;
        const id = btn.dataset.id;
        const doc = btn.dataset.doc;
        const contexto = btn.dataset.contexto;

        const confirm = await Swal.fire({
            title: '¿Eliminar comprobante?',
            text: `¿Está seguro de eliminar el comprobante ${doc}? Esta acción anulará el registro.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        });

        if (!confirm.isConfirmed) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const formData = new FormData();
            formData.append('id', id);
            formData.append('contextoId', contexto);

            const res = await fetch('/CM/ValorizaCompra/Eliminar', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token || ''
                },
                body: formData
            });

            const result = await res.json();
            if (result.success) {
                await Swal.fire({ icon: 'success', title: 'Eliminado', text: result.message, timer: 1400, showConfirmButton: false });
                window.location.reload();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudo eliminar el comprobante.' });
            }
        } catch (err) {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error al comunicarse con el servidor.' });
        }
    });
})();

