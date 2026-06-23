# Sistema de Reservas de Espacios — FEI / UV

Aplicación web (ASP.NET Core 10, Razor Pages) con base de datos **MySQL**.
Pensada para correr en **dos contenedores**: uno para la base de datos y
otro para la aplicación.

## Opción A — Docker (recomendada, dos contenedores)

Requisito: **Docker Desktop**. No necesitas instalar .NET ni MySQL en tu PC.

Desde la carpeta `ReservaEspacios` (donde está `docker-compose.yml`):

```bash
docker compose up --build
```

Esto levanta:
- **reservas-db** -> MySQL 8 (contenedor separado, con volumen `dbdata` que
  conserva los datos entre reinicios).
- **reservas-web** -> la aplicacion, que espera a que la base este lista,
  crea el esquema y siembra los datos iniciales.

Abre **http://localhost:8080**

Para detener: `Ctrl+C` y luego `docker compose down`
(agrega `-v` si quieres borrar tambien los datos: `docker compose down -v`).

### Conectarte a la base de datos por fuera (opcional)

El puerto 3306 esta expuesto, asi que puedes abrir MySQL Workbench / DBeaver:
- Host `localhost`, Puerto `3306`
- Usuario `root`, Contrasena `rootpassword` (o `appuser` / `apppassword`)
- Base de datos `reservas_fei`

### Por que la base "se crea sola"

El **servidor** de base de datos vive en su propio contenedor (con su volumen).
La aplicacion solo crea el **esquema** (las tablas) la primera vez, con
`EnsureCreated()`, y mete los datos iniciales. Asi la base queda totalmente
separada de la app: puedes reiniciar la web sin perder datos, escalar cada
parte por separado, etc.

> Si tu profesor pide crear el esquema con un script SQL a mano, en
> `docs/schema.sql` esta el DDL del modelo (MySQL) para ejecutarlo
> directamente en el contenedor de la base.

## Opcion B — Local sin Docker

Requisitos: **.NET SDK 10** y un **MySQL** corriendo en `localhost:3306`
con la base `reservas_fei` y el usuario configurado en
`appsettings.json` (`ConnectionStrings:Default`).

```bash
cd ReservaEspacios
dotnet restore
dotnet run
```

> El proveedor MySQL es `Pomelo.EntityFrameworkCore.MySql` (fijado a 8.0.3).
> Si `dotnet restore` no encuentra esa version en tu entorno, ejecuta
> `dotnet add package Pomelo.EntityFrameworkCore.MySql` para tomar la ultima
> compatible y ajusta la version del servidor en `Program.cs` si tu MySQL no
> es 8.0.

## Cuenta de administrador (precargada)

- **Correo:** `admin@fei.uv.mx`
- **Contrasena:** `Admin123!`

Los usuarios (profesores/alumnos) se crean desde **Registrarse**.

## Que incluye

**Usuario:** registro/login, ver espacios, crear reservacion Normal
(auto-aprobada; max. 3 h; 1 dia; min. 1 h de anticipacion; max. 2 activas)
o UV (institucional; varios dias; requiere autorizacion); *Mis reservaciones*
con cancelar, confirmar uso, cancelar serie y apelar.

**Administrador:** panel con indicadores y boton de *procesar expiraciones*,
autorizar/rechazar eventos UV (series completas juntas), resolver apelaciones,
CRUD de espacios (dias de operacion y horario), fechas sin servicio globales,
gestion de usuarios (faltas de 30 dias + sugerencia de bloqueo, bloqueo manual),
calendario semanal e historial de bloqueos.

La logica de reglas vive en `Services/ReservaService.cs` e
`InasistenciaService.cs`.

## Decisiones de diseno (para la defensa)

- **Dias de operacion:** tabla `DiaOperacionEspacio` (PK compuesta
  espacio + dia). El horario lo da el espacio. FEI = lunes a viernes.
- **Fechas sin servicio:** tabla **global** (aplica a todos los espacios);
  dia completo o por rango de horas.
- **Reservas de varios dias (solo UV):** una fila por dia, agrupadas con
  `IdGrupoReserva`. Se omiten los dias en que el espacio no abre o que caen
  en una fecha sin servicio; un traslape en cualquier dia rechaza toda la solicitud.
- **Inasistencias:** sin contador almacenado. Se cuentan al vuelo solo los
  eventos **Normal** "no realizada" de los ultimos **30 dias**. Umbral **3**
  dispara una *sugerencia*; el bloqueo es **manual**.
- **Confirmacion de uso:** desde la hora de apertura del espacio hasta la
  medianoche del dia; lo no confirmado pasa a "no realizada".

## Estructura

```
Models/          Entidades y enums
Data/            AppDbContext + DbSeeder
Services/        ReservaService, InasistenciaService, PasswordHasher
Pages/           Razor Pages (Account, Espacios, Reservaciones, Admin/*)
Dockerfile       Imagen de la aplicacion
docker-compose.yml   Orquesta db (MySQL) + web (app)
docs/            schema.sql (DDL MySQL), ER.puml (diagrama), este README
```
