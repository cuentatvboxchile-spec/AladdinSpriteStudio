namespace AladdinSpriteStudio.UI.Core;

/// <summary>
/// Representa los datos almacenados en el encabezado interno
/// de una ROM de Super Nintendo.
/// </summary>
public sealed class RomHeader
{
    /// <summary>
    /// Posición del encabezado dentro del archivo.
    /// </summary>
    public int HeaderOffset { get; init; }

    /// <summary>
    /// Indica si la ROM posee una cabecera externa
    /// de copiador de 512 bytes.
    /// </summary>
    public bool HasCopierHeader { get; init; }

    /// <summary>
    /// Nombre interno almacenado en la ROM.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Código que indica el modo de mapeo de memoria.
    /// </summary>
    public byte MapMode { get; init; }

    /// <summary>
    /// Código del tipo de cartucho y chips adicionales.
    /// </summary>
    public byte CartridgeType { get; init; }

    /// <summary>
    /// Código del tamaño declarado de la ROM.
    /// </summary>
    public byte RomSizeCode { get; init; }

    /// <summary>
    /// Código del tamaño declarado de la memoria RAM.
    /// </summary>
    public byte RamSizeCode { get; init; }

    /// <summary>
    /// Código de región o destino.
    /// </summary>
    public byte CountryCode { get; init; }

    /// <summary>
    /// Código antiguo del fabricante o licenciatario.
    /// </summary>
    public byte LicenseeCode { get; init; }

    /// <summary>
    /// Número de versión de la ROM.
    /// </summary>
    public byte Version { get; init; }

    /// <summary>
    /// Complemento del checksum almacenado.
    /// </summary>
    public ushort ChecksumComplement { get; init; }

    /// <summary>
    /// Checksum almacenado en la ROM.
    /// </summary>
    public ushort Checksum { get; init; }

    /// <summary>
    /// Devuelve el nombre descriptivo del mapeo.
    /// </summary>
    public string MappingName => MapMode switch
    {
        0x20 or 0x30 => "LoROM",
        0x21 or 0x31 => "HiROM",
        0x25 or 0x35 => "ExHiROM",
        _ => $"Desconocido (0x{MapMode:X2})"
    };

    /// <summary>
    /// Comprueba si el checksum y su complemento suman 0xFFFF.
    /// </summary>
    public bool HasValidChecksum =>
        (ushort)(Checksum + ChecksumComplement) == 0xFFFF;

    /// <summary>
    /// Tamaño de ROM declarado, expresado en bytes.
    /// </summary>
    public long DeclaredRomSizeBytes =>
        RomSizeCode <= 0x1F
            ? 1L << (RomSizeCode + 10)
            : 0;

    /// <summary>
    /// Tamaño de RAM declarado, expresado en bytes.
    /// </summary>
    public long DeclaredRamSizeBytes =>
        RamSizeCode == 0
            ? 0
            : RamSizeCode <= 0x1F
                ? 1L << (RamSizeCode + 10)
                : 0;
}