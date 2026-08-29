using CsvHelper;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.DTOs.Orders;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;
using OrderManagement.Core.Interfaces;
using OrderManagement.Infrastructure.Data;
using System.Globalization;

namespace OrderManagement.Infrastructure.Services;

public class CsvOrderImportService : ICsvOrderImportService
{
    private readonly AppDbContext _dbContext;

    private static readonly string[] RequiredHeaders =
    [
        "OrderId",
        "Date",
        "FullName",
        "Phone",
        "Address1",
        "City",
        "ProductName",
        "Price",
        "Variant",
        "SKU",
        "Quantity",
        "TotalPrice"
    ];

    public CsvOrderImportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CsvImportResultDto> ImportAsync(
        int storeId,
        Stream csvStream,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvImportResultDto();

        using var reader = new StreamReader(csvStream);

        using var csv = new CsvReader(
            reader,
            CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync())
        {
            result.Errors.Add(new CsvImportErrorDto
            {
                Message = "CSV file is empty."
            });

            return result;
        }

        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? [];

        var headerLookup = headers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToDictionary(
                x => x.Trim(),
                x => x,
                StringComparer.OrdinalIgnoreCase);

        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headerLookup.ContainsKey(requiredHeader))
            {
                result.Errors.Add(new CsvImportErrorDto
                {
                    Message =
                        $"Missing required column: {requiredHeader}"
                });
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var rows = new List<ParsedCsvRow>();

        var rowNumber = 1;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            rowNumber++;
            result.TotalRows++;

            string GetValue(string header)
            {
                return csv.GetField(headerLookup[header])
                    ?.Trim() ?? string.Empty;
            }

            var row = new ParsedCsvRow
            {
                RowNumber = rowNumber,
                DisplayOrderId = GetValue("OrderId"),
                FullName = GetValue("FullName"),
                Phone = GetValue("Phone"),
                Address1 = GetValue("Address1"),
                City = GetValue("City"),
                ProductName = GetValue("ProductName"),

                Variant = string.IsNullOrWhiteSpace(
                    GetValue("Variant"))
                    ? null
                    : GetValue("Variant"),

                SKU = string.IsNullOrWhiteSpace(
                    GetValue("SKU"))
                    ? null
                    : GetValue("SKU")
            };

            ValidateRequiredText(
                row.DisplayOrderId,
                "OrderId",
                row);

            ValidateRequiredText(
                row.FullName,
                "FullName",
                row);

            ValidateRequiredText(
                row.Phone,
                "Phone",
                row);

            ValidateRequiredText(
                row.Address1,
                "Address1",
                row);

            ValidateRequiredText(
                row.City,
                "City",
                row);

            ValidateRequiredText(
                row.ProductName,
                "ProductName",
                row);

            var dateText = GetValue("Date");

            if (!DateTime.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal |
                    DateTimeStyles.AdjustToUniversal,
                    out var orderDate))
            {
                row.Errors.Add("Date is invalid.");
            }
            else
            {
                row.OrderDateUtc = orderDate;
            }

            var priceText = GetValue("Price");

            if (!decimal.TryParse(
                    priceText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var price) ||
                price < 0)
            {
                row.Errors.Add(
                    "Price must be a number greater than or equal to 0.");
            }
            else
            {
                row.UnitPrice = price;
            }

            var quantityText = GetValue("Quantity");

            if (!int.TryParse(
                    quantityText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var quantity) ||
                quantity <= 0)
            {
                row.Errors.Add(
                    "Quantity must be greater than 0.");
            }
            else
            {
                row.Quantity = quantity;
            }

            var totalPriceText = GetValue("TotalPrice");

            if (!decimal.TryParse(
                    totalPriceText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var totalPrice) ||
                totalPrice < 0)
            {
                row.Errors.Add(
                    "TotalPrice must be a number greater than or equal to 0.");
            }
            else
            {
                row.TotalPrice = totalPrice;
            }

            rows.Add(row);

            foreach (var error in row.Errors)
            {
                result.Errors.Add(new CsvImportErrorDto
                {
                    RowNumber = row.RowNumber,
                    DisplayOrderId =
                        string.IsNullOrWhiteSpace(row.DisplayOrderId)
                            ? null
                            : row.DisplayOrderId,
                    Message = error
                });
            }

