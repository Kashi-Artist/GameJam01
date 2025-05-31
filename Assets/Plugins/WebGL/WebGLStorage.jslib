// Assets/Plugins/WebGL/WebGLStorage.jslib
mergeInto(LibraryManager.library, {
  // Guarda clave/valor en localStorage (o sessionStorage de fallback)
  guardarDatosJS: function(ptrClave, ptrValor) {
    var clave = UTF8ToString(ptrClave);
    var valor = UTF8ToString(ptrValor);
    try {
      console.log("[WebGLStorage] Guardando en localStorage:", clave);
      localStorage.setItem(clave, valor);
    } catch (e) {
      console.warn("[WebGLStorage] localStorage falló, usando sessionStorage:", e);
      try {
        sessionStorage.setItem(clave, valor);
      } catch (e2) {
        console.error("[WebGLStorage] sessionStorage también falló:", e2);
      }
    }
  },

  // Carga valor para clave; retorna puntero a string UTF-8 o null
  cargarDatosJS: function(ptrClave) {
    var clave = UTF8ToString(ptrClave);
    var valor = null;
    try {
      valor = localStorage.getItem(clave);
      if (valor === null) {
        valor = sessionStorage.getItem(clave);
        if (valor !== null) {
          console.log("[WebGLStorage] Cargado de sessionStorage:", clave);
        }
      } else {
        console.log("[WebGLStorage] Cargado de localStorage:", clave);
      }
    } catch (e) {
      console.error("[WebGLStorage] Error al cargar datos:", e);
      return null;
    }
    if (valor === null) return null;
    var bufSize = lengthBytesUTF8(valor) + 1;
    var buf = _malloc(bufSize);
    stringToUTF8(valor, buf, bufSize);
    return buf;
  },

  // Elimina clave de ambos almacenamientos
  eliminarDatosJS: function(ptrClave) {
    var clave = UTF8ToString(ptrClave);
    try {
      localStorage.removeItem(clave);
      sessionStorage.removeItem(clave);
      console.log("[WebGLStorage] Eliminado:", clave);
    } catch (e) {
      console.error("[WebGLStorage] Error al eliminar:", e);
    }
  }
});
