import { LightningElement, wire } from 'lwc';
import obtenerCasos from '@salesforce/apex/GestorCasosController.ObtenerCasos';

export default class PanelCasos extends LightningElement {
    estadoSeleccionado = '';
    opcionesEstado = [
        { label: 'Todos', value: '' },
        { label: 'Nuevo', value: 'New' },
        { label: 'En progreso', value: 'Working' },
        { label: 'Cerrado', value: 'Closed' }
    ];
    @wire(obtenerCasos, {estado: '$estadoSeleccionado'})
    casos;

    handleCambioEstado(event){
        this.estadoSeleccionado = event.detail.value;
    }
}

