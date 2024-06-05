using TrailBuddy.ViewModels;
namespace TrailBuddy.Views;

public partial class PreferencePage : ContentPage
{
	public PreferencePage(INavigation nav)
	{
		InitializeComponent();
		BindingContext = new PreferenceViewModel(nav);
	}


}
