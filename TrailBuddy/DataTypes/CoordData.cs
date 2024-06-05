using System;
using SQLite;

namespace TrailBuddy;

[Table("local_coord")]
public class CoordData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string PlaceId { get; set; }

    // Stored as "lat,long"
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }

}




