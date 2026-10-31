// Gestión de Comprobantes de Pago - Modo SUNAT BETA
let itemIndex = 0;

document.addEventListener('DOMContentLoaded', function () {
    // Al cargar
});

function irPagina(p) {
    const form = document.getElementById('formFiltros');
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = 'page';
    input.value = p;
    form.appendChild(input);
    form.submit();
}

function abrirModalNuevo() {
    document.getElementById('comp_id').value = '0';
    document.getElementById('comp_numero').value = '';
    document.getElementById('tbodyItems').innerHTML = '';
    itemIndex = 0;

    // Cargar series del tipo seleccionado
    cargarSeries();

    // Agregar un ítem por defecto
    agregarItemFila({
        productoId: 1,
        descripcion: 'PRODUCTO / SERVICIO DE PRUEBA',
        unidadMedida: 'NIU',
        cantidad: 1,
        precioUnitario: 100.00
    });

    const modal = new bootstrap.Modal(document.getElementById('modalComprobante'));
    modal.show();
}

async function cargarSeries() {
    const tipoDocId = document.getElementById('comp_tipoDoc').value;
    try {
        const resp = await fetch(`/VT/Comprobante/Series?tipoDocId=${tipoDocId}`);
        const res = await resp.json();
        const select = document.getElementById('comp_serie');
        select.innerHTML = '';
        if (res.success && res.data && res.data.length > 0) {
            res.data.forEach(s => {
                const opt = document.createElement('option');
                opt.value = s.codigo;
                opt.textContent = s.descripcion;
                select.appendChild(opt);
            });
            cargarSiguienteNumero();
        } else {
            // Serie por defecto según tipo
            const defSerie = tipoDocId == '1' ? 'F001' : (tipoDocId == '2' ? 'B001' : 'T001');
            select.innerHTML = `<option value="${defSerie}">${defSerie} (BETA)</option>`;
            cargarSiguienteNumero();
        }
    } catch (e) {
        console.error('Error al cargar series:', e);
    }
}

async function cargarSiguienteNumero() {
    const tipoDocId = document.getElementById('comp_tipoDoc').value;
    const serie = document.getElementById('comp_serie').value;
    if (!serie) return;

    try {
        const resp = await fetch(`/VT/Comprobante/SiguienteNumero?tipoDocId=${tipoDocId}&serie=${encodeURIComponent(serie)}`);
        const res = await resp.json();
        if (res.success) {
            document.getElementById('comp_numero').value = res.numero;
        }
    } catch (e) {
        console.error('Error al cargar número:', e);
    }
}

function agregarItemFila(item = {}) {
    itemIndex++;
    const tbody = document.getElementById('tbodyItems');
    const tr = document.createElement('tr');
    tr.id = `item_fila_${itemIndex}`;

    const prodId = item.productoId || 1;
    const desc = item.descripcion || '';
    const um = item.unidadMedida || 'NIU';
    const cant = item.cantidad || 1;
    const pu = item.precioUnitario || 0.0;
    const total = (cant * pu).toFixed(2);

    tr.innerHTML = `
        <td class="text-center small text-muted">${itemIndex}</td>
        <td>
            <input type="hidden" class="item-prod-id" value="${prodId}" />
            <input type="text" class="form-control form-control-sm item-desc" value="${desc}" placeholder="Descripción..." required />
        </td>
        <td>
            <input type="text" class="form-control form-control-sm item-um text-center" value="${um}" style="width: 70px;" />
        </td>
        <td>
            <input type="number" step="0.01" min="0.01" class="form-control form-control-sm text-end item-cant" value="${cant}" onchange="calcularTotales()" onkeyup="calcularTotales()" required />
        </td>
        <td>
            <input type="number" step="0.01" min="0.00" class="form-control form-control-sm text-end item-pu" value="${pu}" onchange="calcularTotales()" onkeyup="calcularTotales()" required />
        </td>
        <td>
            <select class="form-select form-select-sm item-afecta" onchange="calcularTotales()">
                <option value="1" selected>Gravado - 10</option>
                <option value="8">Exonerado - 20</option>
                <option value="9">Inafecto - 30</option>
                <option value="2">Gratuita - 11</option>
            </select>
        </td>
        <td class="text-end fw-bold item-total-txt">S/ ${total}</td>
        <td class="text-center">
            <button type="button" class="btn btn-sm btn-outline-danger py-0 px-1" onclick="eliminarFila(${itemIndex})">
                <i class="bi bi-trash"></i>
            </button>
        </td>
    `;
    tbody.appendChild(tr);
    calcularTotales();
}

function eliminarFila(idx) {
    const fila = document.getElementById(`item_fila_${idx}`);
    if (fila) fila.remove();
    calcularTotales();
}

