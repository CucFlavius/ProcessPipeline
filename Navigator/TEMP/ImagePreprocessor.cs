using System;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class ImagePreprocessor
{
    /// <summary>
    /// Decodes the image from <paramref name="imageData"/>, resizes to 224×224,
    /// and converts it into a DenseTensor<float> of shape [1,3,224,224].
    /// Pixel values are normalized to [0..1].
    /// 
    /// Note: Adjust channel order (B,G,R vs. R,G,B) or size (e.g., 224×224)
    /// to match your specific ONNX model’s requirements.
    /// </summary>
    /// <param name="imageData">Raw image bytes (e.g., PNG/JPG file content).</param>
    /// <returns>A DenseTensor<float> for feeding into an ONNX model.</returns>
    public static Tensor<float> PreprocessImage(byte[] imageData)
    {
        // 1) Decode the image using ImageSharp
        using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

        // 2) Resize to the dimensions your model expects. Here, 224×224 as an example.
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(224, 224),
            Mode = ResizeMode.Stretch
        }));

        // 3) Create a tensor with shape [1, 3, 224, 224]
        //    - '1' for batch size
        //    - '3' for channels (B, G, R) or (R, G, B)
        //    - '224' for height
        //    - '224' for width
        var height = image.Height;
        var width = image.Width;
        
        var frame = image.Frames.RootFrame;
        var pixelMemoryGroup = frame.GetPixelMemoryGroup();
        
        var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

        // 4) Convert each pixel to float in [0..1]. 
        //    *Order the channels* according to what your model expects.
        //    Commonly in PyTorch-based models, the default is [B, G, R].
        //    If your model doc says it needs [R, G, B], adjust accordingly.

        for (int y = 0; y < height; y++)
        {
            // Each row is pixelMemoryGroup[y].Span, if one row = one group
            Span<Rgba32> rowSpan = pixelMemoryGroup[y].Span;

            for (int x = 0; x < width; x++)
            {
                Rgba32 pixel = rowSpan[x];

                // Example: B, G, R channel order
                tensor[0, 0, y, x] = pixel.B / 255f;
                tensor[0, 1, y, x] = pixel.G / 255f;
                tensor[0, 2, y, x] = pixel.R / 255f;
            }
        }

        // 5) Optionally apply mean/std normalization
        //    For example, some ImageNet-based models do:
        //       mean = [0.485, 0.456, 0.406]
        //       std  = [0.229, 0.224, 0.225]
        //    Then you'd do something like:
        //    for (int c = 0; c < 3; c++)
        //    {
        //        for (int y = 0; y < height; y++)
        //        {
        //            for (int x = 0; x < width; x++)
        //            {
        //                float value = tensor[0, c, y, x];
        //                value = (value - mean[c]) / std[c];
        //                tensor[0, c, y, x] = value;
        //            }
        //        }
        //    }

        return tensor;
    }
}