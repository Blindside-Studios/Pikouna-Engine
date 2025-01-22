using Microsoft.Graphics.Canvas.Svg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        private void ChateauCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_svgDocument != null)
            {
                var canvasWidth = _windowWidth;
                var canvasHeight = _windowHeight;

                float svgWidth = 3840f;
                float svgHeight = 2160f;

                float scale = canvasWidth / svgWidth;
                if (svgHeight * scale > canvasHeight)
                {
                    scale = canvasHeight / svgHeight;
                }

                float xOffset = (canvasWidth - svgWidth * scale) / 2;
                float yOffset = canvasHeight - svgHeight * scale;

                // Draw the SVG
                args.DrawingSession.Transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(xOffset, yOffset);
                args.DrawingSession.DrawSvg(_svgDocument, new Windows.Foundation.Size(_windowWidth, _windowHeight));
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
            ChateauCanvas.Invalidate();
        }
    }
}
