import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ABP, ListService, LocalizationPipe, PagedResultDto } from '@abp/ng.core';
import {
  ButtonComponent,
  Confirmation,
  ConfirmationService,
  ModalCloseDirective,
  ModalComponent,
  ToasterService,
} from '@abp/ng.theme.shared';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';
import { EMPTY, Observable, catchError, finalize, tap } from 'rxjs';
import {
  PantryItemDto,
  PantryItemService,
  PantryItemStatus,
  UpdatePantryItemDto,
} from '../proxy/pantry-items';
import { PANTRY_ITEM_RULES, expirationDateValidator } from './pantry-item.validators';

/** Estados de la pantalla: carga, error, lista vacía y resultados. */
export type PantryViewState = 'loading' | 'error' | 'empty' | 'loaded';

/** Qué ítems se muestran. Los consumidos se conservan como historial (regla 6). */
export type PantryFilter = 'active' | 'consumed' | 'all';

/** Página que se muestra (totalCount siempre definido). */
export interface PantryPage {
  items: PantryItemDto[];
  totalCount: number;
}

/**
 * TP10 - Pantalla "Mi despensa" (RF-11, con RF-09 para los datos faltantes).
 *
 * - Consume el BackEnd sólo con el proxy generado (PantryItemService). No hay tipos
 *   ni llamadas HttpClient propias para la despensa.
 * - El listado es paginado en el servidor, por eso se usa ListService.
 * - Que cada usuario vea y modifique sólo sus ítems lo garantiza el servidor
 *   (ICurrentUser + 404 para ítems ajenos). Ocultar botones no es seguridad.
 */
