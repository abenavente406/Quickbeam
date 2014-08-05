using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Quickbeam.Dialogs;
using Quickbeam.Native;
using Quickbeam.Views;
using Quickbeam.ViewModels;

namespace Quickbeam.Windows
{
	/// <summary>
	/// Interaction logic for Home.xaml
	/// </summary>
	public partial class Home
	{
		public HomeViewModel ViewModel { get; private set; }

		public Home()
		{
			InitializeComponent();
			App.Storage.HomeWindow = this;

			ViewModel = new HomeViewModel();
			DataContext = App.Storage.HomeWindowViewModel = ViewModel;
			ViewModel.AssemblyPage = new ReplPage();

			Closing += OnClosing;
		}

		private static void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			cancelEventArgs.Cancel = !App.Storage.HomeWindowViewModel.AssemblyPage.Close();
		}

		protected override void OnStateChanged(EventArgs e)
		{
			ViewModel.OnStateChanged(WindowState, e);
			base.OnStateChanged(e);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			OnStateChanged(null);

			base.OnSourceInitialized(e);
		}

		private void OpenCacheMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.BlamCache));
		}

		private void OpenMapImageMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.MapImage));
		}

		private void OpenMapInfoMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.MapInfo));
		}

		private void OpenCampaignMenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.ValidateFile(ViewModel.FindFile(HomeViewModel.Type.Campaign));
		}
	}
}
