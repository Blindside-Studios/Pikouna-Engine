using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
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
using System.Threading.Tasks;
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

        // animation variables
        private bool _isRenderingStrike = false;
        private bool _isRenderingStrikeDetails = true;
        private double _pathfindingProgress = 0;
        private float _strikeBloomModifier = 1;

        DispatcherTimer _thunderAnimationTimer;

        public ThunderboltView()
        {
            this.InitializeComponent();
            this.Loaded += ThunderboltView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WeatherViewModel.Instance.WeatherType))
            {
                Initiate();
            }
        }

        private void Initiate()
        {
            var weather = WeatherViewModel.Instance.WeatherType;
            var isCorrectWeather = weather == WeatherType.ThunderstormSlightOrModerate || weather == WeatherType.ThunderstormWithHailSlight || weather == WeatherType.ThunderstormWithHailHeavy;
            if (isCorrectWeather)
            {
                startAnimating();
            }
            else
            {
                _dotMatrix.Clear();
                _lightningBolt.Clear();
                LightningBoltCanvas.Invalidate();
            }
        }

        private void ThunderboltView_Loaded(object sender, RoutedEventArgs e)
        {
            var weather = WeatherViewModel.Instance.WeatherType;
            if (weather == WeatherType.ThunderstormSlightOrModerate || weather == WeatherType.ThunderstormWithHailSlight || weather == WeatherType.ThunderstormWithHailHeavy)
            {
                startAnimating();
            }
        }

        private void startAnimating()
        {
            _isRenderingStrike = false;
            _isRenderingStrikeDetails = true;
            _pathfindingProgress = 0;
            _strikeBloomModifier = 1;
            handleAnimations();
            GenerateLightningBolt();
            playAnimation();
        }

        private void handleAnimations()
        {
            if (_thunderAnimationTimer != null) _thunderAnimationTimer.Stop();
            _thunderAnimationTimer = new DispatcherTimer();
            _thunderAnimationTimer.Interval = TimeSpan.FromMilliseconds(10000); // Fires every 20 seconds
            _thunderAnimationTimer.Tick += _thunderAnimationTimer_Tick;
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _thunderAnimationTimer.Start();
        }

        private void _thunderAnimationTimer_Tick(object sender, object e)
        {
            GenerateLightningBolt();
            playAnimation();
        }

        private async void playAnimation()
        {
            UISettings settings = new UISettings();
            if (settings.AnimationsEnabled && ApplicationViewModel.Instance.CanPlayAnimations)
            {
                // start values
                _isRenderingStrike = false;
                _isRenderingStrikeDetails = true;
                _pathfindingProgress = 0;
                _strikeBloomModifier = 1;

                // pathfinding animation to the floor
                _isRenderingStrike = true;
                while (_pathfindingProgress < 0.9)
                {
                    _pathfindingProgress += 0.075;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(2);
                }

                while (_strikeBloomModifier < 10)
                {
                    _strikeBloomModifier += 2;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(1);
                }
                _isRenderingStrikeDetails = false;

                while (_strikeBloomModifier > 5)
                {
                    _strikeBloomModifier -= 0.1f;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(2);
                }
                while (_strikeBloomModifier < 7)
                {
                    _strikeBloomModifier += 0.2f;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(2);
                }
                while (_strikeBloomModifier > 1)
                {
                    _strikeBloomModifier -= 0.1f;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(2);
                }
                while (_strikeBloomModifier > 0)
                {
                    _strikeBloomModifier -= 0.01f;
                    LightningBoltCanvas.Invalidate();
                    await Task.Delay(3);
                }
            }
        }

        private void GenerateLightningBolt()
        {
            UISettings settings = new UISettings();
            if (settings.AnimationsEnabled && ApplicationViewModel.Instance.CanPlayAnimations)
            {
                _dotMatrix.Clear();
                _lightningBolt.Clear();

                Random rnd = new Random();
                for (int i = 0; i < 4000; i++)
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
                    if (rnd.NextDouble() < (1 - lastPiece.EndPoint.Y) / 5) _lightningBolt.AddRange(AddDetails(0, lastPiece.EndPoint)); // only add "subarms" in 10% of cases
                }
                LightningBoltCanvas.Invalidate();
            }
        }

        private List<LightningBoltPiece> AddDetails(int parentDepth, Vector2 startingPoint)
        {
            List<LightningBoltPiece> list = new();
            int maxDepth = 30;
            
            Random rnd = new Random();
            var preferredDir = PreferredDirection.Left;
            if (rnd.NextDouble() < 0.5) preferredDir = PreferredDirection.Right;
            LightningBoltPiece lastPiece = new LightningBoltPiece() { StrayFromMainDepth = parentDepth, EndPoint = startingPoint };

            while (lastPiece.StrayFromMainDepth < maxDepth)
            {
                LightningBoltPiece piece = new LightningBoltPiece()
                {
                    StartPoint = lastPiece.EndPoint,
                    EndPoint = findClosestPoint(lastPiece.EndPoint, false, lastPiece.StepsSinceLastJunction + 1, preferredDir),
                    IsInMainBolt = false,
                    StrayFromMainDepth = lastPiece.StrayFromMainDepth + 1,
                    StepsSinceLastJunction = lastPiece.StepsSinceLastJunction + 1,
                };

                if (piece.EndPoint.Y > 0 && piece.EndPoint.Y < 0.85)
                {
                    list.Add(piece);
                    lastPiece = piece;
                    if (rnd.NextDouble() < 0.05)
                    {
                        list.AddRange(AddDetails(piece.StrayFromMainDepth, piece.EndPoint));
                    }
                }
                else break;
            }
            return list;
        }

        private Vector2 findClosestPoint(Vector2 StartPoint, bool preferDownwards, int stepsSinceLastJunction = 0, PreferredDirection preferredDirection = PreferredDirection.Unspecified)
        {
            var filteredPoints = _dotMatrix.Where(p => p.Y > StartPoint.Y);
            if (preferredDirection == PreferredDirection.Left) filteredPoints = filteredPoints.Where(p => p.X < StartPoint.X);
            else if (preferredDirection == PreferredDirection.Right) filteredPoints = filteredPoints.Where(p => p.X > StartPoint.X);

            IOrderedEnumerable<Vector2> closest;

            float verticalImportanceModifier = 5;
            if (!preferDownwards)
            {
                verticalImportanceModifier = (float)(0.75 + (stepsSinceLastJunction) / 5);
            }

            closest = filteredPoints
            .OrderBy(p => (p.Y - StartPoint.Y) + verticalImportanceModifier * Math.Abs(p.X - StartPoint.X));

            if (preferDownwards) return closest.FirstOrDefault();
            else
            {
                if (closest.Count() > 0)
                {
                    Random rnd = new Random();
                    return closest.ElementAt(rnd.Next(0, Convert.ToInt32(closest.Count() / 500)));
                }
                else return new Vector2(-1, -1);
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
            if (_isRenderingStrike)
            {
                foreach (var piece in _lightningBolt)
                {
                    var renderBolt = true;
                    float thickness = 12;
                    if (!piece.IsInMainBolt && piece.EndPoint.Y < _pathfindingProgress)
                    {
                        thickness = 5 - piece.StrayFromMainDepth / 6;
                    }
                    else if (piece.EndPoint.Y > _pathfindingProgress)
                    {
                        renderBolt = false;
                    }

                    if (piece.IsInMainBolt) thickness = thickness * _strikeBloomModifier;
                    else if (!_isRenderingStrikeDetails) renderBolt = false;

                    if (renderBolt) {
                        ds.DrawLine(
                            new Vector2(piece.StartPoint.X * screenDimensions.X, piece.StartPoint.Y * screenDimensions.Y),
                            new Vector2(piece.EndPoint.X * screenDimensions.X, piece.EndPoint.Y * screenDimensions.Y),
                            Microsoft.UI.Colors.LightYellow,
                            thickness,
                            strokeStyle);
                    }
                }

                // render bloom as required by rendering into an off-screen area
                if (_strikeBloomModifier > 1)
                {
                    CanvasRenderTarget lightningTarget = new CanvasRenderTarget(sender, (float)LightningBoltCanvas.ActualWidth, (float)LightningBoltCanvas.ActualHeight, 96);
                    using (var _ds = lightningTarget.CreateDrawingSession())
                    {
                        _ds.Clear(Colors.Transparent);
                        foreach (var piece in _lightningBolt)
                        {
                            if (piece.IsInMainBolt)
                            {
                                float thickness = 12;

                                _ds.DrawLine(
                                        new Vector2(piece.StartPoint.X * screenDimensions.X, piece.StartPoint.Y * screenDimensions.Y),
                                        new Vector2(piece.EndPoint.X * screenDimensions.X, piece.EndPoint.Y * screenDimensions.Y),
                                        Microsoft.UI.Colors.LightYellow,
                                        thickness * (_strikeBloomModifier - 1) * 10,
                                        strokeStyle);
                            }
                        }
                    }
                    var blurredLightning = new GaussianBlurEffect
                    {
                        Source = lightningTarget,
                        BlurAmount = _strikeBloomModifier * 15,
                        BorderMode = EffectBorderMode.Hard
                    };
                    ds.DrawImage(blurredLightning);
                }
            }
        }
    }

    class LightningBoltPiece
    {
        public Vector2 StartPoint { get; set; }
        public Vector2 EndPoint { get; set; }
        public bool IsInMainBolt { get; set; }
        public int StrayFromMainDepth { get; set; }
        public int StepsSinceLastJunction { get; set; } = 0;
        public PreferredDirection PreferredDirection { get; set; } = PreferredDirection.Unspecified;
    }

    enum PreferredDirection
    {
        Unspecified,
        Left,
        Right
    }
}
