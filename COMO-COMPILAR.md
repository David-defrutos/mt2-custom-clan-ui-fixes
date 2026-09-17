# Como se compila este mod

Igual que los clanes: **lo compila GitHub Actions**, porque el `nuget.config` apunta a
GitHub Packages y eso pide credenciales aunque el paquete sea publico. En el runner las
pone el `GITHUB_TOKEN` solo.

El workflow (`.github/workflows/build.yml`) dispara con cada push a `main` que toque
`src/`, y sube el DLL como artefacto **`mt2_custom_clan_ui_fixes.Plugin`**.

## De push a DLL instalado

```powershell
$mod  = "C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes"
$ddls = "D:\Juegos\MT2_mod\ddls\dll-nuevo"
Set-Location $mod

git add src
git commit -m "lo que sea"
git push
$sha = git rev-parse HEAD

# Actions tarda unos segundos en registrar el run: se espera al de ESTE commit
$runId = $null
for ($i = 0; $i -lt 24 -and -not $runId; $i++) {
    Start-Sleep -Seconds 5
    $runId = (gh run list --workflow build.yml --branch main --limit 10 --json databaseId,headSha |
              ConvertFrom-Json | Where-Object headSha -eq $sha).databaseId
}
if (-not $runId) { throw "Actions no ha registrado ningun run para $sha" }

gh run watch $runId --exit-status     # se queda aqui hasta que acaba

if ($LASTEXITCODE -ne 0) {
    gh run view $runId --log-failed | Select-String -Pattern 'error|MSB\d|NU\d{4}' | Select-Object -First 40
    return
}

# gh run download NO sobrescribe: si queda el DLL de antes, falla y te deja el viejo
Remove-Item "$ddls\*" -Recurse -Force -ErrorAction SilentlyContinue
gh run download $runId --name mt2_custom_clan_ui_fixes.Plugin --dir $ddls

# y puede no bajar nada sin dar error: hay que mirarlo, no darlo por hecho
$dll = Get-ChildItem $ddls -Recurse -Filter mt2_custom_clan_ui_fixes.Plugin.dll | Select-Object -First 1
if (-not $dll) { throw "el artefacto no ha bajado: $ddls esta vacio" }

Copy-Item $dll.FullName $mod -Force
"copiado $($dll.Length) bytes -> $mod"
```

**El bloque va autocontenido a proposito**: `$mod` y `$ddls` se declaran arriba porque la
terminal de la proxima vez no tiene por que ser la misma. Un `$runId` heredado de otra
sesion apunta a un run viejo, y entonces se instala un DLL que no es el que acabas de
compilar.

**Y el juego tiene que estar CERRADO al copiar el DLL.** BepInEx lo carga al arrancar: con
el juego abierto, la copia no hace nada y sigues viendo el comportamiento de antes. Paso el
16-sep y costo una ronda entera de pensar que el parche no funcionaba.

## Comprobar que va

Arranca el juego **despues** de copiar el DLL, abre el logbook en la pagina de mejoras y en
la de artefactos, y mira la traza:

```powershell
$log = "C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\LogOutput.log"
Select-String -Path $log -Pattern '\[LogbookFit\]|\[ArtifactsPaging\]' | Select-Object -Last 15
```

Este mod no tiene JSON, asi que el validador no lo mira: **la traza es la unica
comprobacion**. Si no ves las lineas que esperas de la version nueva, lo primero a descartar
es que el juego siga con el DLL viejo cargado. Se ve en el log: una sola linea
`Plugin mt2_custom_clan_ui_fixes.Plugin is loaded!` significa un unico arranque, y si esa
linea es anterior a la copia del DLL, estas mirando el mod de antes.

## Compilar en local

No sale a cuenta por lo del `nuget.config`, pero si algun dia hace falta, **la salida nunca
dentro de `plugins\`**: BepInEx escanea en profundidad y cargaria el plugin dos veces.

```powershell
dotnet build .\src -c Release --output D:\Juegos\MT2_mod\_dll-build\out
```

## Empaquetar para Thunderstore

Un paquete de Thunderstore es **un zip plano**: `manifest.json`, `icon.png` (256x256),
`README.md`, `CHANGELOG.md` y el DLL, todos en la raiz del zip, sin carpetas. Asi estan
montados los mods que ya tienes instalados, que es la mejor referencia.

**Antes de nada, el DLL tiene que estar bajado en la raiz del mod.**

```powershell
$ui  = "C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes"
$ver = "0.1.0"
$pkg = Join-Path $env:TEMP "CustomClanUIFixes-pkg"

Remove-Item $pkg -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pkg | Out-Null
New-Item -ItemType Directory -Path "$ui\build" -Force | Out-Null

Copy-Item "$ui\manifest.json","$ui\icon.png","$ui\README.md","$ui\CHANGELOG.md",
          "$ui\mt2_custom_clan_ui_fixes.Plugin.dll" $pkg

Compress-Archive -Path "$pkg\*" -DestinationPath "$ui\build\frutos-CustomClanUIFixes-$ver.zip" -Force
Get-ChildItem "$ui\build"
```

El zip se sube a mano en https://thunderstore.io/c/monster-train-2/create/ (hace falta
pertenecer a un **team** con el mismo nombre que el `namespace` del manifiesto).

Publicado en: https://thunderstore.io/c/monster-train-2/p/frutos/CustomClanUIFixes/

Tambien existe el **Thunderstore CLI** (`tcli build` / `tcli publish --token ...`), que lee
el `thunderstore.toml` que hay en el repo. Se descarga de
https://github.com/thunderstore-io/thunderstore-cli/releases. Hace lo mismo que el bloque de
arriba, mas la subida.

Dos avisos antes de publicar nada:

- Subir la version en **tres sitios a la vez**: `thunderstore.toml`, `manifest.json` y el
  `<Version>` del csproj. Thunderstore rechaza una version ya subida.
- El `version_number` tiene que ser `x.y.z`.

## Aviso: no instalar la version publicada en este perfil

Esta carpeta ES el repositorio de desarrollo y vive dentro de
`...\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes`, que es exactamente la
carpeta que el Thunderstore Mod Manager reclama al instalar el mod publicado: la vacia
antes de descomprimir. Al probarlo paso esto:

```
Failed to install mod [CustomClanUIFixes] to profile [Default]
UnauthorizedAccessException: Access to the path
'...\plugins\frutos-CustomClanUIFixes\.git\objects\00\1cd50d2...' is denied.
```

Solo se salvo porque `.git\objects` es de solo lectura. Reglas:

- **Nunca instalar el mod publicado en el perfil `Default`.**
- Probar la version publicada en un **perfil limpio** aparte.
- Lo ideal es sacar este repo de `plugins\` (por ejemplo a `D:\Juegos\MT2_mod\repos\`) y
  copiar solo el DLL al perfil de pruebas.

La pestana **Online** del gestor va con cache: un mod recien subido tarda un rato en
aparecer. Mientras tanto, *Import local mod* apuntando al zip de `build\`.


## Nota

El puente de ficheros de la sesion de Claude **no puede escribir en `.github\workflows\`**.
El YAML se deja en `D:\Juegos\MT2_mod\_dll-build\` y se mueve a mano.
