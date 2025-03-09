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
using System.Diagnostics;
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
        List<Vector2> _dotMatrix = new List<Vector2>();
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
            _dotMatrix.Clear();
            
            Random rnd = new Random(); 
            for (int i = 0; i < 10000; i++)
            {
                _dotMatrix.Add(new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble() - 0.1f));
            }

            var width = LightningBoltCanvas.ActualWidth;
            var lastPiece = new LightningBoltPiece() { EndPoint = new Vector2((float)Math.Clamp(rnd.NextDouble(), 0.25, 0.75), -0.1f) };
            
            while (lastPiece.EndPoint.Y < 0.8)
            {
                LightningBoltPiece bolt = new LightningBoltPiece()
                {
                    StartPoint = lastPiece.EndPoint,
                    EndPoint = findClosestPoint(lastPiece.EndPoint, true),
                    IsInMainBolt = true,
                    StrayFromMainDepth = 0
                };
                lastPiece = bolt;
                _lightningBolt.Add(bolt);
                if (rnd.NextDouble() < 0.05) _lightningBolt.AddRange(AddDetails(0, lastPiece.EndPoint)); // only add "subarms" in 10% of cases
            }
            LightningBoltCanvas.Invalidate();
        }

        private List<LightningBoltPiece> AddDetails(int parentDepth, Vector2 startingPoint)
        {
            List<LightningBoltPiece> list = new();
            int maxDepth = 20;
            
            Random rnd = new Random();
            LightningBoltPiece lastPiece = new LightningBoltPiece() { StrayFromMainDepth = parentDepth, EndPoint = startingPoint };

            while (lastPiece.StrayFromMainDepth < maxDepth)
            {
                LightningBoltPiece piece = new LightningBoltPiece()
                {
                    StartPoint = lastPiece.EndPoint,
                    EndPoint = findClosestPoint(lastPiece.EndPoint, false),
                    IsInMainBolt = false,
                    StrayFromMainDepth = lastPiece.StrayFromMainDepth + 1
                };

                if (piece.EndPoint.Y > 0 && piece.EndPoint.Y < 0.85)
                {
                    list.Add(piece);
                    lastPiece = piece;
                    if (rnd.Next() < 0.2) list.AddRange(AddDetails(piece.StrayFromMainDepth, piece.EndPoint));
                }
                else break;
            }
            return list;
        }

        private Vector2 findClosestPoint(Vector2 StartPoint, bool preferDownwards)
        {
            var filteredPoints = _dotMatrix.Where(p => p.Y > StartPoint.Y);

            IOrderedEnumerable<Vector2> closest;

            float verticalImportanceModifier = 2f;
            if (preferDownwards) verticalImportanceModifier = 5;

            closest = filteredPoints
            .OrderBy(p => (p.Y - StartPoint.Y) + verticalImportanceModifier * Math.Abs(p.X - StartPoint.X));

            if (preferDownwards) return closest.FirstOrDefault();
            else
            {
                Random rnd = new Random();
                return closest.ElementAt(rnd.Next(0, Convert.ToInt32(closest.Count() / 250)));
            }
        }

        private void LightningBoltCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            var strokeStyle = new CanvasStrokeStyle()
            {
                StartCap = CanvasCapStyle.Round,
                EndCap = CanvasCapStyle.Round
            };
            Vector2 screenDimensions = new Vector2((float)LightningBoltCanvas.ActualWidth, (float)LightningBoltCanvas.ActualHeight);
            foreach (var piece in _lightningBolt)
            {
                var thickness = 15;
                if (!piece.IsInMainBolt)
                {
                    thickness = 10 - piece.StrayFromMainDepth / 2;
                }

                ds.DrawLine(
                    new Vector2(piece.StartPoint.X * screenDimensions.X, piece.StartPoint.Y * screenDimensions.Y),
                    new Vector2(piece.EndPoint.X * screenDimensions.X, piece.EndPoint.Y * screenDimensions.Y),
                    Microsoft.UI.Colors.LightYellow, 
                    thickness, 
                    strokeStyle);
            }
            foreach (var dot in _dotMatrix)
            {
                ds.FillCircle(new Vector2(dot.X * screenDimensions.X, dot.Y * screenDimensions.Y), 1, Microsoft.UI.Colors.Red);
            }
        }
    }

    class LightningBoltPiece
    {
        public Vector2 StartPoint { get; set; }
        public Vector2 EndPoint { get; set; }
        public bool IsInMainBolt { get; set; }
        public int StrayFromMainDepth { get; set; }
    }
}
