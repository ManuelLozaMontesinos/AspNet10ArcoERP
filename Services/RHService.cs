using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;
using Dapper;

namespace AspNet10ArcoERP.Services
{
    public class RHService : IRHService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private sealed class PlanillaHorasEmpleadoRow
        {
            public int PersonalId { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Estado { get; set; } = "A";
        }

        private sealed class PlanillaHorasValorRow
        {
            public int PersonalId { get; set; }
            public int HoraRemId { get; set; }
            public decimal Cantidad { get; set; }
            public string Estado { get; set; } = "A";
        }

        public RHService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        // =========================================================================
        // 1. MAESTRO DE PERSONAL (BANDEJA Y CRUD)
        // =========================================================================
        public async Task<List<PersonalModel>> GetPersonalListAsync(
            string? search = null, 
            string? estado = null, 
            string? sort = null, 
            string? dir = null, 
            int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();

            string sortCol = (sort ?? "codigo").ToLower();
            string sortDir = (dir ?? "desc").ToLower() == "asc" ? "ASC" : "DESC";

            string orderBy = sortCol switch
            {
                "codigo" => $"p.personal {sortDir}",
                "nombre" or "razonsocial" => $"prv.razon_social {sortDir}",
                "documento" or "numdoc" => $"prv.num_doc_iden {sortDir}",
                "cargo" => $"cp.nombre {sortDir}",
                "centrocosto" => $"cc.nombre {sortDir}",
                "fechaingreso" or "fechaing" => $"p.fecha_ing {sortDir}",
                "fechacese" => $"p.fecha_cese {sortDir}",
                "estado" => $"p.estado {sortDir}",
                _ => $"p.personal_id {sortDir}"
            };

            string sql = $@"
                SELECT TOP (@Top)
                    p.personal_id AS Id,
                    p.personal AS Codigo,
                    ISNULL(p.num_fotocheck, '') AS NumFotocheck,
                    p.empresa_sede_id AS EmpresaSedeId,
                    p.proveedor_id AS ProveedorId,
                    ISNULL(prv.razon_social, '') AS RazonSocial,
                    ISNULL(prv.apellido_pat, '') AS ApellidoPaterno,
                    ISNULL(prv.apellido_mat, '') AS ApellidoMaterno,
                    ISNULL(prv.nombres, '') AS Nombres,
                    ISNULL(tdi.abrev, 'DNI') AS TipoDocIdentidad,
                    ISNULL(prv.num_doc_iden, '') AS NumDocIdentidad,
                    ISNULL(s.nombre, 'M') AS Sexo,
                    p.fecha_nac AS FechaNacimiento,
                    p.fecha_ing AS FechaIngreso,
                    p.fecha_alta AS FechaAlta,
                    p.fecha_cese AS FechaCese,
                    ISNULL(cp.nombre, '') AS CargoPersonalNombre,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    ISNULL(rp.nombre, '') AS RegimenPensionarioNombre,
                    ISNULL(prv.nombre_via, '') + ' ' + ISNULL(prv.numero_via, '') + ' ' + ISNULL(d.nombre, '') AS DireccionCompleta,
                    p.estado AS Estado
                FROM personal p
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN tipo_doc_iden tdi ON tdi.tipo_doc_iden_id = prv.tipo_doc_iden_id
                LEFT JOIN sexo s ON s.sexo_id = p.sexo_id
                LEFT JOIN cargo_perso cp ON cp.cargo_perso_id = p.cargo_perso_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                LEFT JOIN reg_pension rp ON rp.reg_pension_id = p.reg_pension_id
                LEFT JOIN distrito d ON d.distrito_id = prv.distrito_id
                WHERE (@Search IS NULL OR prv.razon_social LIKE '%' + @Search + '%' OR prv.num_doc_iden LIKE '%' + @Search + '%' OR p.personal LIKE '%' + @Search + '%')
                  AND (
                      @Estado IS NULL 
                      OR (@Estado = 'A' AND p.estado = 'A' AND p.fecha_cese IS NULL)
                      OR (@Estado = 'C' AND p.fecha_cese IS NOT NULL)
                      OR (@Estado = 'I' AND p.estado = 'I')
                  )
                ORDER BY {orderBy}";

            var rows = await conn.QueryAsync<PersonalModel>(sql, new {
                Top = top > 0 ? top : 25,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpper()
            });

            return rows.ToList();
        }

        public async Task<(int total, int activos, int cesados)> GetPersonalCountsAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    COUNT(*) AS Total,
                    SUM(CASE WHEN estado = 'A' AND fecha_cese IS NULL THEN 1 ELSE 0 END) AS Activos,
                    SUM(CASE WHEN fecha_cese IS NOT NULL THEN 1 ELSE 0 END) AS Cesados
                FROM personal";

            var row = await conn.QueryFirstOrDefaultAsync(sql);
            if (row != null)
            {
                return ((int)row.Total, (int)row.Activos, (int)row.Cesados);
            }
            return (0, 0, 0);
        }

        public async Task<PersonalModel?> GetPersonalByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT
                    p.personal_id AS Id,
                    p.personal AS Codigo,
                    ISNULL(p.num_fotocheck, '') AS NumFotocheck,
                    p.empresa_sede_id AS EmpresaSedeId,
                    p.proveedor_id AS ProveedorId,
                    ISNULL(prv.razon_social, '') AS RazonSocial,
                    ISNULL(prv.apellido_pat, '') AS ApellidoPaterno,
                    ISNULL(prv.apellido_mat, '') AS ApellidoMaterno,
                    ISNULL(prv.nombres, '') AS Nombres,
                    prv.tipo_doc_iden_id AS TipoDocIdentidadId,
                    ISNULL(tdi.abrev, 'DNI') AS TipoDocIdentidad,
                    ISNULL(prv.num_doc_iden, '') AS NumDocIdentidad,
                    p.pais_emisor_id AS PaisEmisorId,
                    ISNULL(pe.nombre, '') AS PaisEmisorNombre,
                    p.sexo_id AS SexoId,
                    ISNULL(s.nombre, 'M') AS Sexo,
                    p.fecha_nac AS FechaNacimiento,
                    p.pais_id AS PaisId,
                    ISNULL(pa.nombre, 'PERU') AS PaisNombre,
                    p.estado_civil_id AS EstadoCivilId,
                    ISNULL(prv.telefono, '') AS Telefono,
                    ISNULL(prv.email, '') AS Email,

                    -- Domicilio 1
                    prv.tipo_via_id AS TipoViaId,
                    ISNULL(prv.nombre_via, '') AS NombreVia,
                    ISNULL(prv.numero_via, '') AS NumeroVia,
                    ISNULL(prv.dpto, '') AS Departamento,
                    ISNULL(prv.interior, '') AS Interior,
                    ISNULL(prv.manzana, '') AS Manzana,
                    ISNULL(prv.lote, '') AS Lote,
                    ISNULL(prv.kilometro, '') AS Kilometro,
                    ISNULL(prv.block, '') AS Block,
                    ISNULL(prv.etapa, '') AS Etapa,
                    prv.tipo_zona_id AS TipoZonaId,
                    ISNULL(prv.nombre_zona, '') AS NombreZona,
                    ISNULL(prv.referencia, '') AS Referencia,
                    prv.distrito_id AS DistritoId,
                    d.provincia_id AS ProvinciaId,
                    pvc.departamento_id AS DepartamentoId,
                    ISNULL(d.nombre, '') AS DistritoNombre,
                    ISNULL(pvc.nombre, '') AS ProvinciaNombre,
                    ISNULL(dp.nombre, '') AS DepartamentoNombre,

                    -- Domicilio 2
                    ISNULL(p.es_centro_asis, 'N') AS EsCentroAsistencial,
                    p.tipo_via_id AS TipoViaId2,
                    ISNULL(p.nombre_via, '') AS NombreVia2,
                    ISNULL(p.numero_via, '') AS NumeroVia2,
                    ISNULL(p.dpto, '') AS Departamento2,
                    ISNULL(p.interior, '') AS Interior2,
                    ISNULL(p.manzana, '') AS Manzana2,
                    ISNULL(p.lote, '') AS Lote2,
                    ISNULL(p.kilometro, '') AS Kilometro2,
                    ISNULL(p.block, '') AS Block2,
                    ISNULL(p.etapa, '') AS Etapa2,
                    p.tipo_zona_id AS TipoZonaId2,
                    ISNULL(p.nombre_zona, '') AS NombreZona2,
                    ISNULL(p.referencia, '') AS Referencia2,
                    p.distrito_id AS DistritoId2,
                    d2.provincia_id AS ProvinciaId2,
                    pvc2.departamento_id AS DepartamentoId2,

                    -- Laboral
                    ISNULL(p.domiciliado, 'S') AS Domiciliado,
                    p.tipo_trabaja_id AS TipoTrabajadorId,
                    ISNULL(tt.nombre, '') AS TipoTrabajadorNombre,
                    p.cate_ocupa_id AS CategoriaOcupacionalId,
                    ISNULL(co.nombre, '') AS CategoriaOcupacionalNombre,
                    p.reg_laboral_id AS RegimenLaboralId,
                    ISNULL(rl.nombre, '') AS RegimenLaboralNombre,
                    p.situa_educa_id AS SituacionEducativaId,
                    ISNULL(se.nombre, '') AS SituacionEducativaNombre,
                    p.cargo_perso_id AS CargoPersonalId,
                    ISNULL(cp.nombre, '') AS CargoPersonalNombre,
                    p.ocupacion_id AS OcupacionId,
                    ISNULL(o.nombre, '') AS OcupacionNombre,
                    p.centro_costo_id AS CentroCostoId,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    p.tipo_contrato_id AS TipoContratoId,
                    ISNULL(tc.nombre, '') AS TipoContratoNombre,
                    p.situ_espe_trab_id AS SituacionEspecialTrabajadorId,
                    p.peri_remu_id AS PeriodicidadRemunerativaId,
                    ISNULL(p.es_eventual, 'N') AS EsEventual,
                    ISNULL(p.es_regimen_atipico, 'N') AS EsRegimenAtipico,
                    ISNULL(p.es_jorn_t_max, 'S') AS EsJornadaTrabajoMaxima,
                    ISNULL(p.es_jorn_t_noc, 'N') AS EsJornadaTrabajoNocturna,
                    ISNULL(p.es_miemb_sin, 'N') AS EsMiembroSindicalizado,
                    ISNULL(p.asigna_fami, 'N') AS AsignacionFamiliar,
                    ISNULL(p.es_discapa, 'N') AS EsDiscapacitado,
                    ISNULL(p.es_exon_5ta, 'N') AS EsExonerado5ta,
                    p.fecha_ing AS FechaIngreso,
                    p.fecha_alta AS FechaAlta,
                    p.fecha_cese AS FechaCese,
                    p.motivo_fin_peri_id AS MotivoFinPeriodoId,
                    ISNULL(mfp.nombre, '') AS MotivoFinPeriodoNombre,
                    p.conve_d_trib_id AS ConvenioDobleTributacionId,
                    p.situa_trab_id AS SituacionTrabajadorId,

                    -- Aseguramiento Social
                    p.reg_salud_id AS RegimenSaludId,
                    ISNULL(rs.nombre, '') AS RegimenSaludNombre,
                    p.eps_id AS EpsId,
                    ISNULL(eps.nombre, '') AS EpsNombre,
                    p.reg_pension_id AS RegimenPensionarioId,
                    ISNULL(rp.nombre, '') AS RegimenPensionarioNombre,
                    p.reg_pens_esq_id AS RegimenPensionarioEsquemaId,
                    p.fecha_ins_reg_p AS FechaInscripcionRegimenPensionario,
                    ISNULL(p.num_cuspp, '') AS NumeroCuspp,
                    ISNULL(p.aporta_sctr, 'N') AS AportaSctr,
                    p.sctr_pension_id AS SctrPensionId,
                    p.sctr_salud_id AS SctrSaludId,
                    p.tipo_moda_forma_id AS TipoModalidadFormativaId,

                    -- Complementarios
                    p.tipo_pago_remu_id AS TipoPagoRemuneracionId,
                    ISNULL(tpr.nombre, '') AS TipoPagoRemuneracionNombre,
                    p.banco_id AS BancoId,
                    ISNULL(b.nombre, '') AS BancoNombre,
                    ISNULL(p.num_cta_abo, '') AS NumeroCuentaAbono,
                    p.banco_cts_id AS BancoCTSId,
                    ISNULL(bcts.nombre, '') AS BancoCTSNombre,
                    ISNULL(p.num_cta_cts, '') AS NumeroCuentaCTS,

                    p.estado AS Estado,
                    p.fecha_creacion AS FechaCreacion,
                    p.fecha_modifica AS FechaModifica
                FROM personal p
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN tipo_doc_iden tdi ON tdi.tipo_doc_iden_id = prv.tipo_doc_iden_id
                LEFT JOIN pais_emisor pe ON pe.pais_emisor_id = p.pais_emisor_id
                LEFT JOIN sexo s ON s.sexo_id = p.sexo_id
                LEFT JOIN pais pa ON pa.pais_id = p.pais_id
                LEFT JOIN distrito d ON d.distrito_id = prv.distrito_id
                LEFT JOIN provincia pvc ON pvc.provincia_id = d.provincia_id
                LEFT JOIN departamento dp ON dp.departamento_id = pvc.departamento_id
                LEFT JOIN distrito d2 ON d2.distrito_id = p.distrito_id
                LEFT JOIN provincia pvc2 ON pvc2.provincia_id = d2.provincia_id
                LEFT JOIN departamento dp2 ON dp2.departamento_id = pvc2.departamento_id
                LEFT JOIN tipo_trabaja tt ON tt.tipo_trabaja_id = p.tipo_trabaja_id
                LEFT JOIN cate_ocupa co ON co.cate_ocupa_id = p.cate_ocupa_id
                LEFT JOIN reg_laboral rl ON rl.reg_laboral_id = p.reg_laboral_id
                LEFT JOIN situa_educa se ON se.situa_educa_id = p.situa_educa_id
                LEFT JOIN cargo_perso cp ON cp.cargo_perso_id = p.cargo_perso_id
                LEFT JOIN ocupacion o ON o.ocupacion_id = p.ocupacion_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                LEFT JOIN tipo_contrato tc ON tc.tipo_contrato_id = p.tipo_contrato_id
                LEFT JOIN motivo_fin_peri mfp ON mfp.motivo_fin_peri_id = p.motivo_fin_peri_id
                LEFT JOIN reg_salud rs ON rs.reg_salud_id = p.reg_salud_id
                LEFT JOIN eps eps ON eps.eps_id = p.eps_id
                LEFT JOIN reg_pension rp ON rp.reg_pension_id = p.reg_pension_id
                LEFT JOIN tipo_pago_remu tpr ON tpr.tipo_pago_remu_id = p.tipo_pago_remu_id
                LEFT JOIN banco b ON b.banco_id = p.banco_id
                LEFT JOIN banco bcts ON bcts.banco_id = p.banco_cts_id
                WHERE p.personal_id = @Id";

            return await conn.QueryFirstOrDefaultAsync<PersonalModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SavePersonalAsync(PersonalModel model, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                string apePat = (model.ApellidoPaterno ?? "").Trim().ToUpper();
                string apeMat = (model.ApellidoMaterno ?? "").Trim().ToUpper();
                string nombres = (model.Nombres ?? "").Trim().ToUpper();
                string razonSocial = $"{apePat} {apeMat} {nombres}".Trim();
                if (string.IsNullOrWhiteSpace(razonSocial))
                    razonSocial = (model.RazonSocial ?? "").Trim().ToUpper();

                string numDoc = (model.NumDocIdentidad ?? "").Trim();
                int tipoDocId = model.TipoDocIdentidadId > 0 ? model.TipoDocIdentidadId : 1;

                int proveedorId = model.ProveedorId;

                // 1. GESTIÓN DEL REGISTRO EN PROVEEDOR (PERSONA NATURAL)
                if (proveedorId <= 0)
                {
                    // Buscar si ya existe una persona con ese documento
                    var existente = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT proveedor_id FROM proveedor WHERE num_doc_iden = @NumDoc AND num_doc_iden <> ''",
                        new { NumDoc = numDoc }, trans);

                    if (existente.HasValue && existente.Value > 0)
                    {
                        proveedorId = existente.Value;
                        // Actualizar datos del proveedor
                        await conn.ExecuteAsync(@"
                            UPDATE proveedor 
                            SET razon_social = @RazonSocial,
                                apellido_pat = @ApellidoPat,
                                apellido_mat = @ApellidoMat,
                                nombres = @Nombres,
                                tipo_via_id = @TipoViaId,
                                nombre_via = @NombreVia,
                                numero_via = @NumeroVia,
                                dpto = @Dpto,
                                interior = @Interior,
                                manzana = @Manzana,
                                lote = @Lote,
                                kilometro = @Kilometro,
                                block = @Block,
                                etapa = @Etapa,
                                tipo_zona_id = @TipoZonaId,
                                nombre_zona = @NombreZona,
                                referencia = @Referencia,
                                distrito_id = @DistritoId,
                                telefono = @Telefono,
                                email = @Email,
                                usuario_modifica_id = @UserId,
                                fecha_modifica = GETDATE()
                            WHERE proveedor_id = @ProveedorId",
                            new {
                                ProveedorId = proveedorId,
                                RazonSocial = razonSocial,
                                ApellidoPat = apePat,
                                ApellidoMat = apeMat,
                                Nombres = nombres,
                                TipoViaId = model.TipoViaId > 0 ? model.TipoViaId : 1,
                                NombreVia = model.NombreVia ?? "",
                                NumeroVia = model.NumeroVia ?? "",
                                Dpto = model.Departamento ?? "",
                                Interior = model.Interior ?? "",
                                Manzana = model.Manzana ?? "",
                                Lote = model.Lote ?? "",
                                Kilometro = model.Kilometro ?? "",
                                Block = model.Block ?? "",
                                Etapa = model.Etapa ?? "",
                                TipoZonaId = model.TipoZonaId > 0 ? model.TipoZonaId : 1,
                                NombreZona = model.NombreZona ?? "",
                                Referencia = model.Referencia ?? "",
                                DistritoId = model.DistritoId > 0 ? model.DistritoId : 1,
                                Telefono = model.Telefono ?? "",
                                Email = model.Email ?? "",
                                UserId = userId
                            }, trans);
                    }
                    else
                    {
                        // Insertar nuevo proveedor con su código correlativo oficial y secuencia
                        const string sqlProv = @"
                            UPDATE tabla 
                            SET correlativo = correlativo + 1 
                            WHERE tabla = '0003';

                            DECLARE @codProv VARCHAR(10);
                            SELECT @codProv = RIGHT('0000000' + CAST(correlativo AS VARCHAR(10)), 7) 
                            FROM tabla 
                            WHERE tabla = '0003';

                            IF @codProv IS NULL OR @codProv = ''
                                SET @codProv = RIGHT('0000000' + CAST(NEXT VALUE FOR proveedor_seq AS VARCHAR(10)), 7);

                            DECLARE @newId INT = NEXT VALUE FOR proveedor_seq;

                            INSERT INTO proveedor (
                                proveedor_id, empresa_id, proveedor, tipo_nat_juri_id, razon_social, abrev,
                                apellido_pat, apellido_mat, nombres, tipo_doc_iden_id, num_doc_iden, numero_ruc,
                                numero_cci, contacto, tipo_via_id, nombre_via, numero_via, dpto, interior,
                                manzana, lote, kilometro, block, etapa, tipo_zona_id, nombre_zona, referencia,
                                telefono, distrito_id, email, web, tipo_prove_id, cond_pago_id, origen_registro_id,
                                es_local, es_prospecto, num_doc_iden2, cargo, cod_externo, estado,
                                usuario_creacion_id, fecha_creacion
                            )
                            VALUES (
                                @newId, 1, @codProv, 1, @RazonSocial, @Abrev,
                                @ApellidoPat, @ApellidoMat, @Nombres, @TipoDocId, @NumDoc, '',
                                '', '', @TipoViaId, @NombreVia, @NumeroVia, @Dpto, @Interior,
                                @Manzana, @Lote, @Kilometro, @Block, @Etapa, @TipoZonaId, @NombreZona, @Referencia,
                                @Telefono, @DistritoId, @Email, '', 1, 1, 2,
                                'S', 'N', '', '', '', 'A',
                                @UserId, GETDATE()
                            );

                            SELECT @newId;";

                        proveedorId = await conn.ExecuteScalarAsync<int>(sqlProv, new {
                            TipoDocId = tipoDocId,
                            NumDoc = numDoc,
                            RazonSocial = razonSocial,
                            Abrev = razonSocial.Length > 50 ? razonSocial.Substring(0, 50) : razonSocial,
                            ApellidoPat = apePat,
                            ApellidoMat = apeMat,
                            Nombres = nombres,
                            TipoViaId = model.TipoViaId > 0 ? model.TipoViaId : 1,
                            NombreVia = model.NombreVia ?? "",
                            NumeroVia = model.NumeroVia ?? "",
                            Dpto = model.Departamento ?? "",
                            Interior = model.Interior ?? "",
                            Manzana = model.Manzana ?? "",
                            Lote = model.Lote ?? "",
                            Kilometro = model.Kilometro ?? "",
                            Block = model.Block ?? "",
                            Etapa = model.Etapa ?? "",
                            TipoZonaId = model.TipoZonaId > 0 ? model.TipoZonaId : 1,
                            NombreZona = model.NombreZona ?? "",
                            Referencia = model.Referencia ?? "",
                            DistritoId = model.DistritoId > 0 ? model.DistritoId : 1,
                            Telefono = model.Telefono ?? "",
                            Email = model.Email ?? "",
                            UserId = userId
                        }, trans);
                    }
                }
                else
                {
                    // Actualizar proveedor existente
                    await conn.ExecuteAsync(@"
                        UPDATE proveedor 
                        SET razon_social = @RazonSocial,
                            apellido_pat = @ApellidoPat,
                            apellido_mat = @ApellidoMat,
                            nombres = @Nombres,
                            tipo_doc_iden_id = @TipoDocId,
                            num_doc_iden = @NumDoc,
                            tipo_via_id = @TipoViaId,
                            nombre_via = @NombreVia,
                            numero_via = @NumeroVia,
                            dpto = @Dpto,
                            interior = @Interior,
                            manzana = @Manzana,
                            lote = @Lote,
                            kilometro = @Kilometro,
                            block = @Block,
                            etapa = @Etapa,
                            tipo_zona_id = @TipoZonaId,
                            nombre_zona = @NombreZona,
                            referencia = @Referencia,
                            distrito_id = @DistritoId,
                            telefono = @Telefono,
                            email = @Email,
                            usuario_modifica_id = @UserId,
                            fecha_modifica = GETDATE()
                        WHERE proveedor_id = @ProveedorId",
                        new {
                            ProveedorId = proveedorId,
                            RazonSocial = razonSocial,
                            ApellidoPat = apePat,
                            ApellidoMat = apeMat,
                            Nombres = nombres,
                            TipoDocId = tipoDocId,
                            NumDoc = numDoc,
                            TipoViaId = model.TipoViaId > 0 ? model.TipoViaId : 1,
                            NombreVia = model.NombreVia ?? "",
                            NumeroVia = model.NumeroVia ?? "",
                            Dpto = model.Departamento ?? "",
                            Interior = model.Interior ?? "",
                            Manzana = model.Manzana ?? "",
                            Lote = model.Lote ?? "",
                            Kilometro = model.Kilometro ?? "",
                            Block = model.Block ?? "",
                            Etapa = model.Etapa ?? "",
                            TipoZonaId = model.TipoZonaId > 0 ? model.TipoZonaId : 1,
                            NombreZona = model.NombreZona ?? "",
                            Referencia = model.Referencia ?? "",
                            DistritoId = model.DistritoId > 0 ? model.DistritoId : 1,
                            Telefono = model.Telefono ?? "",
                            Email = model.Email ?? "",
                            UserId = userId
                        }, trans);
                }

                // 2. GESTIÓN DEL REGISTRO EN PERSONAL
                int personalId = model.Id;

