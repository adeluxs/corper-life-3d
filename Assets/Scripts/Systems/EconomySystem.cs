namespace CorperLife
{
    /// <summary>All money in and out goes through here so it is logged for the in-game bank app.</summary>
    public static class EconomySystem
    {
        // In-game amounts. Labelled as such in the store listing; they don't have to match real life exactly.
        public const int FederalAllawee = 77000;
        public const int CampStartingCashRich = 60000;
        public const int CampStartingCashAverage = 25000;
        public const int CampStartingCashStruggling = 9000;

        public static int StartingCash(string background)
        {
            switch (background)
            {
                case "Rich": return CampStartingCashRich;
                case "Struggling": return CampStartingCashStruggling;
                default: return CampStartingCashAverage;
            }
        }

        public static bool CanAfford(GameState s, int amount) => s.money >= amount;

        public static bool TrySpend(GameState s, int amount, string label)
        {
            if (amount <= 0) return true;
            if (s.money < amount) return false;
            s.money -= amount;
            Log(s, -amount, label);
            return true;
        }

        public static void Earn(GameState s, int amount, string label)
        {
            if (amount == 0) return;
            s.money += amount;
            if (s.money < 0) s.money = 0;
            Log(s, amount, label);
        }

        static void Log(GameState s, int amount, string label)
        {
            s.transactions.Add(new Transaction
            {
                day = s.day,
                minute = (int)s.minuteOfDay,
                amount = amount,
                label = label
            });
            if (s.transactions.Count > 200) s.transactions.RemoveAt(0);
        }
    }
}
