# Mini Sistema de Gestión de Tickets (Prueba Técnica)

Este repositorio contiene un sistema ligero de soporte técnico, diseñado y desarrollado para cumplir con los requerimientos técnicos de la evaluación, y expandido con características adicionales de administración.

El sistema permite a cualquier persona reportar problemas y adjuntar evidencia sin necesidad de crear una cuenta. Por otro lado, proporciona un panel de administración seguro para que el equipo de soporte pueda revisar, filtrar, responder y cerrar dichos tickets, manteniendo la privacidad de los archivos adjuntos.
* URL Desplegado: https://ticketappdev.runasp.net/
---

## Características y Requisitos Cumplidos (Incluyendo Valor Agregado)

* **Creación Pública de Tickets:** Interfaz abierta para capturar asunto, descripción, datos de contacto y archivos adjuntos.
* **Validación de Evidencia:** Control estricto de archivos en el servidor (máximo 3 archivos por envío, tamaño máximo de 5 MB cada uno, limitados a `.jpg`, `.png`, `.webp` y `.pdf`).
* **Seguridad de Archivos (Zero Public Access):** Los adjuntos se almacenan en el directorio interno `App_Data/Attachments` y no son accesibles públicamente. Solo se sirven mediante un endpoint protegido que valida la sesión del usuario.
* **Panel de Soporte Autenticado:** Acceso restringido para el personal mediante login.
* **Gestión de Estados:** Capacidad de filtrar tickets por estado (Abierto, En Progreso, Resuelto).
* **Auditoría de Soporte y Trazabilidad (Extra):** El personal puede colocar sus comentarios y adjuntar su propia evidencia de resolución al cerrar un ticket. Además, el sistema **registra exactamente qué usuario de soporte cerró o actualizó el ticket**, mejorando el control de calidad.
* **Gestión de Usuarios (Extra):** Se agregó un panel administrativo que permite la creación de nuevos usuarios de soporte y la gestión/cambio de contraseñas, yendo más allá de una cuenta estática.
* **Manejo de Errores:** Validaciones preventivas de tamaño y tipo de archivo, así como control de excepciones en la base de datos.

---

## Arquitectura y Stack Tecnológico

* **Framework:** .NET 10
* **Frontend:** Blazor (Interactive Server) + HTML5.
* **Estilos:** CSS3 puro (sin uso de librerías externas). Toda la UI, incluyendo modales responsivos, fue construida con ayuda de la IA Gemini.
* **Base de Datos:** SQLite (Embebida y autogenerada).
* **ORM:** Dapper (Micro-ORM elegido para maximizar el rendimiento y control sobre SQL).
* **Alojamiento:** Desplegado en **MonsterASP.NET** (garantizando almacenamiento en disco persistente).

---

## Instrucciones para ejecución local

La aplicación tiene una arquitectura *Zero-Friction*: no requiere configuraciones complejas, scripts de base de datos previos ni contenedores externos.

### Prerrequisitos
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado.

### Pasos

1. Clona el repositorio en tu máquina local:

       git clone https://github.com/wearandas84/TicketApp
       cd TicketApp

2. Restaura las dependencias (Dapper y SQLite):

       dotnet restore

3. Ejecuta la aplicación:

       dotnet run

4. Abre tu navegador web en la URL indicada en la consola. La base de datos `tickets.db` se autogenerará en el primer arranque.

---

## Uso del Sistema y Credenciales

**1. Flujo de Usuario (Público):**
Ingresa a la página principal (`/`) para llenar el formulario de reporte y subir imágenes o PDFs de evidencia.

**2. Flujo de Soporte (Privado):**
Navega a `/login` para acceder al panel de control.

* **Usuario por defecto:** `admin`
* **Contraseña por defecto:** `admin123`

Desde él `/dashboard`, podrás ver los tickets, gestionar usuarios, registrar tu usuario en la resolución del ticket y adjuntar evidencia interna de cierre.

---

## Uso de Inteligencia Artificial

