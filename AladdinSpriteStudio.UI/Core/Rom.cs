namespace AladdinSpriteStudio.UI.Core;

public sealed class Rom
{
    public string FileName { get; }

    public byte[] Data { get; }

    public int Size => Data.Length;

    public Rom(string fileName, byte[] data)
    {
        FileName = fileName;
        Data = data;
    }
}