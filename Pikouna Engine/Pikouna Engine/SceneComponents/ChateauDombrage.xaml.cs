using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.SceneComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChateauDombrage : Page
    {
        private CanvasSvgDocument _svgDocument;
        private float _windowWidth = 100f;
        private float _windowHeight = 100f;
        private const float MinSvgWidth = 1000f;

        public ChateauDombrage()
        {
            this.InitializeComponent();
            ChateauCanvas.Loaded += ChateauCanvas_Loaded;
        }

        private void ChateauCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            _windowWidth = (float)ChateauCanvas.ActualWidth;
            _windowHeight = (float)ChateauCanvas.ActualHeight;
            OzoraViewModel.Instance.NightTimeUpdate += Instance_NightTimeUpdate;
            // this doesnt load this yet for some reason
            ChateauCanvas.Invalidate();
        }

        private void Instance_NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ChateauCanvas.Invalidate();
        }

        private CanvasRenderTarget _offscreenTarget;

        private void ChateauCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_svgDocument != null)
            {
                var canvasWidth = _windowWidth;
                var canvasHeight = _windowHeight;

                float svgWidth = 3840f;
                float svgHeight = 2160f;

                float scale = Math.Max(canvasWidth / svgWidth, MinSvgWidth / svgWidth);
                if (svgHeight * scale > canvasHeight)
                {
                    scale = canvasHeight / svgHeight;
                }

                float xOffset = (canvasWidth - svgWidth * scale) / 2;
                float yOffset = canvasHeight - svgHeight * scale;

                // calculate dynamic multipliers for the red and green channels
                float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                // red remains unchanged
                float greenMultiplier = 1 - (float)Math.Sqrt(nightTimeModifier);
                float blueMultiplier = 1 - (float)(nightTimeModifier * 0.75);

                CanvasGradientStop[] gradientStops =
                {
                    new CanvasGradientStop() { Position = 0.0f, Color = Color.FromArgb(255, 0, (byte)(33 * greenMultiplier), (byte)(55 * blueMultiplier)) }, // top color
                    new CanvasGradientStop() { Position = 1.0f, Color = Color.FromArgb(255, 0, (byte)(17 * greenMultiplier), (byte)(28 * blueMultiplier)) }  // bottom color
                };

                using (var gradientBrush = new CanvasLinearGradientBrush(args.DrawingSession, gradientStops))
                {
                    gradientBrush.StartPoint = new Vector2(0, 0);
                    gradientBrush.EndPoint = new Vector2(0, canvasHeight);

                    float sideWidth = (canvasWidth - svgWidth * scale) / 2;

                    // artificially extend the recatngles to prevent seams from forming
                    args.DrawingSession.FillRectangle(-1, -1, sideWidth + 2, canvasHeight + 2, gradientBrush);
                    args.DrawingSession.FillRectangle(canvasWidth - sideWidth - 1, -1, sideWidth + 2, canvasHeight + 2, gradientBrush);
                }

                //args.DrawingSession.Transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(xOffset, yOffset);
                //args.DrawingSession.DrawSvg(_svgDocument, new Windows.Foundation.Size(_windowWidth, _windowHeight));

                if (_offscreenTarget == null || _offscreenTarget.Size.Width != _windowWidth || _offscreenTarget.Size.Height != _windowHeight)
                {
                    _offscreenTarget?.Dispose();
                    _offscreenTarget = new CanvasRenderTarget(sender, _windowWidth, _windowHeight);
                }

                using (var ds = _offscreenTarget.CreateDrawingSession())
                {
                    // Draw SVG and other elements normally
                    ds.Transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(xOffset, yOffset);
                    ds.DrawSvg(_svgDocument, new Windows.Foundation.Size(canvasWidth, canvasHeight));
                }

                // Apply a color matrix effect
                var colorMatrixEffect = new ColorMatrixEffect
                {
                    Source = _offscreenTarget,
                    ColorMatrix = new Matrix5x4
                    {
                        M11 = 1.0f, M12 = 0.0f, M13 = 0.0f, M14 = 0.0f, // Red multiplier
                        M21 = 0.0f, M22 = greenMultiplier, M23 = 0.0f, M24 = 0.0f, // Green multiplier
                        M31 = 0.0f, M32 = 0.0f, M33 = blueMultiplier, M34 = 0.0f, // Blue remains unchanged
                        M41 = 0.0f, M42 = 0.0f, M43 = 0.0f, M44 = 1.0f, // Alpha remains unchanged
                        M51 = 0.0f, M52 = 0.0f, M53 = 0.0f, M54 = 0.0f  // Offset values
                    }
                };

                // Draw the effect onto the main canvas
                args.DrawingSession.DrawImage(colorMatrixEffect);
            }
        }

        private void ChateauCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _windowWidth = (float)ChateauCanvas.ActualWidth; 
            _windowHeight = (float)ChateauCanvas.ActualHeight;
            ChateauCanvas.Invalidate();
        }

        private async void ChateauCanvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            var uri = new Uri("ms-appx:///SceneComponents/ChateauDombrage.svg");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);

            using (var stream = await file.OpenReadAsync())
            {
                _svgDocument = await CanvasSvgDocument.LoadAsync(sender.Device, stream);
            }

            // refresh the canvas and animate it in to ensure a smooth entrance
            ChateauCanvas.Invalidate();
            ChateauCanvas.Opacity = 1.0;
        }
    }
}
