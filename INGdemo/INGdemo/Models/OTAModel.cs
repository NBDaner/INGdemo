﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions;

using INGota.FOTA;
using INGdemo.Models;

namespace INGota.Models
{
    class ThermoFOTAViewer : ContentPage
    {
        IDevice BleDevice;
        internal ActivityIndicator RunningInd;
        internal Button Action;
        internal StackLayout UpdateInfo;
        internal Label LocalVersion;
        internal Label LatestVersion;
        internal Label VersionInfo;
        internal Label Summary;
        internal bool DetailVisible;
        string FOTA_SERVER = "http://192.168.100.101/fota/";
        static public Guid GUID_SERVICE = new Guid("3345c2f0-6f36-45c5-8541-92f56728d5f3");
        static public Guid GUID_CHAR_OTA_VER = new Guid("3345c2f1-6f36-45c5-8541-92f56728d5f3");
        static public Guid GUID_CHAR_OTA_CTRL = new Guid("3345c2f2-6f36-45c5-8541-92f56728d5f3");
        static public Guid GUID_CHAR_OTA_DATA = new Guid("3345c2f3-6f36-45c5-8541-92f56728d5f3");
        static public string SERVICE_NAME = "INGChips FOTA Service";
        static public string ICON_STR = Char.ConvertFromUtf32(0x1F680);

        OTA ota;
        IBleDriver driver;
        WaitActivity Wait;
        Entry urlInput;

        void InitUI()
        {
            var container = new StackLayout();
            var server = new Label();
            server.Text = "FOTA Server";
            server.Style = Device.Styles.TitleStyle;
            urlInput = new Entry();
            urlInput.Text = FOTA_SERVER;

            container.Margin = 10;
            container.Children.Add(server);
            container.Children.Add(urlInput);

            var btn = new Button
            {
                Text = "Activate Secondary App"
            };
            btn.Pressed += Btn_Pressed;

            container.Children.Add(new BoxView());

            container.Children.Add(BuildSummary());
            container.Children.Add(BuidUpdateInfo());

            container.Children.Add(new BoxView());
            container.Children.Add(btn);

            Title = SERVICE_NAME;
            Content = new ScrollView { Content = container };
        }

        private async void Btn_Pressed(object sender, EventArgs e)
        {
            await ota.ActivateSecondaryApp();
            var adapter = CrossBluetoothLE.Current.Adapter;
            await adapter.DisconnectDeviceAsync(BleDevice);
        }

        View BuildSummary()
        {
            var container = new StackLayout();
            container.Orientation = StackOrientation.Horizontal;

            Summary = new Label();
            Summary.Text = "FOTA";
            Summary.Style = Device.Styles.TitleStyle;
            Summary.VerticalOptions = LayoutOptions.Center;
            Summary.HorizontalOptions = LayoutOptions.StartAndExpand;

            RunningInd = new ActivityIndicator();
            RunningInd.IsRunning = true;
            RunningInd.Color = Color.Blue;
            RunningInd.VerticalOptions = LayoutOptions.Center;

            Action = new Button();

            Action.Text = "Update";
            Action.HorizontalOptions = LayoutOptions.End;
            Action.VerticalOptions = LayoutOptions.Center;

            container.Spacing = 10;
            container.HorizontalOptions = LayoutOptions.Fill;
            container.Children.Add(RunningInd);
            container.Children.Add(Summary);
            container.Children.Add(Action);

            return container;
        }

        View BuidUpdateInfo()
        {
            UpdateInfo = new StackLayout();

            LocalVersion = new Label()
            {
                Text = "LocalVersion",
                Style = Device.Styles.CaptionStyle
            };

            LatestVersion = new Label()
            {
                Text = "LatestVersion",
                Style = Device.Styles.CaptionStyle
            };

            VersionInfo = new Label()
            {
                Text = "VersionInfo",
                Style = Device.Styles.BodyStyle
            };

            UpdateInfo.Spacing = 5;
            UpdateInfo.Children.Add(LocalVersion);
            UpdateInfo.Children.Add(LatestVersion);
            UpdateInfo.Children.Add(new BoxView { HeightRequest = 20 });
            UpdateInfo.Children.Add(VersionInfo);
            return UpdateInfo;
        }

