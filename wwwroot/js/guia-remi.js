document.addEventListener('DOMContentLoaded', function () {
    const modalGuiaEl = document.getElementById('modalGuia');
    const modalGuia = modalGuiaEl ? new bootstrap.Modal(modalGuiaEl) : null;
    const modalBuscarMovEl = document.getElementById('modalBuscarMovimiento');
    const modalBuscarMov = modalBuscarMovEl ? new bootstrap.Modal(modalBuscarMovEl) : null;

    let itemsData = [];

    function fillSelect(selectId, items, selectedValue, defaultText = '-- Seleccione --') {
        const select = document.getElementById(selectId);
        if (!select) return;
        select.innerHTML = defaultText ? `<option value="">${defaultText}</option>` : '';
        (items || []).forEach(item => {
            const opt = document.createElement('option');
            opt.value = item.id !== undefined ? item.id : item.codigo;
            opt.textContent = item.descripcion || item.nombre || item.codigo;
            if (selectedValue !== null && selectedValue !== undefined && opt.value.toString() === selectedValue.toString()) {
                opt.selected = true;
            }
            select.appendChild(opt);
        });
    }

    async function cargarProvincias(depId, provSelectId, distSelectId, selProv = null, selDist = null) {
        const provSelect = document.getElementById(provSelectId);
        const distSelect = document.getElementById(distSelectId);
        if (!provSelect) return;
        provSelect.innerHTML = '<option value="">-- Cargando... --</option>';
        if (distSelect) distSelect.innerHTML = '<option value="">-- Seleccione provincia --</option>';

        if (!depId) {
            provSelect.innerHTML = '<option value="">-- Seleccione departamento --</option>';
            return;
        }

        try {
            const res = await fetch(`/AS/Empresa/GetProvincias?departamentoId=${depId}`);
            const data = await res.json();
            fillSelect(provSelectId, data, selProv, '-- Seleccione Provincia --');
            if (selProv && distSelect) {
                await cargarDistritos(selProv, distSelectId, selDist);
            }
        } catch (e) {
            console.error('Error cargando provincias:', e);
        }
    }

    async function cargarDistritos(provId, distSelectId, selDist = null) {
        const distSelect = document.getElementById(distSelectId);
        if (!distSelect) return;
        distSelect.innerHTML = '<option value="">-- Cargando... --</option>';

        if (!provId) {
            distSelect.innerHTML = '<option value="">-- Seleccione provincia --</option>';
            return;
        }

        try {
            const res = await fetch(`/AS/Empresa/GetDistritos?provinciaId=${provId}`);
            const data = await res.json();
            fillSelect(distSelectId, data, selDist, '-- Seleccione Distrito --');
        } catch (e) {
            console.error('Error cargando distritos:', e);
        }
    }

    // Ubigeo listeners
    document.getElementById('partidaDepId')?.addEventListener('change', function () {
        cargarProvincias(this.value, 'partidaProvId', 'partidaDistId');
    });
    document.getElementById('partidaProvId')?.addEventListener('change', function () {
        cargarDistritos(this.value, 'partidaDistId');
    });
    document.getElementById('llegadaDepId')?.addEventListener('change', function () {
        cargarProvincias(this.value, 'llegadaProvId', 'llegadaDistId');
    });
    document.getElementById('llegadaProvId')?.addEventListener('change', function () {
        cargarDistritos(this.value, 'llegadaDistId');
    });

    // Almacen change -> reload series
    document.getElementById('guiaAlmacenId')?.addEventListener('change', async function () {
        if (!this.value) return;
        try {
            const res = await fetch(`/IV/GuiaRemi/GetSeries?almacenId=${this.value}`);
            const series = await res.json();
            fillSelect('guiaSerie', series, series.length > 0 ? series[0].codigo : null, null);
        } catch (e) {
            console.error('Error al cargar series:', e);
        }
    });

    // Transportista change -> reload conductores y unidades
    document.getElementById('guiaTransportistaId')?.addEventListener('change', async function () {
        const condSelect = document.getElementById('guiaConductorId');
        const unidSelect = document.getElementById('guiaUnidadId');
        if (!this.value) {
            if (condSelect) condSelect.innerHTML = '<option value="">-- Ninguno --</option>';
            if (unidSelect) unidSelect.innerHTML = '<option value="">-- Ninguno --</option>';
            return;
        }
        try {
            const res = await fetch(`/IV/GuiaRemi/GetConductoresYUnidades?transportistaId=${this.value}`);
            const data = await res.json();
            fillSelect('guiaConductorId', data.conductores, null, '-- Seleccione Conductor --');
            fillSelect('guiaUnidadId', data.unidades, null, '-- Seleccione Unidad --');
        } catch (e) {
            console.error('Error al cargar conductores/unidades:', e);
        }
    });

    // Render Items
    function renderItems() {
        const tbody = document.querySelector('#tablaItems tbody');
        if (!tbody) return;
        tbody.innerHTML = '';

        let totalPeso = 0;
        document.getElementById('badgeTotalItems').textContent = itemsData.length;

        if (itemsData.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-3 text-muted">No hay ítems agregados a la guía.</td></tr>';
            document.getElementById('totalPesoBruto').textContent = '0.00';
            return;
        }

        itemsData.forEach((it, idx) => {
            totalPeso += (parseFloat(it.pesoBruto) || 0);
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><input type="text" class="form-control form-control-sm font-monospace item-codigo" value="${it.productoCodigo || ''}" data-idx="${idx}" /></td>
                <td><input type="text" class="form-control form-control-sm item-desc" value="${it.descripcion || ''}" data-idx="${idx}" required /></td>
                <td><input type="text" class="form-control form-control-sm item-um text-center" value="${it.unidadMedida || 'UND'}" data-idx="${idx}" /></td>
                <td><input type="number" step="0.01" class="form-control form-control-sm text-end item-cant" value="${it.cantidad || 1}" data-idx="${idx}" required /></td>
                <td><input type="number" step="0.01" class="form-control form-control-sm text-end item-pneto" value="${it.pesoNeto || 0}" data-idx="${idx}" /></td>
                <td><input type="number" step="0.01" class="form-control form-control-sm text-end item-pbruto" value="${it.pesoBruto || 0}" data-idx="${idx}" /></td>
                <td class="text-center">
                    <button type="button" class="btn btn-outline-danger btn-sm py-0 px-1 js-item-remove" data-idx="${idx}"><i class="bi bi-trash"></i></button>
                </td>
            `;
            tbody.appendChild(tr);
        });

        document.getElementById('totalPesoBruto').textContent = totalPeso.toFixed(2);

        // Inputs events
        tbody.querySelectorAll('.item-codigo').forEach(inp => inp.addEventListener('change', e => { itemsData[e.target.dataset.idx].productoCodigo = e.target.value; }));
        tbody.querySelectorAll('.item-desc').forEach(inp => inp.addEventListener('change', e => { itemsData[e.target.dataset.idx].descripcion = e.target.value; }));
        tbody.querySelectorAll('.item-um').forEach(inp => inp.addEventListener('change', e => { itemsData[e.target.dataset.idx].unidadMedida = e.target.value; }));
        tbody.querySelectorAll('.item-cant').forEach(inp => inp.addEventListener('input', e => { itemsData[e.target.dataset.idx].cantidad = parseFloat(e.target.value) || 0; }));
        tbody.querySelectorAll('.item-pneto').forEach(inp => inp.addEventListener('input', e => { itemsData[e.target.dataset.idx].pesoNeto = parseFloat(e.target.value) || 0; }));
        tbody.querySelectorAll('.item-pbruto').forEach(inp => inp.addEventListener('input', e => {
            itemsData[e.target.dataset.idx].pesoBruto = parseFloat(e.target.value) || 0;
            let sum = 0;
            itemsData.forEach(x => sum += (parseFloat(x.pesoBruto) || 0));
            document.getElementById('totalPesoBruto').textContent = sum.toFixed(2);
        }));

        tbody.querySelectorAll('.js-item-remove').forEach(btn => btn.addEventListener('click', e => {
            const idx = parseInt(btn.dataset.idx);
            itemsData.splice(idx, 1);
            renderItems();
        }));
    }

    document.querySelector('.js-add-item')?.addEventListener('click', function () {
        itemsData.push({
            id: 0,
            movimientoAlmacenProductoId: null,
            productoId: null,
            productoCodigo: '',
            descripcion: '',
            unidadMedida: 'UND',
            cantidad: 1,
            pesoNeto: 0,
            pesoBruto: 0
        });
        renderItems();
    });

    // Abrir modal de guía
    async function abrirModalGuia(id) {
        document.getElementById('formGuia').reset();
        document.getElementById('guiaId').value = id;
        document.getElementById('modalGuiaTitle').textContent = id > 0 ? 'Editar Guía de Remisión' : 'Nueva Guía de Remisión';
        itemsData = [];
        renderItems();

        try {
            const res = await fetch(`/IV/GuiaRemi/GetDetalle?id=${id}`);
            const json = await res.json();
            if (!json.success) {
                Swal.fire('Error', json.message || 'No se pudo cargar la guía', 'error');
                return;
            }

            const d = json.data;
            fillSelect('guiaAlmacenId', d.almacenes, d.almacenId, null);
            fillSelect('guiaSerie', d.series, d.serie, null);
            fillSelect('guiaMotivoTrasladoId', d.motivosTraslado, d.motivoTrasladoId, null);
            fillSelect('guiaTransportistaId', d.transportistas, d.transportistaId, '-- Seleccione Transportista --');
            fillSelect('guiaConductorId', d.conductores, d.transportistaConductorId, '-- Conductor --');
            fillSelect('guiaUnidadId', d.unidades, d.transportistaUnidadId, '-- Unidad / Placa --');

            fillSelect('partidaDepId', d.departamentos, d.departamentoPartidaId, '-- Departamento --');
            fillSelect('llegadaDepId', d.departamentos, d.departamentoLlegadaId, '-- Departamento --');

            document.getElementById('guiaNumero').value = d.numero || '';
            document.getElementById('guiaFecha').value = d.fecha ? d.fecha.substring(0, 10) : new Date().toISOString().substring(0, 10);
            document.getElementById('guiaFechaInicioTraslado').value = d.fechaInicioTraslado ? d.fechaInicioTraslado.substring(0, 10) : new Date().toISOString().substring(0, 10);
            document.getElementById('movimientoAlmacenId').value = d.movimientoAlmacenId || '';
            document.getElementById('guiaMovimientoTexto').value = d.movimientoAlmacenCodigo ? `Movimiento: ${d.movimientoAlmacenCodigo}` : '';
            document.getElementById('proveedorDestinoId').value = d.proveedorDestinoId || '';
            document.getElementById('guiaDestinatarioNombre').value = d.proveedorDestinoNombre || '';
            document.getElementById('guiaDestinatarioDoc').value = d.proveedorDestinoDocumento || '';
            document.getElementById('guiaCostoFlete').value = d.costoFlete || 0;
            document.getElementById('partidaDireccion').value = d.puntoPartida || '';
            document.getElementById('partidaCodSunat').value = d.codigoSunatPartida || '0000';
            document.getElementById('llegadaDireccion').value = d.puntoLlegada || '';
            document.getElementById('llegadaCodSunat').value = d.codigoSunatLlegada || '0000';
            document.getElementById('guiaObservacion').value = d.observacion || '';

            if (d.departamentoPartidaId) {
                await cargarProvincias(d.departamentoPartidaId, 'partidaProvId', 'partidaDistId', d.provinciaPartidaId, d.distritoPartidaId);
            }
            if (d.departamentoLlegadaId) {
                await cargarProvincias(d.departamentoLlegadaId, 'llegadaProvId', 'llegadaDistId', d.provinciaLlegadaId, d.distritoLlegadaId);
            }

            itemsData = d.productos || [];
            renderItems();

            if (modalGuia) modalGuia.show();
        } catch (err) {
            console.error('Error al abrir modal guía:', err);
            Swal.fire('Error', 'Ocurrió un error al cargar la guía.', 'error');
        }
    }

    // Modal Buscar Movimiento
    document.getElementById('btnBuscarMovimiento')?.addEventListener('click', async function () {
        const almId = document.getElementById('guiaAlmacenId').value;
        if (!almId) {
            Swal.fire('Atención', 'Seleccione primero el almacén origen.', 'warning');
            return;
        }
        await cargarMovimientosParaGuia(almId, '');
        if (modalBuscarMov) modalBuscarMov.show();
    });

    document.getElementById('btnEjecutarBuscarMovimiento')?.addEventListener('click', async function () {
        const almId = document.getElementById('guiaAlmacenId').value;
        const search = document.getElementById('searchMovimientoInput').value;
        await cargarMovimientosParaGuia(almId, search);
    });

    async function cargarMovimientosParaGuia(almacenId, search) {
        const tbody = document.querySelector('#tablaBuscarMovimientos tbody');
        if (!tbody) return;
        tbody.innerHTML = '<tr><td colspan="3" class="text-center py-3">Buscando movimientos...</td></tr>';
        try {
            const res = await fetch(`/IV/GuiaRemi/BuscarMovimientos?almacenId=${almacenId}&search=${encodeURIComponent(search || '')}`);
            const data = await res.json();
            tbody.innerHTML = '';
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="3" class="text-center py-3 text-muted">No se encontraron movimientos disponibles.</td></tr>';
                return;
            }
            data.forEach(m => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td class="font-monospace fw-bold">${m.codigo}</td>
                    <td><small>${m.descripcion}</small></td>
                    <td class="text-end">
                        <button type="button" class="btn btn-sm btn-outline-success js-sel-mov" data-id="${m.id}" data-desc="${m.codigo}">
                            Seleccionar
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });

            tbody.querySelectorAll('.js-sel-mov').forEach(b => {
                b.addEventListener('click', async () => {
                    const movId = parseInt(b.dataset.id);
                    document.getElementById('movimientoAlmacenId').value = movId;
                    document.getElementById('guiaMovimientoTexto').value = `Movimiento: ${b.dataset.desc}`;
                    if (modalBuscarMov) modalBuscarMov.hide();

                    // Importar productos automáticamente
                    try {
                        const resProds = await fetch(`/IV/GuiaRemi/GetProductosMovimiento?movimientoAlmacenId=${movId}`);
                        const jsonProds = await resProds.json();
                        if (jsonProds.success && jsonProds.data && jsonProds.data.length > 0) {
                            itemsData = jsonProds.data;
                            renderItems();
                            Swal.fire({
                                icon: 'info',
                                title: 'Ítems importados',
                                text: `Se cargaron ${jsonProds.data.length} ítems del movimiento seleccionado.`,
                                timer: 1500,
                                showConfirmButton: false
                            });
                        }
                    } catch (e) {
                        console.error('Error importando productos del movimiento:', e);
                    }
                });
            });
        } catch (e) {
            console.error('Error buscando movimientos:', e);
            tbody.innerHTML = '<tr><td colspan="3" class="text-center py-3 text-danger">Error al buscar.</td></tr>';
        }
    }

    document.getElementById('btnLimpiarMovimiento')?.addEventListener('click', function () {
        document.getElementById('movimientoAlmacenId').value = '';
        document.getElementById('guiaMovimientoTexto').value = '';
    });

    // Guardar Guía
    document.getElementById('formGuia')?.addEventListener('submit', async function (e) {
        e.preventDefault();

        if (itemsData.length === 0) {
            Swal.fire('Atención', 'Debe agregar al menos un ítem a la guía de remisión.', 'warning');
            return;
        }

        let totalPeso = 0;
        itemsData.forEach(x => totalPeso += (parseFloat(x.pesoBruto) || 0));

        const payload = {
            id: parseInt(document.getElementById('guiaId').value || '0'),
            almacenId: parseInt(document.getElementById('guiaAlmacenId').value),
            serie: document.getElementById('guiaSerie').value,
            fecha: document.getElementById('guiaFecha').value,
            fechaInicioTraslado: document.getElementById('guiaFechaInicioTraslado').value,
            movimientoAlmacenId: document.getElementById('movimientoAlmacenId').value ? parseInt(document.getElementById('movimientoAlmacenId').value) : null,
            motivoTrasladoId: parseInt(document.getElementById('guiaMotivoTrasladoId').value),
            proveedorDestinoId: parseInt(document.getElementById('proveedorDestinoId').value || '1'),
            transportistaId: document.getElementById('guiaTransportistaId').value ? parseInt(document.getElementById('guiaTransportistaId').value) : null,
            transportistaConductorId: document.getElementById('guiaConductorId').value ? parseInt(document.getElementById('guiaConductorId').value) : null,
            transportistaUnidadId: document.getElementById('guiaUnidadId').value ? parseInt(document.getElementById('guiaUnidadId').value) : null,
            distritoPartidaId: parseInt(document.getElementById('partidaDistId').value || '0'),
            puntoPartida: document.getElementById('partidaDireccion').value.trim(),
            codigoSunatPartida: document.getElementById('partidaCodSunat').value.trim() || '0000',
            distritoLlegadaId: parseInt(document.getElementById('llegadaDistId').value || '0'),
            puntoLlegada: document.getElementById('llegadaDireccion').value.trim(),
            codigoSunatLlegada: document.getElementById('llegadaCodSunat').value.trim() || '0000',
            costoFlete: parseFloat(document.getElementById('guiaCostoFlete').value) || 0,
            pesoBrutoTotal: totalPeso,
            observacion: document.getElementById('guiaObservacion').value.trim() || null,
            esInterno: document.getElementById('guiaEsInterno').value,
            productos: itemsData.map(it => ({
                id: it.id || 0,
                movimientoAlmacenProductoId: it.movimientoAlmacenProductoId || null,
                productoId: it.productoId || null,
                productoCodigo: it.productoCodigo || 'GENERICO',
                descripcion: it.descripcion,
                cantidad: it.cantidad,
                pesoNeto: it.pesoNeto || 0,
                pesoBruto: it.pesoBruto || 0
            }))
        };

        if (payload.distritoPartidaId <= 0 || payload.distritoLlegadaId <= 0) {
            Swal.fire('Atención', 'Complete los distritos de punto de partida y llegada.', 'warning');
            return;
        }

        try {
            const res = await fetch('/IV/GuiaRemi/Guardar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const json = await res.json();
            if (json.success) {
                Swal.fire({
                    icon: 'success',
                    title: '¡Éxito!',
                    text: json.message,
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => window.location.reload());
            } else {
                Swal.fire('Error', json.message || 'No se pudo guardar la guía.', 'error');
            }
        } catch (err) {
            console.error('Error al guardar guía:', err);
            Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
        }
    });

    // Enviar a Aprobación
    document.querySelectorAll('.js-guia-enviar-aproba').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = parseInt(this.dataset.id);
            const rev = this.dataset.rev;
            Swal.fire({
                title: '¿Enviar guía a aprobación?',
                text: 'La guía pasará a la bandeja de autorizaciones para su emisión oficial.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar',
                cancelButtonText: 'Cancelar'
            }).then(async r => {
                if (r.isConfirmed) {
                    try {
                        const res = await fetch(`/IV/GuiaRemi/SolicitarAprobacion?id=${id}&revision=${encodeURIComponent(rev)}`, {
                            method: 'POST'
                        });
                        const json = await res.json();
                        if (json.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Enviada',
                                text: json.message,
                                timer: 1200,
                                showConfirmButton: false
                            }).then(() => window.location.reload());
                        } else {
                            Swal.fire('Error', json.message, 'error');
                        }
                    } catch (e) {
                        console.error('Error solicitando aprobación:', e);
                    }
                }
            });
        });
    });

    // Anular Guía
    document.querySelectorAll('.js-guia-anular').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = parseInt(this.dataset.id);
            const rev = this.dataset.rev;
            Swal.fire({
                title: 'Anular Guía de Remisión',
                text: 'Ingrese el motivo por el cual se anula la guía:',
                input: 'text',
                inputPlaceholder: 'Motivo de anulación...',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Anular Guía',
                confirmButtonColor: '#d33',
                cancelButtonText: 'Cancelar',
                inputValidator: (val) => {
                    if (!val || val.trim().length < 5) {
                        return 'Debe ingresar un motivo de al menos 5 caracteres.';
                    }
                }
            }).then(async r => {
                if (r.isConfirmed && r.value) {
                    try {
                        const res = await fetch('/IV/GuiaRemi/Anular', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ id: id, motivoAnulacion: r.value.trim(), revision: rev })
                        });
                        const json = await res.json();
                        if (json.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Anulada',
                                text: json.message,
                                timer: 1200,
                                showConfirmButton: false
                            }).then(() => window.location.reload());
                        } else {
                            Swal.fire('Error', json.message, 'error');
                        }
                    } catch (e) {
                        console.error('Error anulando guía:', e);
                    }
                }
            });
        });
    });

    // Attach botones principales
    document.querySelector('.js-guia-new')?.addEventListener('click', () => abrirModalGuia(0));
    document.querySelectorAll('.js-guia-edit').forEach(b => b.addEventListener('click', () => abrirModalGuia(parseInt(b.dataset.id))));
});

