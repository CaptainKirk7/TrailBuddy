using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrailBuddy;

public class ApiClient
{
	HttpClient _client;
    Location currentLocation;

	public ApiClient()
	{
		_client = new HttpClient();
    }

    public async Task SendFeedback(string feedback = "", string email = "")
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dinolips.xyz/db/trailbuddy/feedback");
        var content = new StringContent($"{{\n    \"feedback\": \"{feedback}\",\n    \"email\": \"{email}\"\n}}", null, "application/json");
        request.Content = content;

        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Debug.WriteLine(await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    public async Task<TrailData> SearchTrails(string query, string token = "")
    {
        TrailData trailData = null;
        int distance = (int)Preferences.Get("distanceSlider", 10.0);
        Preferences.Set("query", query);
        string uri = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query=hiking trails within {distance.ToString()} {Preferences.Get("distanceAbv", "mi")} of {Preferences.Get("query", "me")}&pagetoken={token}&key={Constants.MapsAPIKey}";

        try
        {
            var response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                trailData = JsonConvert.DeserializeObject<TrailData>(responseData);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return trailData;
    }

    // Method that gets trails near a specified coordinate location
    public async Task<List<Place>> GetPlaces(Location loc)
    {
        string address = await GetCurrentAddress(loc);
        var trails = await SearchTrails(address);
        List<Place> places = trails.Places.ToList();
        return places;
    }

    // Method that gets trail data of current location
    public async Task<TrailData> GetTrailData()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High);
        Location ret = await Geolocation.GetLocationAsync(request);

        string address = await GetCurrentAddress(ret);
        var trails = await SearchTrails(address);
        return trails;
    }

    // Method that retrieves the ImageSource of an image given its name
    public async Task<ImageSource> GetPhotoSource(string name)
    {
        ImageSource imageSource = null;
        string uri = $"https://places.googleapis.com/v1/{name}/media?" +
            "maxHeightPx=400" +
            "&maxWidthPx=400" +
            $"&key={Constants.MapsAPIKey}";

        try
        {
            var response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                imageSource = ImageSource.FromStream(() => stream);
            }
        } catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return imageSource;
    }

    // Method that retrieves the address for a specified coordinate location
    public async Task<string> GetCurrentAddress(Location loc)
    {
        string uri = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={loc.Latitude},{loc.Longitude}&key={Constants.MapsAPIKey}";
        var response = await _client.GetStringAsync(uri);

        JObject obj = JObject.Parse(response);
        var results = obj["results"] as JArray;

        if (results != null)
            return results[0]["formatted_address"].ToString();

        return "";
    }

    // Method that creates and returns a collection of Pins to be populated on a map
    public async Task<ObservableCollection<Position>> PopulatePins(Location loc)
    {
        // get address and search trails from it
        string address = await GetCurrentAddress(loc);
        var trails = await SearchTrails(address);
        List<Place> Places = trails.Places.ToList();
        ObservableCollection<Position> positions = new ObservableCollection<Position>();

        foreach (var trail in Places)
        {
            // For each trail, make a pin and add it to pin list.
            Position p = new Position();
            p.Location = new Location(trail.Geometry.Location.Latitude, trail.Geometry.Location.Longitude);
            p.Address = trail.Address;
            p.Description = trail.Name;

            positions.Add(p);
        }

        return positions;
    }

    // Method that retrieves the reviews of a specified place
    public async Task<ReviewData> GetReviews(string id)
    {
        ReviewData reviews = null;
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://places.googleapis.com/v1/places/{id}");
        request.Headers.Add("X-Goog-Api-Key", Constants.MapsAPIKey);
        request.Headers.Add("X-Goog-FieldMask", "reviews");

        try
        {
            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                reviews = JsonConvert.DeserializeObject<ReviewData>(responseData);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return reviews;
    }

    // Method that returns the weather info of your current location
    public async Task<WeatherData> GetWeather()
    {
        WeatherData weatherData = null;
        Location loc = await CurrentLoc();
        string uri = $"{Constants.WeatherEndpoint}lat={loc.Latitude}&lon={loc.Longitude}&units={Preferences.Get("weatherUnits", "imperial")}&appid={Constants.WeatherAPIKey}";

        try
        {
            var response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                weatherData = JsonConvert.DeserializeObject<WeatherData>(responseData);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return weatherData;
    }

    // Method that returns the distance of two points
    public async Task<object[]> GetDistance(Location currentLocation, string destination)
    {
        string uri = $"https://maps.googleapis.com/maps/api/distancematrix/json?units={Preferences.Get("distanceUnits", "imperial")}&origins={currentLocation.Latitude} {currentLocation.Longitude}&destinations={destination}&key={Constants.MapsAPIKey}";
        object[] info = new object[2];

        try
        {
            var response = await _client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();

                JObject obj = JObject.Parse(responseContent);
                JToken dist = obj["rows"][0]["elements"][0]["distance"];

                if (info != null)
                {
                    info[0] = dist["value"].ToString();
                    info[1] = dist["text"].ToString();
                }

            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            info[0] = "0.0";
            info[1] = "";
        }

        return info;
    }

    // Helper method that returns current location
    public async Task<Location> CurrentLoc()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.High);
        var ret = await Geolocation.GetLocationAsync(request);
        Preferences.Set("currentCoord", $"{ret.Latitude} {ret.Longitude}");
        return ret;
    }

}
