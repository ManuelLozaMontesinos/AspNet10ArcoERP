// ========================================================
// SISTEMA DE NOTIFICACIONES Y SONIDO (CAMPANITA)
// ========================================================
var _notificationAudioCtx = null;
var _isSoundEnabled = localStorage.getItem('arco_notification_sound_enabled') !== 'false';
var _prevNotificationTotal = -1;
var _hasInitialLoaded = false;
var _knownNotificationIds = {};

function getNotificationAudioContext() {
    if (!_notificationAudioCtx) {
        var AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (AudioContextClass) {
            _notificationAudioCtx = new AudioContextClass();
        }
    }
    return _notificationAudioCtx;
}

// Desbloquear AudioContext en la primera interacción del usuario según políticas del navegador
function unlockAudioContext() {
    var ctx = getNotificationAudioContext();
    if (ctx && ctx.state === 'suspended') {
        ctx.resume().catch(function () { });
    }
}
document.addEventListener('click', unlockAudioContext, { once: true, passive: true });
document.addEventListener('keydown', unlockAudioContext, { once: true, passive: true });
document.addEventListener('touchstart', unlockAudioContext, { once: true, passive: true });

function tocarCampanita(ctx) {
    var now = ctx.currentTime;

    // 1. Nota Principal (Campana brillante cristalina ~1320 Hz - Mi / E6)
    var osc1 = ctx.createOscillator();
    var gain1 = ctx.createGain();
    osc1.type = 'sine';
    osc1.frequency.setValueAtTime(1320, now);
    osc1.frequency.exponentialRampToValueAtTime(1318, now + 0.6);
    gain1.gain.setValueAtTime(0, now);
    gain1.gain.linearRampToValueAtTime(0.32, now + 0.015);
    gain1.gain.exponentialRampToValueAtTime(0.0001, now + 0.7);
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.start(now);
    osc1.stop(now + 0.75);

    // 2. Armónico Superior / Eco metálico (~2093 Hz - Do / C7), retrasado 75ms
    var osc2 = ctx.createOscillator();
    var gain2 = ctx.createGain();
    osc2.type = 'sine';
    osc2.frequency.setValueAtTime(2093, now + 0.075);
    gain2.gain.setValueAtTime(0, now);
    gain2.gain.setValueAtTime(0, now + 0.075);
    gain2.gain.linearRampToValueAtTime(0.25, now + 0.09);
    gain2.gain.exponentialRampToValueAtTime(0.0001, now + 1.1);
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.start(now + 0.075);
    osc2.stop(now + 1.15);

    // 3. Resonancia base de calidez (~880 Hz - La / A5)
    var osc3 = ctx.createOscillator();
    var gain3 = ctx.createGain();
    osc3.type = 'triangle';
    osc3.frequency.setValueAtTime(880, now);
    gain3.gain.setValueAtTime(0, now);
    gain3.gain.linearRampToValueAtTime(0.12, now + 0.02);
    gain3.gain.exponentialRampToValueAtTime(0.0001, now + 0.5);
    osc3.connect(gain3);
    gain3.connect(ctx.destination);
    osc3.start(now);
    osc3.stop(now + 0.55);
}

function reproducirCampanitaNotificacion(force) {
    if (!force && !_isSoundEnabled) return;
    try {
        var ctx = getNotificationAudioContext();
        if (!ctx) return;
        if (ctx.state === 'suspended') {
            ctx.resume().then(function () {
                tocarCampanita(ctx);
            }).catch(function () { });
        } else {
            tocarCampanita(ctx);
        }
    } catch (e) {
        console.warn('Error al reproducir campanita de notificación:', e);
    }
}

// Expuesto globalmente para pruebas y llamadas desde consola u otros módulos
window.reproducirSonidoNotificacion = reproducirCampanitaNotificacion;
window.probarCampanita = function () {
    reproducirCampanitaNotificacion(true);
    animarCampanitaNavbar();
};

function animarCampanitaNavbar() {
    var $icon = $('#app-notification-trigger').find('i.fa-bell, i.bi-bell');
    if ($icon.length) {
        $icon.removeClass('app-notification-bell-ring');
        setTimeout(function () {
            $icon.addClass('app-notification-bell-ring');
        }, 20);
        setTimeout(function () {
            $icon.removeClass('app-notification-bell-ring');
        }, 1200);
    }
}

