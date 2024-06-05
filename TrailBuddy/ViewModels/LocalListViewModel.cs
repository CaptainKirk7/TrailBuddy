using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreFoundation;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class LocalListViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private INavigation _navigation;
    private string filterText;
    private string sortBy;
    ObservableCollection<LocalTrails> trails;
    NetworkAccess _network;
    ApiClient _client;

    public LocalListViewModel(INavigation navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;
        FilterText = "Asc";

        PopulatePage();

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
                        _navigation.PushAsync(new TrailPage(await _client.GetTrailData(), "Nearby Search"), false);
                    }
                    else
                    {
                        DisplayPopup(name);
                    }
                }
                if (name.Equals("weather"))
                    if (_network == NetworkAccess.Internet)
                    {
                        await _navigation.PushAsync(new WeatherPage(await _client.GetWeather()));
                    }
                    else
                    {
                        DisplayPopup(name);
                    }

                if (name.Equals("local"))
                    Shell.Current.GoToAsync($"//{name}");
            }
        );

        DeleteCommand = new Command<LocalTrails>(
            execute: (LocalTrails local) =>
            {
                try
                {
                    DB.conn.Delete(local);
                    PopulatePage();
                } catch (Exception e) { }
            }
        );

        MapCommand = new Command<LocalTrails>(OpenMap);

        FilterSortButton = new Command(
            execute: () =>
            {
                FilterText = FilterText.Equals("Asc") ? "Desc" : "Asc";
                PopulatePage();
            }
        );

    }

    // Getters & Setters -----------------------------------------------------
    public ObservableCollection<LocalTrails> Trails
    {
        set
        {
            if (trails != value)
            {
                trails = value;
                NotifyPropertyChanged(nameof(Trails));
            }
        }
        get
        {
            return trails;
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
                PopulatePage();
                NotifyPropertyChanged(nameof(SortBy));
            }
        }
        get
        {
            return sortBy;
        }
    }

    // Helper Methods -----------------------------------------------------
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


    public void PopulatePage()
    {
        // Fetch list of LocalTrails
        ObservableCollection<LocalTrails> localTrails = new ObservableCollection<LocalTrails>(DB.conn.Table<LocalTrails>().ToList());
        foreach (var trail in localTrails)
        {
            // If an image isn't set, use the placeholder instead
            if (trail.ImgUrl == null)
                trail.ImgUrl = "placeholder.png";

            // set distance string
            string dist = Preferences.Get("distanceUnits", "imperial");

            // Converts distance to km if it is in metric
            trail.DistanceString = $"{(dist == "imperial" ? trail.DistanceTraveled.ToString("F2") : (trail.DistanceTraveled * 1.60934).ToString("F2"))} {(dist == "imperial" ? "mi" : "km")}";
        }

        IOrderedEnumerable<LocalTrails> sortedTrails;

        switch(SortBy)
        {
            case "By Date":
                sortedTrails = FilterText == "Desc"
                    ? localTrails.OrderBy(t => DateTime.Parse(t.DetailedDateAdded))
                    : localTrails.OrderByDescending(t => DateTime.Parse(t.DetailedDateAdded));
                break;
            case "By Distance":
                sortedTrails = FilterText == "Desc"
                    ? localTrails.OrderBy(t => t.DistanceTraveled)
                    : localTrails.OrderByDescending(t => t.DistanceTraveled);
                break;
            case "By Name":
                sortedTrails = FilterText == "Desc"
                    ? localTrails.OrderByDescending(t => t.Name)
                    : localTrails.OrderBy(t => t.Name);
                break;
            case "By Rating":
                sortedTrails = FilterText == "Desc"
                    ? localTrails.OrderBy(t => t.Rating)
                    : localTrails.OrderByDescending(t => t.Rating);
                break;
            default:
                sortedTrails = FilterText == "Desc"
                    ? localTrails.OrderBy(t => DateTime.Parse(t.DetailedDateAdded))
                    : localTrails.OrderByDescending(t => DateTime.Parse(t.DetailedDateAdded));
                break;
        }

        Trails = new ObservableCollection<LocalTrails>(sortedTrails);
    }

    private async void OpenMap(LocalTrails f)
    {
        string currentLocation = Preferences.Get("currentLocation", "");

        // Getting coordinates for specififed trial
        List<CoordData> data = DB.conn.Table<CoordData>().Where(x => x.PlaceId == f.PlaceId).ToList();


        if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
        {
            await Launcher.OpenAsync($"http://maps.apple.com/?daddr={data.First().Latitude} {data.First().Longitude}&saddr={currentLocation}");
        }
        else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
        {
            await Launcher.OpenAsync($"http://maps.google.com/?daddr={data.First().Latitude} {data.First().Longitude}&saddr={currentLocation}");
        }
    }

    // Base methods -----------------------------------------------------
    public ICommand FavoriteCommand { private set; get; }
    public ICommand NavCommand { private set; get; }
    public ICommand MapCommand { private set; get; }
    public ICommand DeleteCommand { private set; get; }
    public ICommand FilterSortButton { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

