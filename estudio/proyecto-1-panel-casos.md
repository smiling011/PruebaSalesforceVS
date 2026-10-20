# Proyecto 1: Panel de casos conectado a Apex

Es el primero de un proyecto por día. Tienes el día completo, igual que en el examen; apunta a terminar en 3 horas. Usa `estudio/plantilla-lwc-apex.md` como referencia, pero **no la copies**: aquí el objeto es Case, no Contact.

## Punto de partida

El componente `panelCasos` ya existe en `force-app/main/default/lwc/panelCasos`. Muestra tres números fijos en 0 y un botón de prueba. El trabajo es conectarlo a datos reales.

## Datos de prueba

Antes de empezar, crea casos en tu org con `scripts/apex/datos-casos.apex`:

```powershell
sf apex run --file scripts/apex/datos-casos.apex --target-org vscodeOrg
```

## Criterios (como los que te dará el examen)

**Apex**

1. Crear la clase `CasoController` con `with sharing`.
2. Método `getResumen()` que devuelva el total de casos, los abiertos y los cerrados. Debe poder usarse con `@wire`.
3. Método `getCasos()` que devuelva los 10 casos más recientes con `CaseNumber`, `Subject`, `Status` y `Priority`. Debe poder usarse con `@wire`.
4. Método `cerrarCaso(Id casoId)` que cambie el estado del caso a `Closed`. Si falla, debe devolver un mensaje de error al componente.
5. Sin SOQL ni DML dentro de bucles.

**LWC**

6. Reemplazar los tres `0` del HTML por los valores reales de `getResumen()`.
7. Mostrar la lista de casos con `for:each` y `key`.
8. Cada caso muestra una etiqueta: "Cerrado" si su estado es `Closed` y "Abierto" en cualquier otro caso.
9. Cada caso abierto tiene un botón "Cerrar" que llama a `cerrarCaso` de forma imperativa. Los casos cerrados no muestran el botón.
10. Al cerrar un caso: mostrar un toast de éxito (o de error si falla) y refrescar la lista y los tres números.
11. Si no hay casos, mostrar el texto "No hay casos".
12. Eliminar el botón "Probar componente" y el mensaje de prueba.

**Bonus**

13. Un filtro (todos, abiertos, cerrados) que cambie la lista sin volver a llamar a Apex.
14. Una clase de prueba de Apex con al menos 75% de cobertura.

## Pistas (sin darte el código)

- Para el resumen puedes usar tres consultas `SELECT COUNT() ...` y devolver un `Map<String, Integer>`. El campo `IsClosed` de Case indica si está cerrado.
- Para los tres números, `@wire` sobre una función te deja guardar `data`.
- Para decidir qué mostrar por caso, un getter no sirve dentro de un `for:each`. Piensa en agregar una propiedad calculada al mapear la lista antes de guardarla, o en `lwc:if` dentro del bucle comparando con el valor.
- Para refrescar dos `@wire` distintos necesitas guardar el resultado completo de cada uno.
- El botón sabe qué caso es gracias a `data-id`.

## Cómo verificar cada criterio

| Criterio | Cómo lo compruebas |
|---|---|
| 2 y 3 | Anónimo: `System.debug(CasoController.getResumen());` y `System.debug(CasoController.getCasos());` |
| 4 | Cierra un caso desde la pantalla y mira el estado en la org |
| 6 a 8 | Abre el componente y compara con los casos de la org |
| 9 y 10 | Cierra un caso y observa el toast y los números |
| 11 | Cierra todos los casos o filtra una lista vacía |

## Entrega

Cuando termines, avísame y reviso tu código contra los criterios: te digo qué cumple, qué falla y por qué, sin reescribirlo por ti.