@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    DecimalPipe,
    LocalizationPipe,
    ModalComponent,
    ModalCloseDirective,
    ButtonComponent,
    NgbPagination,
  ],
  providers: [ListService],
})
export class PantryComponent {
  readonly list = inject(ListService);
  private readonly pantryItemService = inject(PantryItemService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly fb = inject(FormBuilder);

  readonly status = PantryItemStatus;
  readonly rules = PANTRY_ITEM_RULES;

  // --- Listado -------------------------------------------------------------
  readonly state = signal<PantryViewState>('loading');
  readonly result = signal<PantryPage>({ items: [], totalCount: 0 });
  readonly filter = signal<PantryFilter>('active');
  /** Ítem con una acción (consumir / eliminar) en curso, para deshabilitar sus botones. */
  readonly busyItemId = signal<string | null>(null);

  // --- Edición ---------------------------------------------------------------
  readonly isModalOpen = signal(false);
  readonly saving = signal(false);
  readonly selectedItem = signal<PantryItemDto | null>(null);
  private dateBadInput = false;

  readonly form = this.fb.group({
    quantity: this.fb.control<number | null>(null, [
      Validators.min(0),
      Validators.max(PANTRY_ITEM_RULES.maxQuantity),
    ]),
    unit: this.fb.control<string>('', [Validators.maxLength(PANTRY_ITEM_RULES.maxUnitLength)]),
    expirationDate: this.fb.control<string>('', [
      expirationDateValidator(() => this.dateBadInput),
    ]),
  });

  constructor() {
    // ListService arma la consulta (página, tamaño, orden) y vuelve a pedir la lista
    // cada vez que se llama a list.get() o cambia la página.
    this.list
      .hookToQuery(query => this.loadPage(query))
      .pipe(takeUntilDestroyed())
      .subscribe();
  }

  // --- Listado -------------------------------------------------------------

  changeFilter(value: string): void {
    this.filter.set(value as PantryFilter);
    this.list.page = 0;
    this.list.get();
  }

  onPageChange(page: number): void {
    this.list.page = page - 1; // ngb-pagination empieza en 1, ListService en 0
  }

  retry(): void {
    this.list.get();
  }

  private loadPage(query: ABP.PageQueryParams): Observable<PagedResultDto<PantryItemDto>> {
    this.state.set('loading');

    // skipHandleError: el error del listado lo muestra la propia pantalla (estado de
    // error con "Reintentar"). Sin esto, ABP además abriría su diálogo de error.
    return this.pantryItemService
      .getList(
        {
          skipCount: query.skipCount ?? 0,
          maxResultCount: query.maxResultCount ?? this.list.maxResultCount,
          sorting: query.sorting,
          status: this.statusForFilter(),
        },
        { skipHandleError: true },
      )
      .pipe(
        tap(result => this.showResult(result)),
        catchError(() => {
          this.state.set('error');
          return EMPTY;
        }),
      );
  }

  private showResult(result: PagedResultDto<PantryItemDto>): void {
    const items = result.items ?? [];
    const totalCount = result.totalCount ?? 0;

    // Si se eliminó o consumió el último ítem de una página que no es la primera,
    // se vuelve a la página anterior (sigue el estado de carga).
    if (!items.length && totalCount > 0 && this.list.page > 0) {
      this.list.page = this.list.page - 1;
      return;
    }

    this.result.set({ items, totalCount });
    this.state.set(totalCount > 0 ? 'loaded' : 'empty');
  }

  private statusForFilter(): PantryItemStatus | undefined {
    switch (this.filter()) {
      case 'active':
        return PantryItemStatus.Active;
      case 'consumed':
        return PantryItemStatus.Consumed;
      default:
        return undefined;
    }
  }

  // --- Edición -------------------------------------------------------------

  edit(item: PantryItemDto): void {
    this.selectedItem.set(item);
    this.dateBadInput = false;
    this.form.reset({
      quantity: item.quantity ?? null,
      unit: item.unit ?? '',
      expirationDate: item.expirationDate ?? '',
    });
    this.isModalOpen.set(true);
  }

  onDateInput(input: HTMLInputElement): void {
    this.dateBadInput = input.validity?.badInput ?? false;
    this.form.controls.expirationDate.updateValueAndValidity();
  }

  showError(controlName: 'quantity' | 'unit' | 'expirationDate', error: string): boolean {
    const control = this.form.controls[controlName];
    return control.hasError(error) && (control.dirty || control.touched);
  }

  save(): void {
    const item = this.selectedItem();
    if (!item?.id || this.saving()) {
      return;
    }

    // Datos inválidos: se muestran los mensajes y NO se llama al servidor.
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.pantryItemService
      .update(item.id, this.toUpdateDto())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.form.markAsPristine();
          this.isModalOpen.set(false);
          this.toaster.success('::Pantry:SavedSuccessfully');
          this.list.get();
        },
        // Si el servidor rechaza los datos, el manejador de errores de ABP muestra su
        // mensaje. El modal sigue abierto con lo que el usuario ingresó.
        error: () => undefined,
      });
  }

  private toUpdateDto(): UpdatePantryItemDto {
    const { quantity, unit, expirationDate } = this.form.getRawValue();
    const trimmedUnit = unit?.trim();

    // Campo vacío = dato no disponible (RF-09): se envía sin valor y el servidor
    // guarda null. No se inventa un valor por defecto.
    return {
      quantity: quantity === null || quantity === undefined ? undefined : Number(quantity),
      unit: trimmedUnit ? trimmedUnit : undefined,
      expirationDate: expirationDate ? expirationDate : undefined,
    };
  }

  // --- Consumo y eliminación -------------------------------------------------

  markAsConsumed(item: PantryItemDto): void {
    if (!item.id || this.busyItemId()) {
      return;
    }

    this.busyItemId.set(item.id);
    this.pantryItemService
      .markAsConsumed(item.id)
      .pipe(finalize(() => this.busyItemId.set(null)))
      .subscribe({
        next: () => {
          this.toaster.success('::Pantry:ConsumedSuccessfully', undefined, {
            messageLocalizationParams: [item.productName ?? ''],
          });
          this.list.get();
        },
        error: () => undefined, // mensaje del manejador de errores de ABP
      });
  }

  delete(item: PantryItemDto): void {
    if (!item.id || this.busyItemId()) {
      return;
    }

    const id = item.id;
    this.confirmation
      .warn('::Pantry:DeleteConfirmationMessage', '::Pantry:DeleteConfirmationTitle', {
        messageLocalizationParams: [item.productName ?? ''],
        yesText: '::Pantry:Delete',
        cancelText: '::Pantry:Cancel',
      })
      .subscribe(status => {
        // Cancelar o cerrar el diálogo: no se invoca al servidor.
        if (status !== Confirmation.Status.confirm) {
          return;
        }

        this.busyItemId.set(id);
        this.pantryItemService
          .delete(id)
          .pipe(finalize(() => this.busyItemId.set(null)))
          .subscribe({
            next: () => {
              this.toaster.success('::Pantry:DeletedSuccessfully');
              this.list.get();
            },
            error: () => undefined, // mensaje del manejador de errores de ABP
          });
      });
  }
}
