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
		int Scale;

		int CurX = 0;
		int CurY = 0;

		int FontW = 8;
		int FontH = 8;

		int CurW;
		int CurH;

		Image GfxImage;
		Color* GfxImagePix;

		Image FontImage;
		Color* FontPix;

		Texture2D GfxTex;

		bool Running = false;
		bool Dirty = false;
		object Lock = new object();
		object RaylibLock = new object();

		public Graphics()
		{

		}

		public void Setup(int W, int H, int Scale)
		{
			this.W = W;
			this.H = H;
			this.Scale = Scale;

			CurW = W / FontW - 2;
			CurH = H / FontH - 2;
		}

		void WaitForRunning()
		{
			while (!Running)
				Thread.Sleep(1);
		}

		public void SetPixel(int X, int Y, Color Clr, bool SetDirty = true)
		{
			WaitForRunning();

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

		public void Write(string Str)
		{
			foreach (var C in Str)
			{
				Write(C);
			}
		}

		public void Write(char arg1)
		{
			WaitForRunning();

			int ScreenX = FontW + FontW * CurX;
			int ScreenY = FontH + FontH * CurY;

			if (arg1 == ' ')
			{
				CurX++;
			}
			else if (arg1 == '\n')
			{
				CurX = 0;
				CurY++;
			}
			else if (arg1 == '\b')
			{
				CurX--;

				if (CurX < 0)
					CurX = 0;
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

			if (CurX >= CurW)
			{
				CurY++;
				CurX = 0;
			}

			if (CurY >= CurH)
			{
				// TODO, scroll screen up by one row
				throw new NotImplementedException();
			}
		}

		void RunThread()
		{
			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			Raylib.InitWindow(W * Scale, H * Scale, "Fishmachine");
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
				if (Raylib.IsMouseButtonPressed(MouseButton.Left))
				{
					MousePressedLeft = true;
				}

				CharPressedCode = (uint)Raylib.GetCharPressed();

				if (CharPressedCode != 0)
				{
					if (Ascii.IsValid((char)CharPressedCode))
					{
					}
					else
					{
						CharPressedCode = 0;
					}
				}
				else
				{
					CharPressedCode = 0;
				}

				KeyPressedCode = (uint)Raylib.GetKeyPressed();

				
				if (KeyPressedCode != 0)
				{
					if (KeyPressedCode == 0x101 || KeyPressedCode == 0x14f)
					{
						KeyPressedCode = 0;
						CharPressedCode = (uint)'\n';
					}
				}

				if (Dirty)
				{
					lock (Lock)
					{
						Dirty = false;
						Raylib.UpdateTexture(GfxTex, GfxImage.Data);
					}
				}

				Raylib.BeginDrawing();
				//				Raylib.DrawTexture(GfxTex, 0, 0, Color.White);
				Raylib.DrawTexturePro(GfxTex, new Rectangle(0, 0, GfxTex.Width, GfxTex.Height), new Rectangle(0, 0, W * Scale, H * Scale), System.Numerics.Vector2.Zero, 0, Color.White);
				Raylib.EndDrawing();

			}

			Raylib.CloseWindow();
		}

		bool MousePressedLeft = false;
		uint KeyPressedCode = 0;
		uint CharPressedCode = 0;

		public bool MousePressed()
		{
			if (MousePressedLeft)
			{
				MousePressedLeft = false;
				return true;
			}

			return false;
		}

		public bool KeyPressed(out uint key)
		{
			if (KeyPressedCode != 0)
			{
				key = KeyPressedCode;
				KeyPressedCode = 0;
				return true;
			}

			key = 0;
			return false;
		}

		public bool CharPressed(out uint key)
		{
			if (CharPressedCode != 0)
			{
				key = CharPressedCode;
				CharPressedCode = 0;
				return true;
			}

			key = 0;
			return false;
		}

		public void StartThread()
		{
			Thread T = new Thread(RunThread);
			T.IsBackground = true;
			T.Start();
		}
	}
}
