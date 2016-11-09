using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AlmueAndroid
{
    public class LightingDialogFragment : DialogFragment
    {
        public static LightingDialogFragment NewInstance(Bundle bundle)
        {
            var fragment = new LightingDialogFragment {Arguments = bundle};
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            var view = inflater.Inflate(Resource.Layout.dialog_lighting, container, false);
            var button = view.FindViewById<Button>(Resource.Id.CloseButton);
            button.Click += delegate {
                Dismiss();
                Toast.MakeText(Context, "Dialog fragment dismissed!", ToastLength.Short).Show();
            };

            return view;
        }
    }
}
