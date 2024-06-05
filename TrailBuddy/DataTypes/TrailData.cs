using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;

namespace TrailBuddy;

public class TrailData : INotifyPropertyChanged
{
    [JsonProperty("next_page_token")]
    public string Token { get; set; }

    [JsonProperty("results")]
    public Place[] Places { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}

public class Place : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    private ImageSource source;
    private ImageSource favoriteSource;
    private bool isFavorited;

    [JsonProperty("place_id")]
    public string Id { get; set; }

    [JsonProperty("formatted_address")]
    public string Address { get; set; }

    public Geometry Geometry { get; set; }
    public double Rating { get; set; }

    [JsonProperty("user_ratings_total")]
    public int UserRatingCount { get; set; }

    public string Name { get; set; }
    public Photo[] Photos { get; set; }

    [JsonIgnore]
    public double Distance { get; set; }
    [JsonIgnore]
    public string DistanceString { get; set; }

    [JsonIgnore]
    public ImageSource Source
    {
        set
        {
            if (source != value)
            {
                source = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
            }
        }
        get
        {
            return source;
        }
    }

    [JsonIgnore]
    public ImageSource FavoriteSource
    {
        set
        {
            if (favoriteSource != value)
            {
                favoriteSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavoriteSource)));
            }
        }
        get
        {
            return favoriteSource;
        }
    }

    [JsonIgnore]
    public bool IsFavorited
    {
        set
        {
            if (isFavorited != value)
            {
                isFavorited = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorited)));
            }
        }
        get
        {
            return isFavorited;
        }
    }
}

public class Geometry
{
    public Coords Location { get; set; }
}

public class Coords
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }

    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Name
{
    public string Text { get; set; }
}

public class Photo
{
    [JsonProperty("photo_reference")]
    public string Name { get; set; }
}


