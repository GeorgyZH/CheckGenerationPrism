namespace CheckGenerationPrism
{
    public static class CheckIntersectionPrismService
    {
        public static bool Check(List<PrismModel> prisms, PrismModel pm, bool canAdd)
        {
            if (prisms.Count == 0)
                return true;

            bool flag = true;

            foreach (var prism in prisms)
            {
                if (prism == pm)
                    continue;
                var t = pm.IsPrismIntersection(prism);
                if (t)
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }
    }
}
