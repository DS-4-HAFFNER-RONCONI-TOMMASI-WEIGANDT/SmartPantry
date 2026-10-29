namespace TP04.PantryItems
{
    /// <summary>
    /// Tipo de advertencia asociada a un ítem de despensa.
    ///
    /// Forma parte de la clave lógica de la advertencia: (PantryItemId, Type).
    /// Por ahora sólo existe Expiration (RF-15). Cuando se implemente RF-12
    /// (alérgenos/preferencias) se agrega un nuevo valor sin cambiar la clave.
    /// </summary>
    public enum PantryWarningType
    {
        Expiration = 1
    }
}
