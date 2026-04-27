using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Protocol utility class fully consistent with the Python version
/// $ escaping + $$ terminator
/// </summary>
public static class Utils
{
    // Single-byte marker: $
    public static readonly byte HALF_EOQ = (byte)'$';
    
    // Buffer size 256KB (consistent with Python)
    public const int MAX_BUFFER = 262144;

    // Terminator: $$ (fixed 2 bytes)
    public static byte[] Eoq() => new[] { HALF_EOQ, HALF_EOQ };

    /// <summary>
    /// Escape: $ → $044 (ASCII 36 → octal 044)
    /// </summary>
    public static byte[] Escape(byte[] msg)
    {
        if (msg == null || msg.Length == 0)
            return Array.Empty<byte>();

        // Correct: Convert $ ASCII 36 to octal 044
        int value = HALF_EOQ;  // 36
        string octalString = Convert.ToString(value, 8);  // Convert to octal = "44"
        octalString = octalString.PadLeft(3, '0');        // Pad leading zero = "044"

        byte[] octalBytes = Encoding.ASCII.GetBytes(octalString); // Bytes: 0 4 4

        // Combine into $044
        byte[] escapeSeq = new byte[] { HALF_EOQ }.Concat(octalBytes).ToArray();

        // Replace all $ with $044
        return ReplaceSequence(msg, new[] { HALF_EOQ }, escapeSeq);
    }

    /// <summary>
    /// Unescape: $xxx → original byte (octal parsing)
    /// </summary>
    public static byte[] Unescape(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        var pattern = Regex.Escape(Encoding.ASCII.GetString(new[] { HALF_EOQ })) + @"(\d{3})";
        var input = Encoding.ASCII.GetString(data);
        
        var result = Regex.Replace(input, pattern, m =>
        {
            int octal = Convert.ToInt32(m.Groups[1].Value, 8);
            return ((char)octal).ToString();
        });

        return Encoding.ASCII.GetBytes(result);
    }

    #region Helper Methods: Byte Sequence Replacement
    private static byte[] ReplaceSequence(byte[] source, byte[] oldSeq, byte[] newSeq)
    {
        var ms = new MemoryStream();
        int i = 0;
        while (i < source.Length)
        {
            if (IsMatch(source, i, oldSeq))
            {
                ms.Write(newSeq, 0, newSeq.Length);
                i += oldSeq.Length;
            }
            else
            {
                ms.WriteByte(source[i]);
                i++;
            }
        }
        return ms.ToArray();
    }

    private static bool IsMatch(byte[] source, int index, byte[] seq)
    {
        if (index + seq.Length > source.Length) return false;
        for (int i = 0; i < seq.Length; i++)
            if (source[index + i] != seq[i]) return false;
        return true;
    }
    #endregion
}