            if (row.Errors.Count > 0)
            {
                result.Failed++;
            }
        }

        /*
         * Rows without OrderId cannot safely belong to an order,
         * so they are already represented by their row errors above.
         */
        var groupedOrders = rows
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.DisplayOrderId))
            .GroupBy(
                x => x.DisplayOrderId,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var importedOrderIds = groupedOrders
            .Select(x => x.Key)
            .ToList();

        var existingOrderIds = await _dbContext.Orders
            .AsNoTracking()
            .Where(x =>
                x.StoreId == storeId &&
                importedOrderIds.Contains(x.DisplayOrderId))
            .Select(x => x.DisplayOrderId)
            .ToListAsync(cancellationToken);

        var existingOrderIdSet =
            new HashSet<string>(
                existingOrderIds,
                StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;

        foreach (var group in groupedOrders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupRows = group.ToList();

            /*
             * Never create a partially valid multi-item order.
             */
            if (groupRows.Any(x => x.Errors.Count > 0))
            {
                result.Skipped++;

                continue;
            }

            if (existingOrderIdSet.Contains(group.Key))
            {
                result.Duplicates++;

                result.Errors.Add(new CsvImportErrorDto
                {
                    DisplayOrderId = group.Key,
                    Message =
                        "Duplicate order. Existing order was not changed."
                });

                continue;
            }

            var first = groupRows[0];

            if (!HasConsistentOrderData(groupRows, first))
            {
                result.Skipped++;

                result.Errors.Add(new CsvImportErrorDto
                {
                    DisplayOrderId = group.Key,
                    Message =
                        "Rows for this OrderId contain inconsistent order-level data."
                });

                continue;
            }

            var customer = new Customer
            {
                StoreId = storeId,

                ExternalCustomerId = null,

                FullName = first.FullName,

                Phone = first.Phone,

                AddressLine1 = first.Address1,

                City = first.City,

                // Existing business default in Customer entity.
                Country = "UAE",

                CreatedAtUtc = now,

                UpdatedAtUtc = now
            };

            var order = new Order
            {
                StoreId = storeId,

                ExternalOrderId = null,

                DisplayOrderId = group.Key,

                OrderSource = OrderSource.CSVImport,

                Customer = customer,

                OrderStatus = OrderStatus.New,

                TrackingNumber = null,

                LocationLink = null,

                FinalDecision = null,

                InvoiceStatus = InvoiceStatus.Unpaid,

                TotalAmount = first.TotalPrice,

                Currency = "AED",

                OrderDateUtc = first.OrderDateUtc,

                WarehouseLocationId = null,

                LastStatusChangedAtUtc = now,

                CreatedAtUtc = now,

                UpdatedAtUtc = now
            };

            foreach (var csvRow in groupRows)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductName = csvRow.ProductName,

                    VariantName = csvRow.Variant,

                    SKU = csvRow.SKU,

                    Quantity = csvRow.Quantity,

                    UnitPrice = csvRow.UnitPrice,

                    LineTotal =
                        csvRow.Quantity *
                        csvRow.UnitPrice,

                    CreatedAtUtc = now,

                    UpdatedAtUtc = now
                });
            }

            _dbContext.Orders.Add(order);

            existingOrderIdSet.Add(group.Key);

            result.ImportedOrders++;
        }

        if (result.ImportedOrders > 0)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return result;
    }

    private static void ValidateRequiredText(
        string value,
        string fieldName,
        ParsedCsvRow row)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            row.Errors.Add(
                $"{fieldName} is required.");
        }
    }

    private static bool HasConsistentOrderData(
        List<ParsedCsvRow> rows,
        ParsedCsvRow first)
    {
        return rows.All(x =>
            string.Equals(
                x.FullName,
                first.FullName,
                StringComparison.OrdinalIgnoreCase) &&

            string.Equals(
                x.Phone,
                first.Phone,
                StringComparison.OrdinalIgnoreCase) &&

            string.Equals(
                x.Address1,
                first.Address1,
                StringComparison.OrdinalIgnoreCase) &&

            string.Equals(
                x.City,
                first.City,
                StringComparison.OrdinalIgnoreCase) &&

            x.OrderDateUtc == first.OrderDateUtc &&

            x.TotalPrice == first.TotalPrice);
    }

    private sealed class ParsedCsvRow
    {
        public int RowNumber { get; set; }

        public string DisplayOrderId { get; set; }
            = string.Empty;

        public DateTime OrderDateUtc { get; set; }

        public string FullName { get; set; }
            = string.Empty;

        public string Phone { get; set; }
            = string.Empty;

        public string Address1 { get; set; }
            = string.Empty;

        public string City { get; set; }
            = string.Empty;

        public string ProductName { get; set; }
            = string.Empty;

        public string? Variant { get; set; }

        public string? SKU { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public List<string> Errors { get; } = [];
    }
}