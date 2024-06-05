using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Mopups.Hosting;
using TrailBuddy;

namespace TrailBuddy;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureMopups()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("icons.ttf", "Icons");
			});

#if DEBUG
        builder.Logging.AddDebug();
#endif
#if IOS
	 Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("CancelButtonColor", (handler, view) =>
	{
		handler.PlatformView.SetShowsCancelButton(false, false);
	});
#endif
        FormHandler.RemoveBorders();

		
        return builder.Build();
	}
}

