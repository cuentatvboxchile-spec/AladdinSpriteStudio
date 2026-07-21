using System.Buffers.Binary;

namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Lee y convierte colores del formato BGR555 utilizado por SNES.
/// Cada color ocupa 2 bytes y utiliza 5 bits por componente.
/// </summary>
public static class SnesPalette
{
    public const int ColorsPerPalette = 16;
    public const int BytesPerColor = 2;
    public const int BytesPerPalette =
        ColorsPerPalette * BytesPerColor;

    /// <summary>
    /// Convierte un color SNES BGR555 a un Color de Windows.
    /// </summary>
    public static Color DecodeColor(ushort snesColor)
    {
        int red5 = snesColor & 0x1F;
        int green5 = (snesColor >> 5) & 0x1F;
        int blue5 = (snesColor >> 10) & 0x1F;

        int red8 = Expand5To8(red5);
        int green8 = Expand5To8(green5);
        int blue8 = Expand5To8(blue5);

        return Color.FromArgb(
            red8,
            green8,
            blue8);
    }

    /// <summary>
    /// Lee una paleta completa de 16 colores desde 32 bytes.
    /// </summary>
    public static Color[] DecodePalette(
        ReadOnlySpan<byte> paletteData)
    {
        if (paletteData.Length < BytesPerPalette)
        {
            throw new ArgumentException(
                $"Una paleta SNES de 16 colores necesita " +
                $"{BytesPerPalette} bytes.",
                nameof(paletteData));
        }

        Color[] colors =
            new Color[ColorsPerPalette];

        for (int index = 0;
             index < ColorsPerPalette;
             index++)
        {
            int byteOffset =
                index * BytesPerColor;

            ushort snesColor =
                BinaryPrimitives.ReadUInt16LittleEndian(
                    paletteData.Slice(
                        byteOffset,
                        BytesPerColor));

            colors[index] =
                DecodeColor(snesColor);
        }

        return colors;
    }

    /// <summary>
    /// Lee una paleta desde una posición de un arreglo mayor,
    /// como los datos completos de una ROM.
    /// </summary>
    public static Color[] DecodePalette(
        byte[] data,
        int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 ||
            offset > data.Length - BytesPerPalette)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "No existen 32 bytes disponibles " +
                "desde el offset de paleta indicado.");
        }

        return DecodePalette(
            data.AsSpan(
                offset,
                BytesPerPalette));
    }

    private static int Expand5To8(int value)
    {
        return (value << 3) |
               (value >> 2);
    }
}