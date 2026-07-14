using System.Text;

namespace MothersonBoxManagement.PrintAgent.Core;

public static class ZplLabelBuilder
{
    public static string Build(AgentPrintLabelPayload payload)
    {
        Validate(payload);
        var boxNumber = Escape(payload.BoxNumber);
        var barcode = Escape(payload.BarcodeValue);
        var zpl = new StringBuilder(384);
        zpl.Append("^XA");
        zpl.Append("^CI28^PW800^LL800^LH0,0");
        zpl.Append("^FO190,35^A0N,44,44^FH\\^FDMOTHERSON BOX^FS");
        zpl.Append("^FO145,95^A0N,34,30^FH\\^FD").Append(boxNumber).Append("^FS");
        zpl.Append("^FO145,155^BQN,2,10^FH\\^FDLA,").Append(barcode).Append("^FS");
        zpl.Append("^FO125,720^A0N,30,26^FH\\^FD").Append(barcode).Append("^FS");
        zpl.Append("^PQ1,0,1,N^XZ");
        return zpl.ToString();
    }

    public static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length + 16);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\5C",
                '^' => "\\5E",
                '~' => "\\7E",
                _ when char.IsControl(character) => string.Empty,
                _ => character.ToString()
            });
        }
        return builder.ToString();
    }

    private static void Validate(AgentPrintLabelPayload payload)
    {
        if (payload.Version != 1 || payload.WidthDots != 800 || payload.HeightDots != 800 ||
            payload.Dpi != 203 || payload.Copies != 1)
            throw new InvalidOperationException("Unsupported label profile.");
        if (string.IsNullOrWhiteSpace(payload.BoxNumber) || payload.BoxNumber.Length > 100 ||
            string.IsNullOrWhiteSpace(payload.BarcodeValue) || payload.BarcodeValue.Length > 100)
            throw new InvalidOperationException("Invalid label payload.");
    }
}
