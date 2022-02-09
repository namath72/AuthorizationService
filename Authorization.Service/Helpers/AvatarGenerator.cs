using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;


namespace Authorization.Service.Helpers
{
	public class AvatarGenerator
	{
		private static readonly List<string> _BackgroundColours = new List<string> { "C7788F", "D169F1", "BE29EC", "003366", "37D1D3","8D1673","76DFE1" };

		public static byte[] Generate(string firstName, string lastName)
		{
			var avatarString = string.Format("{0}{1}", firstName[0], lastName[0]).ToUpper();

			var randomIndex = new Random().Next(0, _BackgroundColours.Count - 1);
			var bgColour = _BackgroundColours[randomIndex];

			var bmp = new Bitmap(192, 192);
			var sf = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};

			var font = new Font("Arial", 48, FontStyle.Bold, GraphicsUnit.Pixel);
			var graphics = Graphics.FromImage(bmp);

			graphics.Clear((Color)new ColorConverter().ConvertFromString("#" + bgColour));
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			graphics.DrawString(avatarString, font, new SolidBrush(Color.WhiteSmoke), new RectangleF(0, 0, 192, 192), sf);
			graphics.Flush();

			var ms = new MemoryStream();
			bmp.Save(ms, ImageFormat.Png);

			return ms.ToArray();
		}
	}
}
