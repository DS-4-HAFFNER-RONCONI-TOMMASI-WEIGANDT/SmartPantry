import {
  Component,
  Directive,
  Pipe,
  PipeTransform,
  TemplateRef,
  contentChild,
  input,
  model,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LIST_QUERY_DEBOUNCE_TIME, LocalizationPipe, PagedResultDto } from '@abp/ng.core';
import {
  ButtonComponent,
  Confirmation,
  ConfirmationService,
  ModalCloseDirective,
  ModalComponent,
  ToasterService,
} from '@abp/ng.theme.shared';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';
import { PantryComponent } from './pantry.component';
import { expirationDateValidator, isValidIsoDate } from './pantry-item.validators';

// ---------------------------------------------------------------------------
// Dobles de prueba: sin red ni servidor. El proxy generado se reemplaza por un
// mock y los componentes visuales de ABP por versiones mínimas.
// ---------------------------------------------------------------------------

/** Devuelve la clave tal cual (más sus parámetros), para comprobar qué texto se usa. */
@Pipe({ name: 'abpLocalization' })
class FakeLocalizationPipe implements PipeTransform {
  transform(key: string, ...params: string[]): string {
    return params.length ? `${key}(${params.join(',')})` : key;
  }
}

@Component({
  selector: 'abp-modal',
  imports: [NgTemplateOutlet],
  template: `
    @if (visible()) {
      <div data-test="modal">
        <ng-container [ngTemplateOutlet]="header() ?? null" />
        <ng-container [ngTemplateOutlet]="body() ?? null" />
        <ng-container [ngTemplateOutlet]="footer() ?? null" />
      </div>
    }
  `,
})
class FakeModalComponent {
  visible = model(false);
  busy = input(false);
  header = contentChild<TemplateRef<unknown>>('abpHeader');
  body = contentChild<TemplateRef<unknown>>('abpBody');
  footer = contentChild<TemplateRef<unknown>>('abpFooter');
}

@Component({
  selector: 'abp-button',
  template: `
    <button
      data-test="save"
      [attr.type]="buttonType()"
      [attr.form]="formName()"
      [disabled]="disabled() || loading()"
    >
      <ng-content />
    </button>
  `,
})
class FakeButtonComponent {
  iconClass = input('');
  buttonType = input('button');
  formName = input<string | undefined>(undefined);
  loading = input(false);
  disabled = input(false);
}

@Directive({ selector: '[abpClose]' })
class FakeModalCloseDirective {}

type PantryItemServiceMock = {
  getList: ReturnType<typeof vi.fn>;
  update: ReturnType<typeof vi.fn>;
  markAsConsumed: ReturnType<typeof vi.fn>;
  delete: ReturnType<typeof vi.fn>;
};

function page(items: PantryItemDto[]): Observable<PagedResultDto<PantryItemDto>> {
  return of({ items, totalCount: items.length });
}

const MILK: PantryItemDto = {
  id: 'item-1',
  productName: 'Leche',
  productBrand: 'Marca A',
  quantity: 2,
  unit: 'l',
  expirationDate: '2026-11-20',
  status: PantryItemStatus.Active,
};

