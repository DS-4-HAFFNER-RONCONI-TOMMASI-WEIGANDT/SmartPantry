namespace TP04.PantryItems
{
    /// <summary>
    /// Resumen de una ejecución de ExpirationWarningManager.ProcessAsync.
    /// Se usa para el log del worker y para comprobar la idempotencia en las
    /// pruebas: una segunda ejecución sobre los mismos datos da TotalChanges = 0.
    /// </summary>
    public class ExpirationWarningProcessResult
    {
        public int Created { get; set; }

        public int Updated { get; set; }

        public int Reactivated { get; set; }

        public int Deactivated { get; set; }

        public int Unchanged { get; set; }

        public int TotalChanges => Created + Updated + Reactivated + Deactivated;

        public override string ToString()
        {
            return $"creadas={Created}, actualizadas={Updated}, reactivadas={Reactivated}, " +
                   $"desactivadas={Deactivated}, sin cambios={Unchanged}";
        }
    }
}