function calcularTotales() {
    let subtotal = 0;
    let igv = 0;
    let total = 0;

    const filas = document.querySelectorAll('#tbodyItems tr');
    filas.forEach(f => {
        const cant = parseFloat(f.querySelector('.item-cant').value) || 0;
        const pu = parseFloat(f.querySelector('.item-pu').value) || 0;
        const afecta = f.querySelector('.item-afecta').value;

        const mTotal = cant * pu;
        let mNeto = mTotal;
        let mIgv = 0;

        if (afecta === '1') { // Gravado
            mNeto = mTotal / 1.18;
            mIgv = mTotal - mNeto;
        } else {
            mNeto = mTotal;
            mIgv = 0;
        }

        f.querySelector('.item-total-txt').textContent = `S/ ${mTotal.toFixed(2)}`;

        subtotal += mNeto;
        igv += mIgv;
        total += mTotal;
    });

    document.getElementById('lblSubtotal').textContent = `S/ ${subtotal.toFixed(2)}`;
    document.getElementById('lblIgv').textContent = `S/ ${igv.toFixed(2)}`;
    document.getElementById('lblTotal').textContent = `S/ ${total.toFixed(2)}`;
}

async function guardarComprobante(emitirSunat) {
    const filas = document.querySelectorAll('#tbodyItems tr');
    if (filas.length === 0) {
        Swal.fire('Atención', 'Debe agregar al menos un ítem al comprobante.', 'warning');
        return;
    }

    const items = [];
    filas.forEach(f => {
        const cant = parseFloat(f.querySelector('.item-cant').value) || 1;
        const pu = parseFloat(f.querySelector('.item-pu').value) || 0;
        const afecta = parseInt(f.querySelector('.item-afecta').value) || 1;
        const mTotal = cant * pu;
        const mNeto = afecta === 1 ? (mTotal / 1.18) : mTotal;
        const mIgv = afecta === 1 ? (mTotal - mNeto) : 0;

        items.push({
            productoId: parseInt(f.querySelector('.item-prod-id').value) || 1,
            descripcion: f.querySelector('.item-desc').value,
            unidadMedida: f.querySelector('.item-um').value,
            cantidad: cant,
            precioUnitario: pu,
            valorUnitario: afecta === 1 ? pu / 1.18 : pu,
            montoBruto: mTotal,
            montoNeto: mNeto,
            montoIgv: mIgv,
            montoTotal: mTotal,
            tipoAfectacionIgvId: afecta
        });
    });

    const payload = {
        comprobanteId: parseInt(document.getElementById('comp_id').value) || 0,
        tipoDocumentoComercialId: parseInt(document.getElementById('comp_tipoDoc').value),
        serie: document.getElementById('comp_serie').value,
        numero: document.getElementById('comp_numero').value,
        fecha: document.getElementById('comp_fecha').value,
        clienteId: parseInt(document.getElementById('comp_clienteId').value) || 1,
        nombreAlterno: document.getElementById('comp_clienteNombre').value,
        monedaId: parseInt(document.getElementById('comp_moneda').value) || 1,
        tipoCondicionPagoId: parseInt(document.getElementById('comp_condPago').value) || 1,
        medioPagoId: parseInt(document.getElementById('comp_medioPago').value) || 1,
        numeroGuiaRemision: document.getElementById('comp_guiaRem').value,
        items: items
    };

    try {
        Swal.fire({
            title: emitirSunat ? 'Emitiendo a SUNAT BETA...' : 'Guardando...',
            text: 'Procesando comprobante electrónico',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        const resp = await fetch('/VT/Comprobante/Guardar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const res = await resp.json();

        if (!res.success) {
            Swal.fire('Error', res.message, 'error');
            return;
        }

        const compId = res.id;

        if (emitirSunat) {
            // Emisión electrónica formal
            const emitResp = await fetch('/VT/Comprobante/Emitir', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ comprobanteId: compId, envioEnLinea: 'S' })
            });
            const emitRes = await emitResp.json();

            if (emitRes.success) {
                Swal.fire({
                    icon: 'success',
                    title: '¡Comprobante Emitido!',
                    html: `<b>SUNAT BETA:</b> ${emitRes.message}<br><small class='text-muted'>Hash UBL: ${emitRes.feResponse?.hashCode || 'Generado'}</small>`,
                    confirmButtonText: 'Aceptar'
                }).then(() => location.reload());
            } else {
                Swal.fire('Aviso', `Guardado pero error en envío: ${emitRes.message}`, 'warning').then(() => location.reload());
            }
        } else {
            Swal.fire('Éxito', res.message, 'success').then(() => location.reload());
        }
    } catch (e) {
        Swal.fire('Error', 'No se pudo comunicar con el servidor.', 'error');
    }
}

async function emitirSunat(id) {
    const confirm = await Swal.fire({
        title: '¿Emitir a SUNAT BETA?',
        text: 'Se firmará el comprobante UBL 2.1 y se generará la constancia CDR de prueba.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, emitir',
        cancelButtonText: 'Cancelar'
    });

    if (!confirm.isConfirmed) return;

    try {
        Swal.fire({ title: 'Emitiendo...', didOpen: () => Swal.showLoading() });
        const resp = await fetch('/VT/Comprobante/Emitir', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ comprobanteId: id, envioEnLinea: 'S' })
        });
        const res = await resp.json();
        if (res.success) {
            Swal.fire('Aceptado (BETA)', res.message, 'success').then(() => location.reload());
        } else {
            Swal.fire('Error', res.message, 'error');
        }
    } catch (e) {
        Swal.fire('Error', 'Fallo de conexión.', 'error');
    }
}

