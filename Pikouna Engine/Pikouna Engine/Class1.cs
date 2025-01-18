using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pikouna_Engine
{
    public class Class1
    {

    }

    public class OzoraViewModel : INotifyPropertyChanged
    {
        private static OzoraViewModel _instance;
        public static OzoraViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OzoraViewModel();
                }
                return _instance;
            }
        }

        public Windows.Foundation.Point MousePosition
        {
            get => _mousePosition;
            set
            {
                if (_mousePosition != value)
                {
                    _mousePosition = value;
                    OnPropertyChanged(nameof(MousePosition));
                }
            }
        }
        private Windows.Foundation.Point _mousePosition;

        public bool MouseEngaged
        {
            get => _mouseEngaged;
            set
            {
                if (value != _mouseEngaged)
                {
                    _mouseEngaged = value;
                    OnPropertyChanged(nameof(MouseEngaged));
                }
            }
        }
        private bool _mouseEngaged = true;

        public ElementTheme ViewTheme
        {
            get => _viewTheme;
            set
            {
                if (value != _viewTheme)
                {
                    _viewTheme = value;
                    OnThemeChangeRequested(nameof(ViewTheme));
                }
            }
        }
        private ElementTheme _viewTheme = ElementTheme.Default;

        public event PropertyChangedEventHandler ThemeChangeRequested;
        protected virtual void OnThemeChangeRequested(string propertyName)
        {
            ThemeChangeRequested?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
