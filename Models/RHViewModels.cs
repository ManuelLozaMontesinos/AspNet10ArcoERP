using System;
using System.Collections.Generic;

namespace AspNet10ArcoERP.Models
{
    // =========================================================================
    // 1. PERSONAL (EMPLEADO / TRABAJADOR)
    // =========================================================================
    public class PersonalModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // personal (char 5)
        public string NumFotocheck { get; set; } = string.Empty;
        public int EmpresaSedeId { get; set; } = 1;

        // Datos Personales (Proveedor)
        public int ProveedorId { get; set; }
        public string RazonSocial { get; set; } = string.Empty; // Apellidos y Nombres completos
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? Nombres { get; set; }
        public int TipoDocIdentidadId { get; set; } = 1; // 1 = DNI
        public string TipoDocIdentidad { get; set; } = "DNI";
        public string NumDocIdentidad { get; set; } = string.Empty;
        public int? PaisEmisorId { get; set; }
        public string? PaisEmisorNombre { get; set; }
        public int SexoId { get; set; } = 1;
        public string Sexo { get; set; } = "M";
        public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-25);
        public int PaisId { get; set; } = 1; // Perú
        public string PaisNombre { get; set; } = "PERU";
        public int EstadoCivilId { get; set; } = 1;
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        // Domicilio 1 (Principal)
        public int TipoViaId { get; set; } = 1;
        public string? NombreVia { get; set; }
        public string? NumeroVia { get; set; }
        public string? Departamento { get; set; }
        public string? Interior { get; set; }
        public string? Manzana { get; set; }
        public string? Lote { get; set; }
        public string? Kilometro { get; set; }
        public string? Block { get; set; }
        public string? Etapa { get; set; }
        public int TipoZonaId { get; set; } = 1;
        public string? NombreZona { get; set; }
        public string? Referencia { get; set; }
        public int DistritoId { get; set; } = 1;
        public int ProvinciaId { get; set; } = 1;
        public int DepartamentoId { get; set; } = 1;
        public string? DistritoNombre { get; set; }
        public string? ProvinciaNombre { get; set; }
        public string? DepartamentoNombre { get; set; }
        public string DireccionCompleta => $"{NombreVia} {NumeroVia} {DistritoNombre}".Trim();

        // Domicilio 2 (Centro Asistencial)
        public string EsCentroAsistencial { get; set; } = "N";
        public int? TipoViaId2 { get; set; }
        public string? NombreVia2 { get; set; }
        public string? NumeroVia2 { get; set; }
        public string? Departamento2 { get; set; }
        public string? Interior2 { get; set; }
        public string? Manzana2 { get; set; }
        public string? Lote2 { get; set; }
        public string? Kilometro2 { get; set; }
        public string? Block2 { get; set; }
        public string? Etapa2 { get; set; }
        public int? TipoZonaId2 { get; set; }
        public string? NombreZona2 { get; set; }
        public string? Referencia2 { get; set; }
        public int? DistritoId2 { get; set; }
        public int? ProvinciaId2 { get; set; }
        public int? DepartamentoId2 { get; set; }

        // Datos Laborales
        public string Domiciliado { get; set; } = "S";
        public int TipoTrabajadorId { get; set; } = 1;
        public string? TipoTrabajadorNombre { get; set; }
        public int? CategoriaOcupacionalId { get; set; }
        public string? CategoriaOcupacionalNombre { get; set; }
        public int RegimenLaboralId { get; set; } = 1;
        public string? RegimenLaboralNombre { get; set; }
        public int? SituacionEducativaId { get; set; }
        public string? SituacionEducativaNombre { get; set; }
        public int CargoPersonalId { get; set; } = 1;
        public string CargoPersonalNombre { get; set; } = string.Empty;
        public int OcupacionId { get; set; } = 1;
        public string? OcupacionNombre { get; set; }
        public int? CentroCostoId { get; set; }
        public string CentroCostoNombre { get; set; } = string.Empty;
        public int TipoContratoId { get; set; } = 1;
        public string? TipoContratoNombre { get; set; }
        public int SituacionEspecialTrabajadorId { get; set; } = 1;
        public int PeriodicidadRemunerativaId { get; set; } = 1; // 1 = Mensual, 2 = Quincenal, etc.
        public string EsEventual { get; set; } = "N";
        public string EsRegimenAtipico { get; set; } = "N";
        public string EsJornadaTrabajoMaxima { get; set; } = "S";
        public string EsJornadaTrabajoNocturna { get; set; } = "N";
        public string EsMiembroSindicalizado { get; set; } = "N";
        public string AsignacionFamiliar { get; set; } = "N";
        public string EsDiscapacitado { get; set; } = "N";
        public string EsExonerado5ta { get; set; } = "N";
        public DateTime FechaIngreso { get; set; } = DateTime.Today;
        public DateTime? FechaAlta { get; set; }
        public DateTime? FechaCese { get; set; }
        public int? MotivoFinPeriodoId { get; set; }
        public string? MotivoFinPeriodoNombre { get; set; }
        public int ConvenioDobleTributacionId { get; set; } = 1;
        public int SituacionTrabajadorId { get; set; } = 1; // 1 = Activo

        // Aseguramiento Social
        public int RegimenSaludId { get; set; } = 1; // EsSalud
        public string? RegimenSaludNombre { get; set; }
        public int? EpsId { get; set; }
        public string? EpsNombre { get; set; }
        public int RegimenPensionarioId { get; set; } = 1; // AFP / ONP
        public string RegimenPensionarioNombre { get; set; } = string.Empty;
        public int? RegimenPensionarioEsquemaId { get; set; } // Flujo / Mixta
        public string? RegimenPensionarioEsquemaNombre { get; set; }
        public DateTime? FechaInscripcionRegimenPensionario { get; set; }
        public string NumeroCuspp { get; set; } = string.Empty;
        public string AportaSctr { get; set; } = "N";
        public int? SctrPensionId { get; set; }
        public int? SctrSaludId { get; set; }
        public int? TipoModalidadFormativaId { get; set; }

        // Datos Complementarios / Pago
        public int TipoPagoRemuneracionId { get; set; } = 1; // Transferencia, Efectivo
        public string? TipoPagoRemuneracionNombre { get; set; }
        public int? BancoId { get; set; }
        public string? BancoNombre { get; set; }
        public string NumeroCuentaAbono { get; set; } = string.Empty;
        public int? BancoCTSId { get; set; }
        public string? BancoCTSNombre { get; set; }
        public string NumeroCuentaCTS { get; set; } = string.Empty;

        // Estado y Auditoría
        public string Estado { get; set; } = "A"; // A = Activo, I = Inactivo
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaModifica { get; set; }

        // Indicador si está cesado
        public bool EstaCesado => FechaCese.HasValue;
    }

    // =========================================================================
    // 2. PERIODOS LABORALES (perso_per_lab)
    // =========================================================================
    public class PersonalPeriodoLaboralModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? MotivoFinPeriodoId { get; set; }
        public string? MotivoFinPeriodoNombre { get; set; }
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 3. REMUNERACIONES ASIGNADAS (personal_rem)
    // =========================================================================
    public class PersonalRemuneracionModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public int TipoRemuneracionId { get; set; }
        public string TipoRemuneracionNombre { get; set; } = string.Empty;
        public int MonedaId { get; set; } = 1; // 1 = Soles
        public string MonedaSimbolo { get; set; } = "S/";
        public decimal Monto { get; set; }
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 4. DERECHOHABIENTES (personal_depen)
    // =========================================================================
    public class PersonalDependienteModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string NombreCompleto => $"{ApellidoPaterno} {ApellidoMaterno} {Nombres}".Trim();
        public int TipoDocIdentidadId { get; set; } = 1;
        public string TipoDocIdentidadNombre { get; set; } = "DNI";
        public string NumDocIdentidad { get; set; } = string.Empty;
        public int? PaisEmisorId { get; set; }
        public string PaisEmisorNombre { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; } = DateTime.Today;
        public int SexoId { get; set; } = 1;
        public string SexoNombre { get; set; } = "M";
        public int VinculoFamiliarId { get; set; } = 1; // Hijo, Cónyuge, etc.
        public string VinculoFamiliarNombre { get; set; } = string.Empty;
        public DateTime? FechaBaja { get; set; }
        public int? MotivoBajaId { get; set; }
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 5. PLANILLAS ASIGNADAS (perso_plan)
    // =========================================================================
    public class PersonalPlanillaModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public int PlanillaId { get; set; }
        public string PlanillaNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 6. CARGOS DE PERSONAL (cargo_perso)
    // =========================================================================
    // 6. CARGOS DE PERSONAL (cargo_perso)
    // =========================================================================
    public class CargoPersonalModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public int PersonalCount { get; set; }
    }

    // =========================================================================
    // 7. RELOJ MARCADOR (reloj)
    // =========================================================================
    public class RelojModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; } = 1;
        public string Nombre { get; set; } = string.Empty;
        public string? RutaArchivo { get; set; }
        public string EsCentral { get; set; } = "N";
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 8. FERIADOS (feriado)
    // =========================================================================
    public class FeriadoModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string Estado { get; set; } = "A";
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string DiaSemana => Fecha.ToString("dddd", new System.Globalization.CultureInfo("es-PE"));
    }

    // =========================================================================
    // 9. RÉGIMEN PENSIONARIO (reg_pension)
    // =========================================================================
    public class RegimenPensionarioModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 10. TIPO DE REMUNERACIÓN (tipo_remunera)
    // =========================================================================
    public class TipoRemuneraModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int VariableCalculoId { get; set; }
        public string VariableCalculoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 11. HORA REMUNERATIVA (hora_rem)
    // =========================================================================
    public class HoraRemunerativaModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; } = 1;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public int VariableCalculoId { get; set; }
        public string VariableCalculoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 12. PLANILLA (planilla)
    // =========================================================================
    public class PlanillaModel
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; } = 1;
        public string Nombre { get; set; } = string.Empty;
        public short ProcesoAnual { get; set; } = 1;
        public int TipoTrabajadorId { get; set; } = 1;
        public string TipoTrabajadorNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 13. VARIABLE DE CÁLCULO (var_calc)
    // =========================================================================
    public class VariableCalculoModel
    {
        public int Id { get; set; }
        public string Variable { get; set; } = string.Empty; // var_calc (varchar 20)
        public string Nombre { get; set; } = string.Empty; // varchar 100
        public int TipoVariableCalculoId { get; set; } = 1; // tipo_v_calc_id
        public string TipoVariableCalculoNombre { get; set; } = string.Empty;
        public int? TipoDatoId { get; set; } = 3; // tipo_dato_id (default 3 = Decimal)
        public string TipoDatoNombre { get; set; } = string.Empty;
        public string Condicion { get; set; } = string.Empty;
        public decimal ValorNumericoVerdad { get; set; } = 0;
        public decimal ValorNumericoFalso { get; set; } = 0;
        public string ValorCadenaVerdad { get; set; } = string.Empty;
        public string ValorCadenaFalso { get; set; } = string.Empty;
        public string TablaOrigen { get; set; } = string.Empty;
        public string TablaCondicion { get; set; } = string.Empty;
        public string TablaAgrupacion { get; set; } = string.Empty;
        public string EsSubconsulta { get; set; } = "N";
        public string SubconsultaAlias { get; set; } = string.Empty;
        public string SubconsultaTipo { get; set; } = "I";
        public string SubconsultaColumna { get; set; } = string.Empty;
        public string SubconsultaEnlace { get; set; } = string.Empty;
        public string AlcanceFormulaHora { get; set; } = "S";
        public string AlcanceFormula { get; set; } = "N";
        public string AlcanceCondicion { get; set; } = "S";
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 14. CONCEPTO REMUNERATIVO (concep_remu)
    // =========================================================================
    public class ConceptoRemunerativoModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // concep_remu (char 3)
        public int EmpresaId { get; set; } = 1;
        public string Nombre { get; set; } = string.Empty; // varchar 150
        public string NombreBoleta { get; set; } = string.Empty; // varchar 100
        public string EsRegularizacion { get; set; } = "N"; // es_regula char 1
        public int TipoConceptoRemunerativoId { get; set; } = 1; // tipo_con_remu_id (1=Remuneración, 2=Aportación, 3=Retención)
        public string TipoConceptoRemunerativoNombre { get; set; } = string.Empty;
        public int? IngresoTributoDescuentoId { get; set; } // ing_trib_des_id
        public string UsaFormula { get; set; } = "S"; // usa_formula char 1
        public string FormulaHora { get; set; } = string.Empty; // formula_hora varchar 450
        public string Formula { get; set; } = string.Empty; // formula varchar 600
        public string Condicion { get; set; } = string.Empty; // condicion varchar 250
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 15. PRÉSTAMO Y ADELANTO DE PERSONAL (presta_perso, presta_cuota)
    // =========================================================================
    public class PrestamoPersonalModel
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty; // presta_perso (char 5)
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public int MonedaId { get; set; } = 1;
        public string MonedaNombre { get; set; } = "SOLES";
        public string MonedaSimbolo { get; set; } = "S/";
        public decimal MontoTipoCambio { get; set; } = 1;
        public decimal Monto { get; set; }
        public int CantidadCuotas { get; set; } = 1;
        public DateTime FechaInicio { get; set; } = DateTime.Today;
        public int DiasCuota { get; set; } = 30; // intervalo en días
        public int TipoPrestamoId { get; set; } = 2; // 1=Adelanto, 2=Préstamo, 3=Descuento
        public string TipoPrestamoNombre { get; set; } = "PRESTAMO";
        public int EstadoPrestamoId { get; set; } = 1; // 1=Pendiente, 2=Pagado, 3=Anulado
        public string EstadoPrestamoNombre { get; set; } = "PENDIENTE";
        public string Estado { get; set; } = "A";
        public List<PrestamoCuotaModel> Cuotas { get; set; } = new();
    }

    public class PrestamoCuotaModel
    {
        public int Id { get; set; }
        public int PrestamoPersonalId { get; set; }
        public short Cuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public int TipoCancelacionCuotaId { get; set; } = 1;
        public string TipoCancelacionCuotaNombre { get; set; } = "PLANILLA";
        public int EstadoCuotaId { get; set; } = 1; // 1=Pendiente, 2=Cancelado, 3=Procesado
        public string EstadoCuotaNombre { get; set; } = "PENDIENTE";
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 16. VACACIONES DE PERSONAL (personal_vaca)
    // =========================================================================
    public class PersonalVacacionModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string CentroCostoNombre { get; set; } = string.Empty;
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaInicio { get; set; } = DateTime.Today;
        public DateTime? FechaFin { get; set; } = DateTime.Today.AddDays(14);
        public int DiasACuenta { get; set; } = 15;
        public int EstadoVacacionId { get; set; } = 1; // 1=Programado, 2=Gozado, 3=Anulado
        public string EstadoVacacionNombre { get; set; } = "PROGRAMADO";
        public string Estado { get; set; } = "A";
    }

    // =========================================================================
    // 17. MARCACIÓN DE PERSONAL (perso_marca & perso_m_hora)
    // =========================================================================
    public class MarcacionPersonalModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string CargoNombre { get; set; } = string.Empty;
        public string CentroCostoNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string HoraMinuto { get; set; } = string.Empty;
        public string IgnoraTolera { get; set; } = "N"; // S/N
        public string EsFalta { get; set; } = "N"; // S/N
        public string EsRemunera { get; set; } = "S"; // S/N
        public string Verificado { get; set; } = "N"; // S/N
        public string Estado { get; set; } = "A";
        public List<MarcacionHoraDetalleModel> HorasDetalle { get; set; } = new();
        public decimal TotalHoras => HorasDetalle.Sum(h => h.Cantidad);
    }

    public class MarcacionHoraDetalleModel
    {
        public int Id { get; set; }
        public int MarcacionId { get; set; }
        public int HoraRemId { get; set; }
        public string HoraRemNombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class MarcacionCrearEditarModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string HoraMinuto { get; set; } = "08:00";
        public string IgnoraTolera { get; set; } = "N";
        public string EsFalta { get; set; } = "N";
        public string EsRemunera { get; set; } = "S";
        public string Verificado { get; set; } = "N";
        public string Estado { get; set; } = "A";
        public List<MarcacionHoraItemInput> Horas { get; set; } = new();
    }

    public class MarcacionHoraItemInput
    {
        public int HoraRemId { get; set; }
        public decimal Cantidad { get; set; }
    }

    // =========================================================================
    // 18. PROCESO DE PLANILLA (plan_proce & plan_calculo)
    // =========================================================================
    public class PlanillaProcesoModel
    {
        public int Id { get; set; }
        public int PeriodoEmpSedeId { get; set; }
        public int PeriodoAnio { get; set; }
        public int PlanillaId { get; set; }
        public string PlanillaNombre { get; set; } = string.Empty;
        public short ProcAnual { get; set; } = 1; // Número de proceso dentro del año, según el legacy.
        public int MesId { get; set; } = DateTime.Today.Month;
        public string MesNombre { get; set; } = string.Empty;
        public int MonedaId { get; set; } = 1; // 1=Soles, 2=Dólares
        public string MonedaNombre { get; set; } = "SOLES";
        public decimal MontoTipoCambio { get; set; } = 1.0m;
        public int PeriRemuId { get; set; } = 1; // 1=Mensual, 2=Quincenal, 3=Semanal
        public string PeriRemuNombre { get; set; } = "MENSUAL";
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public DateTime? FechaProceso { get; set; }
        public int EstadoPlanProceId { get; set; }
        public string EstadoPlanProceCodigo { get; set; } = string.Empty;
        public string EstadoPlanProceNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public int TotalTrabajadores { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TotalNeto => TotalIngresos - TotalDescuentos;
    }

    public class PlanillaProcesoPageModel
    {
        public List<PlanillaProcesoModel> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int PeriodoAnio { get; set; }
        public int? MonedaLocalId { get; set; }
        public List<LookupItemModel> Planillas { get; set; } = new();
        public List<LookupItemModel> Monedas { get; set; } = new();
        public List<LookupItemModel> Periodicidades { get; set; } = new();
        public List<LookupItemModel> Meses { get; set; } = new();
    }

    public class PlanillaCalculoDetalleModel
    {
        public int Id { get; set; }
        public int PlanProceId { get; set; }
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string CargoNombre { get; set; } = string.Empty;
        public int ConcepRemuId { get; set; }
        public string ConcepRemuCodigo { get; set; } = string.Empty;
        public string ConcepRemuNombre { get; set; } = string.Empty;
        public int TipoConceptoId { get; set; }
        public string TipoConceptoCodigo { get; set; } = string.Empty; // 01=Remuneración, 02=Aportación, 03=Retención.
        public string TipoConceptoNombre { get; set; } = string.Empty;
        public decimal Horas { get; set; }
        public decimal MontoSub { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class PlanillaRegularizacionModel
    {
        public int Id { get; set; }
        public int PlanProceId { get; set; }
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public int ConcepRemuId { get; set; }
        public string ConcepRemuCodigo { get; set; } = string.Empty;
        public string ConcepRemuNombre { get; set; } = string.Empty;
        public decimal CantidadHora { get; set; }
        public decimal Monto { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
    }

    public class PlanillaRegularizacionPageModel
    {
        public List<PlanillaRegularizacionModel> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PlanillaRegularizacionGuardarModel
    {
        public int Id { get; set; }
        public int PlanProceId { get; set; }
        public int PersonalId { get; set; }
        public int ConcepRemuId { get; set; }
        public decimal Monto { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }

    public class EstadoAprobacionModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class SolicitudAccesoModel
    {
        public int Id { get; set; }
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string HoraSolicitada { get; set; } = string.Empty;
        public int? PersonalAutorizaId { get; set; }
        public string PersonalAutorizaNombre { get; set; } = string.Empty;
        public string HoraAutorizada { get; set; } = string.Empty;
        public DateTime? FechaAutoriza { get; set; }
        public int EstadoAprobacionId { get; set; }
        public string EstadoAprobacionCodigo { get; set; } = string.Empty;
        public string EstadoAprobacionNombre { get; set; } = string.Empty;
    }

    public class SolicitudAccesoPageModel
    {
        public List<SolicitudAccesoModel> Items { get; set; } = new();
        public List<EstadoAprobacionModel> Estados { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int? EstadoSeleccionadoId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }

    public class ResolverSolicitudAccesoModel
    {
        public int Id { get; set; }
        public string HoraAutorizada { get; set; } = string.Empty;
    }

    public class PlanillaProcesoCrearModel
    {
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public int Id { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public int PlanillaId { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public short ProcAnual { get; set; } = 1;
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public int MesId { get; set; } = DateTime.Today.Month;
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public int MonedaId { get; set; } = 1;
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public decimal MontoTipoCambio { get; set; } = 1.0m;
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public int PeriRemuId { get; set; } = 1;
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public DateTime FechaDesde { get; set; }
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
        public DateTime FechaHasta { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class PlanillaHorasPageModel
    {
        public List<PlanillaProcesoModel> Procesos { get; set; } = new();
        public PlanillaHorasModel? Detalle { get; set; }
        public int? ProcesoSeleccionadoId { get; set; }
    }

    public class PlanillaHorasModel
    {
        public PlanillaProcesoModel Proceso { get; set; } = new();
        public List<LookupItemModel> Horas { get; set; } = new();
        public List<PlanillaHorasEmpleadoModel> Empleados { get; set; } = new();
    }

    public class PlanillaHorasEmpleadoModel
    {
        public int PersonalId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "A";
        public List<PlanillaHoraCantidadModel> Cantidades { get; set; } = new();
    }

    public class PlanillaHoraCantidadModel
    {
        public int HoraRemId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class PlanillaHorasGuardarModel
    {
        public int ProcesoId { get; set; }
        public List<PlanillaHorasEmpleadoModel> Empleados { get; set; } = new();
    }

    // =========================================================================
    // 19. REPORTE BOLETA DE PAGO
    // =========================================================================
    public class BoletaPagoModel
    {
        public int PlanProceId { get; set; }
        public string PlanillaNombre { get; set; } = string.Empty;
        public string PeriodoTexto { get; set; } = string.Empty;
        public string EmpresaRazonSocial { get; set; } = "ARCO METALS S.A.C.";
        public string EmpresaRuc { get; set; } = "20601234567";
        public string EmpresaDireccion { get; set; } = "Av. Industrial 123 - Lima";
        public int PersonalId { get; set; }
        public string PersonalCodigo { get; set; } = string.Empty;
        public string PersonalNombre { get; set; } = string.Empty;
        public string DocumentoIdentidad { get; set; } = string.Empty;
        public string CargoNombre { get; set; } = string.Empty;
        public string CentroCostoNombre { get; set; } = string.Empty;
        public string RegimenPensionario { get; set; } = string.Empty;
        public string Cuspp { get; set; } = string.Empty;
        public DateTime? FechaIngreso { get; set; }
        public decimal DiasLaborados { get; set; } = 30;
        public decimal HorasLaboradas { get; set; } = 240;
        public List<BoletaItemConceptoModel> Ingresos { get; set; } = new();
        public List<BoletaItemConceptoModel> Descuentos { get; set; } = new();
        public List<BoletaItemConceptoModel> AportesEmpleador { get; set; } = new();
        public decimal TotalIngresos => Ingresos.Sum(x => x.Monto);
        public decimal TotalDescuentos => Descuentos.Sum(x => x.Monto);
        public decimal TotalAportes => AportesEmpleador.Sum(x => x.Monto);
        public decimal NetoPagar => TotalIngresos - TotalDescuentos;
        public string NetoEnLetras { get; set; } = string.Empty;
    }

    public class BoletaItemConceptoModel
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Horas { get; set; }
        public decimal Monto { get; set; }
    }
}
