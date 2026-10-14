import { LightningElement } from 'lwc';

export default class PanelCasos extends LightningElement {
    mensaje = 'Componente listo para probar';

    mostrarMensaje() {
        this.mensaje = '¡Tu componente LWC funciona correctamente!';
    }
}