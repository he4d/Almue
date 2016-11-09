using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlmueAndroid
{
    [Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar",
          Label = "AlmueAndroid", MainLauncher = true, WindowSoftInputMode = SoftInput.StateHidden)]
    public class LoginActivity : Activity
    {
        private Button _btnLogin;
        private EditText _txtBroker, _txtUser, _txtPw;
        private ProgressBar _pgBar;
        private CheckBox _chkSaveLogin;
        private readonly MqttController _mqttController = MqttController.Instance;
        private HashSet<EditText> _loginEdits = new HashSet<EditText>();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_login);

            var sp = GetSharedPreferences("Login", FileCreationMode.Private);
            var brokerIp = sp.GetString("IP", null);
            var userName = sp.GetString("Username", null);
            var password = sp.GetString("Password", null);
            _pgBar = FindViewById<ProgressBar>(Resource.Id.pgBar);
            _btnLogin = FindViewById<Button>(Resource.Id.btnLogin);
            _txtBroker = FindViewById<EditText>(Resource.Id.txtBroker);
            _txtUser = FindViewById<EditText>(Resource.Id.txtUser);
            _txtPw = FindViewById<EditText>(Resource.Id.txtPw);
            _chkSaveLogin = FindViewById<CheckBox>(Resource.Id.chkSaveLoginData);
            _mqttController.ReceivedConfig -= OnMqttControllerIsReady;
            _mqttController.FailedToConnect -= OnMqttControllerFailedToConnect;
            _mqttController.ReceivedConfig += OnMqttControllerIsReady;
            _mqttController.FailedToConnect += OnMqttControllerFailedToConnect;
            _loginEdits.Add(_txtBroker);
            _loginEdits.Add(_txtPw);
            _loginEdits.Add(_txtUser);
            _txtBroker.TextChanged += LoginTextChanged;
            _txtUser.TextChanged += LoginTextChanged;
            _txtPw.TextChanged += LoginTextChanged;

            if (!string.IsNullOrEmpty(brokerIp) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                _txtBroker.Text = brokerIp;
                _txtUser.Text = userName;
                _txtPw.Text = password;
                _chkSaveLogin.Checked = true;
                _btnLogin.Enabled = true;
                if (_mqttController.IsConnectedToBroker)
                {
                    StartMainActivity();
                    return;
                }
                ConnectToBroker();
            }

            _btnLogin.Click += (x, e) =>
            {
                if (_mqttController.IsConnectedToBroker)
                {
                    if (!_chkSaveLogin.Checked)
                        sp.Edit().Clear().Commit();
                    StartMainActivity();
                    return;
                }

                if (_chkSaveLogin.Checked)
                {
                    sp.Edit().PutString("IP", _txtBroker.Text)
                    .PutString("Username", _txtUser.Text)
                    .PutString("Password", _txtPw.Text)
                    .Commit();
                }
                else
                    sp.Edit().Clear().Commit();

                ConnectToBroker();
            };
        }

        private void ConnectToBroker()
        {
            _txtBroker.Enabled = false;
            _txtUser.Enabled = false;
            _txtPw.Enabled = false;
            _chkSaveLogin.Enabled = false;
            _btnLogin.Enabled = false;
            _pgBar.Visibility = ViewStates.Visible;
            _mqttController.ConnectToBroker(_txtBroker.Text, _txtUser.Text, _txtPw.Text);
        }

        private void StartMainActivity()
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        private void LoginTextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            _btnLogin.Enabled = _loginEdits.All(edit => !string.IsNullOrEmpty(edit.Text));
        }

        private void OnMqttControllerFailedToConnect(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                _pgBar.Visibility = ViewStates.Gone;
                _txtBroker.Enabled = true;
                _txtUser.Enabled = true;
                _txtPw.Enabled = true;
                _chkSaveLogin.Enabled = true;
                _btnLogin.Enabled = true;
                Toast.MakeText(ApplicationContext, Resource.String.FailedToConnect, ToastLength.Short).Show();
            });
        }

        private void OnMqttControllerIsReady(object sender, EventArgs e)
        {
            _mqttController.ReceivedConfig -= OnMqttControllerIsReady;
            RunOnUiThread(() =>
            {
                _pgBar.Visibility = ViewStates.Gone;
                _btnLogin.Enabled = true;
            });
            StartMainActivity();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Process.KillProcess(Process.MyPid());
        }
    }
}
