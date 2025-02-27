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

            // Load the foreground
            // TODO: Implement that it can load different scenes, but who cares rn?
            SceneFrame.NavigateToType(typeof(SceneComponents.ChateauDombrage), null, null);
            WeatherViewModel.Instance.PropertyChanged += RequestedWeatherChanged;
        }

        private void RequestedWeatherChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // potentially handle weather changed here later
        }
    }
}
