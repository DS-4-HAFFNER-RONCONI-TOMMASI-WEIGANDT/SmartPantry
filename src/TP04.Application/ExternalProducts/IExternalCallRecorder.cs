using System.Threading.Tasks;

namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05): registra cada llamada al catálogo externo para las métricas.
///
/// Interfaz propia en Application: quien consulta el catálogo no sabe si el registro
/// va a un log (implementación por defecto) o a la base (la agrega el Frente 4).
/// Las implementaciones no deben lanzar excepciones: un fallo al registrar no puede
/// romper la consulta del usuario.
/// </summary>
public interface IExternalCallRecorder
{
    Task RecordAsync(ExternalCallRecord record);
}
