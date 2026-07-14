using System.ComponentModel;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using MothersonBoxManagement.PrintAgent.Core;
using QRCoder;

namespace MothersonBoxManagement.PrintAgent;

internal static class LabelPrinter
{
    public static string[] InstalledPrinters() => PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(name => name).ToArray();

    public static void Print(AgentClaimedPrintJob job)
    {
        if (!InstalledPrinters().Contains(job.PrinterName, StringComparer.OrdinalIgnoreCase))
            throw new PrinterAgentException("PRINTER_UNAVAILABLE", transient: true);
        if (string.Equals(job.PrintMode, "Zpl", StringComparison.Ordinal))
            RawPrinter.Send(job.PrinterName, ZplLabelBuilder.Build(job.Payload));
        else if (string.Equals(job.PrintMode, "Windows", StringComparison.Ordinal))
            PrintWindows(job.PrinterName, job.Payload);
        else
            throw new PrinterAgentException("INVALID_PAYLOAD", transient: false);
    }

    private static void PrintWindows(string printerName, AgentPrintLabelPayload payload)
    {
        if (payload is not { Version: 1, WidthDots: 800, HeightDots: 800, Dpi: 203, Copies: 1 })
            throw new PrinterAgentException("INVALID_PAYLOAD", false);
        using var document = new PrintDocument
        {
            PrinterSettings = new PrinterSettings { PrinterName = printerName, Copies = 1 },
            OriginAtMargins = false,
            PrintController = new StandardPrintController(),
            DocumentName = $"Motherson {payload.BoxNumber}"
        };
        document.DefaultPageSettings.PaperSize = new PaperSize("Motherson 100x100mm", 394, 394);
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.PrintPage += (_, eventArgs) =>
        {
            if (eventArgs.Graphics is null)
                throw new PrinterAgentException("SPOOLER_ERROR", true);
            DrawWindowsLabel(eventArgs.Graphics, payload);
        };
        try { document.Print(); }
        catch (Exception exception) when (exception is InvalidPrinterException or Win32Exception)
        {
            throw new PrinterAgentException("SPOOLER_ERROR", true, exception);
        }
    }

    private static void DrawWindowsLabel(Graphics graphics, AgentPrintLabelPayload payload)
    {
        graphics.Clear(Color.White);
        using var titleFont = new Font("Arial", 17, FontStyle.Bold);
        using var boxFont = new Font("Arial", 13, FontStyle.Bold);
        using var barcodeFont = new Font("Consolas", 11, FontStyle.Bold);
        using var centered = new StringFormat { Alignment = StringAlignment.Center };
        graphics.DrawString("MOTHERSON BOX", titleFont, Brushes.Black, new RectangleF(0, 18, 394, 30), centered);
        graphics.DrawString(payload.BoxNumber, boxFont, Brushes.Black, new RectangleF(0, 55, 394, 25), centered);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload.BarcodeValue, QRCodeGenerator.ECCLevel.Q);
        const float qrX = 72;
        const float qrY = 91;
        const float qrSize = 250;
        var cell = qrSize / data.ModuleMatrix.Count;
        for (var row = 0; row < data.ModuleMatrix.Count; row++)
        for (var column = 0; column < data.ModuleMatrix[row].Count; column++)
            if (data.ModuleMatrix[row][column])
                graphics.FillRectangle(Brushes.Black, qrX + column * cell, qrY + row * cell, cell + .4f, cell + .4f);
        graphics.DrawString(payload.BarcodeValue, barcodeFont, Brushes.Black, new RectangleF(0, 350, 394, 25), centered);
    }
}

internal sealed class PrinterAgentException : Exception
{
    public string ErrorCode { get; }
    public bool Transient { get; }
    public PrinterAgentException(string errorCode, bool transient, Exception? inner = null) : base(errorCode, inner)
    {
        ErrorCode = errorCode;
        Transient = transient;
    }
}

internal static class RawPrinter
{
    public static void Send(string printerName, string value)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
            throw new PrinterAgentException("PRINTER_UNAVAILABLE", true, new Win32Exception(Marshal.GetLastWin32Error()));
        try
        {
            var info = new DocInfo { DocName = "Motherson ZPL label", DataType = "RAW" };
            if (!StartDocPrinter(printer, 1, ref info) || !StartPagePrinter(printer))
                throw new PrinterAgentException("SPOOLER_ERROR", true, new Win32Exception(Marshal.GetLastWin32Error()));
            var bytes = Encoding.UTF8.GetBytes(value);
            var unmanaged = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, unmanaged, bytes.Length);
                if (!WritePrinter(printer, unmanaged, bytes.Length, out var written) || written != bytes.Length)
                    throw new PrinterAgentException("SPOOLER_ERROR", true, new Win32Exception(Marshal.GetLastWin32Error()));
            }
            finally { Marshal.FreeHGlobal(unmanaged); }
            EndPagePrinter(printer);
            EndDocPrinter(printer);
        }
        finally { ClosePrinter(printer); }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DocInfo { [MarshalAs(UnmanagedType.LPWStr)] public string DocName; [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile; [MarshalAs(UnmanagedType.LPWStr)] public string DataType; }
    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool StartDocPrinter(IntPtr printer, int level, ref DocInfo info);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool StartPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool WritePrinter(IntPtr printer, IntPtr bytes, int count, out int written);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndDocPrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool ClosePrinter(IntPtr printer);
}
