using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mopups.Services;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class ViewImagePopupViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private double screenWidth;
    private double screenHeight;
    private ImageSource imageSource;

    private double frameHeight;
    private double frameWidth;

    private double buttonWidth;

    public ViewImagePopupViewModel(string imgSource)
    {
        CloseCommand = new Command(
            execute: async () =>
            {
                 await MopupService.Instance.PopAsync();
            }
        );

        ScreenHeight = DeviceDisplay.MainDisplayInfo.Height;
        ScreenWidth = DeviceDisplay.MainDisplayInfo.Width;
        FrameWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 75;
        FrameHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - 300;
        ImageSource = imgSource;
    }

    // Getters & Setters -----------------------------------------------------
    public ImageSource ImageSource
    {
        set
        {
            if (imageSource != value)
            {
                imageSource = value;
                NotifyPropertyChanged(nameof(ImageSource));
            }
        }
        get
        {
            return imageSource;
        }
    }

    public double FrameWidth
    {
        private set
        {
            if (frameWidth != value)
            {
                frameWidth = value;
                NotifyPropertyChanged(nameof(FrameWidth));
            }
        }
        get
        {
            return frameWidth;
        }
    }

    public double FrameHeight
    {
        private set
        {
            if (frameHeight != value)
            {
                frameHeight = value;
                NotifyPropertyChanged(nameof(FrameHeight));
            }
        }
        get
        {
            return frameHeight;
        }
    }

    public double ButtonWidth
    {
        private set
        {
            if (buttonWidth != value)
            {
                buttonWidth = value;
                NotifyPropertyChanged(nameof(ButtonWidth));
            }
        }
        get
        {
            return buttonWidth;
        }
    }

    public double ScreenWidth
    {
        private set
        {
            if (screenWidth != value)
            {
                screenWidth = value;
                NotifyPropertyChanged(nameof(ScreenWidth));
            }
        }
        get
        {
            return screenWidth;
        }
    }

    public double ScreenHeight
    {
        private set
        {
            if (screenHeight != value)
            {
                screenHeight = value;
                NotifyPropertyChanged(nameof(ScreenHeight));
            }
        }
        get
        {
            return screenHeight;
        }
    }

    // Base methods -----------------------------------------------------
    public ICommand CloseCommand { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

