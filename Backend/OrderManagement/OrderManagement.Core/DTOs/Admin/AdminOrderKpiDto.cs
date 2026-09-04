namespace OrderManagement.Core.DTOs.Admin;

public class AdminOrderKpiDto
{
    public int TotalOrders { get; set; }

    public int New { get; set; }

    public int Confirmed { get; set; }

    public int Shipped { get; set; }

    public int Delivered { get; set; }

    public int Cancelled { get; set; }

    public int NoResponse { get; set; }

    public int Return { get; set; }

    public int ReturnInProcess { get; set; }

    public int RepeatedOrder { get; set; }

    public int Returns { get; set; } // its the Admin Dashboard aggregate

    public int NeedsAttention { get; set; }

    public int NeedToShip { get; set; }
}