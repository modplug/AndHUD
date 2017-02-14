using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Airbnb.Lottie;
using Com.Airbnb.Lottie.Model;

namespace AndroidHUD
{
    public class AndHUD
    {
        private static AndHUD _shared;

        private readonly object _dialogLock = new object();
        private ImageView _imageView;
        private ProgressWheel _progressWheel;
        private object _statusObj;
        private TextView _statusText;

        private readonly ManualResetEvent _waitDismiss = new ManualResetEvent(false);
        private LottieAnimationView _animationView;

        public static AndHUD Shared => _shared ?? (_shared = new AndHUD());

        public Dialog CurrentDialog { get; private set; }

        public void Show(Context context, string status = null, int progress = -1, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, bool centered = true, Action cancelCallback = null)
        {
            if (progress >= 0)
            {
                ShowProgress(context, progress, status, maskType, timeout, clickCallback, cancelCallback);
            }
            else
            {
                ShowStatus(context, true, status, maskType, timeout, clickCallback, centered, cancelCallback);
            }
        }

        public void ShowSuccess(Context context, string status = null, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, Action cancelCallback = null)
        {
            showImage(context, context.Resources.GetDrawable(Resource.Drawable.ic_successstatus), status, maskType,
                timeout, clickCallback, cancelCallback);
        }

        public void ShowError(Context context, string status = null, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, Action cancelCallback = null)
        {
            showImage(context, context.Resources.GetDrawable(Resource.Drawable.ic_errorstatus), status, maskType,
                timeout, clickCallback, cancelCallback);
        }

        public void ShowSuccessWithStatus(Context context, string status, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, Action cancelCallback = null)
        {
            showImage(context, context.Resources.GetDrawable(Resource.Drawable.ic_successstatus), status, maskType,
                timeout, clickCallback, cancelCallback);
        }

        public void ShowErrorWithStatus(Context context, string status, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, Action cancelCallback = null)
        {
            showImage(context, context.Resources.GetDrawable(Resource.Drawable.ic_errorstatus), status, maskType,
                timeout, clickCallback, cancelCallback);
        }

        public void ShowImage(Context context, int drawableResourceId, string status = null,
            MaskType maskType = MaskType.Black, TimeSpan? timeout = null, Action clickCallback = null,
            Action cancelCallback = null)
        {
            showImage(context, context.Resources.GetDrawable(drawableResourceId), status, maskType, timeout,
                clickCallback, cancelCallback);
        }

        public void ShowImage(Context context, Drawable drawable, string status = null,
            MaskType maskType = MaskType.Black, TimeSpan? timeout = null, Action clickCallback = null,
            Action cancelCallback = null)
        {
            showImage(context, drawable, status, maskType, timeout, clickCallback, cancelCallback);
        }

        public void ShowToast(Context context, string status, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, bool centered = true, Action clickCallback = null, Action cancelCallback = null)
        {
            ShowStatus(context, false, status, maskType, timeout, clickCallback, centered, cancelCallback);
        }

        public void Dismiss(Context context = null)
        {
            DismissCurrent(context);
        }

