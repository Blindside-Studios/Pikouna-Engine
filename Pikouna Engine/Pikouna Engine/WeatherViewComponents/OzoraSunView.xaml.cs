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
using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OzoraSunView : Page
    {
        private OzoraEngine Ozora = new OzoraEngine();
        private Vector2 _sunPosition = new Vector2(0, 0); // Initial position

        public OzoraSunView()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            this.SunGrid.Loaded += PhysicsSunSimulation_Loaded;
            this.Unloaded += PhysicsSunSimulation_Unloaded;
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
                ObjectWidth = (float)SunObject.ActualWidth,
                ObjectHeight = (float)SunObject.ActualHeight,
                Settings = SunSettings,
                AreaDimensions = new Windows.Foundation.Point(SunGrid.ActualWidth, SunGrid.ActualHeight)
            };

            Ozora.Physics.ObjectPositionCalculated += Physics_ObjectPositionCalculated;
            OzoraViewModel.Instance.PropertyChanged += MouseViewModel_PropertyChanged;

            Ozora.Physics.StartSimulation();
            Ozora.Physics.MouseCursorEngaged = true;
        }

        public void MoveSun(Vector2 newPosition)
        {
            _sunPosition = newPosition;

            // Trigger a redraw to reflect the updated position
            SunCanvas.Invalidate();
        }

        private void SunCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Draw the sun at the current position
            args.DrawingSession.FillCircle(_sunPosition, 25, Colors.Yellow); // Radius = 25
        }

        private void MouseViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Ozora != null)
            {
                Ozora.Physics.Interface.PointerLocation = OzoraViewModel.Instance.MousePosition;
                Ozora.Physics.MouseCursorEngaged = OzoraViewModel.Instance.MouseEngaged;
            }
        }

        private void Physics_ObjectPositionCalculated(object sender, ObjectPositionUpdatedEvent e)
        {
            MoveSun(new Vector2(e.NewTranslationVector.X, e.NewTranslationVector.Y));
        }

        private void SunGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            /// Null check as this event is fired when the page loads, 
            /// briefly before the code in the PageLoaded event is executed, 
            /// which initializes the Interface
            if (Ozora.Physics.Interface != null)
            {
                Ozora.Physics.Interface.AreaDimensions =
                new Windows.Foundation.Point(SunGrid.ActualWidth, SunGrid.ActualHeight);
            }
        }
    }
}
