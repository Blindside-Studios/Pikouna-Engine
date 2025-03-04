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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WeatherView : Page
    {
        public WeatherView()
        {
            this.InitializeComponent();
            SunView.NavigateToType(typeof(WeatherViewComponents.OzoraSunView), null, null);
            CloudsView.NavigateToType(typeof(WeatherViewComponents.CloudsView), null, null);
            RainView.NavigateToType(typeof(WeatherViewComponents.RainView), null, null);
            HailView.NavigateToType(typeof(WeatherViewComponents.HailView), null, null);
            SnowView.NavigateToType(typeof(WeatherViewComponents.SnowView), null, null);
            FogView1.NavigateToType(typeof(WeatherViewComponents.FogView), null, null);
            FogView2.NavigateToType(typeof(WeatherViewComponents.FogView), null, null);

            // Load the foreground
            // TODO: Implement that it can load different scenes, but who cares rn?
            SceneFrame.NavigateToType(typeof(SceneComponents.ChateauDombrage), null, null);
            WeatherViewModel.Instance.PropertyChanged += RequestedWeatherChanged;
        }

        private void RequestedWeatherChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FogView1.OpacityTransition = new ScalarTransition();
            FogView2.OpacityTransition = new ScalarTransition();
            if (WeatherViewModel.Instance.WeatherType == WeatherType.Fog)
            {
                FogView1.Opacity = 0.5;
                FogView2.Opacity = 0.5;
            }
            else
            {
                FogView1.Opacity = 0;
                FogView2.Opacity = 0;
            }
        }
    }
}
