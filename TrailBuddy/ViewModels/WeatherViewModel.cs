using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class WeatherViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private INavigation _navigation;
    ApiClient _client;
    NetworkAccess _network;
    WeatherData weather;
    private WeatherData _weather;
    ImageSource imageSource;
    private double screenWidth;
    private string degreeString;
    private string title;


    public WeatherViewModel(INavigation navigation, WeatherData weatherData)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        ScreenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 80;
        _weather = new WeatherData();
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;

        Title = Preferences.Get("currentLocation", "");
        PopulatePage(weatherData);

        // Command init -----------------------------------------------------
        NavCommand = new Command<string>(
            execute: async (string name) =>
            {
                _network = Connectivity.Current.NetworkAccess;

                if (name.Equals("favorites"))
                    _navigation.PushAsync(new FavoritesPage());
                if (name.Equals("home"))
                    Shell.Current.GoToAsync($"//{name}");
                if (name.Equals("quick"))
                {
                    if (_network == NetworkAccess.Internet)
                    {
                        PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                        PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                        if (locAlways != PermissionStatus.Denied || locInUse != PermissionStatus.Denied)
                        {
                            await navigation.PushAsync(new TrailPage(await _client.GetTrailData(), "Nearby Search"), false);
                        }
                        else
                        {
                            await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot retrieve weather.", "Cancel");
                        }
                    }
                    else
                    {
                        DisplayPopup(null);
                    }
                }
                if (name.Equals("weather"))
                    if (_network == NetworkAccess.Internet)
                    {
                        PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                        PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                        if (locAlways != PermissionStatus.Denied || locInUse != PermissionStatus.Denied)
                        {
                            await _navigation.PushAsync(new WeatherPage(await _client.GetWeather()));
                        }
                        else
                        {
                            await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot find nearby trails.", "Cancel");
                        }
                    }
                    else
                    {
                        DisplayPopup(null);
                    }

                if (name.Equals("local"))
                    Shell.Current.GoToAsync($"//{name}");
            }
        );
    }

    // Getters & Setters -----------------------------------------------------
    public string DegreeString
    {
        private set
        {
            if (degreeString != value)
            {
                degreeString = value;
                NotifyPropertyChanged(nameof(DegreeString));
            }
        }
        get
        {
            return degreeString;
        }
    }

    public WeatherData Weather
    {
        private set
        {
            if (weather != value)
            {
                weather = value;
                NotifyPropertyChanged(nameof(Weather));
            }
        }
        get
        {
            return weather;
        }
    }

    public double ScreenWidth
    {
        private set
        {
            if (screenWidth != value)
            {
                screenWidth = value;
                NotifyPropertyChanged(nameof(ScreenWidth));
            }
        }
        get
        {
            return screenWidth;
        }
    }

    public ImageSource ImageSource
    {
        private set
        {
            if (imageSource != value)
            {
                imageSource = value;
                NotifyPropertyChanged(nameof(ImageSource));
            }
        }
        get
        {
            return imageSource;
        }
    }

    public string Title
    {
        private set
        {
            if (title != value)
            {
                title = value;
                NotifyPropertyChanged(nameof(Title));
            }
        }
        get
        {
            return title;
        }
    }

    // Helper methods -----------------------------------------------------
    public void PopulatePage(WeatherData weatherData)
    {
        if (_network == NetworkAccess.Internet)
        {
            Weather = SetupData(weatherData);
            DegreeString = Preferences.Get("degreeString", "°F");
        }
        else
        {
            DisplayPopup(weatherData);
        }
    }

    public void RefreshWeatherData()
    {
        WeatherData dt = SetupData(Weather);
        Weather = dt;
    }

    public async Task DisplayPopup(WeatherData weatherData)
    {
        _network = Connectivity.Current.NetworkAccess;

        bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
        if (answer)
        {
            PopulatePage(weatherData);
        }
    }

    private WeatherData SetupData(WeatherData weatherData)
    {
        weatherData.WeatherIcon = ImageSource.FromUri(new Uri($"https://openweathermap.org/img/wn/{weatherData.Current.Weather[0].Icon}@4x.png"));
        weatherData.Current.Weather[0].Description = Caps(weatherData.Current.Weather[0].Description);

        // Convert each unix UTC into a datetime Object.
        for (int i = 0; i < weatherData.Hourly.Count(); i++)
        {
            weatherData.Hourly[i].DateTime = UnixTimeStampToDateTime(weatherData.Hourly[i].Time);
            weatherData.Hourly[i].WeatherIcon = ImageSource.FromUri(new Uri($"https://openweathermap.org/img/wn/{weatherData.Hourly[i].Weather[0].Icon}@4x.png"));
        }

        return weatherData; 
    }

    public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        return dateTimeOffset.ToLocalTime().DateTime;
    }

    private string Caps(string entry)
    {
        var split = entry.Split(" ");
        string result = "";

        for (int i = 0; i < split.Count(); i++)
        {
            split[i] = split[i].Substring(0, 1).ToUpper() + split[i].Substring(1);
            result += $"{split[i]} ";
        }

        return result;
    }

    public async Task<Location> CurrentLoc()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High);
        var ret = await Geolocation.GetLocationAsync(request);
        return ret;
    }


    // Base methods -----------------------------------------------------
    public ICommand NavCommand { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

