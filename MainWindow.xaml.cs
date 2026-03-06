using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace ObjectDetectionApp
{
    public partial class MainWindow : Window
    {
        private MLContext? _mlContext;
        private PredictionEngine<MLModel1.ModelInput, MLModel1.ModelOutput>? _engine;
        private string? _loadedModelPath;
        private BitmapImage? _currentBitmap;

        private const float ConfidenceThreshold = 0.5f;

        public MainWindow() => InitializeComponent();

        private void Retrain_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Filter = "ML.NET model|*.mlnet",
                Title = "Save retrained model as…",
                FileName = "MLModel2.mlnet"
            };
            if (saveDlg.ShowDialog() != true) return;

            SetStatus("Retraining… this may take several minutes.");

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    MLModel2.Train(saveDlg.FileName);

                    Dispatcher.Invoke(() =>
                    {
                        SetStatus("Retraining complete! Model saved to: " + saveDlg.FileName);
                        MessageBox.Show("Done! Load the new model with the Browse button.",
                            "Retrain Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus("Retraining failed: " + ex.Message);
                        MessageBox.Show(ex.Message, "Retrain Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

   
        private double _zoomLevel = 1.0;
        private const double ZoomStep = 0.25;
        private const double ZoomMin = 0.25;
        private const double ZoomMax = 5.0;

        private void SetZoom(double zoom)
        {
            _zoomLevel = Math.Clamp(zoom, ZoomMin, ZoomMax);
            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;
            ZoomLabel.Text = $"{_zoomLevel * 100:0}%";
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel + ZoomStep);
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel - ZoomStep);
        private void ZoomFit_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

        private void ImageScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
           
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            e.Handled = true;
            SetZoom(_zoomLevel + (e.Delta > 0 ? ZoomStep : -ZoomStep));
        }

        private void LoadModel(string modelPath)
        {
            try
            {
                SetStatus("Loading model…");
                _mlContext = new MLContext();
                ITransformer model = _mlContext.Model.Load(modelPath, out _);
                _engine = _mlContext.Model
                    .CreatePredictionEngine<MLModel1.ModelInput, MLModel1.ModelOutput>(model);
                _loadedModelPath = modelPath;
                SetStatus($"Model loaded: {Path.GetFileName(modelPath)}");
            }
            catch (Exception ex)
            {
                _engine = null;
                var msg = UnwrapException(ex);
                SetStatus("Failed to load model: " + msg);
                MessageBox.Show(msg, "Model Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string UnwrapException(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            var cur = ex;
            int depth = 0;
            while (cur != null && depth < 6)
            {
                if (depth > 0) sb.AppendLine("\n── Caused by ──");
                sb.AppendLine(cur.GetType().Name + ": " + cur.Message);
                cur = cur.InnerException;
                depth++;
            }
            return sb.ToString();
        }

        
        private List<DetectionResult> RunModel(string imagePath)
        {
            if (_engine == null)
                throw new InvalidOperationException("No model loaded.");

            using var mlImage = MLImage.CreateFromFile(imagePath);
            var input = new MLModel1.ModelInput { Image = mlImage };
            var output = _engine.Predict(input);
            return ParseOutputs(output);
        }

        private static List<DetectionResult> ParseOutputs(MLModel1.ModelOutput output)
        {
            var results = new List<DetectionResult>();

            if (output.PredictedBoundingBoxes == null ||
                output.PredictedLabel == null ||
                output.Score == null)
                return results;

            int count = output.PredictedLabel.Length;
            for (int i = 0; i < count; i++)
            {
                if (output.Score[i] < ConfidenceThreshold) continue;

                int b = i * 4;
                if (b + 3 >= output.PredictedBoundingBoxes.Length) break;

                float x1 = output.PredictedBoundingBoxes[b];
                float y1 = output.PredictedBoundingBoxes[b + 1];
                float x2 = output.PredictedBoundingBoxes[b + 2];
                float y2 = output.PredictedBoundingBoxes[b + 3];

                results.Add(new DetectionResult
                {
                    Label = output.PredictedLabel[i],
                    Confidence = output.Score[i],
                    X = x1,
                    Y = y1,
                    Width = x2 - x1,
                    Height = y2 - y1,
                });
            }
            return results;
        }

       
        private void BrowseModel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ML.NET model|*.mlnet;*.zip|All files|*.*",
                Title = "Select your MLModel1.mlnet file"
            };
            if (dlg.ShowDialog() == true)
            {
                ModelPathTextBox.Text = dlg.FileName;
                LoadModel(dlg.FileName);
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|All files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                PathTextBox.Text = dlg.FileName;
                LoadImage(dlg.FileName);
            }
        }

        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Detect_Click(sender, e);
        }

        private void Detect_Click(object sender, RoutedEventArgs e)
        {
            var modelPath = ModelPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(modelPath))
            { SetStatus("Please provide a model path."); return; }

            if (modelPath != _loadedModelPath) LoadModel(modelPath);
            if (_engine == null) return;

            var imagePath = PathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(imagePath))
            { SetStatus("Please provide an image path."); return; }
            if (!File.Exists(imagePath))
            { SetStatus("Image not found: " + imagePath); return; }

            LoadImage(imagePath);
            RunDetection(imagePath);
        }

       
        private void LoadImage(string path)
        {
            try
            {
                _currentBitmap = new BitmapImage();
                _currentBitmap.BeginInit();
                _currentBitmap.UriSource = new Uri(path, UriKind.Absolute);
                _currentBitmap.CacheOption = BitmapCacheOption.OnLoad;
                _currentBitmap.EndInit();

                MainImage.Source = _currentBitmap;
                BoundingBoxCanvas.Children.Clear();
                PlaceholderText.Visibility = Visibility.Collapsed;
                ImageScroll.Visibility = Visibility.Visible;
                ZoomControls.Visibility = Visibility.Visible; 
                SetZoom(1.0);                                  
                SetStatus($"Loaded: {Path.GetFileName(path)} " +
                          $"({_currentBitmap.PixelWidth}×{_currentBitmap.PixelHeight})");
                DetectionCount.Text = string.Empty;
            }
            catch (Exception ex) { SetStatus("Could not load image: " + ex.Message); }
        }

        private void RunDetection(string path)
        {
            if (_currentBitmap == null) return;
            SetStatus("Running detection…");
            BoundingBoxCanvas.Children.Clear();
            try
            {
                var results = RunModel(path);

              
                float srcW = _currentBitmap.PixelWidth;
                float srcH = _currentBitmap.PixelHeight;
                MLModel2.CalculateAspectAndOffset(srcW, srcH,
                    MLModel2.TrainingImageWidth, MLModel2.TrainingImageHeight,
                    out float xOff, out float yOff, out float aspect);

                foreach (var det in results)
                {
                    det.X = (det.X - xOff) / aspect;
                    det.Y = (det.Y - yOff) / aspect;
                    det.Width = det.Width / aspect;
                    det.Height = det.Height / aspect;
                }

                results = ApplyNMS(results, iouThreshold: 0.4f);

                ImageScroll.UpdateLayout();
                MainImage.UpdateLayout();

                foreach (var det in results)
                    DrawBox(det);

                SetStatus($"Done — {results.Count} object(s) detected.");
                DetectionCount.Text = $"{results.Count} detection(s)";
            }
            catch (Exception ex) { SetStatus("Inference error: " + ex.Message); }


        }

        
        private static List<DetectionResult> ApplyNMS(
            List<DetectionResult> detections, float iouThreshold = 0.4f)
        {
            var sorted = detections.OrderByDescending(d => d.Confidence).ToList();
            var kept = new List<DetectionResult>();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                kept.Add(best);
                sorted.RemoveAt(0);
                sorted.RemoveAll(d => IoU(best, d) > iouThreshold);
            }
            return kept;
        }

        private static float IoU(DetectionResult a, DetectionResult b)
        {
            float ax2 = a.X + a.Width, ay2 = a.Y + a.Height;
            float bx2 = b.X + b.Width, by2 = b.Y + b.Height;

            float interX1 = Math.Max(a.X, b.X);
            float interY1 = Math.Max(a.Y, b.Y);
            float interX2 = Math.Min(ax2, bx2);
            float interY2 = Math.Min(ay2, by2);

            float interW = Math.Max(0, interX2 - interX1);
            float interH = Math.Max(0, interY2 - interY1);
            float intersection = interW * interH;

            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;
            float union = areaA + areaB - intersection;

            return union <= 0 ? 0 : intersection / union;
        }

       
        private void DrawBox(DetectionResult det)
        {
            double scaleX = MainImage.ActualWidth / _currentBitmap!.PixelWidth;
            double scaleY = MainImage.ActualHeight / _currentBitmap!.PixelHeight;

            double x = det.X * scaleX;
            double y = det.Y * scaleY;
            double w = det.Width * scaleX;
            double h = det.Height * scaleY;

            var color = LabelColor(det.Label);

            var rect = new Rectangle
            {
                Width = Math.Max(1, w),
                Height = Math.Max(1, h),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B))
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            BoundingBoxCanvas.Children.Add(rect);

            var pill = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock
                {
                    Text = $"{det.Label}  {det.Confidence:P0}",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(5, 2, 5, 2)
                }
            };
            Canvas.SetLeft(pill, x);
            Canvas.SetTop(pill, Math.Max(0, y - 22));
            BoundingBoxCanvas.Children.Add(pill);
        }

        private static readonly Color[] Palette =
        {
            Color.FromRgb(239, 68,  68),
            Color.FromRgb(59,  130, 246),
            Color.FromRgb(34,  197, 94),
            Color.FromRgb(234, 179, 8),
            Color.FromRgb(168, 85,  247),
            Color.FromRgb(249, 115, 22),
            Color.FromRgb(20,  184, 166),
            Color.FromRgb(236, 72,  153),
            Color.FromRgb(14,  165, 233),
        };

        private readonly Dictionary<string, Color> _labelColorMap = new();
        private Color LabelColor(string label)
        {
            if (!_labelColorMap.TryGetValue(label, out var c))
            {
                c = Palette[_labelColorMap.Count % Palette.Length];
                _labelColorMap[label] = c;
            }
            return c;
        }

        private void SetStatus(string msg) => StatusText.Text = msg;

    } 

  
    public class DetectionResult
    {
        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

} 