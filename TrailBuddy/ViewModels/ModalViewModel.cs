using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoreFoundation;
using Mopups.Services;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class ModalViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private INavigation _navigation;
    private Location currentLocation;
    private string title;
    private string elapsedTime;
    private string trailName;
    private string addressEntry;
    private string descriptionEntry;
    private string imgPath;
    private double totalDistance;
    private string distanceAbv;
    private string mapId;
    private int lineWidth;
    double ratingSlider;
    private Color buttonColor;

    public ModalViewModel(INavigation navigation, LocalTrails lc, string title)
    {
        List<CoordData> data = DB.conn.Table<CoordData>().Where(x => x.PlaceId == lc.PlaceId).ToList();

        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

        PopulateData(lc, title);

        // Command init -----------------------------------------------------
        SaveCommand = new Command<string>(
            execute: (string op) =>
            {
                LocalTrails trail = new LocalTrails();
                trail.PlaceId = lc.PlaceId;
                trail.Name = TrailName;
                trail.Rating = (int) Math.Round(RatingSlider);
                trail.DateAdded = lc.DateAdded;
                trail.DistanceTraveled = lc.DistanceTraveled;
                trail.DetailedDateAdded = lc.DetailedDateAdded;
                trail.TotalTime = lc.TotalTime;
                trail.Description = DescriptionEntry;
                trail.Address = AddressEntry;
                trail.Latitude = data[0].Latitude;
                trail.Longitude = data[0].Longitude;
                trail.ImgUrl = ImgPath;
                trail.DistanceAbv = DistanceAbv;

                if (op == "Modify Trail")
                {
                    trail.Id = lc.Id;

                    try
                    {
                        DB.conn.Update(trail);
                        _navigation.PopModalAsync();
                    }
                    catch (Exception e) { }
                } else
                {
                    try
                    {
                        DB.conn.Insert(trail);
                        _navigation.PopModalAsync();
                    }
                    catch (Exception e) { }
                }
            }
        );

        ImageCommand = new Command<string>(
            execute: (string takeImg) =>
            {
                TakePhoto(takeImg);
            }
        );

        ViewImage = new Command(
            execute: async () =>
            {
                await MopupService.Instance.PushAsync(new ViewImagePopup(ImgPath));
            },
            canExecute: () =>
            {
                return CheckImgPath(lc);
            }
        );

        FullscreenCommand = new Command<string>(
            execute: async (string mapId) =>
            {
                await _navigation.PushModalAsync(new FullscreenPage(mapId));
            }

        );

    }

    // Getters & Setters -----------------------------------------------------
    public Color ButtonColor
    {
        set
        {
            if (buttonColor != value)
            {
                buttonColor = value;
                NotifyPropertyChanged(nameof(ButtonColor));
            }
        }
        get
        {
            return buttonColor;
        }
    }

    public string MapId
    {
        set
        {
            if (mapId != value)
            {
                mapId = value;
                NotifyPropertyChanged(nameof(MapId));
            }
        }
        get
        {
            return mapId;
        }
    }

    public string DistanceAbv
    {
        set
        {
            if (distanceAbv != value)
            {
                distanceAbv = value;
                NotifyPropertyChanged(nameof(DistanceAbv));
            }
        }
        get
        {
            return distanceAbv;
        }
    }

    public double TotalDistance
    {
        set
        {
            if (totalDistance != value)
            {
                totalDistance = value;
                NotifyPropertyChanged(nameof(TotalDistance));
            }
        }
        get
        {
            return totalDistance;
        }
    }

    public int LineWidth
    {
        set
        {
            if (lineWidth != value)
            {
                lineWidth = value;
                NotifyPropertyChanged(nameof(LineWidth));
            }
        }
        get
        {
            return lineWidth;
        }
    }

    public string ImgPath
    {
        set
        {
            if (imgPath != value)
            {
                imgPath = value;
                NotifyPropertyChanged(nameof(ImgPath));
            }
        }
        get
        {
            return imgPath;
        }
    }

    public string DescriptionEntry
    {
        set
        {
            if (descriptionEntry != value)
            {
                descriptionEntry = value;
                NotifyPropertyChanged(nameof(DescriptionEntry));
            }
        }
        get
        {
            return descriptionEntry;
        }
    }

    public string AddressEntry
    {
        set
        {
            if (addressEntry != value)
            {
                addressEntry = value;
                NotifyPropertyChanged(nameof(AddressEntry));
            }
        }
        get
        {
            return addressEntry;
        }
    }

    public string TrailName
    {
        set
        {
            if (trailName != value)
            {
                trailName = value;
                NotifyPropertyChanged(nameof(TrailName));
            }
        }
        get
        {
            return trailName;
        }
    }

    public double RatingSlider
    {
        set
        {
            if (ratingSlider != value)
            {
                ratingSlider = value;
                NotifyPropertyChanged(nameof(RatingSlider));
            }
        }
        get
        {
            return ratingSlider;
        }
    }

    public string Title
    {
        private set
        {
            if (title != value)
            {
                title = value;
                NotifyPropertyChanged(nameof(Title));
            }
        }
        get
        {
            return title;
        }
    }

    public string ElapsedTime
    {
        private set
        {
            if (elapsedTime != value)
            {
                elapsedTime = value;
                NotifyPropertyChanged(nameof(ElapsedTime));
            }
        }
        get
        {
            return elapsedTime;
        }
    }
    public Location CurrentLocation
    {
        set
        {
            // I did not include a if statement here because in the view.cs
            // I subscribed to the property changed event, so I always want it to change
            // so you can use the center location button on the map.
            currentLocation = value;
            NotifyPropertyChanged(nameof(CurrentLocation));
        }
        get
        {
            return currentLocation;
        }
    }

    // Helper methods -----------------------------------------------------
    private bool CheckImgPath(LocalTrails lc)
    {
        if (ImgPath != lc.ImgUrl)
        {
            ButtonColor = Color.FromHex("#8A153C");
        } else
        {
            ButtonColor = Color.FromHex("#212121");
        }

        return ImgPath != lc.ImgUrl;
    }

    private async Task PopulateData(LocalTrails lc, string title)
    {
        Title = title;
        RatingSlider = lc.Rating;
        TrailName = lc.Name;
        AddressEntry = lc.Address;
        ElapsedTime = lc.TotalTime;
        ImgPath = lc.ImgUrl;
        DescriptionEntry = lc.Description;
        TotalDistance = lc.DistanceTraveled;
        LineWidth = title == "Modify Trail" ? 250 : 200;
        DistanceAbv = lc.DistanceAbv;
        MapId = lc.PlaceId;
    }

    private async void TakePhoto(string takeImg)
    {
        PermissionStatus cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (cameraStatus == PermissionStatus.Granted)
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                // Take or pick an image
                if (takeImg == "true")
                {

                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null)
                    {
                        string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                        using Stream sourceStream = await photo.OpenReadAsync();
                        using FileStream localFileStream = File.OpenWrite(localFilePath);
                        await sourceStream.CopyToAsync(localFileStream);

                        // Saving the image path
                        ImgPath = localFilePath;
                    }
                }
                else
                {
                    FileResult photo = await MediaPicker.Default.PickPhotoAsync();

                    if (photo != null)
                    {
                        string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                        using Stream sourceStream = await photo.OpenReadAsync();
                        using FileStream localFileStream = File.OpenWrite(localFilePath);
                        await sourceStream.CopyToAsync(localFileStream);

                        // Saving the image path
                        ImgPath = localFilePath;
                    }
                }
            }
        } else
        {
            // If there is no permission, request and redo method
            DisplayPopup(takeImg);
        }

        // Refresh can execute to show view button
        (ViewImage as Command).ChangeCanExecute();
    }

    public async Task DisplayPopup(string takeImg)
    {

        PermissionStatus camera = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (camera != PermissionStatus.Granted)
        {
            bool answer = await App.Current.MainPage.DisplayAlert("Error", "Please enable camera permissions", "Retry", "Cancel");
            if (answer)
            {
                DisplayPopup(takeImg);
            }
        } else
        {
            TakePhoto(takeImg);
        }
    }

    // Base methods -----------------------------------------------------
    public ICommand SaveCommand { private set; get; }
    public ICommand ImageCommand { private set; get; }
    public ICommand FullscreenCommand { private set; get; }
    public ICommand ViewImage { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
