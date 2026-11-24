namespace TP04.Products
{
    /// <summary>
    /// RF-08: resultado de <see cref="ProductManager.SaveFromExternalAsync"/>.
    ///
    /// <see cref="AlreadyExisted"/> le dice a quien llama qué hacer con el producto:
    /// si es nuevo lo inserta y si ya existía lo actualiza. Así nunca se inserta un
    /// segundo producto con el mismo código de barras (regla de negocio 4).
    /// </summary>
    public class ExternalProductSaveResult
    {
        public ExternalProductSaveResult(Product product, bool alreadyExisted)
        {
            Product = product;
            AlreadyExisted = alreadyExisted;
        }

        public Product Product { get; }

        /// <summary>true si ya había un producto con ese código; false si se creó uno nuevo.</summary>
        public bool AlreadyExisted { get; }
    }
}
