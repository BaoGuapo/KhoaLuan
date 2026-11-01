using SkiaSharp;

namespace QuanLySinhVien.Api.Services;

internal sealed class FaceImage(SKBitmap bitmap) : IDisposable
{
    public int Width => bitmap.Width;
    public int Height => bitmap.Height;
    public byte[] EncodeJpeg() { using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85); return data.ToArray(); }
    public SKColor this[int x, int y] => bitmap.GetPixel(x, y);
    public static FaceImage Decode(SKCodec codec)
    {
        using var original = new SKBitmap(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        if (codec.GetPixels(original.Info, original.GetPixels()) != SKCodecResult.Success) throw new FormatException();
        var orientation = codec.EncodedOrigin;
        var rotated = orientation is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var output = new SKBitmap(rotated ? original.Height : original.Width, rotated ? original.Width : original.Height);
        using var canvas = new SKCanvas(output);
        switch (orientation)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(output.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(output.Width, output.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, output.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(output.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(output.Width, output.Height); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, output.Height); canvas.RotateDegrees(-90); break;
        }
        canvas.DrawBitmap(original, 0, 0);
        return new(output);
    }
    public FaceImage Resize(int width, int height, SKRect? source = null)
    {
        var output = new SKBitmap(width, height);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { IsAntialias = true };
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, source ?? new SKRect(0, 0, Width, Height),
            new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear), paint);
        return new(output);
    }
    public void Dispose() => bitmap.Dispose();
}
