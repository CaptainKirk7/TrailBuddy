namespace TrailBuddy.Views;

using Mopups.Services;
using TrailBuddy.ViewModels;

public partial class ViewImagePopup
{
	private ViewImagePopupViewModel _viewModel;

	public ViewImagePopup(string imgSource)
	{
		InitializeComponent();

		_viewModel = new ViewImagePopupViewModel(imgSource);
		BindingContext = _viewModel;
	}

    async void TapGestureRecognizer_Tapped(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await MopupService.Instance.PopAsync();
    }
}
