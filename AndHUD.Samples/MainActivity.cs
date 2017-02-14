using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using Android.OS;
using Android.Widget;

namespace AndHUD.Samples
{
    [Activity(Label = "AndHUD.Samples", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            var button = FindViewById<Button>(Resource.Id.MyButton);
            button.Click += ButtonOnClick;
        }

        private async void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            var stream = Assets.Open("TwitterHeart.json", Access.Buffer);
            for (int i = 0; i <= 100; i++)
            {
                AndroidHUD.AndHUD.Shared.ShowAnimatedProgress(this, stream, i, false, "Progress: " + i + "%");
                await Task.Delay(50);
                if (i == 100)
                {
                    AndroidHUD.AndHUD.Shared.Dismiss(this);
                }
            }
        }
    }
}

