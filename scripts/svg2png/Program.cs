using System.Globalization;
using SkiaSharp;
using Svg.Skia;

var argsList = args.ToList();
var svgPath = argsList.Count > 0 ? argsList[0] : Path.Combine("..","..","docs","architecture","architecture-diagram.svg");
var pngPath = argsList.Count > 1 ? argsList[1] : Path.Combine("..","..","docs","architecture","architecture-diagram.png");

svgPath = Path.GetFullPath(svgPath);
pngPath = Path.GetFullPath(pngPath);

if (!File.Exists(svgPath))
{
    Console.Error.WriteLine($"SVG not found: {svgPath}");
    return 2;
}

try
{
    using var stream = File.OpenRead(svgPath);
    var svg = new SKSvg();
    svg.Load(stream);
    var picture = svg.Picture;
    if (picture == null)
    {
        Console.Error.WriteLine("Failed to load SVG picture.");
        return 3;
    }

    var bounds = picture.CullRect;
    // Default width if bounds are empty
    var targetWidth = 1200;
    var targetHeight = 760;
    if (bounds.Width > 0 && bounds.Height > 0)
    {
        var ratio = (float)targetWidth / bounds.Width;
        targetHeight = (int)Math.Round(bounds.Height * ratio);
    }

    using var bitmap = new SKBitmap(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.White);

    // Scale to fit
    float scaleX = (float)targetWidth / (bounds.Width > 0 ? bounds.Width : targetWidth);
    float scaleY = (float)targetHeight / (bounds.Height > 0 ? bounds.Height : targetHeight);
    canvas.Scale(scaleX, scaleY);
    canvas.DrawPicture(picture);
    canvas.Flush();

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var outStream = File.OpenWrite(pngPath);
    data.SaveTo(outStream);

    Console.WriteLine($"Created PNG: {pngPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
