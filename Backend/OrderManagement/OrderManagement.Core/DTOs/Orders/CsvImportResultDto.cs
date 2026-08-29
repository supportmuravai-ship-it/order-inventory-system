namespace OrderManagement.Core.DTOs.Orders;

public class CsvImportResultDto
{
    public int TotalRows { get; set; }

    public int ImportedOrders { get; set; }

    public int Duplicates { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public List<CsvImportErrorDto> Errors { get; set; } = [];
}

public class CsvImportErrorDto
{
    public int? RowNumber { get; set; }

    public string? DisplayOrderId { get; set; }

    public string Message { get; set; } = string.Empty;
}