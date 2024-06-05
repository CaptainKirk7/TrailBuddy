using Mopups.Services;
using TrailBuddy.ViewModels;
using Mopups.Services;

namespace TrailBuddy.Views;

public partial class FeedbackPopup
{

    private FeedbackPopupViewModel _viewModel;

    public FeedbackPopup()
	{
		InitializeComponent();
		_viewModel = new FeedbackPopupViewModel();
		BindingContext = _viewModel;
	}

    async void TapGestureRecognizer_Tapped(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await MopupService.Instance.PopAsync();
    }
}
