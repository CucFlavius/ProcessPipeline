using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;

namespace ProcessPipeline
{
    public class Texture : IDisposable
    {
        private uint _handle;
        private readonly GL? _gl;
        public int Width { get; private set; }
        public int Height { get; private set; }
        private byte[]? _pixels;
        private bool _disposed = false;

        // Constructors
        public unsafe Texture(GL? gl, string path)
        {
            _gl = gl;
            LoadFromPath(path);
        }

        public unsafe Texture(GL? gl, Image<Rgba32> image)
        {
            _gl = gl;
            LoadFromImage(image);
        }

        public unsafe Texture(GL? gl, Span<byte> data, uint width, uint height)
        {
            _gl = gl;
            LoadFromData(data, width, height);
        }

        // SetData Methods
        public unsafe void SetData(string path)
        {
            EnsureNotDisposed();
            using var image = Image.Load<Rgba32>(path);
            SetDataInternal(image);
        }

        public unsafe void SetData(Image<Rgba32> image)
        {
            EnsureNotDisposed();
            SetDataInternal(image);
        }

        public unsafe void SetData(ReadOnlySpan<byte> data, uint width, uint height)
        {
            EnsureNotDisposed();
            SetDataInternal(data, width, height);
        }

        // Private Load Methods
        private unsafe void LoadFromPath(string path)
        {
            using var image = Image.Load<Rgba32>(path);
            SetDataInternal(image);
        }

        private unsafe void LoadFromImage(Image<Rgba32> image)
        {
            SetDataInternal(image);
        }

        private unsafe void LoadFromData(ReadOnlySpan<byte> data, uint width, uint height)
        {
            SetDataInternal(data, width, height);
        }

        // Internal SetData Implementations
        private unsafe void SetDataInternal(Image<Rgba32> image)
        {
            // Process the image
            var processedImage = ProcessImage(image);
            
            Width = processedImage.Width;
            Height = processedImage.Height;

            // Flip vertically
            processedImage.Mutate(ctx => ctx.Flip(FlipMode.Vertical));

            // Extract pixel data
            byte[] pixelData = ExtractPixelData(processedImage);

            // Bind the texture
            Bind();

            // Upload data using unsafe pointer
            fixed (byte* p = pixelData)
            {
                _gl?.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)processedImage.Width, (uint)processedImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }

            // Set texture parameters (if not already set)
            SetTextureParameters();

            // Generate mipmaps
            _gl?.GenerateMipmap(TextureTarget.Texture2D);

            // Dispose processed image
            processedImage.Dispose();
        }

        private unsafe void SetDataInternal(ReadOnlySpan<byte> data, uint width, uint height)
        {
            Width = (int)width;
            Height = (int)height;

            // Ensure pixel buffer is the correct size
            if (_pixels == null || _pixels.Length != width * height * 4)
            {
                _pixels = new byte[width * height * 4];
            }

            data.CopyTo(_pixels.AsSpan());

            // Bind the texture
            Bind();

            // Upload data using unsafe pointer
            fixed (byte* p = _pixels)
            {
                _gl?.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }

            // Set texture parameters (if not already set)
            SetTextureParameters();

            // Generate mipmaps
            _gl?.GenerateMipmap(TextureTarget.Texture2D);
        }

