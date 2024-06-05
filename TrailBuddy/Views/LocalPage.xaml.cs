namespace TrailBuddy.Views;

using System.ComponentModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TrailBuddy.ViewModels;

public partial class LocalPage : ContentPage

{
    private LocalViewModel _viewModel;
    private Polyline polyline;

    public LocalPage()
    {
        InitializeComponent();
        BindingContext = new LocalViewModel(Navigation);

        _viewModel = new LocalViewModel(Navigation);
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        polyline = new Polyline
        {
            StrokeColor = Colors.Red,
            StrokeWidth = 5
        };

    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalViewModel.CurrentLocation))
        {
            CenterMapOnLocation(_viewModel.CurrentLocation);
        }
        else if (e.PropertyName == nameof(LocalViewModel.BackgroundLocation))
        {
            // If the model is running, do stuff
            if (!_viewModel.IsPaused)
            {
                // Every 5 seconds, update map with route
                List<CoordData> cd = DB.conn.Table<CoordData>().Where(x => x.PlaceId == _viewModel.SearchId).ToList();

                if (cd.Count > 0)
                    polyline.Geopath.Add(new Location(cd.Last().Latitude, cd.Last().Longitude));

                CenterMapOnLocation(_viewModel.BackgroundLocation);

                if (displayMap.MapElements.Count > 0)
                    displayMap.MapElements.RemoveAt(0);

                displayMap.MapElements.Add(polyline);
                //polyline.Clear();            }

            }
        }
        else if(e.PropertyName == nameof(LocalViewModel.IsRunning))
        {
            // If it is not running, and its not paused, clear it
            if (!_viewModel.IsRunning && !_viewModel.IsPaused)
            {
                if (polyline.Count > 0)
                    polyline.Geopath.Clear();

                displayMap.MapElements.Clear();
            }
        }
        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // refresh the current location
        await _viewModel.UpdateCurrentLocation();
        _viewModel.UpdateDistanceAbv();

        // when page is loaded, center location and populate pins
        CenterMapOnLocation(_viewModel.CurrentLocation);
    }

    private void CenterMapOnLocation(Location location, double zoom = -1.0)
    {
        if (location != null)
        {
            var mapSpan = MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(.3)
            );

            displayMap.MoveToRegion(mapSpan);

        }
    }
}
