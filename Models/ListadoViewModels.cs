namespace AspNet10ArcoERP.Models;

public class ListadoPaginadoModel
{
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int VisibleCount { get; set; }
    public string Sort { get; set; } = "id";
    public string Dir { get; set; } = "asc";
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public int FirstRecord => VisibleCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastRecord => VisibleCount == 0 ? 0 : FirstRecord + VisibleCount - 1;

    public void Paginar(int total, int page, int pageSize)
    {
        Total = total;
        PageSize = pageSize is 25 or 50 or 100 or 500 ? pageSize : 25;
        Page = Math.Clamp(page, 1, PageCount);
    }

    public void Ordenar(string? sort, string? dir, IEnumerable<string> fields, string defaultSort, string defaultDir = "asc")
    {
        var key = sort?.Trim().ToLowerInvariant();
        Sort = key is not null && fields.Contains(key) ? key : defaultSort;
        Dir = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc"
            : string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : defaultDir;
    }
}
