using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RTLighting.GameObjects;
using RTLighting.Primatives;
using RTLighting.Rendering;
using RTLighting.Utilities;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace RTLighting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int Width = 1280;
        private const int Height = 720;

        private const int CellRows = Height / Constants.CELL_SIZE;
        private const int CellCols = Width / Constants.CELL_SIZE;

        private bool _isRendering;
        private Thread _renderThread;

        private ShadowQuality _shadowQuality;

        private TextureBrush _caveBrush;
        private Bitmap _background;
        private Bitmap _frameBuffer;

        private Cell[,] _grid;
        private float[,] _smoothedIntensities;

        private RayTracer _rayTracer;
        private ShadowShader _shadowShader;

        private WriteableBitmap _writeableBitmap;

        private GameTime _gameTime;
        private Controller _controller;

        private List<IGameObject> _gameObjects;
        private List<IRayEmitter> _rayEmitters;

        public MainWindow()
        {
            InitializeComponent();

            _shadowQuality = ShadowQuality.Smooth;

            _grid = new Cell[CellRows, CellCols];
            _smoothedIntensities = new float[CellRows, CellCols];

            for (int r = 0; r < CellRows; r++)
            {
                for (int c = 0; c < CellCols; c++)
                {
                    _grid[r,c] = new Cell();
                }
            }

            _gameObjects = new List<IGameObject>();
            _rayEmitters = new List<IRayEmitter>();

            var player = new Character(new Vector2(400, 200));

            _gameObjects.Add(player);
            _rayEmitters.Add(player);

            _rayTracer = new RayTracer(_grid);
            _shadowShader = new ShadowShader();

            _caveBrush = new TextureBrush(new Bitmap("Content/background.png"));
            _background = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            _frameBuffer = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);

            _writeableBitmap = new WriteableBitmap(Width, Height, 96,96, PixelFormats.Bgr32, null);

            Canvas.Source = _writeableBitmap;

            using (Graphics graphics = Graphics.FromImage(_frameBuffer))
            {
                graphics.Clear(Color.Black);
            }

            AddSurfaces();

            _gameTime = new GameTime();
            _controller = new Controller();

            _isRendering = true;

            _renderThread = new Thread(RenderLoop);
            _renderThread.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_isRendering)
            {
                _isRendering = false;

                _renderThread.Join(100);   
            }

            _caveBrush.Dispose();
            _frameBuffer.Dispose();

            base.OnClosed(e);
        }

        private void AddSurfaces()
        {
            for (int r = 0; r < CellRows; r++)
            {
                for (int c = 0; c < CellCols; c++)
                {
                    var cell = _grid[r, c];

                    cell.IsSolid = false;

                    // Cave opening
                    if ((r > 26 && r < 31) && (c < 38 || c > 57))
                    {
                        cell.IsSolid = true;
                        cell.Emissivity = 0.5f;
                    }

                    // Walls
                    if (c < 3 || c > CellCols - 4)
                    {
                        cell.IsSolid = true;
                        cell.Emissivity = 0.8f;
                    }

                    // Ceiling / Floor
                    if (r < 1 || r > CellRows - 1)
                    {
                        cell.IsSolid = true;
                        cell.Emissivity = 0.8f;
                    }

                    // Pillar 1
                    if ((r > 38) && (c > 32 && c < 38))
                    {
                        cell.IsSolid = true;
                        cell.Emissivity = 0.7f;
                    }

                    // Pillar 2
                    if ((r > 40) && (c > 77 && c < 83))
                    {
                        cell.IsSolid = true;
                        cell.Emissivity = 0.7f;
                    }
                }
            }
        }

        private void CastRays(GameTime gameTime)
        {
            gameTime.StartEvent("Ray Tracing");

            var rays = new List<Ray>();

            foreach (IRayEmitter rayEmitter in _rayEmitters)
            {
                rays.AddRange(rayEmitter.CastRays());
            }

            _rayTracer.Cast(rays);

            gameTime.EndEvent("Ray Tracing");
        }

        private void RenderLoop()
        {
            while (_isRendering)
            {
                _gameTime.BeginUpdate();

                _controller.Update(_gameTime);

                foreach (IGameObject gameObject in _gameObjects)
                {
                    gameObject.Update(_gameTime, _controller);
                }

                CastRays(_gameTime);

                float maxIntensity = 25f;

                for (int r = 0; r < CellRows; r++)
                {
                    for (int c = 0; c < CellCols; c++)
                    {
                        float cappedIntensity = Math.Min(_grid[r, c].Intensity / maxIntensity, 1);

                        float logIntensity = (float)Math.Log(100 * cappedIntensity + 1, 100);

                        float smoothedIntensity = Lerp(_smoothedIntensities[r, c], logIntensity, 0.25f);

                        _smoothedIntensities[r, c] = Math.Max(smoothedIntensity, 0.035f);
                    }
                }

                _gameTime.StartEvent("Shading");

                using (Graphics graphics = Graphics.FromImage(_background))
                {
                    graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.Clear(Color.Black);

                    graphics.FillRectangle(_caveBrush, 0, 0, 1280, 720);
                    
                    foreach (IGameObject gameObject in _gameObjects)
                    {
                        gameObject.Draw(graphics);
                    }

                    _shadowShader.Run(_shadowQuality, _frameBuffer, _background, _smoothedIntensities);
                }

                _gameTime.EndEvent("Shading");

                foreach (Cell cell in _grid)
                {
                    cell.Reset();
                }

                using (Graphics graphics = Graphics.FromImage(_frameBuffer))
                {
                    _gameTime.Draw(graphics, Width, Height);

                    string raysPerSecond = (_rayTracer.RaysCasted * _gameTime.CurrentFps).ToString("###,###,###");

                    graphics.DrawString($"{raysPerSecond} Rays/s", new Font("Arial", 16), new SolidBrush(Color.White), 20, Height - 50);
                }

                SetCaptureSource(_frameBuffer);

                _gameTime.FinalizeUpdate();
            }
        }

        private void SetCaptureSource(Bitmap frame)
        {
            var data = frame.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, frame.PixelFormat);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _writeableBitmap.WritePixels(new Int32Rect(0, 0, Width, Height), data.Scan0, data.Stride * data.Height, data.Stride);

            }), DispatcherPriority.Render, null);

            frame.UnlockBits(data);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _controller.UpdateKey(e.Key, false);

            if (e.Key == Key.X)
            {
                if (_shadowQuality == ShadowQuality.Mesh)
                {
                    _shadowQuality = ShadowQuality.Smooth;
                }
                else
                {
                    _shadowQuality = ShadowQuality.Mesh;
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _controller.UpdateKey(e.Key, true);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + t * (to - from);
        }
    }
}
