namespace RTLighting.Primatives
{
    class Cell
    {
        public bool IsSolid;

        public float Emissivity;

        public float Intensity;

        public void Reset()
        {
            Intensity = 0;
        }
    }
}
