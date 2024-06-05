namespace TrailBuddy.Views;
using TrailBuddy.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

public partial class FullscreenPage : ContentPage
{
    LocalTrails _trails;
    Polyline polyline;
    List<CoordData> _data;

    public FullscreenPage(string mapId)
    {
        InitializeComponent();
        _data = DB.conn.Table<CoordData>().Where(x => x.PlaceId == mapId).ToList();

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

    async void DoneButton_Clicked(System.Object sender, System.EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
