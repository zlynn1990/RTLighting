using System.Collections.Generic;
using RTLighting.Primatives;

namespace RTLighting.GameObjects
{
    interface IRayEmitter
    {
        IEnumerable<Ray> CastRays();
    }
}
