using System;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;

namespace FFSchedule;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static HttpClient HttpClient { get; } = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10) 
    };

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FFSchedule/1.0 (popovis@mer.ci.nsu.ru)");
    }
}