Durante el desarrollo de esta prueba, utilicé **Google Gemini** como un asistente de *pair programming*. Mi objetivo no fue delegar la lógica de negocio, sino rebotar ideas de arquitectura, generar la estructura base de estilos, validar *trade-offs* de infraestructura y acelerar la escritura de código repetitivo (scaffolding).

### Ejemplos de Prompts Estratégicos

1. *"Necesito cumplir con la restricción de NO usar librerías de UI (como Bootstrap o Tailwind). Genera toda la estructura CSS desde cero utilizando variables CSS, flexbox para los grids y diseño de modales interactivos para Blazor."*
    * **Objetivo:** Delegar en la IA la maquetación pura en CSS para agilizar el diseño visual. La IA me ayudó a construir la base visual responsiva y estructurar las clases necesarias, asegurando que la aplicación luzca profesional sin romper la restricción técnica de librerías externas.

2. *"Considerando que necesito alojar una aplicación con SQLite y archivos locales, y que servicios gratuitos como Render o Heroku usan sistemas efímeros: Analiza si me conviene más hacer self-hosting usando Docker y Cloudflare Tunnels (Zero Trust) en mi servidor físico, o utilizar el alojamiento persistente de MonsterASP.NET."*
    * **Objetivo:** Tomar una decisión de infraestructura (DevOps) para evitar el principal punto de fallo en despliegues gratuitos. Evalué con la IA montar la app on-premise, pero finalmente opté por MonsterASP por su soporte nativo para WebSockets y almacenamiento persistente en disco.
   
3. *"Ayúdame a estructurar el esquema de tablas en SQLite y las consultas clave usando Dapper para este sistema."*
    * **Objetivo:** Delegar la escritura mecánica de los comandos de creación (DDL) e inserción (DML), reduciendo el *time-to-market* y centrando mi tiempo en analizar el diseño del modelo relacional.

4. *"Ayúdame a expandir el modelo de datos original en SQLite y las consultas de Dapper para registrar el ID del usuario que cierra el ticket y añadir una tabla para un panel de administración de usuarios."*
    * **Objetivo:** Ampliar los requerimientos originales de forma rápida. Utilicé la IA para reescribir el DDL y las inserciones, permitiendo enfocar mi tiempo en la integración de la trazabilidad y el control de contraseñas dentro de la UI.

### Correcciones y Decisiones de Ingeniería (Descartes de la IA)

Los modelos de IA tienden a sugerir el uso de frameworks pesados por inercia. Estas fueron mis intervenciones críticas sobre las sugerencias generadas:

1. **Rechazo al uso de JSInterop para manipulación del DOM:**
   La IA sugirió en varias ocasiones utilizar JavaScript (`IJSRuntime`) para invocar scripts de modales o atrapar el foco.
    * **Decisión:** Descarté por completo estas sugerencias. Obligué a que toda la interactividad del CSS generado se controlara de forma nativa con el motor de C# (`@if (IsVisible)`), manteniendo el circuito de Blazor Server limpio y rápido.

2. **Interceptación del flujo de autenticación (Antiforgery vs Blazor):**
   La IA propuso utilizar el componente estándar `<EditForm>` de Blazor para el inicio de sesión. Esto generaba una condición de carrera con el middleware de *Antiforgery* al emitir la cookie (`SignInAsync`) dentro del ciclo interactivo.
    * **Decisión:** Eliminé el modelo acoplado y diseñé un formulario HTML tradicional apuntando a un Minimal API (`/api/login`), apagando selectivamente la validación del token solo en ese endpoint para recuperar el control HTTP.

3. **Rechazo al ORM Pesado (Entity Framework):**
   En sus primeras iteraciones, la IA asumió por defecto el uso de Entity Framework Core.
    * **Decisión:** Lo descarté deliberadamente. Para el alcance del proyecto, EF Core añadía dependencias y una sobrecarga innecesaria. Opté por inyectar Dapper y controlar las sentencias SQL manualmente, garantizando máxima velocidad y menor consumo de recursos.