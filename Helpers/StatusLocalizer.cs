using LaptopTracker.Models;
using Microsoft.Extensions.Localization;

namespace LaptopTracker.Helpers;

public class StatusLabels { }

public static class StatusLocalizer
{
    public static string Get(ReturnDeviceStatus status, IStringLocalizer<StatusLabels> loc) =>
        status switch
        {
            ReturnDeviceStatus.PendingPickup => loc["Status_PendingPickup"],
            ReturnDeviceStatus.PickedUp      => loc["Status_PickedUp"],
            ReturnDeviceStatus.HandedOver    => loc["Status_HandedOver"],
            _                                => status.ToString()
        };

    public static string Get(WicStockStatus status, IStringLocalizer<StatusLabels> loc) =>
        status switch
        {
            WicStockStatus.Available    => loc["Status_Available"],
            WicStockStatus.NotAvailable => loc["Status_WicNotAvailable"],
            _                           => status.ToString()
        };

    public static string Get(LoanerStatus status, IStringLocalizer<StatusLabels> loc) =>
        status switch
        {
            LoanerStatus.Available    => loc["Status_Available"],
            LoanerStatus.NotAvailable => loc["Status_LoanerNotAvailable"],
            _                         => status.ToString()
        };

    public static string Get(StockStatus status, IStringLocalizer<StatusLabels> loc) =>
        status switch
        {
            StockStatus.Ok        => loc["Status_StockOk"],
            StockStatus.Surplus   => loc["Status_StockSurplus"],
            StockStatus.Shortage  => loc["Status_StockShortage"],
            StockStatus.Undefined => loc["Status_StockUndefined"],
            _                     => status.ToString()
        };

    // Informational lease indicator text; None has no label by design.
    public static string GetLease(LeaseStatus status, IStringLocalizer<StatusLabels> loc) =>
        status switch
        {
            LeaseStatus.Expired  => loc["Lease_Expired"],
            LeaseStatus.EndsSoon => loc["Lease_EndsSoon"],
            _                    => ""
        };

    // DeviceLocation badge text — maps raw DB value to localized label
    public static string GetDeviceLocation(string? rawValue, IStringLocalizer<StatusLabels> loc) =>
        rawValue switch
        {
            "In WIC"     => loc["DevLoc_InWic"],
            "In Use"     => loc["DevLoc_InUse"],
            "In Transit" => loc["DevLoc_InTransit"],
            null or ""   => "—",
            _            => rawValue
        };

    // Boolean accessory display with icons (charger / cable detail row)
    public static string GetBool(bool? value, IStringLocalizer<StatusLabels> loc) =>
        value switch
        {
            true  => "✓ " + loc["Bool_Yes"].Value,
            false => "✗ " + loc["Bool_No"].Value,
            null  => "—"
        };

    // Boolean without icons — for badge-only cells
    public static string GetBoolBadge(bool? value, IStringLocalizer<StatusLabels> loc) =>
        value switch
        {
            true  => loc["Bool_Yes"],
            false => loc["Bool_No"],
            null  => "—"
        };
}
