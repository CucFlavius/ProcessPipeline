using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ProcessPipeline;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public static class AiComparer
{
    private static InferenceSession _session;
    private static DenseTensor<float> tensor;
    
    public static void Initialize()
    {
        var modelPath = @"D:\Projects\Navigator\Models\bat_resnext26ts_Opset18.onnx";
        _session = new InferenceSession(modelPath);
        tensor = new DenseTensor<float>(new[] { 1, 3, 256, 256 });
    }

    public static float[] ExtractEmbedding(Image<Rgba32> image, Tensor<float> inputTensor)
    {
        // 1) Convert imageData into the model's expected tensor shape:
        //    e.g. [batch=1, channels=3, height=224, width=224] if that's what your model expects.
        //    We'll need a helper method (not shown) to do actual resizing, normalization, etc.
        PreprocessImage(image, inputTensor);

        // 2) Create NamedOnnxValue
        var inputName = _session.InputMetadata.Keys.First();  // e.g. "input"
        var inputData = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        // 3) Run inference
        using var results = _session.Run(inputData);

        // 4) The model's output name
        var outputName = _session.OutputMetadata.Keys.First(); // e.g. "output"
        var output = results.First(x => x.Name == outputName).AsEnumerable<float>().ToArray();
        
        return output;
    }

    public static Tensor<float> PreprocessImage(Image<Rgba32> image, Tensor<float> tensor)
    {
        // 1) Resize to model's expected size (example: 224x224)
        var clone = image.Clone();
        clone.Mutate(ctx => ctx.Resize(256, 256));

        int width = clone.Width;   // now 224
        int height = clone.Height; // now 224

        // 2) Create a DenseTensor for shape [1, 3, height, width]
        //var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

        // 3) Fill the tensor with normalized pixel data (B, G, R)
        image.ProcessPixelRows(pixelAccessor => {

            for (var y = 0; y < height; y++) {
                var rowSpan = pixelAccessor.GetRowSpan(y);

                for (int x = 0; x < width; x++) {
                    tensor[0, 0, y, x] = rowSpan[x].R;
                    tensor[0, 1, y, x] = rowSpan[x].G;
                    tensor[0, 2, y, x] = rowSpan[x].B;
                }
            }
        });
        // Optionally apply mean/std if your model requires it
        // For example:
        //   float[] mean = {0.485f, 0.456f, 0.406f};
        //   float[] std  = {0.229f, 0.224f, 0.225f};
        //   // then loop over each pixel and do (value - mean[c]) / std[c]

        return tensor;
    }

    /// <summary>
    /// Compare two Image<Rgba32> using an ONNX model that outputs embeddings.
    /// Returns a difference percentage in [0..100], where 0 means "identical" and 100 means "totally different."
    /// </summary>
    public static double CompareUsingAi(Image<Rgba32> imgA, Image<Rgba32> imgB)
    {
        // 1) Extract embeddings
        float[] embA = ExtractEmbedding(imgA, tensor);
        float[] embB = ExtractEmbedding(imgB, tensor);

        // 2) Compute a similarity measure (e.g., cosine similarity)
        double similarity = CosineSimilarity(embA, embB);

        // 3) Convert to a "difference" measure (0% = same, 100% = different)
        double difference = 100.0 * (1.0 - similarity);

        return difference;
    }

    /// <summary>
    /// Simple Cosine Similarity in [0..1].
    /// </summary>
    private static double CosineSimilarity(float[] vecA, float[] vecB)
    {
        if (vecA.Length != vecB.Length) 
            return 0.0; // or throw an exception

        double dot = 0.0;
        double magA = 0.0;
        double magB = 0.0;

        for (int i = 0; i < vecA.Length; i++)
        {
            dot += vecA[i] * vecB[i];
            magA += vecA[i] * vecA[i];
            magB += vecB[i] * vecB[i];
        }

        magA = Math.Sqrt(magA);
        magB = Math.Sqrt(magB);
        if (magA == 0.0 || magB == 0.0) 
            return 0.0;

        return dot / (magA * magB);
    }
    
    /// <summary>
    /// Converts the list of images into batches and list of input tensors.
    /// </summary>
    /// <param name="images"></param>
    /// <param name="mean"></param>
    /// <param name="stddev"></param>
    /// <param name="inputDimension">The size of the tensor that the OnnxRuntime model is expecting [1, 3, 224, 224] </param>
    /// <returns></returns>
    private static List<DenseTensor<float>> ImageToTensor(List<Image<Rgb24>> images, float[] mean, float[] stddev, int[] inputDimension)
    {
        // Used to create more than one batch
        int numberBatches = 1;

        // If required, can create batches of different sizes
        var batchSizes = new int[] {images.Count};

        // Keep track of which tile we are using to create multiple batches.
        int tileIndex = 0;

        var strides = GetStrides(inputDimension);

        var inputs = new List<DenseTensor<float>>();

        for (var j = 0; j < numberBatches; j++)
        {

            inputDimension[0] = batchSizes[j];

            // Need to directly use a DenseTensor here because we need access to the underlying span.
            DenseTensor<float> input = new DenseTensor<float>(inputDimension);

            // This is the index used to get the stride offsets for the span.
            // mdIndex[0] = row in the batch.
            // mdIndex[1] = either 1/2/3 depending on if its for RBG
            // mdIndex[2] = y value corresponding to the current image height
            // mdIndex[3] = Always 0 since we want the start index of the x values
            int[] mdIndex = new int[4];
            mdIndex[3] = 0;
            for (var i = 0; i < batchSizes[j]; i++)
            {
                mdIndex[0] = i;
                var image = images[tileIndex];
                image.ProcessPixelRows(pixelAccessor =>
                {
                    var inputSpan = input.Buffer.Span;
                    for (var y = 0; y < image.Height; y++)
                    {
                        mdIndex[2] = y;

                        var rowSpan = pixelAccessor.GetRowSpan(y);

                        // Update the mdIndex based on R/G/B and get a span to the underlying memory
                        mdIndex[1] = 0;
                        var spanR = inputSpan.Slice(GetIndex(strides, mdIndex), image.Width);
                        mdIndex[1] = 1;
                        var spanG = inputSpan.Slice(GetIndex(strides, mdIndex), image.Width);
                        mdIndex[1] = 2;
                        var spanB = inputSpan.Slice(GetIndex(strides, mdIndex), image.Width);

                        // Now we can just directly loop through and copy the values directly from span to span.
                        for (int x = 0; x < image.Width; x++)
                        {
                            spanR[x] = ((rowSpan[x].R / 255f) - mean[0]) / stddev[0];
                            spanG[x] = ((rowSpan[x].G / 255f) - mean[1]) / stddev[1];
                            spanB[x] = ((rowSpan[x].B / 255f) - mean[2]) / stddev[2];
                        }
                    }
                });

                tileIndex++;
                inputs.Add(input);
            }
        }

        return inputs;
    }


    /// <summary>
    /// Gets the set of strides that can be used to calculate the offset of n-dimensions in a 1-dimensional layout
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="reverseStride"></param>
    /// <returns></returns>
    public static int[] GetStrides(ReadOnlySpan<int> dimensions, bool reverseStride = false)
    {
        int[] strides = new int[dimensions.Length];

        if (dimensions.Length == 0)
        {
            return strides;
        }

        int stride = 1;
        if (reverseStride)
        {
            for (int i = 0; i < strides.Length; i++)
            {
                strides[i] = stride;
                stride *= dimensions[i];
            }
        }
        else
        {
            for (int i = strides.Length - 1; i >= 0; i--)
            {
                strides[i] = stride;
                stride *= dimensions[i];
            }
        }

        return strides;
    }


    /// <summary>
    /// Calculates the 1-d index for n-d indices in layout specified by strides.
    /// </summary>
    /// <param name="strides"></param>
    /// <param name="indices"></param>
    /// <param name="startFromDimension"></param>
    /// <returns></returns>
    public static int GetIndex(int[] strides, ReadOnlySpan<int> indices, int startFromDimension = 0)
    {
        Debug.Assert(strides.Length == indices.Length);

        int index = 0;
        for (int i = startFromDimension; i < indices.Length; i++)
        {
            index += strides[i] * indices[i];
        }

        return index;
    }
}