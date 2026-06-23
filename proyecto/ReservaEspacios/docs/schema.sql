-- =====================================================================
-- Sistema de Reservas de Espacios — FEI / Universidad Veracruzana
-- Esquema relacional (MySQL) para documentación / referencia del ER.
--
-- NOTA: La aplicación .NET usa EF Core con SQLite y crea la base
-- automáticamente (EnsureCreated). Este script es la versión
-- "de papel" del modelo, equivalente, por si se quiere implementar
-- en MySQL o presentar el diagrama físico.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS reservas_fei
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE reservas_fei;

-- ---------------------------------------------------------------------
-- USUARIO  (profesores y alumnos)
-- Las inasistencias NO se almacenan: se calculan al vuelo del historial
-- de reservaciones dentro de una ventana de 30 días.
-- ---------------------------------------------------------------------
CREATE TABLE USUARIO (
  id_usuario      INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(150) NOT NULL,
  correo          VARCHAR(150) NOT NULL UNIQUE,
  contrasena      VARCHAR(255) NOT NULL,          -- hash PBKDF2 (salt.key)
  estado          ENUM('Activo','Bloqueado') NOT NULL DEFAULT 'Activo',
  fecha_registro  DATETIME NOT NULL
);

-- ---------------------------------------------------------------------
-- ADMINISTRADOR (secretaría)
-- ---------------------------------------------------------------------
CREATE TABLE ADMINISTRADOR (
  id_admin        INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(150) NOT NULL,
  correo          VARCHAR(150) NOT NULL UNIQUE,
  contrasena      VARCHAR(255) NOT NULL,
  estado          ENUM('Activo','Bloqueado') NOT NULL DEFAULT 'Activo',
  fecha_registro  DATETIME NOT NULL
);

-- ---------------------------------------------------------------------
-- ESPACIO  (Auditorio, Sala de Cristal, …)
-- Horario base de operación: hora_apertura / hora_cierre.
-- ---------------------------------------------------------------------
CREATE TABLE ESPACIO (
  id_espacio      INT AUTO_INCREMENT PRIMARY KEY,
  nombre          VARCHAR(150) NOT NULL,
  tipo            VARCHAR(50)  NOT NULL,
  capacidad       INT          NOT NULL,
  ubicacion       VARCHAR(200) NOT NULL,
  descripcion     VARCHAR(500) NULL,
  estado          ENUM('Disponible','Inactivo') NOT NULL DEFAULT 'Disponible',
  hora_apertura   TIME NOT NULL,
  hora_cierre     TIME NOT NULL
);

-- ---------------------------------------------------------------------
-- DIA_OPERACION_ESPACIO  (qué días de la semana abre cada espacio)
-- Clave compuesta. Si no hay fila para un día, está cerrado ese día.
-- ---------------------------------------------------------------------
CREATE TABLE DIA_OPERACION_ESPACIO (
  id_espacio      INT NOT NULL,
  dia_semana      ENUM('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo') NOT NULL,
  PRIMARY KEY (id_espacio, dia_semana),
  FOREIGN KEY (id_espacio) REFERENCES ESPACIO(id_espacio) ON DELETE CASCADE
);

-- ---------------------------------------------------------------------
-- FECHA_NO_SERVICIO  (global: aplica a TODOS los espacios de la facultad)
-- hora_inicio / hora_fin NULL = día completo; con valor = bloqueo parcial.
-- ---------------------------------------------------------------------
CREATE TABLE FECHA_NO_SERVICIO (
  id_fecha_no_servicio INT AUTO_INCREMENT PRIMARY KEY,
  fecha_inicio    DATE NOT NULL,
  fecha_fin       DATE NOT NULL,                  -- = fecha_inicio si es un solo día
  hora_inicio     TIME NULL,
  hora_fin        TIME NULL,
  tipo            ENUM('Feriado','Vacaciones','Suspension','Mantenimiento','EventoInstitucional') NOT NULL,
  motivo          VARCHAR(300) NOT NULL,
  id_admin        INT NOT NULL,
  fecha_registro  DATETIME NOT NULL,
  FOREIGN KEY (id_admin) REFERENCES ADMINISTRADOR(id_admin)
);

-- ---------------------------------------------------------------------
-- RESERVACION
-- id_grupo_reserva agrupa los días creados juntos (reserva de varios
-- días / semana, solo eventos UV). NULL = reserva de un solo día.
-- ---------------------------------------------------------------------
CREATE TABLE RESERVACION (
  id_reservacion     INT AUTO_INCREMENT PRIMARY KEY,
  id_usuario         INT NOT NULL,
  id_espacio         INT NOT NULL,
  tipo_evento        ENUM('Normal','UV') NOT NULL,
  fecha              DATE NOT NULL,
  hora_inicio        TIME NOT NULL,
  hora_fin           TIME NOT NULL,
  motivo             VARCHAR(500) NOT NULL,
  estado_reservacion ENUM('Activa','Cancelada','Finalizada','Pendiente') NOT NULL,
  estado_aprobacion  ENUM('NA','Pendiente','Aprobada','Rechazada') NOT NULL,
  se_realizo         ENUM('Pendiente','Realizada','NoRealizada') NOT NULL DEFAULT 'Pendiente',
  motivo_cancelacion VARCHAR(300) NULL,
  id_grupo_reserva   INT NULL,
  fecha_registro     DATETIME NOT NULL,
  FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario),
  FOREIGN KEY (id_espacio) REFERENCES ESPACIO(id_espacio)
);

-- ---------------------------------------------------------------------
-- APELACION  (cuando una reservación se marcó como 'no realizada')
-- ---------------------------------------------------------------------
CREATE TABLE APELACION (
  id_apelacion    INT AUTO_INCREMENT PRIMARY KEY,
  id_reservacion  INT NOT NULL,
  id_usuario      INT NOT NULL,
  justificacion   VARCHAR(500) NOT NULL,
  estado_apelacion ENUM('Pendiente','Aprobada','Rechazada') NOT NULL DEFAULT 'Pendiente',
  fecha_apelacion DATETIME NOT NULL,
  fecha_resolucion DATETIME NULL,
  id_admin        INT NULL,
  FOREIGN KEY (id_reservacion) REFERENCES RESERVACION(id_reservacion) ON DELETE CASCADE,
  FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario),
  FOREIGN KEY (id_admin) REFERENCES ADMINISTRADOR(id_admin)
);

-- ---------------------------------------------------------------------
-- HISTORIAL_BLOQUEO  (bitácora de bloqueos/desbloqueos manuales)
-- ---------------------------------------------------------------------
CREATE TABLE HISTORIAL_BLOQUEO (
  id_bloqueo      INT AUTO_INCREMENT PRIMARY KEY,
  id_usuario      INT NOT NULL,
  id_admin        INT NOT NULL,
  tipo_accion     ENUM('Bloqueo','Desbloqueo') NOT NULL,
  motivo          VARCHAR(300) NOT NULL,
  fecha_accion    DATETIME NOT NULL,
  FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario),
  FOREIGN KEY (id_admin) REFERENCES ADMINISTRADOR(id_admin)
);
