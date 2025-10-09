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

		int CurX = 0;
		int CurY = 0;

		int FontW = 8;
		int FontH = 8;

		Image GfxImage;
		Color* GfxImagePix;

		Image FontImage;
		Color* FontPix;

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

		public void SetPixel(int X, int Y, Color Clr, bool SetDirty = true)
		{
			while (!Running)
				Thread.Sleep(1);

			int Idx = Y * W + X;
			if (Idx < 0 || Idx > W * H)
				return;

			lock (Lock)
			{
				GfxImagePix[Idx] = Clr;

				if (SetDirty)
					Dirty = true;
			}
		}

		public void Write(char arg1)
		{
			while (!Running)
				Thread.Sleep(1);

			int ScreenX = FontW * CurX;
			int ScreenY = FontH * CurY;

			if (arg1 == ' ')
			{
				CurX++;
			}
			else if (arg1 == '\n')
			{
				CurX = 0;
				CurY++;
			}
			else
			{
				for (int y = 0; y < FontH; y++)
				{
					for (int x = 0; x < FontW; x++)
					{
						int FontIdx = (arg1 % 16) * FontW + x + ((arg1 / 16) * FontH + y) * (16 * FontW);
						//int ScreenIdx = (ScreenY + y) * W + (ScreenX + x);

						if (FontPix[FontIdx].A > 0)
							SetPixel(ScreenX + x, ScreenY + y, FontPix[FontIdx], false);
						else
							SetPixel(ScreenX + x, ScreenY + y, Color.Black, false);
					}
				}

				CurX++;	
				Dirty = true;
			}

			if (CurX > 60)
			{
				CurY++;
				CurX = 0;
			}
		}

		void RunThread()
		{
			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			Raylib.InitWindow(W, H, "Fishmachine");
			Raylib.SetTargetFPS(60);

			GfxImage = Raylib.GenImageColor(W, H, Color.Black);
			Raylib.ImageFormat(ref GfxImage, PixelFormat.UncompressedR8G8B8A8);
			GfxImagePix = (Color*)GfxImage.Data;

			FontImage = Raylib.LoadImage("data/font.png");
			Raylib.ImageFormat(ref FontImage, PixelFormat.UncompressedR8G8B8A8);
			FontPix = (Color*)FontImage.Data;

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
