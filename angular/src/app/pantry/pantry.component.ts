import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ABP, ListService, LocalizationPipe, PagedResultDto } from '@abp/ng.core';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';
import { EMPTY, Observable, catchError, tap } from 'rxjs';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';

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
 * - Que cada usuario vea sólo sus ítems lo garantiza el servidor (ICurrentUser).
 */
@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, LocalizationPipe, NgbPagination],
  providers: [ListService],
})
export class PantryComponent {
  readonly list = inject(ListService);
  private readonly pantryItemService = inject(PantryItemService);

  readonly status = PantryItemStatus;

  readonly state = signal<PantryViewState>('loading');
  readonly result = signal<PantryPage>({ items: [], totalCount: 0 });
  readonly filter = signal<PantryFilter>('active');

  constructor() {
    // ListService arma la consulta (página, tamaño, orden) y vuelve a pedir la lista
    // cada vez que se llama a list.get() o cambia la página.
    this.list
      .hookToQuery(query => this.loadPage(query))
      .pipe(takeUntilDestroyed())
      .subscribe();
  }

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

    // Página vacía que no es la primera: se vuelve a la anterior.
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
}
