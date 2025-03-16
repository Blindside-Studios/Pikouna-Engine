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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Ozora;
using Pikouna_Engine;
using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
using Microsoft.UI.Dispatching;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OzoraSunView : Page
    {
        private UISettings uiSettings = new();

        private int _SunDimensions = 100;
        private int _SunRadius = 50;
        private int _starFramerate = 12;

        private double _nightTimeModifier = 0;
        private double _starOpacityModifier = 0;

        private OzoraEngine Ozora = new OzoraEngine();
        private Vector2 _sunPosition = new Vector2(0, 0); // Initial position
        private double _workingWidth = 0;
        private double _workingHeight = 0;
        
        private List<Star> BackgroundStars = new List<Star>();
        private List<Star> AnimatedStars = new List<Star>();
        DispatcherTimer twinklingTimer = new DispatcherTimer();

        DispatcherQueue dispatcherQueue;

        public OzoraSunView()
        {
            this.InitializeComponent();
            dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.SunGrid.Loaded += PhysicsSunSimulation_Loaded;
            this.Unloaded += PhysicsSunSimulation_Unloaded;
        }

        private void Physics_ObjectPositionCalculated(object sender, ObjectPositionUpdatedEvent e)
        {
            _sunPosition = new Vector2(e.NewTranslationVector.X, e.NewTranslationVector.Y);

            double _newNightTimeModifier = Calculations.GetNightModifier(e.NewTranslationVector.Y, _workingHeight);
            if (_newNightTimeModifier != _nightTimeModifier)
            {
                // start and handle star-related things
                if (_newNightTimeModifier >= 0.5)
                {
                    // use exponential function to make the transition smoother
                    var _newStarOpacityModifier = 0.1 * Math.Pow(11, (_nightTimeModifier - 0.5) * 4) - 0.1;
                    if (_starOpacityModifier != _newStarOpacityModifier)
                    {
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            StarCanvas.Opacity = _newStarOpacityModifier;
                            BackgroundStarCanvas.Opacity = _newStarOpacityModifier * 0.5;
                            _starOpacityModifier = _newStarOpacityModifier;
                            if (_starOpacityModifier > 0 && !twinklingTimer.IsEnabled) twinklingTimer.Start();
                        });
                    }
                }
                else
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        if (twinklingTimer.IsEnabled) twinklingTimer.Stop();
                        _starOpacityModifier = 0;
                        StarCanvas.Opacity = 0;
                        BackgroundStarCanvas.Opacity = 0;
                    });
                }

                _nightTimeModifier = _newNightTimeModifier;
                OzoraViewModel.Instance.NightTimeModifier = _nightTimeModifier;
                SkyCanvas.Invalidate();
            }

            // Trigger a redraw to reflect the updated view
            SunCanvas.Invalidate();
        }

        private void MouseViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Ozora != null && uiSettings.AnimationsEnabled)
            {
                Ozora.Physics.Interface.PointerLocation = OzoraViewModel.Instance.MousePosition;
                Ozora.Physics.MouseCursorEngaged = OzoraViewModel.Instance.MouseEngaged;
            }
        }

        private void PhysicsSunSimulation_Unloaded(object sender, RoutedEventArgs e)
        {
            Ozora.Physics.MouseCursorEngaged = false;
            Ozora.Physics.InterruptSimulation();
            Ozora = null;
            //Debug.WriteLine("Unloaded Ozora Sun Simulation Model");
        }

        private void PhysicsSunSimulation_Loaded(object sender, RoutedEventArgs e)
        {
            Ozora = new OzoraEngine();

            OzoraSettings SlowSunSettings = new OzoraSettings()
            {
                SimulationStyle = SimulationStyle.Sun,
                FrameRate = 60,
                MaxVectorDeltaPerFrame = 0.1f,
                RubberBandingModifier = 0.01f,
                EnableBorderCollision = true,
                EnableBounceOnCollision = true,
                BounceMomentumRetention = 0.5f,
                TrailingDragCoefficient = 0.03f,
                TrailingType = TrailingType.Vector
            };
            OzoraSettings BouncySunSettings = new OzoraSettings()
            {
                SimulationStyle = SimulationStyle.Sun,
                FrameRate = 60,
                MaxVectorDeltaPerFrame = 1.5f,
                RubberBandingModifier = 0.2f,
                EnableBorderCollision = true,
                EnableBounceOnCollision = true,
                BounceMomentumRetention = 0.8f,
                TrailingDragCoefficient = 0.01f,
                TrailingType = TrailingType.Vector
            };

            OzoraSettings SunSettings = ApplicationViewModel.Instance.SunInteractionStyle == SunInteractionStyle.Gentle ? SlowSunSettings : BouncySunSettings;

            Ozora.Physics.Interface = new OzoraInterface()
            {
                ObjectWidth = _SunDimensions,
                ObjectHeight = _SunDimensions,
                Settings = SunSettings,
                AreaDimensions = new Windows.Foundation.Point(SunGrid.ActualWidth, SunGrid.ActualHeight)
            };

            Ozora.Physics.ObjectPositionCalculated += Physics_ObjectPositionCalculated;
            OzoraViewModel.Instance.PropertyChanged += MouseViewModel_PropertyChanged;

            _workingWidth = SunGrid.ActualWidth;
            _workingHeight = SunGrid.ActualHeight;
            BackgroundStars = Calculations.GenerateStarPositions(Convert.ToInt32(_workingHeight * _workingWidth / 2000 * 0.5), false);
            AnimatedStars = Calculations.GenerateStarPositions(Convert.ToInt32(_workingHeight * _workingWidth / 2000 * 0.5), true);

            twinklingTimer.Interval = TimeSpan.FromMilliseconds(1000/_starFramerate); // Adjust interval as needed
            twinklingTimer.Tick += (s, e) => { if (uiSettings.AnimationsEnabled && ApplicationViewModel.Instance.AreAnimationsPlaying) StarCanvas.Invalidate(); };

            Ozora.Physics.StartSimulation();
            Ozora.Physics.MouseCursorEngaged = true;

            uiSettings.AnimationsEnabledChanged += UiSettings_AnimationsEnabledChanged;
        }

        private void UiSettings_AnimationsEnabledChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
        {
            SunCanvas.Invalidate();
        }

        private void SunGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            /// Null check as this event is fired when the page loads, 
            /// briefly before the code in the PageLoaded event is executed, 
            /// which initializes the Interface
            if (Ozora.Physics.Interface != null)
            {
                _workingWidth = SunGrid.ActualWidth;
                _workingHeight = SunGrid.ActualHeight;
                Ozora.Physics.Interface.AreaDimensions =
                new Windows.Foundation.Point(SunGrid.ActualWidth, SunGrid.ActualHeight);
            }
        }

        private void SunCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (uiSettings.AnimationsEnabled)
            {
                var sunColor = Windows.UI.Color.FromArgb(
                        255, // alpha channel
                        Colors.LightGoldenrodYellow.R,
                        (byte)(0.9 * Colors.LightGoldenrodYellow.G * (1 - _nightTimeModifier * 0.75)),
                        (byte)(0.75 * Colors.LightGoldenrodYellow.B * (1 - _nightTimeModifier))
                    );

                // Sun background half transparent objects
                sunColor.A = 255 / 7;
                args.DrawingSession.FillCircle(new Vector2(_sunPosition.X + _SunRadius, _sunPosition.Y + _SunRadius), (float)(_SunRadius + 86 * Math.Clamp(1 - Math.Pow(_nightTimeModifier * 1.3, 2), 0, 1)), sunColor);
                sunColor.A = 255 / 6;
                args.DrawingSession.FillCircle(new Vector2(_sunPosition.X + _SunRadius, _sunPosition.Y + _SunRadius), (float)(_SunRadius + 64 * Math.Clamp(1 - Math.Pow(_nightTimeModifier * 1.3, 2), 0, 1)), sunColor);
                sunColor.A = 255 / 5;
                args.DrawingSession.FillCircle(new Vector2(_sunPosition.X + _SunRadius, _sunPosition.Y + _SunRadius), (float)(_SunRadius + 34 * Math.Clamp(1 - Math.Pow(_nightTimeModifier * 1.3, 2), 0, 1)), sunColor);
                sunColor.A = 255;

                // Sun object
                args.DrawingSession.FillCircle(new Vector2(_sunPosition.X + _SunRadius, _sunPosition.Y + _SunRadius), _SunRadius, sunColor);
            }
        }

        private void SkyCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            double modifier = Math.Max(0, Math.Min(1, _nightTimeModifier));

            // Interpolate the correct checkpoint colors
            var topColor = PikounaColors.GetInterpolatedColor(modifier, PikounaColors.TopCPs);
            var bottomColor = PikounaColors.GetInterpolatedColor(modifier, PikounaColors.BottomCPs);

            // Build gradient stops (may add more in the future)
            // Easing function (quadratic)
            float EaseOutQuad(float t) => t * (2 - t);

            // Adjust stop positions using easing
            var gradientStops = new CanvasGradientStop[]
            {
                new CanvasGradientStop { Position = EaseOutQuad(0f), Color = topColor },
                new CanvasGradientStop { Position = EaseOutQuad(1f), Color = bottomColor }
            };


            // Brush Creation
            using (var gradientBrush = new CanvasLinearGradientBrush(
                sender,
                gradientStops,
                CanvasEdgeBehavior.Clamp,
                CanvasAlphaMode.Premultiplied))
            {
                // By default, linear gradient goes from left to right. 
                // We want top to bottom, so set StartPoint & EndPoint
                gradientBrush.StartPoint = new Vector2(0, 0);
                gradientBrush.EndPoint = new Vector2(0, (float)sender.ActualHeight);

                // Fill the entire control
                var bounds = new Rect(0, 0, _workingWidth, _workingHeight);
                args.DrawingSession.FillRectangle(bounds, gradientBrush);
            }
        }

        private void StarCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            foreach (Star star in AnimatedStars)
            {
                // Let the star generate a new opacity to animate
                star.CreateNewOpacity();

                // Define star color with dynamic alpha
                var starColor = Windows.UI.Color.FromArgb(
                    (byte)(255 * star.Opacity), // alpha channel
                    Colors.LightGoldenrodYellow.R,
                    Colors.LightGoldenrodYellow.G,
                    Colors.LightGoldenrodYellow.B
                );

                args.DrawingSession.FillCircle(new Vector2((float)_workingWidth * star.Position.X, (float)_workingHeight * star.Position.Y), star.Radius, starColor);
            }
        }

        private void BackgroundStars_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            foreach (Star star in BackgroundStars)
            {
                // Define star color with dynamic alpha
                var starColor = Windows.UI.Color.FromArgb(
                    (byte)(255 * star.Opacity), // alpha channel
                    Colors.LightGoldenrodYellow.R,
                    Colors.LightGoldenrodYellow.G,
                    Colors.LightGoldenrodYellow.B
                );

                args.DrawingSession.FillCircle(new Vector2((float)_workingWidth * star.Position.X, (float)_workingHeight * star.Position.Y), star.Radius, starColor);
            }
        }
    }
}
