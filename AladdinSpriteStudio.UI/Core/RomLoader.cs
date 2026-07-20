namespace AladdinSpriteStudio.UI.Core;

public static class RomLoader
{
    public static Rom Load(string fileName)
    {
        byte[] data = File.ReadAllBytes(fileName);

        return new Rom(fileName, data);
    }
}