async function anularComprobante(id) {
    const { value: motivo } = await Swal.fire({
        title: 'Anular Comprobante',
        input: 'text',
        inputLabel: 'Motivo de la anulación:',
        inputPlaceholder: 'Ingrese motivo...',
        showCancelButton: true,
        inputValidator: (v) => !v && '¡Debe ingresar un motivo!'
    });

    if (!motivo) return;

    try {
        const resp = await fetch('/VT/Comprobante/Anular', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ comprobanteId: id, motivo: motivo })
        });
        const res = await resp.json();
        if (res.success) {
            Swal.fire('Anulado', res.message, 'success').then(() => location.reload());
        } else {
            Swal.fire('Error', res.message, 'error');
        }
    } catch (e) {
        Swal.fire('Error', 'Fallo de red.', 'error');
    }
}

async function verDetalle(id) {
    try {
        Swal.fire({ title: 'Cargando...', didOpen: () => Swal.showLoading() });
        const resp = await fetch(`/VT/Comprobante/Detalle?id=${id}`);
        const res = await resp.json();
        Swal.close();

        if (!res.success || !res.data) {
            Swal.fire('Error', 'No se pudo cargar el detalle.', 'error');
            return;
        }

        const d = res.data;
        let html = `
            <div class="border p-3 rounded mb-3 bg-light">
                <div class="row align-items-center">
                    <div class="col-8">
                        <h4 class="fw-bold mb-0 text-primary">${d.tipoDocumentoComercialNombre}</h4>
                        <div class="fs-5 fw-bold text-dark">${d.serie}-${d.numero}</div>
                        <small class="text-muted">Fecha: ${new Date(d.fecha).toLocaleDateString()}</small>
                    </div>
                    <div class="col-4 text-end">
                        <span class="badge ${d.estadoSunat === '4' ? 'bg-success' : 'bg-warning'} fs-6 p-2">
                            ${d.estadoSunat === '4' ? 'ACEPTADO SUNAT (BETA)' : d.estadoSunatNombre}
                        </span>
                    </div>
                </div>
            </div>

            <div class="row mb-3">
                <div class="col-md-7">
                    <h6 class="fw-bold text-secondary">Cliente (Receptor):</h6>
                    <div><strong>${d.clienteNombre}</strong></div>
                    <div>Doc: ${d.clienteRuc}</div>
                    <small class="text-muted">${d.clienteDireccion || 'Sin dirección registrada'}</small>
                </div>
                <div class="col-md-5">
                    <h6 class="fw-bold text-secondary">Condiciones:</h6>
                    <div>Moneda: <strong>${d.monedaSimbolo}</strong> | T.C: ${d.montoTipoCambio}</div>
                    <div>Vendedor: ${d.vendedorNombre || '-'}</div>
                    <div class="small text-truncate">Hash: <code>${d.codigoHash || 'Pendiente'}</code></div>
                </div>
            </div>

            <table class="table table-sm table-bordered">
                <thead class="table-light">
                    <tr>
                        <th>Descripción</th>
                        <th class="text-center">U.M.</th>
                        <th class="text-end">Cant.</th>
                        <th class="text-end">P. Unit</th>
                        <th class="text-end">Total</th>
                    </tr>
                </thead>
                <tbody>
        `;

        (d.items || []).forEach(i => {
            html += `
                <tr>
                    <td>${i.descripcion}</td>
                    <td class="text-center">${i.unidadMedida}</td>
                    <td class="text-end">${parseFloat(i.cantidad).toFixed(2)}</td>
                    <td class="text-end">${parseFloat(i.precioUnitario).toFixed(2)}</td>
                    <td class="text-end fw-bold">${parseFloat(i.montoTotal).toFixed(2)}</td>
                </tr>
            `;
        });

        html += `
                </tbody>
            </table>
            <div class="row justify-content-end">
                <div class="col-md-5">
                    <table class="table table-sm table-borderless text-end">
                        <tr><td>Op. Gravada:</td><td class="fw-bold">S/ ${parseFloat(d.montoNeto).toFixed(2)}</td></tr>
                        <tr><td>IGV (18%):</td><td class="fw-bold">S/ ${parseFloat(d.montoIgv).toFixed(2)}</td></tr>
                        <tr class="border-top"><td class="fs-6 fw-bold">Total:</td><td class="fs-6 fw-bold text-primary">S/ ${parseFloat(d.montoTotal).toFixed(2)}</td></tr>
                    </table>
                </div>
            </div>
        `;

        document.getElementById('contenidoVisor').innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('modalVisor'));
        modal.show();
    } catch (e) {
        Swal.fire('Error', 'Error al ver comprobante.', 'error');
    }
}

