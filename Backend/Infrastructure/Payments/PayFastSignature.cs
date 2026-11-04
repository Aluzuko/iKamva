using System.Security.Cryptography;
using System.Text;

namespace Backend.Infrastructure.Payments;

public static class PayFastSignature
{
    public static string ForCheckout(IEnumerable<KeyValuePair<string, string>> fields, string? passphrase) =>
        Hash(fields.Where(f => !string.IsNullOrEmpty(f.Value)).Select(f => new KeyValuePair<string, string>(f.Key, f.Value.Trim())), passphrase);

    public static string ParamString(IEnumerable<KeyValuePair<string, string>> fields) =>
        string.Join('&', fields.Select(f => f.Key + "=" + Encode(f.Value)));

    public static string Hash(IEnumerable<KeyValuePair<string, string>> fields, string? passphrase)
    {
        var text = ParamString(fields);
        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            text += "&passphrase=" + Encode(passphrase.Trim());
        }
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Matches(string expected, string? actual) =>
        actual is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual.Trim().ToLowerInvariant()));

    public static string Encode(string value)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (char)b;
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.')
            {
                sb.Append(c);
            }
            else if (c == ' ')
            {
                sb.Append('+');
            }
            else
            {
                sb.Append('%').Append(b.ToString("X2"));
            }
        }
        return sb.ToString();
    }

    public static List<KeyValuePair<string, string>> ParseForm(string body)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = pair.IndexOf('=');
            var key = index < 0 ? pair : pair[..index];
            var value = index < 0 ? string.Empty : pair[(index + 1)..];
            result.Add(new(Decode(key), Decode(value)));
        }
        return result;
    }

    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
}
