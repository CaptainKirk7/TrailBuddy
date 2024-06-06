using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class HomeViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private INavigation _navigation;
    private Location currentLocation;
    private Location centerLocation;
    private ObservableCollection<Position> positions;
    List<Place> places;
    private double zoomLevel;
    private double screenWidth;
    private bool isShowingUser = true;
    private bool isFavorited = false;
    private Place selectedItem = null;

    // kilometers (5000 meters)
    private int distThreshold = 5;
    private int startupNum = 0;

    TrailPage trailPage;
    PreferencePage preferencePage;
    ApiClient _client = null;
    Favorites _helper = null;
    NetworkAccess _network;

    public HomeViewModel(INavigation navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _client = new ApiClient();
        _helper = new Favorites();
        ScreenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 30;
        _network = Connectivity.Current.NetworkAccess;

        // Command init -----------------------------------------------------
        SearchCommand = new Command<string>(
            execute: async (string locationQuery) =>
            {
                _network = Connectivity.Current.NetworkAccess;

                if (_network == NetworkAccess.Internet)
                {
                    // Create api client & search
                    var result = await _client.SearchTrails(locationQuery);

                    trailPage = new TrailPage(result, "Results");
                    await _navigation.PushAsync(trailPage);
                } else
                {
                    DisplayPopup();
                }
            }
        );

        PreferenceCommand = new Command(
            execute: async () =>
            {
                preferencePage = new PreferencePage(_navigation);
                await _navigation.PushModalAsync(preferencePage);
            }
        );

        CenterOnUser = new Command(
            execute: async () =>
            {
                PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (locAlways == PermissionStatus.Granted || locInUse == PermissionStatus.Granted)
                {
                    await CenterOnUserLocation();
                    await PopulatePins(CurrentLocation);
                }
            }
        );

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
                            await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot find nearby trails.", "Cancel");
                        }
                    }
                    else
                    {
                        DisplayPopup();
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
                        DisplayPopup();
                    }

                if (name.Equals("local"))
                    Shell.Current.GoToAsync($"//{name}");
            }
        );

        FavoriteCommand = new Command<Place>(ToggleFavorite);
        MapCommand = new Command<Place>(OpenMap);
    }

    // Getters & Setters -----------------------------------------------------
    public Location CenterLocation
    {
        private set
        {
            if (centerLocation != value)
            {
                centerLocation = value;
                NotifyPropertyChanged(nameof(CenterLocation));

            }
        }
        get
        {
            return centerLocation;
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

    public Place SelectedItem
    {
        private set
        {
            if (selectedItem != null)
            {
                selectedItem = value;
                NotifyPropertyChanged(nameof(SelectedItem));
            }
        }
        get
        {
            return selectedItem;
        }
    }

    public bool IsFavorited
    {
        private set
        {
            if (isFavorited != value)
            {
                isFavorited = value;
                NotifyPropertyChanged(nameof(IsFavorited));
            }
        }
        get
        {
            return isFavorited;
        }
    }

    public List<Place> Places
    {
        private set
        {
            if (places != value)
            {
                places = value;
                NotifyPropertyChanged(nameof(Places));
            }
        }
        get
        {
            return places;
        }
    }

    public double ZoomLevel
    {
        private set
        {
            if (zoomLevel != value)
            {
                zoomLevel = value;
                NotifyPropertyChanged(nameof(ZoomLevel));
            }
        }
        get
        {
            return zoomLevel;
        }
    }

    public ObservableCollection<Position> Positions
    {
        private set
        {
            if (positions != value)
            {
                positions = value;
                NotifyPropertyChanged(nameof(Positions));
            }
        }
        get
        {
            return positions;
        }
    }

    public Location CurrentLocation
    {
        set
        {
            // I did not include a if statement here because in the view.cs
            // I subscribed to the property changed event, so I always want it to change
            // so you can use the center location button on the map.
            currentLocation = value;
            NotifyPropertyChanged(nameof(CurrentLocation));
        }
        get
        {
            return currentLocation;
        }
    }

    public bool IsShowingUser
    {
        set
        {
            if (isShowingUser != value)
            {
                isShowingUser = value;
                NotifyPropertyChanged(nameof(IsShowingUser));
            }
        }
        get
        {
            return isShowingUser;
        }
    }

    // Helper methods -----------------------------------------------------
    private void ToggleFavorite(Place p)
    {
        // Swap image source of favorite icon & add/remove from DB
        if (p.IsFavorited)
        {
            p.IsFavorited = !p.IsFavorited;
            p.FavoriteSource = ImageSource.FromFile("unfavorite.png");
            _helper.RemoveFromFavorites(p);
        }
        else
        {
            p.IsFavorited = !p.IsFavorited;
            p.FavoriteSource = ImageSource.FromFile("favorites.png");
            _helper.AddToFavorites(p);
        }

        // save
        _helper.SaveFavorite(p.Id, p.IsFavorited);
    }

    public async Task PopulateData(Location location)
    {
        // TODO: Ensure device has connection. If not, display a popup
        if (_network == NetworkAccess.Internet)
        {
            // Connection to internet is available
            // Sets the address on initial startup
            Preferences.Set("currentLocation", await _client.GetCurrentAddress(location));

            await PopulateTopTrails(location);
            await PopulatePins(location);
        } else
        {
            await DisplayPopup();
        }

    }

    public async Task DisplayPopup()
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network != NetworkAccess.Internet)
        {
            bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
            if (answer)
            {
                await PopulateData(CurrentLocation);
            }
        }
    }


    public async Task PopulateTopTrails(Location loc)
    {
        if (_network == NetworkAccess.Internet)
        {
            var places = await _client.GetPlaces(loc);
            Places = (from p in places
                      orderby p.UserRatingCount descending, p.Rating descending
                      select p).ToList();

            //List<Place> placeList = (from p in places
            //                         orderby p.UserRatingCount descending, p.Rating descending
            //                         select p).ToList();

            SetupPlaces();
        }
    }

    private async void SetupPlaces()
    {
        foreach (var place in Places)
        {
            // For each element, we set the image source
            var photoName = place.Photos?.First()?.Name;
            if (!string.IsNullOrEmpty(photoName))
            {
                var name = place.Photos[0].Name;
                var uri = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=1200&maxheight=1000&photo_reference={name}&key={Constants.MapsAPIKey}";

                // Samve uri to device
                place.Source = await GetImageFromCacheOrDownloadAsync(uri, photoName);

                //place.Source = ImageSource.FromUri(new Uri(uri));
                // await GetImageFromCacheOrDownloadAsync(uri);
            }
            else
            {
                place.Source = ImageSource.FromFile("placeholder.png");
            }

            // fetch & set favorite status & image
            place.IsFavorited = Preferences.Get(place.Id, false);
            place.FavoriteSource = ImageSource.FromFile(place.IsFavorited ? "favorites.png" : "unfavorite.png");
        }

        //Places = placeList;
    }

    public async Task PopulatePins(Location loc)
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network == NetworkAccess.Internet)
            Positions = await _client.PopulatePins(loc);
    }

    public async Task UpdateCurrentLocation()
    {
        // Get cached location
        var cachedLocation = await Geolocation.GetLastKnownLocationAsync();

        if (cachedLocation != null && CurrentLocation != null)
        {
            var dist = Location.CalculateDistance(cachedLocation, CurrentLocation, DistanceUnits.Kilometers);
            if (dist > distThreshold)
            {
                // Significant change (5 km) so recalculate and refresh trails
                var request = new GeolocationRequest(GeolocationAccuracy.Best);
                CurrentLocation = await Geolocation.GetLocationAsync(request);

                Preferences.Set("refreshTopTrails", true);
            }
        } else
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best);
            CurrentLocation = await Geolocation.GetLocationAsync(request);
        }

        // If first time startup, refresh top trails
        if (startupNum == 0)
        {
            Preferences.Set("refreshTopTrails", true);
            startupNum++;
        }
    }

    public void UpdateFavoriteImages()
    {
        IEnumerable<Place> places = Places;

        foreach (Place place in places)
        {
            place.IsFavorited = Preferences.Get(place.Id, false);
            place.FavoriteSource = ImageSource.FromFile(place.IsFavorited ? "favorites.png" : "unfavorite.png");
        }

        Places = places.ToList();
    }

    public async Task CenterOnUserLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        CenterLocation = await Geolocation.GetLocationAsync(request);
    }

    public async Task<Location> CurrentLoc()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        var ret = await Geolocation.GetLocationAsync(request);
        return ret;
    }

    public async void OpenMap(Place p)
    {
        string currentLocation = Preferences.Get("currentLocation", "");

        if (DeviceInfo.Current.Platform == DevicePlatform.iOS || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
        {
            // https://developer.apple.com/library/ios/featuredarticles/iPhoneURLScheme_Reference/MapLinks/MapLinks.html
            await Launcher.OpenAsync($"http://maps.apple.com/?daddr={p.Geometry.Location.Latitude} {p.Geometry.Location.Longitude}&saddr={currentLocation}");
        }
        else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            // opens the 'task chooser' so the user can pick Maps, Chrome or other mapping app
            await Launcher.OpenAsync($"http://maps.google.com/?daddr={p.Geometry.Location.Latitude} {p.Geometry.Location.Longitude}&saddr={currentLocation}");
        }
    }

    private async Task<ImageSource> GetImageFromCacheOrDownloadAsync(string uri, string photoName)
    {
        var cacheDir = FileSystem.CacheDirectory;
        var filePath = Path.Combine(cacheDir, $"{photoName}.jpg");

        if (File.Exists(filePath))
        {
            return ImageSource.FromFile(filePath);
        }
        else
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        var imageBytes = await response.Content.ReadAsByteArrayAsync();
                        File.WriteAllBytes(filePath, imageBytes);
                        return ImageSource.FromFile(filePath);
                    }
                }
            } catch
            {
                return ImageSource.FromFile("placeholder.png");
            }

        }

        // return placeholder if all else fails
        return null;
    }


    // Base methods -----------------------------------------------------
    public ICommand SearchCommand { private set; get; }
    public ICommand PreferenceCommand { private set; get; }
    public ICommand CenterOnUser { private set; get; }
    public ICommand FavoriteCommand { private set; get; }
    public ICommand NavCommand { private set; get; }
    public ICommand MapCommand { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
