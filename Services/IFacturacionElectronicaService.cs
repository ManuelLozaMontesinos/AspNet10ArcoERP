using System;
using System.Threading.Tasks;
using AspNet10ArcoERP.Models;

namespace AspNet10ArcoERP.Services;

public interface IFacturacionElectronicaService
{
    FacturacionElectronicaOpciones GetOpciones();
    Task<ResponseDTO> GenerarEnviarDocumentoAsync(int comprobanteId, string tipoDoc, string envioEnLinea, int userId);
    Task<ResponseDTO> EnviarDocumentoAsync(int comprobanteId, string tipoDoc, int userId);
    Task<ResponseDTO> ActualizarCdrAsync(int comprobanteId, string tipoDoc, int userId);
    Task<string?> ObtenerXmlAsync(int comprobanteId, string tipoDoc);
    Task<string?> ObtenerCdrAsync(int comprobanteId, string tipoDoc);
    Task<ResponseDTO> GenerarResumenDiarioAsync(int empresaId, DateTime fecha, int userId);
}

