using System;
using System.Collections.Generic;

namespace AspNet10ArcoERP.Models
{
    public class CalendarioAreaModel
    {
        public int AreaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Color { get; set; } = "#0d6efd";
        public int Orden { get; set; } = 1;
        public string Estado { get; set; } = "A";
        public int TotalUsuarios { get; set; }
        public int TotalEventos { get; set; }
        public string FechaCreacion { get; set; } = string.Empty;
    }

    public class CalendarioEventoModel
    {
        public int EventoId { get; set; }
        public int AreaId { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string UsuarioLogon { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool TodoElDia { get; set; }
        public string Color { get; set; } = "#0d6efd";
        public string Estado { get; set; } = "A";
        public string RecordatorioEstado { get; set; } = "P";
        public bool PuedeEditar { get; set; } = true;
        public string FechaCreacionFormateada { get; set; } = string.Empty;
    }

    public class CalendarioEtiquetaModel
    {
        public int SlotIndex { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Color { get; set; } = "#0d6efd";
    }

    public class EventoInputModel
    {
        public int? Id { get; set; }
        public int? AreaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public bool TodoElDia { get; set; }
        public string Color { get; set; } = "#0d6efd";
    }

    public class MoverEventoInputModel
    {
        public int Id { get; set; }
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public bool TodoElDia { get; set; }
    }

    public class EtiquetaInputModel
    {
        public int? AreaId { get; set; }
        public int SlotIndex { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Color { get; set; } = "#0d6efd";
    }

    public class AreaAdminInputModel
    {
        public int AreaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Color { get; set; } = "#0d6efd";
        public int Orden { get; set; } = 1;
        public string Estado { get; set; } = "A";
    }

    public class UsuarioAreaModel
    {
        public int UsuarioId { get; set; }
        public string UsuarioLogon { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
        public string Roles { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
    }

    public class NotificacionDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string FechaFormateada => FechaRegistro.ToString("dd/MM/yyyy HH:mm");
        public string Lectura { get; set; } = "N";
        public int UsuarioId { get; set; }
    }
}

