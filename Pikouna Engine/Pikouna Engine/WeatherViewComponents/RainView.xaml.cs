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
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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
    public sealed partial class RainView : Page
    {
        DispatcherTimer _rainAnimationTimer;
        UISettings uiSettings = new UISettings();

        List<RainDrop> RainDrops = new List<RainDrop>();
        double RainAmountMM = 0;
        Vector2 areaDimensions = new();

        public RainView()
        {
            this.InitializeComponent();
            this.Loaded += RainView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += RainChanged;
            OzoraViewModel.Instance.NightTimeUpdate += NightTimeUpdate;
            ApplicationViewModel.Instance.PropertyChanged += AppPreferencesChanged;
        }

        private void RainView_Loaded(object sender, RoutedEventArgs e)
        {
            RainAmountMM = WeatherViewModel.Instance.Showers;
            handleAnimations();
        }

        private void AppPreferencesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _rainAnimationTimer.Start();
            else _rainAnimationTimer.Stop();
        }

        private void NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RainCanvas.Invalidate();
        }

        private void RainChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RainAmountMM = WeatherViewModel.Instance.Showers;
            Random rnd = new Random();
            for (int i = 0; i < RainDrops.Count(); i++)
            {
                try
                {
                    RainDrops[i].recalculateAngle();
                }
                catch { }
            }
            if (!uiSettings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying)
            {
                while (RainDrops.Count < WeatherViewModel.Instance.Showers * 100)
                {
                    RainDrops.Add(RainDrop.CreateNewRainDrop());
                }
                while (RainDrops.Count > WeatherViewModel.Instance.Showers * 100)
                {
                    RainDrops.RemoveAt(rnd.Next(0, RainDrops.Count() - 1));
                }
            }
            RainCanvas.Invalidate();
        }

        private void handleAnimations()
        {
            _rainAnimationTimer = new DispatcherTimer();
            _rainAnimationTimer.Interval = TimeSpan.FromMilliseconds(1000 / ApplicationViewModel.Instance.Framerate);
            _rainAnimationTimer.Tick += AnimationTimer_Tick;
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _rainAnimationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            Random rnd = new Random();
            for (int i = 0; i < RainAmountMM * 2 * ApplicationViewModel.Instance.MotionModifier; i++) RainDrops.Add(RainDrop.CreateNewRainDrop());

            if (uiSettings.AnimationsEnabled)
            {
                var areaWidth = RainCanvas.ActualWidth;

                for (int i = 0; i < RainDrops.Count(); i++)
                {
                    var droplet = RainDrops[i];
                    droplet.AnimateDroplet();
                    if (droplet.Translation.Y > 1 + droplet.Length / areaWidth)
                    {
                        RainDrops.Remove(droplet);
                    }
                }

                //collect clouds to render again and render
                RainCanvas.Invalidate();
            }
        }

        private void RainCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            if (RainDrops.Count() > 0)
            {
                // calculate colors ahead of running the for loop
                var color = Colors.AliceBlue;
                float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                nightTimeModifier = (float)(Math.Clamp(nightTimeModifier - 0.33, 0, 1) * 1.25);

                float colorMultiplier = 1f - (nightTimeModifier * 0.9f);

                color.R = (byte)(color.R * colorMultiplier);
                color.G = (byte)(color.G * colorMultiplier);
                color.B = (byte)(color.B * colorMultiplier);

                foreach (var obj in RainDrops)
                {
                    var leadingTranslation = new System.Numerics.Vector2(obj.Translation.X * areaDimensions.X, obj.Translation.Y * areaDimensions.Y);
                    var trailingTranslation = new Vector2(obj.Translation.X * areaDimensions.X + (float)Math.Sin(obj.Angle) * obj.Length, obj.Translation.Y * areaDimensions.Y + (float)Math.Cos(obj.Angle) * obj.Length);


                    if (leadingTranslation.X > -obj.Length &&
                        leadingTranslation.Y > -obj.Length &&
                        trailingTranslation.X < areaDimensions.X + obj.Length &&
                        trailingTranslation.Y < areaDimensions.Y + obj.Length)
                    {

                    ds.DrawLine(
                        leadingTranslation, 
                        trailingTranslation,
                        color, 
                        obj.Width, 
                        new CanvasStrokeStyle() 
                        { 
                            // use triangles instead of circles to avoid anti aliasing of rounded shapes
                            StartCap = CanvasCapStyle.Triangle, 
                            EndCap = CanvasCapStyle.Triangle 
                        });
                    }
                }
            }
        }

        private void RainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            areaDimensions = new Vector2((float)RainCanvas.ActualWidth, (float)RainCanvas.ActualHeight);
            RainCanvas.Invalidate();
        }

        internal class RainDrop
        {
            public System.Numerics.Vector2 Translation { get; set; }
            public float Length { get; set; }
            public float Width { get; set; }
            public float Speed { get; set; }
            public float Angle { get; set; }
            public float WindAffection { get; set; }
            internal float Proximity { get; set; }

            public static RainDrop CreateNewRainDrop()
            {
                Random rnd = new Random();
                Vector2 position = new Vector2((float)(rnd.NextDouble() * 2 - 1), (float)-0.25);
                UISettings settings = new UISettings();
                if (!settings.AnimationsEnabled || !ApplicationViewModel.Instance.AreAnimationsPlaying) position = new Vector2(position.X, (float)rnd.NextDouble());
                float proximity = (float)(rnd.NextDouble() * 0.75 + 0.25);
                float speed = (float)(0.2 * proximity);
                float windSpeed = (float)WeatherViewModel.Instance.WindSpeed;
                float windAffection = (float)((rnd.NextDouble() * 0.5 + 1));
                
                var rainDrop = new RainDrop()
                {
                    Translation = position,
                    Length = 50 * proximity + windSpeed,
                    Width = 2 * proximity,
                    Speed = speed,
                    Angle = (float)Math.Atan(windSpeed * windAffection * 0.01),
                    WindAffection = windAffection,
                    Proximity = proximity
                };
                return rainDrop;
            }

            public void recalculateAngle()
            {
                Random rnd = new Random();
                float windSpeed = (float)WeatherViewModel.Instance.WindSpeed;
                float windAffection = (float)((rnd.NextDouble() * 0.5 + 1));
                Length = 50 * this.Proximity + windSpeed;
                this.Angle = (float)Math.Atan(windSpeed * windAffection * 0.01);
            }

            public void AnimateDroplet()
            {
                this.Translation += new System.Numerics.Vector2((float)(WindAffection * (WeatherViewModel.Instance.WindSpeed / 150) * this.Speed * ApplicationViewModel.Instance.MotionModifier), (float)(this.Speed * ApplicationViewModel.Instance.MotionModifier));
            }
        }
    }
}
