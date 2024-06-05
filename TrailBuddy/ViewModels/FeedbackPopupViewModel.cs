using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mopups.Services;
using TrailBuddy.Views;

namespace TrailBuddy.ViewModels;

public class FeedbackPopupViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    ApiClient _client;
    NetworkAccess _network;

    private double screenWidth;
    private double screenHeight;

    private double frameHeight;
    private double frameWidth;

    private double buttonWidth;

    public FeedbackPopupViewModel()
    {
        _client = new ApiClient();
        _network = Connectivity.Current.NetworkAccess;

        Feedback = new Command<object>(
            execute: async (object param) =>
            {
                // obj[0] = feedback
                // obj[1] = email
                var obj = param as object[];
                if (obj[0] == null) obj[0] = "";
                if (obj[1] == null) obj[1] = "";

                if (_network == NetworkAccess.Internet)
                {
                    // If both fields are empty, just close
                    if (obj[0].ToString().Equals("") && obj[1].ToString().Equals(""))
                    {
                        await MopupService.Instance.PopAsync();
                    }
                    else
                    {
                        SendFeedback(obj[0].ToString(), obj[1].ToString());
                    }
                } else
                {
                    DisplayPopup(obj[0].ToString(), obj[1].ToString());
                }

            }
        );

        ScreenHeight = DeviceDisplay.MainDisplayInfo.Height;
        ScreenWidth = DeviceDisplay.MainDisplayInfo.Width;
        FrameWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 75;
        FrameHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - 300;

    }

    // Getters & Setters -----------------------------------------------------
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

    public async Task DisplayPopup(string feedback = "", string email = "")
    {
        _network = Connectivity.Current.NetworkAccess;

        bool answer = await App.Current.MainPage.DisplayAlert("Error", "No internet connection detected", "Retry", "Cancel");
        if (answer)
        {
            SendFeedback(feedback, email);
        }
    }

    public async void SendFeedback(string feedback = "", string email = "")
    {
        _network = Connectivity.Current.NetworkAccess;

        if (_network == NetworkAccess.Internet)
        {
            // Send data if the text field has something
            if (feedback.Length > 0)
                await _client.SendFeedback(feedback, email);

            // Close window
            await MopupService.Instance.PopAsync();
        }
        else
        {
            DisplayPopup(feedback, email);
        }
    }

    // Base methods -----------------------------------------------------
    public ICommand Feedback { private set; get; }

    protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