        private void ShowStatus(Context context, bool spinner, string status = null, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, bool centered = true, Action cancelCallback = null)
        {
            if (timeout == null)
                timeout = TimeSpan.Zero;

            DismissCurrent(context);

            if (CurrentDialog != null && _statusObj == null)
                DismissCurrent(context);

            lock (_dialogLock)
            {
                if (CurrentDialog == null)
                {
                    SetupDialog(context, maskType, cancelCallback, (a, d, m) =>
                    {
                        var view = LayoutInflater.From(context).Inflate(Resource.Layout.loading, null);

                        if (clickCallback != null)
                            view.Click += (sender, e) => clickCallback();

                        _statusObj = new object();

                        _statusText = view.FindViewById<TextView>(Resource.Id.textViewStatus);

                        if (!spinner)
                            view.FindViewById<ProgressBar>(Resource.Id.loadingProgressBar).Visibility = ViewStates.Gone;

                        if (maskType != MaskType.Black)
                            view.SetBackgroundResource(Resource.Drawable.roundedbgdark);

                        if (_statusText != null)
                        {
                            _statusText.Text = status ?? "";
                            _statusText.Visibility = string.IsNullOrEmpty(status) ? ViewStates.Gone : ViewStates.Visible;
                        }

                        if (!centered)
                        {
                            d.Window.SetGravity(GravityFlags.Bottom);
                            var p = d.Window.Attributes;

                            p.Y = DpToPx(context, 22);

                            d.Window.Attributes = p;
                        }

                        return view;
                    });

                    if (timeout > TimeSpan.Zero)
                        Task.Factory.StartNew(() =>
                        {
                            if (!_waitDismiss.WaitOne(timeout.Value))
                                DismissCurrent(context);
                        }).ContinueWith(ct =>
                        {
                            var ex = ct.Exception;

                            if (ex != null)
                                Log.Error("AndHUD", ex.ToString());
                        }, TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    Application.SynchronizationContext.Send(state =>
                    {
                        if (_statusText != null)
                            _statusText.Text = status ?? "";
                    }, null);
                }
            }
        }

        private int DpToPx(Context context, int dp)
        {
            var displayMetrics = context.Resources.DisplayMetrics;
            var px = (int) Math.Round(dp * (displayMetrics.Xdpi / (double) DisplayMetricsDensity.Default));
            return px;
        }

        private void ShowProgress(Context context, int progress, string status = null,
            MaskType maskType = MaskType.Black, TimeSpan? timeout = null, Action clickCallback = null,
            Action cancelCallback = null)
        {
            if (!timeout.HasValue || timeout == null)
                timeout = TimeSpan.Zero;

            if (CurrentDialog != null && _progressWheel == null)
                DismissCurrent(context);

            lock (_dialogLock)
            {
                if (CurrentDialog == null)
                {
                    SetupDialog(context, maskType, cancelCallback, (a, d, m) =>
                    {
                        var inflater = LayoutInflater.FromContext(context);
                        var view = inflater.Inflate(Resource.Layout.loadingprogress, null);

                        if (clickCallback != null)
                            view.Click += (sender, e) => clickCallback();

                        _progressWheel = view.FindViewById<ProgressWheel>(Resource.Id.loadingProgressWheel);
                        _statusText = view.FindViewById<TextView>(Resource.Id.textViewStatus);

                        if (maskType != MaskType.Black)
                            view.SetBackgroundResource(Resource.Drawable.roundedbgdark);

                        _progressWheel.SetProgress(0);

                        if (_statusText != null)
                        {
                            _statusText.Text = status ?? "";
                            _statusText.Visibility = string.IsNullOrEmpty(status) ? ViewStates.Gone : ViewStates.Visible;
                        }

                        return view;
                    });

                    if (timeout.Value > TimeSpan.Zero)
                        Task.Factory.StartNew(() =>
                        {
                            if (!_waitDismiss.WaitOne(timeout.Value))
                                DismissCurrent(context);
                        }).ContinueWith(ct =>
                        {
                            var ex = ct.Exception;

                            if (ex != null)
                                Log.Error("AndHUD", ex.ToString());
                        }, TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    Application.SynchronizationContext.Send(state =>
                    {
                        _progressWheel.SetProgress(progress);
                        _statusText.Text = status ?? "";
                    }, null);
                }
            }
        }

        public void ShowAnimatedProgress(Context context, Stream animationStream, int progress, bool isIndeterminate = false,
            string status = null,
            MaskType maskType = MaskType.Black, TimeSpan? timeout = null, Action clickCallback = null,
            Action cancelCallback = null)
        {
            if (!timeout.HasValue)
            {
                timeout = TimeSpan.Zero;
            }

            if (CurrentDialog != null && _animationView == null)
            {
                DismissCurrent(context);
            }

            lock (_dialogLock)
            {
                if (CurrentDialog == null)
                {
                    SetupDialog(context, maskType, cancelCallback, (a, d, m) =>
                    {
                        var inflater = LayoutInflater.FromContext(context);
                        var view = inflater.Inflate(Resource.Layout.loadinganimatedprogress, null);

                        if (clickCallback != null)
                        {
                            view.Click += (sender, e) => clickCallback();
                        }
                        _animationView = view.FindViewById<LottieAnimationView>(Resource.Id.lottieAnimationView);
                        try
                        {
                            LottieComposition.FromInputStream(context, animationStream,
                                lottieComposition =>
                                {
                                    _animationView.SetComposition(lottieComposition);
                                    _animationView.Loop(isIndeterminate);
                                    if (!isIndeterminate)
                                    {
                                        _animationView.PauseAnimation();
                                    }
                                });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        
                        _statusText = view.FindViewById<TextView>(Resource.Id.textViewStatus);

                        if (maskType != MaskType.Black)
                        {
                            view.SetBackgroundResource(Resource.Drawable.roundedbgdark);
                        }

                        if (_statusText != null)
                        {
                            _statusText.Text = status ?? "";
                            _statusText.Visibility = string.IsNullOrEmpty(status) ? ViewStates.Gone : ViewStates.Visible;
                        }

                        return view;
                    });

                    if (timeout.Value > TimeSpan.Zero)
                        Task.Factory.StartNew(() =>
                        {
                            if (!_waitDismiss.WaitOne(timeout.Value))
                                DismissCurrent(context);
                        }).ContinueWith(ct =>
                        {
                            var ex = ct.Exception;

                            if (ex != null)
                                Log.Error("AndHUD", ex.ToString());
                        }, TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    Application.SynchronizationContext.Send(state =>
                    {
                        _animationView.Progress = (float)progress / 100;
                        _statusText.Text = status ?? "";
                    }, null);
                }
            }
        }


        public void showImage(Context context, Drawable image, string status = null, MaskType maskType = MaskType.Black,
            TimeSpan? timeout = null, Action clickCallback = null, Action cancelCallback = null)
        {
            if (timeout == null)
                timeout = TimeSpan.Zero;

            if (CurrentDialog != null && _imageView == null)
                DismissCurrent(context);

            lock (_dialogLock)
            {
                if (CurrentDialog == null)
                {
                    SetupDialog(context, maskType, cancelCallback, (a, d, m) =>
                    {
                        var inflater = LayoutInflater.FromContext(context);
                        var view = inflater.Inflate(Resource.Layout.loadingimage, null);

                        if (clickCallback != null)
                            view.Click += (sender, e) => clickCallback();

                        _imageView = view.FindViewById<ImageView>(Resource.Id.loadingImage);
                        _statusText = view.FindViewById<TextView>(Resource.Id.textViewStatus);

                        if (maskType != MaskType.Black)
                            view.SetBackgroundResource(Resource.Drawable.roundedbgdark);

                        _imageView.SetImageDrawable(image);

                        if (_statusText != null)
                        {
                            _statusText.Text = status ?? "";
                            _statusText.Visibility = string.IsNullOrEmpty(status) ? ViewStates.Gone : ViewStates.Visible;
                        }

                        return view;
                    });

                    if (timeout > TimeSpan.Zero)
                        Task.Factory.StartNew(() =>
                        {
                            if (!_waitDismiss.WaitOne(timeout.Value))
                                DismissCurrent(context);
                        }).ContinueWith(ct =>
                        {
                            var ex = ct.Exception;

                            if (ex != null)
                                Log.Error("AndHUD", ex.ToString());
                        }, TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    Application.SynchronizationContext.Send(state =>
                    {
                        _imageView.SetImageDrawable(image);
                        _statusText.Text = status ?? "";
                    }, null);
                }
            }
        }


        private void SetupDialog(Context context, MaskType maskType, Action cancelCallback,
            Func<Context, Dialog, MaskType, View> customSetup)
        {
            Application.SynchronizationContext.Send(state =>
            {
                CurrentDialog = new Dialog(context);

                CurrentDialog.RequestWindowFeature((int) WindowFeatures.NoTitle);

                if (maskType != MaskType.Black)
                    CurrentDialog.Window.ClearFlags(WindowManagerFlags.DimBehind);

                if (maskType == MaskType.None)
                    CurrentDialog.Window.SetFlags(WindowManagerFlags.NotTouchModal, WindowManagerFlags.NotTouchModal);

                CurrentDialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

                var customView = customSetup(context, CurrentDialog, maskType);

                CurrentDialog.SetContentView(customView);

                CurrentDialog.SetCancelable(cancelCallback != null);
                if (cancelCallback != null)
                    CurrentDialog.CancelEvent += (sender, e) => cancelCallback();

                CurrentDialog.Show();
            }, null);
        }

        private void DismissCurrent(Context context = null)
        {
            lock (_dialogLock)
            {
                if (CurrentDialog != null)
                {
                    _waitDismiss.Set();

                    Action actionDismiss = () =>
                    {
                        CurrentDialog.Hide();
                        CurrentDialog.Dismiss();

                        _statusText = null;
                        _statusObj = null;
                        _imageView = null;
                        _animationView = null;
                        _progressWheel = null;
                        CurrentDialog = null;

                        _waitDismiss.Reset();
                    };

                    //First try the SynchronizationContext
                    if (Application.SynchronizationContext != null)
                    {
                        Application.SynchronizationContext.Send(state => actionDismiss(), null);
                        return;
                    }

                    //Next let's try and get the Activity from the CurrentDialog
                    if (CurrentDialog != null && CurrentDialog.Window != null && CurrentDialog.Window.Context != null)
                    {
                        var activity = CurrentDialog.Window.Context as Activity;

                        if (activity != null)
                        {
                            activity.RunOnUiThread(actionDismiss);
                            return;
                        }
                    }

                    //Finally if all else fails, let's see if someone passed in a context to dismiss and it
                    // happens to also be an Activity
                    if (context != null)
                    {
                        var activity = context as Activity;

                        if (activity != null)
                        {
                            activity.RunOnUiThread(actionDismiss);
                        }
                    }
                }
            }
        }
    }

    public enum MaskType
    {
        None = 1,
        Clear = 2,
        Black = 3
    }
}