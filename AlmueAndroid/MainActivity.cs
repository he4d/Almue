using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace AlmueAndroid
{
    [Activity(Theme = "@android:style/Theme.Material.Light",
        Label = "AlmueAndroid", MainLauncher = false)]
    public class MainActivity : Activity
    {
        private readonly MqttController _mqttController = MqttController.Instance;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);
            ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
            _mqttController.ConnectionToBrokerClosed -= OnMqttConnectionClosed;
            _mqttController.ConnectionToBrokerClosed += OnMqttConnectionClosed;

            foreach (var floor in _mqttController.AllConfiguredFloors)
                AddTab(floor);
        }

        private void AddTab(string floor)
        {
            var tab = ActionBar.NewTab();
            tab.SetText(floor);

            tab.TabSelected += (x, e) => 
            {
                e.FragmentTransaction.Replace(Resource.Id.deviceFragmentContainer, new DevicesFragment(), floor);
            };

            ActionBar.AddTab(tab);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menuOpenAll:
                    _mqttController.OpenAllShutters();
                    return true;
                case Resource.Id.menuCloseAll:
                    _mqttController.CloseAllShutters();
                    return true;
                case Resource.Id.menuLogout:
                    Logout();
                    return true;
                case Resource.Id.menuAbout:
                    ShowAbout();
                    return true;
                case Resource.Id.menuExit:
                    {
                        FinishAffinity();
                        return true;
                    }
            }
            return base.OnOptionsItemSelected(item);
        }

        private void ShowAbout()
        {
            var msgView = LayoutInflater.Inflate(Resource.Layout.dialog_about, null, false);
            var textView = msgView.FindViewById<Android.Widget.TextView>(Resource.Id.about_credits);

            var builder = new AlertDialog.Builder(this);
            builder.SetIcon(Resource.Mipmap.ic_launcher);
            builder.SetTitle("AlmueAndroid");
            builder.SetView(msgView);
            builder.Create();
            builder.Show();

        }

        private void OnMqttConnectionClosed(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                Toast.MakeText(ApplicationContext, Resource.String.ConnectionLost, ToastLength.Short).Show();
            });
            Finish();
        }

        private void Logout()
        {
            var sp = GetSharedPreferences("Login", FileCreationMode.Private);
            sp.Edit().Clear().Commit();
            var intent = new Intent(this, typeof(LoginActivity));
            _mqttController.Disconnect();
            StartActivity(intent);
        }
    }
}
