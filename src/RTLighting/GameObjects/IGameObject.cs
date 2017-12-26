using System.Drawing;
using RTLighting.Utilities;

namespace RTLighting.GameObjects
{
    interface IGameObject
    {
        void Update(GameTime gameTime, Controller controller);

        void Draw(Graphics graphics);
    }
}
