namespace ProcessPipeline;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class ObjectDetector
{
    private static InferenceSession _session;
    private static string[] _classNames; // e.g., ["person","bicycle","car","building","house",...]

    public static void Initialize(string onnxModelPath, string[] classNames)
    {
        _session = new InferenceSession(onnxModelPath);
        _classNames = classNames;
    }

    public static List<DetectionResult> DetectObjects(Image<Rgba32> image)
    {
        // 1) Preprocess to match model’s input shape, e.g. 640×640
        const int inputSize = 460;
        using var resized = image.Clone(ctx => ctx.Resize(inputSize, inputSize));
        
        // Build input tensor: [1, 3, 640, 640], BGR or RGB as needed
        var inputTensor = Preprocess(resized);

        // 2) Inference
        var inputName = _session.InputMetadata.Keys.First();  // e.g. "images"
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using var outputs = _session.Run(inputs);

        // 3) Parse output
        //    Let's assume the model output is a Nx6 array: [x, y, w, h, confidence, classIndex]
        //    Real YOLO models can differ. Adjust parse logic to your model’s spec.

        var outputName = _session.OutputMetadata.Keys.First();
        var resultTensor = outputs.First(x => x.Name == outputName).AsTensor<float>();
        
        // Convert Nx6 to a list<DetectionResult>
        var detections = new List<DetectionResult>();
        int rowSize = 6; // [x, y, w, h, conf, classIndex]
        int totalRows = resultTensor.Dimensions[0];

        for (int i = 0; i < totalRows; i++)
        {
            float x      = resultTensor[i, 0];
            float y      = resultTensor[i, 1];
            float width  = resultTensor[i, 2];
            float height = resultTensor[i, 3];
            float conf   = resultTensor[i, 4];
            float cls    = resultTensor[i, 5];

            // Filter out low confidence
            if (conf < 0.5f) 
                continue;

            int classIndex = (int)cls;
            if (classIndex < 0 || classIndex >= _classNames.Length)
                continue;

            string label = _classNames[classIndex];

            // Convert box coords if needed:
            // YOLO might output XYWH in [0..640], map back to original image if you want.

            detections.Add(new DetectionResult
            {
                Label = label,
                Confidence = conf,
                BoundingBox = (x, y, width, height)
            });
        }

        return detections;
    }

    private static DenseTensor<float> Preprocess(Image<Rgba32> image)
    {
        const int targetWidth = 416;
        const int targetHeight = 416;

        // Resize the image to the target dimensions
        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        // Create a tensor with shape [1, targetHeight, targetWidth, 3]
        var tensor = new DenseTensor<float>(new[] {1, targetHeight, targetWidth, 3});

        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                Rgba32 pixel = image[x, y];
                // Store pixel values in HWC format (height, width, channels)
                tensor[0, y, x, 0] = pixel.R / 255f; // Red
                tensor[0, y, x, 1] = pixel.G / 255f; // Green
                tensor[0, y, x, 2] = pixel.B / 255f; // Blue
            }
        }

        return tensor;
    }

}

public class DetectionResult
{
    public string Label { get; init; }
    public float Confidence { get; set; }
    // You could store a bounding box as a rectangle or tuple
    public (float X, float Y, float W, float H) BoundingBox { get; set; }
}