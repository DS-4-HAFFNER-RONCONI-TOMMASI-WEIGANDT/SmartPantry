namespace TP04;

public static class TP04DomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */

    public const string ProductDuplicateBarcode = "TP04:Product:DuplicateBarcode";

    // TP10 (RF-11): reglas del ítem de despensa.
    public const string PantryItemNegativeQuantity = "TP04:PantryItem:NegativeQuantity";
    public const string PantryItemAlreadyConsumed = "TP04:PantryItem:AlreadyConsumed";
}
