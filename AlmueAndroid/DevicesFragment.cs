using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AlmueAndroid
{
    public class DevicesFragment : Fragment
    {
        private IList<DeviceItem> _items;
        private readonly MqttController _mqttController = MqttController.Instance;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _items = _mqttController.GetAllDevicesOfFloor(Tag).ToList();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_devices, container, false);
            var listView = view.FindViewById<ListView>(Resource.Id.devicesListView);

            if (_mqttController.GetAllDevicesOfFloor(Tag).Any())
            {
                listView.Visibility = ViewStates.Visible;
                listView.Adapter = new DeviceAdapter(Activity, _items);
                listView.ItemClick += OnLvItemClick;
            }
            else
            {
                listView.Visibility = ViewStates.Gone;
            }

            return view;
        }

        private void OnLvItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var item = _items[e.Position];
            switch (item.DeviceType)
            {
                case DeviceType.Shutter:
                    CreateShutterDialog(e.Position);
                    break;
                case DeviceType.Lighting:
                    CreateLightingDialog(e.Position);
                    break;
                default:
                    return;
            }
        }

        private void CreateLightingDialog(int position)
        {
            var ft = ChildFragmentManager.BeginTransaction();
            //Remove fragment else it will crash as it is already added to backstack
            var prev = ChildFragmentManager.FindFragmentByTag("lightingDialog");
            if (prev != null)
            {
                ft.Remove(prev);
            }

            ft.AddToBackStack(null);

            // Create and show the dialog.
            var bundle = new Bundle();
            bundle.PutString("floor", _items[position].Floor);
            bundle.PutString("descr", _items[position].Description);
            var newFragment = LightingDialogFragment.NewInstance(bundle);

            //Add fragment
            newFragment.Show(ft, "lightingDialog");
        }

        private void CreateShutterDialog(int position)
        {
            var ft = ChildFragmentManager.BeginTransaction();
            //Remove fragment else it will crash as it is already added to backstack
            var prev = ChildFragmentManager.FindFragmentByTag("shutterDialog");
            if (prev != null)
            {
                ft.Remove(prev);
            }

            ft.AddToBackStack(null);

            // Create and show the dialog.
            var bundle = new Bundle();
            bundle.PutString("floor", _items[position].Floor);
            bundle.PutString("descr", _items[position].Description);
            var newFragment = ShutterDialogFragment.NewInstance(bundle);

            //Add fragment
            newFragment.Show(ft, "shutterDialog");
        }
    }
}