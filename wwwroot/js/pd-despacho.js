document.addEventListener('DOMContentLoaded', function () {
    const modalElement = document.getElementById('modalDespacho');
    const modal = modalElement ? new bootstrap.Modal(modalElement) : null;
    let currentComprobanteId = 0;

    document.querySelectorAll('.btn-detalle').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            cargarDetalleDespacho(id);
        });
    });

    async function cargarDetalleDespacho(id) {
        currentComprobanteId = id;
        try {
            const res = await fetch(`/PD/Despacho/Obtener?id=${id}`);
            const json = await res.json();
            if (!json.success) {
                alert(json.message || 'No se pudo cargar el detalle.');
                return;
            }

            const cab = json.data.cabecera;
            const items = json.data.items;

            document.getElementById('modalDocTitle').textContent = `${cab.tipoDocumento} ${cab.serieNumero}`;
            document.getElementById('modalCliente').textContent = cab.razonSocial;
            document.getElementById('modalDocIden').textContent = cab.numeroDocumento;
            document.getElementById('modalCotiza').textContent = cab.cotizacionPedido || '—';

            const badge = document.getElementById('modalEstadoBadge');
            badge.textContent = cab.estadoDespachoNombre;
            badge.className = 'badge ' + (cab.estadoDespachoId === 3 ? 'bg-success text-white' : cab.estadoDespachoId === 2 ? 'bg-warning text-dark' : 'bg-secondary text-white');

            const tbody = document.querySelector('#tblItemsDespacho tbody');
            tbody.innerHTML = '';

            items.forEach(it => {
                const tr = document.createElement('tr');
                const pendiente = it.cantidadPendiente;
                const esCompleto = pendiente <= 0;

                tr.innerHTML = `
                    <td class="fw-bold">${it.codigo}</td>
                    <td>${it.descripcion}</td>
                    <td class="text-center">${it.unidadMedida}</td>
                    <td class="text-end">${it.cantidad.toFixed(2)}</td>
                    <td class="text-end text-success fw-bold">${it.cantidadEntregada.toFixed(2)}</td>
                    <td class="text-end text-danger fw-bold">${pendiente.toFixed(2)}</td>
                    <td class="text-center">
                        ${esCompleto ? '<span class="badge bg-success">Completado</span>' : `
                            <div class="input-group input-group-sm">
                                <input type="number" class="form-control form-control-sm text-end input-cant" min="0.01" max="${pendiente}" step="any" value="${pendiente}" />
                                <button type="button" class="btn btn-primary btn-sm btn-entregar-item" data-item-id="${it.comprobanteProductoId}">
                                    <i class="bi bi-check-lg"></i>
                                </button>
                            </div>
                        `}
                    </td>
                `;
                tbody.appendChild(tr);
            });

            // Bind click for each deliver button
            tbody.querySelectorAll('.btn-entregar-item').forEach(b => {
                b.addEventListener('click', async function () {
                    const itemId = this.getAttribute('data-item-id');
                    const input = this.closest('tr').querySelector('.input-cant');
                    const cant = parseFloat(input.value);
                    if (!cant || cant <= 0) {
                        alert('Ingrese una cantidad válida.');
                        return;
                    }

                    if (!confirm(`¿Confirma la entrega de ${cant} unidades?`)) return;

                    const resp = await fetch('/PD/Despacho/EntregarItem', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            comprobanteId: currentComprobanteId,
                            comprobanteProductoId: itemId,
                            cantidad: cant
                        })
                    });
                    const rJson = await resp.json();
                    if (rJson.success) {
                        cargarDetalleDespacho(currentComprobanteId);
                    } else {
                        alert(rJson.message || 'Error al procesar la entrega.');
                    }
                });
            });

            if (modal) modal.show();
        } catch (e) {
            console.error(e);
            alert('Error de conexión.');
        }
    }

    const btnCerrarTodo = document.getElementById('btnCerrarTodo');
    if (btnCerrarTodo) {
        btnCerrarTodo.addEventListener('click', async function () {
            if (!confirm('¿Confirma cerrar todo el despacho y dar por entregados todos los productos?')) return;
            try {
                const resp = await fetch('/PD/Despacho/Cerrar', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        comprobanteId: currentComprobanteId,
                        observacion: 'Cierre total de despacho'
                    })
                });
                const rJson = await resp.json();
                if (rJson.success) {
                    alert('Despacho cerrado exitosamente.');
                    location.reload();
                } else {
                    alert(rJson.message || 'Error al cerrar el despacho.');
                }
            } catch (e) {
                console.error(e);
                alert('Error al cerrar despacho.');
            }
        });
    }
});

