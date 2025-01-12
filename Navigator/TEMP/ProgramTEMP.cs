using System.Diagnostics;
using LibVLCSharp.Shared;
using ProcessPipeline;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

class ProgramTEMP
{
    private static Process? _ffmpegProcess;
    
    // We'll store some frame-related info in static fields for brevity:
    private static int _width;
    private static int _height;
    private static int _pitch;
    private static byte[]? _frameBuffer;
    
    static void MainTEMP(string[] args)
    {
        string videoPath = "D:\\Projects\\Navigator\\TestData\\One Piece - 0892.mkv";

        string modelPath = "D:\\Projects\\Navigator\\Models\\yolov4.onnx";
        string[] classNames = File.ReadAllLines("D:\\Projects\\Navigator\\Models\\coco.names"); 
        ObjectDetector.Initialize(modelPath, classNames);
        
        double differenceThreshold = 10.0;

        // Initialize the AI model once
        AiComparer.Initialize();
        
        // 1) Create extractor
        var extractor = new LibVlcFrameExtractor(videoPath);

        // 2) Start playback => frames begin to flow
        extractor.Start();

        // 3) Pull frames from the extractor and do your pipeline
        //    This is conceptually similar to 'ExtractFrames' in your old approach.
        //    We'll process frames in real time as they arrive.
        var frames = extractor.GetFrames();

        ProcessFrames(frames, differenceThreshold);

        // 4) Program ends. 
        //    If you want to stop early, call extractor.Stop().
        //    Disposal of 'extractor' will clean up.
        Console.WriteLine("Processing complete. Press any key to exit.");
        Console.ReadKey();
    }

    private static void ProcessFrames(IEnumerable<Image<Rgba32>> frames, double differenceThreshold)
    {
        var frameBuffer = new Queue<Image<Rgba32>>();
        int frameCounter = 0;
        
        foreach (var frame in frames)
        {
            frameCounter++;
            
            // Same logic you used before:
            if (frameBuffer.Count == 0)
            {
                frameBuffer.Enqueue(frame);
                continue;
            }

            var prevFrame = frameBuffer.Peek();
            //double diff = GetDifferencePercentage(prevFrame, frame);
            double diff = GetDifferenceUsingAi(prevFrame, frame);

            if (diff > differenceThreshold)
            {
                CategorizeAndSave(frame, frameCounter);
            }

            var frameOld = frameBuffer.Dequeue();
            frameOld.Dispose();
            frameBuffer.Enqueue(frame);
        }
        
        // Dispose of any remaining frames in the buffer
        while (frameBuffer.Count > 0)
        {
            var frameOld = frameBuffer.Dequeue();
            frameOld.Dispose();
        }
        
        GC.Collect();
    }
    
    public static double GetDifferencePercentage(Image<Rgba32> imgA, Image<Rgba32> imgB)
    {
        // Check dimension match
        if (imgA.Width != imgB.Width || imgA.Height != imgB.Height)
        {
            return 100.0;
        }

        // Access frames (if multi-frame, we take the RootFrame)
        var frameA = imgA.Frames.RootFrame;
        var frameB = imgB.Frames.RootFrame;

        // Access the pixel memory groups
        var pixelMemoryGroupA = frameA.GetPixelMemoryGroup();
        var pixelMemoryGroupB = frameB.GetPixelMemoryGroup();

        // Verify each "group" is indeed one row
        // (In most cases this is true and the count will match the image height.)
        if (pixelMemoryGroupA.Count != frameA.Height || pixelMemoryGroupB.Count != frameB.Height)
        {
            // Fallback: memory might be chunked differently.
            // If that happens, you have to handle it manually or use the indexer approach.
            return CompareUsingIndexer(imgA, imgB);
        }

        long diffSum = 0;
        long totalChannels = (long)frameA.Width * frameA.Height * 3; // R,G,B

        // Row-by-row iteration
        for (int y = 0; y < frameA.Height; y++)
        {
            // Each group entry is a row’s worth of pixels
            Span<Rgba32> rowA = pixelMemoryGroupA[y].Span;
            Span<Rgba32> rowB = pixelMemoryGroupB[y].Span;

            for (int x = 0; x < frameA.Width; x++)
            {
                Rgba32 pxA = rowA[x];
                Rgba32 pxB = rowB[x];

                diffSum += Math.Abs(pxA.R - pxB.R);
                diffSum += Math.Abs(pxA.G - pxB.G);
                diffSum += Math.Abs(pxA.B - pxB.B);
            }
        }

        double avgDiff = (double)diffSum / totalChannels;
        return (avgDiff / 255.0) * 100.0;
    }

