# RH / Entrada a Planilla — 2026-10-31

Al entrar a `/RH/Planilla` se mostraba un JSON con «Seleccione un período y una sede válidos en Perfil de acceso». Planilla y Procesos de planilla conservaban una validación de contexto anterior: ante la pérdida de `USER_SESSION` devolvía 409 incluso durante la navegación normal, sin recuperar el contexto de la cookie de autenticación ni conducir al perfil.

## Corrección

`RHContextController` ahora reutiliza `ModuloContextController`, el manejo de contexto ya utilizado por los otros módulos. Conserva sus propiedades protegidas anteriores como alias y recibe `IAuthService` mediante los constructores de `PlanillaController` y `PlanillaProcesoController`. El servicio ya estaba registrado; no se cambió el registro DI, las vistas ni las operaciones de negocio.

- En GET, si falta la sesión de trabajo, se intenta recuperar exactamente la empresa, sede y período de la autenticación y se valida el contexto activo y el permiso del módulo. También se admite la cookie anterior con año, si identifica un único período autorizado.
- Si no puede recuperarse el contexto, la navegación a Index redirige a Perfil de acceso, conservando la ruta y los filtros de retorno. Las solicitudes AJAX/JSON mantienen una respuesta 409 con `redirectUrl`.
- Una sesión de trabajo seleccionada conserva prioridad sobre datos antiguos de la cookie. Las sesiones corruptas, ajenas, inválidas o de un contexto deshabilitado no habilitan el acceso.
- Los POST no recuperan silenciosamente una sesión ausente. Se conservan autorización, antiforgery, permisos y los IDs de usuario, empresa y contexto de sesión.
- Los permisos siguen siendo `rh / planilla/index` y `rh / planillaProceso/index`: la función anterior `GetPermisoModuloRHAsync` ya delegaba en la misma consulta genérica que emplea el controlador común.

## Validación

**65 comprobaciones HTTP aprobadas**, usando controladores, Razor y consultas reales con conexiones restringidas a SELECT. Se comprobaron ambos módulos, recuperación con cookie actual/anterior, sesión seleccionada frente a cookie obsoleta, mayúsculas/minúsculas de propiedades, navegación con retorno y filtros, siete casos de contexto no recuperable, respuestas AJAX/JSON, permisos en acciones de lectura/escritura, antiforgery y guardados simulados con IDs de sesión. No se ejecutaron escrituras ni procedimientos de modificación en SQL Server.

El build normal compiló el código, pero falló al copiar el ensamblado porque la aplicación del usuario estaba ejecutándose bajo el depurador y mantenía bloqueada la DLL. La compilación y las pruebas se completaron correctamente en una salida temporal separada, sin detener la aplicación. La advertencia CS8602 preexistente de `FamiliaProducto` permanece fuera de alcance.

Para cargar la corrección hay que detener y volver a iniciar la ejecución/depuración del ERP. No se modificó MG/TablaGeneral ni se alteraron los cambios acumulados de otros módulos.
