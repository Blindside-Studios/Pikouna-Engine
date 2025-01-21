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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OzoraSunView : Page
    {
        private int _SunDimensions = 100;
        private int _SunRadius = 50;

        private double _nightTimeModifier = 0;


        private OzoraEngine Ozora = new OzoraEngine();
        private Vector2 _sunPosition = new Vector2(0, 0); // Initial position
        private double _workingWidth = 0;
        private double _workingHeight = 0;

        public OzoraSunView()
        {
            this.InitializeComponent();
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
                _nightTimeModifier = _newNightTimeModifier;
                SkyCanvas.Invalidate();
            }

            // Trigger a redraw to reflect the updated view
            SunCanvas.Invalidate();
        }

        private void MouseViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Ozora != null)
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

            OzoraSettings SunSettings = new OzoraSettings()
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

            Ozora.Physics.StartSimulation();
            Ozora.Physics.MouseCursorEngaged = true;
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
            // Sun object
            args.DrawingSession.FillCircle(new Vector2(_sunPosition.X + _SunRadius, _sunPosition.Y + _SunRadius), _SunRadius, Colors.Yellow);
        }

        private void SkyCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            double modifier = Math.Max(0, Math.Min(1, _nightTimeModifier));

            // Interpolate the correct checkpoint colors
            var topColor = PikounaColors.GetInterpolatedColor(modifier, PikounaColors.TopCPs);
            var bottomColor = PikounaColors.GetInterpolatedColor(modifier, PikounaColors.BottomCPs);

            // Build gradient stops (may add more in the future)
            float bottomWeight = 0.6f;
            var gradientStops = new CanvasGradientStop[]
            {
                new CanvasGradientStop { Position = 0f, Color = topColor },
                new CanvasGradientStop { Position = bottomWeight, Color = bottomColor }
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

                // 4. Fill the entire control
                var bounds = new Rect(0, 0, _workingWidth, _workingHeight);
                args.DrawingSession.FillRectangle(bounds, gradientBrush);
            }
        }
    }
}
