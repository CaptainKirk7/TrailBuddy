using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreFoundation;
using GeolocatorPlugin;
using GeolocatorPlugin.Abstractions;
using TrailBuddy.Views;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace TrailBuddy.ViewModels;

public class LocalViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private INavigation _navigation;
    NetworkAccess _network;
    private Location currentLocation;
    private Location backgroundLocation;

    // Map values
    private double zoomLevel;
    private bool isShowingUser = true;
    private double distance;
    private string heading;

    // Timer vlaues
    private string elapsedTime = "";
    private bool stopButtonPressed = false;
    private bool playButtonPressed = false;
    private bool timerRunning = false;
    private bool isRunning = false;
    private bool isPaused = false;
    private Stopwatch _stopwatch;

    // Trail data
    private string searchId;
    private string title;
    private string distanceAbv;

    ApiClient _client = null;

    public LocalViewModel(INavigation navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _client = new ApiClient();
        _stopwatch = new Stopwatch();
        _network = Connectivity.Current.NetworkAccess;
        Title = Preferences.Get("currentLocation", "");

        InitializeThings();

        // Command init -----------------------------------------------------
        CenterOnUser = new Command(
            execute: async () =>
            {
                await UpdateCurrentLocation();
            }
        );

        Start = new Command(
            execute: async () =>
            {
                PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (locAlways != PermissionStatus.Denied || locInUse != PermissionStatus.Denied)
                {
                    if (!timerRunning && !IsPaused)
                    {
                        playButtonPressed = true;
                        stopButtonPressed = false;
                        _stopwatch.Start();

                        // Create random string and set it for this trail.
                        Random random = new Random();
                        SearchId = random.Next(0, 1000000).ToString();
                        Preferences.Set("searchId", SearchId);

                        ToggleBackgroundLocation();
                        ToggleTimer();

                        RefreshCanExecutes();
                    }
                    else if (!timerRunning && IsPaused)
                    {
                        // If the timer isn't running, and is paused, just resume.
                        TogglePause();
                    }
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot record trail.", "Cancel");
                }

            }
        );

        Stop = new Command(
            execute: async () =>
            {
                PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (locAlways != PermissionStatus.Denied || locInUse != PermissionStatus.Denied)
                {
                    _stopwatch.Stop();
                    _stopwatch.Reset();
                    timerRunning = false;
                    stopButtonPressed = true;

                    // Stops background location
                    ToggleBackgroundLocation();

                    // Create trail
                    int count = DB.conn.Table<LocalTrails>().ToList().Count() + 1;

                    LocalTrails lc = new LocalTrails();
                    lc.PlaceId = SearchId;
                    lc.Name = $"Trail {count}";
                    lc.DateAdded = DateTime.Now.ToString("MM-dd-yy");
                    lc.DetailedDateAdded = DateTime.Now.ToString();
                    lc.TotalTime = ElapsedTime;
                    lc.Address = Title;
                    lc.Rating = 5;
                    lc.DistanceTraveled = Distance;
                    lc.DistanceAbv = DistanceAbv;

                    // Open modal with more info to save
                    await _navigation.PushModalAsync(new ModalPage(lc, "Save Trail"));

                    // TODO: Save current details and open new screen for more info
                    ElapsedTime = "00:00:00";
                    Distance = 0.0;
                    RefreshCanExecutes();
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot find nearby trails.", "Cancel");
                }

            },
            canExecute: () =>
            {
                return timerRunning && playButtonPressed;
            }
        );

        Pause = new Command(
            execute: async () =>
            {
                PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (locAlways != PermissionStatus.Denied || locInUse != PermissionStatus.Denied)
                {
                    TogglePause();
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Location not enabled, cannot find nearby trails.", "Cancel");
                }

            },
            canExecute: () =>
            {
                return playButtonPressed && !stopButtonPressed;
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

        TrailCommand = new Command(
            execute: async () =>
            {
                await _navigation.PushAsync(new LocalListPage());
            }
        );

    }

    // Getters & Setters -----------------------------------------------------
    public string DistanceAbv
    {
        private set
        {
            if (distanceAbv != value)
            {
                distanceAbv = value;
                NotifyPropertyChanged(nameof(DistanceAbv));
            }
        }
        get
        {
            return distanceAbv;
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

    public bool IsRunning
    {
        private set
        {
            if (isRunning != value)
            {
                isRunning = value;
                NotifyPropertyChanged(nameof(IsRunning));
            }
        }
        get
        {
            return isRunning;
        }
    }

    public bool IsPaused
    {
        private set
        {
            if (isPaused != value)
            {
                isPaused = value;
                NotifyPropertyChanged(nameof(IsPaused));
            }
        }
        get
        {
            return isPaused;
        }
    }

    public string Heading
    {
        private set
        {
            if (heading != value)
            {
                heading = value;
                NotifyPropertyChanged(nameof(Heading));
            }
        }
        get
        {
            return heading;
        }
    }

    public double Distance
    {
        private set
        {
            if (distance != value)
            {
                distance = value;
                NotifyPropertyChanged(nameof(Distance));
            }
        }
        get
        {
            return distance;
        }
    }

    public string SearchId
    {
        private set
        {
            if (searchId != value)
            {
                searchId = value;
                NotifyPropertyChanged(nameof(SearchId));
            }
        }
        get
        {
            return searchId;
        }
    }

    public string ElapsedTime
    {
        private set
        {
            if (elapsedTime != value)
            {
                elapsedTime = value;
                NotifyPropertyChanged(nameof(ElapsedTime));
            }
        }
        get
        {
            return elapsedTime;
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

    public Location BackgroundLocation
    {
        set
        {
            if (backgroundLocation != value)
            {
                backgroundLocation = value;
                NotifyPropertyChanged(nameof(BackgroundLocation));
            }
        }
        get
        {
            return backgroundLocation;
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
    public void TogglePause()
    {
        if (timerRunning)
        {
            IsPaused = true;

            // If on, turn off 
            timerRunning = false;
            _stopwatch.Stop();
        }
        else
        {
            IsPaused = false;

            // timerRunning = false, so toggle on
            ToggleTimer();
            timerRunning = true;

            _stopwatch.Start();
        }

        // Toggle Background location off/on
        ToggleBackgroundLocation();
    }


    public void ToggleTimer()
    {
        if (!timerRunning)
        {
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                ElapsedTime = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                TimeSpan elapsed = TimeSpan.Parse(ElapsedTime);
                int totalSeconds = (int)elapsed.TotalSeconds;

                return timerRunning;
            });
            timerRunning = true;
        }
    }

    public async Task ToggleBackgroundLocation()
    {
        BasePermission gpsPermission = new LocationWhenInUse();

        if (IsRunning)
        {
            if (await CrossGeolocator.Current.StopListeningAsync())
            {
                CrossGeolocator.Current.PositionChanged -= CrossGeolocator_Current_PositionChanged;
            }

            IsRunning = false;
        }
        else
        {
            CrossGeolocator.Current.DesiredAccuracy = 10;

            if (await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5.0), 7, true, new ListenerSettings
            {
                ActivityType = ActivityType.Fitness,
                AllowBackgroundUpdates = true,
                DeferLocationUpdates = false,
                DeferralDistanceMeters = 10,
                DeferralTime = TimeSpan.FromSeconds(10),
                ListenForSignificantChanges = false,
                PauseLocationUpdatesAutomatically = true,
                ShowsBackgroundLocationIndicator = true,
                
            }))
            {
                CrossGeolocator.Current.PositionChanged += CrossGeolocator_Current_PositionChanged;
            }

            IsRunning = true;
        }

        // Should be accurate within 10 meters

    }

    private void CrossGeolocator_Current_PositionChanged(object sender, PositionEventArgs e)
    {
        // New location every 5 seconds.

        Location location = new Location(e.Position.Latitude, e.Position.Longitude, e.Position.Altitude);
        BackgroundLocation = location;
        SaveLocation(location, e.Position.Heading);

        IsRunning = true;
    }

    public void InitializeThings()
    {
        ElapsedTime = "00:00:00";
        Distance = 0.0;
        DistanceAbv = Preferences.Get("distanceUnits", "imperial") == "imperial" ? "mi" : "km";
    }

    public async Task SaveLocation(Location location, double heading)
    {
        // Inserting into db
        CoordData cd = new CoordData();
        cd.PlaceId = SearchId;
        cd.Latitude = location.Latitude;
        cd.Longitude = location.Longitude;
        DB.conn.Insert(cd);

        // Updating Distance
        List<CoordData> distance = DB.conn.Table<CoordData>().Where(x => x.PlaceId == SearchId).ToList();
        DistanceUnits du = DistanceUnits.Miles;

        if (distance.Count > 1)
        {
            var lastCoord = distance.ElementAt(distance.Count - 2);
            double distanceCalc = Location.CalculateDistance(new Location(lastCoord.Latitude, lastCoord.Longitude), location, du);

            Distance += distanceCalc;
            Heading = GetHeading(heading);
        }

    }

    public string GetHeading(double heading)
    {
        switch (heading)
        {
            case >= 337 or < 22:
                return "North";
            case >= 22 and < 67:
                return "Northeast";
            case >= 67 and < 112:
                return "East";
            case >= 112 and < 157:
                return "Southeast";
            case >= 157 and < 202:
                return "South";
            case >= 202 and < 247:
                return "Southwest";
            case >= 247 and < 292:
                return "West";
            case >= 292 and < 337:
                return "Northwest";
            default:
                return "";
        }
    }

    void RefreshCanExecutes() {
		((Command)Start).ChangeCanExecute();
		((Command)Stop).ChangeCanExecute();
		((Command)Pause).ChangeCanExecute();
    }

    public async Task UpdateCurrentLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        CurrentLocation = await Geolocation.GetLocationAsync(request);
    }

    public async Task<Location> CurrentLoc()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        var ret = await Geolocation.GetLocationAsync(request);
        return ret;
    }

    public void UpdateDistanceAbv()
    {
        DistanceAbv = Preferences.Get("distanceUnits", "imperial") == "imperial" ? "mi" : "km";
    }

    // Base methods -----------------------------------------------------
    public ICommand CenterOnUser { private set; get; }
    public ICommand NavCommand { private set; get; }
    public ICommand Start { private set; get; }
    public ICommand Stop { private set; get; }
    public ICommand Pause { private set; get; }
    public ICommand TrailCommand { private set; get; }


    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
