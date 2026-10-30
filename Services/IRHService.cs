using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services
{
    public interface IRHService
    {
        // =========================================================================
        // 1. MAESTRO DE PERSONAL (EMPLEADOS / TRABAJADORES)
        // =========================================================================
        Task<List<PersonalModel>> GetPersonalListAsync(
            string? search = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25);

        Task<(int total, int activos, int cesados)> GetPersonalCountsAsync();
        Task<PersonalModel?> GetPersonalByIdAsync(int id);
        Task<(bool success, string message, int id)> SavePersonalAsync(PersonalModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoPersonalAsync(int id, int userId);
        Task<(bool success, string message)> RegistrarCesePersonalAsync(int id, DateTime fechaCese, int? motivoId, int userId);

        // =========================================================================
        // 2. PERIODOS LABORALES
        // =========================================================================
        Task<List<PersonalPeriodoLaboralModel>> GetPeriodosLaboralesAsync(int personalId);
        Task<(bool success, string message, int id)> SavePeriodoLaboralAsync(PersonalPeriodoLaboralModel model, int userId);

        // =========================================================================
        // 3. REMUNERACIONES ASIGNADAS
        // =========================================================================
        Task<List<PersonalRemuneracionModel>> GetPersonalRemuneracionesAsync(int personalId);
        Task<(bool success, string message, int id)> SavePersonalRemuneracionAsync(PersonalRemuneracionModel model, int userId);
        Task<bool> DeletePersonalRemuneracionAsync(int id, int userId);

        // =========================================================================
        // 4. DERECHOHABIENTES (DEPENDIENTES)
        // =========================================================================
        Task<List<PersonalDependienteModel>> GetPersonalDependientesAsync(int personalId);
        Task<(bool success, string message, int id)> SavePersonalDependienteAsync(PersonalDependienteModel model, int userId);
        Task<bool> DeletePersonalDependienteAsync(int id, int userId);

        // =========================================================================
        // 5. PLANILLAS ASIGNADAS
        // =========================================================================
        Task<List<PersonalPlanillaModel>> GetPersonalPlanillasAsync(int personalId);
        Task<(bool success, string message, int id)> SavePersonalPlanillaAsync(PersonalPlanillaModel model, int userId);
        Task<bool> DeletePersonalPlanillaAsync(int id, int userId);

        // =========================================================================
        // 6. CATÁLOGOS / LOOKUPS
        // =========================================================================
        Task<List<LookupItemModel>> GetTiposDocIdentidadLookupAsync();
        Task<List<LookupItemModel>> GetSexosLookupAsync();
        Task<List<LookupItemModel>> GetEstadosCivilesLookupAsync();
        Task<List<LookupItemModel>> GetPaisesLookupAsync();
        Task<List<LookupItemModel>> GetPaisesEmisoresLookupAsync();
        Task<List<LookupItemModel>> GetDepartamentosLookupAsync();
        Task<List<LookupItemModel>> GetProvinciasLookupAsync(int? departamentoId = null);
        Task<List<LookupItemModel>> GetDistritosLookupAsync(int? provinciaId = null);
        Task<List<LookupItemModel>> GetTiposViaLookupAsync();
        Task<List<LookupItemModel>> GetTiposZonaLookupAsync();
        Task<List<LookupItemModel>> GetTiposTrabajadorLookupAsync();
        Task<List<LookupItemModel>> GetCategoriasOcupacionalesLookupAsync();
        Task<List<LookupItemModel>> GetRegimenesLaboralesLookupAsync();
        Task<List<LookupItemModel>> GetSituacionesEducativasLookupAsync();
        Task<List<LookupItemModel>> GetCargosPersonalLookupAsync();
        Task<List<LookupItemModel>> GetOcupacionesLookupAsync();
        Task<List<LookupItemModel>> GetCentrosCostoLookupAsync();
        Task<List<LookupItemModel>> GetTiposContratoLookupAsync();
        Task<List<LookupItemModel>> GetSituacionesEspecialesLookupAsync();
        Task<List<LookupItemModel>> GetPeriodicidadesRemuLookupAsync();
        Task<List<LookupItemModel>> GetRegimenesSaludLookupAsync();
        Task<List<LookupItemModel>> GetEpsLookupAsync();
        Task<List<LookupItemModel>> GetRegimenesPensionariosLookupAsync();
        Task<List<LookupItemModel>> GetEsquemasPensionariosLookupAsync(int? regimenPensionarioId = null);
        Task<List<LookupItemModel>> GetSctrPensionLookupAsync();
        Task<List<LookupItemModel>> GetSctrSaludLookupAsync();
        Task<List<LookupItemModel>> GetTiposPagoRemuLookupAsync();
        Task<List<LookupItemModel>> GetBancosLookupAsync();
        Task<List<LookupItemModel>> GetConveniosDobleTribLookupAsync();
        Task<List<LookupItemModel>> GetMotivosFinPeriodoLookupAsync();
        Task<List<LookupItemModel>> GetVinculosFamiliaresLookupAsync();
        Task<List<LookupItemModel>> GetTiposRemuneracionLookupAsync();
        Task<List<LookupItemModel>> GetPlanillasLookupAsync();
        Task<List<LookupItemModel>> GetMonedasLookupAsync();
        Task<List<LookupItemModel>> GetVariablesCalculoLookupAsync();

        // =========================================================================
        // 7. CARGOS DE PERSONAL (cargo_perso)
        // =========================================================================
        Task<List<CargoPersonalModel>> GetCargosPersonalListAsync(string? search = null, string? estado = null, int top = 25);
        Task<CargoPersonalModel?> GetCargoPersonalByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveCargoPersonalAsync(CargoPersonalModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCargoPersonalAsync(int id, int userId);

        // =========================================================================
        // 8. RELOJ MARCADOR (reloj)
        // =========================================================================
        Task<List<RelojModel>> GetRelojesListAsync(int empresaId, string? search = null, string? estado = null, int top = 25);
        Task<RelojModel?> GetRelojByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveRelojAsync(RelojModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRelojAsync(int id, int userId);

        // =========================================================================
        // 9. FERIADOS (feriado)
        // =========================================================================
        Task<List<FeriadoModel>> GetFeriadosListAsync(string? estado = null, int top = 25);
        Task<FeriadoModel?> GetFeriadoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveFeriadoAsync(FeriadoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFeriadoAsync(int id, int userId);

        // =========================================================================
        // 10. RÉGIMEN PENSIONARIO (reg_pension)
        // =========================================================================
        Task<List<RegimenPensionarioModel>> GetRegimenesPensionariosCrudListAsync(string? search = null, string? estado = null, int top = 25);
        Task<RegimenPensionarioModel?> GetRegimenPensionarioByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveRegimenPensionarioAsync(RegimenPensionarioModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRegimenPensionarioAsync(int id, int userId);

        // =========================================================================
        // 11. TIPOS DE REMUNERACIÓN (tipo_remunera)
        // =========================================================================
        Task<List<TipoRemuneraModel>> GetTiposRemuneraListAsync(string? search = null, string? estado = null, int top = 25);
        Task<TipoRemuneraModel?> GetTipoRemuneraByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveTipoRemuneraAsync(TipoRemuneraModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoRemuneraAsync(int id, int userId);

        // =========================================================================
        // 12. HORAS REMUNERATIVAS (hora_rem)
        // =========================================================================
        Task<List<HoraRemunerativaModel>> GetHorasRemunerativasListAsync(int empresaId, string? search = null, string? estado = null, int top = 25);
        Task<HoraRemunerativaModel?> GetHoraRemunerativaByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveHoraRemunerativaAsync(HoraRemunerativaModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoHoraRemunerativaAsync(int id, int userId);

        // =========================================================================
        // 13. PLANILLAS (planilla)
        // =========================================================================
        Task<PlanillaPageModel> GetPlanillasPageAsync(int empresaId, string? search = null, string? estado = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<PlanillaModel?> GetPlanillaAdminByIdAsync(int id, int empresaId);
        Task<List<PlanillaConceptoModel>?> GetPlanillaConceptosAsync(int planillaId, int empresaId);
        Task<(bool success, string message, int id)> SavePlanillaConceptoAsync(PlanillaConceptoModel model, int empresaId, int userId);
        Task<(bool success, string message)> AnularPlanillaConceptoAsync(int id, int planillaId, int empresaId, int userId);
        Task<(bool success, string message, int id)> SavePlanillaAsync(PlanillaModel model, int userId);
        Task<(bool success, string message)> AnularPlanillaAsync(int id, int empresaId, int userId);

        // =========================================================================
        // 14. VARIABLES DE CÁLCULO (var_calc)
        // =========================================================================
        Task<List<LookupItemModel>> GetTiposVariableCalculoLookupAsync();
        Task<List<LookupItemModel>> GetTiposDatoLookupAsync();
        Task<List<VariableCalculoModel>> GetVariablesCalculoListAsync(string? search = null, string? estado = null, int top = 25);
        Task<VariableCalculoModel?> GetVariableCalculoByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveVariableCalculoAsync(VariableCalculoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoVariableCalculoAsync(int id, int userId);

        // =========================================================================
        // 15. CONCEPTOS REMUNERATIVOS (concep_remu)
        // =========================================================================
        Task<List<LookupItemModel>> GetTiposConceptoRemuLookupAsync();
        Task<List<ConceptoRemunerativoModel>> GetConceptosRemuListAsync(int empresaId, string? search = null, string? estado = null, int top = 25);
        Task<ConceptoRemunerativoModel?> GetConceptoRemuByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveConceptoRemuAsync(ConceptoRemunerativoModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoConceptoRemuAsync(int id, int userId);

        // =========================================================================
        // 16. PRÉSTAMOS Y ADELANTOS (presta_perso, presta_cuota)
        // =========================================================================
        Task<List<LookupItemModel>> GetTiposPrestamoLookupAsync();
        Task<List<LookupItemModel>> GetEstadosPrestamoLookupAsync();
        Task<List<LookupItemModel>> GetPersonalLookupAsync(int empresaId);
        Task<List<PrestamoPersonalModel>> GetPrestamosListAsync(int empresaId, string? search = null, string? estado = null, int top = 25);
        Task<PrestamoPersonalModel?> GetPrestamoByIdAsync(int id);
        Task<List<PrestamoCuotaModel>> GetCuotasPorPrestamoAsync(int prestamoId);
        Task<(bool success, string message, int id)> SavePrestamoAsync(PrestamoPersonalModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoPrestamoAsync(int id, int userId);

        // =========================================================================
        // 17. VACACIONES DE PERSONAL (personal_vaca)
        // =========================================================================
        Task<List<LookupItemModel>> GetEstadosVacacionLookupAsync();
        Task<List<PersonalVacacionModel>> GetVacacionesListAsync(int empresaId, string? search = null, string? estado = null, int top = 25);
        Task<PersonalVacacionModel?> GetVacacionByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveVacacionAsync(PersonalVacacionModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoVacacionAsync(int id, int userId);

        // =========================================================================
        // 18. MARCACIONES DE PERSONAL (perso_marca & perso_m_hora)
        // =========================================================================
        Task<List<MarcacionPersonalModel>> GetMarcacionesListAsync(int empresaId, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? personalId = null, string? estado = null, string? verificado = null, string? search = null, int top = 100);
        Task<MarcacionPersonalModel?> GetMarcacionByIdAsync(int id);
        Task<(bool success, string message, int id)> SaveMarcacionAsync(MarcacionCrearEditarModel model, int userId);
        Task<(bool success, string message, string nuevoEstado)> ToggleEstadoMarcacionAsync(int id, int userId);
        Task<(bool success, string message, string nuevoVerificado)> ToggleVerificadoMarcacionAsync(int id, int userId);

        // =========================================================================
        // 19. PROCESO DE PLANILLA (plan_proce & plan_calculo)
        // =========================================================================
        Task<List<PlanillaProcesoModel>> GetPlanillasProcesoListAsync(int empresaId, int? anio = null, int? mes = null, string? search = null, int top = 50);
        Task<int?> GetPeriodoEmpresaSedePlanillaAsync(int empresaId, int empresaSedeId, int periodoId);
        Task<int> GetPermisoPlanillaProcesoAsync(int userId, int empresaId);
        Task<int> GetPermisoModuloRHAsync(int userId, int empresaId, string menuUrl);
        Task<int> GetPermisoModuloAsync(int userId, int empresaId, string area, string menuUrl);
        Task<PlanillaProcesoPageModel> GetPlanillasProcesoPageAsync(int empresaId, int periodoEmpSedeId, int? anio = null, int? mes = null, string? search = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<PlanillaProcesoModel?> GetPlanillaProcesoByIdAsync(int id);
        Task<PlanillaProcesoModel?> GetPlanillaProcesoByIdAsync(int id, int empresaId, int periodoEmpSedeId);
        Task<List<PlanillaCalculoDetalleModel>?> GetPlanillaCalculoDetalleAsync(int planProceId, int empresaId, int periodoEmpSedeId, int? personalId = null);
        Task<(bool success, string message, int id)> SavePlanillaProcesoAsync(PlanillaProcesoCrearModel model, int userId, int empresaId, int periodoEmpSedeId);
        Task<(bool success, string message)> InactivarPlanillaProcesoAsync(int id, int userId, int empresaId, int periodoEmpSedeId);
        Task<(bool success, string message)> ProcesarCalculoPlanillaAsync(int planProceId, int userId);

        // =========================================================================
        // 20. HORAS REMUNERATIVAS POR PROCESO (plan_p_hora)
        // =========================================================================
        Task<PlanillaHorasModel?> GetPlanillaHorasAsync(int planProceId, int empresaId);
        Task<List<PlanillaHorasEmpleadoModel>> GetPersonalDisponiblePlanillaHorasAsync(int planProceId, int empresaId, string? search = null);
        Task<(bool success, string message)> SavePlanillaHorasAsync(PlanillaHorasGuardarModel model, int empresaId, int userId);
        Task<(bool success, string message)> ImportarHorasDesdeMarcacionesAsync(int planProceId, int empresaId, int userId);

        // =========================================================================
        // 21. REGULARIZACIONES DE PLANILLA (plan_p_regu)
        // =========================================================================
        Task<PlanillaRegularizacionPageModel?> GetPlanillaRegularizacionesAsync(int planProceId, int empresaId, int page = 1, int pageSize = 25);
        Task<List<LookupItemModel>?> GetPersonalDisponibleRegularizacionAsync(int planProceId, int empresaId, string? search = null, int top = 25);
        Task<List<LookupItemModel>?> GetConceptosDisponiblesRegularizacionAsync(int planProceId, int empresaId, string? search = null, int top = 25);
        Task<(bool success, string message)> SavePlanillaRegularizacionAsync(PlanillaRegularizacionGuardarModel model, int empresaId, int userId);
        Task<(bool success, string message)> AnularPlanillaRegularizacionAsync(int id, int planProceId, int empresaId, int userId);

        // =========================================================================
        // 22. SOLICITUDES DE ACCESO EXTENDIDO (perso_sol_acc)
        // =========================================================================
        Task<SolicitudAccesoPageModel> GetSolicitudesAccesoAsync(int empresaId, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? estadoAprobacionId = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null);
        Task<(bool success, string message)> ResolverSolicitudAccesoAsync(int id, string? horaAutorizada, bool aprobar, int empresaId, int userId);

        // =========================================================================
        // 20. BOLETAS DE PAGO (ReporteBoleta)
        // =========================================================================
        Task<List<LookupItemModel>> GetPlanillasProcesadasLookupAsync(int empresaId);
        Task<List<LookupItemModel>> GetColaboradoresPorPlanillaProcesadaAsync(int planProceId);
        Task<BoletaPagoModel?> GetBoletaPagoAsync(int planProceId, int personalId);
        Task<List<BoletaPagoModel>> GetBoletasPorPlanillaAsync(int planProceId);
    }
}
