namespace TP04.PantryItems
{
    /// <summary>
    /// Estado de un ítem de despensa (RF-11: "marca consumido").
    ///
    /// Regla de negocio 6: los productos consumidos NO se eliminan automáticamente;
    /// se conservan como historial con estado Consumed.
    /// </summary>
    public enum PantryItemStatus
    {
        Active = 0,
        Consumed = 1
    }
}
