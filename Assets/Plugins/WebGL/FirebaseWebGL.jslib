mergeInto(LibraryManager.library, {
  // Esta es la única función que Unity invoca desde C#
  inicializarFirebase: function() {
    console.log("[FirebaseWebGL] Inicializando plugin…");
    // Guarda instancia Unity
    Module._unityInstance = Module;
    // Si ya cargamos los SDK, configúralo
    if (typeof firebase !== 'undefined' && Module._firebaseScriptsLoaded) {
      configure();
      return true;
    }

    // Función interna para configurar Firebase
    function configure() {
      try {
        console.log("[FirebaseWebGL] Configurando Firebase…");
        var cfg = {
          apiKey: "AIzaSyDDSzb8OxhUo9vq2hBnMP8iPv2zdte4bSc",
          authDomain: "akaya-d5aae.firebaseapp.com",
          projectId: "akaya-d5aae",
          databaseURL: "https://akaya-d5aae-default-rtdb.firebaseio.com",
          storageBucket: "akaya-d5aae.firebasestorage.app",
          messagingSenderId: "145309590011",
          appId: "1:145309590011:web:f540245b5c26ce542c6b5e",
          measurementId: "G-EKFC70LZL4"
        };
        var app;
        try { app = firebase.app(); console.log("[FirebaseWebGL] App ya iniciada"); }
        catch(e) { app = firebase.initializeApp(cfg); console.log("[FirebaseWebGL] App inicializada"); }
        var db   = firebase.database();
        var auth = firebase.auth();
        Module._firebaseScriptsLoaded = true;
        console.log("[FirebaseWebGL] Firebase listo");
        sendUnity("firebase_inicializado", { estado: "correcto" });
        auth.signInAnonymously()
          .then(()=>console.log("[FirebaseWebGL] Anónimo ok"))
          .catch(err=>console.error("[FirebaseWebGL] Auth err", err));
      } catch(err) {
        console.error("[FirebaseWebGL] Error init:", err);
        sendUnity("firebase_error", { mensaje: err.message });
      }
    }

    // Función interna para envío de mensajes
    function sendUnity(evento, datos) {
      datos = datos||{}; datos.evento = evento;
      var msg = JSON.stringify(datos);
      Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",msg);
    }

    // Carga secuencial de scripts
    var head = document.head;
    function load(src, cb) {
      var s = document.createElement("script");
      s.src = src; s.onload = cb; head.appendChild(s);
    }
    console.log("[FirebaseWebGL] Cargando scripts…");
    load("https://www.gstatic.com/firebasejs/9.6.1/firebase-app-compat.js", function(){
      console.log("[FirebaseWebGL] app.js listo");
      load("https://www.gstatic.com/firebasejs/9.6.1/firebase-auth-compat.js", function(){
        console.log("[FirebaseWebGL] auth.js listo");
        load("https://www.gstatic.com/firebasejs/9.6.1/firebase-database-compat.js", function(){
          console.log("[FirebaseWebGL] database.js listo");
          configure();
        });
      });
    });

    return true;
  },

  // Exponer el resto de llamadas como antes, enviando siempre por SendMessage
  guardarUsuarioFirebase: function(ptrJson) {
    var json = UTF8ToString(ptrJson);
    try {
      var u = JSON.parse(json);
      var uid = firebase.auth().currentUser.uid||"anonimo";
      firebase.database().ref("usuarios/"+uid).set(u);
      firebase.database().ref("ranking/"+u.nombre).set({
        nombre: u.nombre,
        puntajeMaximo: u.puntajeMaximo,
        ultimaActualizacion: new Date().toISOString()
      });
      return 1;
    } catch(e) {
      Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",
        JSON.stringify({ evento:"firebase_error", mensaje:e.message })
      );
      return 0;
    }
  },

  cargarUsuarioFirebase: function(ptrNombre) {
    var name = UTF8ToString(ptrNombre);
    firebase.database().ref("ranking/"+name).once("value")
      .then(snap=>{
        var datos = snap.exists()?snap.val():null;
        Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",
          JSON.stringify({ evento:"datos_usuario", datos:datos })
        );
      }).catch(e=>{
        Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",
          JSON.stringify({ evento:"firebase_error", mensaje:e.message })
        );
      });
    return 0;
  },

  cargarRankingFirebase: function() {
    firebase.database().ref("ranking")
      .orderByChild("puntajeMaximo").limitToLast(20).once("value")
      .then(snap=>{
        var arr=[]; snap.forEach(c=>arr.push(c.val())); arr.reverse();
        Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",
          JSON.stringify({ evento:"ranking_global", datos:arr })
        );
      }).catch(e=>{
        Module._unityInstance.SendMessage("GestorFirebase","ProcesarDatosDesdeJS",
          JSON.stringify({ evento:"firebase_error", mensaje:e.message })
        );
      });
    return 0;
  },

  eliminarUsuarioFirebase: function(ptrNombre) {
    var name = UTF8ToString(ptrNombre);
    try {
      var uid = firebase.auth().currentUser.uid||"anonimo";
      firebase.database().ref("usuarios/"+uid).remove();
      firebase.database().ref("ranking/"+name).remove();
      return 1;
    } catch(e) {
      return 0;
    }
  },
  verificarObjetosFirebaseJS: function(ptrExists, ptrApp, ptrAuth, ptrDB) {
    var exists     = (typeof firebase !== 'undefined')         ? 1 : 0;
    var appExists  = exists && (!!firebase.app())             ? 1 : 0;
    var authExists = exists && (!!firebase.auth())            ? 1 : 0;
    var dbExists   = exists && (!!firebase.database())        ? 1 : 0;
    setValue(ptrExists, exists,     'i32');
    setValue(ptrApp,    appExists,  'i32');
    setValue(ptrAuth,   authExists, 'i32');
    setValue(ptrDB,     dbExists,   'i32');
  }
});
