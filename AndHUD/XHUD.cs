﻿using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;

using AndroidHUD;

namespace XHUD
{
	public enum MaskType
	{
//		None = 1,
		Clear,
		Black,
//		Gradient
	}

	public static class HUD
	{
		public static Activity MyActivity;

		public static void Show(string message, int progress = -1, MaskType maskType = MaskType.Black)
		{
			AndHUD.Shared.Show(HUD.MyActivity, message, progress,(AndroidHUD.MaskType)maskType);
		}

        public static void ShowAnimatedProgress(Stream stream, string message, int progress, MaskType maskType = MaskType.Black)
        {
            AndHUD.Shared.ShowAnimatedProgress(HUD.MyActivity, stream, progress);
        }

		public static void Dismiss()
		{
			AndHUD.Shared.Dismiss(HUD.MyActivity);
		}

		public static void ShowToast(string message, bool showToastCentered = true, double timeoutMs = 1000)
		{
			AndHUD.Shared.ShowToast(HUD.MyActivity, message, (AndroidHUD.MaskType)MaskType.Black, TimeSpan.FromSeconds(timeoutMs/1000), showToastCentered);
		}

		public static void ShowToast(string message, MaskType maskType, bool showToastCentered = true, double timeoutMs = 1000)
		{
			AndHUD.Shared.ShowToast(HUD.MyActivity, message, (AndroidHUD.MaskType)maskType, TimeSpan.FromSeconds(timeoutMs/1000), showToastCentered);
		}
	}
}

