using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Windows;

namespace RevoluteConfigApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            nvSample.ItemInvoked += NvSample_ItemInvoked;
        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // Handle settings item invocation if needed
            }
            else
            {
                var selectedItem = args.InvokedItemContainer as NavigationViewItem;
                if (selectedItem != null)
                {
                    string pageTag = selectedItem.Tag.ToString();
                    Type pageType = null;

                    switch (pageTag)
                    {
                        case "Discover":
                            pageType = typeof(Pages.DiscoverPage);
                            break;
                        case "BLEPage":
                            pageType = typeof(Pages.BLEPage);
                            break;
                        default:
                            break;
                    }

                    if (pageType != null)
                    {
                        contentFrame.Navigate(pageType);
                    }
                }
            }
        }
    }
}