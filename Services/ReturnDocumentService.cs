using LaptopTracker.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace LaptopTracker.Services
{
    public class ReturnDocumentService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ReturnDocumentService(IWebHostEnvironment env, IConfiguration config)
        { _env = env; _config = config; }

        public string ResolveEmail(string? kid)
        {
            if (string.IsNullOrWhiteSpace(kid)) return "";
            var pattern = _config["Email:AddressPattern"] ?? "{kid}@eon.com";
            return pattern.Replace("{kid}", kid.ToLowerInvariant());
        }

        public byte[] GeneratePdf(ReturnDevice device)
        {
            using var doc = new PdfDocument();
            doc.Info.Title   = $"Gerätrückgabe – {device.SerialNumber}";
            doc.Info.Author  = "WIC Asset Tracker";
            doc.Info.Creator = "WIC Asset Tracker";

            var page = doc.AddPage();
            page.Width  = XUnit.FromMillimeter(210);
            page.Height = XUnit.FromMillimeter(297);

            DrawPage(page, device);

            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }

        private void DrawPage(PdfPage page, ReturnDevice device)
        {
            // XGraphics must be disposed before doc.Save()
            using var gfx = XGraphics.FromPdfPage(page);

            const double lm = 20.0;   // left margin mm
            const double pw = 170.0;  // printable width mm
            const double lh = 6.0;   // standard line height mm
            double y = 20.0;

            var titleFont = new XFont("Segoe UI", 16, XFontStyleEx.Bold);
            var sectFont  = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var bodyFont  = new XFont("Segoe UI", 10, XFontStyleEx.Regular);
            var smallFont = new XFont("Segoe UI",  8, XFontStyleEx.Regular);

            var black  = XBrushes.Black;
            var muted  = new XSolidBrush(XColor.FromArgb(110, 110, 110));
            var bgSect = new XSolidBrush(XColor.FromArgb(232, 232, 232));

            XRect R(double yy, double h) =>
                new XRect(XUnit.FromMillimeter(lm), XUnit.FromMillimeter(yy),
                          XUnit.FromMillimeter(pw),  XUnit.FromMillimeter(h));
            XRect RC(double x, double yy, double w, double h) =>
                new XRect(XUnit.FromMillimeter(x), XUnit.FromMillimeter(yy),
                          XUnit.FromMillimeter(w),  XUnit.FromMillimeter(h));

            // ── HEADER ──────────────────────────────────────────────
            gfx.DrawString("Gerätrückgabe – Bestätigung",
                titleFont, black, R(y, 12), XStringFormats.TopLeft);
            y += 9;
            gfx.DrawString(
                $"Dokument-ID: {device.Id}  ·  Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm}",
                smallFont, muted, R(y, 5), XStringFormats.TopLeft);
            y += 5;
            gfx.DrawLine(new XPen(XColor.FromArgb(20, 184, 196), 1.0),
                XUnit.FromMillimeter(lm), XUnit.FromMillimeter(y),
                XUnit.FromMillimeter(lm + pw), XUnit.FromMillimeter(y));
            y += 5;

            // ── LOCAL HELPERS ────────────────────────────────────────
            void Section(string title)
            {
                y += 2;
                gfx.DrawRectangle(bgSect,
                    XUnit.FromMillimeter(lm), XUnit.FromMillimeter(y),
                    XUnit.FromMillimeter(pw), XUnit.FromMillimeter(5.5));
                gfx.DrawString(title, sectFont, black,
                    RC(lm + 2, y + 0.5, pw - 2, 5), XStringFormats.TopLeft);
                y += 7;
            }

            void Row(string label, string value)
            {
                gfx.DrawString(label, sectFont, muted,
                    RC(lm, y, 55, lh), XStringFormats.TopLeft);
                gfx.DrawString(value, bodyFont, black,
                    RC(lm + 57, y, pw - 57, lh), XStringFormats.TopLeft);
                y += lh;
            }

            string JN(bool? v) => v == true ? "Ja" : v == false ? "Nein" : "–";

            // ── MITARBEITER ──────────────────────────────────────────
            Section("Mitarbeiter");
            Row("KID:", device.KID ?? "–");
            Row("Rückgabedatum:", device.Date.ToString("dd.MM.yyyy"));
            if (!string.IsNullOrWhiteSpace(device.Location))
                Row("Standort:", device.Location);

            // ── GERÄT ────────────────────────────────────────────────
            Section("Gerät");
            Row("Seriennummer:", device.SerialNumber);
            Row("Gerätetyp:", device.DeviceType);
            Row("RITM:", device.RITM);
            if (!string.IsNullOrWhiteSpace(device.WorkOrder))
                Row("Work Order:", device.WorkOrder);

            // ── ZUBEHÖR (only if captured) ───────────────────────────
            if (device.ChargerReturned.HasValue || device.PowerCableReturned.HasValue)
            {
                Section("Zubehör");
                Row("Ladegerät zurückgegeben:", JN(device.ChargerReturned));
                Row("Netzkabel zurückgegeben:", JN(device.PowerCableReturned));
            }

            // ── ZUSTAND (only if relevant) ───────────────────────────
            bool hasCond = !string.IsNullOrWhiteSpace(device.DamageStatus)
                        || !string.IsNullOrWhiteSpace(device.DamageComment)
                        || device.IsCustomerInducedDamage
                        || device.IsBatterySwollen;
            if (hasCond)
            {
                Section("Zustand");
                if (!string.IsNullOrWhiteSpace(device.DamageStatus))
                    Row("Schadensstatus:", device.DamageStatus);
                if (!string.IsNullOrWhiteSpace(device.DamageComment))
                    Row("Kommentar:", device.DamageComment);
                if (device.IsCustomerInducedDamage)
                    Row("Kundenverschulden:", "Ja");
                if (device.IsBatterySwollen)
                    Row("Akku aufgebläht:", "Ja");
            }

            // ── BESTÄTIGUNG ──────────────────────────────────────────
            Section("Bestätigung");
            const string confirmText =
                "Hiermit wird der Eingang des oben genannten Geräts beim Global Service Desk bestätigt.";
            gfx.DrawString(confirmText, bodyFont, black, R(y, 10), XStringFormats.TopLeft);
            y += 10;

            // Signature (image or blank lines)
            var sig = device.Photos?.FirstOrDefault(p => p.IsSignature);
            if (sig is not null)
            {
                var sigPath = Path.Combine(_env.WebRootPath,
                    sig.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(sigPath))
                {
                    try
                    {
                        using var img = XImage.FromFile(sigPath);
                        double maxH = 25.0;
                        double imgW = maxH * img.PixelWidth / img.PixelHeight;
                        if (imgW > 80) { imgW = 80; maxH = imgW * img.PixelHeight / img.PixelWidth; }
                        gfx.DrawImage(img,
                            XUnit.FromMillimeter(lm + 90), XUnit.FromMillimeter(y),
                            XUnit.FromMillimeter(imgW), XUnit.FromMillimeter(maxH));
                        y += maxH + 1;
                        gfx.DrawString($"Erfasst: {sig.UploadedAt:dd.MM.yyyy}", smallFont, muted,
                            RC(lm + 90, y, 80, 4), XStringFormats.TopLeft);
                        y += 5;
                    }
                    catch { y += 25; }
                }
                else { y += 25; }
            }
            else
            {
                y += 22; // space for pen signature
            }

            // Signature lines
            var thin = new XPen(XColors.Black, 0.5);
            gfx.DrawLine(thin,
                XUnit.FromMillimeter(lm), XUnit.FromMillimeter(y),
                XUnit.FromMillimeter(lm + 65), XUnit.FromMillimeter(y));
            gfx.DrawLine(thin,
                XUnit.FromMillimeter(lm + 90), XUnit.FromMillimeter(y),
                XUnit.FromMillimeter(lm + pw), XUnit.FromMillimeter(y));
            y += 3;
            gfx.DrawString("Ort, Datum",    smallFont, muted, RC(lm,      y, 65,      4), XStringFormats.TopLeft);
            gfx.DrawString("Unterschrift",  smallFont, muted, RC(lm + 90, y, pw - 90, 4), XStringFormats.TopLeft);

            // ── FOOTER ───────────────────────────────────────────────
            const double fy = 282.0;
            gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200), 0.5),
                XUnit.FromMillimeter(lm), XUnit.FromMillimeter(fy),
                XUnit.FromMillimeter(lm + pw), XUnit.FromMillimeter(fy));
            gfx.DrawString(
                $"WIC Asset Tracker  ·  Dokument-ID: {device.Id}  ·  {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
                smallFont, muted, R(fy + 1, 4), XStringFormats.TopLeft);
        }
    }
}
