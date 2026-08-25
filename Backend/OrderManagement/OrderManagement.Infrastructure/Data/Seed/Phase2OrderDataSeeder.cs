using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Entities;
using OrderManagement.Core.Enums;

namespace OrderManagement.Infrastructure.Data.Seed;

public static class Phase2OrderDataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        // Prevent duplicate development orders every time the API starts.
        if (await dbContext.Orders.AnyAsync())
        {
            return;
        }

        var uaeStore = await dbContext.Stores
            .SingleAsync(x => x.Code == "UAE");

        var testStore = await dbContext.Stores
            .SingleAsync(x => x.Code == "TEST");

        // -------------------------
        // Warehouse Locations
        // -------------------------

        var uaeWarehouse = await dbContext.WarehouseLocations
            .SingleOrDefaultAsync(x => x.Code == "DXB-01");

        if (uaeWarehouse is null)
        {
            uaeWarehouse = new WarehouseLocation
            {
                Name = "Dubai Main Warehouse",
                Code = "DXB-01",
                Country = "UAE",
                City = "Dubai",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.WarehouseLocations.Add(uaeWarehouse);
        }

        var testWarehouse = await dbContext.WarehouseLocations
            .SingleOrDefaultAsync(x => x.Code == "DXB-TEST");

        if (testWarehouse is null)
        {
            testWarehouse = new WarehouseLocation
            {
                Name = "Test Warehouse",
                Code = "DXB-TEST",
                Country = "UAE",
                City = "Dubai",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.WarehouseLocations.Add(testWarehouse);
        }

        await dbContext.SaveChangesAsync();

        // -------------------------
        // Customers
        // -------------------------

        var customers = new[]
        {
            CreateCustomer(
                uaeStore.Id,
                "gid://shopify/Customer/700001",
                "Ahmed Khan",
                "+971501112233",
                "Marina Promenade",
                "Dubai"),

            CreateCustomer(
                uaeStore.Id,
                "gid://shopify/Customer/700002",
                "Fatima Ali",
                "+971522223344",
                "Al Nahda Street",
                "Sharjah"),

            CreateCustomer(
                uaeStore.Id,
                "gid://shopify/Customer/700003",
                "Omar Hassan",
                "+971543334455",
                "Khalifa City",
                "Abu Dhabi"),

            CreateCustomer(
                uaeStore.Id,
                "gid://shopify/Customer/700004",
                "Sara Mohammed",
                "+971504445566",
                "Business Bay",
                "Dubai"),

            CreateCustomer(
                uaeStore.Id,
                null,
                "Ayesha Malik",
                "+971555556677",
                "Al Majaz",
                "Sharjah"),

            CreateCustomer(
                uaeStore.Id,
                null,
                "Bilal Ahmed",
                "+971506667788",
                "International City",
                "Dubai"),

            CreateCustomer(
                uaeStore.Id,
                "gid://shopify/Customer/700007",
                "Hassan Raza",
                "+971527778899",
                "Al Barsha 1",
                "Dubai"),

            CreateCustomer(
                uaeStore.Id,
                null,
                "Mariam Noor",
                "+971548889900",
                "Al Reem Island",
                "Abu Dhabi"),

            CreateCustomer(
                testStore.Id,
                "gid://shopify/Customer/800001",
                "Test Customer One",
                "+971501234001",
                "Jumeirah Village Circle",
                "Dubai"),

            CreateCustomer(
                testStore.Id,
                null,
                "Test Customer Two",
                "+971501234002",
                "Al Qusais",
                "Dubai"),

            CreateCustomer(
                testStore.Id,
                "gid://shopify/Customer/800003",
                "Test Customer Three",
                "+971501234003",
                "Al Taawun",
                "Sharjah"),

            CreateCustomer(
                testStore.Id,
                null,
                "Test Customer Four",
                "+971501234004",
                "Corniche Road",
                "Abu Dhabi")
        };

        dbContext.Customers.AddRange(customers);

        await dbContext.SaveChangesAsync();

        // -------------------------
        // UAE Store Orders
        // -------------------------

        var orders = new List<Order>
        {
            CreateOrder(
                uaeStore.Id,
                customers[0].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900001",
                "#1132",
                OrderSource.Shopify,
                OrderStatus.Confirmed,
                InvoiceStatus.Unpaid,
                149m,
                daysAgo: 1,
                trackingNumber: null,
                locationLink: "https://maps.google.com/?q=Dubai+Marina",
                finalDecision: "Call customer"),

            CreateOrder(
                uaeStore.Id,
                customers[1].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900002",
                "#1133",
                OrderSource.Shopify,
                OrderStatus.Shipped,
                InvoiceStatus.Unpaid,
                199m,
                daysAgo: 2,
                trackingNumber: "TRV100001",
                locationLink: "https://maps.google.com/?q=Al+Nahda+Sharjah",
                finalDecision: "Approved"),

            CreateOrder(
                uaeStore.Id,
                customers[2].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900003",
                "#1134",
                OrderSource.Shopify,
                OrderStatus.Delivered,
                InvoiceStatus.Paid,
                298m,
                daysAgo: 4,
                trackingNumber: "TRV100002",
                locationLink: "https://maps.google.com/?q=Khalifa+City",
                finalDecision: "Completed"),

            CreateOrder(
                uaeStore.Id,
                customers[3].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900004",
                "#1135",
                OrderSource.Shopify,
                OrderStatus.NoResponse,
                InvoiceStatus.Unpaid,
                119m,
                daysAgo: 1,
                trackingNumber: null,
                locationLink: "https://maps.google.com/?q=Business+Bay",
                finalDecision: "Try again"),

            CreateOrder(
                uaeStore.Id,
                customers[4].Id,
                uaeWarehouse.Id,
                null,
                "WA05",
                OrderSource.WhatsApp,
                OrderStatus.Confirmed,
                InvoiceStatus.Unpaid,
                149m,
                daysAgo: 0,
                trackingNumber: null,
                locationLink: "https://maps.google.com/?q=Al+Majaz",
                finalDecision: "Confirmed on WhatsApp"),

            CreateOrder(
                uaeStore.Id,
                customers[5].Id,
                uaeWarehouse.Id,
                null,
                "WA06",
                OrderSource.WhatsApp,
                OrderStatus.Cancelled,
                InvoiceStatus.Unpaid,
                199m,
                daysAgo: 3,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Customer cancelled"),

            CreateOrder(
                uaeStore.Id,
                customers[6].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900007",
                "#1138",
                OrderSource.Shopify,
                OrderStatus.ReturnInProcess,
                InvoiceStatus.Unpaid,
                229m,
                daysAgo: 6,
                trackingNumber: "TRV100007",
                locationLink: "https://maps.google.com/?q=Al+Barsha",
                finalDecision: "Awaiting return"),

            CreateOrder(
                uaeStore.Id,
                customers[7].Id,
                uaeWarehouse.Id,
                null,
                "CSV-1001",
                OrderSource.CSVImport,
                OrderStatus.Confirmed,
                InvoiceStatus.Unpaid,
                179m,
                daysAgo: 1,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Imported"),

            CreateOrder(
                uaeStore.Id,
                customers[0].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900009",
                "#1140",
                OrderSource.Shopify,
                OrderStatus.RepeatedOrder,
                InvoiceStatus.Unpaid,
                149m,
                daysAgo: 2,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Check duplicate customer"),

            CreateOrder(
                uaeStore.Id,
                customers[1].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900010",
                "#1141",
                OrderSource.Shopify,
                OrderStatus.Return,
                InvoiceStatus.Unpaid,
                249m,
                daysAgo: 8,
                trackingNumber: "TRV100010",
                locationLink: null,
                finalDecision: "Returned"),

            CreateOrder(
                uaeStore.Id,
                customers[2].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900011",
                "#1142",
                OrderSource.Shopify,
                OrderStatus.Shipped,
                InvoiceStatus.Unpaid,
                318m,
                daysAgo: 2,
                trackingNumber: "TRV100011",
                locationLink: null,
                finalDecision: "Dispatched"),

            CreateOrder(
                uaeStore.Id,
                customers[3].Id,
                uaeWarehouse.Id,
                null,
                "OTHER-01",
                OrderSource.Other,
                OrderStatus.Confirmed,
                InvoiceStatus.Paid,
                99m,
                daysAgo: 0,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Special order"),

            CreateOrder(
                uaeStore.Id,
                customers[4].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900013",
                "#1144",
                OrderSource.Shopify,
                OrderStatus.Delivered,
                InvoiceStatus.Paid,
                149m,
                daysAgo: 10,
                trackingNumber: "TRV100013",
                locationLink: null,
                finalDecision: "Completed"),

            CreateOrder(
                uaeStore.Id,
                customers[5].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900014",
                "#1145",
                OrderSource.Shopify,
                OrderStatus.NoResponse,
                InvoiceStatus.Unpaid,
                199m,
                daysAgo: 1,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Call tomorrow"),

            CreateOrder(
                uaeStore.Id,
                customers[6].Id,
                uaeWarehouse.Id,
                null,
                "CSV-1002",
                OrderSource.CSVImport,
                OrderStatus.Confirmed,
                InvoiceStatus.Unpaid,
                258m,
                daysAgo: 3,
                trackingNumber: null,
                locationLink: null,
                finalDecision: "Ready to process"),

            CreateOrder(
                uaeStore.Id,
                customers[7].Id,
                uaeWarehouse.Id,
                "gid://shopify/Order/900016",
                "#1147",
                OrderSource.Shopify,
                OrderStatus.Shipped,
                InvoiceStatus.Unpaid,
                149m,
                daysAgo: 2,
                trackingNumber: "TRV100016",
                locationLink: null,
                finalDecision: "In transit")
        };

        // -------------------------
        // Test Store Orders
        // -------------------------

        orders.AddRange(
        [
            CreateOrder(
                testStore.Id,
                customers[8].Id,
                testWarehouse.Id,
                "gid://shopify/Order/910001",
                "#2001",
                OrderSource.Shopify,
                OrderStatus.Confirmed,
                InvoiceStatus.Unpaid,
                149m,
                1,
                null,
                null,
                "Test"),

            CreateOrder(
                testStore.Id,
                customers[9].Id,
                testWarehouse.Id,
                null,
                "WA-T01",
                OrderSource.WhatsApp,
                OrderStatus.NoResponse,
                InvoiceStatus.Unpaid,
                199m,
                2,
                null,
                null,
                "Test follow-up"),

            CreateOrder(
                testStore.Id,
                customers[10].Id,
                testWarehouse.Id,
                "gid://shopify/Order/910003",
                "#2003",
                OrderSource.Shopify,
                OrderStatus.Delivered,
                InvoiceStatus.Paid,
                298m,
                5,
                "TESTTRK003",
                null,
                "Completed"),

            CreateOrder(
                testStore.Id,
                customers[11].Id,
                testWarehouse.Id,
                null,
                "CSV-T01",
                OrderSource.CSVImport,
                OrderStatus.Cancelled,
                InvoiceStatus.Unpaid,
                119m,
                3,
                null,
                null,
                "Cancelled"),

            CreateOrder(
                testStore.Id,
                customers[8].Id,
                testWarehouse.Id,
                "gid://shopify/Order/910005",
                "#2005",
                OrderSource.Shopify,
                OrderStatus.Shipped,
                InvoiceStatus.Unpaid,
                229m,
                2,
                "TESTTRK005",
                null,
                "In transit"),

            CreateOrder(
                testStore.Id,
                customers[9].Id,
                testWarehouse.Id,
                null,
                "OTHER-T01",
                OrderSource.Other,
                OrderStatus.Confirmed,
                InvoiceStatus.Paid,
                149m,
                1,
                null,
                null,
                "Test order"),

            CreateOrder(
                testStore.Id,
                customers[10].Id,
                testWarehouse.Id,
                "gid://shopify/Order/910007",
                "#2007",
                OrderSource.Shopify,
                OrderStatus.ReturnInProcess,
                InvoiceStatus.Unpaid,
                199m,
                7,
                "TESTTRK007",
                null,
                "Return requested"),

            CreateOrder(
                testStore.Id,
                customers[11].Id,
                testWarehouse.Id,
                "gid://shopify/Order/910008",
                "#2008",
                OrderSource.Shopify,
                OrderStatus.RepeatedOrder,
                InvoiceStatus.Unpaid,
                149m,
                1,
                null,
                null,
                "Duplicate check")
        ]);

        dbContext.Orders.AddRange(orders);

        await dbContext.SaveChangesAsync();

        // -------------------------
        // Order Items
        // -------------------------

        for (var i = 0; i < orders.Count; i++)
        {
            var order = orders[i];

            switch (i % 5)
            {
                case 0:
                    AddItem(
                        dbContext,
                        order,
                        "Smart Automatic Robot Mop",
                        "White",
                        "RM-WHT-01",
                        1,
                        149m);
                    break;

                case 1:
                    AddItem(
                        dbContext,
                        order,
                        "Ergonomic Memory Foam Pillow",
                        "Standard",
                        "PIL-STD-01",
                        1,
                        199m);
                    break;

                case 2:
                    AddItem(
                        dbContext,
                        order,
                        "Filtered Shower Head",
                        "Chrome",
                        "SHR-CHR-01",
                        2,
                        order.TotalAmount / 2);
                    break;

                case 3:
                    AddItem(
                        dbContext,
                        order,
                        "7-Day Pill Organizer",
                        "Black",
                        "PILL-BLK-01",
                        1,
                        order.TotalAmount);
                    break;

                default:
                    // Multi-product order
                    AddItem(
                        dbContext,
                        order,
                        "Smart Automatic Robot Mop",
                        "White",
                        "RM-WHT-01",
                        1,
                        149m);

                    AddItem(
                        dbContext,
                        order,
                        "Reusable Mop Pad Set",
                        "2 Pack",
                        "PAD-2PK-01",
                        1,
                        Math.Max(0, order.TotalAmount - 149m));
                    break;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static Customer CreateCustomer(
        int storeId,
        string? externalCustomerId,
        string fullName,
        string phone,
        string addressLine1,
        string city)
    {
        return new Customer
        {
            StoreId = storeId,
            ExternalCustomerId = externalCustomerId,
            FullName = fullName,
            Phone = phone,
            AddressLine1 = addressLine1,
            City = city,
            Country = "UAE",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Order CreateOrder(
        int storeId,
        int customerId,
        int warehouseLocationId,
        string? externalOrderId,
        string displayOrderId,
        OrderSource orderSource,
        OrderStatus orderStatus,
        InvoiceStatus invoiceStatus,
        decimal totalAmount,
        int daysAgo,
        string? trackingNumber,
        string? locationLink,
        string? finalDecision)
    {
        var orderDate = DateTime.UtcNow
            .AddDays(-daysAgo)
            .AddHours(-2);

        return new Order
        {
            StoreId = storeId,
            CustomerId = customerId,
            WarehouseLocationId = warehouseLocationId,

            ExternalOrderId = externalOrderId,
            DisplayOrderId = displayOrderId,

            OrderSource = orderSource,
            OrderStatus = orderStatus,

            TrackingNumber = trackingNumber,
            LocationLink = locationLink,
            FinalDecision = finalDecision,

            InvoiceStatus = invoiceStatus,
            TotalAmount = totalAmount,
            Currency = "AED",

            OrderDateUtc = orderDate,
            LastStatusChangedAtUtc = orderDate,

            CreatedAtUtc = orderDate,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static void AddItem(
        AppDbContext dbContext,
        Order order,
        string productName,
        string variantName,
        string sku,
        int quantity,
        decimal unitPrice)
    {
        dbContext.OrderItems.Add(
            new OrderItem
            {
                OrderId = order.Id,

                ProductName = productName,
                VariantName = variantName,
                SKU = sku,

                Quantity = quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * quantity,

                CreatedAtUtc = order.CreatedAtUtc,
                UpdatedAtUtc = order.UpdatedAtUtc
            });
    }
}