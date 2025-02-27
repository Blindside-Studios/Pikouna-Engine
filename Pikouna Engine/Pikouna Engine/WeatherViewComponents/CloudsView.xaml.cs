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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    public sealed partial class CloudsView : Page
    {
        List<CloudMainEntity> Clouds = new List<CloudMainEntity> { };
        
        public CloudsView()
        {
            this.InitializeComponent();
            this.Loaded += CloudsView_Loaded;
            WeatherViewModel.Instance.PropertyChanged += Instance_PropertyChanged;
            OzoraViewModel.Instance.NightTimeUpdate += Instance_NightTimeUpdate;
        }

        private void CloudsView_Loaded(object sender, RoutedEventArgs e)
        {
            WeatherViewModel.Instance.CloudCover = 50;
            for (int i = 0; i < 10; i++)
            {
                Clouds.Add(CloudMainEntity.RequestNewCloud(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth));
            }
        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // add logic to handle updated cloud cover here
            if (WeatherViewModel.Instance.CloudCover != 0 && Clouds.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    Clouds.Add(CloudMainEntity.RequestNewCloud(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth));
                }
            }
            else if (WeatherViewModel.Instance.CloudCover == 0 && Clouds.Count != 0) Clouds.Clear();
            else if (Clouds.Count() > 0) foreach (var cloud in Clouds) cloud.ManageProperties(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth);

            if (WeatherViewModel.Instance.CloudCover == 100) CloudsCanvas.ClearColor = Colors.White;
            else CloudsCanvas.ClearColor = Colors.Transparent;

            CloudsCanvas.Invalidate();
        }

        private void CloudsCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            //ds.Clear(Colors.SkyBlue);

            if (Clouds.Count() > 0)
            {
                foreach (var cloud in Clouds)
                {
                    var renderObjects = cloud.getObjectsToRender((float)CloudsCanvas.ActualWidth, (float)CloudsCanvas.ActualHeight);

                    foreach (var obj in renderObjects)
                    {
                        var color = Colors.White;
                        
                        float nightTimeModifier = (float)OzoraViewModel.Instance.NightTimeModifier;
                        nightTimeModifier = (float)(Math.Clamp(nightTimeModifier - 0.33, 0, 1) * 1.25);
                        float redMultiplier = 1 - (float)Math.Clamp((3 * nightTimeModifier * (Math.Pow(nightTimeModifier - 0.66, 3) * 1.7 + 0.374)), 0, 1);
                        //float redMultiplier = 1 - (float)Math.Clamp(Math.Sqrt(nightTimeModifier * 1.2), 0, 1);
                        float greenMultiplier = 1 - (float)Math.Clamp(Math.Sqrt(nightTimeModifier * 1.25), 0, 1);
                        //float blueMultiplier = 1 - (float)Math.Clamp(Math.Sqrt(nightTimeModifier * 1.05), 0, 1);
                        float blueMultiplier = 1 - (float)Math.Clamp((2 * nightTimeModifier * (Math.Pow(nightTimeModifier - 0.6, 5) * 6.43 + 0.5)) * 1.025, 0, 1);

                        // make sure the background color is correct
                        if (CloudsCanvas.ClearColor != Colors.Transparent) CloudsCanvas.ClearColor = color;

                        color.R = color.G = (byte)(255 - (5 * obj.RenderHierarchy + nightTimeModifier / 10));
                        
                        color.R = (byte)(color.R * redMultiplier);
                        color.G = (byte)(color.G * greenMultiplier);
                        color.B = (byte)(color.B * blueMultiplier);


                        ds.FillCircle(obj.Translation, (float)obj.Radius, color);
                    }
                }
            }
        }

        private void CloudsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Clouds.Count() > 0)
            {
                foreach (var cloud in Clouds) cloud.ManageProperties(CloudsCanvas.ActualHeight * CloudsCanvas.ActualWidth);
            }
            CloudsCanvas.Invalidate();
        }

        private void Instance_NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CloudsCanvas.Invalidate();
        }
    }

    class CloudMainEntity
    {
        public static CloudMainEntity RequestNewCloud(double AreaSize)
        {
            Random rnd = new Random();
            var cloud = new CloudMainEntity
            {
                Translation = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()),
                MovementSpeed = 0
            };
            cloud.ManageProperties(AreaSize);
            return cloud;
        }

        public void ManageProperties(double AreaSize)
        {
            var cloudCover = WeatherViewModel.Instance.CloudCover;
            var attachments = Convert.ToInt32(Math.Round(cloudCover / 20)) + 2;

            if (cloudCover > 0) this.Radius = cloudCover * (AreaSize / 500000);
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
            var translation = new Vector2(this.Translation.X * CanvasWidth, this.Translation.Y * CanvasHeight);
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

        public double Radius { get; set; }
        public Vector2 Translation { get; set; }
        public double MovementSpeed { get; set; }
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
                AngularMovementSpeed = 0,
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
                var attachments = Convert.ToInt32(Math.Round(cloudCover / 33 * this.ChildGenerationVariance));
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
