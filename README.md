# Mentality - Arquitectura y Patrón de Diseño (SOLID)

Este proyecto está estructurado bajo los principios de **Clean Architecture** y **SOLID**, adaptados al entorno de desarrollo en Unity. La arquitectura busca separar la lógica pura del juego del motor gráfico, permitiendo mayor modularidad, testabilidad y escalabilidad.

---

## 📂 Estructura del Proyecto

Las carpetas principales dentro de `Assets/` se dividen en lógica de desarrollo y assets estáticos:

```
Assets/
├── Scripts/                  # Código fuente C# estructurado en SOLID
│   ├── Domain/               # Capa de Dominio (Reglas de negocio puras, sin dependencias de Unity)
│   │   ├── Models/           # Entidades del juego y datos puros
│   │   └── Interfaces/       # Contratos esenciales (Interfaces de dominio)
│   │
│   ├── Application/          # Capa de Aplicación (Casos de uso y servicios coordinadores)
│   │   └── Interfaces/       # Abstracciones para servicios necesarios (ej. guardado, red)
│   │
│   ├── Infrastructure/       # Capa de Infraestructura (Implementaciones tecnológicas y dependencias externas)
│   │   └── Persistence/      # Guardado local, Base de datos (JSON, PlayerPrefs)
│   │
│   └── Presentation/         # Capa de Presentación (Controladores, Vistas y entrada de Unity)
│       ├── Controllers/      # Controladores de juego (MonoBehaviours que reciben entrada del usuario)
│       ├── Views/            # Componentes visuales encargados de la interfaz (UI Toolkit, Canvas)
│       └── Presenters/       # Intermediarios que conectan reactivamente los Modelos con las Vistas
│
├── UI/                       # Recursos visuales y estilos de UI Toolkit (No Scripts)
│   ├── Documents/            # Archivos .uxml (Layouts y árbol visual)
│   ├── Styles/               # Archivos .uss (Hojas de estilo)
│   └── PanelSettings/        # Configuraciones de resolución y escalado
│
├── Audio/                    # Audio y sonido ordenado
│   ├── Music/                # Banda sonora y temas de fondo (BGM)
│   └── SFX/                  # Efectos de sonido rápidos (UI, pisadas, impactos)
│
├── Shaders/                  # Archivos de Shaders y lógicas gráficas
├── ScriptableObjects/        # Configuración de datos estáticos persistentes en el Editor
├── Sprites/                  # Texturas y elementos 2D
├── Prefabs/                  # Plantillas de GameObjects listos para instanciar
└── Materials/                # Materiales de renderizado
```

---

## 🛠️ Aplicación de Principios SOLID en el Código

El proyecto inicializa con una plantilla del **Sistema de Salud y Persistencia**, que ilustra la aplicación práctica de cada principio:

### 1. S - Single Responsibility Principle (Responsabilidad Única)
Cada componente tiene una única razón para cambiar:
* **[Health.cs](Assets/Scripts/Domain/Models/Health.cs):** Solo calcula la salud, daño y curación. No sabe cómo se dibuja en pantalla ni cómo se guarda en disco.
* **[HealthBarView.cs](Assets/Scripts/Presentation/Views/HealthBarView.cs):** Solo se encarga de actualizar los sliders y textos en la UI.
* **[JsonSaveService.cs](Assets/Scripts/Infrastructure/Persistence/JsonSaveService.cs):** Solo se encarga de escribir y leer archivos en formato JSON.

### 2. O - Open/Closed Principle (Abierto/Cerrado)
El comportamiento debe ser extensible sin modificar el código existente.
* Los comportamientos externos o extensiones interactúan con el modelo de salud a través de eventos expuestos (`OnHealthChanged`, `OnDeath`) y métodos abiertos. No es necesario modificar la clase `Health` para agregar efectos visuales de daño, partículas o sonidos de muerte; otros scripts pueden suscribirse a sus eventos.

### 3. L - Liskov Substitution Principle (Sustitución de Liskov)
Las subclases o implementaciones deben ser sustituibles por sus interfaces base sin alterar el funcionamiento del programa.
* Si el sistema de combate requiere dañar algo, interactúa con la interfaz **[IDamageable.cs](Assets/Scripts/Domain/Interfaces/IDamageable.cs)**. Ya sea el jugador, un enemigo o un barril explosivo el que implemente la interfaz, el sistema los tratará a todos por igual sin necesidad de conocer su tipo concreto.

### 4. I - Interface Segregation Principle (Segregación de Interfaces)
Es mejor tener interfaces pequeñas y específicas que una interfaz genérica y sobrecargada.
* En lugar de una interfaz masiva como `ICharacter`, dividimos las responsabilidades en interfaces finas como **[IDamageable.cs](Assets/Scripts/Domain/Interfaces/IDamageable.cs)** (recibe daño) e **[IHealable.cs](Assets/Scripts/Domain/Interfaces/IHealable.cs)** (recibe curación). Si un elemento de entorno (como una pared destructible) puede recibir daño pero no curarse, solo implementará `IDamageable`.

### 5. D - Dependency Inversion Principle (Inversión de Dependencias)
Los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones.
* **[PlayerController.cs](Assets/Scripts/Presentation/Controllers/PlayerController.cs)** no interactúa directamente con `JsonSaveService` de la capa de infraestructura. En su lugar, depende de la abstracción **[ISaveService.cs](Assets/Scripts/Application/Interfaces/ISaveService.cs)**. Esto permite cambiar el guardado JSON local por un sistema de guardado en la nube (ej. PlayFab) sin cambiar una sola línea de código dentro del controlador del jugador.

---

## ⚡ Cómo Usar las Plantillas Creadas

1. Crea un GameObject en tu escena llamado `Player` y agrégale el componente `PlayerController`.
2. Crea un Slider de UI y agrégale el componente `HealthBarView`.
3. Arrastra tu `HealthBarView` al campo `Health Bar View` del `PlayerController` en el Inspector de Unity.
4. Presiona **Play**:
   * Teclas de prueba configuradas en `PlayerController`:
     * **[Space]** -> Recibir 10 de daño.
     * **[H]** -> Curar 10 de salud.
     * **[S]** -> Guardar la salud actual a un archivo JSON.
     * **[L]** -> Cargar la salud guardada desde el archivo JSON.
