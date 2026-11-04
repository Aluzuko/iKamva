using System.Globalization;
using System.Text;

namespace Backend.Infrastructure.Email;

public static class SimplePdf
{
    public static byte[] Invoice(string invoiceNumber, string billedTo, string billedEmail, string student, string period,
        IReadOnlyList<InvoiceLine> lines, decimal total, bool paid, DateTime issuedAt)
    {
        var content = new StringBuilder();
        var y = 790;
        void Text(string font, int size, int x, string value)
        {
            content.Append($"BT /{font} {size} Tf {x} {y} Td ({Escape(value)}) Tj ET\n");
        }
        void Right(string font, int size, int right, string value)
        {
            var width = (int)Math.Round(Width(value, font == "F2") * size);
            content.Append($"BT /{font} {size} Tf {right - width} {y} Td ({Escape(value)}) Tj ET\n");
        }

        Text("F2", 22, 50, "iKamva");
        Right("F2", 16, 545, paid ? "INVOICE (PAID)" : "INVOICE");
        y -= 22; Right("F1", 10, 545, $"No. {invoiceNumber}");
        y -= 14; Right("F1", 10, 545, $"Issued {issuedAt:d MMMM yyyy}");
        y -= 40; Text("F2", 10, 50, "BILLED TO");
        y -= 16; Text("F1", 11, 50, billedTo);
        y -= 14; Text("F1", 10, 50, billedEmail);
        y -= 14; Text("F1", 10, 50, $"Lessons for {student} - {period}");
        y -= 36;
        content.Append($"0.85 g 50 {y - 6} 495 20 re f 0 g\n");
        Text("F2", 10, 58, "DATE"); Text("F2", 10, 140, "DESCRIPTION"); Right("F2", 10, 537, "AMOUNT");
        y -= 24;
        foreach (var line in lines.Take(30))
        {
            Text("F1", 10, 58, line.Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
            Text("F1", 10, 140, Truncate(line.Description, 60));
            Right("F1", 10, 537, EmailTemplates.Rand(line.Amount));
            y -= 18;
        }
        if (lines.Count > 30)
        {
            Text("F1", 9, 140, $"... and {lines.Count - 30} more lessons");
            y -= 18;
        }
        content.Append($"0.6 G 50 {y + 8} m 545 {y + 8} l S\n");
        y -= 12;
        Text("F2", 12, 360, "Total"); Right("F2", 12, 537, EmailTemplates.Rand(total));
        y -= 40;
        Text("F1", 9, 50, paid ? "Paid in full. Thank you." : "Pay online from the Payments page in iKamva.");
        y -= 14;
        Text("F1", 9, 50, "Amounts in South African Rand (ZAR).");

        var stream = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(stream)} >>\nstream\n{stream}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
        };

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var (obj, i) in objects.Select((o, i) => (o, i)))
        {
            offsets.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
            pdf.Append($"{i + 1} 0 obj\n{obj}\nendobj\n");
        }
        var xref = Encoding.Latin1.GetByteCount(pdf.ToString());
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append($"{offset:D10} 00000 n \n");
        }
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private static double Width(string value, bool bold)
    {
        double total = 0;
        foreach (var c in value)
        {
            total += c switch
            {
                >= 'A' and <= 'Z' => bold ? 0.72 : 0.667,
                >= '0' and <= '9' => 0.556,
                ' ' or '.' or ',' or ':' => 0.278,
                '-' => 0.333,
                'i' or 'l' or 'j' => 0.222,
                'm' or 'w' => 0.833,
                '(' or ')' => 0.333,
                _ => bold ? 0.556 : 0.5,
            };
        }
        return total;
    }

    private static string Escape(string value)
    {
        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if (c is '(' or ')' or '\\')
            {
                sb.Append('\\').Append(c);
            }
            else if (c is '’' or '‘')
            {
                sb.Append('\'');
            }
            else if (c is '—' or '–')
            {
                sb.Append('-');
            }
            else if (c < 32 || c > 255)
            {
                sb.Append('?');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 3)] + "...";
}
