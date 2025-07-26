namespace HL7Processor.Application.DTOs;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalItems { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    public PagedResult()
    {
    }

    public PagedResult(IEnumerable<T> items, int totalItems, int pageNumber, int pageSize)
    {
        Items = items.ToList();
        TotalItems = totalItems;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
    }
}