using System;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Statiq.Common;

namespace Statiq.Images.Operations
{
    internal class WatermarkOperation : IImageOperation
    {
        private readonly string _content;
        public WatermarkOperation(string content)
        {
            _content = content;
        }

        public IImageProcessingContext<Rgba32> Apply(IImageProcessingContext<Rgba32> image)
        {
            throw new System.NotImplementedException();
        }

        public NormalizedPath GetPath(NormalizedPath path)
        {
            var unsafeContent = _content;

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                unsafeContent.Replace(invalidChar.ToString(), string.Empty);
            }

            var maxLength = Math.Min(unsafeContent.Length, 20);
            var truncatedContent = unsafeContent.Substring(0, maxLength);

            return path.InsertSuffix($"-wm_{truncatedContent}_");
        }
    }
}