using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class TrailViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private INavigation _navigation;
    ApiClient _client;
    NetworkAccess _network;

    private string trail = "";
    private bool isFavorited = false;
    private Color favoriteColor;
    private ImageSource photoSource;
    private ObservableCollection<Place> places;
    private string title;
    private int selectedIndex;
    private string token;
    private bool nearbyBool;
    private bool canOpenPlace = false;
    private string filterText;
    private string sortBy;
    private IList<string> pickerItems;

    public TrailViewModel(TrailData data, INavigation navigation, string title = null)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;
        FilterText = "Asc";

        if (_network == NetworkAccess.Internet)
        {
            // Creating items for picker
            IList<string> items = new List<string> { "By Name", "By Rating", "By Favorited", "By Distance", };

            // Populate picker
            if (Preferences.Get("searchIncludesDistance", false) || (title != null && title.Equals("Nearby Search")))
            {
                // If the search includes the distance add it to the PickerItems
                PickerItems = items;
                SelectedIndex = 3;
                FilterText = "Desc";
            }
            else
            {
                // Remove distance if not included in search
                items.RemoveAt(items.Count - 1);
                PickerItems = items;
                SelectedIndex = 1;
            }

            // Populate page with trail data
            PopulatePage(data, title);
        }
        else
        {
            DisplayPopup(title);
        }

        

        // Command init -----------------------------------------------------
        FavoriteCommand = new Command<Place>(ToggleFavorite);

        NavCommand = new Command<string>(
            execute: async (string name) =>
            {
                _network = Connectivity.Current.NetworkAccess;

                if (name.Equals("favorites"))
                    _navigation.PushAsync(new FavoritesPage(), false);
                if (name.Equals("home"))
                    Shell.Current.GoToAsync($"//{name}");
                if (name.Equals("quick"))
                {
                    if (_network == NetworkAccess.Internet)
                    {
                        _navigation.PushAsync(new TrailPage(await _client.GetTrailData(), "Nearby Search"), false);
                    }
                    else
                    {
                        DisplayPopup(title);
                    }
                }
                if (name.Equals("weather"))
                    if (_network == NetworkAccess.Internet)
                    {
                        await _navigation.PushAsync(new WeatherPage(await _client.GetWeather()));
                    }
                    else
                    {
                        DisplayPopup(title);
                    }

                if (name.Equals("local"))
                    Shell.Current.GoToAsync($"//{name}");
            }
        );

        OpenPlace = new Command<Place?>(OpenPlaceHelper);
        MapCommand = new Command<Place>(OpenMap);
        RefreshCommand = new Command(Refresh);

        FilterSortButton = new Command(
            execute: () =>
            {
                if (_network == NetworkAccess.Internet)
                {
                    FilterText = FilterText.Equals("Asc") ? "Desc" : "Asc";
                    Places = new ObservableCollection<Place>(SortByFilter(Places.ToList()));
                } else
                {
                    DisplayPopup(title);
                }
            }
        );

    }

    // Getters & Setters -----------------------------------------------------
    public int SelectedIndex
    {
        set
        {
            if (selectedIndex != value)
            {
                selectedIndex = value;
                NotifyPropertyChanged(nameof(SelectedIndex));
            }
        }
        get
        {
            return selectedIndex;
        }
    }

    public IList<string> PickerItems
    {
        set
        {
            if (pickerItems != value)
            {
                pickerItems = value;
                NotifyPropertyChanged(nameof(PickerItems));
            }
        }
        get
        {
            return pickerItems;
        }
    }

    public string FilterText
    {
        set
        {
            if (filterText != value)
            {
                filterText = value;
                NotifyPropertyChanged(nameof(FilterText));
            }
        }
        get
        {
            return filterText;
        }
    }

    public string SortBy
    {
        set
        {
            if (sortBy != value)
            {
                sortBy = value;
                Places = new ObservableCollection<Place>(SortByFilter(Places.ToList()));
                NotifyPropertyChanged(nameof(SortBy));
            }
        }
        get
        {
            return sortBy;
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

    public Color FavoriteColor
    {
        private set
        {
            if (favoriteColor != value)
            {
                favoriteColor = value;
                NotifyPropertyChanged(nameof(FavoriteColor));
            }
        }
        get
        {
            return favoriteColor;
        }
    }

    public ObservableCollection<Place> Places
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

    public ImageSource PhotoSource
    {
        private set
        {
            if (photoSource != value)
            {
                photoSource = value;
                NotifyPropertyChanged(nameof(PhotoSource));
            }
        }
        get
        {
            return photoSource;
        }
    }

    public string Trail
    {
        private set
        {
            if (trail != value)
            {
                trail = value;
                NotifyPropertyChanged(nameof(Trail));
            }
        }
        get
        {
            return trail;
        }
    }

    // Helper methods -----------------------------------------------------
    public async Task DisplayPopup(string title)
    {
        _network = Connectivity.Current.NetworkAccess;

        bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
        if (answer)
        {
            PopulatePage(null, title);
        }
    }

    private void Refresh()
    {
        Places = null;
        Places = places;
    }

    private void OpenPlaceHelper(Place p)
    {
        if (p != null) 
            _navigation.PushAsync(new InfoPage(p, null));
    }

    public async Task LoadMore()
    {
        // During load, set status to false so it doesn't load twice &
        // make sure to refresh distance cache if 
        Preferences.Set("loadMore", false);
        canOpenPlace = false;

        var placeholders = CreatePlaceholders(1);

        // if "null" do not load anything.
        if (token != "null")
        {
            // Load placeholders to indicate loading process
            foreach (var place in placeholders)
            {
                Places.Add(place);
            }

            // Now that placeholders are active, fetch more data
            TrailData addedPlaces = await _client.SearchTrails(Preferences.Get("query", ""), token);
            string checkDistUnits = Preferences.Get($"{addedPlaces.Places[0].Id}_DistanceString", "null");
            checkDistUnits = checkDistUnits.Substring(checkDistUnits.Length - 2);

            // If the current cached distance is not the correct unit, make sure to refresh them
            // by setting the refreshDistanceUnits value
            if (checkDistUnits != Preferences.Get("distanceAbv", "mi"))
            {
                Preferences.Set("refreshDistanceUnits", true);
            }

            // Update new token
            token = addedPlaces.Token == null ? "null" : addedPlaces.Token;

            await HandleList(addedPlaces);

            Preferences.Set("loadMore", true);
        } else
        {
            Preferences.Set("loadMore", false);
        }

    }

    private async Task HandleList(TrailData data)
    {
        Places.RemoveAt(Places.Count - 1);

        List<Place> plac = await SetupPlaces(nearbyBool, data.Places.ToList());
        
        foreach (var p in plac)
        {
            Places.Add(p);
        }

        canOpenPlace = true;
    }

    private async void PopulatePage(TrailData data, string title)
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network == NetworkAccess.Internet)
        {
            // Set loadMore to true to allow  loading
            Preferences.Set("loadMore", true);

            Title = title;
            var places = data.Places.ToList();

            // Set the first update token
            token = data.Token;

            // Set placeholders
            var placeholders = CreatePlaceholders(places.Count);
            Places = new ObservableCollection<Place>(placeholders);

            if (title.Equals("Nearby Search"))
            {
                places = await SetupPlaces(true, places);
            }
            else
            {
                // Setting preferance to make sure searched trails are properly
                // cached with the correct distance units.
                places = await SetupPlaces(false, places);
            }

            Preferences.Set("refreshSearchedTrails", false);
            Places = new ObservableCollection<Place>(places); // Replace placeholders with actual data
        }
        else
        {
            DisplayPopup(title);
        }
    }

    private List<Place> CreatePlaceholders(int count)
    {
        var placeholders = new List<Place>();
        Place p = new Place();
        p.Name = "Loading ...";
        p.Source = "placeholder.png";

        for (int i = 0; i < count; i++)
        {
            placeholders.Add(p); // Use appropriate default/empty values
        }
        return placeholders;
    }

    private async Task<List<Place>> SetupPlaces(bool nearby, List<Place> places)
    {
        Location currentLocation = await _client.CurrentLoc();
        bool refreshSearchedTrails = Preferences.Get("refreshSearchedTrails", false);
        nearbyBool = nearby;
        Task<ImageSource> sc = null;

        foreach (var place in places)
        {
            // For each element, we set the image source
            var photoName = place.Photos?.First()?.Name;
            if (!string.IsNullOrEmpty(photoName))
            {
                var name = place.Photos[0].Name;
                var uri = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=1200&maxheight=1000&photo_reference={name}&key={Constants.MapsAPIKey}";

                place.Source = ImageSource.FromUri(new Uri(uri));
            } else
            {
                place.Source = ImageSource.FromFile("placeholder.png");
            }

            // fetch & set favorite status & image
            place.IsFavorited = Preferences.Get(place.Id, false);
            place.FavoriteSource = ImageSource.FromFile(place.IsFavorited ? "favorites.png" : "unfavorite.png");

            // If null, distance hasn't been set. Set it.
            // If distance is > 2 miles (~3000m) from cached location, refresh for more accurate data
            // (Preferences.Get($"{place.Id}_Distance", null) == null) || Preferences.Get("refreshDistanceUnits", false) || refreshSearchedTrails || nearby

            // Disabled by default. If true, includes distance calculations
            if (Preferences.Get("searchIncludesDistance", false) || title == "Nearby Search")
            {
                object[] info = await _client.GetDistance(currentLocation, $"{place.Geometry.Location.Latitude} {place.Geometry.Location.Longitude}");
                place.Distance = double.Parse(info[0].ToString());
                place.DistanceString = $"{info[1].ToString()} away";

                Preferences.Set($"{place.Id}_Distance", place.Distance);
                Preferences.Set($"{place.Id}_DistanceString", place.DistanceString);
            }
            else
            {
                place.Distance = Preferences.Get($"{place.Id}_Distance", 0.0);
                //place.DistanceString = Preferences.Get($"{place.Id}_DistanceString", "");
            }

        }

        Preferences.Set("refreshDistanceUnits", false);

        if (nearby)
        {

            return (from p in places
                      orderby p.Distance ascending, p.UserRatingCount descending, p.Rating descending
                      select p).ToList();
        }
        else
        {
            // Upon completion of a search, set to false to cache data.
            Preferences.Set("refreshSearchedTrails", false);

            return SortByFilter(places).ToList();

            //return (from p in places
            //        orderby p.UserRatingCount descending, p.Rating descending
            //          select p).ToList();
        }
    }

    public IEnumerable<Place> SortByFilter(List<Place> places) {
        IEnumerable<Place> sortedPlaces;

        switch (SortBy)
        {
            case "By Distance":
                sortedPlaces = FilterText == "Desc"
                    ? places.OrderBy(t => t.Distance)
                    : places.OrderByDescending(t => t.Distance);
                break;
            case "By Name":
                sortedPlaces = FilterText == "Desc"
                    ? places.OrderByDescending(t => t.Name)
                    : places.OrderBy(t => t.Name);
                break;
            case "By Rating":
                sortedPlaces = FilterText == "Desc"
                    ? places.OrderBy(t => t.Rating).ThenBy(t => t.UserRatingCount)
                    : places.OrderByDescending(t => t.Rating).ThenBy(t => t.UserRatingCount);
                break;
            case "By Favorited":
                sortedPlaces = FilterText == "Desc"
                    ? places.OrderBy(t => t.IsFavorited)
                    : places.OrderByDescending(t => t.IsFavorited);
                break;
            default:
                sortedPlaces = FilterText == "Desc"
                    ? places.OrderBy(t => t.Rating).ThenBy(t => t.UserRatingCount)
                    : places.OrderByDescending(t => t.Rating).ThenBy(t => t.UserRatingCount);
                break;
        }


        return sortedPlaces;
    }

    public void RefreshFromFilters()
    {
        IEnumerable<Place> places = SortByFilter(Places.ToList());

        foreach (Place place in places)
        {
            place.IsFavorited = Preferences.Get(place.Id, false);
            place.FavoriteSource = ImageSource.FromFile(place.IsFavorited ? "favorites.png" : "unfavorite.png");
        }

        Places = new ObservableCollection<Place>(places);
    }

    private void ToggleFavorite(Place p)
    {
        Favorites _helper = new Favorites();

        // Swap image source of favorite icon & add/remove from DB
        if (p.IsFavorited)
        {
            p.FavoriteSource = ImageSource.FromFile("unfavorite.png");
            p.IsFavorited = !p.IsFavorited;
            _helper.RemoveFromFavorites(p);
        }
        else
        {
            p.FavoriteSource = ImageSource.FromFile("favorites.png");
            p.IsFavorited = !p.IsFavorited;
            _helper.AddToFavorites(p);
        }

        _helper.SaveFavorite(p.Id, p.IsFavorited);
        Preferences.Set("refreshTopTrails", true);

        // Refresh to update favorites
        RefreshFromFilters();
    }

    private async void OpenMap(Place p)
    {
        string currentLocation = Preferences.Get("currentLocation", "");

        if (_network == NetworkAccess.Internet)
        {
            if (p != null && p.Name != "Loading ...")
            {
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
        }
        else
        {
            DisplayPopup(null);
        }


    }

    // Base methods -----------------------------------------------------
    public ICommand FavoriteCommand { private set; get; }
    public ICommand NavCommand { private set; get; }
    public ICommand OpenPlace { private set; get; }
    public ICommand MapCommand { private set; get; }
    public ICommand RefreshCommand { private set; get; }
    public ICommand FilterSortButton { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
