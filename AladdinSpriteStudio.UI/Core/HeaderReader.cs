using System.Buffers.Binary;
using System.Text;

namespace AladdinSpriteStudio.UI.Core;

/// <summary>
/// Localiza y lee el encabezado interno de una ROM de Super Nintendo.
/// </summary>
public static class HeaderReader
{
    private const int CopierHeaderSize = 0x200;
    private const int InternalHeaderSize = 0x40;

    private const int LoRomHeaderOffset = 0x7FC0;
    private const int HiRomHeaderOffset = 0xFFC0;
    private const int ExHiRomHeaderOffset = 0x40FFC0;

    /// <summary>
    /// Busca y devuelve el encabezado más probable de la ROM.
    /// </summary>
    public static RomHeader Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        bool hasCopierHeader =
            data.Length % 0x8000 == CopierHeaderSize;

        int adjustment =
            hasCopierHeader ? CopierHeaderSize : 0;

        List<HeaderCandidate> candidates = [];

        AddCandidate(
            candidates,
            data,
            LoRomHeaderOffset + adjustment,
            HeaderKind.LoRom,
            hasCopierHeader);

        AddCandidate(
            candidates,
            data,
            HiRomHeaderOffset + adjustment,
            HeaderKind.HiRom,
            hasCopierHeader);

        AddCandidate(
            candidates,
            data,
            ExHiRomHeaderOffset + adjustment,
            HeaderKind.ExHiRom,
            hasCopierHeader);

        if (candidates.Count == 0)
        {
            throw new InvalidDataException(
                "La ROM es demasiado pequeña o no contiene " +
                "un encabezado interno reconocible.");
        }

        HeaderCandidate bestCandidate = candidates
            .OrderByDescending(candidate => candidate.Score)
            .First();

        if (bestCandidate.Score < 4)
        {
            throw new InvalidDataException(
                "No se pudo identificar con seguridad " +
                "el encabezado interno de la ROM.");
        }

        return bestCandidate.Header;
    }

    private static void AddCandidate(
        ICollection<HeaderCandidate> candidates,
        byte[] data,
        int offset,
        HeaderKind expectedKind,
        bool hasCopierHeader)
    {
        if (offset < 0 ||
            offset + InternalHeaderSize > data.Length)
        {
            return;
        }

        RomHeader header = ParseHeader(
            data,
            offset,
            hasCopierHeader);

        int score = CalculateScore(
            data,
            header,
            expectedKind);

        candidates.Add(
            new HeaderCandidate(header, score));
    }

    private static RomHeader ParseHeader(
        byte[] data,
        int offset,
        bool hasCopierHeader)
    {
        string title = Encoding.ASCII
            .GetString(data, offset, 21)
            .TrimEnd('\0', ' ');

        ushort checksumComplement =
            BinaryPrimitives.ReadUInt16LittleEndian(
                data.AsSpan(offset + 0x1C, 2));

        ushort checksum =
            BinaryPrimitives.ReadUInt16LittleEndian(
                data.AsSpan(offset + 0x1E, 2));

        return new RomHeader
        {
            HeaderOffset = offset,
            HasCopierHeader = hasCopierHeader,
            Title = title,
            MapMode = data[offset + 0x15],
            CartridgeType = data[offset + 0x16],
            RomSizeCode = data[offset + 0x17],
            RamSizeCode = data[offset + 0x18],
            CountryCode = data[offset + 0x19],
            LicenseeCode = data[offset + 0x1A],
            Version = data[offset + 0x1B],
            ChecksumComplement = checksumComplement,
            Checksum = checksum
        };
    }

    private static int CalculateScore(
        byte[] data,
        RomHeader header,
        HeaderKind expectedKind)
    {
        int score = 0;

        if (header.HasValidChecksum)
        {
            score += 4;
        }

        if (MapModeMatches(
                header.MapMode,
                expectedKind))
        {
            score += 4;
        }

        if (HasReadableTitle(
                data,
                header.HeaderOffset))
        {
            score += 2;
        }

        ushort resetVector =
            BinaryPrimitives.ReadUInt16LittleEndian(
                data.AsSpan(
                    header.HeaderOffset + 0x3C,
                    2));

        if (resetVector >= 0x8000)
        {
            score += 2;
        }

        if (!string.IsNullOrWhiteSpace(header.Title))
        {
            score++;
        }

        return score;
    }

    private static bool MapModeMatches(
        byte mapMode,
        HeaderKind expectedKind)
    {
        return expectedKind switch
        {
            HeaderKind.LoRom =>
                mapMode is 0x20 or 0x30,

            HeaderKind.HiRom =>
                mapMode is 0x21 or 0x31,

            HeaderKind.ExHiRom =>
                mapMode is 0x25 or 0x35,

            _ => false
        };
    }

    private static bool HasReadableTitle(
        byte[] data,
        int offset)
    {
        int readableBytes = 0;

        for (int index = 0; index < 21; index++)
        {
            byte value = data[offset + index];

            bool isReadable =
                value == 0x00 ||
                value == 0x20 ||
                value is >= 0x21 and <= 0x7E;

            if (isReadable)
            {
                readableBytes++;
            }
        }

        return readableBytes >= 16;
    }

    private readonly record struct HeaderCandidate(
        RomHeader Header,
        int Score);

    private enum HeaderKind
    {
        LoRom,
        HiRom,
        ExHiRom
    }
}