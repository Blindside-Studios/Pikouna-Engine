using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThunderboltView : Page
    {
        List<LightningBoltPiece> _lightningBolt = new();

        public ThunderboltView()
        {
            this.InitializeComponent();
            this.Loaded += ThunderboltView_Loaded;
        }

        private void ThunderboltView_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateLightningBolt();
        }

        private void GenerateLightningBolt()
        {
            _lightningBolt.Clear();
            
            Random rnd = new Random();
            var width = LightningBoltCanvas.ActualWidth;
            var lastPiece = new LightningBoltPiece() { EndPoint = new Vector2((float)Math.Clamp(rnd.NextDouble() * width, width / 4, width * 0.75), -10) };
            while (lastPiece.EndPoint.Y < LightningBoltCanvas.ActualHeight)
            {
                var bolt = LightningBoltPiece.GenerateNewMainPiece(lastPiece.EndPoint, LightningBoltCanvas.ActualWidth);
                lastPiece = bolt;
                _lightningBolt.Add(bolt);
                _lightningBolt.AddRange(bolt.AddDetails(1, bolt.EndPoint));
            }
            LightningBoltCanvas.Invalidate();
        }

        private void LightningBoltCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            var strokeStyle = new CanvasStrokeStyle()
            {
                StartCap = CanvasCapStyle.Round,
                EndCap = CanvasCapStyle.Round
            };
            foreach (var piece in _lightningBolt)
            {
                ds.DrawLine(piece.StartPoint, piece.EndPoint, Microsoft.UI.Colors.LightYellow, 10 / (piece.StrayFromMainDepth + 1), strokeStyle);
            }
        }
    }

    class LightningBoltPiece
    {
        public Vector2 StartPoint { get; set; }
        public Vector2 EndPoint { get; set; }
        public bool IsInMainBolt { get; set; }
        public int StrayFromMainDepth { get; set; }

        public static LightningBoltPiece GenerateNewMainPiece(Vector2 StartingPoint, double ScreenWidth)
        {
            Random rnd = new Random();
            Vector2 path = new Vector2((float)((rnd.NextDouble() - 0.5) * (ScreenWidth / 10)), (float)(Math.Clamp(rnd.NextDouble() * 100 , 40, 100)));
            LightningBoltPiece piece = new LightningBoltPiece() {
                StartPoint = StartingPoint,
                EndPoint = StartingPoint + path,
                IsInMainBolt = true,
                StrayFromMainDepth = 0
            };
            return piece;
        }

        public List<LightningBoltPiece> AddDetails(int depth, Vector2 startingPoint)
        {
            List<LightningBoltPiece> list = new();

            int maxDepth = 3;
            int maxComplexity = 1;

            if (depth <= maxDepth)
            {
                for (int i = 0; i < maxComplexity; i++)
                {
                    Random rnd = new Random();
                    Vector2 path = new Vector2((float)((rnd.NextDouble() - 0.5) * 150), (float)(Math.Clamp((rnd.NextDouble() - 0.5) * 200, -100, 100)));
                    LightningBoltPiece piece = new LightningBoltPiece()
                    {
                        StartPoint = startingPoint,
                        EndPoint = startingPoint + path,
                        IsInMainBolt = false,
                        StrayFromMainDepth = depth
                    };
                    list.AddRange(piece.AddDetails(depth + 1, piece.EndPoint));
                    list.Add(piece);
                }
            }
            return list;
        }
    }
}
