using Android.Widget;
using System.Collections.Generic;
using Android.Views;
using Android.App;

namespace AlmueAndroid
{
    public class DeviceAdapter : BaseAdapter<string>
    {
        private readonly IList<DeviceItem> _items;

        private readonly Activity _ctx;

        public DeviceAdapter(Activity context, IList<DeviceItem> items) : base()
        {
            _ctx = context;
            _items = items;
        }

        public override long GetItemId(int position) => position;

        public override string this[int position] => _items[position].Description;

        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? _ctx.LayoutInflater.Inflate(Resource.Layout.item_device, null);

            var itm = _items[position];
            view.FindViewById<TextView>(Resource.Id.deviceDescription).Text = itm.Description;
            var imageView = view.FindViewById<ImageView>(Resource.Id.deviceImage);
            switch (itm.DeviceType)
            {
                case DeviceType.Lighting:
                    imageView.SetImageResource(Resource.Drawable.ic_lightbulb_outline_black_24dp);
                    break;
                case DeviceType.Shutter:
                    imageView.SetImageResource(Resource.Drawable.ic_line_weight_black_24dp);
                    break;
            }

            return view;
        }
    }
}
