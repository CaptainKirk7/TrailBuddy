using System.ComponentModel;
using CoreFoundation;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TrailBuddy.ViewModels;

namespace TrailBuddy.Views;

public partial class HomePage : ContentPage
{
    private HomeViewModel _viewModel;
    NetworkAccess _network;
    int startupNum = 0;

    public HomePage()
    {
        InitializeComponent();
        _network = Connectivity.Current.NetworkAccess;

        // Creating viewmodel with navigation element &
        // Binding it to propertychanged so I can update non-bindable
        // map properties while still using view model
        _viewModel = new HomeViewModel(Navigation);
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _network = Connectivity.Current.NetworkAccess;
        PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (locAlways == PermissionStatus.Unknown || locInUse == PermissionStatus.Unknown)
        {
            await Permissions.RequestAsync<Permissions.LocationAlways>();
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        }

        if (locAlways == PermissionStatus.Granted || locInUse == PermissionStatus.Granted)
        {
            PopulateData();
        } else
        {
            DisplayPermissionPopup();
        }
    }

    public async void PopulateData()
    {
        PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        // refresh the current location
        if (locAlways == PermissionStatus.Granted || locInUse == PermissionStatus.Granted)
        {
            await _viewModel.UpdateCurrentLocation();

            // when page is loaded, center location and populate pins
            CenterMapOnLocation(_viewModel.CurrentLocation);

            if (_network == NetworkAccess.Internet)
            {
                // By default, gets set to false when preference page is loaded, so this activates on first run.
                if (Preferences.Get("refreshTopTrails", true))
                {
                    await _viewModel.PopulateData(_viewModel.CurrentLocation);
                    Preferences.Set("refreshTopTrails", false);
                }

                // Refresh favorites
                _viewModel.UpdateFavoriteImages();
            }
            else
            {
                DisplayPopup();
            }
        }

    }

    public async Task DisplayPermissionPopup()
    {
        PermissionStatus locAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        PermissionStatus locInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (locAlways == PermissionStatus.Denied || locInUse == PermissionStatus.Denied)
        {
            if (startupNum < 1)
            {
                bool answer = await App.Current.MainPage.DisplayAlert("Error", "Location not enabled. We could not find trails near you. Please use the search bar to look for trails. You may still search for trails.", "Retry", "Continue");
                if (answer)
                {
                    DisplayPermissionPopup();
                }
            } else
            {
                return;
            }

            startupNum++; 

        } else
        {
            PopulateData();
        }
    }
    public async Task DisplayPopup()
    {
        _network = Connectivity.Current.NetworkAccess;

        bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
        if (answer)
        {
            await _viewModel.PopulateData(_viewModel.CurrentLocation);
        }
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.CurrentLocation))
        {
            // If your location changes, it will center the map on your location
            CenterMapOnLocation(_viewModel.CurrentLocation);
        }

        if (e.PropertyName == nameof(HomeViewModel.CenterLocation))
        {
            CenterMapOnLocation(_viewModel.CenterLocation);
        }
    }

    private void CenterMapOnLocation(Location location, double zoom = -1.0)
    {
        // if zoom is not provided, get default
        if (zoom < 0)
            zoom = Preferences.Get("zoomValue", 2.5);

        if (location != null)
        {
            var mapSpan = MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(zoom)
            );

            displayMap.MoveToRegion(mapSpan);

        }
    }

    void trailCollectionView_SelectionChanged(System.Object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
        {
            Place place = e.CurrentSelection[0] as Place;
            CollectionView cw = sender as CollectionView;
            cw.SelectedItem = null;
            Navigation.PushAsync(new InfoPage(place, null));
        }
    }

    async void Pin_MarkerClicked(System.Object sender, Microsoft.Maui.Controls.Maps.PinClickedEventArgs e)
    {
        if (sender is Pin pin)
        {
            Place p = new Place();
            p.Geometry = new Geometry();
            p.Geometry.Location = new Coords();
            p.Geometry.Location.Latitude = pin.Location.Latitude;
            p.Geometry.Location.Longitude = pin.Location.Longitude;

            _viewModel.OpenMap(p);
        }
    }
}
