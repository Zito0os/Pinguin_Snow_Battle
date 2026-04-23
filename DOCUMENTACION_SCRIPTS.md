# 📚 DOCUMENTACIÓN COMPLETA DE SCRIPTS
## The Real Blood - Sistema de Juego Unity

---

## 📑 ÍNDICE

1. [Scripts de Inteligencia Artificial (AI)](#ai)
2. [Scripts de Cámara (Camera)](#camera)
3. [Scripts de Interactuables](#interactuables)
4. [Scripts de Multijugador (Multiplayer)](#multiplayer)
5. [Scripts del Jugador (Player)](#player)
6. [Scripts de Interfaz de Usuario (UI)](#ui)
7. [Scripts de Armas (Weapon)](#weapon)
8. [Scripts del Mundo (World)](#world)

---

<a name="ai"></a>
## 🤖 1. SCRIPTS DE INTELIGENCIA ARTIFICIAL (AI)

### 📄 **AI.cs**
**Propósito**: Controla el comportamiento completo de los enemigos con IA.

**Funcionalidades principales:**
- **Sistema de navegación**: Utiliza NavMeshAgent para mover a los enemigos entre puntos de destino
- **Detección y persecución del jugador**: Cuando el jugador está dentro del rango de detección, el enemigo lo persigue
- **Sistema de combate**: Coordina con EnemyShoot.cs para atacar al jugador cuando está en rango
- **Gestión de vida**: Controla los puntos de vida del enemigo (liveEnemy = 100)
- **Animaciones**: Sincroniza animaciones con el Animator (isAlive, isFire, isFollow, isChill)
- **Sistema IK (Inverse Kinematics)**: Controla las manos del enemigo para sostener el arma de forma realista usando TwoBoneIKConstraint
- **Icono de minimapa**: Crea automáticamente un icono en el minimapa para representar al enemigo
- **Sonido de muerte**: Reproduce audio cuando el enemigo muere con control de volumen y distancia
- **Agresividad por daño**: Si recibe daño, el enemigo puede volverse permanentemente agresivo

**Variables importantes:**
- `distanceToFollowPlayer = 15`: Distancia a la que empieza a perseguir
- `distanceToAttackPlayer = 8`: Distancia a la que empieza a disparar
- `liveEnemy = 100`: Vida del enemigo
- `perseguirSiempreSiRecibeDanio = true`: Persigue siempre después de recibir daño

**Flujo de ejecución:**
1. Patrulla entre puntos de destino
2. Detecta jugador si está en rango
3. Persigue al jugador
4. Ataca cuando está en rango de ataque
5. Muere cuando la vida llega a 0
6. Desactiva IK y reproduce animación/sonido de muerte

---

### 📄 **Anclajes_de_arma.cs**
**Propósito**: Gestiona los puntos de agarre (grips) para que las manos de los enemigos sostengan el arma correctamente.

**Funcionalidades principales:**
- **Grips automáticos**: Define puntos de agarre para mano derecha e izquierda
- **Sincronización con IK**: Trabaja en conjunto con el sistema IK para posicionar las manos
- **Configuración personalizable**: Permite ajustar la posición exacta de cada grip

**Uso típico:**
- Se coloca en el arma del enemigo
- El script AI.cs lo busca y utiliza para configurar el IK de las manos

---

### 📄 **EnemyBullet.cs**
**Propósito**: Controla el comportamiento de las balas disparadas por los enemigos.

**Funcionalidades principales:**
- **Auto-destrucción**: Se destruye después de 3 segundos
- **Detección de colisión**: Detecta cuando impacta al jugador
- **Limpieza automática**: Elimina la bala después de impactar

**Diferencia con Bullet.cs del jugador:**
- Las balas del enemigo NO hacen daño automático (el daño se maneja en PlayerInteractions.cs)
- Solo se destruyen al colisionar con el jugador

---

### 📄 **EnemyShoot.cs**
**Propósito**: Sistema de disparo para los enemigos IA.

**Funcionalidades principales:**
- **Disparo automático periódico**: Dispara cada cierto intervalo (shootInterval = 3 segundos)
- **Puntería hacia el jugador**: Calcula la dirección hacia el jugador y ajusta altura
- **Sistema de munición**: Instancia balas en el punto de spawn
- **Física de proyectil**: Aplica velocidad a las balas (bulletVelocity = 100)
- **Sonido de disparo**: Reproduce audio al disparar con volumen configurable
- **Control de disparo**: Método SetShooting() permite activar/desactivar el disparo

**Variables importantes:**
- `alturaObjetivoJugador = 1.2f`: Apunta a la altura del torso del jugador
- `shootInterval = 3f`: Tiempo entre disparos
- `bulletVelocity = 100f`: Velocidad de la bala

**Uso:**
- AI.cs llama a SetShooting(true) cuando el jugador está en rango de ataque
- SetShooting(false) cuando el jugador sale del rango

---

### 📄 **IA_Manager.cs**
**Propósito**: Gestor central que coordina todos los enemigos en la escena.

**Funcionalidades principales:**
- **Registro de enemigos**: Mantiene una lista de todos los enemigos activos
- **Generación dinámica**: Puede instanciar enemigos en puntos específicos
- **Gestión de oleadas**: Controla oleadas de enemigos si es necesario
- **Sistema de pool**: Reutiliza enemigos en lugar de destruirlos para optimización
- **Coordinación global**: Maneja eventos que afectan a todos los enemigos

---

<a name="camera"></a>
## 📷 2. SCRIPTS DE CÁMARA (Camera)

### 📄 **Cameralook.cs**
**Propósito**: Controla la rotación de la cámara en primera persona con el mouse.

**Funcionalidades principales:**
- **Vista en primera persona**: Rota la cámara verticalmente con movimiento del mouse
- **Control de sensibilidad**: Variable configurable (sensitivity = 80f)
- **Rotación del cuerpo**: Sincroniza la rotación horizontal del cuerpo del jugador
- **Límite vertical**: Previene que la cámara gire 360° verticalmente (clamp)
- **Soporte multijugador**: Solo funciona para el jugador local (IsMine)
- **Bloqueo de cursor**: Oculta y bloquea el cursor durante el juego

**Variables importantes:**
- `sensitivity = 80f`: Velocidad de rotación de la cámara
- `xRotation`: Acumula la rotación vertical para aplicar límites

**Compatibilidad:**
- Single player y multiplayer (con Photon)
- Se desactiva en menús de pausa

---

### 📄 **CameraSwitch.cs**
**Propósito**: Permite cambiar entre vista de primera persona y tercera persona.

**Funcionalidades principales:**
- **Alternancia de cámaras**: Cambia entre cámara de primera y tercera persona
- **Reposicionamiento de armas**: Mueve las armas a posiciones diferentes según la vista
- **Escala de armas**: Ajusta el tamaño de las armas para cada vista
- **Ocultación de mesh**: Puede ocultar el modelo del jugador en primera persona
- **Soporte multijugador**: Solo el jugador local puede cambiar su vista

**Uso típico:**
- Tecla configurable (ejemplo: V) para cambiar vista
- Las armas se reposicionan automáticamente

**Arrays importantes:**
- `weaponsTransformFirstPerson[]`: Posiciones de armas en primera persona
- `weaponsTransformThirdPerson[]`: Posiciones de armas en tercera persona
- `weapons[]`: Lista de GameObjects de armas activas

---

### 📄 **ThirdPersonCamera.cs**
**Propósito**: Implementa la lógica de la cámara en tercera persona.

**Funcionalidades principales:**
- **Seguimiento del jugador**: Sigue al jugador manteniendo una distancia fija
- **Rotación orbital**: Permite rotar alrededor del jugador
- **Detección de obstáculos**: Acerca la cámara si hay obstáculos entre ella y el jugador
- **Suavizado de movimiento**: Usa interpolación para movimientos fluidos

---

<a name="interactuables"></a>
## 🎮 3. SCRIPTS DE INTERACTUABLES

### 📄 **Presionar.cs**
**Propósito**: Sistema genérico de interacción con objetos del mundo.

**Funcionalidades principales:**
- **Detección por Raycast**: Lanza un rayo desde la cámara para detectar objetos interactuables
- **Búsqueda de cámara**: Encuentra automáticamente la cámara del jugador local
- **Soporte multijugador**: Compatible con Photon, busca solo la cámara del jugador local
- **Distancia de interacción**: Define qué tan lejos puede interactuar el jugador
- **Indicador visual**: Puede mostrar UI cuando se puede interactuar

**Uso:**
- Detecta objetos con tag específico
- Puede abrir puertas, recoger items, activar mecanismos
- Fundamental para el sistema de interacción del juego

---

<a name="multiplayer"></a>
## 🌐 4. SCRIPTS DE MULTIJUGADOR (Multiplayer)

### 📄 **AudioListenerManager.cs**
**Propósito**: Gestiona los AudioListeners en multijugador para evitar múltiples listeners activos.

**Funcionalidades principales:**
- **Deduplicación de listeners**: Asegura que solo hay UN AudioListener activo
- **Prioridad de ejecución**: Se ejecuta primero (ExecutionOrder = -100)
- **Detección automática**: Escanea todos los listeners en la escena
- **Filtrado por jugador**: Solo activa el listener del jugador local
- **Logs de debug**: Muestra información sobre listeners encontrados

**Problema que resuelve:**
- Unity genera warnings si hay múltiples AudioListeners activos
- En multijugador, cada clon del jugador trae su propio listener
- Este script desactiva todos excepto el del jugador local

**Configuración:**
```csharp
executionOrder: -100  // Se ejecuta ANTES que otros scripts
```

---

### 📄 **Launcher.cs**
**Propósito**: Punto de entrada del sistema multijugador, maneja la conexión y spawn inicial.

**Funcionalidades principales:**
- **Conexión a Photon**: Conecta el jugador a los servidores de Photon
- **Creación de nombre**: Asigna nombre "Player X" basado en ActorNumber
- **Spawn del jugador**: Instancia el prefab del jugador en la red
- **Selección de spawn aleatorio**: Elige un punto de spawn al azar del array
- **Detección automática de spawns**: Busca objetos con tag "Respawn" si no hay spawns asignados
- **Unión a sala**: Une al jugador a la sala "Room" automáticamente

**Variables importantes:**
- `player_prefab`: Prefab del jugador con PhotonView
- `spawnPoints[]`: Array de puntos de spawn disponibles

**Flujo de conexión:**
1. Conecta a Photon Cloud
2. Une/crea sala "Room"
3. Asigna nombre al jugador
4. Selecciona spawn aleatorio
5. Instancia jugador en red

---

### 📄 **MultiplayerMinimapOwner.cs**
**Propósito**: Gestiona el icono del jugador en el minimapa en multijugador.

**Funcionalidades principales:**
- **Creación de icono único**: Cada jugador tiene su propio icono
- **Color distintivo**: El jugador local tiene color diferente que otros jugadores
- **Sincronización de posición**: Actualiza la posición del icono en tiempo real
- **Configuración de capa**: Usa capas específicas para que el minimapa lo detecte
- **Detección automática**: Encuentra la cámara del minimapa automáticamente

**Diferencias de color:**
- Jugador LOCAL: Color personalizado (configurable)
- Otros jugadores: Color diferente para distinguirlos

---

<a name="player"></a>
## 🏃 5. SCRIPTS DEL JUGADOR (Player)

### 📄 **PlayerInteractions.cs**
**Propósito**: Maneja todas las interacciones del jugador con el entorno y enemigos.

**Funcionalidades principales:**
- **Recepción de daño de enemigos**: Detecta colisiones con balas enemigas
- **Interacción con objetos**: Recoge munición, vendas, cajas
- **Raycast de interacción**: Detecta objetos interactuables frente al jugador
- **Gestión de inventario**: Controla munición y vendas recolectadas
- **Feedback visual**: Muestra mensajes de interacción
- **Sonidos de interacción**: Reproduce audio al recoger items
- **Soporte multijugador**: Solo el jugador local puede interactuar

**Interacciones soportadas:**
- Cajas de munición (AmmoBox)
- Objetos de salud (HealthObject)
- Puertas y mecanismos
- Cualquier objeto con interface IInteractable

**Variables clave:**
- `distanciaInteraccion`: Qué tan lejos puede interactuar
- Usa GameManager para actualizar UI de munición/vendas

---

### 📄 **PlayerMovement.cs**
**Propósito**: Controla todo el movimiento del jugador (caminar, correr, saltar).

**Funcionalidades principales:**
- **Movimiento WASD**: Control estándar de movimiento
- **Sprint**: Correr con Shift, consume stamina
- **Salto**: Salto con Espacio, verifica si está en el suelo
- **Sistema de gravedad**: Aplica gravedad personalizada
- **Detección de suelo**: Usa esfera de colisión para verificar si está en el suelo
- **Animaciones sincronizadas**: Actualiza parámetros del Animator (VelX, VelZ, isSprinting)
- **Audio de pasos**: Reproduce sonidos diferentes para caminar y correr
- **Soporte multijugador**: Solo el jugador local controla su movimiento
- **Bloqueo temporal**: No permite moverse si EmotePanel está activo

**Variables importantes:**
- `speed = 15f`: Velocidad de movimiento base
- `sprintSpeedMultiplier = 2f`: Multiplicador al correr
- `jumpheigth = 3f`: Altura del salto
- `gravity = -3f`: Fuerza de gravedad
- `staminaUseAmount = 5f`: Stamina que consume al correr

**Sistema de audio:**
- Cambiando sonidos diferentes para caminar vs correr
- Control de volumen independiente
- Solo el jugador local escucha sus propios pasos

---

### 📄 **PlayerStats.cs**
**Propósito**: Gestiona las estadísticas del jugador (vida, kills, muertes) y el sistema de respawn.

**Funcionalidades principales:**
- **Gestión de vida**: Control de health actual y maxHealth
- **Sistema de kills/deaths**: Contabiliza kills y muertes
- **Recepción de daño con RPC**: Sistema de daño sincronizado en red
- **Notificación de kills**: Cuando un jugador mata a otro, ambos se enteran
- **Sistema de respawn**: Reaparece en punto aleatorio al morir
- **Selección aleatoria de spawn**: Nunca aparece dos veces seguidas en el mismo lugar
- **Sincronización con Photon**: Actualiza Custom Properties para el leaderboard
- **Sincronización con GameManager**: Mantiene UI actualizada

**Sistema de daño (víctima-notifica-shooter):**
1. Bala impacta → Envía RPC_LoseHealth con damage y shooterViewID
2. Víctima aplica daño localmente
3. Si health <= 0 → Víctima detecta muerte
4. Víctima busca shooter por ViewID → Envía RPC_NotificarKill
5. Shooter recibe RPC → Incrementa kill local
6. Sincroniza con Photon CustomProperties

**Sistema de respawn:**
1. Restaura health a maxHealth
2. Selecciona spawn aleatorio diferente al anterior
3. Envía RPC para sincronizar teletransporte con todos los clientes
4. Desactiva CharacterController temporalmente para cambiar posición
5. Todos los clientes ejecutan el teletransporte simultáneamente

**Variables importantes:**
- `health = 100`: Vida actual
- `maxHealth = 100`: Vida máxima
- `kills`: Número de kills
- `muertes`: Número de muertes
- `spawnPoints[]`: Array de puntos de respawn
- `lastSpawnIndex`: Para evitar repetir spawn

**RPCs implementados:**
- `RPC_LoseHealth`: Aplica daño
- `RPC_SincronizarRespawn`: Sincroniza teletransporte
- `RPC_NotificarKill`: Notifica kill al shooter

---

<a name="ui"></a>
## 🖥️ 6. SCRIPTS DE INTERFAZ DE USUARIO (UI)

### 📄 **EmotePanel.cs**
**Propósito**: Panel de emotes que se abre manteniendo la tecla H.

**Funcionalidades principales:**
- **Activación con tecla H**: Mantener presionado muestra el panel
- **8 emotes disponibles**: Rueda de selección con 8 opciones
- **Highlights visuales**: Resalta el emote seleccionado
- **Bloqueo de controles**: Mientras está abierto, bloquea movimiento y disparo
- **Variable estática**: `isEmotePanelActive` permite a otros scripts saber si está abierto

**Uso:**
- Mantener H → Abre panel
- Mover mouse → Selecciona emote
- Soltar H → Cierra panel y ejecuta emote

---

### 📄 **HealthBar.cs**
**Propósito**: Barra de vida visual que muestra el health del jugador.

**Funcionalidades principales:**
- **Sincronización con GameManager**: Lee health de GameManager.Instance
- **Color dinámico**: Cambia de verde → amarillo → rojo según vida
- **Actualización automática**: Se actualiza cada frame
- **Soporte para maxHealth dinámico**: Ajusta slider si maxHealth cambia

**Sistema de colores:**
- Verde: 60-100% de vida
- Amarillo: 30-60% de vida
- Rojo: 0-30% de vida

---

### 📄 **Menu_Configuracion.cs**
**Propósito**: Panel de configuración del juego.

**Funcionalidades principales:**
- **Control de volumen**: Slider para ajustar volumen de música
- **Calidad gráfica**: Dropdown para seleccionar nivel de calidad
- **Sensibilidad de mouse**: Slider para ajustar sensibilidad de cámara
- **Persistencia**: Guarda configuración con PlayerPrefs
- **Aplicación en tiempo real**: Cambios se aplican inmediatamente

**Configuraciones guardadas:**
- MusicVolume: Volumen de música (0-1)
- QualityLevel: Nivel de calidad gráfica (0-5)
- Se cargan automáticamente al iniciar el juego

---

### 📄 **Menu_GameOver.cs**
**Propósito**: Menú que aparece cuando el jugador muere.

**Funcionalidades principales:**
- **Reiniciar nivel**: Botón para volver a jugar single player
- **Volver al menú principal**: Botón para salir al menú
- **Habilitación de input**: Activa el cursor al morir
- **EventSystem**: Asegura que EventSystem esté activo para UI

---

### 📄 **Menu_principal.cs**
**Propósito**: Menú principal del juego.

**Funcionalidades principales:**
- **Cargar Single Player**: Carga escena 1 (modo solitario)
- **Cargar Multiplayer**: Carga escena 2 (modo multijugador)
- **Salir del juego**: Cierra la aplicación
- **Compatibilidad Editor**: En editor detiene play mode en lugar de cerrar

---

### 📄 **Menu.cs**
**Propósito**: Menú de pausa durante el juego.

**Funcionalidades principales:**
- **Pausa con ESC**: Abre/cierra menú de pausa
- **Panel de estadísticas con TAB**: Abre panel de stats en multiplayer
- **Gestión de Time.timeScale**: Pausa/reanuda el juego
- **Múltiples paneles**: Pausa, configuración, estadísticas
- **Control de cursor**: Muestra/oculta cursor según estado
- **Jerarquía de paneles**: Maneja apertura/cierre de submenús

**Estados del menú:**
- Jugando: Time.timeScale = 1, cursor oculto
- Pausado: Time.timeScale = 0, cursor visible
- Stats abierto: Time.timeScale = 0, muestra leaderboard

---

### 📄 **StaminaBar.cs**
**Propósito**: Barra de stamina que se consume al correr.

**Funcionalidades principales:**
- **Consumo al correr**: PlayerMovement la consume al sprintar
- **Regeneración automática**: Se regenera cuando no estás corriendo
- **Prevención de exploit**: No puedes correr si stamina está en 0
- **Corutinas**: Usa corutinas para regeneración suave
- **Control de velocidad**: regenerateStaminaTime y regeneratesAmount configurables

**Sistema:**
- `maxStamina = 100`: Stamina máxima
- `regeneratesAmount = 2`: Cantidad que regenera cada tick
- `regenerateStaminaTime = 0.1f`: Tiempo entre regeneraciones

---

### 📄 **StatsPanel.cs**
**Propósito**: Panel de estadísticas/leaderboard en multijugador.

**Funcionalidades principales:**
- **Top 3 jugadores**: Muestra los 3 jugadores con más kills
- **Actualización automática**: Se actualiza cuando cambian las Custom Properties
- **Callback de Photon**: Usa OnPlayerPropertiesUpdate para actualizaciones en tiempo real
- **Actualización periódica**: InvokeRepeating cada 2 segundos como respaldo
- **Ordenamiento por kills**: Ordena jugadores por cantidad de kills

**Formato de visualización:**
```
1º - [Nombre] - [Kills] Kills - [Muertes] Muertes
2º - [Nombre] - [Kills] Kills - [Muertes] Muertes
3º - [Nombre] - [Kills] Kills - [Muertes] Muertes
```

**Tecnología:**
- Lee de PhotonNetwork.PlayerList
- Custom Properties: "Kills" y "Muertes"
- Updates cada vez que alguien mata/muere

---

<a name="weapon"></a>
## 🔫 7. SCRIPTS DE ARMAS (Weapon)

### 📄 **Bullet.cs**
**Propósito**: Controla el comportamiento de las balas disparadas por el jugador.

**Funcionalidades principales:**
- **Detección de escena multijugador**: Inicializa usarPhotonEnEscena en Awake()
- **Asignación de shooter**: Recibe el PhotonView del jugador que disparó
- **Sistema de daño Player vs Player**: Aplica daño con ViewID del shooter
- **Auto-destrucción**: Se destruye después de 1 segundo
- **Detección de impacto**: Detecta colisión con jugadores y enemigos
- **Daño configurable**: damageToPlayer = 3 (requiere 34 balas para matar)

**Sistema en multiplayer:**
1. WeaponLogic instancia bala y llama SetShooter(PhotonView)
2. Bala guarda shooterPhotonView
3. Al colisionar con jugador → Obtiene PlayerStats
4. Llama LoseHealth(damage, shooterViewID)
5. Víctima procesa daño y notifica al shooter si muere

**Timing crítico:**
- Usa Awake() para inicializar (antes que physics)
- Previene bug de colisión antes de inicialización

---

### 📄 **Cargador.cs**
**Propósito**: Script placeholder para sistema de munición (actualmente vacío).

**Posible uso futuro:**
- Gestionar cargadores individuales
- System de recarga realista
- Diferentes tipos de munición

---

### 📄 **Granada.cs**
**Propósito**: Controla el comportamiento de la granada después de ser lanzada.

**Funcionalidades principales:**
- **Temporizador**: Explota después de 3 segundos (delay)
- **Explosión con área**: Radio de explosión configurable (radius = 5)
- **Daño en área**: Aplica daño a todos en el radio (damage = 50)
- **Física de explosión**: Aplica fuerza a objetos con Rigidbody (explotionforce = 70)
- **Efecto visual**: Instancia efecto de explosión
- **Sonido de explosión**: AudioClip reproducido al explotar

**Sistema:**
1. Se lanza con ThrowGranade.cs
2. Cuenta regresiva (countdown)
3. Al llegar a 0 → Explode()
4. Busca todos los colliders en radio
5. Aplica daño y fuerza
6. Instancia efecto visual
7. Se destruye

---

### 📄 **ThrowGranade.cs**
**Propósito**: Sistema de lanzamiento de granadas.

**Funcionalidades principales:**
- **Lanzamiento con tecla E**: Input para lanzar granada
- **Física de lanzamiento**: Aplica fuerza para simular lanzamiento realista
- **Sistema de munición**: Controla cantidad de granadas disponibles
- **Animación de lanzamiento**: Sincroniza con animator
- **Predicción de trayectoria**: Muestra línea de trayectoria antes de lanzar
- **Soporte multijugador**: Compatible con Photon

**Variables:**
- `throwForce = 400f`: Fuerza de lanzamiento
- `granadePrefab`: Prefab de la granada a instanciar
- Usa GameManager para contar granadas disponibles

---

### 📄 **WeaponLogic.cs**
**Propósito**: Sistema principal de disparo y gestión de armas.

**Funcionalidades principales:**

**Sistema de disparo:**
- **Disparo con mouse**: Click izquierdo para disparar (solo PC)
- **Modos de disparo**: Automático (mantener click) o semiautomático (click por disparo)
- **Cambio de modo con C**: Alterna entre automático/manual
- **Cadencia de disparo**: shotRate = 0.3 segundos entre disparos
- **Apuntado con botón derecho**: Zoom al apuntar

**Sistema de munición:**
- **Cargador actual**: cargador_actual (balas en arma)
- **Munición total**: cantidad_balas_total (balas de reserva)
- **Capacidad**: capacidad_cargador = 30 balas
- **Recarga con R**: Recargar desde munición total
- **Tiempo de recarga**: 3 segundos con UI de "Recargando"

**Sistema de curación:**
- **Curación con X**: Usa venda para recuperar health
- **Tiempo de curación**: 3 segundos con UI de "Curando"
- **Bloqueo durante curación**: No puede disparar mientras cura

**Apuntado inteligente:**
- **Raycast para precisión**: Dispara exactamente donde apunta el centro de la pantalla
- **Sistema de dirección**: ObtenerDireccionDisparo() calcula dirección precisa
- **Distancia máxima**: distanciaMaximaApuntado = 300f

**Multijugador:**
- **RPC para disparos**: ShootRPC() envía disparo a todos los clientes
- **Sincronización de munición**: Se sincroniza con GameManager
- **Detección automática de UI**: Busca UI en multiplayer para vincular
- **ViewID tracking**: Asigna ViewID del shooter a cada bala

**Sonidos:**
- Sonido de disparo
- Sonido de recarga
- Sonido de cambio de modo
- Sonido de curación

**Logs de debug extensos:**
- Disparo presionado/soltado
- Llamadas a ShootRPC
- Recepción de RPC
- Estado de PhotonView

**Flujo de disparo en multiplayer:**
1. Input.GetMouseButtonDown(0) detectado
2. Llama ShootRPC()
3. Decrementa cargador_actual
4. Envía RPC "ShootMultiplayer" con posición y dirección
5. TODOS los clientes reciben RPC
6. Cada cliente instancia bala visual
7. Bala tiene ViewID del shooter para atribución de kills

---

### 📄 **WeaponSway.cs**
**Propósito**: Agrega movimiento realista al arma siguiendo el mouse.

**Funcionalidades principales:**
- **Movimiento con mouse**: El arma se mueve sutilmente con el movimiento del mouse
- **Rotación dinámica**: Rota el arma según input del mouse
- **Efecto de inercia**: Simula peso realista del arma
- **Cantidad configurable**: swayAmount = 8f controla intensidad

**Uso:**
- Se coloca en el GameObject del arma
- Crea sensación más inmersiva al apuntar
- No afecta la puntería real (solo visual)

---

### 📄 **WeaponSwitch.cs**
**Propósito**: Sistema de cambio entre múltiples armas.

**Funcionalidades principales:**
- **Cambio con scroll del mouse**: ScrollWheel para cambiar arma
- **Teclas numéricas**: 1, 2, 3... para seleccionar arma específica
- **Gestión de armas**: Activa/desactiva GameObjects de armas
- **Índice de selección**: selectedWeapon rastrea arma actual
- **Animación de cambio**: Puede integrar animaciones de cambio
- **Soporte multijugador**: Solo el jugador local cambia sus armas

**Array de armas:**
- `weapons[]`: Array con todos los GameObjects de armas
- Solo una arma activa a la vez
- Resto desactivadas

---

<a name="world"></a>
## 🌍 8. SCRIPTS DEL MUNDO (World)

### 📄 **AmmoBox.cs**
**Propósito**: Objeto que contiene munición y granadas para recoger.

**Funcionalidades principales:**
- **Munición para arma**: ammo = 12 balas
- **Granadas**: granada = 1 granada
- **Valores configurables**: Permite ajustar cantidad por caja
- **Interacción**: PlayerInteractions.cs lee estos valores al recoger

---

### 📄 **Bienvenida.cs**
**Propósito**: Muestra mensaje de bienvenida temporal al iniciar nivel.

**Funcionalidades principales:**
- **Auto-desactivación**: Desactiva el objeto después de tiempo establecido
- **Tiempo configurable**: tiempoParaDesactivar = 10 segundos
- **Corutina**: Usa corutina para esperar sin bloquear
- **Validación**: Verifica que el objeto exista antes de desactivar

**Uso típico:**
- Mensaje "Bienvenido a [Nivel]"
- Tutorial inicial
- Información temporal

---

### 📄 **cambiar_color.cs**
**Propósito**: Cambia el material de un objeto (usado para objetos interactuables).

**Funcionalidades principales:**
- **Array de materiales**: Dos materiales (normal y activado)
- **Método cambiar()**: Cambia al material alternativo
- **Feedback visual**: Indica que objeto ha sido interactuado

**Uso:**
- Botones que cambian de color al presionar
- Puertas que cambian cuando se abren
- Sistemas de puzzles

---

### 📄 **GameManager.cs**
**Propósito**: Gestor central del juego, controla UI y estado global.

**Funcionalidades principales:**

**Patrón Singleton:**
- `GameManager.Instance`: Acceso global desde cualquier script
- Solo una instancia en toda la escena

**Gestión de stats del jugador:**
- `health`: Vida actual
- `maxHealth`: Vida máxima
- `Kills`: Total de kills
- `muertes`: Total de muertes
- `cargador_balas`: Balas en cargador actual
- `balas_totales`: Munición total
- `numero_de_vendas`: Vendas disponibles
- `numero_de_granadas`: Granadas disponibles

**Gestión de UI:**
- Actualiza TextMeshPro de munición
- Actualiza contador de vendas
- Actualiza contador de granadas
- Maneja barra de vida

**Delegación en multiplayer:**
- En single player: Gestiona todo directamente
- En multiplayer: Delega a PlayerStats.cs para operaciones de red
- Método ObtenerPlayerStatsLocal() busca el PlayerStats del jugador local

**Métodos importantes:**
- `LoseHealth(int damage)`: Reduce vida, delega a PlayerStats en MP
- `AddKill()`: Incrementa kills, delega a PlayerStats en MP
- `AddDeath()`: Incrementa muertes
- Métodos de munición: AddBullets(), ConsumeBullet(), etc.

**Detección de muerte:**
- Si health <= 0 → Abre menú de Game Over
- En multiplayer → PlayerStats maneja el respawn

---

### 📄 **HealthObject.cs**
**Propósito**: Objeto de salud que restaura vida al jugador.

**Funcionalidades principales:**
- **Valor de curación**: health = 2 puntos de vida
- **Configurable**: Permite ajustar cuánta vida restaura
- **Interacción**: PlayerInteractions.cs lee este valor

**Tipos posibles:**
- Botiquín pequeño: 2 health
- Botiquín mediano: 5 health
- Botiquín grande: 10 health

---

### 📄 **luz.cs**
**Propósito**: Activa/desactiva luces cuando el jugador entra/sale de trigger.

**Funcionalidades principales:**
- **Trigger zones**: Usa OnTriggerEnter/OnTriggerExit
- **Detección de jugador**: Verifica tag "Player"
- **Control de luz**: Enciende/apaga componente Light
- **Optimización**: Solo enciende luces cuando el jugador está cerca

**Uso:**
- Pasillos con luces que se encienden al pasar
- Habitaciones que se iluminan al entrar
- Ahorro de rendimiento

---

### 📄 **Recoleccion_cambiar.cs**
**Propósito**: Sistema que alterna entre spawns de salud y munición periódicamente.

**Funcionalidades principales:**
- **Alternancia automática**: Cada 15 segundos cambia entre salud y munición
- **Gestión por tags**: Busca objetos con tags "HealthObject" y "GunAmmo"
- **Activación/desactivación**: Activa uno, desactiva otro
- **Lista dinámica**: Registra todos los objetos en Start()
- **Corutina periódica**: InvokeRepeating para cambios regulares

**Flujo:**
1. Start: Encuentra todos los objetos de salud y munición
2. Desactiva todos los de un tipo
3. Activa todos del otro tipo
4. Cada 15s: Intercambia

**Objetivo de gameplay:**
- Fuerza al jugador a moverse por el mapa
- Evita que acumule demasiada munición o salud
- Crea variedad en la exploración

---

## 📊 RESUMEN DE ARQUITECTURA

### **Jerarquía de sistemas:**

```
GameManager (Singleton) - Control global
├─ PlayerStats (Multiplayer) - Stats individuales + RPCs
├─ PlayerMovement - Input y física
├─ PlayerInteractions - Colisiones y recolección
├─ WeaponLogic - Disparo y munición
└─ Cameralook - Vista del jugador

AI Manager - Coordinación de enemigos
└─ AI (cada enemigo)
    ├─ EnemyShoot - Disparo
    └─ Anclajes_de_arma - IK de armas

Launcher (Multiplayer) - Punto de entrada red
├─ AudioListenerManager - Gestión de audio
└─ StatsPanel - Leaderboard
```

### **Flujo de daño en multiplayer:**

```
[Jugador A] Dispara
    ↓
WeaponLogic.ShootRPC()
    ↓
RPC a todos: ShootMultiplayer()
    ↓
Bala instanciada con ViewID de A
    ↓
Impacta a [Jugador B]
    ↓
PlayerStats B recibe: RPC_LoseHealth(damage, ViewID_A)
    ↓
B aplica daño localmente
    ↓
Si B muere → B envía RPC_NotificarKill a A
    ↓
A incrementa kills localmente
    ↓
A sincroniza CustomProperties con Photon
    ↓
StatsPanel actualiza leaderboard en todos los clientes
```

### **Sistemas clave de sincronización:**

1. **Photon Custom Properties**: Kills y Muertes (para leaderboard)
2. **PhotonView.IsMine**: Determina quién controla qué
3. **RpcTarget.AllBuffered**: Asegura que todos reciban RPCs
4. **ViewID**: Identifica shooters para atribución de kills

---

## 🎮 CONTROLES DEL JUEGO

### **Movimiento:**
- WASD: Moverse
- Espacio: Saltar
- Shift: Correr (consume stamina)

### **Combate:**
- Click Izquierdo: Disparar
- Click Derecho: Apuntar (zoom)
- C: Cambiar modo de disparo (auto/manual)
- R: Recargar
- E: Lanzar granada
- X: Usar venda (curar)

### **Armas:**
- Scroll: Cambiar arma
- 1, 2, 3...: Seleccionar arma específica

### **UI:**
- ESC: Menú de pausa
- Tab: Panel de estadísticas (multiplayer)
- H (mantener): Panel de emotes

---

## 🔧 CONFIGURACIÓN RECOMENDADA

### **Para Single Player:**
- GameManager en escena
- Player con PlayerMovement, PlayerInteractions
- Enemigos con AI, EnemyShoot
- HealthObjects y AmmoBoxes en mapa

### **Para Multiplayer (requiere Photon):**
- Launcher en escena
- AudioListenerManager (ExecutionOrder = -100)
- Player prefab con PhotonView + PlayerStats + PlayerMovement
- Weapon con WeaponLogic (debe tener PhotonView en parent)
- Mínimo 2 spawn points con tag "Respawn"
- StatsPanel para leaderboard

---

## 📝 NOTAS IMPORTANTES

1. **ESP32 Háptico**: El disparo táctil está deshabilitado intencionalmente para integración futura con dispositivo háptico ESP32.

2. **Logs de debug**: Muchos scripts tienen logs extensivos con símbolos ✓ y ✗ para facilitar debugging en desarrollo. Pueden removerse en producción.

3. **Photon Setup**: Requiere configuración de Photon PUN2 y servidor Photon Cloud para funcionar el multijugador.

4. **Prefabs requeridos**:
   - Player_Multijugador (en Resources con PhotonView)
   - Bullet prefab
   - Enemy prefab
   - Granada prefab

5. **Optimización**: El sistema de minimapa usa layers específicas para culling. AudioListenerManager previene warnings de múltiples listeners.

---

**Documento generado por:** GitHub Copilot (Claude Sonnet 4.5)  
**Fecha:** 3 de Marzo, 2026  
**Proyecto:** The Real Blood - Unity FPS/TPS Game  
**Versión:** 1.0 - Documentación Completa

---

## 💡 CÓMO CONVERTIR A PDF

### **Opción 1: VS Code (recomendado)**
1. Instala extensión "Markdown PDF"
2. Abre este archivo .md
3. Ctrl+Shift+P → "Markdown PDF: Export (pdf)"

### **Opción 2: Pandoc (línea de comandos)**
```bash
pandoc DOCUMENTACION_SCRIPTS.md -o DOCUMENTACION_SCRIPTS.pdf
```

### **Opción 3: Navegador**
1. Instala extensión Markdown Viewer en Chrome/Edge
2. Abre el .md en navegador
3. Ctrl+P → Guardar como PDF

### **Opción 4: Word/Google Docs**
1. Copia el contenido
2. Pega en Word/Google Docs
3. Exporta como PDF

---

**FIN DEL DOCUMENTO**