                if (personalId <= 0)
                {
                    // Generar nuevo código de personal de 5 caracteres
                    var maxCodStr = await conn.QueryFirstOrDefaultAsync<string>(
                        "SELECT MAX(personal) FROM personal WHERE ISNUMERIC(personal) = 1", null, trans);
                    int maxCod = int.TryParse(maxCodStr, out int cVal) ? cVal : 0;
                    string nuevoCodigo = (maxCod + 1).ToString("D5");

                    var p = new DynamicParameters();
                    p.Add("@personal", nuevoCodigo);
                    p.Add("@empresaSedeId", model.EmpresaSedeId > 0 ? model.EmpresaSedeId : 1);
                    p.Add("@paisEmisorId", model.PaisEmisorId);
                    p.Add("@numeroFotocheck", string.IsNullOrWhiteSpace(model.NumFotocheck) ? nuevoCodigo : model.NumFotocheck.Trim());
                    p.Add("@proveedorId", proveedorId);
                    p.Add("@sexoId", model.SexoId > 0 ? model.SexoId : 1);
                    p.Add("@fechaNacimiento", model.FechaNacimiento);
                    p.Add("@paisId", model.PaisId > 0 ? model.PaisId : 1);
                    p.Add("@tipoViaId", model.TipoViaId2 ?? model.TipoViaId);
                    p.Add("@nombreVia", model.NombreVia2 ?? model.NombreVia ?? "");
                    p.Add("@numeroVia", model.NumeroVia2 ?? model.NumeroVia ?? "");
                    p.Add("@departamento", model.Departamento2 ?? model.Departamento ?? "");
                    p.Add("@interior", model.Interior2 ?? model.Interior ?? "");
                    p.Add("@manzana", model.Manzana2 ?? model.Manzana ?? "");
                    p.Add("@lote", model.Lote2 ?? model.Lote ?? "");
                    p.Add("@kilometro", model.Kilometro2 ?? model.Kilometro ?? "");
                    p.Add("@block", model.Block2 ?? model.Block ?? "");
                    p.Add("@etapa", model.Etapa2 ?? model.Etapa ?? "");
                    p.Add("@nombreZona", model.NombreZona2 ?? model.NombreZona ?? "");
                    p.Add("@tipoZonaId", model.TipoZonaId2 ?? model.TipoZonaId);
                    p.Add("@referencia", model.Referencia2 ?? model.Referencia ?? "");
                    p.Add("@distritoId", model.DistritoId2 ?? model.DistritoId);
                    p.Add("@esCentroAsistencial", model.EsCentroAsistencial ?? "N");
                    p.Add("@domiciliado", model.Domiciliado ?? "S");
                    p.Add("@tipoTrabajadorId", model.TipoTrabajadorId > 0 ? model.TipoTrabajadorId : 1);
                    p.Add("@categoriaOcupacionalId", model.CategoriaOcupacionalId);
                    p.Add("@regimenLaboralId", model.RegimenLaboralId > 0 ? model.RegimenLaboralId : 1);
                    p.Add("@situacionEducativaId", model.SituacionEducativaId);
                    p.Add("@cargoPersonalId", model.CargoPersonalId > 0 ? model.CargoPersonalId : 1);
                    p.Add("@ocupacionId", model.OcupacionId > 0 ? model.OcupacionId : 1);
                    p.Add("@regimenSaludId", model.RegimenSaludId > 0 ? model.RegimenSaludId : 1);
                    p.Add("@epsId", model.EpsId);
                    p.Add("@regimenPensionarioId", model.RegimenPensionarioId > 0 ? model.RegimenPensionarioId : 1);
                    p.Add("@regimenPensionarioEsquemaId", model.RegimenPensionarioEsquemaId);
                    p.Add("@fechaInscripcionRegimenPensionario", model.FechaInscripcionRegimenPensionario);
                    p.Add("@numeroCuspp", (model.NumeroCuspp ?? "").Trim());
                    p.Add("@aportaSctr", model.AportaSctr ?? "N");
                    p.Add("@sctrPensionId", model.SctrPensionId);
                    p.Add("@sctrSaludId", model.SctrSaludId);
                    p.Add("@tipoModalidadFormativaId", model.TipoModalidadFormativaId);
                    p.Add("@esDiscapacitado", model.EsDiscapacitado ?? "N");
                    p.Add("@tipoContratoId", model.TipoContratoId > 0 ? model.TipoContratoId : 1);
                    p.Add("@situacionEspecialTrabajadorId", model.SituacionEspecialTrabajadorId > 0 ? model.SituacionEspecialTrabajadorId : 1);
                    p.Add("@perioricidadRemunerativaId", model.PeriodicidadRemunerativaId > 0 ? model.PeriodicidadRemunerativaId : 1);
                    p.Add("@estadoCivilId", model.EstadoCivilId > 0 ? model.EstadoCivilId : 1);
                    p.Add("@tipoPagoRemuneracionId", model.TipoPagoRemuneracionId > 0 ? model.TipoPagoRemuneracionId : 1);
                    p.Add("@bancoId", model.BancoId);
                    p.Add("@numeroCuentaAbono", (model.NumeroCuentaAbono ?? "").Trim());
                    p.Add("@bancoCTSId", model.BancoCTSId);
                    p.Add("@numeroCuentaCTS", (model.NumeroCuentaCTS ?? "").Trim());
                    p.Add("@esRegimenAtipico", model.EsRegimenAtipico ?? "N");
                    p.Add("@esJornadaTrabajoMaxima", model.EsJornadaTrabajoMaxima ?? "S");
                    p.Add("@esJornadaTrabajoNocturna", model.EsJornadaTrabajoNocturna ?? "N");
                    p.Add("@esMiembroSindicalizado", model.EsMiembroSindicalizado ?? "N");
                    p.Add("@esExonerado5ta", model.EsExonerado5ta ?? "N");
                    p.Add("@asignacionFamiliar", model.AsignacionFamiliar ?? "N");
                    p.Add("@centroCostoId", model.CentroCostoId);
                    p.Add("@fechaIngreso", model.FechaIngreso);
                    p.Add("@fechaAlta", model.FechaAlta ?? model.FechaIngreso);
                    p.Add("@convenioDobleTributacionId", model.ConvenioDobleTributacionId > 0 ? model.ConvenioDobleTributacionId : 1);
                    p.Add("@situacionTrabajadorId", 1);
                    p.Add("@esEventual", model.EsEventual ?? "N");
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync("sp_personal_insertar", p, trans, commandType: CommandType.StoredProcedure);
                    personalId = p.Get<int>("@newID");

                    // Insertar primer periodo laboral en perso_per_lab con SP oficial
                    var pLab = new DynamicParameters();
                    pLab.Add("@personalId", personalId);
                    pLab.Add("@fechaIngreso", model.FechaIngreso);
                    pLab.Add("@fechaAlta", model.FechaAlta ?? model.FechaIngreso);
                    pLab.Add("@usuarioAccionId", userId);
                    pLab.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_perso_per_lab_insertar", pLab, trans, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    // Actualizar personal con stored procedure existente
                    var p = new DynamicParameters();
                    p.Add("@personalId", personalId);
                    p.Add("@paisEmisorId", model.PaisEmisorId);
                    p.Add("@numeroFotocheck", (model.NumFotocheck ?? "").Trim());
                    p.Add("@sexoId", model.SexoId > 0 ? model.SexoId : 1);
                    p.Add("@fechaNacimiento", model.FechaNacimiento);
                    p.Add("@paisId", model.PaisId > 0 ? model.PaisId : 1);
                    p.Add("@tipoViaId", model.TipoViaId2 ?? model.TipoViaId);
                    p.Add("@nombreVia", model.NombreVia2 ?? model.NombreVia ?? "");
                    p.Add("@numeroVia", model.NumeroVia2 ?? model.NumeroVia ?? "");
                    p.Add("@departamento", model.Departamento2 ?? model.Departamento ?? "");
                    p.Add("@interior", model.Interior2 ?? model.Interior ?? "");
                    p.Add("@manzana", model.Manzana2 ?? model.Manzana ?? "");
                    p.Add("@lote", model.Lote2 ?? model.Lote ?? "");
                    p.Add("@kilometro", model.Kilometro2 ?? model.Kilometro ?? "");
                    p.Add("@block", model.Block2 ?? model.Block ?? "");
                    p.Add("@etapa", model.Etapa2 ?? model.Etapa ?? "");
                    p.Add("@nombreZona", model.NombreZona2 ?? model.NombreZona ?? "");
                    p.Add("@tipoZonaId", model.TipoZonaId2 ?? model.TipoZonaId);
                    p.Add("@referencia", model.Referencia2 ?? model.Referencia ?? "");
                    p.Add("@distritoId", model.DistritoId2 ?? model.DistritoId);
                    p.Add("@esCentroAsistencial", model.EsCentroAsistencial ?? "N");
                    p.Add("@domiciliado", model.Domiciliado ?? "S");
                    p.Add("@tipoTrabajadorId", model.TipoTrabajadorId > 0 ? model.TipoTrabajadorId : 1);
                    p.Add("@categoriaOcupacionalId", model.CategoriaOcupacionalId);
                    p.Add("@regimenLaboralId", model.RegimenLaboralId > 0 ? model.RegimenLaboralId : 1);
                    p.Add("@situacionEducativaId", model.SituacionEducativaId);
                    p.Add("@cargoPersonalId", model.CargoPersonalId > 0 ? model.CargoPersonalId : 1);
                    p.Add("@ocupacionId", model.OcupacionId > 0 ? model.OcupacionId : 1);
                    p.Add("@regimenSaludId", model.RegimenSaludId > 0 ? model.RegimenSaludId : 1);
                    p.Add("@epsId", model.EpsId);
                    p.Add("@regimenPensionarioId", model.RegimenPensionarioId > 0 ? model.RegimenPensionarioId : 1);
                    p.Add("@regimenPensionarioEsquemaId", model.RegimenPensionarioEsquemaId);
                    p.Add("@fechaInscripcionRegimenPensionario", model.FechaInscripcionRegimenPensionario);
                    p.Add("@numeroCuspp", (model.NumeroCuspp ?? "").Trim());
                    p.Add("@aportaSctr", model.AportaSctr ?? "N");
                    p.Add("@sctrPensionId", model.SctrPensionId);
                    p.Add("@sctrSaludId", model.SctrSaludId);
                    p.Add("@tipoModalidadFormativaId", model.TipoModalidadFormativaId);
                    p.Add("@esDiscapacitado", model.EsDiscapacitado ?? "N");
                    p.Add("@tipoContratoId", model.TipoContratoId > 0 ? model.TipoContratoId : 1);
                    p.Add("@situacionEspecialTrabajadorId", model.SituacionEspecialTrabajadorId > 0 ? model.SituacionEspecialTrabajadorId : 1);
                    p.Add("@perioricidadRemunerativaId", model.PeriodicidadRemunerativaId > 0 ? model.PeriodicidadRemunerativaId : 1);
                    p.Add("@estadoCivilId", model.EstadoCivilId > 0 ? model.EstadoCivilId : 1);
                    p.Add("@tipoPagoRemuneracionId", model.TipoPagoRemuneracionId > 0 ? model.TipoPagoRemuneracionId : 1);
                    p.Add("@bancoId", model.BancoId);
                    p.Add("@numeroCuentaAbono", (model.NumeroCuentaAbono ?? "").Trim());
                    p.Add("@bancoCTSId", model.BancoCTSId);
                    p.Add("@numeroCuentaCTS", (model.NumeroCuentaCTS ?? "").Trim());
                    p.Add("@esRegimenAtipico", model.EsRegimenAtipico ?? "N");
                    p.Add("@esJornadaTrabajoMaxima", model.EsJornadaTrabajoMaxima ?? "S");
                    p.Add("@esJornadaTrabajoNocturna", model.EsJornadaTrabajoNocturna ?? "N");
                    p.Add("@esMiembroSindicalizado", model.EsMiembroSindicalizado ?? "N");
                    p.Add("@esExonerado5ta", model.EsExonerado5ta ?? "N");
                    p.Add("@asignacionFamiliar", model.AsignacionFamiliar ?? "N");
                    p.Add("@centroCostoId", model.CentroCostoId);
                    p.Add("@convenioDobleTributacionId", model.ConvenioDobleTributacionId > 0 ? model.ConvenioDobleTributacionId : 1);
                    p.Add("@esEventual", model.EsEventual ?? "N");
                    p.Add("@usuarioAccionId", userId);

                    await conn.ExecuteAsync("sp_personal_actualizar", p, trans, commandType: CommandType.StoredProcedure);
                }

                trans.Commit();
                return (true, $"Trabajador '{razonSocial}' guardado exitosamente.", personalId);
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return (false, "Error al guardar el personal: " + ex.Message, 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoPersonalAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var item = await conn.QueryFirstOrDefaultAsync<PersonalModel>(
                "SELECT personal_id AS Id, estado AS Estado, personal AS Codigo FROM personal WHERE personal_id = @Id", 
                new { Id = id });

            if (item == null) return (false, "Personal no encontrado.", "I");

            string nuevoEstado = item.Estado == "A" ? "I" : "A";
            const string sql = "UPDATE personal SET estado = @NuevoEstado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE personal_id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, NuevoEstado = nuevoEstado, UserId = userId });

            return (true, $"Personal {(nuevoEstado == "A" ? "activado" : "desactivado")} exitosamente.", nuevoEstado);
        }

        public async Task<(bool success, string message)> RegistrarCesePersonalAsync(int id, DateTime fechaCese, int? motivoId, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Actualizar en personal
                await conn.ExecuteAsync(@"
                    UPDATE personal 
                    SET fecha_cese = @FechaCese,
                        motivo_fin_peri_id = @MotivoId,
                        situa_trab_id = 2, -- Cesado / Inactivo laboralmente
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE personal_id = @Id",
                    new { Id = id, FechaCese = fechaCese.Date, MotivoId = motivoId, UserId = userId }, trans);

                // Actualizar en el último registro de perso_per_lab
                await conn.ExecuteAsync(@"
                    UPDATE perso_per_lab
                    SET fecha_cese = @FechaCese,
                        motivo_fin_peri_id = @MotivoId,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE perso_per_lab_id = (
                        SELECT TOP 1 perso_per_lab_id 
                        FROM perso_per_lab 
                        WHERE personal_id = @Id 
                        ORDER BY perso_per_lab_id DESC
                    )", new { Id = id, FechaCese = fechaCese.Date, MotivoId = motivoId, UserId = userId }, trans);

                trans.Commit();
                return (true, "Cese registrado exitosamente.");
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return (false, "Error al registrar el cese: " + ex.Message);
            }
        }

