using System.Diagnostics;
using TrailBuddy.Views;

namespace TrailBuddy;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        //titleLabel.Text = Current.CurrentPage.Title;
    }

    //async void donateButton_Clicked(System.Object sender, System.EventArgs e)
    //{
    //    bool answer = await DisplayAlert(
    //        "Support My App",
    //        "I hope you're enjoying TrailBuddy! Your donations help me to keep improving and adding new features, along with operational costs. Every little bit helps and is greatly appreciated.",
    //        "Donate",
    //        "Later"
    //    );

    //    if (answer)
    //    {
    //        try
    //        {
    //            Uri uri = new Uri("https://www.paypal.com/donate/?business=VJ5N49FETGBSE&no_recurring=0&item_name=Thank+you+for+your+support%21&currency_code=USD");
    //            BrowserLaunchOptions options = new BrowserLaunchOptions()
    //            {
    //                LaunchMode = BrowserLaunchMode.SystemPreferred,
    //                TitleMode = BrowserTitleMode.Show,
    //                PreferredToolbarColor = Color.FromHex("0b5e1e"),
    //                PreferredControlColor = Colors.SandyBrown
    //            };

    //            await Browser.Default.OpenAsync(uri, options);
    //        }
    //        catch (Exception ex)
    //        {
    //            // An unexpected error occurred. No browser may be installed on the device.
    //        }
    //    }
    //}
}

