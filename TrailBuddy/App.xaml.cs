namespace TrailBuddy;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        MainPage = new AppShell();
		DB.OpenConnection();
		Preferences.Set("refreshTopTrails", true);
	}


}

