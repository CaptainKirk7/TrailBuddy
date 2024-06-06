namespace TrailBuddy.Views;
using TrailBuddy.ViewModels;

public partial class WeatherPage : ContentPage
{
	WeatherViewModel _viewModel;
	WeatherData data;

	public WeatherPage(WeatherData weatherData)
	{
		InitializeComponent();
		_viewModel = new WeatherViewModel(Navigation, weatherData);
		BindingContext = _viewModel;
	}

}
