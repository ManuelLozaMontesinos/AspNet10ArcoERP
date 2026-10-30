namespace AspNet10ArcoERP.Models
{
    public class PeriodoSistemaModel
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public bool HabilitarProcesoBatch { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class PeriodoSistemaPageModel : ListadoPaginadoModel
    {
        public List<PeriodoSistemaModel> Items { get; set; } = new();
        public int? SearchYear { get; set; }
    }
}
