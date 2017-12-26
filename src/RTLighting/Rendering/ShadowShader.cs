using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace RTLighting.Rendering
{
    enum ShadowQuality
    {
        Mesh,
        Smooth
    }

    class ShadowShader
    {
        private float[,,] _areaMap;

        public ShadowShader()
        {
            BuildAreaMap();
        }

        private void BuildAreaMap()
        {
            _areaMap = new float[Constants.CELL_SIZE, Constants.CELL_SIZE, 4];

            for (int y = 0; y < Constants.CELL_SIZE; y++)
            {
                float yFactor = (float)y / Constants.CELL_SIZE;

                for (int x = 0; x < Constants.CELL_SIZE; x++)
                {
                    float xFactor = (float)x / Constants.CELL_SIZE;

                    if (xFactor > 0.5f)
                    {
                        float xA1 = 0.5f + 1.0f - xFactor;
                        float xA2 = 1.0f - xA1;

                        float yA1 = 0.5f + 1 - yFactor;
                        float yA2 = 1.0f - yA1;

                        if (yFactor < 0.5f)
                        {
                            yA2 = yFactor + 0.5f;
                            yA1 = 1.0f - yA2;
                        }

                        _areaMap[x, y, 0] = xA1 * yA1;
                        _areaMap[x, y, 1] = xA2 * yA1;
                        _areaMap[x, y, 2] = xA1 * yA2;
                        _areaMap[x, y, 3] = xA2 * yA2;
                    }
                    else
                    {
                        float xA2 = xFactor + 0.5f;
                        float xA1 = 1 - xA2;

                        float yA1 = 0.5f + 1 - yFactor;
                        float yA2 = 1.0f - yA1;

                        if (yFactor < 0.5f)
                        {
                            yA2 = yFactor + 0.5f;
                            yA1 = 1.0f - yA2;
                        }

                        _areaMap[x, y, 0] = xA1 * yA1;
                        _areaMap[x, y, 1] = xA2 * yA1;
                        _areaMap[x, y, 2] = xA1 * yA2;
                        _areaMap[x, y, 3] = xA2 * yA2;   
                    }
                }
            }
        }

        public void Run(ShadowQuality quality, Bitmap frameBuffer, Bitmap background, float[,] intensities)
        {
            if (quality == ShadowQuality.Mesh)
            {
                MeshShade(frameBuffer, intensities);
            }
            else
            {
                SmoothShade(frameBuffer, background, intensities);
            }
        }

        private unsafe void MeshShade(Bitmap frameBuffer, float[,] intensities)
        {
            BitmapData frameData = frameBuffer.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.WriteOnly, frameBuffer.PixelFormat);

            int width = frameData.Width - Constants.CELL_SIZE;
            int height = frameData.Height - Constants.CELL_SIZE;

            var frameHead = (byte*)frameData.Scan0;

            int stride = frameData.Stride;

            Parallel.For(Constants.CELL_SIZE, height, y =>
            {
                byte* frameRow = frameHead + (y * stride);

                int cellY = y / Constants.CELL_SIZE;

                for (int x = Constants.CELL_SIZE; x < width; x++)
                {
                    int cellX = x / Constants.CELL_SIZE;

                    float lerpedIntensity = intensities[cellY, cellX];

                    int rowOffset = x * 4;

                    frameRow[rowOffset] = (byte)(255 * lerpedIntensity);
                    frameRow[rowOffset + 1] = (byte)(255 * lerpedIntensity);
                    frameRow[rowOffset + 2] = (byte)(255 * lerpedIntensity);
                }
            });

            frameBuffer.UnlockBits(frameData);
        }

        private unsafe void SmoothShade(Bitmap frameBuffer, Bitmap background, float[,] intensities)
        {
            BitmapData frameData = frameBuffer.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.WriteOnly, frameBuffer.PixelFormat);
            BitmapData bgData = background.LockBits(new Rectangle(0, 0, frameBuffer.Width, frameBuffer.Height), ImageLockMode.ReadOnly, frameBuffer.PixelFormat);

            int halfCell = Constants.CELL_SIZE / 2;

            int width = background.Width - Constants.CELL_SIZE;
            int height = background.Height - Constants.CELL_SIZE;

            var frameHead = (byte*)frameData.Scan0;
            var bgHead = (byte*) bgData.Scan0;

            int stride = bgData.Stride;

            Parallel.For(Constants.CELL_SIZE, height, y =>
            {
                byte* frameRow = frameHead + (y*stride);
                byte* bgRow = bgHead + (y * stride);

                int cellY = y / Constants.CELL_SIZE;
                int yIndex = y % Constants.CELL_SIZE;

                for (int x = Constants.CELL_SIZE; x < width; x++)
                {
                    float intensity;

                    int cellX = x / Constants.CELL_SIZE;
                    int xIndex = x % Constants.CELL_SIZE;

                    // Use bilinear interopliation for generating the current pixel intensity
                    if (xIndex > halfCell)
                    {
                        if (yIndex > halfCell)
                        {
                            intensity = intensities[cellY, cellX] * _areaMap[xIndex, yIndex, 0] +
                                        intensities[cellY, cellX + 1] * _areaMap[xIndex, yIndex, 1] +
                                        intensities[cellY + 1, cellX] * _areaMap[xIndex, yIndex, 2] +
                                        intensities[cellY + 1, cellX + 1] * _areaMap[xIndex, yIndex, 3];
                        }
                        else
                        {
                            intensity = intensities[cellY - 1, cellX] * _areaMap[xIndex, yIndex, 0] +
                                        intensities[cellY - 1, cellX + 1] * _areaMap[xIndex, yIndex, 1] +
                                        intensities[cellY, cellX] * _areaMap[xIndex, yIndex, 2] +
                                        intensities[cellY, cellX + 1] * _areaMap[xIndex, yIndex, 3];
                        }
                    }
                    else
                    {
                        if (yIndex > halfCell)
                        {
                            intensity = intensities[cellY, cellX - 1] * _areaMap[xIndex, yIndex, 0] +
                                        intensities[cellY, cellX] * _areaMap[xIndex, yIndex, 1] +
                                        intensities[cellY + 1, cellX - 1] * _areaMap[xIndex, yIndex, 2] +
                                        intensities[cellY + 1, cellX] * _areaMap[xIndex, yIndex, 3];
                        }
                        else
                        {
                            intensity = intensities[cellY - 1, cellX - 1] * _areaMap[xIndex, yIndex, 0] +
                                        intensities[cellY - 1, cellX] * _areaMap[xIndex, yIndex, 1] +
                                        intensities[cellY, cellX - 1] * _areaMap[xIndex, yIndex, 2] +
                                        intensities[cellY, cellX] * _areaMap[xIndex, yIndex, 3];
                        }
                    }

                    int rowOffset = x * 4;

                    float aR = bgRow[rowOffset] / 255.0f;
                    float aG = bgRow[rowOffset + 1] / 255.0f;
                    float aB = bgRow[rowOffset + 2] / 255.0f;

                    // Apply intensity more brightly when closer to maximum
                    if (intensity > 0.5f)
                    {
                        frameRow[rowOffset] = (byte)(254 * (1 - (1 - aR) * (1 - 2 * (intensity - 0.5f))));
                        frameRow[rowOffset + 1] = (byte)(254 * (1 - (1 - aG) * (1 - 2 * (intensity - 0.5f))));
                        frameRow[rowOffset + 2] = (byte)(254 * (1 - (1 - aB) * (1 - 2 * (intensity - 0.5f))));
                    }
                    else
                    {
                        // Apply intensity linear
                        frameRow[rowOffset] = (byte)(255 * (2 * aR * intensity));
                        frameRow[rowOffset + 1] = (byte)(255 * (2 * aG * intensity));
                        frameRow[rowOffset + 2] = (byte)(255 * (2 * aB * intensity));
                    }
                }
              });

            frameBuffer.UnlockBits(frameData);
            background.UnlockBits(bgData);
        }
    }
}
