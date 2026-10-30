using AspNet10ArcoERP.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNet10ArcoERP.TagHelpers;

public static class ListadoEnlaces
{
    public static string Url(HttpRequest request, params (string Key, string? Value)[] changes)
    {
        var query = request.Query.ToDictionary(item => item.Key, item => (string?)item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        foreach (var change in changes)
            query[change.Key] = change.Value;
        return QueryHelpers.AddQueryString(request.PathBase.Add(request.Path).Value ?? "/", query);
    }
}

[HtmlTargetElement("th", Attributes = "erp-sort")]
public class ListadoOrdenTagHelper : TagHelper
{
    public string ErpSort { get; set; } = "";
    public bool ErpNumeric { get; set; }
    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext.ViewData.Model is not ListadoPaginadoModel model)
            throw new InvalidOperationException("El listado debe proporcionar su paginación y orden.");
        var active = model.Sort == ErpSort;
        var ascending = active && model.Dir == "asc";
        output.Attributes.RemoveAll("erp-sort");
        output.Attributes.RemoveAll("erp-numeric");
        output.Attributes.SetAttribute("aria-sort", active ? ascending ? "ascending" : "descending" : "none");
        var label = await output.GetChildContentAsync();
        var link = new TagBuilder("a");
        link.Attributes["href"] = ListadoEnlaces.Url(ViewContext.HttpContext.Request,
            ("sort", ErpSort), ("dir", ascending ? "desc" : "asc"), ("page", "1"), ("top", model.PageSize.ToString()));
        link.AddCssClass("text-decoration-none text-body fw-semibold d-inline-flex align-items-center gap-1");
        link.InnerHtml.AppendHtml(label);
        var icon = new TagBuilder("i");
        icon.AddCssClass("bi " + (!active ? "bi-arrow-down-up text-muted opacity-50" :
            ErpNumeric ? ascending ? "bi-sort-numeric-down text-primary" : "bi-sort-numeric-down-alt text-primary" :
            ascending ? "bi-sort-alpha-down text-primary" : "bi-sort-alpha-down-alt text-primary"));
        icon.Attributes["aria-hidden"] = "true";
        link.InnerHtml.AppendHtml(icon);
        output.Content.SetHtmlContent(link);
    }
}
