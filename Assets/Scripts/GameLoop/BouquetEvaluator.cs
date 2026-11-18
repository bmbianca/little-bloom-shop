// Compares the flowers in the bouquet with what the order asks for.
// 3 stars = every flower fits, 2 stars = at least half fit, 1 star = less than half.
public static class BouquetEvaluator
{
    public static int CountFlowers(Bouquet bouquet)
    {
        int count = 0;
        for (int i = 0; i < bouquet.flowers.Count; i++)
        {
            if (bouquet.flowers[i] != null) count++;
        }
        return count;
    }

    public static int CountMatches(Bouquet bouquet, OrderData order)
    {
        int matches = 0;
        for (int i = 0; i < bouquet.flowers.Count; i++)
        {
            FlowerItem flower = bouquet.flowers[i];
            if (flower != null && IsGoodFlower(flower, order)) matches++;
        }
        return matches;
    }

    public static int Stars(Bouquet bouquet, OrderData order)
    {
        int total = CountFlowers(bouquet);
        if (total == 0) return 1;

        int matches = CountMatches(bouquet, order);
        if (matches == total) return 3;
        if (matches * 2 >= total) return 2;
        return 1;
    }

    static bool IsGoodFlower(FlowerItem flower, OrderData order)
    {
        for (int i = 0; i < order.goodFlowers.Length; i++)
        {
            string wanted = order.goodFlowers[i];
            string wantedName = wanted;
            string wantedColor = "";

            int slash = wanted.IndexOf('/');
            if (slash >= 0)
            {
                wantedName = wanted.Substring(0, slash);
                wantedColor = wanted.Substring(slash + 1);
            }

            bool sameName = Same(flower.flowerName, wantedName);
            bool sameColor = wantedColor == "" || Same(flower.colorName, wantedColor);
            if (sameName && sameColor) return true;
        }
        return false;
    }

    static bool Same(string a, string b)
    {
        return a.Trim().ToLower() == b.Trim().ToLower();
    }

    // Plain characters, so the default TextMeshPro font can show them
    public static string StarText(int stars)
    {
        string text = "";
        for (int i = 0; i < 3; i++)
        {
            if (i < stars) text += "* ";
            else text += "- ";
        }
        return "[ " + text + "]";
    }
}
