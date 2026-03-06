// This file completes the MLModel1 partial class.
// It provides the helper method and training-size constants that
// your auto-generated consumption.cs references.

namespace ObjectDetectionApp
{
    public partial class MLModel1
    {
        // ── Training image dimensions ─────────────────────────────────
        // Matches [ImageType(800, 600)] on ModelInput  →  height=800, width=600
        public const int TrainingImageHeight = 800;
        public const int TrainingImageWidth  = 600;

        // ── Aspect + letterbox-offset calculation ─────────────────────
        // Mirrors the logic ML.NET Model Builder uses when it resizes
        // images to fit the training canvas while preserving aspect ratio.
        public static void CalculateAspectAndOffset(
            int   srcWidth,  int   srcHeight,
            int   dstWidth,  int   dstHeight,
            out float xOffset, out float yOffset, out float aspect)
        {
            aspect  = System.Math.Min((float)dstWidth  / srcWidth,
                                      (float)dstHeight / srcHeight);
            xOffset = (dstWidth  - srcWidth  * aspect) / 2f;
            yOffset = (dstHeight - srcHeight * aspect) / 2f;
        }
    }
}
