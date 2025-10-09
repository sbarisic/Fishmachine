using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace Fishmachine
{
	public unsafe class Graphics
	{
		int W;
		int H;

		Image GfxImage;
		Color* GfxImagePix;

		Texture2D GfxTex;

		bool Running = false;
		bool Dirty = false;
		object Lock = new object();

		public Graphics()
		{

		}

		public void Setup(int W, int H)
		{
			this.W = W;
			this.H = H;
		}

		public void SetPixel(int X, int Y, Color Clr)
		{
			while (!Running)
				Thread.Sleep(1);

			int Idx = Y * W + X;
			if (Idx < 0 || Idx > W * H)
				return;

			lock (Lock)
			{
				GfxImagePix[Idx] = Clr;
				Dirty = true;
			}
		}

		void RunThread()
		{
			Raylib.InitWindow(W, H, "Fishmachine");
			Raylib.SetTargetFPS(60);

			GfxImage = Raylib.GenImageColor(W, H, Color.Black);
			Raylib.ImageFormat(ref GfxImage, PixelFormat.UncompressedR8G8B8A8);
			GfxImagePix = (Color*)GfxImage.Data;

			GfxTex = Raylib.LoadTextureFromImage(GfxImage);

			Running = true;

			while (!Raylib.WindowShouldClose())
			{
				if (Dirty)
				{
					lock (Lock)
					{
						Dirty = false;
						Raylib.UpdateTexture(GfxTex, GfxImage.Data);
					}
				}

				Raylib.BeginDrawing();
				Raylib.DrawTexture(GfxTex, 0, 0, Color.White);
				Raylib.EndDrawing();
			}

			Raylib.CloseWindow();
		}

		public void StartThread()
		{
			Thread T = new Thread(RunThread);
			T.IsBackground = true;
			T.Start();
		}

	}
}
