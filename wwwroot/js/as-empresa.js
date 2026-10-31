document.addEventListener('DOMContentLoaded', function () {
    let currentEmpresaData = null;
    let modalEmpresaEl = document.getElementById('modalEmpresa');
    let modalEmpresa = modalEmpresaEl ? new bootstrap.Modal(modalEmpresaEl) : null;
    let modalSedeEl = document.getElementById('modalSede');
    let modalSede = modalSedeEl ? new bootstrap.Modal(modalSedeEl) : null;

    // Lookups cache
    let cachedTiposVia = [];
    let cachedTiposZona = [];
    let cachedDepartamentos = [];

    // Helper para poblar <select>
    function fillSelect(selectId, items, selectedValue, defaultText = '-- Seleccione --') {
        const select = document.getElementById(selectId);
        if (!select) return;
        select.innerHTML = defaultText ? `<option value="">${defaultText}</option>` : '';
        (items || []).forEach(item => {
            const opt = document.createElement('option');
            opt.value = item.id;
            opt.textContent = item.descripcion || item.nombre;
            if (selectedValue !== null && selectedValue !== undefined && item.id.toString() === selectedValue.toString()) {
                opt.selected = true;
            }
            select.appendChild(opt);
        });
    }

    // Ubigeo dinámico genérico
    async function cargarProvincias(departamentoId, provinciaSelectId, distritoSelectId, selectedProvincia = null, selectedDistrito = null) {
        const provSelect = document.getElementById(provinciaSelectId);
        const distSelect = document.getElementById(distritoSelectId);
        if (!provSelect) return;

        provSelect.innerHTML = '<option value="">-- Cargando provincias... --</option>';
        if (distSelect) distSelect.innerHTML = '<option value="">-- Seleccione provincia primero --</option>';

        if (!departamentoId) {
            provSelect.innerHTML = '<option value="">-- Seleccione departamento primero --</option>';
            return;
        }

        try {
            const res = await fetch(`/AS/Empresa/GetProvincias?departamentoId=${departamentoId}`);
            const data = await res.json();
            fillSelect(provinciaSelectId, data, selectedProvincia, '-- Seleccione Provincia --');

            if (selectedProvincia && distSelect) {
                await cargarDistritos(selectedProvincia, distritoSelectId, selectedDistrito);
            }
        } catch (err) {
            console.error('Error al cargar provincias:', err);
            provSelect.innerHTML = '<option value="">-- Error al cargar --</option>';
        }
    }

    async function cargarDistritos(provinciaId, distritoSelectId, selectedDistrito = null) {
        const distSelect = document.getElementById(distritoSelectId);
        if (!distSelect) return;

        distSelect.innerHTML = '<option value="">-- Cargando distritos... --</option>';

        if (!provinciaId) {
            distSelect.innerHTML = '<option value="">-- Seleccione provincia primero --</option>';
            return;
        }

        try {
            const res = await fetch(`/AS/Empresa/GetDistritos?provinciaId=${provinciaId}`);
            const data = await res.json();
            fillSelect(distritoSelectId, data, selectedDistrito, '-- Seleccione Distrito --');
        } catch (err) {
            console.error('Error al cargar distritos:', err);
            distSelect.innerHTML = '<option value="">-- Error al cargar --</option>';
        }
    }

    // Listeners Ubigeo Empresa
    document.getElementById('empresaDepartamentoId')?.addEventListener('change', function () {
        cargarProvincias(this.value, 'empresaProvinciaId', 'empresaDistritoId');
    });

    document.getElementById('empresaProvinciaId')?.addEventListener('change', function () {
        cargarDistritos(this.value, 'empresaDistritoId');
    });

    // Listeners Ubigeo Sede
    document.getElementById('sedeDepartamentoId')?.addEventListener('change', function () {
        cargarProvincias(this.value, 'sedeProvinciaId', 'sedeDistritoId');
    });

    document.getElementById('sedeProvinciaId')?.addEventListener('change', function () {
        cargarDistritos(this.value, 'sedeDistritoId');
    });

    // Renderizar Sedes en la pestaña
    function renderSedes(sedes) {
        const tbody = document.querySelector('#tablaSedes tbody');
        if (!tbody) return;
        tbody.innerHTML = '';

        const badge = document.getElementById('badgeTotalSedes');
        if (badge) badge.textContent = sedes ? sedes.length : 0;

        // También poblar el combo de asignación en Tab 3
        const sedeAsignar = document.getElementById('sedeAsignarSelect');
        if (sedeAsignar) {
            sedeAsignar.innerHTML = '<option value="">-- Seleccione sede a asignar --</option>';
            (sedes || []).forEach(s => {
                const opt = document.createElement('option');
                opt.value = s.id;
                opt.textContent = `${s.codigo} - ${s.nombre}`;
                sedeAsignar.appendChild(opt);
            });
        }

        if (!sedes || sedes.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-3 text-muted">No hay sedes registradas para esta empresa.</td></tr>';
            return;
        }

        sedes.forEach(s => {
            const isActivo = s.estado === 'A';
            const tr = document.createElement('tr');
            if (!isActivo) tr.className = 'table-secondary text-muted';

            tr.innerHTML = `
                <td class="fw-bold font-monospace">${s.codigo}</td>
                <td class="fw-semibold">${s.nombre}</td>
                <td class="font-monospace text-secondary">${s.codigoSunat || '-'}</td>
                <td class="text-center">
                    ${s.esCentral === 'S' ? '<span class="badge bg-primary">Central</span>' : '<span class="text-muted small">-</span>'}
                </td>
                <td><small class="text-secondary">${s.direccion || '-'}</small></td>
                <td><small>${s.telefono || '-'}</small></td>
                <td>
                    <span class="badge ${isActivo ? 'bg-success-subtle text-success border border-success-subtle' : 'bg-secondary text-white'}">
                        ${s.estadoTexto}
                    </span>
                </td>
                <td class="text-end">
                    <div class="btn-group btn-group-sm">
                        <button type="button" class="btn btn-outline-primary js-sede-edit-btn" data-id="${s.id}" title="Editar sede">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button type="button" class="btn ${isActivo ? 'btn-outline-danger' : 'btn-outline-success'} js-sede-toggle-btn"
                                data-id="${s.id}" data-estado="${isActivo ? 'I' : 'A'}"
                                title="${isActivo ? 'Desactivar' : 'Activar'}">
                            <i class="bi ${isActivo ? 'bi-slash-circle' : 'bi-check-circle'}"></i>
                        </button>
                    </div>
                </td>
            `;
            tbody.appendChild(tr);
        });

        // Attach event handlers para botones de sedes
        tbody.querySelectorAll('.js-sede-edit-btn').forEach(b => {
            b.addEventListener('click', () => abrirModalSede(parseInt(b.dataset.id)));
        });

        tbody.querySelectorAll('.js-sede-toggle-btn').forEach(b => {
            b.addEventListener('click', () => toggleSede(parseInt(b.dataset.id), b.dataset.estado));
        });
    }

    // Renderizar Sedes por Período
    async function cargarPeriodoSedes(empresaId, periodoId) {
        const tbody = document.querySelector('#tablaPeriodoSedes tbody');
        if (!tbody) return;
        tbody.innerHTML = '<tr><td colspan="5" class="text-center py-2 text-muted">Cargando sedes por período...</td></tr>';

        try {
            const res = await fetch(`/AS/Empresa/GetPeriodoSedes?empresaId=${empresaId}&periodoId=${periodoId}`);
            const json = await res.json();
            const items = json.data || [];

            tbody.innerHTML = '';
            if (items.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center py-3 text-muted">No hay sedes asignadas a este período.</td></tr>';
                return;
            }

            items.forEach(item => {
                const isActivo = item.estado === 'A';
                const tr = document.createElement('tr');
                if (!isActivo) tr.className = 'table-secondary text-muted';

                tr.innerHTML = `
                    <td class="fw-bold">${item.periodo}</td>
                    <td class="font-monospace">${item.sedeCodigo}</td>
                    <td class="fw-semibold">${item.sedeNombre}</td>
                    <td>
                        <span class="badge ${isActivo ? 'bg-success-subtle text-success border border-success-subtle' : 'bg-secondary text-white'}">
                            ${item.estadoTexto}
                        </span>
                    </td>
                    <td class="text-end">
                        <button type="button" class="btn btn-outline-danger btn-sm js-periodo-sede-delete" data-id="${item.id}" title="Quitar asignación">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });

            tbody.querySelectorAll('.js-periodo-sede-delete').forEach(b => {
                b.addEventListener('click', () => eliminarPeriodoSede(parseInt(b.dataset.id)));
            });
        } catch (err) {
            console.error('Error al cargar sedes por período:', err);
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-2 text-danger">Error al cargar datos.</td></tr>';
        }
    }

    document.getElementById('periodoSelect')?.addEventListener('change', function () {
        const empresaId = parseInt(document.getElementById('empresaId').value || '0');
        if (empresaId > 0 && this.value) {
            cargarPeriodoSedes(empresaId, parseInt(this.value));
        }
    });

    // Abrir Modal de Empresa
    async function abrirModalEmpresa(id) {
        document.getElementById('formEmpresa').reset();
        document.getElementById('empresaId').value = id;
        document.getElementById('modalEmpresaTitle').textContent = id > 0 ? 'Editar Empresa' : 'Nueva Empresa';

        // Reset tabs a la primera
        const tabBtn = document.querySelector('#tab-general-btn');
        if (tabBtn) bootstrap.Tab.getOrCreateInstance(tabBtn).show();

        // Si es nueva empresa, deshabilitar pestañas de sedes hasta que se guarde la cabecera
        const tabSedesBtn = document.getElementById('tab-sedes-btn');
        const tabPeriodosBtn = document.getElementById('tab-periodos-btn');
        if (id === 0) {
            if (tabSedesBtn) tabSedesBtn.classList.add('disabled');
            if (tabPeriodosBtn) tabPeriodosBtn.classList.add('disabled');
        } else {
            if (tabSedesBtn) tabSedesBtn.classList.remove('disabled');
            if (tabPeriodosBtn) tabPeriodosBtn.classList.remove('disabled');
        }

        try {
            const res = await fetch(`/AS/Empresa/GetDetalle?id=${id}`);
            const json = await res.json();
            if (!json.success) {
                Swal.fire('Error', json.message || 'No se pudo cargar la empresa', 'error');
                return;
            }

            const data = json.data;
            currentEmpresaData = data;

            // Guardar cachés
            cachedTiposVia = data.tiposVia;
            cachedTiposZona = data.tiposZona;
            cachedDepartamentos = data.departamentos;

            // Poblar Lookups Empresa
            fillSelect('empresaSectorId', data.sectores, data.sectorId);
            fillSelect('empresaTipoViaId', data.tiposVia, data.tipoViaId);
            fillSelect('empresaTipoZonaId', data.tiposZona, data.tipoZonaId);
            fillSelect('empresaDepartamentoId', data.departamentos, data.departamentoId, '-- Seleccione Departamento --');
            fillSelect('empresaBancoId', data.bancos, data.bancoId, '-- Ninguno --');
            fillSelect('periodoSelect', data.periodos, data.periodos.length > 0 ? data.periodos[0].id : null, null);

            // Valores en formulario
            document.getElementById('empresaRuc').value = data.ruc || '';
            document.getElementById('empresaNombre').value = data.nombre || '';
            document.getElementById('empresaNombreComercial').value = data.nombreComercial || '';
            document.getElementById('empresaNombreVia').value = data.nombreVia || '';
            document.getElementById('empresaNumeroVia').value = data.numeroVia || '';
            document.getElementById('empresaInterior').value = data.interior || '';
            document.getElementById('empresaDpto').value = data.dpto || '';
            document.getElementById('empresaManzana').value = data.manzana || '';
            document.getElementById('empresaLote').value = data.lote || '';
            document.getElementById('empresaKilometro').value = data.kilometro || '';
            document.getElementById('empresaBlock').value = data.block || '';
            document.getElementById('empresaEtapa').value = data.etapa || '';
            document.getElementById('empresaNombreZona').value = data.nombreZona || '';
            document.getElementById('empresaNumCtaDetrac').value = data.numCtaDetrac || '';

            // Cargar cascada de provincias y distritos
            if (data.departamentoId) {
                fillSelect('empresaProvinciaId', data.provincias, data.provinciaId, '-- Seleccione Provincia --');
                if (data.provinciaId) {
                    fillSelect('empresaDistritoId', data.distritos, data.distritoId, '-- Seleccione Distrito --');
                }
            }

            // Renderizar Sedes
            renderSedes(data.sedes);

            // Cargar Sedes por Período
            if (id > 0 && data.periodos && data.periodos.length > 0) {
                cargarPeriodoSedes(id, data.periodos[0].id);
            }

            if (modalEmpresa) modalEmpresa.show();
        } catch (err) {
            console.error('Error al abrir modal empresa:', err);
            Swal.fire('Error', 'Ocurrió un error al cargar la información.', 'error');
        }
    }

    // Submit Empresa
    document.getElementById('formEmpresa')?.addEventListener('submit', async function (e) {
        e.preventDefault();

        const id = parseInt(document.getElementById('empresaId').value || '0');
        const payload = {
            id: id,
            sectorId: parseInt(document.getElementById('empresaSectorId').value),
            ruc: document.getElementById('empresaRuc').value.trim(),
            nombre: document.getElementById('empresaNombre').value.trim(),
            nombreComercial: document.getElementById('empresaNombreComercial').value.trim() || null,
            tipoViaId: parseInt(document.getElementById('empresaTipoViaId').value),
            nombreVia: document.getElementById('empresaNombreVia').value.trim() || null,
            numeroVia: document.getElementById('empresaNumeroVia').value.trim() || null,
            interior: document.getElementById('empresaInterior').value.trim() || null,
            dpto: document.getElementById('empresaDpto').value.trim() || null,
            manzana: document.getElementById('empresaManzana').value.trim() || null,
            lote: document.getElementById('empresaLote').value.trim() || null,
            kilometro: document.getElementById('empresaKilometro').value.trim() || null,
            block: document.getElementById('empresaBlock').value.trim() || null,
            etapa: document.getElementById('empresaEtapa').value.trim() || null,
            tipoZonaId: parseInt(document.getElementById('empresaTipoZonaId').value),
            nombreZona: document.getElementById('empresaNombreZona').value.trim() || null,
            distritoId: parseInt(document.getElementById('empresaDistritoId').value || '0'),
            bancoId: document.getElementById('empresaBancoId').value ? parseInt(document.getElementById('empresaBancoId').value) : null,
            numCtaDetrac: document.getElementById('empresaNumCtaDetrac').value.trim() || null,
            estado: 'A'
        };

        if (!payload.distritoId || payload.distritoId <= 0) {
            Swal.fire('Atención', 'Debe seleccionar un distrito válido.', 'warning');
            return;
        }

        try {
            const res = await fetch('/AS/Empresa/Guardar', {
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
                }).then(() => {
                    window.location.reload();
                });
            } else {
                Swal.fire('Error', json.message || 'No se pudo guardar la empresa.', 'error');
            }
        } catch (err) {
            console.error('Error al guardar empresa:', err);
            Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
        }
    });

    // Activar / Desactivar Empresa
    function toggleEmpresa(id, nuevoEstado) {
        const accion = nuevoEstado === 'A' ? 'activar' : 'desactivar';
        Swal.fire({
            title: `¿Está seguro de ${accion} esta empresa?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: `Sí, ${accion}`,
            cancelButtonText: 'Cancelar'
        }).then(async result => {
            if (result.isConfirmed) {
                try {
                    const res = await fetch(`/AS/Empresa/CambiarEstado?id=${id}&nuevoEstado=${nuevoEstado}`, {
                        method: 'POST'
                    });
                    const json = await res.json();
                    if (json.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Completado',
                            text: json.message,
                            timer: 1200,
                            showConfirmButton: false
                        }).then(() => window.location.reload());
                    } else {
                        Swal.fire('Error', json.message, 'error');
                    }
                } catch (err) {
                    console.error('Error al cambiar estado empresa:', err);
                    Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                }
            }
        });
    }

    // Modal Sede
    async function abrirModalSede(sedeId) {
        const form = document.getElementById('formSede');
        if (!form) return;
        form.reset();

        const empresaId = parseInt(document.getElementById('empresaId').value || '0');
        document.getElementById('sedeId').value = sedeId;
        document.getElementById('sedeEmpresaId').value = empresaId;
        document.getElementById('modalSedeTitle').textContent = sedeId > 0 ? 'Editar Sede' : 'Nueva Sede';

        // Poblar lookups para la sede
        fillSelect('sedeTipoViaId', cachedTiposVia, 1);
        fillSelect('sedeTipoZonaId', cachedTiposZona, 1);
        fillSelect('sedeDepartamentoId', cachedDepartamentos, 15, '-- Seleccione Departamento --');

        if (sedeId > 0) {
            const sede = (currentEmpresaData?.sedes || []).find(s => s.id === sedeId);
            if (sede) {
                document.getElementById('sedeNombre').value = sede.nombre || '';
                document.getElementById('sedeCodigoSunat').value = sede.codigoSunat || '';
                document.getElementById('sedeEsCentral').checked = sede.esCentral === 'S';
                document.getElementById('sedeTipoViaId').value = sede.tipoViaId || 1;
                document.getElementById('sedeNombreVia').value = sede.nombreVia || '';
                document.getElementById('sedeNumeroVia').value = sede.numeroVia || '';
                document.getElementById('sedeInterior').value = sede.interior || '';
                document.getElementById('sedeDpto').value = sede.dpto || '';
                document.getElementById('sedeManzana').value = sede.manzana || '';
                document.getElementById('sedeLote').value = sede.lote || '';
                document.getElementById('sedeKilometro').value = sede.kilometro || '';
                document.getElementById('sedeBlock').value = sede.block || '';
                document.getElementById('sedeEtapa').value = sede.etapa || '';
                document.getElementById('sedeTipoZonaId').value = sede.tipoZonaId || 1;
                document.getElementById('sedeNombreZona').value = sede.nombreZona || '';
                document.getElementById('sedeTelefono').value = sede.telefono || '';
                document.getElementById('sedeTelefono2').value = sede.telefono2 || '';
                document.getElementById('sedeEmail').value = sede.email || '';

                if (sede.departamentoId) {
                    document.getElementById('sedeDepartamentoId').value = sede.departamentoId;
                    await cargarProvincias(sede.departamentoId, 'sedeProvinciaId', 'sedeDistritoId', sede.provinciaId, sede.distritoId);
                }
            }
        } else {
            // Default ubigeo Lima
            await cargarProvincias(15, 'sedeProvinciaId', 'sedeDistritoId', 128, 1251);
        }

        if (modalSede) modalSede.show();
    }

    // Submit Sede
    document.getElementById('formSede')?.addEventListener('submit', async function (e) {
        e.preventDefault();

        const empresaId = parseInt(document.getElementById('sedeEmpresaId').value || '0');
        const sedeId = parseInt(document.getElementById('sedeId').value || '0');
        const payload = {
            id: sedeId,
            empresaId: empresaId,
            nombre: document.getElementById('sedeNombre').value.trim(),
            codigoSunat: document.getElementById('sedeCodigoSunat').value.trim(),
            esCentral: document.getElementById('sedeEsCentral').checked ? 'S' : 'N',
            tipoViaId: parseInt(document.getElementById('sedeTipoViaId').value),
            nombreVia: document.getElementById('sedeNombreVia').value.trim() || null,
            numeroVia: document.getElementById('sedeNumeroVia').value.trim() || null,
            interior: document.getElementById('sedeInterior').value.trim() || null,
            dpto: document.getElementById('sedeDpto').value.trim() || null,
            manzana: document.getElementById('sedeManzana').value.trim() || null,
            lote: document.getElementById('sedeLote').value.trim() || null,
            kilometro: document.getElementById('sedeKilometro').value.trim() || null,
            block: document.getElementById('sedeBlock').value.trim() || null,
            etapa: document.getElementById('sedeEtapa').value.trim() || null,
            tipoZonaId: parseInt(document.getElementById('sedeTipoZonaId').value),
            nombreZona: document.getElementById('sedeNombreZona').value.trim() || null,
            distritoId: parseInt(document.getElementById('sedeDistritoId').value || '0'),
            telefono: document.getElementById('sedeTelefono').value.trim() || null,
            telefono2: document.getElementById('sedeTelefono2').value.trim() || null,
            email: document.getElementById('sedeEmail').value.trim() || null,
            estado: 'A'
        };

        if (!payload.distritoId || payload.distritoId <= 0) {
            Swal.fire('Atención', 'Debe seleccionar un distrito válido para la sede.', 'warning');
            return;
        }

        try {
            const res = await fetch('/AS/Empresa/GuardarSede', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const json = await res.json();
            if (json.success) {
                if (modalSede) modalSede.hide();
                Swal.fire({
                    icon: 'success',
                    title: 'Sede guardada',
                    text: json.message,
                    timer: 1200,
                    showConfirmButton: false
                });

                // Recargar sedes
                const resSedes = await fetch(`/AS/Empresa/GetSedes?empresaId=${empresaId}`);
                const jsonSedes = await resSedes.json();
                if (jsonSedes.success) {
                    if (currentEmpresaData) currentEmpresaData.sedes = jsonSedes.data;
                    renderSedes(jsonSedes.data);
                }
            } else {
                Swal.fire('Error', json.message || 'No se pudo guardar la sede.', 'error');
            }
        } catch (err) {
            console.error('Error al guardar sede:', err);
            Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
        }
    });

    // Toggle Sede
    function toggleSede(id, nuevoEstado) {
        const accion = nuevoEstado === 'A' ? 'activar' : 'desactivar';
        Swal.fire({
            title: `¿Está seguro de ${accion} esta sede?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: `Sí, ${accion}`,
            cancelButtonText: 'Cancelar'
        }).then(async result => {
            if (result.isConfirmed) {
                try {
                    const res = await fetch(`/AS/Empresa/CambiarEstadoSede?id=${id}&nuevoEstado=${nuevoEstado}`, {
                        method: 'POST'
                    });
                    const json = await res.json();
                    if (json.success) {
                        const empresaId = parseInt(document.getElementById('empresaId').value || '0');
                        const resSedes = await fetch(`/AS/Empresa/GetSedes?empresaId=${empresaId}`);
                        const jsonSedes = await resSedes.json();
                        if (jsonSedes.success) {
                            if (currentEmpresaData) currentEmpresaData.sedes = jsonSedes.data;
                            renderSedes(jsonSedes.data);
                        }
                    } else {
                        Swal.fire('Error', json.message, 'error');
                    }
                } catch (err) {
                    console.error('Error al cambiar estado sede:', err);
                    Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                }
            }
        });
    }

    // Asignar Sede a Periodo
    document.querySelector('.js-periodo-sede-add')?.addEventListener('click', async function () {
        const empresaId = parseInt(document.getElementById('empresaId').value || '0');
        const periodoId = parseInt(document.getElementById('periodoSelect').value || '0');
        const sedeId = parseInt(document.getElementById('sedeAsignarSelect').value || '0');

        if (!periodoId || !sedeId) {
            Swal.fire('Atención', 'Seleccione un período y una sede para asignar.', 'warning');
            return;
        }

        try {
            const res = await fetch('/AS/Empresa/AsignarPeriodoSede', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ PeriodoId: periodoId, EmpresaSedeId: sedeId })
            });
            const json = await res.json();
            if (json.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Asignada',
                    text: json.message,
                    timer: 1200,
                    showConfirmButton: false
                });
                cargarPeriodoSedes(empresaId, periodoId);
            } else {
                Swal.fire('Error', json.message || 'No se pudo asignar la sede al período.', 'error');
            }
        } catch (err) {
            console.error('Error al asignar sede a período:', err);
            Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
        }
    });

    // Eliminar Asignación Sede - Periodo
    function eliminarPeriodoSede(id) {
        Swal.fire({
            title: '¿Desea retirar esta sede del período?',
            text: 'La sede dejará de estar activa para operaciones en dicho año.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, retirar',
            cancelButtonText: 'Cancelar'
        }).then(async result => {
            if (result.isConfirmed) {
                try {
                    const res = await fetch(`/AS/Empresa/EliminarPeriodoSede?id=${id}`, {
                        method: 'POST'
                    });
                    const json = await res.json();
                    if (json.success) {
                        const empresaId = parseInt(document.getElementById('empresaId').value || '0');
                        const periodoId = parseInt(document.getElementById('periodoSelect').value || '0');
                        cargarPeriodoSedes(empresaId, periodoId);
                    } else {
                        Swal.fire('Error', json.message, 'error');
                    }
                } catch (err) {
                    console.error('Error al eliminar asignación de sede:', err);
                    Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                }
            }
        });
    }

    // Attach botones principales
    document.querySelector('.js-empresa-new')?.addEventListener('click', () => abrirModalEmpresa(0));

    document.querySelectorAll('.js-empresa-edit').forEach(btn => {
        btn.addEventListener('click', () => abrirModalEmpresa(parseInt(btn.dataset.id)));
    });

    document.querySelectorAll('.js-empresa-toggle').forEach(btn => {
        btn.addEventListener('click', () => toggleEmpresa(parseInt(btn.dataset.id), btn.dataset.estado));
    });

    document.querySelector('.js-sede-new')?.addEventListener('click', () => abrirModalSede(0));
});

