using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RTLighting.Primatives;

namespace RTLighting.Rendering
{
    class RayTracer
    {
        public volatile int RaysCasted;

        private readonly Cell[,] _grid;

        private readonly int _gridWidth;
        private readonly int _gridHeight;

        private const int RandomBounceSize = 5000;
        private const int BounceSafeSize = 4500;
        private int _bounceIndex;
        private float[] _randomBounceFactors;

        public RayTracer(Cell[,] grid)
        {
            _grid = grid;

            _gridWidth = _grid.GetLength(1);
            _gridHeight = _grid.GetLength(0);

            InitializeRandomBounceFactors();
        }

        private void InitializeRandomBounceFactors()
        {
            var random = new Random();

            _randomBounceFactors = new float[RandomBounceSize];

            for (int i = 0; i < RandomBounceSize; i++)
            {
                _randomBounceFactors[i] = (float)(random.NextDouble() * 0.4 - 0.2f);
            }
        }

        public void Cast(List<Ray> initialRays)
        {
            RaysCasted = 0;

            //for (int i = 0; i < initialRays.Count; i++)
            Parallel.For(0, initialRays.Count, i =>
            {
                if (_bounceIndex > BounceSafeSize)
                {
                    _bounceIndex = 0;
                }

                Ray ray = initialRays[i];

                int cellX = (int)(ray.X / Constants.CELL_SIZE);
                int cellY = (int)(ray.Y / Constants.CELL_SIZE);

                float tX;
                float tY;

                float dTx;
                float dTy;

                while (ray.Depth < Constants.RAY_DEPTH)
                {
                    RaysCasted++;

                    // Initialize DDA values
                    if (ray.Vx < 0)
                    {
                        dTx = -_gridWidth / ray.Vx;
                        tX = (cellX * Constants.CELL_SIZE - ray.X) / ray.Vx;
                    }
                    else
                    {
                        dTx = _gridWidth / ray.Vx;
                        tX = ((cellX + 1) * Constants.CELL_SIZE - ray.X) / ray.Vx;
                    }

                    if (ray.Vy < 0)
                    {
                        dTy = -_gridHeight / ray.Vy;
                        tY = (cellY * Constants.CELL_SIZE - ray.Y) / ray.Vx;
                    }
                    else
                    {
                        dTy = _gridHeight / ray.Vy;
                        tY = ((cellY + 1) * Constants.CELL_SIZE - ray.Y) / ray.Vy;
                    }

                    tX += dTx;
                    tY += dTy;

                    // DDA iteration
                    while (true)
                    {
                        bool hitHorizontal;

                        if (tX < tY)
                        {
                            tX += dTx;

                            if (ray.Vx < 0)
                            {
                                cellX++;
                            }
                            else
                            {
                                cellX--;
                            }

                            hitHorizontal = true;
                        }
                        else
                        {
                            tY += dTy;

                            if (ray.Vy < 0)
                            {
                                cellY--;
                            }
                            else
                            {
                                cellY++;
                            }

                            hitHorizontal = false;
                        }

                        if (cellX < 0 || cellX > _gridWidth - 1 ||
                            cellY < 0 || cellY > _gridHeight - 1)
                        {
                            ray.Depth = Constants.RAY_DEPTH;
                            break;
                        }

                        Cell currentCell = _grid[cellY, cellX];

                        // Collision detection
                        if (currentCell.IsSolid)
                        {
                            ray.X = cellX * Constants.CELL_SIZE;
                            ray.Y = cellY * Constants.CELL_SIZE;

                            ray.Intensity *= currentCell.Emissivity;
                            ray.Depth++;

                            if (hitHorizontal)
                            {
                                ray.Vx = -ray.Vx + _randomBounceFactors[_bounceIndex++];
                                ray.Vy += _randomBounceFactors[_bounceIndex++];

                                if (ray.Vx < 0)
                                {
                                    ray.X -= Constants.CELL_SIZE;
                                }
                                else
                                {
                                    ray.X += Constants.CELL_SIZE;
                                }
                            }
                            else
                            {
                                ray.Vx += _randomBounceFactors[_bounceIndex++];
                                ray.Vy = -ray.Vy + _randomBounceFactors[_bounceIndex++];

                                if (ray.Vy < 0)
                                {
                                    ray.Y -= Constants.CELL_SIZE;
                                }
                                else
                                {
                                    ray.Y += Constants.CELL_SIZE;
                                }
                            }

                            break;
                        }

                        currentCell.Intensity += ray.Intensity;
                    }
                }
            });
        }
    }
}
