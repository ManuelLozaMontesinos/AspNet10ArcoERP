    // @ts-nocheck
    // Variables de configuración de Lookups
    const pageConfig = window.erpListings.config('proveedor-page-config');
    var tiposDocData = pageConfig.TiposDoc;

    function onNaturalezaChange() {
        var esNatural = $('#nat-natural').is(':checked');
        
        if (esNatural) {
            $('#bloque-juridico').addClass('d-none');
            $('#bloque-natural').removeClass('d-none');
            $('#prov-razon-social').prop('required', false);
            $('#prov-nombres').prop('required', true);
            $('#prov-ape-pat').prop('required', true);
            
            // Filtrar Tipos de Documento para Persona Natural (DNI, CE, etc.)
            filtrarTiposDoc(1);
        } else {
            $('#bloque-juridico').removeClass('d-none');
            $('#bloque-natural').addClass('d-none');
            $('#prov-razon-social').prop('required', true);
            $('#prov-nombres').prop('required', false);
            $('#prov-ape-pat').prop('required', false);
            
            // Filtrar Tipos de Documento para Persona Jurídica (Solo RUC)
            filtrarTiposDoc(2);
        }
        actualizarEstadoBotonGuardar();
    }

    function filtrarTiposDoc(naturalezaId, docSeleccionado) {
        var $select = $('#prov-tipo-doc');
        $select.empty();

        tiposDocData.forEach(function (td) {
            var esRuc = (td.Codigo === '06' || td.Abreviatura === 'RUC');
            if (naturalezaId === 2 && esRuc) {
                $select.append(new Option(td.Nombre + ' (' + td.Abreviatura + ')', td.Id));
            } else if (naturalezaId === 1 && !esRuc) {
                $select.append(new Option(td.Nombre + ' (' + td.Abreviatura + ')', td.Id));
            }
        });

        if (docSeleccionado) {
            $select.val(docSeleccionado);
        } else {
            $select.val($select.find('option:first').val());
        }
        onTipoDocChange();
    }

    function onTipoDocChange() {
        var $selected = $('#prov-tipo-doc option:selected');
        var cod = $selected.text();
        if (cod.indexOf('RUC') !== -1) {
            $('#prov-num-doc').attr('maxlength', 11).attr('placeholder', 'RUC de 11 dígitos');
        } else if (cod.indexOf('DNI') !== -1) {
            $('#prov-num-doc').attr('maxlength', 8).attr('placeholder', 'DNI de 8 dígitos');
        } else {
            $('#prov-num-doc').attr('maxlength', 20).attr('placeholder', 'N° Documento');
        }
    }

    function calcularRazonSocialNatural() {
        var nom = $('#prov-nombres').val().trim();
        var pat = $('#prov-ape-pat').val().trim();
        var mat = $('#prov-ape-mat').val().trim();
        var res = (pat + ' ' + (mat ? mat + ' ' : '') + nom).trim();
        $('#prov-razon-social-preview').val(res);
        $('#prov-razon-social').val(res);
        actualizarEstadoBotonGuardar();
    }

    function onDepartamentoChange(provinciaSeleccionada, distritoSeleccionado, onComplete) {
        var deptoId = $('#prov-departamento').val();
        var $prov = $('#prov-provincia');
        var $dis = $('#prov-distrito');

        $prov.empty().append(new Option('-- Cargando Provincias... --', '')).prop('disabled', true);
        $dis.empty().append(new Option('-- Seleccione Distrito --', '')).prop('disabled', true);

        if (!deptoId) {
            $prov.empty().append(new Option('-- Seleccione Provincia --', ''));
            actualizarEstadoBotonGuardar();
            if (typeof onComplete === 'function') onComplete();
            return;
        }

        $.ajax({
            url: pageConfig.Urls.ListarProvincias,
            type: 'GET',
            data: { departamentoId: deptoId },
            success: function (res) {
                $prov.empty().append(new Option('-- Seleccione Provincia --', ''));
                if (res && res.success && res.data) {
                    res.data.forEach(function (p) {
                        $prov.append(new Option(p.nombre || p.Nombre, p.id || p.Id));
                    });
                    $prov.prop('disabled', false);

                    if (provinciaSeleccionada) {
                        $prov.val(provinciaSeleccionada);
                        onProvinciaChange(distritoSeleccionado, onComplete);
                    } else {
                        actualizarEstadoBotonGuardar();
                        if (typeof onComplete === 'function') onComplete();
                    }
                } else {
                    actualizarEstadoBotonGuardar();
                    if (typeof onComplete === 'function') onComplete();
                }
            },
            error: function () {
                actualizarEstadoBotonGuardar();
                if (typeof onComplete === 'function') onComplete();
            }
        });
    }

    function onProvinciaChange(distritoSeleccionado, onComplete) {
        var provId = $('#prov-provincia').val();
        var $dis = $('#prov-distrito');

        $dis.empty().append(new Option('-- Cargando Distritos... --', '')).prop('disabled', true);

        if (!provId) {
            $dis.empty().append(new Option('-- Seleccione Distrito --', ''));
            actualizarEstadoBotonGuardar();
            if (typeof onComplete === 'function') onComplete();
            return;
        }

        $.ajax({
            url: pageConfig.Urls.ListarDistritos,
            type: 'GET',
            data: { provinciaId: provId },
            success: function (res) {
                $dis.empty().append(new Option('-- Seleccione Distrito --', ''));
                if (res && res.success && res.data) {
                    res.data.forEach(function (d) {
                        $dis.append(new Option(d.nombre || d.Nombre, d.id || d.Id));
                    });
                    $dis.prop('disabled', false);

                    if (distritoSeleccionado) {
                        $dis.val(distritoSeleccionado);
                    }
                }
                actualizarEstadoBotonGuardar();
                if (typeof onComplete === 'function') onComplete();
            },
            error: function () {
                actualizarEstadoBotonGuardar();
                if (typeof onComplete === 'function') onComplete();
            }
        });
    }

    function consultarDocumento(esClickManual) {
        var doc = $('#prov-num-doc').val().trim();
        if (!doc) {
            if (esClickManual) Swal.fire({ icon: 'warning', title: 'Atención', text: 'Ingrese un número de documento para consultar.' });
            return;
        }

        var $btn = $('#btn-consultar-doc');
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm"></span>');
        $('#doc-status-msg').html('<span class="text-primary"><i class="bi bi-hourglass-split me-1"></i>Consultando...</span>');

        $.ajax({
            url: pageConfig.Urls.ConsultarDocumento,
            type: 'GET',
            data: { doc: doc },
            success: function (res) {
                $btn.prop('disabled', false).html('<i class="bi bi-search"></i>');

                if (res && res.success) {
                    if (res.source === 'BD') {
                        $('#doc-status-msg').html('<span class="text-success"><i class="bi bi-check-circle-fill me-1"></i>Registrado en BD local.</span>');
                        Swal.fire({
                            icon: 'info',
                            title: 'Registro Existente',
                            text: 'El documento ya está registrado como "' + res.data.nombreCompleto + '". ¿Desea cargar sus datos para edición?',
                            showCancelButton: true,
                            confirmButtonText: 'Sí, cargar datos',
                            cancelButtonText: 'No'
                        }).then(function (result) {
                            if (result.isConfirmed) {
                                poblarFormulario(res.data);
                            }
                        });
                    } else if (res.source === 'FORMATO') {
                        $('#doc-status-msg').html('<span class="text-info"><i class="bi bi-lightbulb me-1"></i>' + res.message + '</span>');
                        if (res.tipoNaturalezaJuridicaId === 1) {
                            $('#nat-natural').prop('checked', true);
                            onNaturalezaChange();
                            $('#prov-tipo-doc').val(res.tipoDocumentoIdentidadId || 1);
                        } else {
                            $('#nat-juridica').prop('checked', true);
                            onNaturalezaChange();
                            $('#prov-tipo-doc').val(3);
                            $('#prov-ruc').val(res.numeroRuc);
                        }
                    }
                } else {
                    $('#doc-status-msg').html('<span class="text-muted"><i class="bi bi-info-circle me-1"></i>' + (res.message || 'Sin coincidencias') + '</span>');
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('<i class="bi bi-search"></i>');
                $('#doc-status-msg').html('<span class="text-danger"><i class="bi bi-exclamation-triangle me-1"></i>Error de consulta.</span>');
            }
        });
    }

    function consultarDocumentoAutomatico() {
        var doc = $('#prov-num-doc').val().trim();
        var id = parseInt($('#prov-id').val()) || 0;
        if (id === 0 && (doc.length === 8 || doc.length === 11)) {
            consultarDocumento(false);
        }
    }

    function abrirModalCrear() {
        $('#modal-titulo').text('Nuevo Registro');
        $('#prov-id').val(0);
        $('#prov-codigo').val('');
        $('#prov-num-doc').val('');
        $('#prov-ruc').val('');
        $('#prov-razon-social').val('');
        $('#prov-abrev').val('');
        $('#prov-nombres').val('');
        $('#prov-ape-pat').val('');
        $('#prov-ape-mat').val('');
        $('#prov-razon-social-preview').val('');
        $('#doc-status-msg').empty();
        
        // Vía y Dirección
        $('#prov-tipo-via').val(12); // '-'
        $('#prov-nombre-via').val('');
        $('#prov-numero-via').val('');
        $('#prov-interior').val('');
        $('#prov-dpto').val('');
        $('#prov-manzana').val('');
        $('#prov-lote').val('');
        $('#prov-kilometro').val('');
        $('#prov-block').val('');
        $('#prov-etapa').val('');
        $('#prov-tipo-zona').val(12); // '-'
        $('#prov-nombre-zona').val('');
        $('#prov-referencia').val('');

        // Contacto
        $('#prov-telefono').val('');
        $('#prov-email').val('');
        $('#prov-web').val('');
        $('#prov-contacto').val('');
        $('#prov-cond-pago').val(1); // Contado
        $('#prov-tipo-profesion').val('');
        $('#prov-tipo-venta').val('');
        $('#prov-es-prospecto').val(pageConfig.EsVistaProspectos);
        $('#prov-lead-source').val('');
        $('.js-prospect-opportunity').prop('checked', false);
        $('#prov-cci').val('');
        $('#prov-estado').val('A');

        // Rol según filtro activo o Cliente por defecto
        var rolActivo = pageConfig.Tipo;
        $('#prov-tipo-prove').val(rolActivo ? parseInt(rolActivo) : 1);

        // Naturaleza: Jurídica por defecto
        $('#nat-juridica').prop('checked', true);
        onNaturalezaChange();

        // Ubigeo: Lima / Lima / Lima Cercado por defecto
        $('#prov-departamento').val(15);
        onDepartamentoChange(128, 1281);

        // Estado inicial vacío para modo creación y refresco de botón
        formInitialState = "";
        actualizarEstadoBotonGuardar();

        // Mostrar pestaña 1
        bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-principales-btn')).show();
        
        var modal = new bootstrap.Modal(document.getElementById('modal-crud-proveedor'));
        modal.show();
        setTimeout(function () { $('#prov-num-doc').focus(); }, 350);
    }

    function poblarFormulario(data, onFinished) {
        $('#prov-id').val(data.id || data.Id);
        $('#prov-es-prospecto').val(data.esProspecto || data.EsProspecto || pageConfig.EsVistaProspectos);
        $('#prov-codigo').val(data.codigo || data.Codigo || '');
        $('#prov-tipo-prove').val(data.tipoProveedorId || data.TipoProveedorId || 1);

        var esNat = (data.tipoNaturalezaJuridicaId || data.TipoNaturalezaJuridicaId) === 1;
        if (esNat) {
            $('#nat-natural').prop('checked', true);
        } else {
            $('#nat-juridica').prop('checked', true);
        }
        onNaturalezaChange();

        var numDoc = data.numeroDocumentoIdentidad || data.NumeroDocumentoIdentidad || data.numeroRuc || data.NumeroRuc || data.documento || data.Documento || '';
        $('#prov-num-doc').val(numDoc);
        $('#prov-ruc').val(data.numeroRuc || data.NumeroRuc || '');
        
        var tipoDocId = data.tipoDocumentoIdentidadId || data.TipoDocumentoIdentidadId;
        filtrarTiposDoc(esNat ? 1 : 2, tipoDocId);

        if (esNat) {
            $('#prov-nombres').val(data.nombres || data.Nombres || '');
            $('#prov-ape-pat').val(data.apellidoPat || data.ApellidoPat || '');
            $('#prov-ape-mat').val(data.apellidoMat || data.ApellidoMat || '');
            calcularRazonSocialNatural();
        } else {
            var rz = (data.razonSocial || data.RazonSocial || '').trim();
            $('#prov-razon-social').val(rz);
            var ab = (data.abreviatura || data.Abreviatura || data.abrev || data.Abrev || '').trim();
            $('#prov-abrev').val(ab);
            $('#prov-nombres').val('');
            $('#prov-ape-pat').val('');
            $('#prov-ape-mat').val('');
            $('#prov-razon-social-preview').val(rz);
        }

        // Domicilio y Vía
        $('#prov-tipo-via').val(data.tipoViaId || data.TipoViaId || 12);
        $('#prov-nombre-via').val(data.nombreVia || data.NombreVia || data.direccion || data.Direccion || '');
        $('#prov-numero-via').val(data.numeroVia || data.NumeroVia || '');
        $('#prov-interior').val(data.interior || data.Interior || '');
        $('#prov-dpto').val(data.dpto || data.Dpto || '');
        $('#prov-manzana').val(data.manzana || data.Manzana || '');
        $('#prov-lote').val(data.lote || data.Lote || '');
        $('#prov-kilometro').val(data.kilometro || data.Kilometro || '');
        $('#prov-block').val(data.block || data.Block || '');
        $('#prov-etapa').val(data.etapa || data.Etapa || '');
        $('#prov-tipo-zona').val(data.tipoZonaId || data.TipoZonaId || 12);
        $('#prov-nombre-zona').val(data.nombreZona || data.NombreZona || '');
        $('#prov-referencia').val(data.referencia || data.Referencia || '');

        // Ubigeo
        var depId = data.departamentoId || data.DepartamentoId || 15;
        var provId = data.provinciaId || data.ProvinciaId || 128;
        var disId = data.distritoId || data.DistritoId || 1281;
        $('#prov-departamento').val(depId);
        onDepartamentoChange(provId, disId, onFinished);

        // Contacto
        $('#prov-telefono').val(data.telefono || data.Telefono || '');
        $('#prov-email').val(data.email || data.Email || '');
        $('#prov-web').val(data.web || data.Web || '');
        $('#prov-contacto').val(data.contacto || data.Contacto || '');
        $('#prov-cond-pago').val(data.condicionPagoId || data.CondicionPagoId || 1);
        $('#prov-tipo-profesion').val(data.tipoProfesionId || data.TipoProfesionId || '');
        $('#prov-tipo-venta').val(data.tipoVentaClienteId || data.TipoVentaClienteId || '');
        $('#prov-lead-source').val(data.leadSourceId || data.LeadSourceId || '');
        $('#prov-cci').val(data.numeroCCI || data.NumeroCCI || '');
        $('#prov-estado').val(data.estado || data.Estado || 'A');
    }

    function abrirModalEditar(id) {
        $.ajax({
            url: pageConfig.Urls.Obtener,
            type: 'GET',
            data: { id: id },
            success: function (res) {
                if (res && res.success && res.data) {
                    $('#modal-titulo').text('Editar ' + (res.data.tipoProveedorNombre || 'Cliente / Proveedor') + ' #' + id);
                    formInitialState = "";
                    poblarFormulario(res.data, function () {
                        formInitialState = $('#form-proveedor').serialize();
                        actualizarEstadoBotonGuardar();
                    });
                    if (pageConfig.EsVistaProspectos === 'S') {
                        cargarOportunidadesProspecto(id);
                    }
                    
                    bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-principales-btn')).show();
                    var modal = new bootstrap.Modal(document.getElementById('modal-crud-proveedor'));
                    modal.show();

                    // Fallback para registrar estado inicial exacto
                    setTimeout(function () {
                        if (!formInitialState) {
                            formInitialState = $('#form-proveedor').serialize();
                        }
                        actualizarEstadoBotonGuardar();
                    }, 500);
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: res.message || 'No se pudo cargar el registro.' });
                }
            },
            error: function () {
                Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
            }
        });
    }

    function guardarProveedor(e) {
        e.preventDefault();

        // Validaciones
        var esNat = $('#nat-natural').is(':checked');
        var doc = $('#prov-num-doc').val().trim();
        var depto = $('#prov-departamento').val();
        var prov = $('#prov-provincia').val();
        var dis = $('#prov-distrito').val();
        var nombreVia = $('#prov-nombre-via').val().trim();

        if (!doc) {
            bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-principales-btn')).show();
            Swal.fire({ icon: 'warning', title: 'Campo Requerido', text: 'Por favor complete el número de documento.' });
            $('#prov-num-doc').focus();
            return;
        }

        if (esNat) {
            var nom = $('#prov-nombres').val().trim();
            var pat = $('#prov-ape-pat').val().trim();
            if (!nom || !pat) {
                bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-principales-btn')).show();
                Swal.fire({ icon: 'warning', title: 'Campos Requeridos', text: 'Para personas naturales es obligatorio ingresar Nombres y Apellido Paterno.' });
                $('#prov-nombres').focus();
                return;
            }
        } else {
            var rs = $('#prov-razon-social').val().trim();
            if (!rs) {
                bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-principales-btn')).show();
                Swal.fire({ icon: 'warning', title: 'Campo Requerido', text: 'Para personas jurídicas es obligatoria la Razón Social.' });
                $('#prov-razon-social').focus();
                return;
            }
            $('#prov-ruc').val(doc);
        }

        if (!depto || !prov || !dis) {
            bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-domicilio-btn')).show();
            Swal.fire({ icon: 'warning', title: 'Ubigeo Incompleto', text: 'Por favor seleccione el Departamento, Provincia y Distrito.' });
            return;
        }

        if (!nombreVia) {
            bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-domicilio-btn')).show();
            Swal.fire({ icon: 'warning', title: 'Dirección Requerida', text: 'Por favor ingrese el Nombre de Vía / Dirección.' });
            $('#prov-nombre-via').focus();
            return;
        }

        var $btn = $('#btn-guardar-prov');
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Guardando...');

        var formData = $('#form-proveedor').serialize();
        $.ajax({
            url: pageConfig.Urls.Guardar,
            type: 'POST',
            data: formData,
            success: function (res) {
                if (res && res.success) {
                    if (pageConfig.EsVistaProspectos === 'S') {
                        $.ajax({
                            url: pageConfig.Urls.GuardarOportunidades,
                            type: 'POST',
                            data: $('#form-proveedor').serialize() + '&prospectoId=' + encodeURIComponent(res.id),
                            success: function (opportunityResult) {
                                if (opportunityResult && opportunityResult.success) {
                                    finalizarGuardadoProveedor($btn, res);
                                } else {
                                    $btn.prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Guardar Registro');
                                    Swal.fire({ icon: 'error', title: 'Prospecto guardado; no se actualizaron sus oportunidades', text: opportunityResult.message || 'Vuelve a guardar para reintentar.' });
                                }
                            },
                            error: function (xhr) {
                                $btn.prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Guardar Registro');
                                var message = xhr.responseJSON && xhr.responseJSON.message
                                    ? xhr.responseJSON.message
                                    : 'No se pudo guardar la relación de oportunidades. Vuelve a guardar para reintentar.';
                                Swal.fire({ icon: 'error', title: 'Prospecto guardado; no se actualizaron sus oportunidades', text: message });
                            }
                        });
                    } else {
                        finalizarGuardadoProveedor($btn, res);
                    }
                } else {
                    $btn.prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Guardar Registro');
                    Swal.fire({ icon: 'error', title: 'Error al Guardar', text: res.message || 'No se pudo guardar el registro.' });
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Guardar Registro');
                Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
            }
        });
    }

    function finalizarGuardadoProveedor($btn, res) {
        $btn.prop('disabled', false).html('<i class="bi bi-check-circle me-1"></i>Guardar Registro');
        bootstrap.Modal.getInstance(document.getElementById('modal-crud-proveedor')).hide();
        Swal.fire({
            icon: 'success',
            title: '¡Operación Exitosa!',
            text: res.message || 'Registro guardado correctamente.',
            timer: 1600,
            showConfirmButton: false
        }).then(function () {
            location.reload();
        });
    }

    function cargarOportunidadesProspecto(prospectoId) {
        $('.js-prospect-opportunity').prop('checked', false);
        $.ajax({
            url: pageConfig.Urls.ObtenerOportunidades,
            type: 'GET',
            data: { prospectoId: prospectoId },
            success: function (res) {
                if (!res || !res.success || !Array.isArray(res.data)) {
                    Swal.fire({ icon: 'error', title: 'No se pudieron cargar las oportunidades', text: res && res.message ? res.message : 'Respuesta inválida del servidor.' });
                    return;
                }
                res.data.forEach(function (id) {
                    $('#prov-oportunidades input[value="' + id + '"]').prop('checked', true);
                });
            },
            error: function (xhr) {
                var message = xhr.responseJSON && xhr.responseJSON.message
                    ? xhr.responseJSON.message
                    : 'No se pudieron cargar las oportunidades del prospecto.';
                Swal.fire({ icon: 'error', title: 'Error al cargar oportunidades', text: message });
            }
        });
    }

    function cambiarEstado(id, estadoActual, nombre) {
        var accion = estadoActual === 'A' ? 'desactivar' : 'activar';
        Swal.fire({
            title: '¿Confirmar cambio de estado?',
            text: '¿Desea ' + accion + ' el registro "' + nombre + '"?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, ' + accion,
            cancelButtonText: 'Cancelar',
            confirmButtonColor: estadoActual === 'A' ? '#dc3545' : '#198754'
        }).then(function (result) {
            if (result.isConfirmed) {
                $.ajax({
                    url: pageConfig.Urls.CambiarEstado,
                    type: 'POST',
                    data: { id: id },
                    success: function (res) {
                        if (res && res.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Actualizado',
                                text: res.message,
                                timer: 1200,
                                showConfirmButton: false
                            }).then(function () {
                                location.reload();
                            });
                        } else {
                            Swal.fire({ icon: 'error', title: 'Error', text: res.message || 'No se pudo cambiar el estado.' });
                        }
                    },
                    error: function () {
                        Swal.fire({ icon: 'error', title: 'Error', text: 'Error de comunicación con el servidor.' });
                    }
                });
            }
        });
    }

    // =========================================================================
    // VALIDACIÓN DINÁMICA DE BOTÓN GUARDAR (NUEVO vs EDICIÓN)
    // =========================================================================
    var formInitialState = "";

    function sonCamposRequeridosValidos() {
        var esNat = $('#nat-natural').is(':checked');
        var doc = ($('#prov-num-doc').val() || '').trim();
        if (!doc) return false;

        if (esNat) {
            var nom = ($('#prov-nombres').val() || '').trim();
            var pat = ($('#prov-ape-pat').val() || '').trim();
            if (!nom || !pat) return false;
        } else {
            var rs = ($('#prov-razon-social').val() || '').trim();
            if (!rs) return false;
        }

        var depto = $('#prov-departamento').val();
        var prov = $('#prov-provincia').val();
        var dis = $('#prov-distrito').val();
        if (!depto || !prov || !dis) return false;

        var nombreVia = ($('#prov-nombre-via').val() || '').trim();
        if (!nombreVia) return false;

        return true;
    }

    function actualizarEstadoBotonGuardar() {
        var id = parseInt($('#prov-id').val()) || 0;
        var valido = sonCamposRequeridosValidos();
        var $btn = $('#btn-guardar-prov');

        if (id === 0) {
            // Nuevo Registro: Se habilita solo cuando se completan todos los campos obligatorios
            $btn.prop('disabled', !valido);
        } else {
            // Edición: Se habilita solo cuando sea válido Y haya cambiado algún campo
            var haCambiado = (formInitialState !== "" && $('#form-proveedor').serialize() !== formInitialState);
            $btn.prop('disabled', !(valido && haCambiado));
        }
    }

    $(document).on('input change keyup', '#form-proveedor input, #form-proveedor select, #form-proveedor textarea', function () {
        actualizarEstadoBotonGuardar();
    });

    $(document).on('click', '[data-action="crear-proveedor"]', abrirModalCrear);
    $(document).on('click', '[data-action="editar-proveedor"]', function () {
        abrirModalEditar(parseInt($(this).attr('data-id'), 10));
    });
    $(document).on('click', '[data-action="cambiar-estado-proveedor"]', function () {
        var $button = $(this);
        cambiarEstado(
            parseInt($button.attr('data-id'), 10),
            $button.attr('data-estado'),
            $button.attr('data-nombre')
        );
    });

    $('#filtro-form select').on('change', function () {
        var form = document.getElementById('filtro-form');
        if (form instanceof HTMLFormElement) {
            form.requestSubmit();
        }
    });
    $('#form-proveedor').on('submit', guardarProveedor);
    $('#nat-juridica, #nat-natural').on('change', onNaturalezaChange);
    $('#prov-tipo-doc').on('change', onTipoDocChange);
    $('#prov-num-doc').on('blur', consultarDocumentoAutomatico);
    $('#btn-consultar-doc').on('click', function () {
        consultarDocumento(true);
    });
    $('#prov-nombres, #prov-ape-pat, #prov-ape-mat').on('input', calcularRazonSocialNatural);
    $('#prov-departamento').on('change', function () {
        onDepartamentoChange();
    });
    $('#prov-provincia').on('change', function () {
        onProvinciaChange();
    });