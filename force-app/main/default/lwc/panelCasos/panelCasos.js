
import { LightningElement, wire } from 'lwc';

import obtenerCasos from '@salesforce/apex/GestorCasosController.ObtenerCasos';

import actualizarEstado from '@salesforce/apex/GestorCasosController.ActualizarEstado';

import { refreshApex } from '@salesforce/apex';

export default class PanelCasos extends LightningElement {
    estadoSeleccionado = '';

    opcionesEstado = [
        { label: 'Todos', value: '' },
        { label: 'Nuevo', value: 'New' },
        { label: 'En espera', value: 'On Hold' },
        { label: 'Cerrado', value: 'Closed' },
        { label: 'En escala', value: 'Escalated' }
    ];

    @wire(obtenerCasos, { estado: '$estadoSeleccionado' })
    casos;

    // Filtro de la lista
    handleCambioEstado(event) {
        this.estadoSeleccionado = event.detail.value;
    }

    // Búsqueda de casos
    busquedaCaso = '';
    casoSeleccionado = '';
    casoSeleccionadoNombre = '';

    handleBuscarCaso(event) {
        this.busquedaCaso = event.target.value;
        this.casoSeleccionado = '';
        this.casoSeleccionadoNombre = '';
    }

    get casosEncontrados() {
        const consulta = (this.busquedaCaso || '')
            .trim()
            .toLowerCase();

        if (!consulta || !this.casos?.data) {
            return [];
        }

        return this.casos.data
            .filter(caso => {
                const numero = String(caso.CaseNumber || '')
                    .toLowerCase();

                const asunto = (caso.Subject || '')
                    .toLowerCase();

                return numero.includes(consulta) ||
                    asunto.includes(consulta);
            })
            .slice(0, 8);
    }

    handleSeleccionarCaso(event) {
        const id = event.currentTarget.dataset.id;

        const caso = (this.casos.data || [])
            .find(item => item.Id === id);

        if (caso) {
            this.casoSeleccionado = caso.Id;

            this.casoSeleccionadoNombre =
                `${caso.CaseNumber} - ${caso.Subject || 'Sin asunto'}`;

            this.busquedaCaso = this.casoSeleccionadoNombre;
        }
    }

    // Selección del nuevo estado
    nuevoEstado = '';
    mensaje = '';
    errorActualizacion = '';

    handleNuevoEstado(event) {
        this.nuevoEstado = event.detail.value;
    }

    // Actualización mediante Apex y DML
    async handleActualizar() {
        this.mensaje = '';
        this.errorActualizacion = '';

        if (!this.casoSeleccionado || !this.nuevoEstado) {
            this.errorActualizacion =
                'Selecciona un caso y un nuevo estado.';
            return;
        }

        try {
            await actualizarEstado({
                casoId: this.casoSeleccionado,
                nuevoEstado: this.nuevoEstado
            });

            await refreshApex(this.casos);

            this.mensaje = '¡Caso actualizado correctamente!';
        } catch (error) {
            this.errorActualizacion =
                error?.body?.message ||
                error?.message ||
                'No fue posible actualizar el caso.';
        }
    }
}
