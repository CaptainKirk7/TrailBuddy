namespace TrailBuddy.Views;
using TrailBuddy.ViewModels;

public partial class WeatherPage : ContentPage
{
	public WeatherPage(WeatherData weatherData)
	{
		InitializeComponent();
		BindingContext = new WeatherViewModel(Navigation, weatherData);
	}
}
