# Plantilla LWC + Apex (referencia de estudio)

Ejemplo con **Contactos de una cuenta**. El proyecto 1 usa **Casos**, así que tienes que adaptarlo, no copiarlo.

## Archivos que necesitas

```
force-app/main/default/classes/ContactoController.cls
force-app/main/default/classes/ContactoController.cls-meta.xml
force-app/main/default/lwc/listaContactos/listaContactos.html
force-app/main/default/lwc/listaContactos/listaContactos.js
force-app/main/default/lwc/listaContactos/listaContactos.js-meta.xml
```

En VS Code: `Ctrl+Shift+P` → **SFDX: Create Apex Class** y **SFDX: Create Lightning Web Component**. Crean los archivos y los `-meta.xml` por ti.

## 1. Apex: el controlador

```apex
public with sharing class ContactoController {

    // LECTURA -> se llama con @wire -> necesita cacheable=true
    @AuraEnabled(cacheable=true)
    public static List<Contact> getContactos(Id cuentaId) {
        return [
            SELECT Id, FirstName, LastName, Email
            FROM Contact
            WHERE AccountId = :cuentaId
            ORDER BY LastName
        ];
    }

    // ESCRITURA (DML) -> se llama de forma imperativa -> SIN cacheable
    @AuraEnabled
    public static void eliminarContacto(Id contactoId) {
        try {
            delete [SELECT Id FROM Contact WHERE Id = :contactoId];
        } catch (Exception e) {
            throw new AuraHandledException(e.getMessage());
        }
    }
}
```

Qué es cada cosa:

| Línea | Para qué sirve |
|---|---|
| `with sharing` | Respeta la visibilidad de registros del usuario |
| `@AuraEnabled` | Permite que el LWC llame al método |
| `cacheable=true` | Solo lectura. Obligatorio para usar `@wire` |
| `static` | Los métodos llamados desde LWC deben ser estáticos |
| `:cuentaId` | Variable vinculada: usa una variable de Apex dentro del SOQL |
| `AuraHandledException` | Manda el mensaje de error al LWC |

## 2. LWC: el JavaScript

```js
import { LightningElement, api, wire } from 'lwc';
import { refreshApex } from '@salesforce/apex';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';
import getContactos from '@salesforce/apex/ContactoController.getContactos';
import eliminarContacto from '@salesforce/apex/ContactoController.eliminarContacto';

export default class ListaContactos extends LightningElement {
    @api recordId;          // Id de la cuenta cuando está en su página de registro
    contactos;
    error;
    resultadoWire;          // se guarda para poder usar refreshApex

    // LECTURA automática (reactiva por el $)
    @wire(getContactos, { cuentaId: '$recordId' })
    wiredContactos(resultado) {
        this.resultadoWire = resultado;          // el resultado COMPLETO
        const { data, error } = resultado;
        if (data) {
            this.contactos = data;
            this.error = undefined;
        } else if (error) {
            this.error = error;
            this.contactos = undefined;
        }
    }

    // Getter: valor calculado para usar en el HTML
    get hayContactos() {
        return this.contactos && this.contactos.length > 0;
    }

    // ESCRITURA imperativa (la llamas tú, al hacer clic)
    async handleEliminar(event) {
        const id = event.target.dataset.id;          // viene de data-id en el HTML
        try {
            await eliminarContacto({ contactoId: id });
            this.mostrarToast('Listo', 'Contacto eliminado', 'success');
            await refreshApex(this.resultadoWire);    // recarga la lista
        } catch (error) {
            this.mostrarToast('Error', error.body.message, 'error');
        }
    }

    mostrarToast(titulo, mensaje, variante) {
        this.dispatchEvent(new ShowToastEvent({
            title: titulo,
            message: mensaje,
            variant: variante
        }));
    }
}
```

## 3. LWC: el HTML

```html
<template>
    <lightning-card title="Contactos" icon-name="standard:contact">
        <div class="slds-var-p-around_medium">
            <template lwc:if={hayContactos}>
                <template for:each={contactos} for:item="c">
                    <div key={c.Id} class="slds-var-m-bottom_small">
                        <strong>{c.FirstName} {c.LastName}</strong>
                        <lightning-button
                            variant="destructive-text"
                            label="Eliminar"
                            data-id={c.Id}
                            onclick={handleEliminar}>
                        </lightning-button>
                    </div>
                </template>
            </template>
            <template lwc:else>
                <p>No hay contactos.</p>
            </template>
        </div>
    </lightning-card>
</template>
```

## 4. LWC: el meta XML

```xml
<?xml version="1.0" encoding="UTF-8"?>
<LightningComponentBundle xmlns="http://soap.sforce.com/2006/04/metadata">
    <apiVersion>67.0</apiVersion>
    <isExposed>true</isExposed>
    <masterLabel>Lista de Contactos</masterLabel>
    <targets>
        <target>lightning__AppPage</target>
        <target>lightning__HomePage</target>
        <target>lightning__RecordPage</target>
    </targets>
</LightningComponentBundle>
```

## 5. Cuál herramienta uso

| Necesito... | Uso |
|---|---|
| Leer datos al abrir y que se recarguen solos | `@wire` (método con `cacheable=true`) |
| Guardar, crear, borrar o actualizar | Llamada imperativa (método **sin** `cacheable`) |
| Recibir un dato del padre | `@api` en el hijo |
| Avisar al padre | `CustomEvent` |
| Volver a cargar un `@wire` | `refreshApex(resultadoCompleto)` |

## 6. Desplegar y probar

```powershell
# Apex primero, LWC después
sf project deploy start --source-dir force-app/main/default/classes --target-org vscodeOrg
sf project deploy start --source-dir force-app/main/default/lwc/listaContactos --target-org vscodeOrg

# Probar Apex suelto con un archivo de Apex anónimo
sf apex run --file scripts/apex/hello.apex --target-org vscodeOrg

# Abrir la org
sf org open --target-org vscodeOrg
```

En la org: engranaje → **Editar página** → arrastra el componente desde la lista "Custom" → **Guardar** y **Activar**.

## 7. Orden de trabajo para el ejercicio del examen

1. Lee **todos** los criterios y subraya los verbos (mostrar, filtrar, guardar, validar).
2. Escribe primero **Apex** y despliégalo.
3. Pruébalo con Apex anónimo (`System.debug(MiClase.metodo())`).
4. Escribe el **JS** del LWC.
5. Escribe el **HTML**.
6. Revisa el `.js-meta.xml` (`isExposed` y `targets`).
7. Despliega, agrégalo a una página y prueba cada criterio.
8. Relee los criterios uno por uno con el componente abierto.

## 8. Errores comunes

| Síntoma | Causa probable |
|---|---|
| El LWC no encuentra el método | Falta `@AuraEnabled`, o no desplegaste la clase |
| Error al usar un método con `@wire` | Le falta `cacheable=true` |
| Error al hacer DML con `cacheable=true` | Los métodos cacheables no pueden hacer DML |
| El wire no reacciona al cambiar `recordId` | Falta el `$`: `'$recordId'` |
| `Cannot read properties of undefined` | Los datos aún no llegaron: usa `lwc:if` o un getter |
| La lista no muestra nada | Falta `key` en el primer elemento del `for:each` |
| El componente no aparece en el App Builder | `isExposed` en false o falta el `target` |
| Los datos no se actualizan tras guardar | Falta `refreshApex` con el resultado **completo** del wire |