// RF-09: sin cantidad, sin unidad y sin fecha.
const RICE: PantryItemDto = {
  id: 'item-2',
  productName: 'Arroz',
  status: PantryItemStatus.Active,
};

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;
  let service: PantryItemServiceMock;
  let confirmation: { warn: ReturnType<typeof vi.fn> };
  let toaster: { success: ReturnType<typeof vi.fn> };

  async function create(getList: () => Observable<PagedResultDto<PantryItemDto>>) {
    service = {
      getList: vi.fn(getList),
      update: vi.fn(() => of(MILK)),
      markAsConsumed: vi.fn(() => of({ ...MILK, status: PantryItemStatus.Consumed })),
      delete: vi.fn(() => of(undefined)),
    };
    confirmation = { warn: vi.fn(() => of(Confirmation.Status.reject)) };
    toaster = { success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryItemService, useValue: service },
        { provide: ConfirmationService, useValue: confirmation },
        { provide: ToasterService, useValue: toaster },
        // Sin espera entre cambios de página y petición: la prueba es sincrónica.
        { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
      ],
    });
    TestBed.overrideComponent(PantryComponent, {
      remove: { imports: [LocalizationPipe, ModalComponent, ButtonComponent, ModalCloseDirective] },
      add: {
        imports: [FakeLocalizationPipe, FakeModalComponent, FakeButtonComponent, FakeModalCloseDirective],
      },
    });

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function query(selector: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(`[data-test="${selector}"]`);
  }

  function queryAll(selector: string): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll(`[data-test="${selector}"]`));
  }

  function setInput(id: string, value: string): void {
    const input = fixture.nativeElement.querySelector(`#${id}`) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
  }

  // -------------------------------------------------------------------------
  // Cuatro estados
  // -------------------------------------------------------------------------

  it('muestra el estado de carga y NO la lista vacía mientras la respuesta está pendiente', async () => {
    await create(() => NEVER);

    expect(component.state()).toBe('loading');
    expect(query('pantry-loading')).not.toBeNull();
    expect(query('pantry-empty')).toBeNull();
    expect(query('pantry-error')).toBeNull();
  });

  it('muestra el estado de error con opción de reintentar', async () => {
    await create(() => throwError(() => ({ status: 0 })));

    expect(component.state()).toBe('error');
    expect(query('pantry-error')?.textContent).toContain('::Pantry:LoadError');
    expect(query('pantry-retry')).not.toBeNull();
    expect(query('pantry-empty')).toBeNull();
  });

  it('al reintentar vuelve a pedir la lista y muestra los resultados', async () => {
    await create(() => throwError(() => ({ status: 0 })));
    service.getList.mockImplementation(() => page([MILK]));

    query('pantry-retry')!.click();
    fixture.detectChanges();

    expect(service.getList).toHaveBeenCalledTimes(2);
    expect(component.state()).toBe('loaded');
    expect(queryAll('pantry-item').length).toBe(1);
  });

  it('muestra la lista vacía con un texto localizado', async () => {
    await create(() => page([]));

    expect(component.state()).toBe('empty');
    expect(query('pantry-empty')?.textContent?.trim()).toBe('::Pantry:Empty');
    expect(query('pantry-loading')).toBeNull();
  });

  it('muestra los resultados y comunica los datos faltantes sin inventar valores (RF-09)', async () => {
    await create(() => page([MILK, RICE]));

    expect(component.state()).toBe('loaded');
    const items = queryAll('pantry-item');
    expect(items.length).toBe(2);

    const names = queryAll('pantry-item-name').map(e => e.textContent?.trim());
    expect(names).toEqual(['Leche', 'Arroz']);

    const rice = items[1];
    expect(rice.querySelector('[data-test="pantry-item-quantity"]')?.textContent).toContain('::Pantry:NotAvailable');
    expect(rice.querySelector('[data-test="pantry-item-unit"]')?.textContent).toContain('::Pantry:NotAvailable');
    expect(rice.querySelector('[data-test="pantry-item-expiration"]')?.textContent).toContain('::Pantry:NotAvailable');

    const milk = items[0];
    expect(milk.querySelector('[data-test="pantry-item-quantity"]')?.textContent?.trim()).toBe('2');
    expect(milk.querySelector('[data-test="pantry-item-unit"]')?.textContent?.trim()).toBe('l');
    expect(milk.querySelector('[data-test="pantry-item-expiration"]')?.textContent).not.toContain('NotAvailable');
  });

  it('pide la lista al proxy paginada, con los activos y manejando el error en la pantalla', async () => {
    await create(() => page([MILK]));

    expect(service.getList).toHaveBeenCalledWith(
      expect.objectContaining({ skipCount: 0, maxResultCount: 10, status: PantryItemStatus.Active }),
      { skipHandleError: true },
    );
  });

  it('al elegir "Consumidos" pide el historial', async () => {
    await create(() => page([MILK]));

    component.changeFilter('consumed');
    fixture.detectChanges();

    expect(service.getList).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: PantryItemStatus.Consumed }),
      { skipHandleError: true },
    );
  });

  // -------------------------------------------------------------------------
  // Edición
  // -------------------------------------------------------------------------

  it('abre el formulario con los datos precargados', async () => {
    await create(() => page([MILK]));

    query('pantry-edit')!.click();
    fixture.detectChanges();

    expect(query('modal')).not.toBeNull();
    expect(component.form.getRawValue()).toEqual({
      quantity: 2,
      unit: 'l',
      expirationDate: '2026-11-20',
    });
  });

  it('con cantidad negativa muestra el mensaje localizado y NO llama al servidor', async () => {
    await create(() => page([MILK]));
    component.edit(MILK);
    fixture.detectChanges();

    setInput('pantry-quantity', '-1');
    component.save();
    fixture.detectChanges();

    expect(query('error-quantity-min')?.textContent?.trim()).toBe('::Pantry:Validation:QuantityNonNegative');
    expect((query('save') as HTMLButtonElement).disabled).toBe(true);
    expect(service.update).not.toHaveBeenCalled();
  });

  it('con fecha fuera de rango muestra el mensaje localizado y NO llama al servidor', async () => {
    await create(() => page([MILK]));
    component.edit(MILK);
    fixture.detectChanges();

    component.form.controls.expirationDate.setValue('1999-12-31');
    component.form.controls.expirationDate.markAsDirty();
    component.save();
    fixture.detectChanges();

    expect(query('error-expiration-date')?.textContent?.trim()).toBe('::Pantry:Validation:InvalidDate');
    expect(service.update).not.toHaveBeenCalled();
  });

  it('con datos válidos guarda con el proxy, cierra el modal y actualiza la lista', async () => {
    await create(() => page([MILK]));
    component.edit(MILK);
    fixture.detectChanges();
    const callsBefore = service.getList.mock.calls.length;

    setInput('pantry-quantity', '3.5');
    setInput('pantry-unit', '  kg ');
    component.form.controls.expirationDate.setValue('');
    component.save();
    fixture.detectChanges();

    expect(service.update).toHaveBeenCalledWith('item-1', {
      quantity: 3.5,
      unit: 'kg',
      expirationDate: undefined, // vacía = dato no disponible
    });
    expect(component.isModalOpen()).toBe(false);
    expect(service.getList.mock.calls.length).toBe(callsBefore + 1);
  });

  it('si el servidor rechaza los datos, el modal sigue abierto con lo ingresado', async () => {
    await create(() => page([MILK]));
    service.update.mockImplementation(() => throwError(() => ({ status: 400 })));
    component.edit(MILK);
    fixture.detectChanges();

    setInput('pantry-quantity', '7');
    component.save();
    fixture.detectChanges();

    expect(component.isModalOpen()).toBe(true);
    expect(component.form.controls.quantity.value).toBe(7);
    expect(component.saving()).toBe(false);
  });

  it('deshabilita "Guardar" mientras la petición está en curso', async () => {
    await create(() => page([MILK]));
    service.update.mockImplementation(() => NEVER);
    component.edit(MILK);
    fixture.detectChanges();

    component.save();
    fixture.detectChanges();

    expect(component.saving()).toBe(true);
    expect((query('save') as HTMLButtonElement).disabled).toBe(true);
  });

  // -------------------------------------------------------------------------
  // Consumo y eliminación
  // -------------------------------------------------------------------------

  it('marca un ítem como consumido con el proxy y actualiza la lista', async () => {
    await create(() => page([MILK]));

    query('pantry-consume')!.click();
    fixture.detectChanges();

    expect(service.markAsConsumed).toHaveBeenCalledWith('item-1');
    expect(service.getList).toHaveBeenCalledTimes(2);
  });

  it('pide confirmación antes de eliminar y, si se cancela, NO llama al servidor', async () => {
    await create(() => page([MILK]));
    confirmation.warn.mockImplementation(() => of(Confirmation.Status.reject));

    query('pantry-delete')!.click();
    fixture.detectChanges();

    expect(confirmation.warn).toHaveBeenCalledWith(
      '::Pantry:DeleteConfirmationMessage',
      '::Pantry:DeleteConfirmationTitle',
      expect.objectContaining({ messageLocalizationParams: ['Leche'] }),
    );
    expect(service.delete).not.toHaveBeenCalled();
    expect(queryAll('pantry-item').length).toBe(1);
  });

  it('si se confirma, elimina con el proxy y actualiza la lista', async () => {
    await create(() => page([MILK]));
    confirmation.warn.mockImplementation(() => of(Confirmation.Status.confirm));
    service.getList.mockImplementation(() => page([]));

    query('pantry-delete')!.click();
    fixture.detectChanges();

    expect(service.delete).toHaveBeenCalledWith('item-1');
    expect(component.state()).toBe('empty');
  });

  it('un ítem consumido no ofrece editar ni consumir de nuevo', async () => {
    await create(() => page([{ ...MILK, status: PantryItemStatus.Consumed }]));

    expect(query('pantry-item-consumed')).not.toBeNull();
    expect(query('pantry-edit')).toBeNull();
    expect(query('pantry-consume')).toBeNull();
    expect(query('pantry-delete')).not.toBeNull();
  });
});

describe('expirationDateValidator', () => {
  const validate = (value: unknown) =>
    expirationDateValidator()({ value } as never);

  it('acepta vacío (dato no disponible) y fechas reales dentro del rango', () => {
    expect(validate('')).toBeNull();
    expect(validate(null)).toBeNull();
    expect(validate('2026-11-20')).toBeNull();
  });

  it('rechaza fechas inexistentes, mal formadas o fuera de rango', () => {
    expect(isValidIsoDate('2026-02-30')).toBe(false);
    expect(validate('2026-02-30')).toEqual({ invalidDate: true });
    expect(validate('20-11-2026')).toEqual({ invalidDate: true });
    expect(validate('1999-12-31')).toEqual({ invalidDate: true });
    expect(validate('2101-01-01')).toEqual({ invalidDate: true });
  });

  it('rechaza una fecha incompleta que el navegador entrega vacía', () => {
    expect(expirationDateValidator(() => true)({ value: '' } as never)).toEqual({ invalidDate: true });
  });
});
