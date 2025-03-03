using Microsoft.Graphics.Canvas.Geometry;
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
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HailView : Page
    {
        DispatcherTimer _hailAnimationTimer;
        UISettings uiSettings = new UISettings();

        List<HailCorn> HailCorns = new List<HailCorn>();
        double HailLevel = 0;
        Vector2 areaDimensions = new();

        public HailView()
        {
            this.InitializeComponent();
            this.Loaded += RainView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += RainChanged;
            OzoraViewModel.Instance.NightTimeUpdate += NightTimeUpdate;
            ApplicationViewModel.Instance.PropertyChanged += AppPreferencesChanged;
        }

        private void RainView_Loaded(object sender, RoutedEventArgs e)
        {
            HailLevel = WeatherViewModel.Instance.Hail;
            handleAnimations();
        }

        private void AppPreferencesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _hailAnimationTimer.Start();
            else _hailAnimationTimer.Stop();
        }

        private void NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            HailCanvas.Invalidate();
        }

        private void RainChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            HailLevel = WeatherViewModel.Instance.Hail;
            Random rnd = new Random();
            if (!uiSettings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying)
            {
                while (HailCorns.Count < WeatherViewModel.Instance.Hail * 100)
                {
                    HailCorns.Add(HailCorn.CreateNewHailCorn());
                }
                while (HailCorns.Count > WeatherViewModel.Instance.Hail * 100)
                {
                    HailCorns.RemoveAt(rnd.Next(0, HailCorns.Count() - 1));
                }
            }
            HailCanvas.Invalidate();
        }

        private void handleAnimations()
        {
            _hailAnimationTimer = new DispatcherTimer();
            _hailAnimationTimer.Interval = TimeSpan.FromMilliseconds(17); // Fires 60 times per second
            _hailAnimationTimer.Tick += AnimationTimer_Tick;
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _hailAnimationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            Random rnd = new Random();
            for (int i = 0; i < HailLevel * 4 * ApplicationViewModel.Instance.MotionModifier; i++) HailCorns.Add(HailCorn.CreateNewHailCorn());

            if (uiSettings.AnimationsEnabled)
            {
                var areaWidth = HailCanvas.ActualWidth;

                for (int i = 0; i < HailCorns.Count(); i++)
                {
                    var droplet = HailCorns[i];
                    droplet.AnimateCorn();
                    if (droplet.Translation.Y > 1 + droplet.Width / areaWidth)
                    {
                        HailCorns.Remove(droplet);
                    }
                }

                //collect clouds to render again and render
                HailCanvas.Invalidate();
            }
        }

        private void RainCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            if (HailCorns.Count() > 0)
            {
                // calculate colors ahead of running the for loop
                var color = Colors.White;
                float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                nightTimeModifier = (float)(Math.Clamp(nightTimeModifier - 0.33, 0, 1) * 1.25);

                float colorMultiplier = 1f - (nightTimeModifier * 0.9f);

                color.R = (byte)(color.R * colorMultiplier);
                color.G = (byte)(color.G * colorMultiplier);
                color.B = (byte)(color.B * colorMultiplier);

                foreach (var obj in HailCorns)
                {
                    var leadingTranslation = new System.Numerics.Vector2(obj.Translation.X * areaDimensions.X, obj.Translation.Y * areaDimensions.Y);


                    if (leadingTranslation.X > -obj.Width &&
                        leadingTranslation.Y > -obj.Width &&
                        leadingTranslation.X < areaDimensions.X + obj.Width &&
                        leadingTranslation.Y < areaDimensions.Y + obj.Width)
                    {

                        ds.FillCircle(leadingTranslation, obj.Width, color);
                    }
                }
            }
        }

        private void RainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            areaDimensions = new Vector2((float)HailCanvas.ActualWidth, (float)HailCanvas.ActualHeight);
            HailCanvas.Invalidate();
        }

        internal class HailCorn
        {
            public System.Numerics.Vector2 Translation { get; set; }
            public float Width { get; set; }
            public float Speed { get; set; }
            public float WindAffection { get; set; }
            internal float Proximity { get; set; }

            public static HailCorn CreateNewHailCorn()
            {
                Random rnd = new Random();
                Vector2 position = new Vector2((float)(rnd.NextDouble() * 2 - 1), (float)-0.25);
                UISettings settings = new UISettings();
                if (!settings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying) position = new Vector2(position.X, (float)rnd.NextDouble());
                float proximity = (float)(rnd.NextDouble() * 0.75 + 0.25);
                float speed = (float)(0.2 * proximity);
                float windSpeed = (float)WeatherViewModel.Instance.WindSpeed;
                float windAffection = (float)((rnd.NextDouble() * 0.5 + 1) / 3);

                var rainDrop = new HailCorn()
                {
                    Translation = position,
                    Width = 4 * proximity,
                    Speed = speed,
                    WindAffection = windAffection,
                    Proximity = proximity
                };
                return rainDrop;
            }

            public void AnimateCorn()
            {
                this.Translation += new System.Numerics.Vector2((float)(WindAffection * (WeatherViewModel.Instance.WindSpeed / 150) * this.Speed * ApplicationViewModel.Instance.MotionModifier), (float)(this.Speed * ApplicationViewModel.Instance.MotionModifier));
            }
        }
    }
}
