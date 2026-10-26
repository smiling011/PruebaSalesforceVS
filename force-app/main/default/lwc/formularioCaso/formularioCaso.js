import { LightningElement, api } from 'lwc';
import {FlowAttributeChangeEvent} from 'lightning/flowSupport';

export default class FormularioCaso extends LightningElement {
    @api asunto = '';
    @api descripcion = '';
    @api prioridad = 'Medium';

    opcionesPrioridad = [
        { label: 'Alta', value: 'High' },
        { label: 'Media', value: 'Medium' },
        { label: 'Baja', value: 'Low' }
    ];

    handleAsunto(event) {
        this.asunto = event.target.value;
        this.dispatchEvent(
        new FlowAttributeChangeEvent('asunto', this.asunto)
    );
    }

    handleDescripcion(event) {
        this.descripcion = event.target.value;
        this.dispatchEvent(
        new FlowAttributeChangeEvent('descripcion', this.descripcion)
    );
    }

    handlePrioridad(event) {
        this.prioridad = event.detail.value;
        this.dispatchEvent(
        new FlowAttributeChangeEvent('prioridad', this.prioridad)
    );
    }

    @api
    validate() {
        const campos = [
            ...this.template.querySelectorAll(
                'lightning-input, lightning-textarea, lightning-combobox'
            )
        ];

        const valido = campos.reduce((resultado, campo) => {
            campo.reportValidity();
            return campo.checkValidity() && resultado;
        }, true);

        return {
            isValid: valido,
            errorMessage: valido
                ? null
                : 'Completa los campos obligatorios antes de continuar.'
        };
    }
}