function actualizarBotonSonidoUI() {
    var $btn = $('#app-notification-sound-toggle');
    if (!$btn.length) return;
    if (_isSoundEnabled) {
        $btn.removeClass('muted');
        $btn.find('i').removeClass('fa-volume-mute bi-volume-mute').addClass('fas fa-volume-up');
        $btn.attr('title', 'Sonido de notificaciones: Activado (clic para silenciar)');
    } else {
        $btn.addClass('muted');
        $btn.find('i').removeClass('fa-volume-up bi-volume-up').addClass('fas fa-volume-mute');
        $btn.attr('title', 'Sonido de notificaciones: Silenciado (clic para activar)');
    }
}

$(document).on('click', '#app-notification-sound-toggle', function (e) {
    e.preventDefault();
    e.stopPropagation();
    _isSoundEnabled = !_isSoundEnabled;
    localStorage.setItem('arco_notification_sound_enabled', _isSoundEnabled ? 'true' : 'false');
    actualizarBotonSonidoUI();
    if (_isSoundEnabled) {
        reproducirCampanitaNotificacion(true);
    }
});

$(function () {
    actualizarBotonSonidoUI();
});

// ========================================================
// CARGA Y GESTIÓN DE NOTIFICACIONES
// ========================================================
function listarNotificaciones() {
    $.ajax({
        url: '/ListarNotificaciones',
        type: 'GET',
        cache: false,
        success: function (response) {
            if (response && response.success) {
                buildNotificationDropdown(response);
            }
        },
        error: function(err) {
            console.warn('Error al obtener notificaciones:', err);
        }
    });
}

function buildNotificationDropdown(data) {
    var dropdown = $('#app-notification-dropdown');
    var notifications = data && Array.isArray(data.data) ? data.data : (data && Array.isArray(data.list) ? data.list : []);
    var total = data && Number(data.total) !== undefined ? Number(data.total) : notifications.length;

    // Detectar si llegaron notificaciones nuevas (después de la carga inicial)
    var hayNuevas = false;
    if (_hasInitialLoaded) {
        if (total > _prevNotificationTotal) {
            hayNuevas = true;
        } else {
            for (var i = 0; i < notifications.length; i++) {
                var nId = notifications[i].id || notifications[i].notificacionPushId;
                if (nId && !_knownNotificationIds[nId]) {
                    hayNuevas = true;
                    break;
                }
            }
        }
    }

    _knownNotificationIds = {};
    notifications.forEach(function (item) {
        var id = item.id || item.notificacionPushId;
        if (id) _knownNotificationIds[id] = true;
    });

    _prevNotificationTotal = total;

    if (!_hasInitialLoaded) {
        _hasInitialLoaded = true;
    } else if (hayNuevas) {
        reproducirCampanitaNotificacion();
        animarCampanitaNavbar();
    }

    dropdown.empty();
    $('#app-notification-icon').text(total > 0 ? (total > 99 ? '99+' : total) : '').toggle(total > 0);
    $('#app-notification-total').text(total);
    $('#app-notification-clear-all').prop('disabled', notifications.length === 0);
    $('#app-notification-empty').prop('hidden', notifications.length > 0);

    notifications.forEach(function (item) {
        var id = item.id || item.notificacionPushId;
        var title = item.titulo || 'Notificación';
        var card = $('<article>').addClass('app-notification-card').attr('role', 'listitem');
        var icon = $('<span>').addClass('app-notification-card-icon').append(
            $('<i>').addClass('far fa-bell').attr('aria-hidden', 'true')
        );
        var content = $('<div>').addClass('app-notification-card-content');
        var actions = $('<div>').addClass('app-notification-card-actions');
        var deleteButton = $('<button>')
            .attr('type', 'button')
            .attr('aria-label', 'Eliminar notificación')
            .attr('title', 'Eliminar notificación')
            .attr('data-notification-id', id)
            .addClass('app-notification-delete')
            .append($('<i>').addClass('fas fa-times').attr('aria-hidden', 'true'));

        $('<h3>').addClass('app-notification-card-title').text(title).appendTo(content);
        $('<p>').addClass('app-notification-card-description').text(item.descripcion || '').appendTo(content);
        $('<time>').addClass('app-notification-card-time').text(item.fechaFormateada || item.fecha || '').appendTo(content);
        actions.append(deleteButton);
        card.append(icon, content, actions);
        dropdown.append(card);
    });
}

