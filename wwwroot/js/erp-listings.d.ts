interface Window {
    cambiarTop?: (size: number) => void;
    aplicarFiltros?: () => void;
    aplicarFiltrosLocal?: () => void;
    filtrarTablaCliente?: () => void;
    erpListings: {
        load(url: string, pushHistory?: boolean): Promise<void>;
        config(id: string): any;
        onClick(selector: string, handler: (button: HTMLButtonElement, event: MouseEvent) => void | Promise<void>): void;
    };
}
