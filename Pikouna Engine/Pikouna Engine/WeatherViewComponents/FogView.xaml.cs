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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Engine.WeatherViewComponents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FogView : Page
    {
        public FogView()
        {
            this.InitializeComponent();
            OzoraViewModel.Instance.NightTimeUpdate += Instance_NightTimeUpdate;
            updateColor();
        }

        private async void Instance_NightTimeUpdate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            updateColor();
        }

        private void updateColor()
        {
            Windows.UI.Color color = Microsoft.UI.Colors.LightGray;
            var modifier = (1 - (OzoraViewModel.Instance.NightTimeModifier * 0.75));
            var newColor = Windows.UI.Color.FromArgb(255,
                (byte)(color.R * modifier),
                (byte)(color.G * modifier),
                (byte)(color.B * modifier)
            );
            FogCanvas.ClearColor = newColor;
        }
    }
}
