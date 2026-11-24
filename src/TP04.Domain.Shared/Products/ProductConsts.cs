namespace TP04.Products
{
    public static class ProductConsts
    {
        public const int MaxBarcodeLength = 50;
        public const int MaxNameLength = 150;
        public const int MaxBrandLength = 100;

        // RF-07 / RF-08: datos externos guardados desde Open Food Facts. Los límites son
        // amplios porque el texto viene del proveedor (ingredientes y categorías pueden
        // ser largos); sirven para que la base no reciba valores sin control.
        public const int MaxImageUrlLength = 2048;
        public const int MaxCategoriesLength = 2000;
        public const int MaxIngredientsTextLength = 10000;
        public const int MaxAllergenTagsLength = 1000;

        /// <summary>Nutri-Score: una sola letra, de "a" a "e".</summary>
        public const int NutriScoreLength = 1;

        public const int MinNovaGroup = 1;
        public const int MaxNovaGroup = 4;
    }
}