function confirmarVaciarNotificaciones() {
    var darkMode = document.documentElement.getAttribute('data-bs-theme') === 'dark' || document.body.classList.contains('dark-mode');
    var dialog = window.Swal
        ? Swal.fire({
            title: '¿Vaciar la bandeja?',
            text: 'Se eliminarán las notificaciones de hoy.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, vaciar',
            cancelButtonText: 'Conservar',
            reverseButtons: true,
            background: darkMode ? '#202833' : '#ffffff',
            color: darkMode ? '#edf2f7' : '#1e293b',
            confirmButtonColor: '#dc3545',
            cancelButtonColor: darkMode ? '#556273' : '#64748b'
        })
        : Promise.resolve({ isConfirmed: window.confirm('¿Eliminar las notificaciones de hoy?') });

    dialog.then(function (result) {
        if (!result.isConfirmed) return;

        var clearButton = $('#app-notification-clear-all');
        clearButton.prop('disabled', true);
        $.ajax({
            url: '/VaciarNotificaciones',
            type: 'POST',
            success: function (response) {
                if (response.success) {
                    listarNotificaciones();
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Bandeja vaciada',
                            timer: 1500,
                            showConfirmButton: false
                        });
                    }
                } else {
                    clearButton.prop('disabled', false);
                    listarNotificaciones();
                }
            },
            error: function () {
                clearButton.prop('disabled', false);
                listarNotificaciones();
            }
        });
    });
}

$(document)
    .off('click.appNotifications', '.app-notification-delete')
    .on('click.appNotifications', '.app-notification-delete', function (event) {
        event.preventDefault();
        event.stopPropagation();

        var button = $(this);
        var notificationId = Number(button.attr('data-notification-id'));
        if (!notificationId || button.prop('disabled')) return;

        button.prop('disabled', true);
        $.ajax({
            url: '/EliminarNotificacion',
            type: 'POST',
            data: { id: notificationId },
            success: function (response) {
                if (response.success) {
                    listarNotificaciones();
                } else {
                    button.prop('disabled', false);
                }
            },
            error: function () {
                button.prop('disabled', false);
            }
        });
    });

$('#app-notification-clear-all')
    .off('click.appNotifications')
    .on('click.appNotifications', function (event) {
        event.preventDefault();
        event.stopPropagation();
        if (!this.disabled) confirmarVaciarNotificaciones();
    });

// ========================================================
// CONEXIÓN SIGNALR CON REINTENTO AUTOMÁTICO
// ========================================================
if (window.signalR) {
    try {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/notifyHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        connection.on("refreshNotifications", function (userId, data) {
            try {
                var parsed = typeof data === 'string' ? JSON.parse(data) : data;
                buildNotificationDropdown(parsed);
            } catch (e) {
                listarNotificaciones();
            }
        });

        connection.on("refreshNotificationList", function () {
            listarNotificaciones();
        });

        connection.on("calendarReminder", function (customMsg) {
            var msg = customMsg || 'Tienes un evento próximo que inicia en 15 minutos o menos. Revisa tus notificaciones.';
            reproducirCampanitaNotificacion();
            animarCampanitaNavbar();
            listarNotificaciones();

            if (window.Swal) {
                Swal.fire({
                    icon: 'info',
                    title: '¡Recordatorio de Evento!',
                    text: msg,
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 8000,
                    timerProgressBar: true
                });
            }
        });

        connection.onreconnected(function () {
            listarNotificaciones();
        });

        connection.start().then(function () {
            listarNotificaciones();
        }).catch(function (err) {
            console.warn("SignalR Hub connection failed, falling back to polling:", err);
            listarNotificaciones();
        });
    } catch (e) {
        console.warn("Error initializing SignalR:", e);
        listarNotificaciones();
    }
} else {
    // Si no está disponible SignalR, cargar vía polling
    listarNotificaciones();
}

// Polling suave de respaldo cada 30 segundos
setInterval(function () {
    listarNotificaciones();
}, 30000);

