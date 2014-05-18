using System;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace WPFTest
{
	public partial class MainApplication : System.Windows.Application
	{
		protected override void OnLoadCompleted(NavigationEventArgs e)
		{
			// Set the Window title.
			this.MainWindow.Title = "Application";

			// Get the window to take the size of its content, and then allow
			// it to be set by the user, and have the content take the size
			// of the window.
			/*if (!this.IsWebBrowserApplication)
			{
				this.MainWindow.SizeToContent = SizeToContent.WidthAndHeight;
				this.MainWindow.SizeToContent = SizeToContent.Manual;
			}

			FrameworkElement root = this.MainWindow.Content as FrameworkElement;
			if (root != null)
			{
				root.Height = double.NaN;
				root.Width = double.NaN;

				root.Focus();
			}*/
		}
		
		private bool IsWebBrowserApplication
		{
			get
			{
				try
				{
					PermissionSet testSet = new PermissionSet(PermissionState.None);
					testSet.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));
					testSet.Assert();

					return false;
				}
				catch (SecurityException)
				{
					return true;
				}
			}
		}
	}
}
