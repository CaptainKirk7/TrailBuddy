using System;
using SQLite;

namespace TrailBuddy;

[Table("local_trails")]
public class LocalTrails
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string PlaceId { get; set; }

    public string Name { get; set; }
    public int Rating { get; set; }
    public string DateAdded { get; set; }
    public string DetailedDateAdded { get; set; }
    public double DistanceTraveled { get; set; }
    public string DistanceString { get; set; }
    public string TotalTime { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ImgUrl { get; set; }

    public string DistanceAbv { get; set; }

    // Helper methods -----------------------------------------------------
    public void Remove(LocalTrails local)
    {
        if (local != null)
        {
            try
            {
                int v = DB.conn.Delete(local);
            }
            catch (Exception e) { }
        }
    }

    public void Add(LocalTrails local)
    {
        if (local != null)
        {
            try
            {
                DB.conn.Insert(local);
            }
            catch (Exception e) { }
        }
    }

}




