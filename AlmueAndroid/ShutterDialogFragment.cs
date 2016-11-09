using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace AlmueAndroid
{
    public class ShutterDialogFragment : DialogFragment
    {
        private string _shutterTopic;
        private string _timerOpenTopic;
        private string _timerCloseTopic;
        private string _floor;
        private string _description;

        private TextView _txtTimerOpenTime;
        private TextView _txtTimerCloseTime;
        private TextView _txtShutterStatus;
        private TextView _txtShutterDesc;
        private TextView _txtShutterFloor;

        private DevicesConfigObject.ShutterConfig _shutterConfig;

        private readonly MqttController _mqttController = MqttController.Instance;

        public static ShutterDialogFragment NewInstance(Bundle bundle)
        {
            var fragment = new ShutterDialogFragment { Arguments = bundle };
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.dialog_shutter, container, false);
            return view;
        }

        public override void OnResume()
        {
            var window = Dialog.Window;
            var size = new Point();
            var display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);
            window.SetLayout(ViewGroup.LayoutParams.MatchParent, (int)(size.Y * 0.75));
            window.SetGravity(GravityFlags.Center);
            base.OnResume();
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = base.OnCreateDialog(savedInstanceState);
            dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            return dialog;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            _mqttController.ReceivedConfig -= OnReceivedConfig;
            _mqttController.ReceivedConfig += OnReceivedConfig;
            _floor = Arguments.GetString("floor");
            _description = Arguments.GetString("descr");
            _shutterConfig = _mqttController.GetShutterByTag(_floor, _description);
            if (_shutterConfig != null)
            {
                _txtShutterDesc = view.FindViewById<TextView>(Resource.Id.txtShutterDescription);
                _txtShutterFloor = view.FindViewById<TextView>(Resource.Id.txtShutterFloor);
                _txtTimerOpenTime = view.FindViewById<TextView>(Resource.Id.txtTimeOpen);
                _txtTimerCloseTime = view.FindViewById<TextView>(Resource.Id.txtTimeClose);
                _txtShutterStatus = view.FindViewById<TextView>(Resource.Id.txtShutterStatus);
                var timesLayout = view.FindViewById<LinearLayout>(Resource.Id.linlayTimes);
                var timesLabelLayout = view.FindViewById<LinearLayout>(Resource.Id.linlayTimesLabels);

                _txtShutterDesc.Text = _description;
                _txtShutterFloor.Text = _floor;
                _txtTimerCloseTime.Text = _shutterConfig.CloseTime.ToString(@"hh\:mm");
                _txtTimerOpenTime.Text = _shutterConfig.OpenTime.ToString(@"hh\:mm");
                _shutterTopic = $"almue/shutter/{_floor}/{_description}";
                _timerOpenTopic = $"{_shutterTopic}/timeron";
                _timerCloseTopic = $"{_shutterTopic}/timeroff";
                _txtShutterStatus.Text = GetDeviceStatusText();

                var btnShutterUp = view.FindViewById<ImageButton>(Resource.Id.btnShutterUp);
                var btnShutterDown = view.FindViewById<ImageButton>(Resource.Id.btnShutterDown);
                var btnShutterStop = view.FindViewById<ImageButton>(Resource.Id.btnShutterStop);

                var btnShutterEnable = view.FindViewById<Switch>(Resource.Id.btnEnableDisableShutter);
                var btnTimerEnable = view.FindViewById<Switch>(Resource.Id.btnEnableDisableTimer);

                btnShutterEnable.Checked = !_shutterConfig.Disabled;
                btnTimerEnable.Checked = _shutterConfig.TimerEnabled;

                btnShutterDown.Click += (x, e) =>
                {
                    _mqttController.SendCommand(_shutterTopic, "close");
                };

                btnShutterUp.Click += (x, e) =>
                {
                    _mqttController.SendCommand(_shutterTopic, "open");
                };

                btnShutterStop.Click += (x, e) =>
                {
                    _mqttController.SendCommand(_shutterTopic, "stop");
                };

                btnShutterEnable.CheckedChange += (x, e) =>
                {
                    if (e.IsChecked)
                    {
                        _mqttController.SendCommand(_shutterTopic, "enable");
                        //TODO: IN EIGENE FUNKTION AUSLAGERN
                        btnShutterDown.Enabled = true;
                        btnShutterStop.Enabled = true;
                        btnShutterUp.Enabled = true;
                        btnTimerEnable.Enabled = true;
                        if (btnTimerEnable.Checked)
                        {
                            _txtTimerCloseTime.Enabled = true;
                            _txtTimerOpenTime.Enabled = true;
                            timesLabelLayout.Visibility = ViewStates.Visible;
                            timesLayout.Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            _txtTimerCloseTime.Enabled = false;
                            _txtTimerOpenTime.Enabled = false;
                            timesLabelLayout.Visibility = ViewStates.Gone;
                            timesLayout.Visibility = ViewStates.Gone;
                        }
                    }
                    else
                    {
                        _mqttController.SendCommand(_shutterTopic, "disable");
                        btnShutterDown.Enabled = false;
                        btnShutterStop.Enabled = false;
                        btnShutterUp.Enabled = false;
                        btnTimerEnable.Enabled = false;
                        _txtTimerCloseTime.Enabled = false;
                        _txtTimerOpenTime.Enabled = false;
                    }
                };

                btnTimerEnable.CheckedChange += (x, e) =>
                {
                    if (e.IsChecked)
                    {
                        _mqttController.SendCommand(_shutterTopic, "enabletimer");
                        _txtTimerOpenTime.Enabled = true;
                        _txtTimerCloseTime.Enabled = true;
                        timesLabelLayout.Visibility = ViewStates.Visible;
                        timesLayout.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        _mqttController.SendCommand(_shutterTopic, "disabletimer");
                        _txtTimerOpenTime.Enabled = false;
                        _txtTimerCloseTime.Enabled = false;
                        timesLabelLayout.Visibility = ViewStates.Gone;
                        timesLayout.Visibility = ViewStates.Gone;
                    }
                };

                _txtTimerCloseTime.Click += (x, e) =>
                {
                    var tpDlg = new TimePickerDialog(Activity, TimePickerCloseCallback, _shutterConfig.CloseTime.Hours, _shutterConfig.CloseTime.Minutes, true);
                    tpDlg.SetTitle(Resource.String.SetupCloseTime);
                    tpDlg.Show();
                };

                _txtTimerOpenTime.Click += (x, e) =>
                {
                    var tpDlg = new TimePickerDialog(Activity, TimePickerOpenCallback, _shutterConfig.OpenTime.Hours, _shutterConfig.OpenTime.Minutes, true);
                    tpDlg.SetTitle(Resource.String.SetupOpenTime);
                    tpDlg.Show();
                };

                //TODO: IN EIGENE FUNKTION AUSLAGERN
                if (btnShutterEnable.Checked)
                {
                    btnShutterDown.Enabled = true;
                    btnShutterStop.Enabled = true;
                    btnShutterUp.Enabled = true;
                    btnTimerEnable.Enabled = true;
                    if (btnTimerEnable.Checked)
                    {
                        timesLabelLayout.Visibility = ViewStates.Visible;
                        timesLayout.Visibility = ViewStates.Visible;
                        _txtTimerCloseTime.Enabled = true;
                        _txtTimerOpenTime.Enabled = true;
                    }
                    else
                    {
                        timesLabelLayout.Visibility = ViewStates.Gone;
                        timesLayout.Visibility = ViewStates.Gone;
                        _txtTimerCloseTime.Enabled = false;
                        _txtTimerOpenTime.Enabled = false;
                    }
                }
                else
                {
                    btnShutterDown.Enabled = false;
                    btnShutterStop.Enabled = false;
                    btnShutterUp.Enabled = false;
                    btnTimerEnable.Enabled = false;
                    _txtTimerCloseTime.Enabled = false;
                    _txtTimerOpenTime.Enabled = false;
                    if (btnTimerEnable.Checked)
                    {
                        timesLabelLayout.Visibility = ViewStates.Visible;
                        timesLayout.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        timesLabelLayout.Visibility = ViewStates.Gone;
                        timesLayout.Visibility = ViewStates.Gone;
                    }
                }
            }
            else
            {
                Toast.MakeText(Context, Resource.String.ShutterDialogException, ToastLength.Short).Show();
            }
        }

        private void OnReceivedConfig(object sender, EventArgs e)
        {
            _shutterConfig = _mqttController.GetShutterByTag(_floor, _description);

            if (IsAdded)
            {
                Activity.RunOnUiThread(() =>
                {
                    _txtShutterStatus.Text = GetDeviceStatusText();
                    _txtTimerCloseTime.Text = _shutterConfig.CloseTime.ToString(@"hh\:mm");
                    _txtTimerOpenTime.Text = _shutterConfig.OpenTime.ToString(@"hh\:mm");
                });
            }
        }

        private string GetDeviceStatusText()
        {
            var retval = string.Empty;
            if (_shutterConfig != null)
            {
                switch (_shutterConfig.DeviceStatus)
                {
                    case DeviceStatus.Opened:
                        retval = GetString(Resource.String.Opened);
                        break;
                    case DeviceStatus.Closed:
                        retval = GetString(Resource.String.Closed);
                        break;
                    case DeviceStatus.On:
                        retval = GetString(Resource.String.On);
                        break;
                    case DeviceStatus.Off:
                        retval = GetString(Resource.String.Off);
                        break;
                    case DeviceStatus.FailState:
                        retval = GetString(Resource.String.FailState);
                        break;
                    case DeviceStatus.Undefined:
                        retval = GetString(Resource.String.Undefinded);
                        break;
                    default:
                        break;
                }
            }
            return retval;
        }

        private void TimePickerCloseCallback(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            var ts = new TimeSpan(e.HourOfDay, e.Minute, 0);
            _mqttController.SendCommand(_timerCloseTopic, ts.ToString(@"hh\:mm\:ss"));
        }

        private void TimePickerOpenCallback(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            var ts = new TimeSpan(e.HourOfDay, e.Minute, 0);
            _mqttController.SendCommand(_timerOpenTopic, ts.ToString(@"hh\:mm\:ss"));
        }
    }
}
