// public with sharing class GestorCasosController {

//     @auraEnabled(cacheable=true)
//     public static list<Case> ObtenerCasos() {
//         List<Case> casos = [
//                 Select Id, CaseNumber, Subject, Status, Priority
//                 from Case Order by CreatedDate Desc limit 50
//             ];

//             return casos; 
//     }
// }

public with sharing class GestorCasosController {

    @auraEnabled(cacheable=true)
    public static list<Case> ObtenerCasos(String estado) {
        
        if (string.isBlank(estado)){
            return [
                select id, CaseNumber, Subject, Status, Priority from Case
                order by CreatedDate desc limit 50
            ];
        }

        return [
            select Id, CaseNumber, Subject, Status, Priority from Case
            Where status = :estado order by CreatedDate desc limit 50
        ];
    }
}