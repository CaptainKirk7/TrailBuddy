using System;
using SQLite;

namespace TrailBuddy;

[Table("favorites")]
public class Favorites
{
    [PrimaryKey]
    public string Id { get; set; }

    public string Address { get; set; }
    public double Rating { get; set; }
    public string Uri { get; set; }
    public int UserRatingCount { get; set; }
    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Source { get; set; }
    public string FavoriteSource { get; set; }
    public bool IsFavorited { get; set; }

    [Ignore]
    public ImageSource ImageSource { get; set; }
    [Ignore]
    public ImageSource FavoriteImage { get; set; }
    [Ignore]
    public string DistanceString { get; set; }

    // Helper methods -----------------------------------------------------
    public List<Favorites> UpdateList(List<Favorites> fav)
    {
        foreach (Favorites f in fav)
        {
            if (!f.Source.Equals("null"))
            {
                f.ImageSource = ImageSource.FromUri(new Uri(f.Source));
            } else
            {
                f.ImageSource = ImageSource.FromFile("no_image.png");
            }

            f.FavoriteImage = ImageSource.FromFile(f.FavoriteSource);
        }

        return fav;
    }

    public void RemoveFromFavorites(Place p = null, Favorites f = null)
    {
        if (p != null)
        {
            try
            {
                f = CreateFavorite(p);
                int v = DB.conn.Delete(f);
            }
            catch (Exception e) { }
        } else
        {
            try
            {
                int v = DB.conn.Delete(f);
            }
            catch (Exception e) { }
        }
    }

    public void AddToFavorites(Place p = null, Favorites f = null)
    {
        if (p != null)
        {
            try
            {
                f = CreateFavorite(p);
                DB.conn.Insert(f);
            }
            catch (Exception e) { }
        } else
        {
            try
            {
                DB.conn.Insert(f);
            }
            catch (Exception e) { }
        }
    }

    public void SaveFavorite(string id, bool isFavorited)
    {
        // Same as place ID
        Preferences.Set(id, isFavorited);
    }

    public Favorites CreateFavorite(Place p)
    {
        Favorites fav = new Favorites();
        fav.Id = p.Id;
        fav.Name = p.Name;
        fav.Address = p.Address;
        fav.Rating = p.Rating;
        fav.UserRatingCount = p.UserRatingCount;
        fav.Latitude = p.Geometry.Location.Latitude;
        fav.Longitude = p.Geometry.Location.Longitude;
        fav.FavoriteSource = "favorites.png";
        fav.IsFavorited = p.IsFavorited;

        var photoName = p.Photos?.First()?.Name;
        if (!string.IsNullOrEmpty(photoName))
        {
            var name = p.Photos[0].Name;
            var uri = $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=1200&maxheight=1000&photo_reference={name}&key={Constants.MapsAPIKey}";
            fav.Source = uri.ToString();
        } else
        {
            fav.Source = "null";
        }

        return fav;
    }
}




