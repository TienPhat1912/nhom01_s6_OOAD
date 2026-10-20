using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.Common.Models;

public class PageQuery
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
