using System.Globalization;
using System.Text;

namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Representa una paleta cargada desde un archivo externo.
/// </summary>
public sealed class LoadedPaletteFile
{
    private readonly Color[] _colors;

    public LoadedPaletteFile(
        string filePath,
        string formatName,
        Color[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);

        if (colors.Length == 0)
        {
            throw new ArgumentException(
                "La paleta debe contener al menos un color.",
                nameof(colors));
        }

        FilePath = filePath;
        FormatName = formatName;

        _colors = (Color[])colors.Clone();
    }

    /// <summary>
    /// Ruta del archivo cargado.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Nombre del formato detectado.
    /// </summary>
    public string FormatName { get; }

    /// <summary>
    /// Cantidad total de colores encontrados.
    /// </summary>
    public int ColorCount =>
        _colors.Length;

    /// <summary>
    /// Cantidad de bancos de 16 colores disponibles.
    /// </summary>
    public int BankCount =>
        (_colors.Length +
         SnesPalette.ColorsPerPalette - 1) /
        SnesPalette.ColorsPerPalette;

    /// <summary>
    /// Devuelve una copia de todos los colores.
    /// </summary>
    public Color[] GetAllColors()
    {
        return (Color[])_colors.Clone();
    }

    /// <summary>
    /// Obtiene un banco de 16 colores.
    /// Si el último banco está incompleto,
    /// los espacios restantes se completan con negro.
    /// </summary>
    public Color[] GetBank(int bankIndex)
    {
        if (bankIndex < 0 ||
            bankIndex >= BankCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bankIndex),
                "El banco de paleta indicado no existe.");
        }

        Color[] bank =
            new Color[
                SnesPalette.ColorsPerPalette];

        for (int index = 0;
             index < bank.Length;
             index++)
        {
            bank[index] = Color.Black;
        }

        int sourceOffset =
            bankIndex *
            SnesPalette.ColorsPerPalette;

        int availableColors =
            Math.Min(
                SnesPalette.ColorsPerPalette,
                _colors.Length - sourceOffset);

        Array.Copy(
            _colors,
            sourceOffset,
            bank,
            0,
            availableColors);

        return bank;
    }
}

/// <summary>
/// Detecta y carga diferentes formatos de archivos .pal.
/// </summary>
public static class PaletteFileLoader
{
    /// <summary>
    /// Abre y detecta automáticamente el formato
    /// de un archivo de paleta.
    /// </summary>
    public static LoadedPaletteFile Load(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Debe indicar una ruta de archivo.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo de paleta.",
                filePath);
        }

        byte[] data =
            File.ReadAllBytes(filePath);

        if (data.Length == 0)
        {
            throw new InvalidDataException(
                "El archivo de paleta está vacío.");
        }

        if (IsJascPalette(data))
        {
            Color[] colors =
                DecodeJascPalette(filePath);

            return new LoadedPaletteFile(
                filePath,
                "JASC-PAL",
                colors);
        }

        if (data.Length ==
            SnesPalette.BytesPerPalette)
        {
            Color[] colors =
                SnesPalette.DecodePalette(
                    data,
                    0);

            return new LoadedPaletteFile(
                filePath,
                "SNES BGR555 - 16 colores",
                colors);
        }

        if (data.Length == 48)
        {
            Color[] colors =
                DecodeRawRgb24(data);

            return new LoadedPaletteFile(
                filePath,
                "RGB de 24 bits - 16 colores",
                colors);
        }

        if (data.Length == 768)
        {
            Color[] colors =
                DecodeRawRgb24(data);

            return new LoadedPaletteFile(
                filePath,
                "RGB de 24 bits - 256 colores",
                colors);
        }

        throw new InvalidDataException(
            $"Formato de paleta no reconocido.\n\n" +
            $"Tamaño encontrado: {data.Length:N0} bytes.\n\n" +
            "Formatos compatibles actualmente:\n" +
            "• SNES BGR555 de 32 bytes.\n" +
            "• RGB de 48 bytes.\n" +
            "• RGB de 768 bytes.\n" +
            "• Archivo de texto JASC-PAL.");
    }

    /// <summary>
    /// Comprueba si el archivo comienza con JASC-PAL.
    /// </summary>
    private static bool IsJascPalette(
        byte[] data)
    {
        int bytesToRead =
            Math.Min(
                data.Length,
                64);

        string beginning =
            Encoding.UTF8.GetString(
                data,
                0,
                bytesToRead);

        beginning =
            beginning.TrimStart('\uFEFF');

        return beginning.StartsWith(
            "JASC-PAL",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Lee una paleta RGB donde cada color ocupa
    /// tres bytes: rojo, verde y azul.
    /// </summary>
    private static Color[] DecodeRawRgb24(
        byte[] data)
    {
        if (data.Length % 3 != 0)
        {
            throw new InvalidDataException(
                "La paleta RGB no contiene una cantidad válida de bytes.");
        }

        int colorCount =
            data.Length / 3;

        Color[] colors =
            new Color[colorCount];

        for (int index = 0;
             index < colorCount;
             index++)
        {
            int byteOffset =
                index * 3;

            int red =
                data[byteOffset];

            int green =
                data[byteOffset + 1];

            int blue =
                data[byteOffset + 2];

            colors[index] =
                Color.FromArgb(
                    red,
                    green,
                    blue);
        }

        return colors;
    }

    /// <summary>
    /// Lee un archivo de texto JASC-PAL.
    /// </summary>
    private static Color[] DecodeJascPalette(
        string filePath)
    {
        string text =
            File.ReadAllText(
                filePath,
                Encoding.UTF8);

        string[] lines =
            text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (lines.Length < 4)
        {
            throw new InvalidDataException(
                "El archivo JASC-PAL está incompleto.");
        }

        string header =
            lines[0].TrimStart('\uFEFF');

        if (!header.Equals(
                "JASC-PAL",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "El encabezado JASC-PAL no es válido.");
        }

        if (!int.TryParse(
                lines[2],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int colorCount) ||
            colorCount <= 0)
        {
            throw new InvalidDataException(
                "La cantidad de colores del archivo JASC-PAL no es válida.");
        }

        if (lines.Length <
            colorCount + 3)
        {
            throw new InvalidDataException(
                "El archivo JASC-PAL contiene menos colores de los declarados.");
        }

        Color[] colors =
            new Color[colorCount];

        for (int index = 0;
             index < colorCount;
             index++)
        {
            string[] components =
                lines[index + 3].Split(
                    new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            if (components.Length < 3)
            {
                throw new InvalidDataException(
                    $"El color número {index} no tiene " +
                    "los tres componentes RGB.");
            }

            if (!TryParseByte(
                    components[0],
                    out int red) ||
                !TryParseByte(
                    components[1],
                    out int green) ||
                !TryParseByte(
                    components[2],
                    out int blue))
            {
                throw new InvalidDataException(
                    $"El color número {index} contiene " +
                    "valores RGB no válidos.");
            }

            colors[index] =
                Color.FromArgb(
                    red,
                    green,
                    blue);
        }

        return colors;
    }

    private static bool TryParseByte(
        string text,
        out int value)
    {
        if (!int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value))
        {
            return false;
        }

        return value is >= 0 and <= 255;
    }
}