    public static double GetDifferenceUsingAi(Image<Rgba32> imgA, Image<Rgba32> imgB)
    {
        return AiComparer.CompareUsingAi(imgA, imgB);
    }
    
    private static double CompareUsingIndexer(Image<Rgba32> imgA, Image<Rgba32> imgB)
    {
        // Fallback method if the memory groups aren't row-aligned
        // or you just prefer the simpler approach.
        long diffSum = 0;
        long totalChannels = (long)imgA.Width * imgA.Height * 3;

        for (int y = 0; y < imgA.Height; y++)
        {
            for (int x = 0; x < imgA.Width; x++)
            {
                Rgba32 pxA = imgA[x, y];
                Rgba32 pxB = imgB[x, y];

                diffSum += Math.Abs(pxA.R - pxB.R);
                diffSum += Math.Abs(pxA.G - pxB.G);
                diffSum += Math.Abs(pxA.B - pxB.B);
            }
        }

        double avgDiff = (double)diffSum / totalChannels;
        return (avgDiff / 255.0) * 100.0;
    }

    private static void CategorizeAndSave(Image<Rgba32> frame, int frameNumber)
    {
        // 2) Save to disk in a folder named after the category
        string outputDir = Path.Combine("OutputFrames");
        string outputDirClassified = Path.Combine(outputDir, "Classified");
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(outputDirClassified);
        string fileName = Path.Combine(outputDir, $"frame_{frameNumber:D8}.png");
        string fileNameClassified = Path.Combine(outputDirClassified, $"frame_{frameNumber:D8}.png");

        var targetLabels = new List<string> { "house", "building" };

        {
            var detections = ObjectDetector.DetectObjects(frame);
            bool containsTarget = detections
                .Any(d => targetLabels.Contains(d.Label, StringComparer.OrdinalIgnoreCase));

            if (containsTarget)
            {
                // Save or keep the frame
                //SaveFrame(frame, "OutputFrames");
                frame.Save(fileNameClassified); // save as PNG
                Console.WriteLine($"Saved frame to: {fileNameClassified}");
            }

            //frame.Dispose(); // if you're done
        }
        
        
        frame.Save(fileName); // save as PNG
    }
    
    public static IEnumerable<Image<Rgba32>> ExtractFrames(string videoPath, int frameStep, string tempOutputFolder)
    {
        // 1) Use ffmpeg to output frames to a folder
        // - start_number 0 ensures frames are numbered from 0, etc.
        // - the '-vf "select=' syntax can get you every Nth frame.
        //   Alternatively, you can extract every frame and skip in code.
        // This example extracts *every* frame, then we do the skipping in C#.

        var ffmpegArgs = $"-i \"{videoPath}\" -vsync 0 \"{tempOutputFolder}\\frame_%08d.png\"";
        RunFFmpeg(ffmpegArgs);

        // 2) Read the extracted PNGs from the folder
        //    This example enumerates all .png files and yields every Nth.
        var files = Directory.GetFiles(tempOutputFolder, "frame_*.png")
            .OrderBy(x => x);

        int counter = 0;
        foreach (var file in files)
        {
            if (counter % frameStep == 0)
            {
                // Load with ImageSharp
                using var image = Image.Load<Rgba32>(file);
            
                // In case you need to keep the image around,
                // clone it so you can safely yield (otherwise we’d
                // dispose it when exiting using{}).
                var frameClone = image.Clone();
                yield return frameClone;
            }

            counter++;
        }

        // Optionally, cleanup the frame files afterwards
        // Directory.Delete(tempOutputFolder, true);
    }

    private static void RunFFmpeg(string arguments)
    {
        var ffmpegPath = "../../../../Native/ffmpeg.exe";
        Console.WriteLine($"Using FFmpeg at: {ffmpegPath}");
        Console.WriteLine($"Exists? {File.Exists(ffmpegPath)}");
        var processStartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        _ffmpegProcess = process;
        
        process.Start();

        // Read logs
        string output = process.StandardOutput.ReadToEnd();
        string error  = process.StandardError.ReadToEnd();

        process.WaitForExit();

        // Print or log them
        Console.WriteLine("FFmpeg stdout:\n" + output);
        Console.WriteLine("FFmpeg stderr:\n" + error);

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"FFmpeg exited with error code {process.ExitCode}");
        }
        
        _ffmpegProcess = null;
    }
}