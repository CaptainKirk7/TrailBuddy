using System;
using Newtonsoft.Json;

namespace TrailBuddy;

public class WeatherData
{
    public double Lat { get; set; }
    public double Lon { get; set; }

    public Current Current { get; set; }
    public Hourly[] Hourly { get; set; }
    public Daily[] Daily { get; set; }
    
    public ImageSource WeatherIcon { get; set; }
}

public class Current
{
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
    public double Temp { get; set; }

    [JsonProperty("feels_like")]
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Clouds { get; set; }

    [JsonProperty("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonProperty("wind_deg")]
    public int WindDeg { get; set; }

    [JsonProperty("wind_gust")]
    public double WindGust { get; set; }
    public Weather[] Weather { get; set; }
}

public class Hourly
{
    public double Temp { get; set; }

    [JsonProperty("dt")]
    public long Time { get; set; }
    public DateTime DateTime { get; set; }

    [JsonProperty("feels_like")]
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Clouds { get; set; }

    [JsonProperty("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonProperty("wind_deg")]
    public int WindDeg { get; set; }

    [JsonProperty("wind_gust")]
    public double WindGust { get; set; }
    public Weather[] Weather { get; set; }

    [JsonProperty("pop")]
    public double Precipitation { get; set; }
    public ImageSource WeatherIcon { get; set; }

}

public class Daily
{
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
    public string Summary { get; set; }
    public Temp Temp { get; set; }

    [JsonProperty("dt")]
    public long Time { get; set; }

    [JsonProperty("feels_like")]
    public Temp FeelsLike { get; set; }

    public int Humidity { get; set; }
    public int Clouds { get; set; }

    [JsonProperty("wind_speed")]
    public double WindSpeed { get; set; }

    [JsonProperty("wind_deg")]
    public int WindDeg { get; set; }

    [JsonProperty("wind_gust")]
    public double WindGust { get; set; }
    public Weather[] Weather { get; set; }

    [JsonProperty("pop")]
    public double Precipitation { get; set; }

}

public class Temp
{
    public double Day { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Night { get; set; }
    public double Eve { get; set; }
    public double Morn { get; set; }
}

public class Weather
{
    public string Main { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
}