        // =========================================================================
        // 2. PERIODOS LABORALES
        // =========================================================================
        public async Task<List<PersonalPeriodoLaboralModel>> GetPeriodosLaboralesAsync(int personalId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pl.perso_per_lab_id AS Id,
                    pl.personal_id AS PersonalId,
                    pl.fecha_ing AS FechaInicio,
                    pl.fecha_cese AS FechaFin,
                    pl.motivo_fin_peri_id AS MotivoFinPeriodoId,
                    ISNULL(mfp.nombre, '') AS MotivoFinPeriodoNombre,
                    pl.estado AS Estado
                FROM perso_per_lab pl
                LEFT JOIN motivo_fin_peri mfp ON mfp.motivo_fin_peri_id = pl.motivo_fin_peri_id
                WHERE pl.personal_id = @PersonalId
                ORDER BY pl.fecha_ing DESC";

            var rows = await conn.QueryAsync<PersonalPeriodoLaboralModel>(sql, new { PersonalId = personalId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePeriodoLaboralAsync(PersonalPeriodoLaboralModel model, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id <= 0)
            {
                var p = new DynamicParameters();
                p.Add("@personalId", model.PersonalId);
                p.Add("@fechaIngreso", model.FechaInicio.Date);
                p.Add("@fechaAlta", model.FechaInicio.Date);
                p.Add("@usuarioAccionId", userId);
                p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("sp_perso_per_lab_insertar", p, commandType: CommandType.StoredProcedure);
                int id = p.Get<int>("@newID");
                if (model.FechaFin.HasValue || model.MotivoFinPeriodoId > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE perso_per_lab SET fecha_cese = @FechaFin, motivo_fin_peri_id = @MotivoId WHERE perso_per_lab_id = @Id",
                        new { Id = id, FechaFin = model.FechaFin?.Date, MotivoId = model.MotivoFinPeriodoId });
                }
                return (true, "Periodo laboral registrado.", id);
            }
            else
            {
                const string sql = @"
                    UPDATE perso_per_lab
                    SET fecha_ing = @FechaInicio,
                        fecha_cese = @FechaFin,
                        motivo_fin_peri_id = @MotivoId,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE perso_per_lab_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    FechaInicio = model.FechaInicio.Date,
                    FechaFin = model.FechaFin?.Date,
                    MotivoId = model.MotivoFinPeriodoId,
                    UserId = userId
                });
                return (true, "Periodo laboral actualizado.", model.Id);
            }
        }

        // =========================================================================
        // 3. REMUNERACIONES ASIGNADAS
        // =========================================================================
        public async Task<List<PersonalRemuneracionModel>> GetPersonalRemuneracionesAsync(int personalId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pr.personal_rem_id AS Id,
                    pr.personal_id AS PersonalId,
                    pr.tipo_remunera_id AS TipoRemuneracionId,
                    ISNULL(tr.nombre, '') AS TipoRemuneracionNombre,
                    pr.moneda_id AS MonedaId,
                    ISNULL(m.simbolo, 'S/') AS MonedaSimbolo,
                    pr.monto AS Monto,
                    pr.estado AS Estado
                FROM personal_rem pr
                LEFT JOIN tipo_remunera tr ON tr.tipo_remunera_id = pr.tipo_remunera_id
                LEFT JOIN moneda m ON m.moneda_id = pr.moneda_id
                WHERE pr.personal_id = @PersonalId AND pr.estado = 'A'
                ORDER BY tr.nombre ASC";

            var rows = await conn.QueryAsync<PersonalRemuneracionModel>(sql, new { PersonalId = personalId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePersonalRemuneracionAsync(PersonalRemuneracionModel model, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id <= 0)
            {
                var p = new DynamicParameters();
                p.Add("@personalId", model.PersonalId);
                p.Add("@tipoRemuneracionId", model.TipoRemuneracionId);
                p.Add("@monedaId", model.MonedaId > 0 ? model.MonedaId : 1);
                p.Add("@monto", model.Monto);
                p.Add("@fechaAlta", DateTime.Now);
                p.Add("@fechaBaja", null);
                p.Add("@usuarioAccionId", userId);
                p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("sp_personal_rem_insertar", p, commandType: CommandType.StoredProcedure);
                return (true, "Concepto remunerativo asignado exitosamente.", p.Get<int>("@newID"));
            }
            else
            {
                const string sql = @"
                    UPDATE personal_rem
                    SET tipo_remunera_id = @TipoRemuneraId,
                        moneda_id = @MonedaId,
                        monto = @Monto,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE personal_rem_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    TipoRemuneraId = model.TipoRemuneracionId,
                    MonedaId = model.MonedaId > 0 ? model.MonedaId : 1,
                    Monto = model.Monto,
                    UserId = userId
                });
                return (true, "Remuneración actualizada.", model.Id);
            }
        }

        public async Task<bool> DeletePersonalRemuneracionAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "UPDATE personal_rem SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE personal_rem_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }

        // =========================================================================
        // 4. DERECHOHABIENTES
        // =========================================================================
        public async Task<List<PersonalDependienteModel>> GetPersonalDependientesAsync(int personalId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pd.personal_depen_id AS Id,
                    pd.personal_id AS PersonalId,
                    ISNULL(pd.apellido_pat, '') AS ApellidoPaterno,
                    ISNULL(pd.apellido_mat, '') AS ApellidoMaterno,
                    ISNULL(pd.nombres, '') AS Nombres,
                    pd.tipo_doc_iden_id AS TipoDocIdentidadId,
                    ISNULL(tdi.abrev, 'DNI') AS TipoDocIdentidadNombre,
                    ISNULL(pd.num_doc_iden, '') AS NumDocIdentidad,
                    pd.pais_emisor_id AS PaisEmisorId,
                    ISNULL(pe.nombre, '') AS PaisEmisorNombre,
                    pd.fecha_nac AS FechaNacimiento,
                    pd.sexo_id AS SexoId,
                    ISNULL(s.nombre, 'M') AS SexoNombre,
                    pd.vincu_fami_id AS VinculoFamiliarId,
                    ISNULL(vf.nombre, '') AS VinculoFamiliarNombre,
                    pd.fecha_baja AS FechaBaja,
                    pd.motivo_b_depen_id AS MotivoBajaId,
                    pd.estado AS Estado
                FROM personal_depen pd
                LEFT JOIN tipo_doc_iden tdi ON tdi.tipo_doc_iden_id = pd.tipo_doc_iden_id
                LEFT JOIN pais_emisor pe ON pe.pais_emisor_id = pd.pais_emisor_id
                LEFT JOIN sexo s ON s.sexo_id = pd.sexo_id
                LEFT JOIN vincu_fami vf ON vf.vincu_fami_id = pd.vincu_fami_id
                WHERE pd.personal_id = @PersonalId AND pd.estado = 'A'
                ORDER BY pd.apellido_pat, pd.apellido_mat, pd.nombres";

            var rows = await conn.QueryAsync<PersonalDependienteModel>(sql, new { PersonalId = personalId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePersonalDependienteAsync(PersonalDependienteModel model, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (model.Id <= 0)
            {
                var p = new DynamicParameters();
                p.Add("@personalId", model.PersonalId);
                p.Add("@tipoDocumentoIdentidadId", model.TipoDocIdentidadId > 0 ? model.TipoDocIdentidadId : 1);
                p.Add("@numeroDocumentoIdentidad", (model.NumDocIdentidad ?? "").Trim());
                p.Add("@paisEmisorId", model.PaisEmisorId);
                p.Add("@apellidoPaterno", (model.ApellidoPaterno ?? "").Trim().ToUpper());
                p.Add("@apellidoMaterno", (model.ApellidoMaterno ?? "").Trim().ToUpper());
                p.Add("@nombres", (model.Nombres ?? "").Trim().ToUpper());
                p.Add("@fechaNacimiento", model.FechaNacimiento);
                p.Add("@sexoId", model.SexoId > 0 ? model.SexoId : 1);
                p.Add("@vinculoFamiliarId", model.VinculoFamiliarId > 0 ? model.VinculoFamiliarId : 1);
                p.Add("@tipoDocumentoVinculoFamiliarId", 1);
                p.Add("@numeroDocumentoVinculoFamiliar", "");
                p.Add("@fechaConcepcion", null);
                p.Add("@tipoViaId", 1);
                p.Add("@nombreVia", "");
                p.Add("@numeroVia", "");
                p.Add("@departamento", "");
                p.Add("@interior", "");
                p.Add("@manzana", "");
                p.Add("@lote", "");
                p.Add("@kilometro", "");
                p.Add("@block", "");
                p.Add("@etapa", "");
                p.Add("@tipoZonaId", 1);
                p.Add("@nombreZona", "");
                p.Add("@referencia", "");
                p.Add("@distritoId", 1);
                p.Add("@telefono", "");
                p.Add("@email", "");
                p.Add("@fechaAlta", DateTime.Now);
                p.Add("@usuarioAccionId", userId);
                p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("sp_personal_depen_insertar", p, commandType: CommandType.StoredProcedure);
                return (true, "Derechohabiente registrado exitosamente.", p.Get<int>("@newID"));
            }
            else
            {
                const string sql = @"
                    UPDATE personal_depen
                    SET tipo_doc_iden_id = @TipoDocId,
                        num_doc_iden = @NumDoc,
                        pais_emisor_id = @PaisEmisorId,
                        apellido_pat = @ApePat,
                        apellido_mat = @ApeMat,
                        nombres = @Nombres,
                        fecha_nac = @FechaNac,
                        sexo_id = @SexoId,
                        vincu_fami_id = @VinculoId,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE personal_depen_id = @Id";
                await conn.ExecuteAsync(sql, new {
                    Id = model.Id,
                    TipoDocId = model.TipoDocIdentidadId > 0 ? model.TipoDocIdentidadId : 1,
                    NumDoc = (model.NumDocIdentidad ?? "").Trim(),
                    PaisEmisorId = model.PaisEmisorId,
                    ApePat = (model.ApellidoPaterno ?? "").Trim().ToUpper(),
                    ApeMat = (model.ApellidoMaterno ?? "").Trim().ToUpper(),
                    Nombres = (model.Nombres ?? "").Trim().ToUpper(),
                    FechaNac = model.FechaNacimiento,
                    SexoId = model.SexoId > 0 ? model.SexoId : 1,
                    VinculoId = model.VinculoFamiliarId > 0 ? model.VinculoFamiliarId : 1,
                    UserId = userId
                });
                return (true, "Derechohabiente actualizado.", model.Id);
            }
        }

        public async Task<bool> DeletePersonalDependienteAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "UPDATE personal_depen SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE personal_depen_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }

        // =========================================================================
        // 5. PLANILLAS ASIGNADAS
        // =========================================================================
        public async Task<List<PersonalPlanillaModel>> GetPersonalPlanillasAsync(int personalId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pp.perso_plan_id AS Id,
                    pp.personal_id AS PersonalId,
                    pp.planilla_id AS PlanillaId,
                    ISNULL(p.nombre, '') AS PlanillaNombre,
                    pp.estado AS Estado
                FROM perso_plan pp
                LEFT JOIN planilla p ON p.planilla_id = pp.planilla_id
                WHERE pp.personal_id = @PersonalId AND pp.estado = 'A'
                ORDER BY p.nombre ASC";

            var rows = await conn.QueryAsync<PersonalPlanillaModel>(sql, new { PersonalId = personalId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePersonalPlanillaAsync(PersonalPlanillaModel model, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            // Verificar si ya está asignado
            var existe = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT perso_plan_id FROM perso_plan WHERE personal_id = @PersonalId AND planilla_id = @PlanillaId AND estado = 'A'",
                new { model.PersonalId, model.PlanillaId });

            if (existe.HasValue)
                return (false, "El trabajador ya se encuentra asignado a esta planilla.", existe.Value);

            var p = new DynamicParameters();
            p.Add("@personalId", model.PersonalId);
            p.Add("@planillaId", model.PlanillaId);
            p.Add("@usuarioAccionId", userId);
            p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_perso_plan_insertar", p, commandType: CommandType.StoredProcedure);
            return (true, "Asignación a planilla registrada exitosamente.", p.Get<int>("@newID"));
        }

        public async Task<bool> DeletePersonalPlanillaAsync(int id, int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "UPDATE perso_plan SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE perso_plan_id = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }

        // =========================================================================
        // 6. CATÁLOGOS Y LOOKUPS
        // =========================================================================
        public async Task<List<LookupItemModel>> GetTiposDocIdentidadLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_doc_iden_id AS Id, abrev AS Codigo, nombre AS Nombre FROM tipo_doc_iden WHERE estado = 'A' ORDER BY tipo_doc_iden_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetSexosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT sexo_id AS Id, sexo AS Codigo, nombre AS Nombre FROM sexo WHERE estado = 'A' ORDER BY sexo_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEstadosCivilesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT estado_civil_id AS Id, estado_civil AS Codigo, nombre AS Nombre FROM estado_civil WHERE estado = 'A' ORDER BY estado_civil_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetPaisesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT pais_id AS Id, '' AS Codigo, nombre AS Nombre FROM pais WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetPaisesEmisoresLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT pais_emisor_id AS Id, RTRIM(pais_emisor) AS Codigo, nombre AS Nombre FROM pais_emisor WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetDepartamentosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT departamento_id AS Id, departamento AS Codigo, nombre AS Nombre FROM departamento ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetProvinciasLookupAsync(int? departamentoId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = "SELECT provincia_id AS Id, provincia AS Codigo, nombre AS Nombre FROM provincia WHERE (@DepId IS NULL OR departamento_id = @DepId) ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { DepId = departamentoId > 0 ? departamentoId : null });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetDistritosLookupAsync(int? provinciaId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = "SELECT distrito_id AS Id, distrito AS Codigo, nombre AS Nombre FROM distrito WHERE (@ProvId IS NULL OR provincia_id = @ProvId) ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { ProvId = provinciaId > 0 ? provinciaId : null });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposViaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_via_id AS Id, abrev AS Codigo, nombre AS Nombre FROM tipo_via WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposZonaLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_zona_id AS Id, abrev AS Codigo, nombre AS Nombre FROM tipo_zona WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposTrabajadorLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_trabaja_id AS Id, tipo_trabaja AS Codigo, nombre AS Nombre FROM tipo_trabaja WHERE estado = 'A' ORDER BY tipo_trabaja_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetCategoriasOcupacionalesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT cate_ocupa_id AS Id, cate_ocupa AS Codigo, nombre AS Nombre FROM cate_ocupa WHERE estado = 'A' ORDER BY cate_ocupa_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetRegimenesLaboralesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT reg_laboral_id AS Id, reg_laboral AS Codigo, nombre AS Nombre FROM reg_laboral WHERE estado = 'A' ORDER BY reg_laboral_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetSituacionesEducativasLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT situa_educa_id AS Id, situa_educa AS Codigo, nombre AS Nombre FROM situa_educa WHERE estado = 'A' ORDER BY situa_educa_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetCargosPersonalLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT cargo_perso_id AS Id, cargo_perso AS Codigo, nombre AS Nombre FROM cargo_perso WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetOcupacionesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT ocupacion_id AS Id, ocupacion AS Codigo, nombre AS Nombre FROM ocupacion WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetCentrosCostoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT centro_costo_id AS Id, ISNULL(sigla, '') AS Codigo, nombre AS Nombre FROM centro_costo WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposContratoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_contrato_id AS Id, tipo_contrato AS Codigo, nombre AS Nombre FROM tipo_contrato WHERE estado = 'A' ORDER BY tipo_contrato_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetSituacionesEspecialesLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT situ_espe_trab_id AS Id, situ_espe_trab AS Codigo, nombre AS Nombre FROM situ_espe_trab WHERE estado = 'A' ORDER BY situ_espe_trab_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetPeriodicidadesRemuLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT peri_remu_id AS Id, peri_remu AS Codigo, nombre AS Nombre FROM peri_remu WHERE estado = 'A' ORDER BY peri_remu_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetRegimenesSaludLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT reg_salud_id AS Id, reg_salud AS Codigo, nombre AS Nombre FROM reg_salud WHERE estado = 'A' ORDER BY reg_salud_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEpsLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT eps_id AS Id, eps AS Codigo, nombre AS Nombre FROM eps WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetRegimenesPensionariosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT reg_pension_id AS Id, reg_pension AS Codigo, nombre AS Nombre FROM reg_pension WHERE estado = 'A' ORDER BY reg_pension_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEsquemasPensionariosLookupAsync(int? regimenPensionarioId = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = @"
                SELECT rpe.reg_pens_esq_id AS Id, '' AS Codigo, tep.nombre AS Nombre 
                FROM reg_pens_esq rpe
                INNER JOIN tipo_esque_pen tep ON tep.tipo_esque_pen_id = rpe.tipo_esque_pen_id
                WHERE rpe.estado = 'A' 
                  AND (@RegId IS NULL OR rpe.reg_pension_id = @RegId)
                ORDER BY rpe.reg_pens_esq_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { RegId = regimenPensionarioId > 0 ? regimenPensionarioId : null });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetSctrPensionLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT sctr_pension_id AS Id, sctr_pension AS Codigo, nombre AS Nombre FROM sctr_pension WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetSctrSaludLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT sctr_salud_id AS Id, sctr_salud AS Codigo, nombre AS Nombre FROM sctr_salud WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposPagoRemuLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_pago_remu_id AS Id, tipo_pago_remu AS Codigo, nombre AS Nombre FROM tipo_pago_remu WHERE estado = 'A' ORDER BY tipo_pago_remu_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetBancosLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT banco_id AS Id, banco AS Codigo, nombre AS Nombre FROM banco WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetConveniosDobleTribLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT conve_d_trib_id AS Id, conve_d_trib AS Codigo, nombre AS Nombre FROM conve_d_trib WHERE estado = 'A' ORDER BY conve_d_trib_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetMotivosFinPeriodoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT motivo_fin_peri_id AS Id, motivo_fin_peri AS Codigo, nombre AS Nombre FROM motivo_fin_peri WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetVinculosFamiliaresLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT vincu_fami_id AS Id, vincu_fami AS Codigo, nombre AS Nombre FROM vincu_fami WHERE estado = 'A' ORDER BY vincu_fami_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposRemuneracionLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_remunera_id AS Id, '' AS Codigo, nombre AS Nombre FROM tipo_remunera WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetPlanillasLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT planilla_id AS Id, '' AS Codigo, nombre AS Nombre FROM planilla WHERE estado = 'A' ORDER BY nombre ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetMonedasLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT moneda_id AS Id, simbolo AS Codigo, nombre AS Nombre FROM moneda WHERE estado = 'A' ORDER BY moneda_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetVariablesCalculoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT var_calc_id AS Id, 
                       var_calc AS Codigo, 
                       CONCAT(var_calc, ' - ', nombre) AS Nombre 
                FROM var_calc 
                WHERE estado = 'A' 
                ORDER BY var_calc ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        // =========================================================================
        // 7. CARGOS DE PERSONAL (cargo_perso)
        // =========================================================================
        public async Task<List<CargoPersonalModel>> GetCargosPersonalListAsync(string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    cp.cargo_perso_id AS Id,
                    cp.cargo_perso AS Codigo,
                    cp.nombre AS Nombre,
                    cp.estado AS Estado,
                    (SELECT COUNT(*) FROM personal p WHERE p.cargo_perso_id = cp.cargo_perso_id) AS PersonalCount
                FROM cargo_perso cp
                WHERE 1 = 1
                {(!string.IsNullOrWhiteSpace(search) ? "AND (cp.nombre LIKE @Search OR cp.cargo_perso LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND cp.estado = @Estado" : "")}
                ORDER BY cp.cargo_perso_id DESC";
            var rows = await conn.QueryAsync<CargoPersonalModel>(sql, new {
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<CargoPersonalModel?> GetCargoPersonalByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cp.cargo_perso_id AS Id,
                    cp.cargo_perso AS Codigo,
                    cp.nombre AS Nombre,
                    cp.estado AS Estado,
                    (SELECT COUNT(*) FROM personal p WHERE p.cargo_perso_id = cp.cargo_perso_id) AS PersonalCount
                FROM cargo_perso cp
                WHERE cp.cargo_perso_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<CargoPersonalModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveCargoPersonalAsync(CargoPersonalModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del cargo es obligatorio.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var nextCorr = await conn.QueryFirstOrDefaultAsync<int>(
                        "SELECT ISNULL(correlativo, 0) + 1 FROM tabla WHERE tabla = '0006'");
                    if (nextCorr <= 0) nextCorr = 1;
                    string codigo = nextCorr.ToString("D5");

                    var p = new DynamicParameters();
                    p.Add("@cargoPersonal", codigo);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_cargo_perso_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    await conn.ExecuteAsync("UPDATE tabla SET correlativo = @Corr WHERE tabla = '0006'", new { Corr = nextCorr });

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE cargo_perso SET estado = @Estado WHERE cargo_perso_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Cargo creado exitosamente.", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@cargoPersonalId", model.Id);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_cargo_perso_actualizar", p, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "UPDATE cargo_perso SET estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE cargo_perso_id = @Id",
                        new { Estado = estado, UserId = userId, Id = model.Id });

                    return (true, "Cargo actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar cargo: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoCargoPersonalAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM cargo_perso WHERE cargo_perso_id = @Id", new { Id = id });
                if (actual == null) return (false, "Cargo no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE cargo_perso SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE cargo_perso_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 8. RELOJ MARCADOR (reloj)
        // =========================================================================
        public async Task<List<RelojModel>> GetRelojesListAsync(int empresaId, string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    r.reloj_id AS Id,
                    r.empresa_id AS EmpresaId,
                    r.nombre AS Nombre,
                    r.ruta_archivo AS RutaArchivo,
                    r.es_central AS EsCentral,
                    r.estado AS Estado
                FROM reloj r
                WHERE r.empresa_id = @EmpresaId
                {(!string.IsNullOrWhiteSpace(search) ? "AND (r.nombre LIKE @Search OR r.ruta_archivo LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND r.estado = @Estado" : "")}
                ORDER BY r.reloj_id DESC";
            var rows = await conn.QueryAsync<RelojModel>(sql, new {
                EmpresaId = empresaId,
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<RelojModel?> GetRelojByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    r.reloj_id AS Id,
                    r.empresa_id AS EmpresaId,
                    r.nombre AS Nombre,
                    r.ruta_archivo AS RutaArchivo,
                    r.es_central AS EsCentral,
                    r.estado AS Estado
                FROM reloj r
                WHERE r.reloj_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<RelojModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveRelojAsync(RelojModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del reloj es obligatorio.", 0);

                if (string.IsNullOrWhiteSpace(model.RutaArchivo))
                    return (false, "La ruta del archivo de marcaciones es obligatoria.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var p = new DynamicParameters();
                    p.Add("@empresaId", model.EmpresaId <= 0 ? 1 : model.EmpresaId);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@rutaArchivo", model.RutaArchivo.Trim());
                    p.Add("@esCentral", string.IsNullOrWhiteSpace(model.EsCentral) ? "N" : model.EsCentral);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_reloj_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE reloj SET estado = @Estado WHERE reloj_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Reloj marcador registrado exitosamente.", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@relojId", model.Id);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@rutaArchivo", model.RutaArchivo.Trim());
                    p.Add("@esCentral", string.IsNullOrWhiteSpace(model.EsCentral) ? "N" : model.EsCentral);
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_reloj_actualizar", p, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "UPDATE reloj SET estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE reloj_id = @Id",
                        new { Estado = estado, UserId = userId, Id = model.Id });

                    return (true, "Reloj marcador actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar reloj: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRelojAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM reloj WHERE reloj_id = @Id", new { Id = id });
                if (actual == null) return (false, "Reloj no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE reloj SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE reloj_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 9. FERIADOS (feriado)
        // =========================================================================
        public async Task<List<FeriadoModel>> GetFeriadosListAsync(string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    f.feriado_id AS Id,
                    f.fecha AS Fecha,
                    f.estado AS Estado
                FROM feriado f
                WHERE 1 = 1
                {(!string.IsNullOrWhiteSpace(estado) ? "AND f.estado = @Estado" : "")}
                ORDER BY f.fecha DESC";
            var rows = await conn.QueryAsync<FeriadoModel>(sql, new { Estado = estado });
            return rows.ToList();
        }

        public async Task<FeriadoModel?> GetFeriadoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    f.feriado_id AS Id,
                    f.fecha AS Fecha,
                    f.estado AS Estado
                FROM feriado f
                WHERE f.feriado_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<FeriadoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveFeriadoAsync(FeriadoModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var existe = await conn.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM feriado WHERE CONVERT(date, fecha) = @Fecha",
                        new { Fecha = model.Fecha.Date });
                    if (existe > 0)
                        return (false, "Ya existe un feriado registrado para la fecha seleccionada.", 0);

                    var p = new DynamicParameters();
                    p.Add("@fecha", model.Fecha.Date);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_feriado_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE feriado SET estado = @Estado WHERE feriado_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Feriado registrado exitosamente.", newId);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE feriado 
                        SET fecha = @Fecha, 
                            estado = @Estado,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE feriado_id = @Id",
                        new { Id = model.Id, Fecha = model.Fecha.Date, Estado = estado, UserId = userId });
                    return (true, "Feriado actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar feriado: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoFeriadoAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM feriado WHERE feriado_id = @Id", new { Id = id });
                if (actual == null) return (false, "Feriado no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE feriado SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE feriado_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 10. RÉGIMEN PENSIONARIO (reg_pension)
        // =========================================================================
        public async Task<List<RegimenPensionarioModel>> GetRegimenesPensionariosCrudListAsync(string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    rp.reg_pension_id AS Id,
                    rp.reg_pension AS Codigo,
                    rp.nombre AS Nombre,
                    rp.estado AS Estado
                FROM reg_pension rp
                WHERE 1 = 1
                {(!string.IsNullOrWhiteSpace(search) ? "AND (rp.nombre LIKE @Search OR rp.reg_pension LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND rp.estado = @Estado" : "")}
                ORDER BY rp.reg_pension_id ASC";
            var rows = await conn.QueryAsync<RegimenPensionarioModel>(sql, new {
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<RegimenPensionarioModel?> GetRegimenPensionarioByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    rp.reg_pension_id AS Id,
                    rp.reg_pension AS Codigo,
                    rp.nombre AS Nombre,
                    rp.estado AS Estado
                FROM reg_pension rp
                WHERE rp.reg_pension_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<RegimenPensionarioModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveRegimenPensionarioAsync(RegimenPensionarioModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Codigo))
                    return (false, "El código del régimen pensionario es obligatorio.", 0);

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del régimen pensionario es obligatorio.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var p = new DynamicParameters();
                    p.Add("@regimenPensionario", model.Codigo.Trim().ToUpper());
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_reg_pension_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE reg_pension SET estado = @Estado WHERE reg_pension_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Régimen pensionario registrado exitosamente.", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@regimenPensionarioId", model.Id);
                    p.Add("@regimenPensionario", model.Codigo.Trim().ToUpper());
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_reg_pension_actualizar", p, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "UPDATE reg_pension SET estado = @Estado, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE reg_pension_id = @Id",
                        new { Estado = estado, UserId = userId, Id = model.Id });

                    return (true, "Régimen pensionario actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar régimen pensionario: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoRegimenPensionarioAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM reg_pension WHERE reg_pension_id = @Id", new { Id = id });
                if (actual == null) return (false, "Régimen no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE reg_pension SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE reg_pension_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 11. TIPOS DE REMUNERACIÓN (tipo_remunera)
        // =========================================================================
        public async Task<List<TipoRemuneraModel>> GetTiposRemuneraListAsync(string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    tr.tipo_remunera_id AS Id,
                    tr.nombre AS Nombre,
                    tr.var_calc_id AS VariableCalculoId,
                    ISNULL(vc.var_calc, '') AS VariableCalculoNombre,
                    tr.estado AS Estado
                FROM tipo_remunera tr
                LEFT JOIN var_calc vc ON vc.var_calc_id = tr.var_calc_id
                WHERE 1 = 1
                {(!string.IsNullOrWhiteSpace(search) ? "AND (tr.nombre LIKE @Search OR vc.var_calc LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND tr.estado = @Estado" : "")}
                ORDER BY tr.tipo_remunera_id ASC";
            var rows = await conn.QueryAsync<TipoRemuneraModel>(sql, new {
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<TipoRemuneraModel?> GetTipoRemuneraByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    tr.tipo_remunera_id AS Id,
                    tr.nombre AS Nombre,
                    tr.var_calc_id AS VariableCalculoId,
                    ISNULL(vc.var_calc, '') AS VariableCalculoNombre,
                    tr.estado AS Estado
                FROM tipo_remunera tr
                LEFT JOIN var_calc vc ON vc.var_calc_id = tr.var_calc_id
                WHERE tr.tipo_remunera_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<TipoRemuneraModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveTipoRemuneraAsync(TipoRemuneraModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del tipo de remuneración es obligatorio.", 0);

                if (model.VariableCalculoId <= 0)
                    return (false, "Debe seleccionar una variable de cálculo.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var p = new DynamicParameters();
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@variableCalculoId", model.VariableCalculoId);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_tipo_remunera_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE tipo_remunera SET estado = @Estado WHERE tipo_remunera_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Tipo de remuneración registrado exitosamente.", newId);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE tipo_remunera 
                        SET nombre = @Nombre, 
                            var_calc_id = @VariableCalculoId, 
                            estado = @Estado,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE tipo_remunera_id = @Id",
                        new {
                            Id = model.Id,
                            Nombre = model.Nombre.Trim().ToUpper(),
                            VariableCalculoId = model.VariableCalculoId,
                            Estado = estado,
                            UserId = userId
                        });
                    return (true, "Tipo de remuneración actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar tipo de remuneración: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoTipoRemuneraAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM tipo_remunera WHERE tipo_remunera_id = @Id", new { Id = id });
                if (actual == null) return (false, "Tipo de remuneración no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE tipo_remunera SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE tipo_remunera_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 12. HORAS REMUNERATIVAS (hora_rem)
        // =========================================================================
        public async Task<List<HoraRemunerativaModel>> GetHorasRemunerativasListAsync(int empresaId, string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    hr.hora_rem_id AS Id,
                    hr.empresa_id AS EmpresaId,
                    hr.hora_rem AS Codigo,
                    hr.nombre AS Nombre,
                    hr.abrev AS Abreviatura,
                    hr.var_calc_id AS VariableCalculoId,
                    ISNULL(vc.var_calc, '') AS VariableCalculoNombre,
                    hr.estado AS Estado
                FROM hora_rem hr
                LEFT JOIN var_calc vc ON vc.var_calc_id = hr.var_calc_id
                WHERE hr.empresa_id = @EmpresaId
                {(!string.IsNullOrWhiteSpace(search) ? "AND (hr.nombre LIKE @Search OR hr.abrev LIKE @Search OR hr.hora_rem LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND hr.estado = @Estado" : "")}
                ORDER BY hr.hora_rem_id ASC";
            var rows = await conn.QueryAsync<HoraRemunerativaModel>(sql, new {
                EmpresaId = empresaId,
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<HoraRemunerativaModel?> GetHoraRemunerativaByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    hr.hora_rem_id AS Id,
                    hr.empresa_id AS EmpresaId,
                    hr.hora_rem AS Codigo,
                    hr.nombre AS Nombre,
                    hr.abrev AS Abreviatura,
                    hr.var_calc_id AS VariableCalculoId,
                    ISNULL(vc.var_calc, '') AS VariableCalculoNombre,
                    hr.estado AS Estado
                FROM hora_rem hr
                LEFT JOIN var_calc vc ON vc.var_calc_id = hr.var_calc_id
                WHERE hr.hora_rem_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<HoraRemunerativaModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveHoraRemunerativaAsync(HoraRemunerativaModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del tipo de hora es obligatorio.", 0);

                if (string.IsNullOrWhiteSpace(model.Abreviatura))
                    return (false, "La abreviatura es obligatoria.", 0);

                if (model.VariableCalculoId <= 0)
                    return (false, "Debe seleccionar una variable de cálculo.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var nextCorr = await conn.QueryFirstOrDefaultAsync<int>(
                        "SELECT ISNULL(correlativo, 0) + 1 FROM tabla WHERE tabla = '0004'");
                    if (nextCorr <= 0) nextCorr = 1;
                    string codigo = nextCorr.ToString("D3");

                    var p = new DynamicParameters();
                    p.Add("@empresaId", model.EmpresaId <= 0 ? 1 : model.EmpresaId);
                    p.Add("@horaRemunerativa", codigo);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@abreviatura", model.Abreviatura.Trim().ToUpper());
                    p.Add("@variableCalculoId", model.VariableCalculoId);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_hora_rem_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    await conn.ExecuteAsync("UPDATE tabla SET correlativo = @Corr WHERE tabla = '0004'", new { Corr = nextCorr });

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE hora_rem SET estado = @Estado WHERE hora_rem_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Hora remunerativa registrada exitosamente.", newId);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE hora_rem 
                        SET nombre = @Nombre, 
                            abrev = @Abrev, 
                            var_calc_id = @VariableCalculoId, 
                            estado = @Estado,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE hora_rem_id = @Id",
                        new {
                            Id = model.Id,
                            Nombre = model.Nombre.Trim().ToUpper(),
                            Abrev = model.Abreviatura.Trim().ToUpper(),
                            VariableCalculoId = model.VariableCalculoId,
                            Estado = estado,
                            UserId = userId
                        });
                    return (true, "Hora remunerativa actualizada exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar hora remunerativa: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoHoraRemunerativaAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM hora_rem WHERE hora_rem_id = @Id", new { Id = id });
                if (actual == null) return (false, "Hora remunerativa no encontrada.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE hora_rem SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE hora_rem_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 13. PLANILLAS (planilla)
        // =========================================================================
        private const string PlanillaSelect = @"
            SELECT p.planilla_id AS Id, p.empresa_id AS EmpresaId, p.nombre AS Nombre,
                   p.proc_anual AS ProcesoAnual, p.tipo_trabaja_id AS TipoTrabajadorId,
                   tt.nombre AS TipoTrabajadorNombre, p.estado AS Estado
            FROM planilla p INNER JOIN tipo_trabaja tt ON tt.tipo_trabaja_id = p.tipo_trabaja_id ";

        public async Task<PlanillaPageModel> GetPlanillasPageAsync(
            int empresaId, string? search = null, string? estado = null, int page = 1,
            int pageSize = 25, string? sort = null, string? dir = null)
        {
            var columns = new Dictionary<string, string> {
                ["id"] = "p.planilla_id", ["nombre"] = "p.nombre", ["trabajador"] = "tt.nombre",
                ["procesos"] = "p.proc_anual", ["estado"] = "p.estado" };
            var model = new PlanillaPageModel();
            model.Ordenar(sort, dir, columns.Keys, "id");
            using var conn = _dbConnectionFactory.CreateConnection();
            const string where = @" WHERE p.empresa_id = @EmpresaId
                AND (@Search IS NULL OR p.nombre LIKE @Search OR tt.nombre LIKE @Search)
                AND (@Estado IS NULL OR p.estado = @Estado)";
            var args = new DynamicParameters(new { EmpresaId = empresaId,
                Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
                Estado = string.IsNullOrWhiteSpace(estado) ? null : estado });
            int total = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM planilla p INNER JOIN tipo_trabaja tt ON tt.tipo_trabaja_id=p.tipo_trabaja_id" + where, args);
            model.Paginar(total, page, pageSize);
            args.Add("Offset", (model.Page - 1) * model.PageSize);
            args.Add("PageSize", model.PageSize);
            model.Items = (await conn.QueryAsync<PlanillaModel>(PlanillaSelect + where +
                $" ORDER BY {columns[model.Sort]} {model.Dir}{(model.Sort == "id" ? "" : ", p.planilla_id ASC")} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                args)).ToList();
            model.VisibleCount = model.Items.Count;
            model.TiposTrabajador = await GetTiposTrabajadorLookupAsync();
            model.ConceptosDisponibles = (await conn.QueryAsync<LookupItemModel>(@"
                SELECT concep_remu_id AS Id, RTRIM(concep_remu) AS Codigo, nombre AS Nombre
                FROM concep_remu WHERE empresa_id = @EmpresaId AND estado = 'A' ORDER BY concep_remu",
                new { EmpresaId = empresaId })).ToList();
            return model;
        }

        public async Task<PlanillaModel?> GetPlanillaAdminByIdAsync(int id, int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PlanillaModel>(
                PlanillaSelect + " WHERE p.planilla_id=@Id AND p.empresa_id=@EmpresaId", new { Id = id, EmpresaId = empresaId });
        }

        public async Task<(bool success, string message, int id)> SavePlanillaAsync(PlanillaModel model, int userId)
        {
            if (model.Id < 0 || model.EmpresaId <= 0 || userId <= 0 ||
                string.IsNullOrWhiteSpace(model.Nombre) || model.Nombre.Trim().Length > 150)
                return (false, "Ingrese un nombre válido de hasta 150 caracteres y un contexto de acceso válido.", 0);
            if (model.TipoTrabajadorId <= 0 || model.ProcesoAnual <= 0)
                return (false, "Seleccione un tipo de trabajador y un número positivo de procesos anuales.", 0);
            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            if (model.Id > 0)
            {
                var actual = await conn.QuerySingleOrDefaultAsync<string>(@"
                    SELECT estado FROM planilla WITH (UPDLOCK,HOLDLOCK)
                    WHERE planilla_id=@Id AND empresa_id=@EmpresaId", new { model.Id, model.EmpresaId }, tx);
                if (actual is null) return (false, "Planilla no encontrada para la empresa seleccionada.", 0);
                if (actual != "A") return (false, "La planilla está anulada y no admite cambios.", 0);
            }
            int validType = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM tipo_trabaja WHERE tipo_trabaja_id=@Id AND estado='A'",
                new { Id = model.TipoTrabajadorId }, tx);
            if (validType != 1) return (false, "El tipo de trabajador no está habilitado.", 0);
            var parameters = new DynamicParameters(new {
                nombre = model.Nombre.Trim().ToUpperInvariant(), procesoAnual = model.ProcesoAnual,
                tipoTrabajadorId = model.TipoTrabajadorId, usuarioAccionId = userId });
            int id = model.Id;
            if (id == 0)
            {
                parameters.Add("empresaId", model.EmpresaId);
                parameters.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("dbo.sp_planilla_insertar", parameters, tx, commandType: CommandType.StoredProcedure);
                id = parameters.Get<int>("newID");
            }
            else
            {
                parameters.Add("planillaId", id);
                await conn.ExecuteAsync("dbo.sp_planilla_actualizar", parameters, tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
            return (true, "Planilla guardada correctamente.", id);
        }

        public async Task<(bool success, string message)> AnularPlanillaAsync(int id, int empresaId, int userId)
        {
            if (id <= 0 || empresaId <= 0 || userId <= 0) return (false, "La planilla indicada no es válida.");
            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            var state = await conn.QuerySingleOrDefaultAsync<string>(@"
                SELECT estado FROM planilla WITH (UPDLOCK,HOLDLOCK)
                WHERE planilla_id=@Id AND empresa_id=@EmpresaId", new { Id=id, EmpresaId=empresaId }, tx);
            if (state != "A") return (false, "La planilla no existe para esta empresa o ya está anulada.");
            await conn.ExecuteAsync("dbo.sp_planilla_eliminar",
                new { planillaId=id, usuarioAccionId=userId }, tx, commandType: CommandType.StoredProcedure);
            tx.Commit();
            return (true, "Planilla anulada.");
        }

        public async Task<List<PlanillaConceptoModel>?> GetPlanillaConceptosAsync(int planillaId, int empresaId)
        {
            if (await GetPlanillaAdminByIdAsync(planillaId, empresaId) is null) return null;
            using var conn = _dbConnectionFactory.CreateConnection();
            return (await conn.QueryAsync<PlanillaConceptoModel>(@"
                SELECT pcr.plan_con_rem_id AS Id, pcr.planilla_id AS PlanillaId, pcr.concep_remu_id AS ConceptoId,
                       RTRIM(cr.concep_remu) AS Codigo, cr.nombre AS Nombre, tcr.nombre AS TipoNombre,
                       pcr.orden AS Orden, pcr.estado AS Estado
                FROM plan_con_rem pcr
                INNER JOIN planilla p ON p.planilla_id=pcr.planilla_id
                INNER JOIN concep_remu cr ON cr.concep_remu_id=pcr.concep_remu_id AND cr.empresa_id=p.empresa_id
                INNER JOIN tipo_con_remu tcr ON tcr.tipo_con_remu_id=cr.tipo_con_remu_id
                WHERE pcr.planilla_id=@PlanillaId AND p.empresa_id=@EmpresaId AND pcr.estado='A'
                ORDER BY pcr.orden, pcr.plan_con_rem_id",
                new { PlanillaId=planillaId, EmpresaId=empresaId })).ToList();
        }

        public async Task<(bool success, string message, int id)> SavePlanillaConceptoAsync(
            PlanillaConceptoModel model, int empresaId, int userId)
        {
            if (model.Id < 0 || model.PlanillaId <= 0 || model.ConceptoId <= 0 || empresaId <= 0 || userId <= 0)
                return (false, "Seleccione una planilla y un concepto válidos.", 0);
            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            var state = await conn.QuerySingleOrDefaultAsync<string>(@"
                SELECT estado FROM planilla WITH (UPDLOCK,HOLDLOCK)
                WHERE planilla_id=@PlanillaId AND empresa_id=@EmpresaId",
                new { model.PlanillaId, EmpresaId=empresaId }, tx);
            if (state != "A") return (false, "La planilla no existe para esta empresa o está anulada.", 0);
            if (model.Id > 0)
            {
                var current = await conn.QuerySingleOrDefaultAsync<int?>(@"
                    SELECT concep_remu_id FROM plan_con_rem WITH (UPDLOCK,HOLDLOCK)
                    WHERE plan_con_rem_id=@Id AND planilla_id=@PlanillaId AND estado='A'",
                    new { model.Id, model.PlanillaId }, tx);
                if (current != model.ConceptoId)
                    return (false, "El concepto no pertenece a la planilla o su identidad fue modificada.", 0);
            }
            else
            {
                int valid = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM concep_remu
                    WHERE concep_remu_id=@ConceptoId AND empresa_id=@EmpresaId AND estado='A'",
                    new { model.ConceptoId, EmpresaId=empresaId }, tx);
                if (valid != 1) return (false, "El concepto no está habilitado para esta empresa.", 0);
                int duplicate = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM plan_con_rem
                    WHERE planilla_id=@PlanillaId AND concep_remu_id=@ConceptoId AND estado='A'",
                    new { model.PlanillaId, model.ConceptoId }, tx);
                if (duplicate > 0) return (false, "El concepto ya está asignado a esta planilla.", 0);
            }
            var parameters = new DynamicParameters(new { orden=model.Orden, usuarioAccionId=userId });
            int id = model.Id;
            if (id == 0)
            {
                parameters.Add("planillaId", model.PlanillaId);
                parameters.Add("conceptoRemunerativoId", model.ConceptoId);
                parameters.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("dbo.sp_plan_con_rem_insertar", parameters, tx, commandType: CommandType.StoredProcedure);
                id = parameters.Get<int>("newID");
            }
            else
            {
                parameters.Add("planillaConceptoRemunerativoId", id);
                await conn.ExecuteAsync("dbo.sp_plan_con_rem_actualizar", parameters, tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
            return (true, "Concepto guardado.", id);
        }

        public async Task<(bool success, string message)> AnularPlanillaConceptoAsync(
            int id, int planillaId, int empresaId, int userId)
        {
            if (id <= 0 || planillaId <= 0 || empresaId <= 0 || userId <= 0)
                return (false, "La asignación indicada no es válida.");
            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            var state = await conn.QuerySingleOrDefaultAsync<string>(@"
                SELECT estado FROM planilla WITH (UPDLOCK,HOLDLOCK)
                WHERE planilla_id=@PlanillaId AND empresa_id=@EmpresaId",
                new { PlanillaId=planillaId, EmpresaId=empresaId }, tx);
            if (state != "A") return (false, "La planilla no existe para esta empresa o está anulada.");
            int exists = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM plan_con_rem
                WHERE plan_con_rem_id=@Id AND planilla_id=@PlanillaId AND estado='A'",
                new { Id=id, PlanillaId=planillaId }, tx);
            if (exists != 1) return (false, "El concepto no existe en esta planilla o ya fue anulado.");
            // The local SP expects usuarioAccionId, unlike the stale legacy repository parameter.
            await conn.ExecuteAsync("dbo.sp_plan_con_rem_eliminar",
                new { planillaConceptoRemunerativoId=id, usuarioAccionId=userId }, tx, commandType: CommandType.StoredProcedure);
            tx.Commit();
            return (true, "Concepto retirado de la planilla.");
        }

        // =========================================================================
        // 14. VARIABLES DE CÁLCULO (var_calc)
        // =========================================================================
        public async Task<List<LookupItemModel>> GetTiposVariableCalculoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_v_calc_id AS Id, tipo_v_calc AS Codigo, nombre AS Nombre FROM tipo_v_calc WHERE estado = 'A' ORDER BY tipo_v_calc_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetTiposDatoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_dato_id AS Id, tipo_dato AS Codigo, nombre AS Nombre FROM tipo_dato WHERE estado = 'A' ORDER BY tipo_dato_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<VariableCalculoModel>> GetVariablesCalculoListAsync(string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    vc.var_calc_id AS Id,
                    vc.var_calc AS Variable,
                    vc.nombre AS Nombre,
                    vc.tipo_v_calc_id AS TipoVariableCalculoId,
                    ISNULL(tvc.nombre, '') AS TipoVariableCalculoNombre,
                    vc.tipo_dato_id AS TipoDatoId,
                    ISNULL(td.nombre, '') AS TipoDatoNombre,
                    ISNULL(vc.condicion, '') AS Condicion,
                    vc.valor_n_v AS ValorNumericoVerdad,
                    vc.valor_n_f AS ValorNumericoFalso,
                    ISNULL(vc.valor_c_v, '') AS ValorCadenaVerdad,
                    ISNULL(vc.valor_c_f, '') AS ValorCadenaFalso,
                    ISNULL(vc.tabla_orig, '') AS TablaOrigen,
                    ISNULL(vc.tabla_condi, '') AS TablaCondicion,
                    ISNULL(vc.tabla_agrupa, '') AS TablaAgrupacion,
                    vc.es_subconsu AS EsSubconsulta,
                    ISNULL(vc.subconsu_alias, '') AS SubconsultaAlias,
                    ISNULL(vc.subconsu_tipo, 'I') AS SubconsultaTipo,
                    ISNULL(vc.subconsu_column, '') AS SubconsultaColumna,
                    ISNULL(vc.subconsu_enlace, '') AS SubconsultaEnlace,
                    vc.alcance_for_h AS AlcanceFormulaHora,
                    vc.alcance_formu AS AlcanceFormula,
                    vc.alcance_condi AS AlcanceCondicion,
                    vc.estado AS Estado
                FROM var_calc vc
                LEFT JOIN tipo_v_calc tvc ON tvc.tipo_v_calc_id = vc.tipo_v_calc_id
                LEFT JOIN tipo_dato td ON td.tipo_dato_id = vc.tipo_dato_id
                WHERE 1 = 1
                {(!string.IsNullOrWhiteSpace(search) ? "AND (vc.var_calc LIKE @Search OR vc.nombre LIKE @Search OR tvc.nombre LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND vc.estado = @Estado" : "")}
                ORDER BY vc.var_calc_id DESC";

            var rows = await conn.QueryAsync<VariableCalculoModel>(sql, new {
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<VariableCalculoModel?> GetVariableCalculoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    vc.var_calc_id AS Id,
                    vc.var_calc AS Variable,
                    vc.nombre AS Nombre,
                    vc.tipo_v_calc_id AS TipoVariableCalculoId,
                    ISNULL(tvc.nombre, '') AS TipoVariableCalculoNombre,
                    vc.tipo_dato_id AS TipoDatoId,
                    ISNULL(td.nombre, '') AS TipoDatoNombre,
                    ISNULL(vc.condicion, '') AS Condicion,
                    vc.valor_n_v AS ValorNumericoVerdad,
                    vc.valor_n_f AS ValorNumericoFalso,
                    ISNULL(vc.valor_c_v, '') AS ValorCadenaVerdad,
                    ISNULL(vc.valor_c_f, '') AS ValorCadenaFalso,
                    ISNULL(vc.tabla_orig, '') AS TablaOrigen,
                    ISNULL(vc.tabla_condi, '') AS TablaCondicion,
                    ISNULL(vc.tabla_agrupa, '') AS TablaAgrupacion,
                    vc.es_subconsu AS EsSubconsulta,
                    ISNULL(vc.subconsu_alias, '') AS SubconsultaAlias,
                    ISNULL(vc.subconsu_tipo, 'I') AS SubconsultaTipo,
                    ISNULL(vc.subconsu_column, '') AS SubconsultaColumna,
                    ISNULL(vc.subconsu_enlace, '') AS SubconsultaEnlace,
                    vc.alcance_for_h AS AlcanceFormulaHora,
                    vc.alcance_formu AS AlcanceFormula,
                    vc.alcance_condi AS AlcanceCondicion,
                    vc.estado AS Estado
                FROM var_calc vc
                LEFT JOIN tipo_v_calc tvc ON tvc.tipo_v_calc_id = vc.tipo_v_calc_id
                LEFT JOIN tipo_dato td ON td.tipo_dato_id = vc.tipo_dato_id
                WHERE vc.var_calc_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<VariableCalculoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveVariableCalculoAsync(VariableCalculoModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Variable))
                    return (false, "El identificador o código de la variable es obligatorio.", 0);

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre de la variable es obligatorio.", 0);

                if (model.TipoVariableCalculoId <= 0)
                    return (false, "Debe seleccionar un tipo de variable de cálculo.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var p = new DynamicParameters();
                    p.Add("@variableCalculo", model.Variable.Trim());
                    p.Add("@nombre", model.Nombre.Trim());
                    p.Add("@tipoVariableCalculoId", model.TipoVariableCalculoId);
                    p.Add("@tipoDatoId", model.TipoDatoId.HasValue && model.TipoDatoId.Value > 0 ? model.TipoDatoId.Value : (int?)null);
                    p.Add("@condicion", model.Condicion ?? string.Empty);
                    p.Add("@valorNumericoVerdad", model.ValorNumericoVerdad);
                    p.Add("@valorNumericoFalso", model.ValorNumericoFalso);
                    p.Add("@valorCadenaVerdad", model.ValorCadenaVerdad ?? string.Empty);
                    p.Add("@valorCadenaFalso", model.ValorCadenaFalso ?? string.Empty);
                    p.Add("@tablaOrigen", model.TablaOrigen ?? string.Empty);
                    p.Add("@tablaCondicion", model.TablaCondicion ?? string.Empty);
                    p.Add("@tablaAgrupacion", model.TablaAgrupacion ?? string.Empty);
                    p.Add("@esSubconsulta", string.IsNullOrWhiteSpace(model.EsSubconsulta) ? "N" : model.EsSubconsulta.Trim().ToUpper());
                    p.Add("@subConsultaAlias", model.SubconsultaAlias ?? string.Empty);
                    p.Add("@subConsultaTipo", string.IsNullOrWhiteSpace(model.SubconsultaTipo) ? "I" : model.SubconsultaTipo.Trim().ToUpper());
                    p.Add("@subConsultaColumna", model.SubconsultaColumna ?? string.Empty);
                    p.Add("@subConsultaEnlace", model.SubconsultaEnlace ?? string.Empty);
                    p.Add("@alcanceFormulaHora", string.IsNullOrWhiteSpace(model.AlcanceFormulaHora) ? "N" : model.AlcanceFormulaHora.Trim().ToUpper());
                    p.Add("@alcanceFormula", string.IsNullOrWhiteSpace(model.AlcanceFormula) ? "N" : model.AlcanceFormula.Trim().ToUpper());
                    p.Add("@alcanceCondicion", string.IsNullOrWhiteSpace(model.AlcanceCondicion) ? "N" : model.AlcanceCondicion.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_var_calc_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE var_calc SET estado = @Estado WHERE var_calc_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Variable de cálculo registrada exitosamente.", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@variableCalculoId", model.Id);
                    p.Add("@nombre", model.Nombre.Trim());
                    p.Add("@condicion", model.Condicion ?? string.Empty);
                    p.Add("@valorNumericoVerdad", model.ValorNumericoVerdad);
                    p.Add("@valorNumericoFalso", model.ValorNumericoFalso);
                    p.Add("@valorCadenaVerdad", model.ValorCadenaVerdad ?? string.Empty);
                    p.Add("@valorCadenaFalso", model.ValorCadenaFalso ?? string.Empty);
                    p.Add("@tablaOrigen", model.TablaOrigen ?? string.Empty);
                    p.Add("@tablaCondicion", model.TablaCondicion ?? string.Empty);
                    p.Add("@tablaAgrupacion", model.TablaAgrupacion ?? string.Empty);
                    p.Add("@esSubconsulta", string.IsNullOrWhiteSpace(model.EsSubconsulta) ? "N" : model.EsSubconsulta.Trim().ToUpper());
                    p.Add("@subConsultaAlias", model.SubconsultaAlias ?? string.Empty);
                    p.Add("@subConsultaTipo", string.IsNullOrWhiteSpace(model.SubconsultaTipo) ? "I" : model.SubconsultaTipo.Trim().ToUpper());
                    p.Add("@subConsultaColumna", model.SubconsultaColumna ?? string.Empty);
                    p.Add("@subConsultaEnlace", model.SubconsultaEnlace ?? string.Empty);
                    p.Add("@alcanceFormulaHora", string.IsNullOrWhiteSpace(model.AlcanceFormulaHora) ? "N" : model.AlcanceFormulaHora.Trim().ToUpper());
                    p.Add("@alcanceFormula", string.IsNullOrWhiteSpace(model.AlcanceFormula) ? "N" : model.AlcanceFormula.Trim().ToUpper());
                    p.Add("@alcanceCondicion", string.IsNullOrWhiteSpace(model.AlcanceCondicion) ? "N" : model.AlcanceCondicion.Trim().ToUpper());
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_var_calc_actualizar", p, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(@"
                        UPDATE var_calc 
                        SET var_calc = @Variable,
                            tipo_v_calc_id = @TipoVariableCalculoId,
                            tipo_dato_id = @TipoDatoId,
                            estado = @Estado,
                            usuario_modifica_id = @UserId,
                            fecha_modifica = GETDATE()
                        WHERE var_calc_id = @Id",
                        new {
                            Variable = model.Variable.Trim(),
                            TipoVariableCalculoId = model.TipoVariableCalculoId,
                            TipoDatoId = model.TipoDatoId.HasValue && model.TipoDatoId.Value > 0 ? model.TipoDatoId.Value : (int?)null,
                            Estado = estado,
                            UserId = userId,
                            Id = model.Id
                        });

                    return (true, "Variable de cálculo actualizada exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar variable de cálculo: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoVariableCalculoAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM var_calc WHERE var_calc_id = @Id", new { Id = id });
                if (actual == null) return (false, "Variable de cálculo no encontrada.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE var_calc SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE var_calc_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 15. CONCEPTOS REMUNERATIVOS (concep_remu)
        // =========================================================================
        public async Task<List<LookupItemModel>> GetTiposConceptoRemuLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_con_remu_id AS Id, tipo_con_remu AS Codigo, nombre AS Nombre FROM tipo_con_remu WHERE estado = 'A' ORDER BY tipo_con_remu_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<ConceptoRemunerativoModel>> GetConceptosRemuListAsync(int empresaId, string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    cr.concep_remu_id AS Id,
                    cr.concep_remu AS Codigo,
                    cr.empresa_id AS EmpresaId,
                    cr.nombre AS Nombre,
                    ISNULL(cr.nombre_boleta, '') AS NombreBoleta,
                    cr.es_regula AS EsRegularizacion,
                    cr.tipo_con_remu_id AS TipoConceptoRemunerativoId,
                    ISNULL(tcr.nombre, '') AS TipoConceptoRemunerativoNombre,
                    cr.ing_trib_des_id AS IngresoTributoDescuentoId,
                    cr.usa_formula AS UsaFormula,
                    ISNULL(cr.formula_hora, '') AS FormulaHora,
                    ISNULL(cr.formula, '') AS Formula,
                    ISNULL(cr.condicion, '') AS Condicion,
                    cr.estado AS Estado
                FROM concep_remu cr
                LEFT JOIN tipo_con_remu tcr ON tcr.tipo_con_remu_id = cr.tipo_con_remu_id
                WHERE cr.empresa_id = @EmpresaId
                {(!string.IsNullOrWhiteSpace(search) ? "AND (cr.concep_remu LIKE @Search OR cr.nombre LIKE @Search OR tcr.nombre LIKE @Search OR cr.formula LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND cr.estado = @Estado" : "")}
                ORDER BY cr.concep_remu_id DESC";

            var rows = await conn.QueryAsync<ConceptoRemunerativoModel>(sql, new {
                EmpresaId = empresaId,
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<ConceptoRemunerativoModel?> GetConceptoRemuByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    cr.concep_remu_id AS Id,
                    cr.concep_remu AS Codigo,
                    cr.empresa_id AS EmpresaId,
                    cr.nombre AS Nombre,
                    ISNULL(cr.nombre_boleta, '') AS NombreBoleta,
                    cr.es_regula AS EsRegularizacion,
                    cr.tipo_con_remu_id AS TipoConceptoRemunerativoId,
                    ISNULL(tcr.nombre, '') AS TipoConceptoRemunerativoNombre,
                    cr.ing_trib_des_id AS IngresoTributoDescuentoId,
                    cr.usa_formula AS UsaFormula,
                    ISNULL(cr.formula_hora, '') AS FormulaHora,
                    ISNULL(cr.formula, '') AS Formula,
                    ISNULL(cr.condicion, '') AS Condicion,
                    cr.estado AS Estado
                FROM concep_remu cr
                LEFT JOIN tipo_con_remu tcr ON tcr.tipo_con_remu_id = cr.tipo_con_remu_id
                WHERE cr.concep_remu_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<ConceptoRemunerativoModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveConceptoRemuAsync(ConceptoRemunerativoModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (string.IsNullOrWhiteSpace(model.Nombre))
                    return (false, "El nombre del concepto remunerativo es obligatorio.", 0);

                if (model.TipoConceptoRemunerativoId <= 0)
                    return (false, "Debe seleccionar un tipo de concepto remunerativo.", 0);

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();
                string esRegula = string.IsNullOrWhiteSpace(model.EsRegularizacion) ? "N" : model.EsRegularizacion.Trim().ToUpper();
                string usaFormula = string.IsNullOrWhiteSpace(model.UsaFormula) ? "S" : model.UsaFormula.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    string codigo = model.Codigo?.Trim().ToUpper() ?? "";
                    if (string.IsNullOrWhiteSpace(codigo))
                    {
                        var maxNumeric = await conn.QueryFirstOrDefaultAsync<int?>(
                            "SELECT MAX(TRY_CAST(concep_remu AS INT)) FROM concep_remu WHERE empresa_id = @EmpresaId",
                            new { EmpresaId = model.EmpresaId <= 0 ? 1 : model.EmpresaId });
                        int next = (maxNumeric ?? 0) + 1;
                        codigo = next.ToString("D3");
                    }

                    var p = new DynamicParameters();
                    p.Add("@conceptoRemunerativo", codigo);
                    p.Add("@empresaId", model.EmpresaId <= 0 ? 1 : model.EmpresaId);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@nombreBoleta", model.NombreBoleta?.Trim().ToUpper() ?? string.Empty);
                    p.Add("@esRegularizacion", esRegula);
                    p.Add("@tipoConceptoRemunerativoId", model.TipoConceptoRemunerativoId);
                    p.Add("@ingresoTributoDescuentoId", model.IngresoTributoDescuentoId.HasValue && model.IngresoTributoDescuentoId.Value > 0 ? model.IngresoTributoDescuentoId.Value : (int?)null);
                    p.Add("@usaFormula", usaFormula);
                    p.Add("@formulaHora", model.FormulaHora?.Trim() ?? string.Empty);
                    p.Add("@formula", model.Formula?.Trim() ?? string.Empty);
                    p.Add("@condicion", model.Condicion?.Trim() ?? string.Empty);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_concep_remu_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE concep_remu SET estado = @Estado WHERE concep_remu_id = @Id", new { Estado = estado, Id = newId });
                    }

                    return (true, "Concepto remunerativo registrado exitosamente.", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@conceptoRemunerativoId", model.Id);
                    p.Add("@nombre", model.Nombre.Trim().ToUpper());
                    p.Add("@nombreBoleta", model.NombreBoleta?.Trim().ToUpper() ?? string.Empty);
                    p.Add("@esRegularizacion", esRegula);
                    p.Add("@tipoConceptoRemunerativoId", model.TipoConceptoRemunerativoId);
                    p.Add("@ingresoTributoDescuentoId", model.IngresoTributoDescuentoId.HasValue && model.IngresoTributoDescuentoId.Value > 0 ? model.IngresoTributoDescuentoId.Value : (int?)null);
                    p.Add("@usaFormula", usaFormula);
                    p.Add("@formulaHora", model.FormulaHora?.Trim() ?? string.Empty);
                    p.Add("@formula", model.Formula?.Trim() ?? string.Empty);
                    p.Add("@condicion", model.Condicion?.Trim() ?? string.Empty);
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_concep_remu_actualizar", p, commandType: CommandType.StoredProcedure);

                    string codigo = model.Codigo?.Trim().ToUpper() ?? "";
                    await conn.ExecuteAsync(@"
                        UPDATE concep_remu 
                        SET estado = @Estado,
                            concep_remu = CASE WHEN @Codigo <> '' THEN @Codigo ELSE concep_remu END,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE concep_remu_id = @Id",
                        new { Estado = estado, Codigo = codigo, UserId = userId, Id = model.Id });

                    return (true, "Concepto remunerativo actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar concepto remunerativo: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoConceptoRemuAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM concep_remu WHERE concep_remu_id = @Id", new { Id = id });
                if (actual == null) return (false, "Concepto remunerativo no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE concep_remu SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE concep_remu_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 16. PRÉSTAMOS Y ADELANTOS (presta_perso, presta_cuota)
        // =========================================================================
        public async Task<List<LookupItemModel>> GetTiposPrestamoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT tipo_prestamo_id AS Id, tipo_prestamo AS Codigo, nombre AS Nombre FROM tipo_prestamo WHERE estado = 'A' ORDER BY tipo_prestamo_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetEstadosPrestamoLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT estado_presta_id AS Id, estado_presta AS Codigo, nombre AS Nombre FROM estado_presta WHERE estado = 'A' ORDER BY estado_presta_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetPersonalLookupAsync(int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    p.personal_id AS Id,
                    p.personal AS Codigo,
                    CONCAT(prv.num_doc_iden, ' - ', prv.razon_social) AS Nombre
                FROM personal p
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                INNER JOIN empresa_sede es ON es.empresa_sede_id = p.empresa_sede_id
                WHERE es.empresa_id = @EmpresaId AND p.estado = 'A'
                ORDER BY prv.razon_social ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { EmpresaId = empresaId });
            return rows.ToList();
        }

        public async Task<List<PrestamoPersonalModel>> GetPrestamosListAsync(int empresaId, string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    pp.presta_perso_id AS Id,
                    pp.presta_perso AS Numero,
                    pp.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    prv.num_doc_iden AS DocumentoIdentidad,
                    pp.fecha AS Fecha,
                    pp.moneda_id AS MonedaId,
                    m.nombre AS MonedaNombre,
                    m.simbolo AS MonedaSimbolo,
                    pp.monto_tc AS MontoTipoCambio,
                    pp.monto AS Monto,
                    pp.cant_cuotas AS CantidadCuotas,
                    pp.fecha_inicio AS FechaInicio,
                    pp.dias_cuota AS DiasCuota,
                    pp.tipo_prestamo_id AS TipoPrestamoId,
                    tp.nombre AS TipoPrestamoNombre,
                    pp.estado_presta_id AS EstadoPrestamoId,
                    ep.nombre AS EstadoPrestamoNombre,
                    pp.estado AS Estado
                FROM presta_perso pp
                INNER JOIN personal p ON p.personal_id = pp.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                INNER JOIN empresa_sede es ON es.empresa_sede_id = p.empresa_sede_id
                INNER JOIN moneda m ON m.moneda_id = pp.moneda_id
                INNER JOIN tipo_prestamo tp ON tp.tipo_prestamo_id = pp.tipo_prestamo_id
                INNER JOIN estado_presta ep ON ep.estado_presta_id = pp.estado_presta_id
                WHERE es.empresa_id = @EmpresaId
                {(!string.IsNullOrWhiteSpace(search) ? "AND (pp.presta_perso LIKE @Search OR prv.razon_social LIKE @Search OR prv.num_doc_iden LIKE @Search OR tp.nombre LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND pp.estado = @Estado" : "")}
                ORDER BY pp.presta_perso_id DESC";

            var rows = await conn.QueryAsync<PrestamoPersonalModel>(sql, new {
                EmpresaId = empresaId,
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<PrestamoPersonalModel?> GetPrestamoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pp.presta_perso_id AS Id,
                    pp.presta_perso AS Numero,
                    pp.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    prv.num_doc_iden AS DocumentoIdentidad,
                    pp.fecha AS Fecha,
                    pp.moneda_id AS MonedaId,
                    m.nombre AS MonedaNombre,
                    m.simbolo AS MonedaSimbolo,
                    pp.monto_tc AS MontoTipoCambio,
                    pp.monto AS Monto,
                    pp.cant_cuotas AS CantidadCuotas,
                    pp.fecha_inicio AS FechaInicio,
                    pp.dias_cuota AS DiasCuota,
                    pp.tipo_prestamo_id AS TipoPrestamoId,
                    tp.nombre AS TipoPrestamoNombre,
                    pp.estado_presta_id AS EstadoPrestamoId,
                    ep.nombre AS EstadoPrestamoNombre,
                    pp.estado AS Estado
                FROM presta_perso pp
                INNER JOIN personal p ON p.personal_id = pp.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                INNER JOIN moneda m ON m.moneda_id = pp.moneda_id
                INNER JOIN tipo_prestamo tp ON tp.tipo_prestamo_id = pp.tipo_prestamo_id
                INNER JOIN estado_presta ep ON ep.estado_presta_id = pp.estado_presta_id
                WHERE pp.presta_perso_id = @Id";

            var prestamo = await conn.QueryFirstOrDefaultAsync<PrestamoPersonalModel>(sql, new { Id = id });
            if (prestamo != null)
            {
                prestamo.Cuotas = await GetCuotasPorPrestamoAsync(id);
            }
            return prestamo;
        }

        public async Task<List<PrestamoCuotaModel>> GetCuotasPorPrestamoAsync(int prestamoId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pc.presta_cuota_id AS Id,
                    pc.presta_perso_id AS PrestamoPersonalId,
                    pc.cuota AS Cuota,
                    pc.monto AS Monto,
                    pc.fecha_vcto AS FechaVencimiento,
                    pc.fecha_cancela AS FechaCancelacion,
                    pc.tipo_can_cuota_id AS TipoCancelacionCuotaId,
                    ISNULL(tcc.nombre, 'PLANILLA') AS TipoCancelacionCuotaNombre,
                    pc.estado_cuota_id AS EstadoCuotaId,
                    ISNULL(ec.nombre, 'PENDIENTE') AS EstadoCuotaNombre,
                    pc.estado AS Estado
                FROM presta_cuota pc
                LEFT JOIN tipo_can_cuota tcc ON tcc.tipo_can_cuota_id = pc.tipo_can_cuota_id
                LEFT JOIN estado_cuota ec ON ec.estado_cuota_id = pc.estado_cuota_id
                WHERE pc.presta_perso_id = @PrestamoId AND pc.estado = 'A'
                ORDER BY pc.cuota ASC";
            var rows = await conn.QueryAsync<PrestamoCuotaModel>(sql, new { PrestamoId = prestamoId });
            return rows.ToList();
        }

        public async Task<(bool success, string message, int id)> SavePrestamoAsync(PrestamoPersonalModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (model.PersonalId <= 0)
                    return (false, "Debe seleccionar un trabajador.", 0);

                if (model.Monto <= 0)
                    return (false, "El monto del préstamo debe ser mayor a cero.", 0);

                if (model.CantidadCuotas <= 0)
                    model.CantidadCuotas = 1;

                if (model.DiasCuota <= 0)
                    model.DiasCuota = 30;

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    // Auto-generar correlativo presta_perso (char 5)
                    var maxCorr = await conn.QueryFirstOrDefaultAsync<int?>("SELECT MAX(TRY_CAST(presta_perso AS INT)) FROM presta_perso");
                    int next = (maxCorr ?? 0) + 1;
                    string numCorr = next.ToString("D5");

                    var p = new DynamicParameters();
                    p.Add("@prestamoPersonal", numCorr);
                    p.Add("@personalId", model.PersonalId);
                    p.Add("@fecha", model.Fecha);
                    p.Add("@monedaId", model.MonedaId <= 0 ? 1 : model.MonedaId);
                    p.Add("@montoTipoCambio", model.MontoTipoCambio <= 0 ? 1m : model.MontoTipoCambio);
                    p.Add("@monto", model.Monto);
                    p.Add("@cantidadCuotas", model.CantidadCuotas);
                    p.Add("@fechaInicio", model.FechaInicio);
                    p.Add("@diasCuota", model.DiasCuota);
                    p.Add("@tipoPrestamoId", model.TipoPrestamoId <= 0 ? 2 : model.TipoPrestamoId);
                    p.Add("@estadoPrestamoId", model.EstadoPrestamoId <= 0 ? 1 : model.EstadoPrestamoId);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_presta_perso_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    if (estado != "A")
                    {
                        await conn.ExecuteAsync("UPDATE presta_perso SET estado = @Estado WHERE presta_perso_id = @Id", new { Estado = estado, Id = newId });
                    }

                    // Generar cuotas automáticas
                    decimal cuotaMonto = Math.Round(model.Monto / model.CantidadCuotas, 2);
                    decimal acumulado = 0;

                    for (short c = 1; c <= model.CantidadCuotas; c++)
                    {
                        decimal montoEstaCuota = (c == model.CantidadCuotas) ? (model.Monto - acumulado) : cuotaMonto;
                        acumulado += montoEstaCuota;

                        DateTime vcto = model.FechaInicio.AddDays((c - 1) * model.DiasCuota);

                        var pc = new DynamicParameters();
                        pc.Add("@prestamoPersonalId", newId);
                        pc.Add("@cuota", c);
                        pc.Add("@monto", montoEstaCuota);
                        pc.Add("@fechaVencimiento", vcto);
                        pc.Add("@fechaCancelacion", (DateTime?)null);
                        pc.Add("@tipoCancelacionCuotaId", 1); // 1 = PLANILLA
                        pc.Add("@estadoCuotaId", 1); // 1 = PENDIENTE
                        pc.Add("@usuarioAccionId", userId);
                        pc.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        await conn.ExecuteAsync("sp_presta_cuota_insertar", pc, commandType: CommandType.StoredProcedure);
                    }

                    return (true, $"Préstamo N° {numCorr} registrado con {model.CantidadCuotas} cuota(s).", newId);
                }
                else
                {
                    var p = new DynamicParameters();
                    p.Add("@prestamoPersonalId", model.Id);
                    p.Add("@monedaId", model.MonedaId <= 0 ? 1 : model.MonedaId);
                    p.Add("@montoTipoCambio", model.MontoTipoCambio <= 0 ? 1m : model.MontoTipoCambio);
                    p.Add("@monto", model.Monto);
                    p.Add("@cantidadCuotas", model.CantidadCuotas);
                    p.Add("@fechaInicio", model.FechaInicio);
                    p.Add("@diasCuota", model.DiasCuota);
                    p.Add("@tipoPrestamoId", model.TipoPrestamoId <= 0 ? 2 : model.TipoPrestamoId);
                    p.Add("@usuarioAccionId", userId);
                    await conn.ExecuteAsync("sp_presta_perso_actualizar", p, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(@"
                        UPDATE presta_perso 
                        SET personal_id = @PersonalId,
                            fecha = @Fecha,
                            estado_presta_id = @EstadoPrestamoId,
                            estado = @Estado,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE presta_perso_id = @Id",
                        new {
                            PersonalId = model.PersonalId,
                            Fecha = model.Fecha,
                            EstadoPrestamoId = model.EstadoPrestamoId,
                            Estado = estado,
                            UserId = userId,
                            Id = model.Id
                        });

                    return (true, "Préstamo actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar préstamo: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoPrestamoAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM presta_perso WHERE presta_perso_id = @Id", new { Id = id });
                if (actual == null) return (false, "Préstamo no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE presta_perso SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE presta_perso_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 17. VACACIONES DE PERSONAL (personal_vaca)
        // =========================================================================
        public async Task<List<LookupItemModel>> GetEstadosVacacionLookupAsync()
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = "SELECT estado_vaca_id AS Id, estado_vaca AS Codigo, nombre AS Nombre FROM estado_vaca WHERE estado = 'A' ORDER BY estado_vaca_id ASC";
            var rows = await conn.QueryAsync<LookupItemModel>(sql);
            return rows.ToList();
        }

        public async Task<List<PersonalVacacionModel>> GetVacacionesListAsync(int empresaId, string? search = null, string? estado = null, int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT {(top > 0 ? $"TOP ({top})" : "")}
                    pv.personal_vaca_id AS Id,
                    pv.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    prv.num_doc_iden AS DocumentoIdentidad,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    p.fecha_ing AS FechaIngreso,
                    pv.fecha_inicio AS FechaInicio,
                    pv.fecha_fin AS FechaFin,
                    pv.dias_a_cuenta AS DiasACuenta,
                    pv.estado_vaca_id AS EstadoVacacionId,
                    ISNULL(ev.nombre, 'PROGRAMADO') AS EstadoVacacionNombre,
                    pv.estado AS Estado
                FROM personal_vaca pv
                INNER JOIN personal p ON p.personal_id = pv.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                INNER JOIN empresa_sede es ON es.empresa_sede_id = p.empresa_sede_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                LEFT JOIN estado_vaca ev ON ev.estado_vaca_id = pv.estado_vaca_id
                WHERE es.empresa_id = @EmpresaId
                {(!string.IsNullOrWhiteSpace(search) ? "AND (p.personal LIKE @Search OR prv.razon_social LIKE @Search OR prv.num_doc_iden LIKE @Search)" : "")}
                {(!string.IsNullOrWhiteSpace(estado) ? "AND pv.estado = @Estado" : "")}
                ORDER BY pv.personal_vaca_id DESC";

            var rows = await conn.QueryAsync<PersonalVacacionModel>(sql, new {
                EmpresaId = empresaId,
                Search = $"%{search}%",
                Estado = estado
            });
            return rows.ToList();
        }

        public async Task<PersonalVacacionModel?> GetVacacionByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pv.personal_vaca_id AS Id,
                    pv.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    prv.num_doc_iden AS DocumentoIdentidad,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    p.fecha_ing AS FechaIngreso,
                    pv.fecha_inicio AS FechaInicio,
                    pv.fecha_fin AS FechaFin,
                    pv.dias_a_cuenta AS DiasACuenta,
                    pv.estado_vaca_id AS EstadoVacacionId,
                    ISNULL(ev.nombre, 'PROGRAMADO') AS EstadoVacacionNombre,
                    pv.estado AS Estado
                FROM personal_vaca pv
                INNER JOIN personal p ON p.personal_id = pv.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                LEFT JOIN estado_vaca ev ON ev.estado_vaca_id = pv.estado_vaca_id
                WHERE pv.personal_vaca_id = @Id";
            return await conn.QueryFirstOrDefaultAsync<PersonalVacacionModel>(sql, new { Id = id });
        }

        public async Task<(bool success, string message, int id)> SaveVacacionAsync(PersonalVacacionModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                if (conn.State != ConnectionState.Open) conn.Open();

                if (model.PersonalId <= 0)
                    return (false, "Debe seleccionar un trabajador.", 0);

                if (model.DiasACuenta <= 0)
                    model.DiasACuenta = 1;

                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.Trim().ToUpper();

                if (model.Id <= 0)
                {
                    var p = new DynamicParameters();
                    p.Add("@personalId", model.PersonalId);
                    p.Add("@diasACuenta", model.DiasACuenta);
                    p.Add("@estadoVacacionId", model.EstadoVacacionId <= 0 ? 1 : model.EstadoVacacionId);
                    p.Add("@usuarioAccionId", userId);
                    p.Add("@newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await conn.ExecuteAsync("sp_personal_vaca_insertar", p, commandType: CommandType.StoredProcedure);
                    int newId = p.Get<int>("@newID");

                    // Actualizar fechas de inicio/fin y estado
                    await conn.ExecuteAsync(@"
                        UPDATE personal_vaca 
                        SET fecha_inicio = @FechaInicio,
                            fecha_fin = @FechaFin,
                            estado = @Estado
                        WHERE personal_vaca_id = @Id",
                        new {
                            FechaInicio = model.FechaInicio,
                            FechaFin = model.FechaFin,
                            Estado = estado,
                            Id = newId
                        });

                    return (true, "Registro de vacaciones programado exitosamente.", newId);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE personal_vaca 
                        SET personal_id = @PersonalId,
                            fecha_inicio = @FechaInicio,
                            fecha_fin = @FechaFin,
                            dias_a_cuenta = @DiasACuenta,
                            estado_vaca_id = @EstadoVacacionId,
                            estado = @Estado,
                            usuario_modifica_id = @UserId, 
                            fecha_modifica = GETDATE() 
                        WHERE personal_vaca_id = @Id",
                        new {
                            PersonalId = model.PersonalId,
                            FechaInicio = model.FechaInicio,
                            FechaFin = model.FechaFin,
                            DiasACuenta = model.DiasACuenta,
                            EstadoVacacionId = model.EstadoVacacionId,
                            Estado = estado,
                            UserId = userId,
                            Id = model.Id
                        });

                    return (true, "Registro de vacaciones actualizado exitosamente.", model.Id);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar registro de vacaciones: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoVacacionAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM personal_vaca WHERE personal_vaca_id = @Id", new { Id = id });
                if (actual == null) return (false, "Registro de vacaciones no encontrado.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE personal_vaca SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE personal_vaca_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 18. MARCACIONES DE PERSONAL (perso_marca & perso_m_hora)
        // =========================================================================
        public async Task<List<MarcacionPersonalModel>> GetMarcacionesListAsync(int empresaId, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? personalId = null, string? estado = null, string? verificado = null, string? search = null, int top = 100)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            string sql = $@"
                SELECT TOP (@Top)
                    pm.perso_marca_id AS Id,
                    pm.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    ISNULL(prv.num_doc_iden, '') AS DocumentoIdentidad,
                    ISNULL(car.nombre, '') AS CargoNombre,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    pm.fecha AS Fecha,
                    ISNULL(pm.hora_minuto, '') AS HoraMinuto,
                    pm.ignora_tolera AS IgnoraTolera,
                    pm.es_falta AS EsFalta,
                    pm.es_remunera AS EsRemunera,
                    pm.verificado AS Verificado,
                    pm.estado AS Estado
                FROM perso_marca pm
                INNER JOIN personal p ON p.personal_id = pm.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                INNER JOIN empresa_sede es ON es.empresa_sede_id = p.empresa_sede_id
                LEFT JOIN cargo_perso car ON car.cargo_perso_id = p.cargo_perso_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                WHERE es.empresa_id = @EmpresaId
                  { (fechaDesde.HasValue ? "AND pm.fecha >= @FechaDesde" : "") }
                  { (fechaHasta.HasValue ? "AND pm.fecha <= @FechaHasta" : "") }
                  { (personalId.HasValue ? "AND pm.personal_id = @PersonalId" : "") }
                  { (!string.IsNullOrWhiteSpace(estado) ? "AND pm.estado = @Estado" : "") }
                  { (!string.IsNullOrWhiteSpace(verificado) ? "AND pm.verificado = @Verificado" : "") }
                  { (!string.IsNullOrWhiteSpace(search) ? "AND (prv.razon_social LIKE @SearchLike OR prv.num_doc_iden LIKE @SearchLike OR p.personal LIKE @SearchLike)" : "") }
                ORDER BY pm.fecha DESC, pm.perso_marca_id DESC";

            var rows = (await conn.QueryAsync<MarcacionPersonalModel>(sql, new {
                Top = top,
                EmpresaId = empresaId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                PersonalId = personalId,
                Estado = estado,
                Verificado = verificado,
                SearchLike = $"%{search}%"
            })).ToList();

            if (rows.Any())
            {
                var ids = rows.Select(r => r.Id).ToList();
                const string sqlHoras = @"
                    SELECT 
                        pmh.perso_m_hora_id AS Id,
                        pmh.perso_marca_id AS MarcacionId,
                        pmh.hora_rem_id AS HoraRemId,
                        ISNULL(hr.nombre, '') AS HoraRemNombre,
                        pmh.cantidad AS Cantidad,
                        pmh.estado AS Estado
                    FROM perso_m_hora pmh
                    INNER JOIN hora_rem hr ON hr.hora_rem_id = pmh.hora_rem_id
                    WHERE pmh.perso_marca_id IN @Ids AND pmh.estado = 'A'";

                var horas = await conn.QueryAsync<MarcacionHoraDetalleModel>(sqlHoras, new { Ids = ids });
                var horasGroup = horas.GroupBy(h => h.MarcacionId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var row in rows)
                {
                    if (horasGroup.TryGetValue(row.Id, out var det))
                    {
                        row.HorasDetalle = det;
                    }
                }
            }

            return rows;
        }

        public async Task<MarcacionPersonalModel?> GetMarcacionByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pm.perso_marca_id AS Id,
                    pm.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    ISNULL(prv.num_doc_iden, '') AS DocumentoIdentidad,
                    ISNULL(car.nombre, '') AS CargoNombre,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    pm.fecha AS Fecha,
                    ISNULL(pm.hora_minuto, '') AS HoraMinuto,
                    pm.ignora_tolera AS IgnoraTolera,
                    pm.es_falta AS EsFalta,
                    pm.es_remunera AS EsRemunera,
                    pm.verificado AS Verificado,
                    pm.estado AS Estado
                FROM perso_marca pm
                INNER JOIN personal p ON p.personal_id = pm.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN cargo_perso car ON car.cargo_perso_id = p.cargo_perso_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                WHERE pm.perso_marca_id = @Id";

            var marca = await conn.QueryFirstOrDefaultAsync<MarcacionPersonalModel>(sql, new { Id = id });
            if (marca != null)
            {
                const string sqlHoras = @"
                    SELECT 
                        pmh.perso_m_hora_id AS Id,
                        pmh.perso_marca_id AS MarcacionId,
                        pmh.hora_rem_id AS HoraRemId,
                        ISNULL(hr.nombre, '') AS HoraRemNombre,
                        pmh.cantidad AS Cantidad,
                        pmh.estado AS Estado
                    FROM perso_m_hora pmh
                    INNER JOIN hora_rem hr ON hr.hora_rem_id = pmh.hora_rem_id
                    WHERE pmh.perso_marca_id = @Id AND pmh.estado = 'A'";

                var horas = await conn.QueryAsync<MarcacionHoraDetalleModel>(sqlHoras, new { Id = id });
                marca.HorasDetalle = horas.ToList();
            }

            return marca;
        }

        public async Task<(bool success, string message, int id)> SaveMarcacionAsync(MarcacionCrearEditarModel model, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                string estado = string.IsNullOrWhiteSpace(model.Estado) ? "A" : model.Estado.ToUpper();
                string ignora = string.IsNullOrWhiteSpace(model.IgnoraTolera) ? "N" : model.IgnoraTolera.ToUpper();
                string falta = string.IsNullOrWhiteSpace(model.EsFalta) ? "N" : model.EsFalta.ToUpper();
                string remunera = string.IsNullOrWhiteSpace(model.EsRemunera) ? "S" : model.EsRemunera.ToUpper();
                string verificado = string.IsNullOrWhiteSpace(model.Verificado) ? "N" : model.Verificado.ToUpper();

                int marcaId = model.Id;

                if (marcaId == 0)
                {
                    marcaId = await conn.ExecuteScalarAsync<int>("SELECT ISNULL(MAX(perso_marca_id), 0) + 1 FROM perso_marca");

                    await conn.ExecuteAsync(@"
                        INSERT INTO perso_marca 
                        (perso_marca_id, personal_id, fecha, hora_minuto, ignora_tolera, es_falta, es_remunera, verificado, estado, usuario_creacion_id, fecha_creacion)
                        VALUES 
                        (@Id, @PersonalId, @Fecha, @HoraMinuto, @IgnoraTolera, @EsFalta, @EsRemunera, @Verificado, @Estado, @UserId, GETDATE())",
                        new {
                            Id = marcaId,
                            PersonalId = model.PersonalId,
                            Fecha = model.Fecha,
                            HoraMinuto = model.HoraMinuto ?? "",
                            IgnoraTolera = ignora,
                            EsFalta = falta,
                            EsRemunera = remunera,
                            Verificado = verificado,
                            Estado = estado,
                            UserId = userId
                        });
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        UPDATE perso_marca
                        SET personal_id = @PersonalId,
                            fecha = @Fecha,
                            hora_minuto = @HoraMinuto,
                            ignora_tolera = @IgnoraTolera,
                            es_falta = @EsFalta,
                            es_remunera = @EsRemunera,
                            verificado = @Verificado,
                            estado = @Estado,
                            usuario_modifica_id = @UserId,
                            fecha_modifica = GETDATE()
                        WHERE perso_marca_id = @Id",
                        new {
                            Id = marcaId,
                            PersonalId = model.PersonalId,
                            Fecha = model.Fecha,
                            HoraMinuto = model.HoraMinuto ?? "",
                            IgnoraTolera = ignora,
                            EsFalta = falta,
                            EsRemunera = remunera,
                            Verificado = verificado,
                            Estado = estado,
                            UserId = userId
                        });

                    await conn.ExecuteAsync("DELETE FROM perso_m_hora WHERE perso_marca_id = @Id", new { Id = marcaId });
                }

                if (model.Horas != null && model.Horas.Any())
                {
                    int nextHoraId = await conn.ExecuteScalarAsync<int>("SELECT ISNULL(MAX(perso_m_hora_id), 0) FROM perso_m_hora");

                    foreach (var h in model.Horas.Where(x => x.Cantidad > 0))
                    {
                        nextHoraId++;
                        await conn.ExecuteAsync(@"
                            INSERT INTO perso_m_hora 
                            (perso_m_hora_id, perso_marca_id, hora_rem_id, cantidad, estado, usuario_creacion_id, fecha_creacion)
                            VALUES 
                            (@Id, @MarcaId, @HoraRemId, @Cantidad, 'A', @UserId, GETDATE())",
                            new {
                                Id = nextHoraId,
                                MarcaId = marcaId,
                                HoraRemId = h.HoraRemId,
                                Cantidad = h.Cantidad,
                                UserId = userId
                            });
                    }
                }

                return (true, "Marcación guardada exitosamente.", marcaId);
            }
            catch (Exception ex)
            {
                return (false, $"Error al guardar marcación: {ex.Message}", 0);
            }
        }

        public async Task<(bool success, string message, string nuevoEstado)> ToggleEstadoMarcacionAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT estado FROM perso_marca WHERE perso_marca_id = @Id", new { Id = id });
                if (actual == null) return (false, "Marcación no encontrada.", "");

                string nuevo = actual == "A" ? "I" : "A";
                await conn.ExecuteAsync(
                    "UPDATE perso_marca SET estado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE perso_marca_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Estado cambiado a {(nuevo == "A" ? "Activo" : "Inactivo")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}", "");
            }
        }

        public async Task<(bool success, string message, string nuevoVerificado)> ToggleVerificadoMarcacionAsync(int id, int userId)
        {
            try
            {
                using var conn = _dbConnectionFactory.CreateConnection();
                var actual = await conn.QueryFirstOrDefaultAsync<string>("SELECT verificado FROM perso_marca WHERE perso_marca_id = @Id", new { Id = id });
                if (actual == null) return (false, "Marcación no encontrada.", "");

                string nuevo = actual == "S" ? "N" : "S";
                await conn.ExecuteAsync(
                    "UPDATE perso_marca SET verificado = @Nuevo, usuario_modifica_id = @UserId, fecha_modifica = GETDATE() WHERE perso_marca_id = @Id",
                    new { Id = id, Nuevo = nuevo, UserId = userId });

                return (true, $"Verificación actualizada a {(nuevo == "S" ? "Verificado" : "Pendiente")}.", nuevo);
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar verificación: {ex.Message}", "");
            }
        }

        // =========================================================================
        // 19. PROCESO DE PLANILLA (plan_proce & plan_calculo)
        // =========================================================================
        // Shared projection: proc_anual is the process number, not the accounting year.
        private const string PlanillaProcesoSelect = @"
            SELECT pp.plan_proce_id AS Id, pp.periodo_emp_sede_id AS PeriodoEmpSedeId,
                   per.periodo AS PeriodoAnio, pp.planilla_id AS PlanillaId, pla.nombre AS PlanillaNombre,
                   pp.proc_anual AS ProcAnual, pp.mes_id AS MesId, mes.nombre AS MesNombre,
                   pp.moneda_id AS MonedaId, m.nombre AS MonedaNombre, pp.monto_tc AS MontoTipoCambio,
                   pp.peri_remu_id AS PeriRemuId, pr.nombre AS PeriRemuNombre,
                   pp.fecha_desde AS FechaDesde, pp.fecha_hasta AS FechaHasta, pp.fecha_proceso AS FechaProceso,
                   pp.estado_plan_pro_id AS EstadoPlanProceId, RTRIM(ep.estado_plan_pro) AS EstadoPlanProceCodigo,
                   ep.nombre AS EstadoPlanProceNombre, pp.estado AS Estado,
                   ISNULL(calc.TotalTrabajadores,0) AS TotalTrabajadores,
                   ISNULL(calc.TotalIngresos,0) AS TotalIngresos,
                   ISNULL(calc.TotalDescuentos,0) AS TotalDescuentos
            FROM plan_proce pp
            INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
            INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pp.periodo_emp_sede_id
            INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
            INNER JOIN periodo per ON per.periodo_id = pes.periodo_id
            INNER JOIN mes ON mes.mes_id = pp.mes_id
            INNER JOIN moneda m ON m.moneda_id = pp.moneda_id
            INNER JOIN peri_remu pr ON pr.peri_remu_id = pp.peri_remu_id
            INNER JOIN estado_plan_pro ep ON ep.estado_plan_pro_id = pp.estado_plan_pro_id
            OUTER APPLY (
                SELECT COUNT(DISTINCT pc.personal_id) AS TotalTrabajadores,
                       SUM(CASE WHEN tcr.tipo_con_remu = '01' THEN pc.monto ELSE 0 END) AS TotalIngresos,
                       SUM(CASE WHEN tcr.tipo_con_remu = '03' THEN pc.monto ELSE 0 END) AS TotalDescuentos
                FROM plan_calculo pc
                INNER JOIN concep_remu cr ON cr.concep_remu_id = pc.concep_remu_id
                INNER JOIN tipo_con_remu tcr ON tcr.tipo_con_remu_id = cr.tipo_con_remu_id
                WHERE pc.plan_proce_id = pp.plan_proce_id AND pc.estado = 'A'
            ) calc ";

        public Task<int> GetPermisoPlanillaProcesoAsync(int userId, int empresaId)
            => GetPermisoModuloRHAsync(userId, empresaId, "planillaProceso/index");

        public Task<int> GetPermisoModuloRHAsync(int userId, int empresaId, string menuUrl)
            => GetPermisoModuloAsync(userId, empresaId, "rh", menuUrl);

        public async Task<int> GetPermisoModuloAsync(int userId, int empresaId, string area, string menuUrl)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            // Legacy administrator role codes; never trust the session's default IsAdmin flag.
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT ISNULL(MAX(CASE
                    WHEN r.rol IN ('S0001','S0002','00020') THEN 3
                    WHEN m.area = @Area AND m.url = @MenuUrl AND m.estado = 'A'
                         AND rm.lectura = 'S' THEN CASE WHEN rm.escritura = 'S' THEN 3 ELSE 1 END
                    ELSE 0 END),0)
                FROM usuario u
                INNER JOIN rol_usuario ru ON ru.usuario_id = u.usuario_id
                INNER JOIN rol r ON r.rol_id = ru.rol_id AND r.estado = 'A'
                LEFT JOIN rol_menu rm ON rm.rol_id = r.rol_id
                LEFT JOIN menu m ON m.menu_id = rm.menu_id
                WHERE u.usuario_id = @UserId AND u.estado = 'A'
                  AND (u.empresa_id IS NULL OR u.empresa_id = @EmpresaId)
                  AND (r.empresa_id IS NULL OR r.empresa_id = @EmpresaId)",
                new { UserId = userId, EmpresaId = empresaId, Area = area, MenuUrl = menuUrl });
        }

        public async Task<int?> GetPeriodoEmpresaSedePlanillaAsync(int empresaId, int empresaSedeId, int periodoId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<int?>(@"
                SELECT pes.periodo_emp_sede_id
                FROM periodo_emp_sede pes
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
                INNER JOIN periodo per ON per.periodo_id = pes.periodo_id
                WHERE sede.empresa_id = @EmpresaId AND sede.empresa_sede_id = @EmpresaSedeId
                  AND per.periodo_id = @PeriodoId
                  AND pes.estado = 'A' AND sede.estado = 'A' AND per.estado = 'A'",
                new { EmpresaId = empresaId, EmpresaSedeId = empresaSedeId, PeriodoId = periodoId });
        }

        public async Task<List<PlanillaProcesoModel>> GetPlanillasProcesoListAsync(
            int empresaId, int? anio = null, int? mes = null, string? search = null, int top = 50)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            var rows = await conn.QueryAsync<PlanillaProcesoModel>(
                PlanillaProcesoSelect + @"
                WHERE pla.empresa_id = @EmpresaId AND sede.empresa_id = @EmpresaId
                  AND (@Anio IS NULL OR per.periodo = @Anio)
                  AND (@Mes IS NULL OR pp.mes_id = @Mes)
                  AND (@Search IS NULL OR pla.nombre LIKE @Search)
                ORDER BY pp.plan_proce_id DESC OFFSET 0 ROWS FETCH NEXT @Top ROWS ONLY",
                new { EmpresaId = empresaId, Anio = anio, Mes = mes,
                    Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
                    Top = top is 25 or 50 or 100 or 500 ? top : 25 });
            return rows.ToList();
        }

        public async Task<PlanillaProcesoPageModel> GetPlanillasProcesoPageAsync(
            int empresaId, int periodoEmpSedeId, int? anio = null, int? mes = null,
            string? search = null, int page = 1, int pageSize = 25, string? sort = null, string? dir = null)
        {
            var columns = new Dictionary<string, string> {
                ["id"]="pp.plan_proce_id", ["planilla"]="pla.nombre", ["mes"]="pp.mes_id",
                ["numero"]="pp.proc_anual", ["desde"]="pp.fecha_desde", ["hasta"]="pp.fecha_hasta",
                ["moneda"]="m.nombre", ["estado"]="ep.nombre", ["trabajadores"]="calc.TotalTrabajadores", ["neto"]="ISNULL(calc.TotalIngresos,0) - ISNULL(calc.TotalDescuentos,0)" };
            var model = new PlanillaProcesoPageModel();
            model.Ordenar(sort, dir, columns.Keys, "id", "desc");
            using var conn = _dbConnectionFactory.CreateConnection();
            pageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var args = new DynamicParameters(new {
                EmpresaId = empresaId, PeriodoEmpSedeId = periodoEmpSedeId, Anio = anio, Mes = mes,
                Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%" });
            const string filter = @"
                WHERE pla.empresa_id = @EmpresaId AND sede.empresa_id = @EmpresaId
                  AND pp.periodo_emp_sede_id = @PeriodoEmpSedeId
                  AND (@Anio IS NULL OR per.periodo = @Anio)
                  AND (@Mes IS NULL OR pp.mes_id = @Mes)
                  AND (@Search IS NULL OR pla.nombre LIKE @Search)";
            int total = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pp.periodo_emp_sede_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
                INNER JOIN periodo per ON per.periodo_id = pes.periodo_id " + filter, args);
            model.Paginar(total, page, pageSize);
            args.Add("Offset", (model.Page - 1) * model.PageSize);
            args.Add("PageSize", model.PageSize);
            model.Items = (await conn.QueryAsync<PlanillaProcesoModel>(
                PlanillaProcesoSelect + filter +
                $" ORDER BY {columns[model.Sort]} {model.Dir}{(model.Sort == "id" ? "" : ", pp.plan_proce_id DESC")} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", args)).ToList();
            model.VisibleCount = model.Items.Count;
            model.PeriodoAnio = await conn.QuerySingleAsync<int>(@"
                SELECT per.periodo FROM periodo_emp_sede pes
                INNER JOIN periodo per ON per.periodo_id = pes.periodo_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
                WHERE pes.periodo_emp_sede_id = @PeriodoEmpSedeId AND sede.empresa_id = @EmpresaId", args);
            model.Planillas = (await conn.QueryAsync<LookupItemModel>(@"
                SELECT planilla_id AS Id, nombre AS Nombre FROM planilla
                WHERE empresa_id = @EmpresaId ORDER BY nombre", args)).ToList();
            model.Monedas = (await conn.QueryAsync<LookupItemModel>(
                "SELECT moneda_id AS Id, RTRIM(moneda) AS Codigo, nombre AS Nombre FROM moneda ORDER BY nombre")).ToList();
            model.MonedaLocalId = await conn.QuerySingleOrDefaultAsync<int?>(
                "SELECT moneda_id FROM moneda WHERE es_local = 'S' AND estado = 'A'");
            model.Periodicidades = (await conn.QueryAsync<LookupItemModel>(
                "SELECT peri_remu_id AS Id, RTRIM(peri_remu) AS Codigo, nombre AS Nombre FROM peri_remu ORDER BY peri_remu_id")).ToList();
            model.Meses = (await conn.QueryAsync<LookupItemModel>(
                "SELECT mes_id AS Id, RTRIM(mes) AS Codigo, nombre AS Nombre FROM mes ORDER BY mes")).ToList();
            return model;
        }

        public async Task<PlanillaProcesoModel?> GetPlanillaProcesoByIdAsync(int id)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PlanillaProcesoModel>(
                PlanillaProcesoSelect + " WHERE pp.plan_proce_id = @Id", new { Id = id });
        }

        public async Task<PlanillaProcesoModel?> GetPlanillaProcesoByIdAsync(int id, int empresaId, int periodoEmpSedeId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PlanillaProcesoModel>(
                PlanillaProcesoSelect + @" WHERE pp.plan_proce_id = @Id
                    AND pla.empresa_id = @EmpresaId AND sede.empresa_id = @EmpresaId
                    AND pp.periodo_emp_sede_id = @PeriodoEmpSedeId",
                new { Id = id, EmpresaId = empresaId, PeriodoEmpSedeId = periodoEmpSedeId });
        }

        public async Task<List<PlanillaCalculoDetalleModel>?> GetPlanillaCalculoDetalleAsync(
            int planProceId, int empresaId, int periodoEmpSedeId, int? personalId = null)
        {
            if (await GetPlanillaProcesoByIdAsync(planProceId, empresaId, periodoEmpSedeId) is null)
                return null;
            using var conn = _dbConnectionFactory.CreateConnection();
            var rows = await conn.QueryAsync<PlanillaCalculoDetalleModel>(@"
                SELECT pc.plan_calculo_id AS Id, pc.plan_proce_id AS PlanProceId,
                       pc.personal_id AS PersonalId, p.personal AS PersonalCodigo,
                       prv.razon_social AS PersonalNombre, ISNULL(prv.num_doc_iden,'') AS DocumentoIdentidad,
                       ISNULL(car.nombre,'') AS CargoNombre, pc.concep_remu_id AS ConcepRemuId,
                       cr.concep_remu AS ConcepRemuCodigo, cr.nombre AS ConcepRemuNombre,
                       tcr.tipo_con_remu_id AS TipoConceptoId, RTRIM(tcr.tipo_con_remu) AS TipoConceptoCodigo,
                       tcr.nombre AS TipoConceptoNombre, pc.horas AS Horas, pc.monto_sub AS MontoSub,
                       pc.monto AS Monto, pc.estado AS Estado
                FROM plan_calculo pc
                INNER JOIN plan_proce pp ON pp.plan_proce_id = pc.plan_proce_id
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pp.periodo_emp_sede_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
                INNER JOIN personal p ON p.personal_id = pc.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN cargo_perso car ON car.cargo_perso_id = p.cargo_perso_id
                INNER JOIN concep_remu cr ON cr.concep_remu_id = pc.concep_remu_id
                INNER JOIN tipo_con_remu tcr ON tcr.tipo_con_remu_id = cr.tipo_con_remu_id
                WHERE pc.plan_proce_id = @PlanProceId AND pp.periodo_emp_sede_id = @PeriodoEmpSedeId
                  AND pla.empresa_id = @EmpresaId AND sede.empresa_id = @EmpresaId
                  AND (@PersonalId IS NULL OR pc.personal_id = @PersonalId) AND pc.estado = 'A'
                ORDER BY prv.razon_social, pc.personal_id, tcr.tipo_con_remu, cr.concep_remu, pc.plan_calculo_id",
                new { PlanProceId = planProceId, EmpresaId = empresaId,
                    PeriodoEmpSedeId = periodoEmpSedeId, PersonalId = personalId });
            return rows.ToList();
        }

        private const string PlanillaProcesoLockSql = @"
            SELECT pp.plan_proce_id AS Id, pp.planilla_id AS PlanillaId, pp.proc_anual AS ProcAnual,
                   pp.mes_id AS MesId, pp.peri_remu_id AS PeriRemuId, pp.moneda_id AS MonedaId,
                   pp.fecha_proceso AS FechaProceso, pp.estado AS Estado,
                   RTRIM(ep.estado_plan_pro) AS EstadoPlanProceCodigo
            FROM plan_proce pp WITH (UPDLOCK,HOLDLOCK)
            INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
            INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pp.periodo_emp_sede_id
            INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
            INNER JOIN estado_plan_pro ep ON ep.estado_plan_pro_id = pp.estado_plan_pro_id
            WHERE pp.plan_proce_id = @Id AND pla.empresa_id = @EmpresaId AND sede.empresa_id = @EmpresaId
              AND pp.periodo_emp_sede_id = @PeriodoEmpSedeId";

        public async Task<(bool success, string message, int id)> SavePlanillaProcesoAsync(
            PlanillaProcesoCrearModel model, int userId, int empresaId, int periodoEmpSedeId)
        {
            if (model.Id < 0 || userId <= 0 || empresaId <= 0 || periodoEmpSedeId <= 0)
                return (false, "El proceso o el contexto de acceso no es válido.", 0);
            if (model.PlanillaId <= 0 || model.MesId <= 0 || model.PeriRemuId <= 0 ||
                model.MonedaId <= 0 || model.ProcAnual <= 0)
                return (false, "Seleccione planilla, mes, período de pago, moneda y un número de proceso positivo.", 0);
            if (model.FechaDesde < new DateTime(1753, 1, 1) || model.FechaHasta < model.FechaDesde)
                return (false, "Ingrese un rango de fechas válido; el inicio no puede superar el fin.", 0);
            if (model.MontoTipoCambio <= 0 || model.MontoTipoCambio > 99999999.9999m ||
                decimal.Round(model.MontoTipoCambio, 4) != model.MontoTipoCambio)
                return (false, "El tipo de cambio debe ser positivo y tener como máximo cuatro decimales.", 0);

            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            var scope = new { Id = model.Id, EmpresaId = empresaId, PeriodoEmpSedeId = periodoEmpSedeId };
            var validScope = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM periodo_emp_sede pes
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pes.empresa_sede_id
                INNER JOIN periodo per ON per.periodo_id = pes.periodo_id
                WHERE pes.periodo_emp_sede_id = @PeriodoEmpSedeId AND sede.empresa_id = @EmpresaId
                  AND pes.estado = 'A' AND sede.estado = 'A' AND per.estado = 'A'", scope, tx);
            if (validScope != 1)
                return (false, "El período y la sede seleccionados no están habilitados.", 0);

            PlanillaProcesoModel? actual = null;
            if (model.Id > 0)
            {
                actual = await conn.QuerySingleOrDefaultAsync<PlanillaProcesoModel>(PlanillaProcesoLockSql, scope, tx);
                if (actual is null)
                    return (false, "Proceso no encontrado para el período y la sede activos.", 0);
                if (actual.Estado != "A" || actual.EstadoPlanProceCodigo != "01")
                    return (false, "Sólo se puede modificar un proceso activo y abierto.", 0);
                if (actual.PlanillaId != model.PlanillaId || actual.ProcAnual != model.ProcAnual ||
                    actual.MesId != model.MesId || actual.PeriRemuId != model.PeriRemuId)
                    return (false, "La planilla, el número de proceso, el mes y el período de pago no se pueden cambiar.", 0);
            }
            var validReferences = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM planilla pla
                CROSS JOIN mes CROSS JOIN moneda mon CROSS JOIN peri_remu pr
                WHERE pla.planilla_id = @PlanillaId AND pla.empresa_id = @EmpresaId
                  AND mes.mes_id = @MesId AND mon.moneda_id = @MonedaId AND pr.peri_remu_id = @PeriRemuId
                  AND (@Editing = 1 OR (pla.estado = 'A' AND mes.estado = 'A' AND pr.estado = 'A'))
                  AND (mon.estado = 'A' OR mon.moneda_id = @ExistingMonedaId)",
                new { model.PlanillaId, EmpresaId = empresaId, model.MesId, model.MonedaId, model.PeriRemuId,
                    Editing = actual is not null, ExistingMonedaId = actual?.MonedaId }, tx);
            if (validReferences != 1)
                return (false, "La planilla, el mes, la moneda o el período de pago no son válidos para esta operación.", 0);
            bool local = await conn.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN es_local = 'S' THEN 1 ELSE 0 END AS bit) FROM moneda WHERE moneda_id = @MonedaId",
                new { model.MonedaId }, tx);
            var args = new DynamicParameters(new {
                monedaId = model.MonedaId, montoTipoCambio = local ? 1m : model.MontoTipoCambio,
                periodicidadRemunerativaId = model.PeriRemuId, fechaDesde = model.FechaDesde.Date,
                fechaHasta = model.FechaHasta.Date, usuarioAccionId = userId });
            int id = model.Id;
            if (actual is null)
            {
                var estadoId = await conn.QuerySingleAsync<int>(
                    "SELECT estado_plan_pro_id FROM estado_plan_pro WHERE estado_plan_pro = '01' AND estado = 'A'",
                    transaction: tx);
                args.Add("periodoEmpresaSedeId", periodoEmpSedeId);
                args.Add("planillaId", model.PlanillaId);
                args.Add("procesoAnual", model.ProcAnual);
                args.Add("mesId", model.MesId);
                args.Add("estadoPlanillaProcesoId", estadoId);
                args.Add("newID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await conn.ExecuteAsync("dbo.sp_plan_proce_insertar", args, tx, commandType: CommandType.StoredProcedure);
                id = args.Get<int>("newID");
            }
            else
            {
                args.Add("planillaProcesoId", id);
                args.Add("fechaProceso", actual.FechaProceso);
                await conn.ExecuteAsync("dbo.sp_plan_proce_actualizar", args, tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
            return (true, actual is null ? "Proceso de planilla creado." : "Proceso de planilla actualizado.", id);
        }

        public async Task<(bool success, string message)> InactivarPlanillaProcesoAsync(
            int id, int userId, int empresaId, int periodoEmpSedeId)
        {
            if (id <= 0 || userId <= 0 || empresaId <= 0 || periodoEmpSedeId <= 0)
                return (false, "El proceso o el contexto de acceso no es válido.");
            using var conn = _dbConnectionFactory.CreateConnection();
            using var tx = conn.BeginTransaction();
            var actual = await conn.QuerySingleOrDefaultAsync<PlanillaProcesoModel>(
                PlanillaProcesoLockSql, new { Id = id, EmpresaId = empresaId, PeriodoEmpSedeId = periodoEmpSedeId }, tx);
            if (actual is null)
                return (false, "Proceso no encontrado para el período y la sede activos.");
            if (actual.Estado != "A" || actual.EstadoPlanProceCodigo != "01")
                return (false, "Sólo se puede anular un proceso activo y abierto.");
            await conn.ExecuteAsync("dbo.sp_plan_proce_eliminar",
                new { planillaProcesoId = id, usuarioAccionId = userId }, tx, commandType: CommandType.StoredProcedure);
            tx.Commit();
            return (true, "Proceso de planilla anulado.");
        }

        public Task<(bool success, string message)> ProcesarCalculoPlanillaAsync(int planProceId, int userId)
        {
            // The legacy calculation is a separate multi-step engine, not a state transition.
            return Task.FromResult((false,
                "El cálculo de remuneraciones aún no está disponible. No se modificó el proceso."));
        }

        public async Task<PlanillaHorasModel?> GetPlanillaHorasAsync(int planProceId, int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string processSql = @"
                SELECT pp.plan_proce_id AS Id,
                       pp.periodo_emp_sede_id AS PeriodoEmpSedeId,
                       per.periodo AS PeriodoAnio,
                       pp.planilla_id AS PlanillaId,
                       pla.nombre AS PlanillaNombre,
                       pp.proc_anual AS ProcAnual,
                       pp.mes_id AS MesId,
                       CASE pp.mes_id
                           WHEN 1 THEN 'ENERO' WHEN 2 THEN 'FEBRERO' WHEN 3 THEN 'MARZO' WHEN 4 THEN 'ABRIL'
                           WHEN 5 THEN 'MAYO' WHEN 6 THEN 'JUNIO' WHEN 7 THEN 'JULIO' WHEN 8 THEN 'AGOSTO'
                           WHEN 9 THEN 'SETIEMBRE' WHEN 10 THEN 'OCTUBRE' WHEN 11 THEN 'NOVIEMBRE' WHEN 12 THEN 'DICIEMBRE'
                           ELSE '' END AS MesNombre,
                       pp.moneda_id AS MonedaId, ISNULL(m.nombre, '') AS MonedaNombre,
                       pp.monto_tc AS MontoTipoCambio, pp.peri_remu_id AS PeriRemuId,
                       pp.fecha_desde AS FechaDesde, pp.fecha_hasta AS FechaHasta,
                       pp.estado_plan_pro_id AS EstadoPlanProceId,
                       CASE WHEN pp.estado_plan_pro_id = 2 THEN 'CERRADO' ELSE 'ABIERTO' END AS EstadoPlanProceNombre,
                       pp.estado AS Estado
                FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN periodo_emp_sede pes ON pes.periodo_emp_sede_id = pp.periodo_emp_sede_id
                INNER JOIN periodo per ON per.periodo_id = pes.periodo_id
                LEFT JOIN moneda m ON m.moneda_id = pp.moneda_id
                WHERE pp.plan_proce_id = @PlanProceId AND pla.empresa_id = @EmpresaId";
            var proceso = await conn.QueryFirstOrDefaultAsync<PlanillaProcesoModel>(
                processSql,
                new { PlanProceId = planProceId, EmpresaId = empresaId });
            if (proceso == null)
                return null;

            const string horasSql = @"
                SELECT hr.hora_rem_id AS Id,
                       ISNULL(NULLIF(RTRIM(vc.var_calc), ''), RTRIM(hr.hora_rem)) AS Codigo,
                       hr.nombre AS Nombre
                FROM hora_rem hr
                LEFT JOIN var_calc vc ON vc.var_calc_id = hr.var_calc_id
                WHERE hr.empresa_id = @EmpresaId
                  AND (hr.estado = 'A' OR EXISTS
                       (SELECT 1 FROM plan_p_hora ph
                        WHERE ph.plan_proce_id = @PlanProceId AND ph.hora_rem_id = hr.hora_rem_id))
                ORDER BY hr.hora_rem_id";
            var horas = (await conn.QueryAsync<LookupItemModel>(
                horasSql,
                new { EmpresaId = empresaId, PlanProceId = planProceId })).ToList();

            const string personalSql = @"
                WITH personal_proceso AS
                (
                    SELECT pplan.personal_id AS PersonalId, 1 AS Asignado
                    FROM plan_proce pp
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    INNER JOIN perso_plan pplan ON pplan.planilla_id = pp.planilla_id AND pplan.estado = 'A'
                    INNER JOIN personal pe ON pe.personal_id = pplan.personal_id AND pe.estado = 'A'
                    INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id AND sede.empresa_id = @EmpresaId
                    WHERE pp.plan_proce_id = @PlanProceId
                      AND pla.empresa_id = @EmpresaId
                      AND (pe.fecha_ing IS NULL OR pe.fecha_ing <= pp.fecha_hasta)
                      AND (pe.fecha_cese IS NULL OR pe.fecha_cese >= pp.fecha_desde)

                    UNION

                    SELECT h.personal_id, 0
                    FROM plan_p_hora h
                    INNER JOIN plan_proce pp ON pp.plan_proce_id = h.plan_proce_id
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    INNER JOIN personal pe ON pe.personal_id = h.personal_id
                    INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id AND sede.empresa_id = @EmpresaId
                    WHERE h.plan_proce_id = @PlanProceId AND pla.empresa_id = @EmpresaId
                )
                SELECT pe.personal_id AS PersonalId, RTRIM(pe.personal) AS Codigo,
                       ISNULL(prv.razon_social, '') AS Nombre,
                       CASE WHEN MAX(personal_proceso.Asignado) = 1 THEN 'A' ELSE 'I' END AS Estado
                FROM personal_proceso
                INNER JOIN personal pe ON pe.personal_id = personal_proceso.PersonalId
                INNER JOIN proveedor prv ON prv.proveedor_id = pe.proveedor_id
                GROUP BY pe.personal_id, pe.personal, prv.razon_social
                ORDER BY Nombre";
            var empleados = (await conn.QueryAsync<PlanillaHorasEmpleadoRow>(
                personalSql,
                new { PlanProceId = planProceId, EmpresaId = empresaId })).ToList();

            const string valoresSql = @"
                SELECT h.personal_id AS PersonalId, h.hora_rem_id AS HoraRemId,
                       SUM(CASE WHEN h.estado = 'A' THEN h.cantidad ELSE 0 END) AS Cantidad,
                       CASE WHEN MAX(CASE WHEN h.estado = 'A' THEN 1 ELSE 0 END) = 1 THEN 'A' ELSE 'I' END AS Estado
                FROM plan_p_hora h
                INNER JOIN plan_proce pp ON pp.plan_proce_id = h.plan_proce_id
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN personal pe ON pe.personal_id = h.personal_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id
                WHERE h.plan_proce_id = @PlanProceId AND pla.empresa_id = @EmpresaId
                  AND sede.empresa_id = @EmpresaId AND h.hora_rem_id IN
                      (SELECT hora_rem_id FROM hora_rem WHERE empresa_id = @EmpresaId)
                GROUP BY h.personal_id, h.hora_rem_id";
            var valores = (await conn.QueryAsync<PlanillaHorasValorRow>(
                valoresSql,
                new { PlanProceId = planProceId, EmpresaId = empresaId }))
                .ToDictionary(x => (x.PersonalId, x.HoraRemId));

            var detalle = new PlanillaHorasModel
            {
                Proceso = proceso,
                Horas = horas,
                Empleados = empleados.Select(empleado => new PlanillaHorasEmpleadoModel
                {
                    PersonalId = empleado.PersonalId,
                    Codigo = empleado.Codigo,
                    Nombre = empleado.Nombre,
                    Estado = empleado.Estado,
                    Cantidades = horas.Select(hora => new PlanillaHoraCantidadModel
                    {
                        HoraRemId = hora.Id,
                        Cantidad = valores.TryGetValue((empleado.PersonalId, hora.Id), out var valor) && valor.Estado == "A"
                            ? valor.Cantidad
                            : 0
                    }).ToList()
                }).ToList()
            };
            return detalle;
        }

        public async Task<List<PlanillaHorasEmpleadoModel>> GetPersonalDisponiblePlanillaHorasAsync(
            int planProceId,
            int empresaId,
            string? search = null)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT TOP (500) pe.personal_id AS PersonalId, RTRIM(pe.personal) AS Codigo,
                       ISNULL(prv.razon_social, '') AS Nombre, 'A' AS Estado
                FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN perso_marca pm ON pm.fecha >= pp.fecha_desde
                    AND pm.fecha < DATEADD(day, 1, pp.fecha_hasta)
                    AND pm.estado = 'A' AND pm.es_remunera = 'S' AND pm.verificado = 'S'
                INNER JOIN personal pe ON pe.personal_id = pm.personal_id AND pe.estado = 'A'
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id AND sede.empresa_id = @EmpresaId
                INNER JOIN proveedor prv ON prv.proveedor_id = pe.proveedor_id
                WHERE pp.plan_proce_id = @PlanProceId AND pla.empresa_id = @EmpresaId
                  AND EXISTS
                      (SELECT 1 FROM perso_m_hora pmh
                       WHERE pmh.perso_marca_id = pm.perso_marca_id AND pmh.estado = 'A')
                  AND NOT EXISTS
                      (SELECT 1 FROM perso_plan pplan
                       WHERE pplan.planilla_id = pp.planilla_id
                         AND pplan.personal_id = pe.personal_id AND pplan.estado = 'A')
                  AND NOT EXISTS
                      (SELECT 1 FROM plan_p_hora ph
                       WHERE ph.plan_proce_id = pp.plan_proce_id AND ph.personal_id = pe.personal_id)
                  AND (@Search IS NULL OR prv.razon_social LIKE @SearchLike OR pe.personal LIKE @SearchLike)
                ORDER BY Nombre";
            var rows = await conn.QueryAsync<PlanillaHorasEmpleadoModel>(
                sql,
                new
                {
                    PlanProceId = planProceId,
                    EmpresaId = empresaId,
                    Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                    SearchLike = $"%{search?.Trim()}%"
                });
            return rows.ToList();
        }

        public async Task<(bool success, string message)> SavePlanillaHorasAsync(
            PlanillaHorasGuardarModel model,
            int empresaId,
            int userId)
        {
            if (model.ProcesoId <= 0)
                return (false, "Seleccione un proceso de planilla.");
            if (model.Empleados == null || model.Empleados.Count == 0 || model.Empleados.Any(empleado => empleado == null))
                return (false, "No se recibieron los datos de horas.");

            using var conn = _dbConnectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                const string processSql = @"
                    SELECT pp.planilla_id AS PlanillaId, pp.estado_plan_pro_id AS EstadoId
                    FROM plan_proce pp WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    WHERE pp.plan_proce_id = @ProcesoId AND pla.empresa_id = @EmpresaId";
                var proceso = await conn.QueryFirstOrDefaultAsync<(int PlanillaId, int EstadoId)>(
                    processSql,
                    new { model.ProcesoId, EmpresaId = empresaId },
                    transaction);
                if (proceso.PlanillaId == 0)
                    return (false, "Proceso de planilla no encontrado para la empresa activa.");
                if (proceso.EstadoId == 2)
                    return (false, "El proceso está cerrado y no admite cambios en sus horas.");

                var personalIds = model.Empleados.Select(x => x.PersonalId).Distinct().ToArray();
                if (personalIds.Length != model.Empleados.Count)
                    return (false, "La lista contiene personal duplicado.");
                var assignedIds = (await conn.QueryAsync<int>(@"
                    SELECT DISTINCT pe.personal_id
                    FROM personal pe
                    INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id
                    WHERE sede.empresa_id = @EmpresaId
                      AND (EXISTS (SELECT 1 FROM perso_plan pp WHERE pp.personal_id = pe.personal_id AND pp.planilla_id = @PlanillaId AND pp.estado = 'A')
                           OR EXISTS (SELECT 1 FROM plan_p_hora ph WHERE ph.personal_id = pe.personal_id AND ph.plan_proce_id = @ProcesoId)
                           OR EXISTS
                              (SELECT 1 FROM plan_proce pproc
                               INNER JOIN perso_marca pm ON pm.fecha >= pproc.fecha_desde
                                   AND pm.fecha < DATEADD(day, 1, pproc.fecha_hasta)
                                   AND pm.estado = 'A' AND pm.es_remunera = 'S' AND pm.verificado = 'S'
                               WHERE pproc.plan_proce_id = @ProcesoId AND pm.personal_id = pe.personal_id
                                 AND EXISTS
                                     (SELECT 1 FROM perso_m_hora pmh
                                      WHERE pmh.perso_marca_id = pm.perso_marca_id AND pmh.estado = 'A')))",
                    new { EmpresaId = empresaId, PlanillaId = proceso.PlanillaId, model.ProcesoId },
                    transaction)).ToHashSet();
                if (personalIds.Any(id => id <= 0 || !assignedIds.Contains(id)))
                    return (false, "La lista contiene personal que no pertenece o no está habilitado para este proceso.");

                var horaIds = model.Empleados.SelectMany(x => x.Cantidades ?? new List<PlanillaHoraCantidadModel>())
                    .Select(x => x.HoraRemId).Distinct().ToArray();
                var allowedHoraIds = (await conn.QueryAsync<int>(@"
                    SELECT hora_rem_id FROM hora_rem
                    WHERE empresa_id = @EmpresaId AND
                          (estado = 'A' OR EXISTS
                              (SELECT 1 FROM plan_p_hora ph
                               WHERE ph.plan_proce_id = @ProcesoId AND ph.hora_rem_id = hora_rem.hora_rem_id))",
                    new { EmpresaId = empresaId, model.ProcesoId },
                    transaction)).ToHashSet();
                if (horaIds.Any(id => id <= 0 || !allowedHoraIds.Contains(id)) ||
                    horaIds.Length != allowedHoraIds.Count)
                    return (false, "La lista contiene conceptos de horas que no están activos para la empresa.");

                if (model.Empleados.Any(x => x.Cantidades == null ||
                    x.Cantidades.Any(c => c.Cantidad < 0) ||
                    x.Cantidades.Select(c => c.HoraRemId).Distinct().Count() != x.Cantidades.Count ||
                    x.Cantidades.Count != allowedHoraIds.Count ||
                    x.Cantidades.Any(c => !allowedHoraIds.Contains(c.HoraRemId))))
                    return (false, "Las cantidades deben ser no negativas y cada concepto debe aparecer una sola vez por persona.");

                await conn.ExecuteAsync(@"
                    UPDATE plan_p_hora
                    SET estado = 'I', usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                    WHERE plan_proce_id = @ProcesoId AND estado = 'A' AND personal_id IN @PersonalIds",
                    new { model.ProcesoId, UserId = userId, PersonalIds = personalIds },
                    transaction);

                foreach (var empleado in model.Empleados)
                {
                    string estado = empleado.Estado == "I" ? "I" : "A";
                    foreach (var cantidad in empleado.Cantidades)
                    {
                        int updated = await conn.ExecuteAsync(@"
                            UPDATE plan_p_hora
                            SET cantidad = @Cantidad, estado = @Estado,
                                usuario_modifica_id = @UserId, fecha_modifica = GETDATE()
                            WHERE plan_proce_id = @ProcesoId
                              AND personal_id = @PersonalId AND hora_rem_id = @HoraRemId",
                            new
                            {
                                model.ProcesoId,
                                empleado.PersonalId,
                                cantidad.HoraRemId,
                                cantidad.Cantidad,
                                Estado = estado,
                                UserId = userId
                            },
                            transaction);

                        if (updated == 0)
                        {
                            await conn.ExecuteAsync(@"
                                DECLARE @newId INT = NEXT VALUE FOR plan_p_hora_seq;
                                INSERT INTO plan_p_hora
                                    (plan_p_hora_id, plan_proce_id, hora_rem_id, personal_id, cantidad, estado, usuario_creacion_id, fecha_creacion)
                                VALUES
                                    (@newId, @ProcesoId, @HoraRemId, @PersonalId, @Cantidad, @Estado, @UserId, GETDATE())",
                                new
                                {
                                    model.ProcesoId,
                                    empleado.PersonalId,
                                    cantidad.HoraRemId,
                                    cantidad.Cantidad,
                                    Estado = estado,
                                    UserId = userId
                                },
                                transaction);
                        }
                    }
                }

                transaction.Commit();
                return (true, "Horas de planilla guardadas correctamente.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<(bool success, string message)> ImportarHorasDesdeMarcacionesAsync(
            int planProceId,
            int empresaId,
            int userId)
        {
            var detalle = await GetPlanillaHorasAsync(planProceId, empresaId);
            if (detalle == null)
                return (false, "Proceso de planilla no encontrado para la empresa activa.");
            if (detalle.Proceso.EstadoPlanProceId == 2)
                return (false, "El proceso está cerrado y no admite cambios en sus horas.");

            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT pm.personal_id AS PersonalId, pmh.hora_rem_id AS HoraRemId,
                       SUM(pmh.cantidad) AS Cantidad
                FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN personal pe ON pe.estado = 'A'
                    AND (EXISTS
                         (SELECT 1 FROM perso_plan pplan
                          WHERE pplan.planilla_id = pp.planilla_id
                            AND pplan.personal_id = pe.personal_id AND pplan.estado = 'A')
                         OR EXISTS
                         (SELECT 1 FROM plan_p_hora ph
                          WHERE ph.plan_proce_id = pp.plan_proce_id AND ph.personal_id = pe.personal_id))
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = pe.empresa_sede_id AND sede.empresa_id = @EmpresaId
                INNER JOIN perso_marca pm ON pm.personal_id = pe.personal_id
                    AND pm.fecha >= pp.fecha_desde AND pm.fecha < DATEADD(day, 1, pp.fecha_hasta)
                    AND pm.estado = 'A' AND pm.es_remunera = 'S' AND pm.verificado = 'S'
                INNER JOIN perso_m_hora pmh ON pmh.perso_marca_id = pm.perso_marca_id AND pmh.estado = 'A'
                INNER JOIN hora_rem hr ON hr.hora_rem_id = pmh.hora_rem_id AND hr.empresa_id = @EmpresaId AND hr.estado = 'A'
                WHERE pp.plan_proce_id = @PlanProceId AND pla.empresa_id = @EmpresaId
                GROUP BY pm.personal_id, pmh.hora_rem_id";
            var marcaciones = (await conn.QueryAsync<(int PersonalId, int HoraRemId, decimal Cantidad)>(
                sql,
                new { PlanProceId = planProceId, EmpresaId = empresaId }))
                .ToDictionary(x => (x.PersonalId, x.HoraRemId), x => x.Cantidad);

            foreach (var empleado in detalle.Empleados)
            {
                foreach (var cantidad in empleado.Cantidades)
                {
                    cantidad.Cantidad = marcaciones.TryGetValue((empleado.PersonalId, cantidad.HoraRemId), out var total)
                        ? total
                        : 0;
                }
            }

            var result = await SavePlanillaHorasAsync(
                new PlanillaHorasGuardarModel { ProcesoId = planProceId, Empleados = detalle.Empleados },
                empresaId,
                userId);
            return result.success
                ? (true, "Horas importadas desde las marcaciones verificadas.")
                : result;
        }

        public async Task<PlanillaRegularizacionPageModel?> GetPlanillaRegularizacionesAsync(
            int planProceId,
            int empresaId,
            int page = 1,
            int pageSize = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (!await PlanillaProcesoPerteneceEmpresaAsync(conn, planProceId, empresaId))
                return null;

            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            const string countSql = @"
                SELECT COUNT(*)
                FROM plan_p_regu reg
                WHERE reg.plan_proce_id = @PlanProceId";
            var total = await conn.QuerySingleAsync<int>(
                countSql,
                new { PlanProceId = planProceId });
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);

            const string sql = @"
                SELECT reg.plan_p_regu_id AS Id,
                       reg.plan_proce_id AS PlanProceId,
                       reg.personal_id AS PersonalId,
                       RTRIM(per.personal) AS PersonalCodigo,
                       ISNULL(prov.razon_social, '') AS PersonalNombre,
                       reg.concep_remu_id AS ConcepRemuId,
                       RTRIM(con.concep_remu) AS ConcepRemuCodigo,
                       con.nombre AS ConcepRemuNombre,
                       reg.cant_hora AS CantidadHora,
                       reg.monto AS Monto,
                       reg.observa AS Observacion,
                       reg.estado AS Estado
                FROM plan_p_regu reg
                INNER JOIN plan_proce pp ON pp.plan_proce_id = reg.plan_proce_id
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN personal per ON per.personal_id = reg.personal_id
                INNER JOIN proveedor prov ON prov.proveedor_id = per.proveedor_id
                INNER JOIN concep_remu con ON con.concep_remu_id = reg.concep_remu_id
                WHERE reg.plan_proce_id = @PlanProceId
                  AND pla.empresa_id = @EmpresaId
                ORDER BY CASE WHEN reg.estado = 'A' THEN 0 ELSE 1 END,
                         prov.razon_social, con.nombre, reg.plan_p_regu_id
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var rows = await conn.QueryAsync<PlanillaRegularizacionModel>(
                sql,
                new
                {
                    PlanProceId = planProceId,
                    EmpresaId = empresaId,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new PlanillaRegularizacionPageModel
            {
                Items = rows.ToList(),
                Total = total,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        public async Task<List<LookupItemModel>?> GetPersonalDisponibleRegularizacionAsync(
            int planProceId,
            int empresaId,
            string? search = null,
            int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (!await PlanillaProcesoPerteneceEmpresaAsync(conn, planProceId, empresaId))
                return null;

            const string sql = @"
                SELECT TOP (@Top)
                       per.personal_id AS Id,
                       RTRIM(per.personal) + ' - ' + prov.razon_social AS Nombre
                FROM personal per
                INNER JOIN proveedor prov ON prov.proveedor_id = per.proveedor_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = per.empresa_sede_id
                WHERE sede.empresa_id = @EmpresaId
                  AND per.estado = 'A'
                  AND (@Search IS NULL OR per.personal LIKE '%' + @Search + '%'
                       OR prov.razon_social LIKE '%' + @Search + '%'
                       OR prov.num_doc_iden LIKE '%' + @Search + '%')
                ORDER BY prov.razon_social, per.personal";
            var rows = await conn.QueryAsync<LookupItemModel>(
                sql,
                new
                {
                    Top = top is 25 or 50 or 100 or 500 ? top : 25,
                    EmpresaId = empresaId,
                    Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
                });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>?> GetConceptosDisponiblesRegularizacionAsync(
            int planProceId,
            int empresaId,
            string? search = null,
            int top = 25)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            if (!await PlanillaProcesoPerteneceEmpresaAsync(conn, planProceId, empresaId))
                return null;

            const string sql = @"
                SELECT TOP (@Top)
                       con.concep_remu_id AS Id,
                       RTRIM(con.concep_remu) + ' - ' + con.nombre AS Nombre
                FROM concep_remu con
                WHERE con.empresa_id = @EmpresaId
                  AND con.es_regula = 'S'
                  AND con.estado = 'A'
                  AND (@Search IS NULL OR con.concep_remu LIKE '%' + @Search + '%'
                       OR con.nombre LIKE '%' + @Search + '%')
                ORDER BY con.nombre, con.concep_remu";
            var rows = await conn.QueryAsync<LookupItemModel>(
                sql,
                new
                {
                    Top = top is 25 or 50 or 100 or 500 ? top : 25,
                    EmpresaId = empresaId,
                    Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
                });
            return rows.ToList();
        }

        public async Task<(bool success, string message)> SavePlanillaRegularizacionAsync(
            PlanillaRegularizacionGuardarModel model,
            int empresaId,
            int userId)
        {
            if (model.PlanProceId <= 0 ||
                model.Monto < 0 ||
                string.IsNullOrWhiteSpace(model.Observacion) ||
                model.Observacion.Length > 200)
                return (false, "Revise el proceso, el monto y la observación de la regularización.");

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                const string processSql = @"
                    SELECT pp.estado_plan_pro_id
                    FROM plan_proce pp WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    WHERE pp.plan_proce_id = @PlanProceId
                      AND pla.empresa_id = @EmpresaId";
                var processState = await conn.QueryFirstOrDefaultAsync<int?>(
                    processSql,
                    new { model.PlanProceId, EmpresaId = empresaId },
                    transaction);
                if (processState == null)
                {
                    transaction.Rollback();
                    return (false, "Proceso de planilla no encontrado para la empresa activa.");
                }
                if (processState == 2)
                {
                    transaction.Rollback();
                    return (false, "El proceso está cerrado y no admite cambios.");
                }

                if (model.Id == 0)
                {
                    const string candidateSql = @"
                        SELECT
                            CASE WHEN EXISTS (
                                SELECT 1
                                FROM personal per
                                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = per.empresa_sede_id
                                WHERE per.personal_id = @PersonalId
                                  AND per.estado = 'A'
                                  AND sede.empresa_id = @EmpresaId
                            ) THEN 1 ELSE 0 END
                            * CASE WHEN EXISTS (
                                SELECT 1
                                FROM concep_remu con
                                WHERE con.concep_remu_id = @ConcepRemuId
                                  AND con.empresa_id = @EmpresaId
                                  AND con.es_regula = 'S'
                                  AND con.estado = 'A'
                            ) THEN 1 ELSE 0 END";
                    var candidateIsValid = await conn.QuerySingleAsync<int>(
                        candidateSql,
                        new
                        {
                            model.PersonalId,
                            model.ConcepRemuId,
                            EmpresaId = empresaId
                        },
                        transaction);
                    if (candidateIsValid == 0)
                    {
                        transaction.Rollback();
                        return (false, "Seleccione personal activo y un concepto de regularización vigente de la empresa.");
                    }

                    var nextId = await conn.QuerySingleAsync<int>(
                        "SELECT NEXT VALUE FOR dbo.plan_p_regu_seq",
                        transaction: transaction);
                    const string insertSql = @"
                        INSERT INTO plan_p_regu
                            (plan_p_regu_id, plan_proce_id, personal_id, concep_remu_id, cant_hora,
                             monto, observa, estado, usuario_creacion_id, fecha_creacion)
                        VALUES
                            (@Id, @PlanProceId, @PersonalId, @ConcepRemuId, 0,
                             @Monto, @Observacion, 'A', @UserId, GETDATE())";
                    await conn.ExecuteAsync(
                        insertSql,
                        new
                        {
                            Id = nextId,
                            model.PlanProceId,
                            model.PersonalId,
                            model.ConcepRemuId,
                            model.Monto,
                            Observacion = model.Observacion.Trim(),
                            UserId = userId
                        },
                        transaction);
                }
                else
                {
                    const string updateSql = @"
                        UPDATE plan_p_regu
                        SET monto = @Monto,
                            observa = @Observacion,
                            usuario_modifica_id = @UserId,
                            fecha_modifica = GETDATE()
                        WHERE plan_p_regu_id = @Id
                          AND plan_proce_id = @PlanProceId
                          AND estado = 'A'";
                    var changed = await conn.ExecuteAsync(
                        updateSql,
                        new
                        {
                            model.Id,
                            model.PlanProceId,
                            model.Monto,
                            Observacion = model.Observacion.Trim(),
                            UserId = userId
                        },
                        transaction);
                    if (changed == 0)
                    {
                        transaction.Rollback();
                        return (false, "La regularización no existe o ya fue anulada.");
                    }
                }

                transaction.Commit();
                return (true, model.Id == 0 ? "Regularización registrada correctamente." : "Regularización actualizada correctamente.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<(bool success, string message)> AnularPlanillaRegularizacionAsync(
            int id,
            int planProceId,
            int empresaId,
            int userId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                const string processSql = @"
                    SELECT pp.estado_plan_pro_id
                    FROM plan_proce pp WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    WHERE pp.plan_proce_id = @PlanProceId
                      AND pla.empresa_id = @EmpresaId";
                var processState = await conn.QueryFirstOrDefaultAsync<int?>(
                    processSql,
                    new { PlanProceId = planProceId, EmpresaId = empresaId },
                    transaction);
                if (processState == null)
                {
                    transaction.Rollback();
                    return (false, "Proceso de planilla no encontrado para la empresa activa.");
                }
                if (processState == 2)
                {
                    transaction.Rollback();
                    return (false, "El proceso está cerrado y no admite cambios.");
                }

                const string sql = @"
                    UPDATE plan_p_regu
                    SET estado = 'I',
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE plan_p_regu_id = @Id
                      AND plan_proce_id = @PlanProceId
                      AND estado = 'A'";
                var changed = await conn.ExecuteAsync(
                    sql,
                    new { Id = id, PlanProceId = planProceId, UserId = userId },
                    transaction);
                if (changed == 0)
                {
                    transaction.Rollback();
                    return (false, "La regularización no existe o ya fue anulada.");
                }

                transaction.Commit();
                return (true, "Regularización anulada correctamente.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task<bool> PlanillaProcesoPerteneceEmpresaAsync(
            IDbConnection conn,
            int planProceId,
            int empresaId)
        {
            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM plan_proce pp
                    INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                    WHERE pp.plan_proce_id = @PlanProceId
                      AND pla.empresa_id = @EmpresaId
                ) THEN 1 ELSE 0 END";
            return await conn.QuerySingleAsync<int>(
                sql,
                new { PlanProceId = planProceId, EmpresaId = empresaId }) == 1;
        }

        public async Task<SolicitudAccesoPageModel> GetSolicitudesAccesoAsync(
            int empresaId,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? estadoAprobacionId = null,
            int page = 1,
            int pageSize = 25, string? sort = null, string? dir = null)
        {
            var columns = new Dictionary<string, string> { ["id"]="solicitud.perso_sol_acc_id", ["solicitante"]="proveedorSolicita.razon_social", ["fecha"]="solicitud.fecha", ["hora"]="solicitud.hora_sol", ["autoriza"]="proveedorAutoriza.razon_social", ["hora_autorizada"]="solicitud.hora_auto", ["estado"]="estado.nombre" };
            var ordering = new ListadoPaginadoModel();
            ordering.Ordenar(sort, dir, columns.Keys, "fecha", "desc");
            using var conn = _dbConnectionFactory.CreateConnection();
            var safePageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
            var safePage = Math.Max(page, 1);
            var estados = (await conn.QueryAsync<EstadoAprobacionModel>(
                @"SELECT estado_aproba_id AS Id,
                         estado_aproba AS Codigo,
                         nombre AS Nombre
                  FROM estado_aproba
                  WHERE estado = 'A'
                  ORDER BY estado_aproba_id")).ToList();

            var selectedStateId = estadoAprobacionId;
            if (!selectedStateId.HasValue)
                selectedStateId = estados.FirstOrDefault(estado => estado.Codigo == "P")?.Id;

            var parameters = new
            {
                EmpresaId = empresaId,
                FechaDesde = fechaDesde?.Date,
                FechaHasta = fechaHasta?.Date,
                EstadoAprobacionId = selectedStateId
            };
            const string fromAndFilter = @"
                FROM perso_sol_acc solicitud
                INNER JOIN personal personalSolicita ON personalSolicita.personal_id = solicitud.personal_id
                INNER JOIN empresa_sede sede ON sede.empresa_sede_id = personalSolicita.empresa_sede_id
                INNER JOIN proveedor proveedorSolicita ON proveedorSolicita.proveedor_id = personalSolicita.proveedor_id
                INNER JOIN estado_aproba estado ON estado.estado_aproba_id = solicitud.estado_aproba_id
                LEFT JOIN personal personalAutoriza ON personalAutoriza.personal_id = solicitud.perso_auto_id
                LEFT JOIN proveedor proveedorAutoriza ON proveedorAutoriza.proveedor_id = personalAutoriza.proveedor_id
                WHERE sede.empresa_id = @EmpresaId
                  AND solicitud.estado = 'A'
                  AND (@FechaDesde IS NULL OR solicitud.fecha >= @FechaDesde)
                  AND (@FechaHasta IS NULL OR solicitud.fecha < DATEADD(DAY, 1, @FechaHasta))
                  AND (@EstadoAprobacionId IS NULL OR solicitud.estado_aproba_id = @EstadoAprobacionId)";
            var total = await conn.QuerySingleAsync<int>(
                $"SELECT COUNT(*) {fromAndFilter}",
                parameters);
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)safePageSize));
            safePage = Math.Min(safePage, lastPage);

            var items = await conn.QueryAsync<SolicitudAccesoModel>(
                $@"
                SELECT solicitud.perso_sol_acc_id AS Id,
                       solicitud.personal_id AS PersonalId,
                       RTRIM(personalSolicita.personal) AS PersonalCodigo,
                       ISNULL(proveedorSolicita.razon_social, '') AS PersonalNombre,
                       solicitud.fecha AS Fecha,
                       solicitud.fecha_registro AS FechaRegistro,
                       solicitud.hora_sol AS HoraSolicitada,
                       solicitud.perso_auto_id AS PersonalAutorizaId,
                       ISNULL(proveedorAutoriza.razon_social, '') AS PersonalAutorizaNombre,
                       solicitud.hora_auto AS HoraAutorizada,
                       solicitud.fecha_auto AS FechaAutoriza,
                       estado.estado_aproba_id AS EstadoAprobacionId,
                       estado.estado_aproba AS EstadoAprobacionCodigo,
                       estado.nombre AS EstadoAprobacionNombre
                {fromAndFilter}
                ORDER BY {columns[ordering.Sort]} {ordering.Dir}{(ordering.Sort == "id" ? "" : ", solicitud.perso_sol_acc_id DESC")}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    parameters.EmpresaId,
                    parameters.FechaDesde,
                    parameters.FechaHasta,
                    parameters.EstadoAprobacionId,
                    Offset = (safePage - 1) * safePageSize,
                    PageSize = safePageSize
                });

            return new SolicitudAccesoPageModel
            {
                Items = items.ToList(),
                VisibleCount = items.Count(),
                Sort = ordering.Sort,
                Dir = ordering.Dir,
                Estados = estados,
                Total = total,
                Page = safePage,
                PageSize = safePageSize,
                EstadoSeleccionadoId = selectedStateId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };
        }

        public async Task<(bool success, string message)> ResolverSolicitudAccesoAsync(
            int id,
            string? horaAutorizada,
            bool aprobar,
            int empresaId,
            int userId)
        {
            if (id <= 0)
                return (false, "Seleccione una solicitud válida.");
            if (aprobar &&
                (string.IsNullOrWhiteSpace(horaAutorizada) ||
                 !TimeOnly.TryParseExact(
                     horaAutorizada.Trim(),
                     "HH:mm",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out _)))
                return (false, "Ingrese una hora autorizada válida en formato HH:mm.");

            using var conn = _dbConnectionFactory.CreateConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                const string authorizerSql = @"
                    SELECT TOP (1) personal.personal_id
                    FROM personal
                    INNER JOIN proveedor ON proveedor.proveedor_id = personal.proveedor_id
                    INNER JOIN empresa_sede ON empresa_sede.empresa_sede_id = personal.empresa_sede_id
                    WHERE proveedor.usuario_id = @UserId
                      AND empresa_sede.empresa_id = @EmpresaId
                      AND personal.estado = 'A'
                      AND proveedor.estado = 'A'
                    ORDER BY personal.personal_id";
                var authorizerId = await conn.QueryFirstOrDefaultAsync<int?>(
                    authorizerSql,
                    new { UserId = userId, EmpresaId = empresaId },
                    transaction);
                if (!authorizerId.HasValue)
                {
                    transaction.Rollback();
                    return (false, "El usuario actual no está vinculado a un trabajador activo de la empresa.");
                }

                const string pendingSql = @"
                    SELECT solicitud.estado_aproba_id
                    FROM perso_sol_acc solicitud WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN personal solicitante ON solicitante.personal_id = solicitud.personal_id
                    INNER JOIN empresa_sede sede ON sede.empresa_sede_id = solicitante.empresa_sede_id
                    INNER JOIN estado_aproba estado ON estado.estado_aproba_id = solicitud.estado_aproba_id
                    WHERE solicitud.perso_sol_acc_id = @Id
                      AND solicitud.estado = 'A'
                      AND sede.empresa_id = @EmpresaId
                      AND estado.estado_aproba = 'P'
                      AND estado.estado = 'A'";
                var pendingStateId = await conn.QueryFirstOrDefaultAsync<int?>(
                    pendingSql,
                    new { Id = id, EmpresaId = empresaId },
                    transaction);
                if (!pendingStateId.HasValue)
                {
                    transaction.Rollback();
                    return (false, "La solicitud no existe, no pertenece a la empresa o ya fue resuelta.");
                }

                var targetCode = aprobar ? "A" : "D";
                var targetStateId = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT estado_aproba_id
                      FROM estado_aproba
                      WHERE estado_aproba = @Codigo AND estado = 'A'",
                    new { Codigo = targetCode },
                    transaction);
                if (!targetStateId.HasValue)
                {
                    transaction.Rollback();
                    return (false, $"No se encontró el estado activo '{targetCode}' para resolver solicitudes.");
                }

                const string updateSql = @"
                    UPDATE perso_sol_acc
                    SET perso_auto_id = @AuthorizerId,
                        hora_auto = CASE WHEN @Approve = 1 THEN @HoraAutorizada ELSE hora_auto END,
                        fecha_auto = GETDATE(),
                        estado_aproba_id = @TargetStateId,
                        usuario_modifica_id = @UserId,
                        fecha_modifica = GETDATE()
                    WHERE perso_sol_acc_id = @Id
                      AND estado_aproba_id = @PendingStateId
                      AND estado = 'A'";
                var changed = await conn.ExecuteAsync(
                    updateSql,
                    new
                    {
                        AuthorizerId = authorizerId.Value,
                        Approve = aprobar,
                        HoraAutorizada = aprobar ? horaAutorizada!.Trim() : string.Empty,
                        TargetStateId = targetStateId.Value,
                        UserId = userId,
                        Id = id,
                        PendingStateId = pendingStateId.Value
                    },
                    transaction);
                if (changed == 0)
                {
                    transaction.Rollback();
                    return (false, "La solicitud ya fue resuelta por otro usuario.");
                }

                transaction.Commit();
                return (true, aprobar
                    ? "Solicitud de acceso aprobada correctamente."
                    : "Solicitud de acceso desaprobada correctamente.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================================================================
        // 20. BOLETAS DE PAGO (ReporteBoleta)
        // =========================================================================
        public async Task<List<LookupItemModel>> GetPlanillasProcesadasLookupAsync(int empresaId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    pp.plan_proce_id AS Id,
                    pla.nombre + ' (' + CAST(pp.proc_anual AS VARCHAR) + ' - ' + 
                    CASE pp.mes_id 
                        WHEN 1 THEN 'Ene' WHEN 2 THEN 'Feb' WHEN 3 THEN 'Mar' WHEN 4 THEN 'Abr'
                        WHEN 5 THEN 'May' WHEN 6 THEN 'Jun' WHEN 7 THEN 'Jul' WHEN 8 THEN 'Ago'
                        WHEN 9 THEN 'Set' WHEN 10 THEN 'Oct' WHEN 11 THEN 'Nov' WHEN 12 THEN 'Dic'
                        ELSE '' END + ')' AS Nombre
                FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                WHERE pla.empresa_id = @EmpresaId AND pp.estado = 'A'
                ORDER BY pp.plan_proce_id DESC";

            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { EmpresaId = empresaId });
            return rows.ToList();
        }

        public async Task<List<LookupItemModel>> GetColaboradoresPorPlanillaProcesadaAsync(int planProceId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT 
                    p.personal_id AS Id,
                    prv.razon_social + ' (' + ISNULL(prv.num_doc_iden, '') + ')' AS Nombre
                FROM plan_calculo pc
                INNER JOIN personal p ON p.personal_id = pc.personal_id
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                WHERE pc.plan_proce_id = @PlanProceId AND pc.estado = 'A'
                ORDER BY prv.razon_social + ' (' + ISNULL(prv.num_doc_iden, '') + ')' ASC";

            var rows = await conn.QueryAsync<LookupItemModel>(sql, new { PlanProceId = planProceId });
            return rows.ToList();
        }

        public async Task<BoletaPagoModel?> GetBoletaPagoAsync(int planProceId, int personalId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sqlHeader = @"
                SELECT 
                    pp.plan_proce_id AS PlanProceId,
                    pla.nombre AS PlanillaNombre,
                    CAST(pp.proc_anual AS VARCHAR) + ' - ' + 
                    CASE pp.mes_id 
                        WHEN 1 THEN 'ENERO' WHEN 2 THEN 'FEBRERO' WHEN 3 THEN 'MARZO' WHEN 4 THEN 'ABRIL'
                        WHEN 5 THEN 'MAYO' WHEN 6 THEN 'JUNIO' WHEN 7 THEN 'JULIO' WHEN 8 THEN 'AGOSTO'
                        WHEN 9 THEN 'SETIEMBRE' WHEN 10 THEN 'OCTUBRE' WHEN 11 THEN 'NOVIEMBRE' WHEN 12 THEN 'DICIEMBRE'
                        ELSE '' END AS PeriodoTexto,
                    p.personal_id AS PersonalId,
                    p.personal AS PersonalCodigo,
                    prv.razon_social AS PersonalNombre,
                    ISNULL(prv.num_doc_iden, '') AS DocumentoIdentidad,
                    ISNULL(car.nombre, '') AS CargoNombre,
                    ISNULL(cc.nombre, '') AS CentroCostoNombre,
                    ISNULL(rp.nombre, '') AS RegimenPensionario,
                    ISNULL(p.num_cuspp, '') AS Cuspp,
                    p.fecha_ing AS FechaIngreso
                FROM plan_proce pp
                INNER JOIN planilla pla ON pla.planilla_id = pp.planilla_id
                INNER JOIN personal p ON p.personal_id = @PersonalId
                INNER JOIN proveedor prv ON prv.proveedor_id = p.proveedor_id
                LEFT JOIN cargo_perso car ON car.cargo_perso_id = p.cargo_perso_id
                LEFT JOIN centro_costo cc ON cc.centro_costo_id = p.centro_costo_id
                LEFT JOIN reg_pension rp ON rp.reg_pension_id = p.reg_pension_id
                WHERE pp.plan_proce_id = @PlanProceId";

            var boleta = await conn.QueryFirstOrDefaultAsync<BoletaPagoModel>(sqlHeader, new {
                PlanProceId = planProceId,
                PersonalId = personalId
            });

            if (boleta == null) return null;

            const string sqlConceptos = @"
                SELECT 
                    cr.concep_remu AS Codigo,
                    cr.nombre AS Descripcion,
                    cr.tipo_con_remu_id AS TipoId,
                    pc.horas AS Horas,
                    pc.monto AS Monto
                FROM plan_calculo pc
                INNER JOIN concep_remu cr ON cr.concep_remu_id = pc.concep_remu_id
                WHERE pc.plan_proce_id = @PlanProceId 
                  AND pc.personal_id = @PersonalId 
                  AND pc.estado = 'A'
                ORDER BY cr.tipo_con_remu_id, cr.concep_remu";

            var items = (await conn.QueryAsync<dynamic>(sqlConceptos, new {
                PlanProceId = planProceId,
                PersonalId = personalId
            })).ToList();

            foreach (var item in items)
            {
                int tipoId = (int)item.TipoId;
                var modelItem = new BoletaItemConceptoModel {
                    Codigo = item.Codigo?.ToString() ?? "",
                    Descripcion = item.Descripcion?.ToString() ?? "",
                    Horas = (decimal)item.Horas,
                    Monto = (decimal)item.Monto
                };

                if (tipoId == 1) // REMUNERACION
                {
                    boleta.Ingresos.Add(modelItem);
                }
                else if (tipoId == 3) // RETENCION
                {
                    boleta.Descuentos.Add(modelItem);
                }
                else if (tipoId == 2) // APORTACION
                {
                    boleta.AportesEmpleador.Add(modelItem);
                }
            }

            boleta.NetoEnLetras = NumeroALetras(boleta.NetoPagar);

            return boleta;
        }

        public async Task<List<BoletaPagoModel>> GetBoletasPorPlanillaAsync(int planProceId)
        {
            using var conn = _dbConnectionFactory.CreateConnection();
            const string sqlPersonalIds = @"
                SELECT DISTINCT personal_id 
                FROM plan_calculo 
                WHERE plan_proce_id = @PlanProceId AND estado = 'A'";

            var personalIds = (await conn.QueryAsync<int>(sqlPersonalIds, new { PlanProceId = planProceId })).ToList();
            var list = new List<BoletaPagoModel>();

            foreach (var pId in personalIds)
            {
                var b = await GetBoletaPagoAsync(planProceId, pId);
                if (b != null) list.Add(b);
            }

            return list;
        }

        private static string NumeroALetras(decimal numero)
        {
            long entero = (long)Math.Truncate(numero);
            int centavos = (int)Math.Round((numero - entero) * 100);
            if (centavos < 0) centavos = 0;

            string letras = ConvertirEnteroALetras(entero);
            return $"{letras} CON {centavos:D2}/100 SOLES";
        }

        private static string ConvertirEnteroALetras(long v)
        {
            if (v == 0) return "CERO";
            if (v < 0) return "MENOS " + ConvertirEnteroALetras(Math.Abs(v));

            if (v == 100) return "CIEN";
            if (v < 1000)
            {
                if (v <= 20)
                {
                    return v switch
                    {
                        1 => "UNO", 2 => "DOS", 3 => "TRES", 4 => "CUATRO", 5 => "CINCO",
                        6 => "SEIS", 7 => "SIETE", 8 => "OCHO", 9 => "NUEVE", 10 => "DIEZ",
                        11 => "ONCE", 12 => "DOCE", 13 => "TRECE", 14 => "CATORCE", 15 => "QUINCE",
                        16 => "DIECISÉIS", 17 => "DIECISIETE", 18 => "DIECIOCHO", 19 => "DIECINUEVE",
                        20 => "VEINTE", _ => ""
                    };
                }
                if (v < 30) return "VEINTI" + ConvertirEnteroALetras(v - 20);
                if (v < 100)
                {
                    string d = (v / 10) switch
                    {
                        3 => "TREINTA", 4 => "CUARENTA", 5 => "CINCUENTA",
                        6 => "SESENTA", 7 => "SETENTA", 8 => "OCHENTA", 9 => "NOVENTA", _ => ""
                    };
                    return (v % 10 == 0) ? d : $"{d} Y {ConvertirEnteroALetras(v % 10)}";
                }
                string c = (v / 100) switch
                {
                    1 => "CIENTO", 2 => "DOSCIENTOS", 3 => "TRESCIENTOS", 4 => "CUATROCIENTOS",
                    5 => "QUINIENTOS", 6 => "SEISCIENTOS", 7 => "SETECIENTOS", 8 => "OCHOCIENTOS",
                    9 => "NOVECIENTOS", _ => ""
                };
                return (v % 100 == 0) ? c : $"{c} {ConvertirEnteroALetras(v % 100)}";
            }
            if (v < 1000000)
            {
                long miles = v / 1000;
                long resto = v % 1000;
                string t = (miles == 1) ? "MIL" : $"{ConvertirEnteroALetras(miles)} MIL";
                return (resto == 0) ? t : $"{t} {ConvertirEnteroALetras(resto)}";
            }
            long millones = v / 1000000;
            long restoMillones = v % 1000000;
            string m = (millones == 1) ? "UN MILLÓN" : $"{ConvertirEnteroALetras(millones)} MILLONES";
            return (restoMillones == 0) ? m : $"{m} {ConvertirEnteroALetras(restoMillones)}";
        }
    }
}
