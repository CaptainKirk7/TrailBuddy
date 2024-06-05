using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mopups.Services;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class PreferenceViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private INavigation _navigation;
    NetworkAccess _network;
    LocalViewModel _localModel;

    // Default Preferences
    private string distanceUnits;
    private bool distanceSwitch;
    private string weatherUnits;
    private bool weatherSwitch;
    private string distanceAbv;
    private bool searchIncludesDistance;

    private double zoom = -1.0;
    private double distanceSlider = 10.0;


    public PreferenceViewModel(INavigation navigation)
	{
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _localModel = new LocalViewModel(_navigation);
        _network = Connectivity.Current.NetworkAccess;
        SetDefaultPreferences();

        // Command init -----------------------------------------------------
        Exit = new Command(
            execute: async () =>
            {
                await _navigation.PopModalAsync();
            }
        );

        Feedback = new Command(
            execute: async () =>
            {
                _network = Connectivity.Current.NetworkAccess;

                if (_network == NetworkAccess.Internet)
                {
                    await MopupService.Instance.PushAsync(new FeedbackPopup());
                } else
                {
                    DisplayPopup();
                }
            }
        );

	}

    // Getters & Setters -----------------------------------------------------
    public bool SearchIncludesDistanceSwitch
    {
        set
        {
            if (searchIncludesDistance != value)
            {
                searchIncludesDistance = value;

                // Update preferences
                Preferences.Set("searchIncludesDistance", value);

                NotifyPropertyChanged(nameof(SearchIncludesDistanceSwitch));
            }
        }
        get
        {
            return searchIncludesDistance;
        }
    }

    public string DistanceAbv
    {
        private set
        {
            if (distanceAbv != value)
            {
                distanceAbv = value;
                Preferences.Set("distanceAbv", value);
                NotifyPropertyChanged(nameof(DistanceAbv));
            }
        }
        get
        {
            return distanceAbv;
        }
    }

    public bool DistanceSwitch
    {
        set
        {
            if (distanceSwitch != value)
            {
                distanceSwitch = value;
                // Saves switch state
                Preferences.Set("distanceUnitSwitch", value);

                // Sets two bool values to handle refreshing preference cache
                Preferences.Set("refreshDistanceUnits", true);
                Preferences.Set("refreshSearchedTrails", true);

                if (!distanceSwitch)
                {
                    Preferences.Set("distanceUnits", "imperial");
                    DistanceAbv = "miles";
                    DistanceUnits = "imperial";
                }
                else
                {
                    Preferences.Set("distanceUnits", "metric");
                    DistanceAbv = "kilometers";
                    DistanceUnits = "metric";
                }

                _localModel.UpdateDistanceAbv();
                NotifyPropertyChanged(nameof(DistanceSwitch));
            }
        }
        get
        {
            return distanceSwitch;
        }
    }

    public bool WeatherSwitch
    {
        set
        {
            if (weatherSwitch != value)
            {
                weatherSwitch = value;
                Preferences.Set("weatherUnitSwitch", value);

                if (!weatherSwitch)
                {
                    Preferences.Set("weatherUnits", "imperial");
                    Preferences.Set("degreeString", "°F");
                    WeatherUnits = "imperial";
                } else
                {
                    Preferences.Set("weatherUnits", "metric");
                    Preferences.Set("degreeString", "°C");
                    WeatherUnits = "metric";
                }

                NotifyPropertyChanged(nameof(WeatherSwitch));
            }
        }
        get
        {
            return weatherSwitch;
        }
    }

    public string WeatherUnits
    {
        set
        {
            if (weatherUnits != value)
            {
                weatherUnits = value;
                NotifyPropertyChanged(nameof(WeatherUnits));
            }
        }
        get
        {
            return weatherUnits;
        }
    }

    public string DistanceUnits
    {
        set
        {
            if (distanceUnits != value)
            {
                distanceUnits = value;
                NotifyPropertyChanged(nameof(DistanceUnits));
            }
        }
        get
        {
            return distanceUnits;
        }
    }

    public double Zoom
    {
        set
        {
            if (zoom != value)
            {
                zoom = value;
                Preferences.Set("zoomValue", value / 2);
                NotifyPropertyChanged(nameof(Zoom));
            }
        }
        get
        {
            return zoom;
        }
    }

    public double DistanceSlider
    {
        set
        {
            if (distanceSlider != value)
            {
                distanceSlider = value;
                Preferences.Set("distanceSlider", value);
                NotifyPropertyChanged(nameof(DistanceSlider));
            }
        }
        get
        {
            return distanceSlider;
        }
    }

    // Helper methods -----------------------------------------------------
    public async Task DisplayPopup()
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network != NetworkAccess.Internet)
        {
            await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "OK");
        }
    }

    public void SetDefaultPreferences()
    {
        if (!Preferences.ContainsKey("distanceSlider")) Preferences.Set("distanceSlider", 10.0);
        if (!Preferences.ContainsKey("zoomValue")) Preferences.Set("zoomValue", 2.5);
        if (!Preferences.ContainsKey("refreshTopTrails")) Preferences.Set("refreshTopTrails", false);
        if (!Preferences.ContainsKey("weatherUnits")) Preferences.Set("weatherUnits", "imperial");
        if (!Preferences.ContainsKey("distanceUnits")) Preferences.Set("distanceUnits", "imperial");
        if (!Preferences.ContainsKey("weatherUnitSwitch")) Preferences.Set("weatherUnitSwitch", false);
        if (!Preferences.ContainsKey("distanceUnitSwitch")) Preferences.Set("distanceUnitSwitch", false);
        if (!Preferences.ContainsKey("distanceAbv")) Preferences.Set("distanceAbv", "mi");
        if (!Preferences.ContainsKey("searchIncludesDistance")) Preferences.Set("searchIncludesDistance", false);

        InstantiatePreferenceValues();
    }

    public void InstantiatePreferenceValues()
    {
        Zoom = Preferences.Get("zoomValue", 2.5) * 2;
        DistanceSlider = Preferences.Get("distanceSlider", 10.0);
        WeatherUnits = Preferences.Get("weatherUnits", "imperial");
        DistanceUnits = Preferences.Get("distanceUnits", "imperial");
        WeatherSwitch = Preferences.Get("weatherUnitSwitch", false);
        DistanceSwitch = Preferences.Get("distanceUnitSwitch", false);
        DistanceAbv = Preferences.Get("distanceAbv", "mi");
        SearchIncludesDistanceSwitch = Preferences.Get("searchIncludesDistance", false);
    }


    // Base methods -----------------------------------------------------
    public ICommand Exit { private set; get; }
    public ICommand Feedback { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
