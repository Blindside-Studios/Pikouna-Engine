using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Pikouna_Engine;
using System.Diagnostics;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pikouna_Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            WeatherViewModel.Instance.WeatherValues = new ObservableCollection<WeatherType>(Enum.GetValues(typeof(WeatherType)) as WeatherType[]);
            ControlPanel.DataContext = Pikouna_Engine.WeatherViewModel.Instance;
            ContentFrame.NavigateToType(typeof(Pikouna_Engine.WeatherView), null, null);
        }

        private void EverythingGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            OzoraViewModel.Instance.MousePosition = e.GetCurrentPoint((UIElement)sender).Position;
        }

        private void EverythingGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            OzoraViewModel.Instance.MouseEngaged = true;
        }

        private void EverythingGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            OzoraViewModel.Instance.MouseEngaged = false;
        }
    }
}
