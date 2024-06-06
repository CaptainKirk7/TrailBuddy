using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreFoundation;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class FavoritesViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private INavigation _navigation;
    NetworkAccess _network;
    List<Favorites> favorites;
    Favorites _helper;
    ApiClient _client;

    public FavoritesViewModel(INavigation navigation)
	{
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _helper = new Favorites();
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;

        PopulatePage();

        // Command init -----------------------------------------------------
        NavCommand = new Command<string>(
            execute: async (string name) =>
            {
                _network = Connectivity.Current.NetworkAccess;

                if (name.Equals("favorites"))
                    PopulatePage();
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
                            await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot find nearby trails.", "Cancel");
                        }
                    }
                    else
                    {
                        DisplayPopup(name);
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
                            await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot retrieve weather.", "Cancel");
                        }

                    }
                    else
                    {
                        DisplayPopup(name);
                    }

                if (name.Equals("local"))
                    Shell.Current.GoToAsync($"//{name}");
            }
        );

        MapCommand = new Command<Favorites>(OpenMap);
        FavoriteCommand = new Command<Favorites>(ToggleFavorite);
    }

    // Getters & Setters -----------------------------------------------------
    public List<Favorites> Favorites
    {
        set
        {
            favorites = value;
            NotifyPropertyChanged(nameof(Favorites));
        }
        get
        {
            return favorites;
        }
    }

    // Helper Methods -----------------------------------------------------
    private void ToggleFavorite(Favorites f)
    {
        try
        {
            DB.conn.Delete(f);
        }
        catch (Exception e) { }

        // Change favorited state and save.
        f.IsFavorited = !f.IsFavorited;
        _helper.SaveFavorite(f.Id, f.IsFavorited);

        // Finally refresh the view by loading the page again
        PopulatePage();
        Preferences.Set("refreshTopTrails", true);
    }

    public void PopulatePage()
    {
        List<Favorites> favList = DB.conn.Table<Favorites>().ToList();
        Favorites = _helper.UpdateList(favList);
    }

    public async Task DisplayPopup(string name)
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network != NetworkAccess.Internet)
        {
            bool result = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
            if (result)
                NavCommand.Execute(name);
        }
    }


    // Opens the systems map app to specified trail
    private async void OpenMap(Favorites f)
    {
        string currentLocation = Preferences.Get("currentLocation", "");

        if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
        {
            await Launcher.OpenAsync($"http://maps.apple.com/?daddr={f.Latitude} {f.Longitude}&saddr={currentLocation}");
        }
        else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            await Launcher.OpenAsync($"http://maps.google.com/?daddr={f.Latitude} {f.Longitude}&saddr={currentLocation}");
        }
    }

    // Base methods -----------------------------------------------------
    public ICommand FavoriteCommand { private set; get; }
    public ICommand NavCommand { private set; get; }
    public ICommand MapCommand { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

