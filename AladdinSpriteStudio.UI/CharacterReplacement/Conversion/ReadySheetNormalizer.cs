namespace AladdinSpriteStudio.UI.CharacterReplacement.Conversion;

public static class ReadySheetNormalizer
{
    public static bool CanNormalizeProportionally(
        System.Drawing.Image sourceImage,
        System.Drawing.Size targetSize,
        double maximumRelativeAspectDifference = 0.01)
    {
        ArgumentNullException.ThrowIfNull(sourceImage);

        if (targetSize.Width <= 0 ||
            targetSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSize));
        }

        double sourceAspect =
            sourceImage.Width /
            (double)sourceImage.Height;

        double targetAspect =
            targetSize.Width /
            (double)targetSize.Height;

        double relativeDifference =
            Math.Abs(sourceAspect - targetAspect) /
            Math.Max(targetAspect, 0.000001);

        return relativeDifference <=
               maximumRelativeAspectDifference;
    }

    public static System.Drawing.Bitmap Normalize(
        System.Drawing.Image sourceImage,
        System.Drawing.Size targetSize)
    {
        ArgumentNullException.ThrowIfNull(sourceImage);

        if (targetSize.Width <= 0 ||
            targetSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSize));
        }

        System.Drawing.Bitmap result =
            new(
                targetSize.Width,
                targetSize.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using System.Drawing.Graphics graphics =
            System.Drawing.Graphics.FromImage(result);

        graphics.Clear(System.Drawing.Color.Transparent);

        graphics.CompositingMode =
            System.Drawing.Drawing2D.CompositingMode.SourceCopy;

        graphics.CompositingQuality =
            System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

        graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        graphics.PixelOffsetMode =
            System.Drawing.Drawing2D.PixelOffsetMode.Half;

        graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.None;

        graphics.DrawImage(
            sourceImage,
            new System.Drawing.Rectangle(
                0,
                0,
                targetSize.Width,
                targetSize.Height),
            new System.Drawing.Rectangle(
                0,
                0,
                sourceImage.Width,
                sourceImage.Height),
            System.Drawing.GraphicsUnit.Pixel);

        return result;
    }
}
