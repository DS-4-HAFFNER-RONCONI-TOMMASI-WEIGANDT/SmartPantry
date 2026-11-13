using System;
using System.ComponentModel.DataAnnotations;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10 (RF-10): agregar un producto guardado a la despensa propia. El propietario
    /// NO viaja en el DTO: lo fija el servidor con el usuario autenticado.
    ///
    /// La pantalla de alta queda fuera del alcance del TP10; la operación se expone para
    /// completar el contrato y para preparar datos de prueba desde Swagger.
    /// </summary>
    public class CreatePantryItemDto : PantryItemDetailsDto
    {
        [Required]
        public Guid ProductId { get; set; }
    }
}
