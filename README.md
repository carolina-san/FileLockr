
# FileLockr - **Gestor de Archivos Cifrados**

---

## **Descripción del Proyecto**
**FileLockr** es una aplicación de Windows Forms diseñada para gestionar, cifrar y descifrar archivos de manera segura utilizando algoritmos avanzados de criptografía. Este proyecto integra servicios RESTful y técnicas de compresión para ofrecer un sistema robusto de protección de datos.

---

## **Características Principales**
### 1. **Gestión de Usuarios**
- Inicio de sesión seguro con autenticación mediante tokens.
- Registro de nuevos usuarios con claves RSA generadas dinámicamente.

### 2. **Cifrado y Compresión de Archivos**
- Uso de AES (Advanced Encryption Standard) para cifrar archivos.
- Compresión de archivos antes de su cifrado para optimizar el almacenamiento.

### 3. **Descompresión y Descifrado de Archivos**
- Recuperación de archivos cifrados del servidor.
- Descompresión y descifrado utilizando claves privadas protegidas.

### 4. **Panel de Interfaz Gráfica**
- Interfaz amigable con secciones separadas para Acceso, Listado y Cifrado de archivos.

---

## **Tecnologías Utilizadas**
- **Lenguaje de Programación:** C#
- **Framework:** .NET
- **Criptografía:** AES y RSA
- **Backend:** API RESTful
- **Base de Datos:** SQL Server

---

## **Requisitos del Sistema**
- **Sistema Operativo:** Windows 10/11
- **Entorno de Ejecución:** .NET Framework 4.8 o superior
- **Herramientas Necesarias:** Visual Studio 2022

---

## **Instalación**
1. Clona este repositorio en tu máquina local.
   ```bash
   git clone https://github.com/usuario/filelockr.git
   ```
2. Abre el proyecto con Visual Studio 2022.
3. Configura la cadena de conexión de la base de datos en `appsettings.json`.
4. Compila y ejecuta el proyecto.

---

## **Contribuciones**
Las contribuciones son bienvenidas. Por favor, crea un fork del repositorio, realiza tus cambios y envía un pull request.

---

## **Licencia**
Este proyecto está licenciado bajo la Licencia MIT. Consulta el archivo LICENSE para más detalles.
