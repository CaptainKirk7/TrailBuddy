namespace TrailBuddy.Views;
using TrailBuddy.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

public partial class ModalPage : ContentPage
{
	LocalTrails _trails;
	Polyline polyline;
    List<CoordData> _data;

    public ModalPage(LocalTrails lc, string title)
	{
		InitializeComponent();
		BindingContext = new ModalViewModel(Navigation, lc, title);
		_trails = lc;
        _data = DB.conn.Table<CoordData>().Where(x => x.PlaceId == _trails.PlaceId).ToList();

        polyline = new Polyline
        {
            StrokeColor = Colors.Red,
            StrokeWidth = 5
        };

        
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_data.Count > 0)
        {
            CenterMapOnLocation(new Location(_data[0].Latitude, _data[0].Longitude));

            // Get coords for polyline
            foreach (var coord in _data)
            {
                polyline.Geopath.Add(new Location(coord.Latitude, coord.Longitude));
            }

            trailMap.MapElements.Add(polyline);
        }
    }

    private void CenterMapOnLocation(Location location)
    {
        if (location != null)
        {
            var mapSpan = MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(.3)
            );

            trailMap.MoveToRegion(mapSpan);

        }
    }
}
