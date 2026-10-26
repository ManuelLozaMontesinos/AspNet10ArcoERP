namespace AspNet10ArcoERP.Models
{
    public class PeriodoSistemaModel
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public bool HabilitarProcesoBatch { get; set; }
        public string Estado { get; set; } = "A";
    }

    public class PeriodoSistemaPageModel
    {
        public List<PeriodoSistemaModel> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int? SearchYear { get; set; }
    }
}
