using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace LootPlacesWeb
{
    public static class SvgRenderer
    {
        public static byte[] ToPng(string svgData, int width = -1, int height = -1)
        {
            using var inStream = new MemoryStream(Encoding.UTF8.GetBytes(svgData ?? ""));
            var svg = new SKSvg();
            svg.Load(inStream);
            if (svg.Drawable == null) throw new Exception("svg.Load failed");

            if (width <= 0) width = (int)svg.Drawable.Bounds.Width;
            if (height <= 0) height = (int)svg.Drawable.Bounds.Height;

            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);
            canvas.Flush();
            canvas.Save();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 80);
            using var svgStream = data.AsStream();
            using var outStream = new MemoryStream();
            svgStream.CopyTo(outStream);
            return outStream.ToArray();
        }
    }
}
