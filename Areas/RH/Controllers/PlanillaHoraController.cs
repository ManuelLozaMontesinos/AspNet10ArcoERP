using System.Security.Claims;
using System.Globalization;
using AspNet10ArcoERP.Models;
using AspNet10ArcoERP.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNet10ArcoERP.Areas.RH.Controllers
{
    [Area("RH")]
    [Authorize]
    public class PlanillaHoraController : Controller
    {
        private readonly IRHService _rhService;

        public PlanillaHoraController(IRHService rhService)
        {
            _rhService = rhService;
        }

        private int CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idStr, out int id) ? id : 1;
            }
        }

        private int CurrentEmpresaId
        {
            get
            {
                var empStr = User.FindFirst("EmpresaId")?.Value;
                return int.TryParse(empStr, out int id) ? id : 1;
            }
        }

        public async Task<IActionResult> Index(int? anio = null, int? mes = null, string? search = null, int top = 25)
        {
            int pageSize = top is 25 or 50 or 100 or 500 ? top : 25;
            ViewBag.Anio = anio;
            ViewBag.Mes = mes;
            ViewBag.Search = search;
            ViewBag.Top = pageSize;

            var procesos = await _rhService.GetPlanillasProcesoListAsync(
                CurrentEmpresaId,
                anio,
                mes,
                search,
                pageSize);
            return View(procesos);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerHoras(int id)
        {
            var detalle = await _rhService.GetPlanillaHorasAsync(id, CurrentEmpresaId);
            if (detalle == null)
                return NotFound(new { success = false, message = "Proceso no encontrado para la empresa activa." });

            return Json(new { success = true, data = detalle });
        }

        [HttpGet]
        public async Task<IActionResult> PersonalDisponible(int id, string? search = null)
        {
            var detalle = await _rhService.GetPlanillaHorasAsync(id, CurrentEmpresaId);
            if (detalle == null)
                return NotFound(new { success = false, message = "Proceso no encontrado para la empresa activa." });
            if (detalle.Proceso.EstadoPlanProceId == 2)
                return BadRequest(new { success = false, message = "El proceso está cerrado y no admite cambios." });

            var candidatos = await _rhService.GetPersonalDisponiblePlanillaHorasAsync(id, CurrentEmpresaId, search);
            foreach (var empleado in candidatos)
            {
                empleado.Cantidades = detalle.Horas.Select(hora => new PlanillaHoraCantidadModel
                {
                    HoraRemId = hora.Id,
                    Cantidad = 0
                }).ToList();
            }
            return Json(new { success = true, data = candidatos });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromBody] PlanillaHorasGuardarModel? model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "No se recibieron los datos de horas." });

            var result = await _rhService.SavePlanillaHorasAsync(model, CurrentEmpresaId, CurrentUserId);
            return Json(new { success = result.success, message = result.message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarDesdeMarcaciones(int id)
        {
            var result = await _rhService.ImportarHorasDesdeMarcacionesAsync(id, CurrentEmpresaId, CurrentUserId);
            return Json(new { success = result.success, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarFormato(int id)
        {
            var detalle = await _rhService.GetPlanillaHorasAsync(id, CurrentEmpresaId);
            if (detalle == null)
                return NotFound(new { success = false, message = "Proceso no encontrado para la empresa activa." });

            var duplicateCode = detalle.Horas
                .GroupBy(hora => hora.Codigo, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateCode != null)
                return Conflict(new { success = false, message = $"El código de concepto '{duplicateCode.Key}' está duplicado y no puede generarse en una plantilla." });

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");
            worksheet.Cell(1, 1).Value = "Codigo";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Column(1).Style.NumberFormat.Format = "@";

            for (var index = 0; index < detalle.Horas.Count; index++)
            {
                var cell = worksheet.Cell(1, index + 2);
                cell.Value = detalle.Horas[index].Codigo;
                cell.Style.Font.Bold = true;
                worksheet.Column(index + 2).Style.NumberFormat.Format = "0.00";
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"FormatoHora_{id}.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Importar(int id, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Seleccione un archivo Excel válido." });
            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "El formato admitido es .xlsx." });

            var detalle = await _rhService.GetPlanillaHorasAsync(id, CurrentEmpresaId);
            if (detalle == null)
                return NotFound(new { success = false, message = "Proceso no encontrado para la empresa activa." });
            if (detalle.Proceso.EstadoPlanProceId == 2)
                return BadRequest(new { success = false, message = "El proceso está cerrado y no admite cambios." });

            var hourByCode = detalle.Horas
                .GroupBy(hora => hora.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var employeeGroups = detalle.Empleados
                .GroupBy(empleado => empleado.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var duplicateEmployeeCode = employeeGroups.FirstOrDefault(group => group.Count() > 1);
            if (duplicateEmployeeCode != null)
                return Conflict(new { success = false, message = $"El código de personal '{duplicateEmployeeCode.Key}' está duplicado en el proceso." });
            var employeeByCode = employeeGroups.ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

            try
            {
                using var workbook = new XLWorkbook(file.OpenReadStream());
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.LastRowUsed() == null || worksheet.LastColumnUsed() == null)
                    return BadRequest(new { success = false, message = "El archivo no contiene datos en la primera hoja." });

                var lastRow = worksheet.LastRowUsed()!.RowNumber();
                var lastColumn = worksheet.LastColumnUsed()!.ColumnNumber();
                if (!string.Equals(worksheet.Cell(1, 1).GetString().Trim(), "Codigo", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "La primera columna debe llamarse Codigo." });

                var columnMap = new List<(int Column, int HoraRemId)>();
                for (var column = 2; column <= lastColumn; column++)
                {
                    var code = worksheet.Cell(1, column).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(code) ||
                        !hourByCode.TryGetValue(code, out var matchingHours) ||
                        matchingHours.Count != 1)
                        return BadRequest(new { success = false, message = $"El código de concepto de la columna {column} no corresponde a un concepto único del proceso." });
                    if (columnMap.Any(item => item.HoraRemId == matchingHours[0].Id))
                        return BadRequest(new { success = false, message = $"El concepto '{code}' aparece más de una vez en la plantilla." });

                    columnMap.Add((column, matchingHours[0].Id));
                }

                if (columnMap.Count == 0)
                    return BadRequest(new { success = false, message = "La plantilla debe incluir al menos una columna de horas." });

                var importedEmployees = new HashSet<int>();
                for (var row = 2; row <= lastRow; row++)
                {
                    var employeeCode = worksheet.Cell(row, 1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(employeeCode))
                        return BadRequest(new { success = false, message = $"La fila {row} no contiene el código del personal." });
                    if (!employeeByCode.TryGetValue(employeeCode, out var employee))
                        return BadRequest(new { success = false, message = $"El código de personal '{employeeCode}' de la fila {row} no pertenece a este proceso." });
                    if (!importedEmployees.Add(employee.PersonalId))
                        return BadRequest(new { success = false, message = $"El personal '{employeeCode}' aparece más de una vez." });

                    foreach (var item in columnMap)
                    {
                        var cell = worksheet.Cell(row, item.Column);
                        var rawValue = cell.GetString().Trim();
                        if (string.IsNullOrWhiteSpace(rawValue))
                            return BadRequest(new { success = false, message = $"La celda {cell.Address} debe contener una cantidad." });
                        if (!cell.TryGetValue<decimal>(out var quantity) &&
                            !decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out quantity) &&
                            !decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
                            return BadRequest(new { success = false, message = $"La celda {cell.Address} no contiene una cantidad numérica válida." });
                        if (quantity < 0)
                            return BadRequest(new { success = false, message = $"La cantidad de la celda {cell.Address} no puede ser negativa." });

                        var storedValue = employee.Cantidades.Single(value => value.HoraRemId == item.HoraRemId);
                        storedValue.Cantidad = quantity;
                    }
                }

                if (importedEmployees.Count == 0)
                    return BadRequest(new { success = false, message = "El archivo no contiene filas de personal para importar." });

                var result = await _rhService.SavePlanillaHorasAsync(
                    new PlanillaHorasGuardarModel { ProcesoId = id, Empleados = detalle.Empleados },
                    CurrentEmpresaId,
                    CurrentUserId);
                return Json(new { success = result.success, message = result.message });
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { success = false, message = "El archivo Excel está dañado o no es válido." });
            }
            catch (IOException)
            {
                return BadRequest(new { success = false, message = "No se pudo leer el archivo Excel." });
            }
            catch (System.Xml.XmlException)
            {
                return BadRequest(new { success = false, message = "El contenido del archivo Excel no es válido." });
            }
            catch (DocumentFormat.OpenXml.Packaging.OpenXmlPackageException)
            {
                return BadRequest(new { success = false, message = "No se pudo leer el archivo Excel. Verifique que sea una plantilla .xlsx válida." });
            }
        }
    }
}
