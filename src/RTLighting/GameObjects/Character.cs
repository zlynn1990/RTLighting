using System;
using System.Collections.Generic;
using System.Drawing;
using RTLighting.Primatives;
using RTLighting.Utilities;

namespace RTLighting.GameObjects
{
    class Character : IGameObject, IRayEmitter
    {
        private Vector2 _position;

        private readonly Bitmap _texture;

        public Character(Vector2 start)
        {
            _position = start;

           _texture = new Bitmap("Content/cubeBot.png");
        }

        public IEnumerable<Ray> CastRays()
        {
            var rays = new List<Ray>(6000);

            var random = new Random();

            for (int i = 0; i < 6000; i++)
            {
                double rayAngle = random.NextDouble() - 0.5f;

                rays.Add(new Ray
                {
                    X = (_position.X + Constants.CELL_SIZE),
                    Y = (_position.Y + random.Next(1, 6) * Constants.CELL_SIZE),
                    Vx = (float)Math.Cos(rayAngle),
                    Vy = (float)Math.Sin(rayAngle),
                    Intensity = 0.02f
                });
            }

            return rays;
        }

        public void Update(GameTime gameTime, Controller controller)
        {
            _position += controller.LeftStick * 10;
        }

        public void Draw(Graphics graphics)
        {
            graphics.DrawImage(_texture, _position.X, _position.Y, _texture.Width, _texture.Height);
        }
    }
}
