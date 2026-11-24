using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.Products
{
    /// <summary>
    /// RF-08: producto del catálogo base guardado internamente a partir de un
    /// producto externo de Open Food Facts. Identificado de forma única por su
    /// código de barras (Regla de negocio 4 / Invariante 4 del Modelo de Dominio).
    ///
    /// TP05/TP06: barcode, nombre y marca (también se pueden cargar a mano).
    /// TP final (RF-07 / RF-08): datos externos guardados (imagen, categorías,
    /// ingredientes, alérgenos, Nutri-Score y NOVA). Son opcionales: null = el
    /// proveedor no los informó (RF-09).
    /// </summary>
    public class Product : AuditedAggregateRoot<Guid>
    {
        // Las listas (categorías y alérgenos) se guardan como texto separado por comas.
        private const char ListSeparator = ',';

        private static readonly string[] ValidNutriScores = { "a", "b", "c", "d", "e" };

        public virtual string Barcode { get; private set; } = null!;
        public virtual string Name { get; private set; } = null!;
        public virtual string? Brand { get; private set; }

        public virtual string? ImageUrl { get; private set; }

        /// <summary>Categorías separadas por coma. Para leerlas como lista: <see cref="GetCategories"/>.</summary>
        public virtual string? Categories { get; private set; }

        /// <summary>Lista de ingredientes tal como la informa el proveedor. La usa el Frente 3 (RF-12).</summary>
        public virtual string? IngredientsText { get; private set; }

        /// <summary>
        /// Tags de alérgenos separados por coma ("en:milk,en:nuts").
        /// null = no informado; "" = informado, sin alérgenos detectados.
        /// Para leerlos como lista: <see cref="GetAllergenTags"/>.
        /// </summary>
        public virtual string? AllergenTags { get; private set; }

        /// <summary>Nutri-Score de "a" a "e".</summary>
        public virtual string? NutriScore { get; private set; }

        /// <summary>Grupo NOVA de 1 a 4.</summary>
        public virtual int? NovaGroup { get; private set; }

        /// <summary>
        /// Última vez que se guardaron los datos externos. null = nunca.
        /// Se toma del reloj de ABP (IClock), el mismo que usa la auditoría (CreationTime).
        /// </summary>
        public virtual DateTime? ExternalDataUpdatedAt { get; private set; }

        // Requerido por EF Core.
        protected Product() { }

        // Constructor internal: la regla "no duplicar código de barras" necesita
        // consultar el repositorio, por eso vive en ProductManager (Domain Service).
        // Los productos se crean siempre con ProductManager.
        internal Product(
            Guid id,
            string barcode,
            string name,
            string? brand = null
        ) : base(id)
        {
            Barcode = NormalizeBarcode(barcode);
            Name = NormalizeName(name);
            Brand = NormalizeBrand(brand);
        }

        /// <summary>
        /// Modifica los datos editables del producto (TP06).
        ///
        /// Es internal por la misma razón que el constructor: si cambia el barcode
        /// hay que verificar unicidad contra el repositorio, así que la única
        /// entrada es ProductManager.UpdateAsync.
        ///
        /// Primero valida y normaliza TODOS los valores en variables locales y recién
        /// después asigna. Si cualquiera es inválido se lanza la excepción antes de
        /// tocar el estado: la entidad nunca queda modificada a medias.
        /// </summary>
        internal void Update(string barcode, string name, string? brand)
        {
            var normalizedBarcode = NormalizeBarcode(barcode);
            var normalizedName = NormalizeName(name);
            var normalizedBrand = NormalizeBrand(brand);

            Barcode = normalizedBarcode;
            Name = normalizedName;
            Brand = normalizedBrand;
        }

        /// <summary>
        /// RF-08: reemplaza los datos externos guardados por los que informa el proveedor.
        ///
        /// Es internal: la entrada es ProductManager.SaveFromExternalAsync, que además
        /// garantiza que no se duplique el código de barras.
        ///
        /// Igual que <see cref="Update"/>: valida y normaliza todo en variables locales y
        /// recién después asigna, así un dato inválido no deja el producto a medias.
        /// Los valores inválidos (Nutri-Score fuera de a-e, NOVA fuera de 1-4) se
        /// rechazan: convertirlos en null es tarea de quien traduce la respuesta del
        /// proveedor, no del dominio.
        /// </summary>
        internal void UpdateExternalData(ProductExternalData data, DateTime updatedAt)
        {
            Check.NotNull(data, nameof(data));

            var imageUrl = NormalizeImageUrl(data.ImageUrl);
            var categories = JoinCategories(data.Categories);
            var ingredientsText = NormalizeOptionalText(
                data.IngredientsText,
                nameof(data.IngredientsText),
                ProductConsts.MaxIngredientsTextLength);
            var allergenTags = JoinAllergenTags(data.AllergenTags);
            var nutriScore = NormalizeNutriScore(data.NutriScore);
            var novaGroup = ValidateNovaGroup(data.NovaGroup);

            ImageUrl = imageUrl;
            Categories = categories;
            IngredientsText = ingredientsText;
            AllergenTags = allergenTags;
            NutriScore = nutriScore;
            NovaGroup = novaGroup;
            ExternalDataUpdatedAt = updatedAt;
        }

        /// <summary>
        /// Alérgenos como lista. La usa el Frente 3 para las advertencias (RF-12):
        ///   - null: el proveedor no informó alérgenos (la advertencia es "no verificable");
        ///   - lista vacía: informó que no detectó alérgenos;
        ///   - si no, los tags ("en:milk", ...).
        /// </summary>
        public IReadOnlyList<string>? GetAllergenTags()
        {
            return SplitList(AllergenTags);
        }

        /// <summary>Categorías como lista; null si el proveedor no las informó.</summary>
        public IReadOnlyList<string>? GetCategories()
        {
            return SplitList(Categories);
        }

        internal static string NormalizeBarcode(string barcode)
        {
            return Check.NotNullOrWhiteSpace(
                barcode,
                nameof(barcode),
                maxLength: ProductConsts.MaxBarcodeLength
            ).Trim();
        }

        private static string NormalizeName(string name)
        {
            return Check.NotNullOrWhiteSpace(
                name,
                nameof(name),
                maxLength: ProductConsts.MaxNameLength
            ).Trim();
        }

        private static string? NormalizeBrand(string? brand)
        {
            return string.IsNullOrWhiteSpace(brand)
                ? null
                : Check.Length(brand.Trim(), nameof(brand), ProductConsts.MaxBrandLength);
        }

        private static string? NormalizeOptionalText(string? value, string parameterName, int maxLength)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : Check.Length(value.Trim(), parameterName, maxLength);
        }

        private static string? NormalizeImageUrl(string? imageUrl)
        {
            var normalized = NormalizeOptionalText(imageUrl, nameof(imageUrl), ProductConsts.MaxImageUrlLength);

            // La URL se muestra como imagen en la interfaz: solo se aceptan direcciones
            // http(s) absolutas, nunca otros esquemas (por ejemplo "javascript:").
            if (normalized != null
                && (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new ArgumentException("La imagen del producto debe ser una URL http o https.", nameof(imageUrl));
            }

            return normalized;
        }

        private static string? NormalizeNutriScore(string? nutriScore)
        {
            if (string.IsNullOrWhiteSpace(nutriScore))
            {
                return null;
            }

            var normalized = nutriScore.Trim().ToLowerInvariant();

            if (!ValidNutriScores.Contains(normalized))
            {
                throw new ArgumentException("El Nutri-Score debe ser una letra de \"a\" a \"e\".", nameof(nutriScore));
            }

            return normalized;
        }

        private static int? ValidateNovaGroup(int? novaGroup)
        {
            if (novaGroup is < ProductConsts.MinNovaGroup or > ProductConsts.MaxNovaGroup)
            {
                throw new ArgumentException(
                    $"El grupo NOVA debe estar entre {ProductConsts.MinNovaGroup} y {ProductConsts.MaxNovaGroup}.",
                    nameof(novaGroup));
            }

            return novaGroup;
        }

        /// <summary>Sin categorías (null o lista vacía) se guarda null: no hay nada que mostrar.</summary>
        private static string? JoinCategories(IReadOnlyList<string>? categories)
        {
            var values = NormalizeListValues(categories, nameof(categories), toLowerCase: false);

            return values is null || values.Count == 0
                ? null
                : Check.Length(string.Join(ListSeparator, values), nameof(categories), ProductConsts.MaxCategoriesLength);
        }

        /// <summary>
        /// A diferencia de las categorías, una lista vacía se guarda como "" y no como null:
        /// "informado sin alérgenos" y "no informado" no significan lo mismo (RF-12).
        /// </summary>
        private static string? JoinAllergenTags(IReadOnlyList<string>? allergenTags)
        {
            var values = NormalizeListValues(allergenTags, nameof(allergenTags), toLowerCase: true);

            return values is null
                ? null
                : Check.Length(string.Join(ListSeparator, values), nameof(allergenTags), ProductConsts.MaxAllergenTagsLength);
        }

        /// <summary>
        /// Recorta cada valor, descarta los vacíos y los repetidos. Un valor con coma se
        /// rechaza porque rompería la lista guardada al volver a separarla.
        /// </summary>
        private static List<string>? NormalizeListValues(IReadOnlyList<string>? values, string parameterName, bool toLowerCase)
        {
            if (values is null)
            {
                return null;
            }

            var result = new List<string>();

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var normalized = toLowerCase ? value.Trim().ToLowerInvariant() : value.Trim();

                if (normalized.Contains(ListSeparator))
                {
                    throw new ArgumentException($"Los valores de la lista no pueden contener \"{ListSeparator}\".", parameterName);
                }

                if (!result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(normalized);
                }
            }

            return result;
        }

        private static IReadOnlyList<string>? SplitList(string? storedValue)
        {
            if (storedValue is null)
            {
                return null;
            }

            return storedValue.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
