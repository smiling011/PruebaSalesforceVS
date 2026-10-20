// Crea casos de práctica para el proyecto 1 (panel de casos).
// Ejecuta el archivo completo: SFDX: Execute Anonymous Apex with Editor Contents

List<Case> casos = new List<Case>();
for (Integer i = 1; i <= 8; i++) {
    casos.add(new Case(
        Subject = 'Caso de práctica ' + i,
        Status = 'New',
        Priority = 'Medium',
        Origin = 'Web'
    ));
}
insert casos;

// Deja un par de casos ya cerrados para probar el conteo de cerrados.
List<Case> paraCerrar = [
    SELECT Id FROM Case WHERE Subject LIKE 'Caso de práctica%' LIMIT 2
];
for (Case c : paraCerrar) {
    c.Status = 'Closed';
}
update paraCerrar;

System.debug('Casos creados: ' + casos.size());
