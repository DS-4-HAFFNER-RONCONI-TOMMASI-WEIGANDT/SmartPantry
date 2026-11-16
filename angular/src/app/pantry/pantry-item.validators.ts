import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Reglas de edición de un ítem de despensa (RF-11).
 *
 * Repiten las del servidor (PantryItemConsts / UpdatePantryItemDto) sólo por
 * usabilidad: el BackEnd vuelve a validar y es quien decide.
 */
export const PANTRY_ITEM_RULES = {
  maxQuantity: 999999,
  maxUnitLength: 20,
  minExpirationDate: '2000-01-01',
  maxExpirationDate: '2100-12-31',
} as const;

/** true si el texto es una fecha calendario real con formato yyyy-MM-dd. */
export function isValidIsoDate(value: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return false;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));

  // Si el día no existe (por ejemplo 2026-02-30), Date lo corre al mes siguiente.
  return (
    date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day
  );
}

/**
 * Fecha de vencimiento opcional: vacía es válida (RF-09, dato no disponible).
 * Si tiene valor, debe ser una fecha real dentro del rango aceptado por el servidor.
 *
 * isBadInput informa si el <input type="date"> quedó con una fecha incompleta:
 * en ese caso el navegador entrega '' y sin esta marca se tomaría como "sin fecha".
 */
export function expirationDateValidator(isBadInput: () => boolean = () => false): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (isBadInput()) {
      return { invalidDate: true };
    }

    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    if (typeof value !== 'string' || !isValidIsoDate(value)) {
      return { invalidDate: true };
    }

    // Con formato yyyy-MM-dd la comparación de texto respeta el orden de las fechas.
    if (value < PANTRY_ITEM_RULES.minExpirationDate || value > PANTRY_ITEM_RULES.maxExpirationDate) {
      return { invalidDate: true };
    }

    return null;
  };
}
