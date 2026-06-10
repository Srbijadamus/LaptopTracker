using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopTracker.Data;
using ClosedXML.Excel;

namespace LaptopTracker.Controllers
{
    public class ExportController : Controller
    {
        private readonly LaptopTrackerDbContext _context;

        public ExportController(LaptopTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ExportAll()
        {
            var returns  = await _context.ReturnDevices.Where(d => !d.IsDeleted).OrderByDescending(d => d.Date).ToListAsync();
            var wic      = await _context.WicStockDevices.Where(d => !d.IsDeleted).OrderByDescending(d => d.Date).ToListAsync();
            var loaners  = await _context.LoanerDevices.Where(d => !d.IsDeleted).OrderByDescending(d => d.Date).ToListAsync();

            using var wb = new XLWorkbook();

            // ReturnDevices sheet
            var ws1 = wb.Worksheets.Add("Return Devices");
            ws1.Cell(1,1).Value = "Serial Number"; ws1.Cell(1,2).Value = "Type"; ws1.Cell(1,3).Value = "RITM";
            ws1.Cell(1,4).Value = "Date"; ws1.Cell(1,5).Value = "Status"; ws1.Cell(1,6).Value = "Device Location";
            ws1.Cell(1,7).Value = "Work Order"; ws1.Cell(1,8).Value = "Pickup Status"; ws1.Cell(1,9).Value = "Location";
            ws1.Cell(1,10).Value = "Charger"; ws1.Cell(1,11).Value = "Power Cable";
            ws1.Row(1).Style.Font.Bold = true;
            for (int i = 0; i < returns.Count; i++)
            {
                var d = returns[i]; var r = i + 2;
                ws1.Cell(r,1).Value = d.SerialNumber; ws1.Cell(r,2).Value = d.DeviceType; ws1.Cell(r,3).Value = d.RITM;
                ws1.Cell(r,4).Value = d.Date.ToString("dd.MM.yyyy"); ws1.Cell(r,5).Value = d.Status.ToString();
                ws1.Cell(r,6).Value = d.DeviceLocation; ws1.Cell(r,7).Value = d.WorkOrder ?? "";
                ws1.Cell(r,8).Value = d.PickupStatus ?? ""; ws1.Cell(r,9).Value = d.Location ?? "";
                ws1.Cell(r,10).Value = d.ChargerReturned == true ? "YES" : "NO";
                ws1.Cell(r,11).Value = d.PowerCableReturned == true ? "YES" : "NO";
            }
            ws1.Columns().AdjustToContents();

            // WIC Stock sheet
            var ws2 = wb.Worksheets.Add("WIC Stock");
            ws2.Cell(1,1).Value = "Serial Number"; ws2.Cell(1,2).Value = "Type"; ws2.Cell(1,3).Value = "RITM";
            ws2.Cell(1,4).Value = "Date"; ws2.Cell(1,5).Value = "Status"; ws2.Cell(1,6).Value = "Device Location";
            ws2.Cell(1,7).Value = "Swap RITM";
            ws2.Row(1).Style.Font.Bold = true;
            for (int i = 0; i < wic.Count; i++)
            {
                var d = wic[i]; var r = i + 2;
                ws2.Cell(r,1).Value = d.SerialNumber; ws2.Cell(r,2).Value = d.DeviceType; ws2.Cell(r,3).Value = d.RITM;
                ws2.Cell(r,4).Value = d.Date.ToString("dd.MM.yyyy"); ws2.Cell(r,5).Value = d.Status.ToString();
                ws2.Cell(r,6).Value = d.DeviceLocation; ws2.Cell(r,7).Value = d.SwapRITM ?? "";
            }
            ws2.Columns().AdjustToContents();

            // Loaner Devices sheet
            var ws3 = wb.Worksheets.Add("Loaner Devices");
            ws3.Cell(1,1).Value = "Serial Number"; ws3.Cell(1,2).Value = "Type"; ws3.Cell(1,3).Value = "RITM";
            ws3.Cell(1,4).Value = "Date"; ws3.Cell(1,5).Value = "Status"; ws3.Cell(1,6).Value = "Device Location";
            ws3.Cell(1,7).Value = "KID"; ws3.Cell(1,8).Value = "Kid Handed To"; ws3.Cell(1,9).Value = "Date Handed Over";
            ws3.Cell(1,10).Value = "WIC";
            ws3.Row(1).Style.Font.Bold = true;
            for (int i = 0; i < loaners.Count; i++)
            {
                var d = loaners[i]; var r = i + 2;
                ws3.Cell(r,1).Value = d.SerialNumber; ws3.Cell(r,2).Value = d.DeviceType; ws3.Cell(r,3).Value = d.RITM;
                ws3.Cell(r,4).Value = d.Date.ToString("dd.MM.yyyy"); ws3.Cell(r,5).Value = d.Status.ToString();
                ws3.Cell(r,6).Value = d.DeviceLocation; ws3.Cell(r,7).Value = d.KID ?? "";
                ws3.Cell(r,8).Value = d.KidHandedTo ?? "";
                ws3.Cell(r,9).Value = d.DateHandedOver.HasValue ? d.DateHandedOver.Value.ToString("dd.MM.yyyy") : "";
                ws3.Cell(r,10).Value = d.WIC ?? "";
            }
            ws3.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            var fileName = $"WIC_Asset_Tracker_Export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
