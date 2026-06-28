# Juego de Memoria

![Memory Game Banner](https://img.shields.io/badge/Juego-Memoria-blue?style=for-the-badge&logo=gamepad)
[![Angular](https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.io/)
[![ASP.NET](https://img.shields.io/badge/ASP.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![MySQL](https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)

**Aplicación web multijugador en línea del clásico juego de memoria (parejas).**

🌐 Sitio web: https://memory-game-david.vercel.app/

---

## 📖 Descripción

**Juego de Memoria** es una aplicación web multijugador desarrollada con **ASP.NET** en el backend y **Angular** en el frontend. Los usuarios pueden registrarse, iniciar sesión, gestionar su perfil, añadir amigos y competir en partidas en línea del clásico juego de concentración por parejas.

---

## 🎮 Reglas del Juego

El juego de memoria es un juego de concentración que se juega con cartas dispuestas **boca abajo en una cuadrícula**. Las reglas son:

1. En su turno, el jugador voltea **dos cartas**.
2. Si las cartas **coinciden**, el jugador se las queda y **vuelve a jugar**.
3. Si las cartas **no coinciden**, se vuelven a colocar boca abajo y el turno pasa al siguiente jugador.
4. El objetivo es recordar la posición de las cartas y formar el **mayor número de pares** posibles.
5. Gana el jugador que consigue **más parejas** al final de la partida.

> ⏱️ Cada turno tiene un límite de **2 minutos**. Si el tiempo se agota, el jugador pierde la partida.

---

## 🛠️ Tecnologías Utilizadas

### Frontend
| Tecnología | Descripción |
|---|---|
| ![Angular](https://img.shields.io/badge/Angular-DD0031?logo=angular&logoColor=white) | Framework principal del frontend |
| ![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white) | Lenguaje tipado para el frontend |
| ![HTML5](https://img.shields.io/badge/HTML5-E34F26?logo=html5&logoColor=white) | Estructura de la aplicación |
| ![CSS3](https://img.shields.io/badge/CSS3-1572B6?logo=css3&logoColor=white) | Estilos y diseño responsivo |

### Backend
| Tecnología | Descripción |
|---|---|
| ![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white) | Lenguaje del backend (ASP.NET) |
| ![Node.js](https://img.shields.io/badge/Node.js-339933?logo=nodedotjs&logoColor=white) | Entorno de ejecución |

### Base de Datos
| Tecnología | Descripción |
|---|---|
| ![MySQL](https://img.shields.io/badge/MySQL-4479A1?logo=mysql&logoColor=white) | Base de datos principal |
| ![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=white) | Base de datos local/desarrollo |

---

## ✨ Funcionalidades

### 👤 Autenticación y Usuarios
- **Registro** con avatar personalizado (o avatar por defecto), apodo único, correo y contraseña hasheada.
- **Inicio de sesión** mediante apodo o correo electrónico, con opción de mantener la sesión activa.
- **Gestión de perfil**: edición de avatar, apodo, correo y contraseña.

### 👥 Sistema de Amigos
- Envío y recepción de **solicitudes de amistad** con sistema de confirmación.
- **Amistad bidireccional**: ambos usuarios deben aceptar la solicitud.
- Visualización del **estado** de cada amigo: conectado, desconectado o jugando.
- Buscador de usuarios y amigos con búsqueda **insensible a mayúsculas y tildes**.

### 🎲 Modos de Juego (Emparejamiento)
- **Vs. Bot**: juega contra la máquina.
- **Oponente aleatorio**: búsqueda automática de emparejamiento.
- **Invitar a un amigo**: solo amigos con estado "conectado".

### 🏟️ Durante la Partida
- Juego en tiempo real con **turnos temporizados**.
- **Chat integrado** para comunicación entre jugadores.
- Modal de resultados al finalizar con opción de **revancha**.

### 📊 Historial y Perfil
- Historial de partidas paginado (5, 10, 15 o 20 por página) en orden cronológico inverso.
- Información por partida: juego, puntuación, jugadores, resultado y tiempo.
- Vista de perfil de otros usuarios con opción de enviar/eliminar amistad.

### 🔧 Panel de Administración
- Listado de todos los usuarios con su rol.
- Cambio de rol de usuario.
- Prohibición o desbloqueo de acceso (con confirmación previa).

### 📱 PWA y Responsividad
- Configurada como **Progressive Web App (PWA)**.
- Diseño **responsivo** adaptado a cualquier tamaño de pantalla.
- Redirección automática a la vista principal con notificación si se pierde la conexión con el servidor.

---

## 🗺️ Vistas de la Aplicación

```
📄 Principal          → Información del juego, reglas e imágenes
🔐 Login / Registro   → Autenticación y creación de cuenta
🏠 Menú               → Panel principal con lista de amigos y estadísticas globales
🎯 Emparejamiento     → Sala de espera antes de iniciar la partida
🃏 Juego              → Vista de la partida en curso con chat
👤 Perfil             → Información y estadísticas del usuario
⚙️  Administración    → Panel de gestión (solo para administradores)
```

> 🔒 Las vistas **Principal**, **Login** y **Registro** son las únicas accesibles sin autenticación.
