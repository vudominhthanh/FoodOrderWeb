using System;
using System.Collections.Generic;

namespace FoodOrderWeb.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string? Label { get; set; }

    public string Street { get; set; } = null!;

    public string? City { get; set; }

    public string? District { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;

    public static implicit operator Address(string v)
    {
        throw new NotImplementedException();
    }
}
