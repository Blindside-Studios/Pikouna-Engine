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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    public sealed partial class CloudsView : Page
    {
        List<CloudMainEntity> Clouds = new List<CloudMainEntity> { };
        List<CloudRenderObject> renderedClouds = new();
        public static double maxCloudWidth = 0;
        private int numberOfClouds = 50;
        private Point areaSize = new Point(0, 0);
        private DispatcherTimer _cloudsAnimationTimer;
        UISettings uiSettings = new UISettings();

        public CloudsView()
        {
            this.InitializeComponent();
            this.Loaded += CloudsView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += CloudCoverageChanged;
            OzoraViewModel.Instance.NightTimeUpdate += NightTimeChanged;
            ApplicationViewModel.Instance.PropertyChanged += AppPreferencesChanged;
        }

        private void AppPreferencesChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ApplicationViewModel.Instance.AreAnimationsPlaying) _cloudsAnimationTimer.Start();
            else _cloudsAnimationTimer.Stop();
        }

        private void CloudsView_Loaded(object sender, RoutedEventArgs e)
        {
            DrawClouds();
            handleAnimations();
        }

        private void handleAnimations()
        {
            _cloudsAnimationTimer = new DispatcherTimer();
            _cloudsAnimationTimer.Interval = TimeSpan.FromMilliseconds(17); // Fires 60 times per second
            _cloudsAnimationTimer.Tick += AnimationTimer_Tick;
            if(ApplicationViewModel.Instance.AreAnimationsPlaying) _cloudsAnimationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            if (uiSettings.AnimationsEnabled)
            {
                var areaWidth = CloudsCanvas.ActualWidth;
                foreach (var cloud in Clouds)
                {
                    cloud.Translation += new Vector2((float)(cloud.MovementSpeed * (WeatherViewModel.Instance.WindSpeed / 20) * ApplicationViewModel.Instance.MotionModifier), 0);
                    if (cloud.Translation.X > 1 + cloud.Radius / areaWidth)
                    {
                        cloud.Translation = new Vector2((float)-(cloud.Radius / areaWidth), cloud.Translation.Y);
                    }
                    cloud.animateChildren();
                }

                //collect clouds to render again and render
                renderedClouds = GetRenderingTargets();
                CloudsCanvas.Invalidate();
            }
        }

        private void CloudCoverageChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WeatherViewModel.CloudCover)) DrawClouds();
        }

        private void DrawClouds()
        {
            maxCloudWidth = (2.58 * WeatherViewModel.Instance.CloudCover * (CloudsCanvas.ActualWidth * CloudsCanvas.ActualHeight / 1000000));

            if (WeatherViewModel.Instance.CloudCover != 0 && Clouds.Count == 0)
            {
                for (int i = 0; i < numberOfClouds; i++)
                {
                    Clouds.Add(CloudMainEntity.RequestNewCloud(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth, (float)i / numberOfClouds));
                }
            }
            else if (WeatherViewModel.Instance.CloudCover == 0 && Clouds.Count != 0) Clouds.Clear();
            else if (Clouds.Count() > 0) foreach (var cloud in Clouds) cloud.ManageProperties(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth);

            renderedClouds = GetRenderingTargets();

            CloudsCanvas.Invalidate();
        }

        private void CloudsCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            if (Clouds.Count() > 0)
            {
                // calculate colors ahead of running the for loop
                var color = Colors.White;
                float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                nightTimeModifier = (float)(Math.Clamp(nightTimeModifier - 0.33, 0, 1) * 1.25);

                float redMultiplier = 1 - (float)Math.Clamp((3 * nightTimeModifier * (Math.Pow(nightTimeModifier - 0.66, 3) * 1.7 + 0.374)), 0, 1);
                float greenMultiplier = 1 - (float)Math.Clamp(Math.Sqrt(nightTimeModifier * 1.25), 0, 1);
                float blueMultiplier = 1 - (float)Math.Clamp((2 * nightTimeModifier * (Math.Pow(nightTimeModifier - 0.6, 5) * 6.43 + 0.5)) * 1.025, 0, 1);

                color.R = (byte)(color.R * redMultiplier);
                color.G = (byte)(color.G * greenMultiplier);
                color.B = (byte)(color.B * blueMultiplier);

                // make sure the background color is correct
                var fullCloudThreshold = 70;
                if (WeatherViewModel.Instance.CloudCover >= fullCloudThreshold)
                {
                    var backgroundColor = color;
                    backgroundColor.A = (byte)(25.5 * (WeatherViewModel.Instance.CloudCover - fullCloudThreshold) / ((100 - fullCloudThreshold) / 10));
                    CloudsCanvas.ClearColor = backgroundColor;
                }
                else CloudsCanvas.ClearColor = Colors.Transparent;
                
                foreach (var obj in renderedClouds)
                {
                    if (obj.Translation.X > -obj.Radius && 
                        obj.Translation.Y > -obj.Radius && 
                        obj.Translation.X < areaSize.X + obj.Radius && 
                        obj.Translation.Y < areaSize.Y + obj.Radius)
                    {
                        var finalColor = color;
                        finalColor.R = (byte)Math.Clamp(finalColor.R - (5 * obj.RenderHierarchy + nightTimeModifier / 10), 0, 255);
                        finalColor.G = (byte)Math.Clamp(finalColor.G - (5 * obj.RenderHierarchy + nightTimeModifier / 10), 0, 255);

                        ds.FillCircle(obj.Translation, (float)obj.Radius, finalColor);
                    }
                }
            }
        }

        private void CloudsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            areaSize = new Point(CloudsCanvas.ActualWidth, CloudsCanvas.ActualHeight);
            maxCloudWidth = (2.58 * WeatherViewModel.Instance.CloudCover * (CloudsCanvas.ActualWidth * CloudsCanvas.ActualHeight / 1000000));
            if (Clouds.Count() > 0)
            {
                foreach (var cloud in Clouds) cloud.ManageProperties(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth);
            }
            renderedClouds = GetRenderingTargets();
            CloudsCanvas.Invalidate();
        }

        private void NightTimeChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CloudsCanvas.Invalidate();
        }

        private List<CloudRenderObject> GetRenderingTargets()
        {
            var renderTargets = new List<CloudRenderObject>();
            foreach (var cloud in Clouds) { 
                var newClouds = cloud.getObjectsToRender((float)CloudsCanvas.ActualWidth, (float)CloudsCanvas.ActualHeight); 
                renderTargets.AddRange(newClouds);
            }
            return renderTargets;
        }
    }

    class CloudMainEntity
    {
        public static CloudMainEntity RequestNewCloud(double AreaSize, float yOffset)
        {
            Random rnd = new Random();
            var cloud = new CloudMainEntity
            {
                Translation = new Vector2((float)rnd.NextDouble(), yOffset),
                MovementSpeed = rnd.NextDouble()/5000 + 0.00025,
                SpeedBoost = 0
            };
            cloud.ManageProperties(AreaSize);
            return cloud;
        }

        public void ManageProperties(double AreaSize)
        {
            var cloudCover = WeatherViewModel.Instance.CloudCover;
            var attachments = Convert.ToInt32(Math.Round(cloudCover / 20)) + 2;

            if (cloudCover > 0) this.Radius = cloudCover * (AreaSize / 1000000);
            else this.Radius = 0;

            if (AttachedClouds == null) AttachedClouds = new List<CloudAttachmentBlob>();

            while (AttachedClouds.Count() > attachments) AttachedClouds.RemoveAt(0);
            
            if (AttachedClouds.Count() < attachments)
            {
                while (AttachedClouds.Count() < attachments)
                {
                    AttachedClouds.Add(CloudAttachmentBlob.GenerateAttachment(1, this.Radius));
                }
            }
            foreach (var cloud in AttachedClouds)
            {
                cloud.ParentRadius = this.Radius;
                cloud.ManageProperties();
            }
        }

        public List<CloudRenderObject> getObjectsToRender(float CanvasWidth, float CanvasHeight)
        {
            var list = new List<CloudRenderObject>();
            // this ensures the cloud is properly drawn evenly and can spawn outside of the screen to prevent "bunching up" inside of the screen area, causing blank spaces in the scroll animation
            var translation = new Vector2((float)(this.Translation.X * (CanvasWidth + 2*CloudsView.maxCloudWidth) - CloudsView.maxCloudWidth), this.Translation.Y * CanvasHeight);
            list.Add(new CloudRenderObject()
            {
                Radius = this.Radius,
                Translation = translation,
                RenderHierarchy = 0
            });
            foreach(var cloud in AttachedClouds)
            {
                list.AddRange(cloud.contributeRenderAssets(translation));
            }
            list = list.OrderBy(o => o.RenderHierarchy).ToList();
            return list;
        }

        public void animateChildren()
        {
            foreach (var cloud in AttachedClouds)
            {
                cloud.animateAngles();
            }
        }

        public double Radius { get; set; }
        public Vector2 Translation { get; set; }
        public double MovementSpeed { get; set; }
        public double SpeedBoost { get; set; }
        public List<CloudAttachmentBlob> AttachedClouds { get; set; }
    }

    class CloudAttachmentBlob
    {
        public static CloudAttachmentBlob GenerateAttachment(int hierarchyDepth, double parentRadius) 
        {
            Random rnd = new Random();
            var cloudCover = WeatherViewModel.Instance.CloudCover;
            var cloud = new CloudAttachmentBlob()
            {
                HierarchyDepth = hierarchyDepth,
                ParentRadius = parentRadius,
                AttachmentAngle = rnd.NextDouble() * 2 * Math.PI,
                AngularMovementSpeed = (rnd.NextDouble() - 0.5) / 200,
                AttachedClouds = new List<CloudAttachmentBlob>() { },
                ChildGenerationVariance = rnd.NextDouble() + 0.5
            };
            cloud.ManageProperties();
            return cloud;
        }

        public void ManageProperties()
        {
            var cloudCover = WeatherViewModel.Instance.CloudCover;
            this.Radius = this.ParentRadius * 0.66;
            if (HierarchyDepth < (cloudCover / 25))
            {
                var attachments = Convert.ToInt32(Math.Round(cloudCover / 25 * this.ChildGenerationVariance * 1/this.HierarchyDepth));
                if (AttachedClouds == null) AttachedClouds = new List<CloudAttachmentBlob>();
                while (AttachedClouds.Count() > attachments) AttachedClouds.RemoveAt(0);
                if (AttachedClouds.Count() < attachments)
                {
                    while (AttachedClouds.Count() < attachments)
                    {
                        AttachedClouds.Add(CloudAttachmentBlob.GenerateAttachment(this.HierarchyDepth + 1, this.Radius));
                    }
                }
                foreach (var cloud in AttachedClouds)
                {
                    cloud.ParentRadius = this.Radius;
                    cloud.ManageProperties();
                }
            }
            else AttachedClouds.Clear();
        }

        public List<CloudRenderObject> contributeRenderAssets(Vector2 parentTranslation)
        {
            var list = new List<CloudRenderObject>();
            var translation = new Vector2(
                (float)(Math.Cos(this.AttachmentAngle) * ParentRadius + parentTranslation.X), 
                (float)(Math.Sin(this.AttachmentAngle) * ParentRadius + parentTranslation.Y));
            list.Add(new CloudRenderObject()
            {
                Radius = this.Radius,
                Translation = translation,
                RenderHierarchy = this.HierarchyDepth,
            });
            foreach (var cloud in AttachedClouds)
            {
                list.AddRange(cloud.contributeRenderAssets(translation));
            }
            return list;
        }

        public void animateAngles()
        {
            this.AttachmentAngle += this.AngularMovementSpeed * ApplicationViewModel.Instance.MotionModifier * (WeatherViewModel.Instance.WindSpeed / 20);
            foreach (var cloud in AttachedClouds)
            {
                cloud.animateAngles();
            }
        }

        public int HierarchyDepth { get; set; }
        public double ParentRadius { get; set; }
        public double Radius { get; set; }
        public double AttachmentAngle { get; set; }
        public double AngularMovementSpeed { get; set; }
        public List<CloudAttachmentBlob> AttachedClouds { get; set; }
        public double ChildGenerationVariance { get; set; }
    }

    class CloudRenderObject
    {
        public double Radius { get; set; }
        public Vector2 Translation { get; set; }
        public int RenderHierarchy { get; set; }
    }
}