        // Helper Methods
        private void SetTextureParameters()
        {
            if (_gl == null) return;

            // Set texture parameters only once or if needed
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Texture));
        }

        private void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            if (_gl == null) return;

            // Bind the texture to the specified texture slot.
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Delete the OpenGL texture handle.
                _gl?.DeleteTexture(_handle);
                _disposed = true;
            }
        }

        public nint Handle
        {
            get
            {
                EnsureNotDisposed();
                return (nint)_handle;
            }
        }

        /// <summary>
        /// Processes the image to fit into a POT dimension with the nearest aspect ratio (1:1, 1:2, 2:1).
        /// Pads with transparent pixels as necessary.
        /// </summary>
        /// <param name="image">The original image.</param>
        /// <returns>A new image with POT dimensions and the chosen aspect ratio.</returns>
        private Image<Rgba32> ProcessImage(Image<Rgba32> image)
        {
            // Define the target aspect ratios.
            var targetAspectRatios = new (float ratio, string name)[]
            {
                (1.0f, "1:1"),
                (0.5f, "1:2"),
                (2.0f, "2:1")
            };

            // Calculate the original aspect ratio.
            float originalAspect = (float)image.Width / image.Height;

            // Find the closest target aspect ratio.
            var closestAspect = targetAspectRatios.OrderBy(ar => Math.Abs(ar.ratio - originalAspect)).First().ratio;

            // Determine the target aspect ratio based on the closest match.
            float targetWidthRatio = 1.0f;
            float targetHeightRatio = 1.0f;

            if (Math.Abs(closestAspect - 0.5f) < 0.001f)
            {
                // 1:2 aspect ratio
                targetWidthRatio = 1.0f;
                targetHeightRatio = 2.0f;
            }
            else if (Math.Abs(closestAspect - 2.0f) < 0.001f)
            {
                // 2:1 aspect ratio
                targetWidthRatio = 2.0f;
                targetHeightRatio = 1.0f;
            }
            else
            {
                // 1:1 aspect ratio
                targetWidthRatio = 1.0f;
                targetHeightRatio = 1.0f;
            }

            // Calculate the scaling factor to fit the original image into the target aspect ratio.
            float widthScale = targetWidthRatio / originalAspect;
            float heightScale = targetHeightRatio;

            float scale = Math.Min(widthScale, heightScale);

            // Calculate the new size to fit within the target aspect ratio.
            int newWidth = (int)Math.Ceiling(image.Width * scale);
            int newHeight = (int)Math.Ceiling(image.Height * scale);

            // Resize the image if necessary.
            var resizedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight));

            // Calculate the next power of two for the target dimensions.
            int potWidth = NextPowerOfTwo(newWidth);
            int potHeight = NextPowerOfTwo(newHeight);

            // Adjust the POT dimensions based on the target aspect ratio.
            if (targetWidthRatio > targetHeightRatio)
            {
                potHeight = potWidth / (int)targetWidthRatio;
                potHeight = NextPowerOfTwo(potHeight); // Ensure potHeight is still a power of two
            }
            else if (targetHeightRatio > targetWidthRatio)
            {
                potWidth = potHeight * (int)targetHeightRatio;
                potWidth = NextPowerOfTwo(potWidth); // Ensure potWidth is still a power of two
            }

            // Create a new transparent image with POT dimensions.
            var finalImage = new Image<Rgba32>(potWidth, potHeight, new Rgba32(0, 0, 0, 0));

            // Calculate the position to center the resized image.
            int posX = (potWidth - resizedImage.Width) / 2;
            int posY = (potHeight - resizedImage.Height) / 2;

            // Draw the resized image onto the transparent POT image.
            finalImage.Mutate(ctx => ctx.DrawImage(resizedImage, new Point(posX, posY), 1f));

            // Dispose the resized image.
            resizedImage.Dispose();

            return finalImage;
        }

        /// <summary>
        /// Extracts pixel data from the image into a byte array in RGBA format.
        /// </summary>
        /// <param name="image">The image to extract pixel data from.</param>
        /// <returns>A byte array containing pixel data in RGBA order.</returns>
        private byte[] ExtractPixelData(Image<Rgba32> image)
        {
            var width = image.Width;
            var height = image.Height;
            if (_pixels == null || _pixels.Length != width * height * 4)
            {
                _pixels = new byte[width * height * 4]; // 4 bytes per pixel (RGBA)
            }

            for (var y = 0; y < height; y++)
            {
                // ImageSharp's row indexing starts at the top, so to flip vertically:
                var flippedY = height - y - 1;
                var pixelRowSpan = image.DangerousGetPixelRowMemory(y).Span;

                for (var x = 0; x < width; x++)
                {
                    var pixelIndex = (flippedY * width + x) * 4;
                    var pixel = pixelRowSpan[x];
                    _pixels[pixelIndex + 0] = pixel.R;
                    _pixels[pixelIndex + 1] = pixel.G;
                    _pixels[pixelIndex + 2] = pixel.B;
                    _pixels[pixelIndex + 3] = pixel.A;
                }
            }

            return _pixels;
        }

        /// <summary>
        /// Calculates the next power of two greater than or equal to the given number.
        /// </summary>
        /// <param name="n">The input number.</param>
        /// <returns>The next power of two.</returns>
        private int NextPowerOfTwo(int n)
        {
            if (n < 1)
                return 1;
            int power = 1;
            while (power < n)
                power <<= 1;
            return power;
        }
    }
}
