using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Primitives;
using Statiq.Common;

namespace Statiq.Images.Operations
{
    internal class WatermarkOperation : IImageOperation
    {
        private readonly string _content;
        private readonly Font _font;
        public WatermarkOperation(string content, Font font)
        {
            _content = content;
            _font = font;
        }

        public IImageProcessingContext<Rgba32> Apply(IImageProcessingContext<Rgba32> image)
        {
            return ApplyScalingWaterMarkWordWrap(image, _font, _content, Rgba32.White, 5);
        }

        public NormalizedPath GetPath(NormalizedPath path)
        {
            string unsafeContent = _content;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                unsafeContent.Replace(invalidChar.ToString(), string.Empty);
            }

            int maxLength = Math.Min(unsafeContent.Length, 20);
            string truncatedContent = unsafeContent.Substring(0, maxLength);

            return path.InsertSuffix($"-wm_{truncatedContent}_");
        }

        private IImageProcessingContext<Rgba32> ApplyScalingWaterMark(
            IImageProcessingContext<Rgba32> processingContext,
            Font font,
            string text,
            Rgba32 color,
            float padding,
            bool wordwrap)
        {
            if (wordwrap)
            {
                return ApplyScalingWaterMarkWordWrap(processingContext, font, text, color, padding);
            }
            else
            {
                return ApplyScalingWaterMarkSimple(processingContext, font, text, color, padding);
            }
        }

        private IImageProcessingContext<Rgba32> ApplyScalingWaterMarkSimple(
            IImageProcessingContext<Rgba32> processingContext,
            Font font,
            string text,
            Rgba32 color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();

            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            // measure the text size
            SizeF size = TextMeasurer.Measure(text, new RendererOptions(font));

            // find out how much we need to scale the text to fill the space (up or down)
            float scalingFactor = Math.Min(imgSize.Width / size.Width, imgSize.Height / size.Height);

            // create a new font
            Font scaledFont = new Font(font, scalingFactor * font.Size);

            PointF center = new PointF(imgSize.Width / 2, imgSize.Height / 2);
            TextGraphicsOptions textGraphicOptions = new TextGraphicsOptions()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return processingContext.DrawText(textGraphicOptions, text, scaledFont, color, center);
        }

        private IImageProcessingContext<Rgba32> ApplyScalingWaterMarkWordWrap(
            IImageProcessingContext<Rgba32> processingContext,
            Font font,
            string text,
            Rgba32 color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            float targetMinHeight = imgSize.Height - (padding * 3); // must be with in a margin width of the target height

            // now we are working i 2 dimensions at once and can't just scale because it will cause the text to
            // reflow we need to just try multiple times

            Font scaledFont = font;
            SizeF s = new SizeF(float.MaxValue, float.MaxValue);

            float scaleFactor = scaledFont.Size / 2; // every time we change direction we half this size
            int trapCount = (int)scaledFont.Size * 2;
            if (trapCount < 10)
            {
                trapCount = 10;
            }

            bool isTooSmall = false;

            while ((s.Height > targetHeight || s.Height < targetMinHeight) && trapCount > 0)
            {
                if (s.Height > targetHeight)
                {
                    if (isTooSmall)
                    {
                        scaleFactor = scaleFactor / 2;
                    }

                    scaledFont = new Font(scaledFont, scaledFont.Size - scaleFactor);
                    isTooSmall = false;
                }

                if (s.Height < targetMinHeight)
                {
                    if (!isTooSmall)
                    {
                        scaleFactor = scaleFactor / 2;
                    }
                    scaledFont = new Font(scaledFont, scaledFont.Size + scaleFactor);
                    isTooSmall = true;
                }
                trapCount--;

                s = TextMeasurer.Measure(text, new RendererOptions(scaledFont)
                {
                    WrappingWidth = targetWidth
                });
            }

            PointF center = new PointF(padding, imgSize.Height / 2);
            TextGraphicsOptions textGraphicOptions = new TextGraphicsOptions()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                WrapTextWidth = targetWidth
            };
            return processingContext.DrawText(textGraphicOptions, text, scaledFont, color, center);
        }
    }
}