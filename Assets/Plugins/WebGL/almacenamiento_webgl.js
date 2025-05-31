mergeInto(LibraryManager.library, {
    // Función para guardar en el almacenamiento de itch.io cuando está disponible
    GuardarEnAlmacenamientoLocal: function(clave, valor) {
        var claveStr = UTF8ToString(clave);
        var valorStr = UTF8ToString(valor);
        
        try {
            // Intentar usar la API de itch.io primero si está disponible
            if (window.parent && window.parent.postMessage) {
                // Técnica para identificar si estamos en un iframe de itch.io
                try {
                    var itchStorage = {};
                    itchStorage.key = claveStr;
                    itchStorage.value = valorStr;
                    // Enviar mensaje al host de itch.io para guardar datos
                    window.parent.postMessage({
                        type: "storage",
                        action: "set",
                        key: claveStr,
                        value: valorStr
                    }, "*");
                    console.log("Dato enviado a itch.io: " + claveStr);
                } catch (e) {
                    console.log("No se pudo comunicar con itch.io: " + e.message);
                }
            }
            
            // Siempre intentar usar localStorage como respaldo
            localStorage.setItem(claveStr, valorStr);
            console.log("WebGL: Dato guardado en localStorage - " + claveStr);
            return true;
        } catch (e) {
            console.error("Error al guardar datos - " + e.message);
            return false;
        }
    },

    // Función para cargar desde el almacenamiento
    CargarDesdeAlmacenamientoLocal: function(clave) {
        var claveStr = UTF8ToString(clave);
        var valor = null;
        
        try {
            // Usamos localStorage como fuente principal
            valor = localStorage.getItem(claveStr);
            
            // Si el valor es nulo, podríamos solicitar a itch.io
            // Pero esto requeriría una comunicación asíncrona que no es compatible
            // con la forma en que Unity llama a estas funciones
            
            if (valor === null) {
                console.log("WebGL: No se encontró valor para - " + claveStr);
                return null;
            }
            
            var bufferSize = lengthBytesUTF8(valor) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(valor, buffer, bufferSize);
            console.log("WebGL: Valor cargado para - " + claveStr);
            return buffer;
        } catch (e) {
            console.error("Error al cargar datos - " + e.message);
            return null;
        }
    },

    // Función para eliminar un elemento del almacenamiento
    EliminarDeAlmacenamientoLocal: function(clave) {
        var claveStr = UTF8ToString(clave);
        
        try {
            // Intentar comunicarse con itch.io para eliminar datos
            if (window.parent && window.parent.postMessage) {
                try {
                    window.parent.postMessage({
                        type: "storage",
                        action: "remove",
                        key: claveStr
                    }, "*");
                } catch (e) {
                    console.log("No se pudo comunicar con itch.io para eliminar: " + e.message);
                }
            }
            
            // Siempre eliminar de localStorage
            localStorage.removeItem(claveStr);
            console.log("WebGL: Dato eliminado - " + claveStr);
            return true;
        } catch (e) {
            console.error("Error al eliminar datos - " + e.message);
            return false;
        }
    },
    
    // Función para limpiar todo el almacenamiento local
    LimpiarAlmacenamientoLocal: function() {
        try {
            // Intentar comunicarse con itch.io para limpiar datos
            if (window.parent && window.parent.postMessage) {
                try {
                    window.parent.postMessage({
                        type: "storage",
                        action: "clear"
                    }, "*");
                } catch (e) {
                    console.log("No se pudo comunicar con itch.io para limpiar: " + e.message);
                }
            }
            
            // Siempre limpiar localStorage
            localStorage.clear();
            console.log("WebGL: Almacenamiento local limpiado completamente");
            return true;
        } catch (e) {
            console.error("Error al limpiar almacenamiento - " + e.message);
            return false;
        }
    },
    
    // Función para verificar si estamos en un iframe
    EstaEnIframe: function() {
        try {
            return (window.self !== window.top) ? 1 : 0;
        } catch (e) {
            return 1; // Si hay error de acceso, probablemente estamos en un iframe
        }
    }
});