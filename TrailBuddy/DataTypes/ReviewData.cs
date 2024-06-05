using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace TrailBuddy;

public class ReviewData
{
    public Review[] Reviews { get; set; }
}

public class Review : INotifyPropertyChanged
{
    [JsonProperty("relativePublishTimeDescription")]
    public string HowLongAgo { get; set; }
    public double Rating { get; set; }

    [JsonProperty("text")]
    public Description Description { get; set; }

    [JsonProperty("authorAttribution")]
    public Author Author { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}

public class Description
{
    public string Text { get; set; }
}

public class Author
{
    [JsonProperty("displayName")]
    public string Name { get; set; }
    public string PhotoUri { get; set; }
    [JsonIgnore]
    public ImageSource PhotoSource { get; set; }
}
