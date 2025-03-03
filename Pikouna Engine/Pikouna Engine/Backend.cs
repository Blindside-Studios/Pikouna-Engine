using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace Pikouna_Engine
{
    public class Calculations
    {
        public static double GetNightModifier(double position, double _workingHeight)
        {
            if (position > (_workingHeight / 4) * 3)
            {
                return 1;
            }
            else if (position > (_workingHeight / 4))
            {
                // Calculate the relative distance from the top that the vector is in the middle third of the grid from 0 to 1.
                double _nightTimeModifier = (position - (_workingHeight / 4)) / (_workingHeight / 2);
                // Clamp to avoid problematic situations
                if (_nightTimeModifier < 0) return 0;
                else if (_nightTimeModifier > 1) return 1;
                else return _nightTimeModifier;
            }
            else return 0;
        }

        public static List<Star> GenerateStarPositions(int amount, bool willAnimate)
        {
            Random rnd = new Random();
            var list = new List<Star>();
            for (int i = 0; i < amount; i++)
            {
                float radius = willAnimate ? (float)(rnd.NextDouble() + 1.5) : (float)(rnd.NextDouble() + 1);
                var vect = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                double opacity = ((double)rnd.Next(50, 100)) / 100;
                list.Add(new Star()
                {
                    Radius = radius,
                    Position = vect,
                    Opacity = opacity,
                    MaximumChangePerUpdate = rnd.NextDouble() * 0.2,
                    MinimumOpacity = (rnd.NextDouble() / 2) + 0.2
                });
            }
            return list;
        }
    }

    public class PikounaColors
    {
        public static List<GradientCP> TopCPs = new List<GradientCP>
        {
            new GradientCP{ Position = 0, Color = GradientCP.FromHex("#6FC5F6") },
            new GradientCP{ Position = 0.5, Color = GradientCP.FromHex("#8509F9") },
            new GradientCP{ Position = 1, Color = GradientCP.FromHex("#050416") }
        };

        public static List<GradientCP> BottomCPs = new List<GradientCP>
        {
            new GradientCP{ Position = 0, Color = GradientCP.FromHex("#117CD3") },
            new GradientCP{ Position = 0.5, Color = GradientCP.FromHex("#FF8B28") },
            new GradientCP{ Position = 1, Color = GradientCP.FromHex("#0C0B25") }
        };

        public static Windows.UI.Color GetInterpolatedColor(double position, List<GradientCP> controlPoints)
        {
            // 1. Clamp input so we don't step out of range
            position = Math.Max(0, Math.Min(1, position));

            // 2. Ensure controlPoints are sorted by Position
            var sortedCPs = controlPoints.OrderBy(cp => cp.Position).ToList();

            // 3. If position is <= first CP or >= last CP, just return that edge color
            if (position <= sortedCPs.First().Position)
                return sortedCPs.First().Color;

            if (position >= sortedCPs.Last().Position)
                return sortedCPs.Last().Color;

            // 4. Otherwise, find the two CPs that bracket our position
            for (int i = 0; i < sortedCPs.Count - 1; i++)
            {
                var cp1 = sortedCPs[i];
                var cp2 = sortedCPs[i + 1];

                if (position >= cp1.Position && position <= cp2.Position)
                {
                    // 5. Compute fraction between these two control points
                    double range = cp2.Position - cp1.Position;
                    double lerpAmount = (position - cp1.Position) / range;
                    return InterpolateColor(cp1.Color, cp2.Color, (float)lerpAmount);
                }
            }

            // Fallback (should never happen if your positions cover [0,1], but in case)
            return sortedCPs.Last().Color;
        }

        private static Windows.UI.Color InterpolateColor(Windows.UI.Color start, Windows.UI.Color end, float t)
        {
            byte a = (byte)(start.A + (end.A - start.A) * t);
            byte r = (byte)(start.R + (end.R - start.R) * t);
            byte g = (byte)(start.G + (end.G - start.G) * t);
            byte b = (byte)(start.B + (end.B - start.B) * t);
            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
    }

    public class Star
    {
        public float Radius { get;set; }
        public Vector2 Position { get; set; }
        public double Opacity { get => _opacity;
            set
            {
                if (_opacity != value)
                {
                    // ensure the star stays within its extreme values
                    // reverse the last stored delta to make the star's opacity "bounce back" on next call
                    double difference = 0;
                    if (value < MinimumOpacity)
                    {
                        double _newValue = MinimumOpacity;
                        difference = _newValue - _opacity;
                        value = _newValue;
                    }
                    else if (value > 1)
                    {
                        double _newValue = 1;
                        difference = _newValue - _opacity;
                        value = _newValue;
                    }
                    else difference = value - _opacity;
                    
                    // clmaping the diffrence to prevent stars from gaining an insane amount of change per update
                    if (difference > MaximumChangePerUpdate) difference = MaximumChangePerUpdate;
                    else if (difference < -MaximumChangePerUpdate) difference = -MaximumChangePerUpdate;
                    _lastOpacityChange = difference;

                    _opacity = value;
                }
            }
        }
        private double _opacity;

        public double MaximumChangePerUpdate { get; set; }
        public double MinimumOpacity { get; set; }

        private double _lastOpacityChange = 0;

        public void CreateNewOpacity()
        {
            Random rnd = new Random();
            double _newOpacity = (double)rnd.Next(-10, 10) * MaximumChangePerUpdate * ApplicationViewModel.Instance.MotionModifier * 0.1;
            Opacity = Opacity + _lastOpacityChange + _newOpacity;
        }
    }


    /// <summary>
    /// CP STANDS FOR CONTROL POINT, NOT WHATEVER YOU MIGHT THINK!!!
    /// <para>Anyways, a GradientCP stores a position and a color that go together in the checkpoint.</para>
    /// <para>Naturally, you can use the position as a position in space, but it is in fact intended to be used to bring a color of one side of the gradient to a position of the curor.</para>
    /// <para>The gradient can then be transitioned over these control points on each side as the cursor moves.</para>
    /// We have the following:
    /// /// <list type="bullet">
    /// <item>
    /// <term>Position</term>
    /// <description>The Position of the cursor in relative space measured from the top. 0 is at the top, 1 is at the bottom.</description>
    /// </item>
    /// <item>
    /// <term>Color</term>
    /// <description>The Color in Windows.UI.Color - use FromRGB() and FromHex() to create</description>
    /// </item>
    /// </list>
    /// Then put them in a list or smth.
    /// </summary>
    public class GradientCP
    {
        public double Position { get; set; } // Position between 0 (top) and 1 (bottom)
        public Windows.UI.Color Color { get; set; }

        public static Windows.UI.Color FromRGB(byte r, byte g, byte b)
        {
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }

        public static Windows.UI.Color FromHex(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);

                return Windows.UI.Color.FromArgb(255, r, g, b);
            }
            else throw new ArgumentException("HEX value must be in the format RRGGBB.");
        }
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

        public double NightTimeModifier
        {
            get => _nightTimeModifier;
            set
            {
                if(value != _nightTimeModifier)
                {
                    _nightTimeModifier = value;
                    OnNightTimeUpdateRequested(nameof(NightTimeModifier));
                }
            }
        }
        private double _nightTimeModifier;

        public event PropertyChangedEventHandler NightTimeUpdate;
        protected virtual void OnNightTimeUpdateRequested(string propertyName)
        {
            NightTimeUpdate?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WeatherViewModel : INotifyPropertyChanged
    {
        private static WeatherViewModel _instance;
        public static WeatherViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WeatherViewModel();
                }
                return _instance;
            }
        }

        public WeatherType WeatherType
        {
            get => _weatherType;
            set
            {
                if (value != _weatherType)
                {
                    _weatherType = value;
                    OnPropertyChanged(nameof(WeatherType));
                }
            }
        }
        private WeatherType _weatherType = WeatherType.ClearSky;

        public double Showers
        {
            // TODO: Make this work with inches if that is ever implemented
            get => _showers;
            private set
            {
                if (value != _showers)
                {
                    _showers = value;
                    OnPropertyChanged(nameof(Showers));
                }
            }
        }
        private double _showers = 0;

        public double WindSpeed
        {
            get => _windSpeed;
            set
            {
                if (value != _windSpeed)
                {
                    _windSpeed = Math.Clamp(value, 0, 100);
                    OnPropertyChanged(nameof(WindSpeed));
                }
            }
        }
        private double _windSpeed = 20;

        internal double CloudCover
        {
            get => _cloudCover;
            private set
            {
                if (Math.Abs(value - _cloudCover) > 0.01)
                {
                    _cloudCover = value;
                    OnPropertyChanged(nameof(CloudCover));
                }
            }
        }
        private double _cloudCover = 0;

        public double CloudCoverageExternal
        {
            get => _cloudCoverage;
            set
            {
                if (Math.Abs(value - _cloudCoverage) > 0.01)
                {
                    UISettings uiSettings = new UISettings();
                    _cloudCoverage = value;
                    if (uiSettings.AnimationsEnabled) AnimateCloudCoverage(value);
                    else CloudCover = value;
                }
            }
        }
        private double _cloudCoverage = 0;

        private CancellationTokenSource _animationCancellationToken;

        private async void AnimateCloudCoverage(double targetValue)
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken = new CancellationTokenSource();
            CancellationToken token = _animationCancellationToken.Token;

            double startValue = CloudCover;
            double duration = 0.25;
            int steps = 15;
            double timePerStep = duration / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    if (token.IsCancellationRequested) return;

                    double t = (double)i / steps;
                    CloudCover = Lerp(startValue, targetValue, t);
                    await Task.Delay(TimeSpan.FromSeconds(timePerStep), token);
                }
            }
            catch (TaskCanceledException)
            {
                // Swallow the exception to prevent crashes when animations are interrupted
            }

            CloudCover = targetValue;
        }

        private static double Lerp(double start, double end, double t)
        {
            t = 1 - (1 - t) * (1 - t);
            return start + (end - start) * t;
        }

        // Used for XAML binding
        public ObservableCollection<WeatherType> WeatherValues { get; set; } = new ObservableCollection<WeatherType>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ApplicationViewModel : INotifyPropertyChanged
    {
        private static ApplicationViewModel _instance;
        public static ApplicationViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ApplicationViewModel();
                }
                return _instance;
            }
        }

        internal bool AreAnimationsPlaying
        {
            get => _areAnimationsPlaying;
            set
            {
                if (_areAnimationsPlaying != value)
                {
                    _areAnimationsPlaying = value;
                    OnPropertyChanged(nameof(AreAnimationsPlaying));
                }
            }
        }
        private bool _areAnimationsPlaying = true;
        internal double MotionModifier { get; set; } = 1;
        
        public bool CanPlayAnimations
        {
            get => _canPlayAnimations;
            set
            {
                if (_canPlayAnimations != value)
                {
                    AnimateAnimations(value);
                    _canPlayAnimations = value;
                }
            }
        }
        private bool _canPlayAnimations = true;

        private CancellationTokenSource _animationCancellationToken;
        private async void AnimateAnimations(bool targetValue)
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken = new CancellationTokenSource();
            CancellationToken token = _animationCancellationToken.Token;

            double targetModifier = 0;
            if (targetValue == true)
            {
                targetModifier = 1;
                AreAnimationsPlaying = true;
            }

            double startValue = MotionModifier;
            double duration = 0.5; // Animation time in seconds
            int steps = 30; // More steps = smoother animation
            double timePerStep = duration / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    if (token.IsCancellationRequested) return;

                    double t = (double)i / steps;
                    MotionModifier = Lerp(startValue, targetModifier, t);
                    await Task.Delay(TimeSpan.FromSeconds(timePerStep), token);
                }
            }
            catch (TaskCanceledException)
            {
                // Swallow the exception to prevent crashes when animations are interrupted
            }

            MotionModifier = targetModifier;
            if (targetValue == false) AreAnimationsPlaying = false;
        }

        private static double Lerp(double start, double end, double t)
        {
            t = t < 0.5
                ? 2 * t * t       // Ease-in (slow start)
                : 1 - Math.Pow(-2 * t + 2, 2) / 2; // Ease-out (slow end)

            return start + (end - start) * t;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum WeatherType
    {
        ClearSky,
        MainlyClear,
        PartlyCloudy,
        Overcast,
        Fog,
        DepositingRimeFog,
        DrizzleLight,
        DrizzleModerate,
        DrizzleDense,
        FreezingDrizzleLight,
        FreezingDrizzleDense,
        RainSlight,
        RainModerate,
        RainHeavy,
        FreezingRainLight,
        FreezingRainHeavy,
        SnowFallSlight,
        SnowFallModerate,
        SnowFallHeavy,
        SnowGrains,
        RainShowersSlight,
        RainShowersModerate,
        RainShowersViolent,
        SnowShowersSlight,
        SnowShowersHeavy,
        ThunderstormSlightOrModerate,
        ThunderstormWithHailSlight,
        ThunderstormWithHailHeavy
    }
}
