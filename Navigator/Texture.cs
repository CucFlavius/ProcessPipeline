using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessPipeline;

    public class Texture : IDisposable
    {
        private uint _handle;
        private readonly GL? _gl;

        public unsafe Texture(GL? gl, string path)
        {
            _gl = gl;
            
            //Loading an image using imagesharp.
            var image = (Image<Rgba32>) Image.Load(path);

            // OpenGL has image origin in the bottom-left corner.
            fixed (void* data = image.DangerousGetPixelRowMemory(0).Span)
            {
                Load(data, (uint) image.Width, (uint) image.Height);
            }

            //Deleting the img from imagesharp.
            image.Dispose();
        }
        
        public unsafe Texture(GL? gl, Image<Rgba32> image)
        {
            _gl = gl;
            
            // OpenGL has image origin in the bottom-left corner.
            fixed (void* data = image.DangerousGetPixelRowMemory(0).Span)
            {
                Load(data, (uint) image.Width, (uint) image.Height);
            }
        }

        public unsafe Texture(GL? gl, Span<byte> data, uint width, uint height)
        {
            _gl = gl;
            //We want the ability to create a texture using data generated from code as well.
            fixed (void* d = &data[0])
            {
                Load(d, width, height);
            }
        }

        private unsafe void Load(void* data, uint width, uint height)
        {
            if (_gl == null) return;
            
            //Generating the opengl handle;
            _handle = _gl.GenTexture();
            Bind();

            //Setting the data of a texture.
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Setting some texture parameters so the texture behaves as expected.
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);

            //Generating mipmaps.
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        private void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            if (_gl == null) return;
            
            //When we bind a texture we can choose which textureslot we can bind it to.
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            //In order to dispose we need to delete the opengl handle for the texture.
            _gl?.DeleteTexture(_handle);
        }
        
        public nint Handle => (nint)_handle;
    }