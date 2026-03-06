# ObjectDetectionApp

A Windows desktop application built with **WPF (.NET)** and **ML.NET** that performs real-time object detection using two trained machine learning models.

## Features

-  Object detection powered by two ML.NET models (`MLModel1` and `MLModel2`)
-  Clean WPF desktop UI (`MainWindow.xaml`)
-  Fast inference using pre-trained `.mlnet` model files
-  Modular model structure — easily swap or extend models

## Tech Stack

| Technology | Purpose |
|---|---|
| C# / WPF | Desktop UI framework |
| ML.NET | Machine learning inference |
| Visual Studio ML Model Builder | Model training & configuration |
| .NET | Runtime |

## Project Structure

```
ObjectDetectionApp/
├── App.xaml / App.xaml.cs          # Application entry point
├── MainWindow.xaml / .xaml.cs      # Main UI window
├── MLModel1.mlnet                  # Trained model 1 weights
├── MLModel1.mbconfig               # Model 1 configuration
├── MLModel1.consumption.cs         # Model 1 inference API
├── MLModel1.helpers.cs             # Model 1 helper utilities
├── MLModel1.training.cs            # Model 1 training pipeline
├── MLModel2.mlnet                  # Trained model 2 weights
├── MLModel2.mbconfig               # Model 2 configuration
├── MLModel2.consumption.cs         # Model 2 inference API
├── MLModel2.training.cs            # Model 2 training pipeline
└── ObjectDetectionApp.csproj       # Project file
```

## Getting Started

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with .NET desktop development workload)
- .NET 6.0 or later

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/UnknownUser746/ObjectDetectionApp.git
   ```

2. Open `ObjectDetectionApp.sln` in Visual Studio

3. Restore NuGet packages (Visual Studio does this automatically on build)

4. Press **F5** to build and run

## Usage

1. Launch the application
2. Load or provide an image for detection
3. The app runs inference using the trained ML models and displays the detected objects

## Models

The app includes two ML.NET object detection models trained using Visual Studio's ML Model Builder:

- **MLModel1** — Primary detection model
- **MLModel2** — Secondary/supplementary detection model

Each model includes its training pipeline (`.training.cs`) and consumption API (`.consumption.cs`), making it straightforward to retrain or fine-tune with new data.


## License

This project is open source. See [LICENSE](LICENSE) for details.
