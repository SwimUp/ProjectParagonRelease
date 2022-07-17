using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DankPyon
{
    class BiomeWorker_AncientForest : BiomeWorker_TemperateForest
    {
        public override float GetScore(Tile tile, int id)
        {
            if (tile.WaterCovered)
            {
                return -100f;
            }
            if (tile.temperature < -10f)
            {
                return 0f;
            }
            if (tile.rainfall < 600f)
            {
                return 0f;
            }
            return 15f + (tile.temperature - 7f) + (tile.rainfall - 600f) / 180f;
        }
    }
}