        public ThermoFOTAViewer(IDevice ADevice, IReadOnlyList<IService> services)
        {
            BleDevice = ADevice;
            if (BleDevice == null)
                return;

            driver = new BleDriver(GUID_SERVICE, GUID_CHAR_OTA_VER, GUID_CHAR_OTA_CTRL, GUID_CHAR_OTA_DATA);
            ota = new OTA(FOTA_SERVER, driver);
            ota.StatusChanged += Ota_StatusChanged;
            ota.Progress += Ota_Progress;

            InitUI();

            var tapGestureRecognizer = new TapGestureRecognizer();

            tapGestureRecognizer.NumberOfTapsRequired = 1;
            tapGestureRecognizer.Tapped += Summary_Tapped;

            Summary.GestureRecognizers.Add(tapGestureRecognizer);
            Summary_Tapped(null, null);
            Action.Clicked += Action_Clicked;

            driver.Init(ADevice, services);
            ota.CheckUpdate();
        }

        private void Summary_Tapped(object sender, EventArgs e)
        {
            DetailVisible = !DetailVisible;
            Ota_StatusChanged(sender, ota.Status);
        }

        private void Ota_Progress(object sender, OTA.ProgressArgs e)
        {
            Wait.Message = e.Msg;
            Wait.Progress = e.Progress;
            if (e.Status == OTA.UpdateStatus.Error)
                DisplayAlert("Alert", e.Msg, "OK");
        }

        private void Ota_StatusChanged(object sender, OTA.OTAStatus e)
        {
            RunningInd.IsVisible = ota.Status == OTA.OTAStatus.Checking;
            Action.IsVisible = ota.Status != OTA.OTAStatus.Checking;

            switch (ota.Status)
            {
                case OTA.OTAStatus.Checking:
                    Summary.Text = "Checking";
                    UpdateInfo.IsVisible = false;
                    break;
                case OTA.OTAStatus.ServerError:
                    Summary.Text = "FOTA Server Error";
                    UpdateInfo.IsVisible = false;
                    Action.Text = "Re-check";
                    break;
                case OTA.OTAStatus.UpToDate:
                    Summary.Text = "Firware Up-to-date";
                    LocalVersion.Text = "Local:\t" + ota.LocalVersion;
                    LatestVersion.Text = "Latest:\t" + ota.LatestVersion;
                    VersionInfo.Text = ota.UpdateInfo;
                    UpdateInfo.IsVisible = DetailVisible;
                    Action.Text = "Re-check";
                    break;
                case OTA.OTAStatus.UpdateAvailable:
                    Summary.Text = "Update Available";
                    LocalVersion.Text = "Local:\t" + ota.LocalVersion;
                    LatestVersion.Text = "Latest:\t" + ota.LatestVersion;
                    VersionInfo.Text = ota.UpdateInfo;
                    UpdateInfo.IsVisible = DetailVisible;
                    Action.Text = "Update";
                    break;
            }
        }

        private async void Action_Clicked(object sender, EventArgs e)
        {
            switch (ota.Status)
            {
                case OTA.OTAStatus.ServerError:
                case OTA.OTAStatus.UpToDate:
                    ota.updateURL = urlInput.Text;
                    await ota.CheckUpdate();
                    break;
                case OTA.OTAStatus.UpdateAvailable:
                    Wait = new WaitActivity();
                    DeviceDisplay.KeepScreenOn = true;
                    bool r = false;
                    await Navigation.PushModalAsync(Wait);
                    try
                    {
                        BleDevice.UpdateConnectionInterval(ConnectionInterval.High);
                        int MtuSize = await BleDevice.RequestMtuAsync(512);
                        r = await ota.Update(Math.Max(23, MtuSize - 4));
                    }
                    finally
                    {
                        await Navigation.PopModalAsync();
                        DeviceDisplay.KeepScreenOn = false;
                    }

                    if (r)
                    {
                        var adapter = CrossBluetoothLE.Current.Adapter;

                        //adapter.DeviceConnectionLost
                        //adapter.DeviceDisconnected -= Adapter_DeviceDisconnected;

                        await adapter.DisconnectDeviceAsync(BleDevice);
                        await DisplayAlert("Congratulations!", "FOTA successfully completed", "OK");

                        await Navigation.PopAsync();
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
