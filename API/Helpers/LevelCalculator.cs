namespace API.Helpers
{
    public static class LevelCalculator
    {
        // level = 1 + floor(xp / 500), xpToNextLevel = level * 500 - xp

        public static (int level, int xpToNextLevel) Calculate(int xp)
        {
            int level = 1 + (int)Math.Floor(xp / 500.0);
            int xpToNextLevel = (level * 500) - xp;

            return (level, xpToNextLevel);
        }
    }
}
