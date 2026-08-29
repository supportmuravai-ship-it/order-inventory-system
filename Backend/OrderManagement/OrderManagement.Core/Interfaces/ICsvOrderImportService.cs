using OrderManagement.Core.DTOs.Orders;

namespace OrderManagement.Core.Interfaces;

public interface ICsvOrderImportService
{
    Task<CsvImportResultDto> ImportAsync(
        int storeId,
        Stream csvStream,
        CancellationToken cancellationToken = default);
}