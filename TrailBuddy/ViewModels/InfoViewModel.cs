using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TrailBuddy.ViewModels;

public class InfoViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    Favorites page;
    Favorites _helper;
    ApiClient _client;
    NetworkAccess _network;
    ImageSource favSource;
    List<Review> reviewSource;
    private double screenWidth;


    public InfoViewModel(Place p = null, Favorites f = null)
    {
        ScreenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 80;
        _helper = new Favorites();
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;

        PopulatePage(p, f);

        // Command init -----------------------------------------------------
        FavoriteCommand = new Command<Favorites>(ToggleFavorite);
        MapCommand = new Command<Favorites>(OpenMap);

    }

    // Getters & Setters -----------------------------------------------------
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

    public List<Review> ReviewSource
    {
        private set
        {
            if (reviewSource != value)
            {
                reviewSource = value;
                NotifyPropertyChanged(nameof(ReviewSource));
            }
        }
        get
        {
            return reviewSource;
        }
    }

    public ImageSource FavSource
    {
        private set
        {
            if (favSource != value)
            {
                favSource = value;
                NotifyPropertyChanged(nameof(FavSource));
            }
        }
        get
        {
            return favSource;
        }
    }

    public Favorites Page
    {
        private set
        {
            if (page != value)
            {
                page = value;
                NotifyPropertyChanged(nameof(Page));
            }
        }
        get
        {
            return page;
        }
    }

    // Helper Methods -----------------------------------------------------
    public async Task DisplayPopup(Place p, Favorites f)
    {
        _network = Connectivity.Current.NetworkAccess;

        bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
        if (answer)
        {
            PopulatePage(p, f);
        }
    }

    private async void OpenMap(Favorites f)
    {
        if (_network == NetworkAccess.Internet)
        {
            string currentLocation = Preferences.Get("currentLocation", "");

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
            {
                // https://developer.apple.com/library/ios/featuredarticles/iPhoneURLScheme_Reference/MapLinks/MapLinks.html
                await Launcher.OpenAsync($"http://maps.apple.com/?daddr={f.Latitude} {f.Longitude}&saddr={currentLocation}");
            }
            else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                // opens the 'task chooser' so the user can pick Maps, Chrome or other mapping app
                await Launcher.OpenAsync($"http://maps.google.com/?daddr={f.Latitude} {f.Longitude}&saddr={currentLocation}");
            }
        }
        else
        {
            DisplayPopup(null, f);
        }
    }

    private void ToggleFavorite(Place p = null)
    {
        if (_network == NetworkAccess.Internet)
        {
            if (p != null)
            {
                // Swap image source of favorite icon & add/remove from DB
                if (p.IsFavorited)
                {
                    p.IsFavorited = !p.IsFavorited;
                    FavSource = ImageSource.FromFile("unfavorite.png");
                    _helper.RemoveFromFavorites(p);
                }
                else
                {
                    p.IsFavorited = !p.IsFavorited;
                    FavSource = ImageSource.FromFile("favorites.png");
                    _helper.AddToFavorites(p);
                }

                // Change favorited state and save.
                _helper.SaveFavorite(p.Id, p.IsFavorited);
            }
        }
        else
        {
            DisplayPopup(p, null);
        }


    }

    private void ToggleFavorite(Favorites f = null)
    {
        if (_network == NetworkAccess.Internet)
        {
            if (f.IsFavorited)
            {
                // Remove
                _helper.RemoveFromFavorites(null, f);

                // Change favorited state and save.
                f.IsFavorited = !f.IsFavorited;
                FavSource = f.IsFavorited ? ImageSource.FromFile("favorites.png") :
                                            ImageSource.FromFile("unfavorite.png");
                _helper.SaveFavorite(f.Id, f.IsFavorited);
            }
            else
            {
                f.IsFavorited = !f.IsFavorited;
                FavSource = f.IsFavorited ? ImageSource.FromFile("favorites.png") :
                                                  ImageSource.FromFile("unfavorite.png");
                _helper.AddToFavorites(null, f);
                _helper.SaveFavorite(f.Id, f.IsFavorited);
            }
        }
        else
        {
            DisplayPopup(null, f);
        }


    }

    public void PopulatePage(Place p = null, Favorites f = null)
    {
        if (_network == NetworkAccess.Internet)
        {
            if (p == null)
            {
                FavSource = f.IsFavorited ? ImageSource.FromFile("favorites.png") :
                                                  ImageSource.FromFile("unfavorite.png");

            }
            else
            {
                f = _helper.CreateFavorite(p);
                FavSource = f.IsFavorited ? ImageSource.FromFile("favorites.png") :
                                                  ImageSource.FromFile("unfavorite");

            }

            if (f.Source.Equals("null"))
                f.Source = "placeholder.png";

            f.DistanceString = Preferences.Get($"{f.Id}_DistanceString", "");
            Page = f;

            GetReviews(Page.Id);
        }
        else
        {
            DisplayPopup(p, f);
        }
    }

    public async void GetReviews(string id)
    {
        ReviewData reviewData = await _client.GetReviews(id);
        if (reviewData.Reviews != null)
        {
            var data = reviewData.Reviews.ToList();
            ReviewSource = UpdateReview(data);
        }
    }

    public List<Review> UpdateReview(List<Review> data)
    {
        foreach (Review r in data)
        {
            if (r.Description == null) {
                r.Description = new Description();
                r.Description.Text = $"Rated {r.Rating} stars";
            }

            r.Author.PhotoSource = ImageSource.FromUri(new Uri(r.Author.PhotoUri));
        }

        return data;
    }

    // Base methods -----------------------------------------------------
    public ICommand FavoriteCommand { private set; get; }
    public ICommand MapCommand { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

