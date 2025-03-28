using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SnowView : Page
    {
        DispatcherTimer _snowAnimationTimer;
        UISettings uiSettings = new UISettings();

        List<SnowFlake> SnowFlakes = new List<SnowFlake>();
        double SnowLevel = 0;
        Vector2 areaDimensions = new();

        public SnowView()
        {
            this.InitializeComponent();
            this.Loaded += RainView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += RainChanged;
            OzoraViewModel.Instance.NightTimeUpdate += NightTimeUpdate;
            ApplicationViewModel.Instance.PropertyChanged += AppPreferencesChanged;
        }

        private void RainView_Loaded(object sender, RoutedEventArgs e)
        {
            SnowLevel = WeatherViewModel.Instance.Hail;
            handleAnimations();
        }

        private void AppPreferencesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _snowAnimationTimer.Start();
            else _snowAnimationTimer.Stop();
        }

        private void NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SnowCanvas.Invalidate();
        }

        private void RainChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SnowLevel = WeatherViewModel.Instance.Snow;
            Random rnd = new Random();
            if (!uiSettings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying)
            {
                while (SnowFlakes.Count < WeatherViewModel.Instance.Snow * 500)
                {
                    SnowFlakes.Add(SnowFlake.CreateNewHailCorn());
                }
                while (SnowFlakes.Count > WeatherViewModel.Instance.Snow * 500)
                {
                    SnowFlakes.RemoveAt(rnd.Next(0, SnowFlakes.Count() - 1));
                }
            }
            SnowCanvas.Invalidate();
        }

        private void handleAnimations()
        {
            _snowAnimationTimer = new DispatcherTimer();
            _snowAnimationTimer.Interval = TimeSpan.FromMilliseconds(1000 / ApplicationViewModel.Instance.Framerate);
            _snowAnimationTimer.Tick += AnimationTimer_Tick;
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _snowAnimationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            Random rnd = new Random();
            for (int i = 0; i < SnowLevel * 3 * ApplicationViewModel.Instance.MotionModifier; i++) SnowFlakes.Add(SnowFlake.CreateNewHailCorn());

            if (uiSettings.AnimationsEnabled)
            {
                var areaWidth = SnowCanvas.ActualWidth;

                for (int i = 0; i < SnowFlakes.Count(); i++)
                {
                    var droplet = SnowFlakes[i];
                    droplet.AnimateCorn();
                    if (droplet.Translation.Y > 1 + droplet.Width / areaWidth)
                    {
                        SnowFlakes.Remove(droplet);
                    }
                }

                //collect clouds to render again and render
                SnowCanvas.Invalidate();
            }
        }

        private void RainCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            if (SnowFlakes.Count() > 0)
            {
                // calculate colors ahead of running the for loop
                var color = Colors.White;
                float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                nightTimeModifier = (float)(Math.Clamp(nightTimeModifier - 0.33, 0, 1) * 1.25);

                float colorMultiplier = 1f - (nightTimeModifier * 0.9f);

                color.R = (byte)(color.R * colorMultiplier);
                color.G = (byte)(color.G * colorMultiplier);
                color.B = (byte)(color.B * colorMultiplier);

                foreach (var obj in SnowFlakes)
                {
                    var leadingTranslation = new System.Numerics.Vector2(
                        obj.Translation.X * areaDimensions.X + (float)(areaDimensions.X * 0.01 * Math.Sin((obj.Translation.Y * 10) + obj.AnimationOffset)),
                        obj.Translation.Y * areaDimensions.Y);


                    if (leadingTranslation.X > -obj.Width &&
                        leadingTranslation.Y > -obj.Width &&
                        leadingTranslation.X < areaDimensions.X + obj.Width &&
                        leadingTranslation.Y < areaDimensions.Y + obj.Width)
                    {
                        // save performance by only rendering things that are close to the camera as proper circles
                        // proximity can assume a value between 0.25 and 1
                        if (obj.Proximity > 0.85) ds.FillCircle(leadingTranslation, (float)(obj.Width * 0.6), color);
                        else
                        {
                            var bounds = new Rect(leadingTranslation.X, leadingTranslation.Y, obj.Width, obj.Width);
                            ds.FillRectangle(bounds, color);
                        }
                    }
                }
            }
        }

        private void RainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            areaDimensions = new Vector2((float)SnowCanvas.ActualWidth, (float)SnowCanvas.ActualHeight);
            SnowCanvas.Invalidate();
        }

        internal class SnowFlake
        {
            public System.Numerics.Vector2 Translation { get; set; }
            public float Width { get; set; }
            public float Speed { get; set; }
            public float WindAffection { get; set; }
            public float AnimationOffset { get; set; }
            internal float Proximity { get; set; }

            public static SnowFlake CreateNewHailCorn()
            {
                Random rnd = new Random();
                Vector2 position = new Vector2((float)(rnd.NextDouble()), (float)-0.02);
                UISettings settings = new UISettings();
                if (!settings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying) 
                {
                    position = new Vector2(position.X, (float)rnd.NextDouble());
                }
                else if (WeatherViewModel.Instance.WindSpeed > 0)
                {
                    position = new Vector2((float)(rnd.NextDouble() * (1 + WeatherViewModel.Instance.WindSpeed / 20) - WeatherViewModel.Instance.WindSpeed / 20), position.Y);
                }
                float proximity = (float)(rnd.NextDouble() * 0.75 + 0.25);
                float speed = (float)(0.005 * proximity);
                float windSpeed = (float)WeatherViewModel.Instance.WindSpeed;
                float windAffection = (float)((rnd.NextDouble() * 0.5 + 1) / 3);

                var rainDrop = new SnowFlake()
                {
                    Translation = position,
                    Width = 5 * proximity,
                    Speed = speed,
                    WindAffection = windAffection,
                    AnimationOffset = (float)(rnd.NextDouble() * 2 * Math.PI),
                    Proximity = proximity
                };
                return rainDrop;
            }

            public void AnimateCorn()
            {
                this.Translation += new System.Numerics.Vector2((float)(WindAffection * (WeatherViewModel.Instance.WindSpeed / 15) * this.Speed * ApplicationViewModel.Instance.MotionModifier), (float)(this.Speed * ApplicationViewModel.Instance.MotionModifier));
            }
        }
    }
}
